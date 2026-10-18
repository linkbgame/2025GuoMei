using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 瓦片地图管理器 —— 支持构建 Native 网格并调度 Burst/Job 版 PathFinding
/// 注意：
/// - 需要把 `Node` 和 `PathFinding` 按 Job/Burst 兼容的版本一起替换为前面建议的实现（`Node` 使用 blittable 字段：`int IsWalkable`, `float3 Center`, `int parentIndex` 等）。
/// - 本类负责在主线程把 tilemap 数据打平成 `NativeArray<Node>`，分配 job 所需的 Native 容器，调度并在完成后把结果转换回可用的 List<Node>（同步调用方式）。
/// </summary>
public class TilemapManager : MonoBehaviour
{
    public static TilemapManager instance;

    public Tilemap WalkableTilemap;
    public Tilemap PlacementTilemap;
    public Tilemap UnreachableTilemap;
    public Tilemap BuildingAreaTilemap;

    private Vector3Int m_GridOffset; // 左下角原点
    private JobHandle jobHandle;

    // Job 所需的原生容器
    private NativeArray<Node> gridArray;          // 扁平化网格，长度 = width * height
    private int width;
    private int height;

    // 输出路径点（float2 的中心点），由 PathFinding 写入
    private NativeList<float2> nodes;             // 持久分配，Job 写入后主线程读取

    // 为优化版 PathFinding 分配的可复用容器（避免每次调度分配）
    private NativeArray<int> openHeap;    // binary heap 存放节点索引，大小 = grid.Length
    private NativeArray<int> heapIndex;   // 节点在 heap 中的位置，-1 表示不在 heap 中
    private NativeArray<byte> closedFlags; // 关闭标志 0/1

    private void Awake()
    {
        instance = this;

        // 输出列表持久化分配（会被 job 写入）
        nodes = new NativeList<float2>(Allocator.Persistent);

        // 构建扁平化网格并分配辅助数组
        BuildGridNative();
    }

    private void OnValidate()
    {
        // 编辑器里修改 Tilemap 后可以重新构建网格
        if (Application.isPlaying) return;
        if (WalkableTilemap != null)
        {
            var bounds = WalkableTilemap.cellBounds;
            m_GridOffset = bounds.min;
            width = bounds.size.x;
            height = bounds.size.y;
        }
    }

