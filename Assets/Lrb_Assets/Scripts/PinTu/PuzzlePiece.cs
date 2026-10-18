using UnityEngine;
using DG.Tweening;
using System.Collections; // 引入 DOTween 命名空间

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PuzzlePiece : MonoBehaviour
{
    private Vector2 targetPosition;   // 目标正确位置（局部坐标）
    private float snapThreshold;       // 吸附阈值
    private bool isLocked = false;     // 是否已锁定（拼对后不可再动）
    public bool IsLocked => isLocked; 

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;
    private Vector3 offset;            // 鼠标点击位置与物体中心点的偏移
    private Vector3 initialScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSortingOrder = spriteRenderer.sortingOrder;
        initialScale = transform.localScale;
    }

    private void OnDestroy()
    {
        //物体销毁时停止所有动画
        transform.DOKill();
    }

    //由 PuzzleManager 初始化时调用，设置目标位置和阈值
    public void SetTarget(Vector2 targetPos, float threshold)
    {
        targetPosition = targetPos;
        snapThreshold = threshold;
        isLocked = false;
        GetComponent<Collider2D>().enabled = true;
    }

    /// <summary>
    /// 一键通关时对单个拼图碎片的处理
    /// </summary>
    public void InstantSnap()
    {
        if (isLocked) return;

        // 1. 停止所有正在进行的 DOTween 动画（防止拖拽或缩放动画冲突）
        transform.DOKill();

        // 2. 需求二：瞬移到目标位置
        transform.localPosition = targetPosition;

        // 3. 状态同步：锁定并禁用碰撞
        isLocked = true;
        GetComponent<Collider2D>().enabled = false;

        // 4. 层级同步：设置为锁定层级
        spriteRenderer.sortingOrder = PuzzleManager.Instance.lockedOrder;

        // 重置缩放确保视觉统一
        transform.localScale = initialScale;
    }

    // 鼠标点击时：准备拖拽
    void OnMouseDown()
    {
        if (isLocked) return;

        // 优先播放拿起音效
        PuzzleManager.Instance.PlaySFX(PuzzleManager.Instance.audioSettings.pickUpClip);

        // 停止之前的动画防止冲突
        transform.DOKill();

        offset = transform.position - GetMouseWorldPosition();
        spriteRenderer.sortingOrder = PuzzleManager.Instance.draggingOrder;

        // --- UX 增强：拿起反馈 ---
        // 放大 1.1 倍
        transform.DOScale(initialScale * 1.1f, 0.2f).SetEase(Ease.OutBack);

        
    }

    // 鼠标移动时：跟随鼠标
    void OnMouseDrag()
    {
        if (isLocked) return;

        Vector3 newPos = GetMouseWorldPosition() + offset;
        transform.position = newPos;
    }

    // 鼠标松开时：执行吸附判定
    void OnMouseUp()
    {
        if (isLocked) return;

        // 还原层级
        spriteRenderer.sortingOrder = PuzzleManager.Instance.baseOrder;

        // 计算当前局部位置与目标局部位置的距离
        float distance = Vector2.Distance(transform.localPosition, targetPosition);

        if (distance <= snapThreshold)
        {
            SnapToTarget();
        }
        else
        {
            // --- UX 增强：未拼对归位 ---
            // 缩回到原始大小
            transform.DOScale(initialScale, 0.2f);
        }
    }

    private void SnapToTarget()
    {
        isLocked = true;
        transform.DOKill(); // 停止所有进行中的动画

        // 播放吸附音效
        PuzzleManager.Instance.PlaySFX(PuzzleManager.Instance.audioSettings.snapClip);

        // 锁定
        transform.localPosition = targetPosition;
        GetComponent<Collider2D>().enabled = false;
        spriteRenderer.sortingOrder = PuzzleManager.Instance.lockedOrder;

        // --- UX 增强：吸附 Q 弹特效 ---
        // 先确保比例回到初始，然后进行一个 Punch 缩放
        transform.localScale = initialScale;
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 10, 1f);

        // 通知管理器：该碎片已完成
        PuzzleManager.Instance.OnPieceLocked(); 
    }

    // 核心工具函数：将屏幕坐标转为世界坐标
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        // 强制设置 z 轴为摄像机到平面的距离（针对 2D 正交相机）
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    // --- UX 增强：胜利欢呼动画 ---
    public void PlayVictoryCheer()
    {
        // 再次强制清理，确保最后一块拼图不会因为吸附动画还没播完而产生缩放残留
        transform.DOKill(false);
        transform.localScale = initialScale;

        // 延迟随机时间让动画更有层次感
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 1.5f, 15, 0.5f)
                 .SetDelay(Random.Range(0f, 0.3f));
    }
}