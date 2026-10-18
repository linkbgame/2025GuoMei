using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics; // 使用 float3

// Job/Burst 兼容的 Node（blittable）
public struct Node
{
    public int ButtomX;
    public int ButtomY;
    // 使用 int 代替 bool 保持 blittable（0 = false, 1 = true）
    public int IsWalkable;
    // 使用 float3 代替 Vector3
    public float3 Center;

    public int gCost;
    public int hCost;
    public int fCost;
    // 父节点的一维索引，-1 表示无父节点
    public int parentIndex;

    // 简单构造函数（在主线程构建 NativeArray 时使用）
    public Node(int x, int y, float3 center, int isWalkable, int parent)
    {
        ButtomX = x;
        ButtomY = y;
        Center = center;
        IsWalkable = isWalkable;
        gCost = int.MaxValue; // 默认未访问，设为大值
        hCost = 0;
        fCost = int.MaxValue;
        parentIndex = parent;
    }
}


