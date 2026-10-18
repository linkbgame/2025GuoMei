using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Burst + Job 兼容的 A* 实现：使用二叉堆 (binary heap) 作为 open list，使用数组作为 closed 标志与 heap 索引映射
/// - 不在 Job 中调用任何 UnityEngine API
/// - 需要调用方（主线程）准备 grid (NativeArray<Node>) 及 outPathCenters、openHeap、heapIndices、closedFlags 的分配
/// </summary>
[BurstCompile]
public struct PathFinding : IJob
{
    // 输入/可写网格（扁平化）
    public NativeArray<Node> grid; // length == width * height

    // 网格尺寸与偏移（世界左下角整点）
    public int width;
    public int height;
    public int offsetX;
    public int offsetY;

    // 起终点（世界坐标）
    public float2 startPos;
    public float2 endPos;

    // 输出（中心点序列，起点->终点顺序）
    public NativeList<float2> outPathCenters;

    // Heap 与辅助数组（由调用方分配，长度应为 grid.Length）
    public NativeArray<int> openHeap;      // binary heap 存放节点索引
    public NativeArray<int> heapIndex;     // 对应节点在 heap 中的位置（-1 表示不在 heap 中）
    public NativeArray<byte> closedFlags;  // 0/1 标志：是否已关闭

    // ----- 辅助方法（基于 grid 的索引、堆操作、代价计算） -----
    private int ToIndex(int gx, int gy) => gx + gy * width;
    private int GetGridX(int index) => index % width;
    private int GetGridY(int index) => index / width;

    private int FindNodeIndexFromWorldPos(float2 pos)
    {
        int fx = (int)math.floor(pos.x);
        int fy = (int)math.floor(pos.y);
        int gridX = fx - offsetX;
        int gridY = fy - offsetY;
        if (gridX >= 0 && gridY >= 0 && gridX < width && gridY < height)
            return ToIndex(gridX, gridY);
        return -1;
    }

    private bool CompareNodes(int a, int b)
    {
        // 返回 true if node a has higher priority (smaller fCost) than b
        Node na = grid[a];
        Node nb = grid[b];
        if (na.fCost < nb.fCost) return true;
        if (na.fCost == nb.fCost && na.hCost < nb.hCost) return true;
        return false;
    }

    private void SwapHeap(int i, int j)
    {
        int ai = openHeap[i];
        int aj = openHeap[j];
        openHeap[i] = aj;
        openHeap[j] = ai;
        heapIndex[ai] = j;
        heapIndex[aj] = i;
    }

