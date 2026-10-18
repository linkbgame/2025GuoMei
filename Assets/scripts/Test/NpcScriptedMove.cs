using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 让 NPC 在激活后沿一组固定坐标做"直线脚本化移动"。
///
/// 设计目标：
/// 1. 与 <see cref="NPCMovement"/> 共用同一个 Rigidbody2D，使用 <c>rb.MovePosition</c> 推进，
///    避免与 NPCMovement 的物理移动方式打架。
/// 2. 一旦 <see cref="NPCMovement.IsScheduledActive"/> 变为 true（例如 NpcAttractPoint
///    通过 BuildPath 把 NPC 拉走），本组件立即让位且不再恢复，控制权完全交还给 NPCMovement。
/// 3. 不动 NPCMovement 的 schedule / 寻路链路，因此聚集功能继续保留原行为。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class NpcScriptedMove : MonoBehaviour
{
    [System.Serializable]
    public class MovePoint
    {
        [Tooltip("目标坐标（世界坐标）。")]
        public Vector3 position;

        [Tooltip("从上一个点到该点所用时间(秒)。<=0 将瞬间到达该点。")]
        public float moveTime = 1f;
    }

    [Header("Path")]
    [Tooltip("依次前往的目标点列表。")]
    public List<MovePoint> points = new List<MovePoint>();

    [Tooltip("启用时是否自动开始移动。")]
    public bool playOnStart = true;

    [Tooltip("走完路径后是否循环。")]
    public bool loop = false;

    [Header("Debug")]
    [SerializeField] private int currentIndex = 0;
    [SerializeField] private bool isPlaying = false;
    [Tooltip("被 NPCMovement 的寻路任务（如 NpcAttractPoint）打断后会置为 true，且不会再自动恢复。")]
    [SerializeField] private bool wasInterrupted = false;

    private Rigidbody2D _rb;
    private NPCMovement _npcMovement;

    // 当前段的插值状态
    private Vector3 _segmentStart;
    private Vector3 _segmentTarget;
    private float _segmentMoveTime;
    private float _segmentElapsed;

    public bool IsPlaying => isPlaying;
    public bool WasInterrupted => wasInterrupted;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _npcMovement = GetComponent<NPCMovement>();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Activate();
        }
    }

    /// <summary>
    /// 从第一个路径点开始执行直线移动。被 attract 中断过也可手动调用此方法重新激活。
    /// </summary>
    public void Activate()
    {
        if (points == null || points.Count == 0)
        {
            isPlaying = false;
            return;
        }

        wasInterrupted = false;
        currentIndex = 0;
        BeginSegment();
        isPlaying = true;
    }

    /// <summary>
    /// 手动停止脚本化移动（不会触发 wasInterrupted）。
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }

    private void BeginSegment()
    {
        if (points == null || currentIndex < 0 || currentIndex >= points.Count)
        {
            return;
        }

        MovePoint p = points[currentIndex];
        _segmentStart = _rb != null ? (Vector3)_rb.position : transform.position;
        _segmentTarget = p.position;
        _segmentMoveTime = p.moveTime;
        _segmentElapsed = 0f;
    }

    private void FixedUpdate()
    {
        if (!isPlaying || wasInterrupted)
        {
            return;
        }

        // NPCMovement 一旦在执行 BuildPath 排出来的寻路任务，立即让位且永久停摆。
        if (_npcMovement != null && _npcMovement.IsScheduledActive)
        {
            wasInterrupted = true;
            isPlaying = false;
            return;
        }

        if (points == null || points.Count == 0)
        {
            isPlaying = false;
            return;
        }

        if (currentIndex < 0)
        {
            currentIndex = 0;
            BeginSegment();
        }

        if (currentIndex >= points.Count)
        {
            if (loop)
            {
                currentIndex = 0;
                BeginSegment();
            }
            else
            {
                isPlaying = false;
                return;
            }
        }

        // 瞬移到目标点
        if (_segmentMoveTime <= 0f)
        {
            _rb.MovePosition(_segmentTarget);
            AdvanceIndex();
            return;
        }

        _segmentElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(_segmentElapsed / _segmentMoveTime);
        Vector3 next = Vector3.Lerp(_segmentStart, _segmentTarget, t);
        _rb.MovePosition(next);

        if (t >= 1f)
        {
            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        currentIndex++;

        if (currentIndex >= points.Count)
        {
            if (loop)
            {
                currentIndex = 0;
                BeginSegment();
            }
            else
            {
                isPlaying = false;
            }
            return;
        }

        BeginSegment();
    }

    private void OnDisable()
    {
        // 物体被禁用 / 销毁时清理状态，避免下一次启用时残留旧索引。
        isPlaying = false;
    }
}