    private void BuildGridNative()
    {
        if (WalkableTilemap == null) return;

        var bounds = WalkableTilemap.cellBounds;
        width = bounds.size.x;
        height = bounds.size.y;
        m_GridOffset = bounds.min;
        int len = width * height;

        // 释放旧的 Native 容器（如果存在）
        if (gridArray.IsCreated) gridArray.Dispose();
        if (openHeap.IsCreated) openHeap.Dispose();
        if (heapIndex.IsCreated) heapIndex.Dispose();
        if (closedFlags.IsCreated) closedFlags.Dispose();

        // 分配新的容器（Persistent，复用）
        gridArray = new NativeArray<Node>(len, Allocator.Persistent);

        // openHeap 使用 len 大小以保证可以存放全部节点
        openHeap = new NativeArray<int>(len, Allocator.Persistent);
        heapIndex = new NativeArray<int>(len, Allocator.Persistent);
        closedFlags = new NativeArray<byte>(len, Allocator.Persistent);

        // 初始化 heapIndex 为 -1，closedFlags 为 0（避免未初始化数据）
        for (int i = 0; i < len; i++)
        {
            heapIndex[i] = -1;
            closedFlags[i] = 0;
        }

        Vector3 cellSize = WalkableTilemap.cellSize;
        float halfX = cellSize.x * 0.5f;
        float halfY = cellSize.y * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int leftButtomPosition = new Vector3Int(x + m_GridOffset.x, y + m_GridOffset.y);
                bool isWalkable = CanPlaceWalk(leftButtomPosition);

                // 注意：这里使用的是 Job 版 Node 构造：Node(int x,int y,float3 center,int isWalkable,int parent)
                var center = new float3(leftButtomPosition.x + halfX, leftButtomPosition.y + halfY, 0f);
                var n = new Node(leftButtomPosition.x, leftButtomPosition.y, center, isWalkable ? 1 : 0, -1);

                gridArray[x + y * width] = n;
            }
        }
    }

    public bool CanPlaceWalk(Vector3Int _position)
    {
        return WalkableTilemap != null && WalkableTilemap.HasTile(_position);
    }

    public bool CanPlaceBuilding(Vector3Int _position)
    {
        return BuildingAreaTilemap != null && BuildingAreaTilemap.HasTile(_position) && !IsPlaceAreaOverObstacle(_position);
    }

    private bool IsPlaceOverUreachbleArea(Vector3Int _position)
    {
        return UnreachableTilemap != null && UnreachableTilemap.HasTile(_position) || IsPlaceAreaOverObstacle(_position);
    }

    private bool IsPlaceAreaOverObstacle(Vector3Int _position)
    {
        Vector3 tileSize = WalkableTilemap.cellSize;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(_position + tileSize * .5f, tileSize * .9f, 0);
        foreach (var collider in colliders)
        {
            if (collider.gameObject.tag == "Unit") return true;
        }
        return false;
    }

    // 同步接口：构建并执行 PathFinding Job，等待完成后返回结果 List<Node>
    // 使用优化版 PathFinding（binary heap），传入预分配的辅助数组以避免重复分配
    public List<Node> FindPath(Vector3 _startPosition, Vector3 _endPosition)
    {
        // 确保网格已构建
        if (!gridArray.IsCreated) BuildGridNative();

        int len = width * height;
        if (len == 0) return new List<Node>();

        // 清理输出容器
        if (!nodes.IsCreated) nodes = new NativeList<float2>(Allocator.Persistent);
        nodes.Clear();

        // 确保辅助数组已分配且大小足够；如果大小不够，重建
        if (!openHeap.IsCreated || openHeap.Length != len ||
            !heapIndex.IsCreated || heapIndex.Length != len ||
            !closedFlags.IsCreated || closedFlags.Length != len)
        {
            BuildGridNative(); // 会重新分配所有数组
        }

        // 构造 Job
        var pfJob = new PathFinding
        {
            grid = gridArray,
            width = width,
            height = height,
            offsetX = m_GridOffset.x,
            offsetY = m_GridOffset.y,
            startPos = new float2(_startPosition.x, _startPosition.y),
            endPos = new float2(_endPosition.x, _endPosition.y),
            outPathCenters = nodes,
            openHeap = openHeap,
            heapIndex = heapIndex,
            closedFlags = closedFlags
        };

        // 调度并等待完成（同步）
        jobHandle = pfJob.Schedule();
        jobHandle.Complete();

        // Job 已完成：nodes 中包含按 起点->终点 顺序的中心点 float2
        // 将 nodes 转换为 List<Node>（从 gridArray 中查找对应 Node）
        var result = new List<Node>(nodes.Length);
        for (int i = 0; i < nodes.Length; i++)
        {
            float2 c = nodes[i];
            int fx = (int)math.floor(c.x);
            int fy = (int)math.floor(c.y);
            int gx = fx - m_GridOffset.x;
            int gy = fy - m_GridOffset.y;
            if (gx >= 0 && gy >= 0 && gx < width && gy < height)
            {
                int idx = gx + gy * width;
                result.Add(gridArray[idx]);
            }
        }

        return result;
    }

    // 如果想要异步版本（不等待 Complete），可以:
    // - 在调度后将 jobHandle 存好并在 LateUpdate 中检测 jobHandle.IsCompleted -> Complete -> 读取 nodes -> 处理结果并释放/复用辅助数组。
    // - 需要确保在 Job 完成前不要释放或重写 gridArray / openHeap / heapIndex / closedFlags / nodes。

    private void LateUpdate()
    {
        // 当前实现为同步 Schedule+Complete，LateUpdate 不处理 job 完成逻辑。
        // 若改为异步，请在这里 Complete 并读取结果（并确保线程安全）。
    }

    private void OnDestroy()
    {
        // 完成任何未完成的 job
        if (jobHandle.IsCompleted == false)
            jobHandle.Complete();

        if (gridArray.IsCreated) gridArray.Dispose();
        if (nodes.IsCreated) nodes.Dispose();
        if (openHeap.IsCreated) openHeap.Dispose();
        if (heapIndex.IsCreated) heapIndex.Dispose();
        if (closedFlags.IsCreated) closedFlags.Dispose();
    }
}


