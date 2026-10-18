using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 挂在玩家上的脚本：维护进入触发范围的 NpcAttractPoint 集合，
/// 按下空格时挑选最近的目标并调用其 TriggerAttract()。
/// 需要玩家身上有一个 IsTrigger 的 2D Collider，用于触发进出事件。
/// </summary>
[DisallowMultipleComponent]
public class PlayerNpcAttractInteractor : MonoBehaviour
{
    [Header("输入")]
    [Tooltip("触发号召的按键。")]
    public KeyCode attractKey = KeyCode.Space;

    [Header("行为开关")]
    [Tooltip("仅当存在可号召目标时，按键才生效（避免误触）。")]
    public bool requireNearbyPoint = true;

    [Header("UI 提示（可选，在 Inspector 里绑定）")]
    [Tooltip("玩家进入第一个可号召范围时触发（用于显示「按空格」提示）。")]
    public UnityEvent onEnterAttractRange;

    [Tooltip("玩家离开所有可号召范围时触发（用于隐藏提示）。")]
    public UnityEvent onExitAttractRange;

    [Tooltip("成功按下号召键并调用 TriggerAttract 后立即触发。玩家仍在触发体内时不会收到 OnTriggerExit，需用本事件立刻关掉提示 UI。")]
    public UnityEvent onAttractTriggered;

    private readonly HashSet<NpcAttractPoint> m_nearbyPoints = new HashSet<NpcAttractPoint>();

    private void Update()
    {
        if (!Input.GetKeyDown(attractKey))
        {
            return;
        }

        CleanupInvalidPoints();

        if (m_nearbyPoints.Count == 0)
        {
            return;
        }

        NpcAttractPoint target = GetNearestPoint();
        if (target != null)
        {
            target.TriggerAttract();
            onAttractTriggered?.Invoke();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NpcAttractPoint point = other.GetComponent<NpcAttractPoint>();
        if (point == null)
        {
            point = other.GetComponentInParent<NpcAttractPoint>();
        }

        if (point != null)
        {
            bool wasEmpty = m_nearbyPoints.Count == 0;
            m_nearbyPoints.Add(point);
            if (wasEmpty && m_nearbyPoints.Count > 0)
            {
                onEnterAttractRange?.Invoke();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        NpcAttractPoint point = other.GetComponent<NpcAttractPoint>();
        if (point == null)
        {
            point = other.GetComponentInParent<NpcAttractPoint>();
        }

        if (point != null)
        {
            m_nearbyPoints.Remove(point);
            if (m_nearbyPoints.Count == 0)
            {
                onExitAttractRange?.Invoke();
            }
        }
    }

    private void CleanupInvalidPoints()
    {
        m_nearbyPoints.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);
    }

    private NpcAttractPoint GetNearestPoint()
    {
        NpcAttractPoint nearest = null;
        float nearestSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (NpcAttractPoint p in m_nearbyPoints)
        {
            if (p == null)
            {
                continue;
            }

            float sqr = (p.transform.position - myPos).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = p;
            }
        }

        return nearest;
    }
}