    private void HeapifyUp(int idx)
    {
        int i = idx;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (CompareNodes(openHeap[i], openHeap[parent]))
            {
                SwapHeap(i, parent);
                i = parent;
            }
            else break;
        }
    }

    private void HeapifyDown(int idx, int heapCount)
    {
        int i = idx;
        while (true)
        {
            int left = (i << 1) + 1;
            int right = left + 1;
            int smallest = i;
            if (left < heapCount && CompareNodes(openHeap[left], openHeap[smallest])) smallest = left;
            if (right < heapCount && CompareNodes(openHeap[right], openHeap[smallest])) smallest = right;
            if (smallest != i)
            {
                SwapHeap(i, smallest);
                i = smallest;
            }
            else break;
        }
    }

    private void HeapPush(int nodeIndex, ref int heapCount)
    {
        int pos = heapCount;
        openHeap[pos] = nodeIndex;
        heapIndex[nodeIndex] = pos;
        heapCount = pos + 1;
        HeapifyUp(pos);
    }

    private int HeapPop(ref int heapCount)
    {
        int root = openHeap[0];
        int lastPos = heapCount - 1;
        if (lastPos > 0)
        {
            openHeap[0] = openHeap[lastPos];
            heapIndex[openHeap[0]] = 0;
        }
        heapIndex[root] = -1;
        heapCount = lastPos;
        if (heapCount > 0) HeapifyDown(0, heapCount);
        return root;
    }

    private bool IsInOpen(int nodeIndex) => heapIndex[nodeIndex] != -1;

    // 曼哈顿/对角混合距离（与原实现一致）
    private int FindDistanceBetweenTwoNodes(int indexA, int indexB)
    {
        Node a = grid[indexA];
        Node b = grid[indexB];
        int distanceX = math.abs(a.ButtomX - b.ButtomX);
        int distanceY = math.abs(a.ButtomY - b.ButtomY);
        if (distanceX > distanceY) return distanceY * 14 + (distanceX - distanceY) * 10;
        return distanceX * 14 + (distanceY - distanceX) * 10;
    }

    private void BuildPath(int endIdx, int startIdx)
    {
        // 使用临时本地栈（NativeList）反向收集索引
        var rev = new NativeList<int>(Allocator.Temp);
        int cur = endIdx;
        while (cur != -1 && cur != startIdx)
        {
            rev.Add(cur);
            cur = grid[cur].parentIndex;
        }
        // 将 rev 逆序写入 outPathCenters（起点 -> 终点）
        for (int i = rev.Length - 1; i >= 0; i--)
        {
            int idx = rev[i];
            float3 c = grid[idx].Center;
            outPathCenters.Add(new float2(c.x, c.y));
        }
        rev.Dispose();
    }

    public void Execute()
    {
        outPathCenters.Clear();

        int len = grid.Length;
        if (len == 0) return;

        // 初始化：重置 costs、parent、辅助 arrays
        for (int i = 0; i < len; i++)
        {
            Node n = grid[i];
            n.gCost = int.MaxValue;
            n.hCost = 0;
            n.fCost = int.MaxValue;
            n.parentIndex = -1;
            grid[i] = n;

            heapIndex[i] = -1;
            closedFlags[i] = 0;
            // openHeap 不需要初始化内容
        }

        int startIndex = FindNodeIndexFromWorldPos(startPos);
        int endIndex = FindNodeIndexFromWorldPos(endPos);
        if (startIndex == -1 || endIndex == -1) return;

        // 起点初始化
        {
            Node s = grid[startIndex];
            s.gCost = 0;
            s.hCost = FindDistanceBetweenTwoNodes(startIndex, endIndex);
            s.fCost = s.hCost;
            grid[startIndex] = s;
        }

        int heapCount = 0;
        HeapPush(startIndex, ref heapCount);

        // A* 主循环（使用二叉堆）
        while (heapCount > 0)
        {
            int current = HeapPop(ref heapCount);
            if (current == endIndex)
            {
                BuildPath(endIndex, startIndex);
                return;
            }

            closedFlags[current] = 1;

            int cx = GetGridX(current);
            int cy = GetGridY(current);

            // 遍历 8 个邻居（直接在循环中处理，避免每次分配）
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                    int neighbor = ToIndex(nx, ny);
                    if (closedFlags[neighbor] != 0) continue;
                    if (grid[neighbor].IsWalkable == 0) continue; // 不可通行

                    int tentativeG = (grid[current].gCost == int.MaxValue ? int.MaxValue : grid[current].gCost + FindDistanceBetweenTwoNodes(current, neighbor));
                    if (tentativeG < grid[neighbor].gCost)
                    {
                        // 更新 neighbor 的父与代价
                        Node nb = grid[neighbor];
                        nb.parentIndex = current;
                        nb.gCost = tentativeG;
                        nb.hCost = FindDistanceBetweenTwoNodes(neighbor, endIndex);
                        nb.fCost = nb.gCost + nb.hCost;
                        grid[neighbor] = nb;

                        if (!IsInOpen(neighbor))
                        {
                            HeapPush(neighbor, ref heapCount);
                        }
                        else
                        {
                            // decrease-key：heapIndex[neighbor] 已有位置，HeapifyUp
                            HeapifyUp(heapIndex[neighbor]);
                        }
                    }
                }
            }
        }

        // 若未找到终点，回溯最近节点（这里选择不回溯最近的最佳点，仅返回空结果）
        // 也可选择追踪最近到终点的节点（需要在 loop 中记录 closest）
    }
}


