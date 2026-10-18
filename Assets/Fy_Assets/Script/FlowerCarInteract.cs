using UnityEngine;

/// <summary>
/// 花车：玩家在 ShowRoadUI 范围内按 E → 停 CarMove、禁用 RunFlowerCar、开对话；
/// 对话 <see cref="Fy_DialogManager.END"/>（在 Inspector 中应先绑 <see cref="Fy_DialogManager.OverText"/>）之后播枯萎 Animator。
/// 同物体需挂禁用的 <see cref="NPCTakeSave"/> 供 OverText→EndTalk（计划约定）。
/// </summary>
public sealed class FlowerCarInteract : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CarMove carMove;
    [SerializeField] private RunFlowerCar runFlowerCar;
    [SerializeField] private Animator witherAnimator;
    [SerializeField] private ShowRoadUI showRoadUI;
    [SerializeField] private Fy_DialogManager dialogManager;
    [SerializeField] private Collider2D interactCollider;

    [Header("Dialogue")]
    [Tooltip("Resources/Read 下 CSV 文件名（无扩展名），与 SimpleFactory.FindText 一致。")]
    [SerializeField] private string nowTalkingName;

    [Header("Animator — 枯萎")]
    [Tooltip("非空则 SetTrigger；否则用 Play 播指定状态名。")]
    [SerializeField] private string witherTriggerParameter;
    [SerializeField] private string witherStateName;
    [SerializeField] private int witherAnimatorLayer;

    [Header("Optional — Event.csv 花车枯萎")]
    [SerializeField] private bool reportFlowerEndEvent;
    [SerializeField] private int flowerEndEventId = 9;

    bool _pendingWitherAfterThisDialog;
    bool _interactionConsumed;

    void Awake()
    {
        //if (dialogManager != null)
            //dialogManager.END.AddListener(OnDialogEndAfterOverTextChain);
        if (interactCollider == null)
            interactCollider = GetComponent<Collider2D>();
    }

    void OnDestroy()
    {
        //if (dialogManager != null)
            //dialogManager.END.RemoveListener(OnDialogEndAfterOverTextChain);
    }

    void Update()
    {
        if (_interactionConsumed) return;
        if (showRoadUI == null || dialogManager == null) return;
        if (!showRoadUI.CanTake) return;
        if (dialogManager.IsTalking) return;
        if (string.IsNullOrWhiteSpace(nowTalkingName)) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryStartFlowerDialog();
    }

    void TryStartFlowerDialog()
    {
        TextAsset text = SimpleFactory.FindText(nowTalkingName);
        if (text == null)
        {
            Debug.LogError($"[FlowerCarInteract] 找不到对话资源 Read/{nowTalkingName}。请在 Resources/Read 配置 CSV。", this);
            return;
        }

        if (carMove != null)
            carMove.Stop();

        if (runFlowerCar != null)
        {
            runFlowerCar.Pause();
            runFlowerCar.enabled = false;
        }

        LockPlayerLikeNpc(showRoadUI);

        _pendingWitherAfterThisDialog = true;
        dialogManager.TakeMyText(text);
    }

    static void LockPlayerLikeNpc(ShowRoadUI road)
    {
        if (road.player == null) return;
        road.player.canMove = false;
        if (road.playerGO != null)
            road.player.TakeIdle(road.transform.position.y - road.playerGO.transform.position.y > 0f);

        var psm = road.player.GetComponent<PlayerStateManager>();
        if (psm != null)
            psm.SetMovementLock(true, road);
    }

    public void OnDialogEndAfterOverTextChain()
    {
        if (!_pendingWitherAfterThisDialog) return;
        _pendingWitherAfterThisDialog = false;

        PlayWitherAnimation();
        FinalizeInteractionArea();
    }

    void PlayWitherAnimation()
    {
        if (witherAnimator == null)
        {
            Debug.LogWarning("[FlowerCarInteract] 未指定 witherAnimator，跳过枯萎动画。", this);
            return;
        }

        if (!string.IsNullOrEmpty(witherTriggerParameter)){
            Debug.LogError(11);
            witherAnimator.SetTrigger(witherTriggerParameter);
        }
        else if (!string.IsNullOrEmpty(witherStateName)){
            witherAnimator.CrossFade("花车枯萎", 0f);
        }
        else
            Debug.LogWarning("[FlowerCarInteract] 未配置 witherTriggerParameter 或 witherStateName。", this);
    }

    void FinalizeInteractionArea()
    {
        _interactionConsumed = true;
        if (showRoadUI != null && showRoadUI.it != null)
            showRoadUI.it.SetActive(false);
        if (interactCollider != null)
            interactCollider.enabled = false;
    }

    /// <summary>
    /// 供枯萎 Animator 动画事件调用：上报「花车枯萎」任务（Event.csv 序号默认 9）。
    /// </summary>
    public void NotifyWitherAnimationFinished()
    {
        if (!reportFlowerEndEvent) return;
        if (SystemEventManager.instance == null) return;
        SystemEventManager.instance.SomeOneHasDown(9);
    }
}
