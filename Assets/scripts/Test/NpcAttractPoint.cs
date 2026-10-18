using System.Collections.Generic;
using MFarm.Map;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 可号召物体：玩家在其触发范围内按下空格时，由 PlayerNpcAttractInteractor
/// 调用 TriggerAttract()，让附近的 NPC 朝该物体移动。
/// 该组件只负责"被指挥时如何聚集 NPC"，不直接监听玩家输入。
/// </summary>
[DisallowMultipleComponent]
public class NpcAttractPoint : MonoBehaviour
{
    [Header("影响范围")]
    [Tooltip("吸引范围（世界单位）。范围内的 NPC 会被驱动。")]
    [Min(0f)] public float attractRadius = 6f;

    [Tooltip("可选：手动指定目标格。为 (maxGridSize, maxGridSize) 表示使用物体当前格。")]
    public Vector2Int targetGridPositionOverride = new Vector2Int(Settings.maxGridSize, Settings.maxGridSize);

    [Header("数量与冷却")]
    [Tooltip("一次最多驱动多少个 NPC，<=0 表示不限。")]
    public int maxNpcCount = 0;

    [Tooltip("两次触发之间的最小冷却时间（秒）。")]
    [Min(0f)] public float cooldown = 0.5f;

    [Header("筛选条件")]
    [Tooltip("仅驱动 interactable=true 的 NPC。关闭则不限制此字段。")]
    public bool requireInteractable = false;

    [Tooltip("是否要求 NPC 的 currentScene 与活动场景名一致。\n单场景项目建议关闭（默认）；多场景项目可打开以避免误驱动跨场景 NPC。")]
    public bool requireSameScene = false;

    [Header("目标分布")]
    [Tooltip("围绕中心格的半径（格），用于多 NPC 分散落点。1 表示 8 邻域，2 表示更外一圈。")]
    [Min(0)] public int distributionRingRadius = 1;

    [Header("调试")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

    private float m_lastTriggerTime = -999f;

    /// <summary>
    /// 触发吸引：收集附近 NPC 并下发寻路目标。
    /// </summary>
    public void TriggerAttract()
    {
        if (Time.time - m_lastTriggerTime < cooldown)
        {
            return;
        }

        Grid grid = FindObjectOfType<Grid>();
        if (grid == null)
        {
            Debug.LogWarning($"[NpcAttractPoint] 未找到 Grid，无法解析目标格：{name}");
            return;
        }

        string activeScene = SceneManager.GetActiveScene().name;
        Vector2Int centerGrid = ResolveCenterGrid(grid);

        if (!IsGridReachable(activeScene, centerGrid))
        {
            Debug.LogWarning($"[NpcAttractPoint] 中心目标格 {centerGrid} 不可达：{name}");
            return;
        }

        List<NPCMovement> candidates = CollectCandidates(activeScene);
        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((a, b) =>
        {
            float da = (a.transform.position - transform.position).sqrMagnitude;
            float db = (b.transform.position - transform.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        int count = candidates.Count;
        if (maxNpcCount > 0 && count > maxNpcCount)
        {
            count = maxNpcCount;
        }

        HashSet<Vector2Int> usedGrids = new HashSet<Vector2Int>();
        usedGrids.Add(centerGrid);

        for (int i = 0; i < count; i++)
        {
            NPCMovement npc = candidates[i];

            Vector2Int targetGrid = (i == 0)
                ? centerGrid
                : PickDistributionGrid(activeScene, centerGrid, usedGrids);

            usedGrids.Add(targetGrid);

            // 关键：把 schedule.targetScene 对齐到 NPC 自己的 currentScene，
            // 这样 NPCMovement.BuildPath 永远走"同场景分支"（A* 寻路），
            // 不会去查空的 sceneRouteDict 而抛 KeyNotFoundException。
            string npcScene = string.IsNullOrEmpty(npc.currentScene) ? activeScene : npc.currentScene;

            ScheduleDetails schedule = new ScheduleDetails(
                hour: 0,
                minute: 0,
                day: 0,
                priority: 0,
                targetScene: npcScene,
                targetGridPosition: targetGrid,
                clipAtStop: null,
                interactable: npc.interactable);

            npc.BuildPath(schedule);
        }

        m_lastTriggerTime = Time.time;
    }

    /// <summary>
    /// 解析中心目标格：优先使用手动指定，否则取物体当前所在格。
    /// </summary>
    private Vector2Int ResolveCenterGrid(Grid grid)
    {
        if (targetGridPositionOverride.x < Settings.maxGridSize &&
            targetGridPositionOverride.y < Settings.maxGridSize)
        {
            return targetGridPositionOverride;
        }

        Vector3Int cell = grid.WorldToCell(transform.position);
        return new Vector2Int(cell.x, cell.y);
    }

    private List<NPCMovement> CollectCandidates(string activeScene)
    {
        List<NPCMovement> result = new List<NPCMovement>();
        NPCMovement[] all = FindObjectsOfType<NPCMovement>();
        float sqrRadius = attractRadius * attractRadius;
        Vector3 myPos = transform.position;

        foreach (NPCMovement npc in all)
        {
            if (npc == null || !npc.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (requireSameScene
                && !string.IsNullOrEmpty(npc.currentScene)
                && npc.currentScene != activeScene)
            {
                continue;
            }

            if (requireInteractable && !npc.interactable)
            {
                continue;
            }

            float sqrDist = (npc.transform.position - myPos).sqrMagnitude;
            if (sqrDist > sqrRadius)
            {
                continue;
            }

            result.Add(npc);
        }

        return result;
    }

    /// <summary>
    /// 在中心格周围按环形挑选一个未占用且可达的格子，避免 NPC 全部扎堆同一格。
    /// 找不到时回退到中心格。
    /// </summary>
    private Vector2Int PickDistributionGrid(string sceneName, Vector2Int center, HashSet<Vector2Int> used)
    {
        for (int r = 1; r <= Mathf.Max(1, distributionRingRadius); r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                    {
                        continue;
                    }

                    Vector2Int candidate = new Vector2Int(center.x + dx, center.y + dy);
                    if (used.Contains(candidate))
                    {
                        continue;
                    }

                    if (!IsGridReachable(sceneName, candidate))
                    {
                        continue;
                    }

                    return candidate;
                }
            }
        }

        return center;
    }

    private bool IsGridReachable(string sceneName, Vector2Int gridPos)
    {
        if (GridMapManager.Instance == null)
        {
            return true;
        }

        TileDetails tile = GridMapManager.Instance.GetTileDetailsByPosition(gridPos.x, gridPos.y);
        if (tile != null && tile.isNPCObstacle)
        {
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, attractRadius);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
        Gizmos.DrawSphere(transform.position, 0.12f);
    }
}
