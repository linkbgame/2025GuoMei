using Inventory.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 普通交互物的实现接口
/// </summary>
public interface IInteractable
{
    void TriggerAction();
}

/// <summary>
/// 谜题实现的接口
/// </summary>
public interface IPuzzleInteractable
{
    string PuzzleID { get; }
    bool IsSolved { get; }

    void SuccessfullySolve();
}

public class InteractiveSystem : MonoBehaviour
{
    public GameObject signSprite; // 交互标志
    public InventorySO inventoryData; // 背包数据
    public GameObject hintText;

    [Header("提示设置")]
    [SerializeField] private string puzzleHint = "使用背包物品进行交互";
    [SerializeField] private string bagFullHint = "背包已满";
    [SerializeField] private float fullBagHintCooldown = 0.75f;
    [SerializeField] private string puzzleSuccessHint = "交互成功";
    [SerializeField] private float puzzleSuccessFadeDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float puzzleSuccessTargetAlpha = 0f;

    [Header("Performance (Reserved, default off)")]
    [SerializeField] private bool reserveNonAllocBuffer = false;
    [SerializeField] private int nonAllocBufferSize = 32;

    private Transform m_playerTrans;
    private TMP_Text m_hintLabel;
    private CanvasGroup m_hintCanvasGroup;
    private float m_lastFullBagHintTime = -999f;
    private Coroutine m_hintFadeCoroutine;
    private bool m_isPlayingSuccessHint;

    private readonly HashSet<SceneItem> nearbySceneItems = new HashSet<SceneItem>();
    private readonly HashSet<Collider2D> nearbyInteractables = new HashSet<Collider2D>();
    private readonly HashSet<Collider2D> nearbyPuzzles = new HashSet<Collider2D>();

    private Collider2D[] m_nonAllocResults;

    private bool m_candidatesDirty = true;
    private bool m_canInteract;
    private bool m_canPickUp;
    private bool m_canSign;

    private void Start()
    {
        m_playerTrans = GetComponentInParent<Transform>();
        m_hintLabel = hintText.GetComponentInChildren<TMP_Text>();
        m_hintCanvasGroup = hintText.GetComponent<CanvasGroup>();
        if (m_hintCanvasGroup == null)
        {
            m_hintCanvasGroup = hintText.AddComponent<CanvasGroup>();
        }

        if (reserveNonAllocBuffer)
        {
            nonAllocBufferSize = Mathf.Max(8, nonAllocBufferSize);
            m_nonAllocResults = new Collider2D[nonAllocBufferSize];
        }

        SetHint(string.Empty, false);
        UpdateSignVisual();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (signSprite == null)
        {
            Debug.LogError("[InteractiveSystem] Missing signSprite reference.");
            valid = false;
        }

        if (inventoryData == null)
        {
            Debug.LogError("[InteractiveSystem] Missing inventoryData reference.");
            valid = false;
        }

        if (hintText == null)
        {
           // Debug.LogError("[InteractiveSystem] Missing hintText reference.");
            valid = false;
        }

        return valid;
    }

    private void Update()
    {
        if (CleanupInvalidCandidates())
        {
            m_candidatesDirty = true;
        }

        if (m_candidatesDirty)
        {
            RecalculateStateFromCandidates();
        }

        UpdateSignVisual();

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandlePrimaryInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool changed = false;

        // 组件判断
        SceneItem sceneItem = other.GetComponent<SceneItem>();
        if (sceneItem != null)
        {
            changed |= nearbySceneItems.Add(sceneItem);
        }

        // Tag判断
        if (other.CompareTag("Interactable") && other.GetComponent<IInteractable>() != null)
        {
            changed |= nearbyInteractables.Add(other);
        }

        if (other.CompareTag("Puzzle") && other.GetComponent<IPuzzleInteractable>() != null)
        {
            changed |= nearbyPuzzles.Add(other);
        }

        if (changed)
        {
            m_candidatesDirty = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        bool changed = false;

        SceneItem sceneItem = other.GetComponent<SceneItem>();
        if (sceneItem != null)
        {
            changed |= nearbySceneItems.Remove(sceneItem);
        }

        changed |= nearbyInteractables.Remove(other);
        changed |= nearbyPuzzles.Remove(other);

        if (changed)
        {
            m_candidatesDirty = true;
        }
    }

    /// <summary>
    /// 处理主要(Primary)交互
    /// </summary>
    private void HandlePrimaryInteract()
    {
        // 交互优先级：SceneItem > Interactable
        SceneItem nearestSceneItem = GetNearestSceneItem();
        if (nearestSceneItem != null)
        {
            TryPickup(nearestSceneItem);
            return;
        }

        IInteractable nearestInteractable = GetNearestInteractable();
        if (nearestInteractable != null)
        {
            nearestInteractable.TriggerAction();
        }
    }

    /// <summary>
    /// 拾取场景物品
    /// </summary>
    /// <param name="target"></param>
    private void TryPickup(SceneItem target)
    {
        if (target == null || target.InventoryItem == null)
        {
            return;
        }

        int reminder = inventoryData.AddItem(target.InventoryItem, target.Quantity);

        if (reminder == 0)
        {
            nearbySceneItems.Remove(target);
            target.DestroyItem();
            m_candidatesDirty = true;
            return;
        }

        target.Quantity = reminder;
        ShowBagFullHintThrottled();
    }

    // 物品交互行为调用
    public void TrySolvePuzzle(string keyName)
    {
        CleanupInvalidCandidates();

        IPuzzleInteractable nearestMatch = null;
        float nearestSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (Collider2D puzzleCollider in nearbyPuzzles)
        {
            if (puzzleCollider == null || !puzzleCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            IPuzzleInteractable puzzle = puzzleCollider.GetComponent<IPuzzleInteractable>();
            // 只要 PuzzleID 不匹配或已解开，就不考虑这个 Puzzle 了
            if (puzzle == null || puzzle.IsSolved || puzzle.PuzzleID != keyName)
            {
                continue;
            }

            float sqrDist = (puzzleCollider.transform.position - myPos).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearestMatch = puzzle;
            }
        }

        if (nearestMatch != null)
        {
            nearestMatch.SuccessfullySolve();
            PlayPuzzleSuccessHintFade();
            m_candidatesDirty = true;
        }
    }

    private void RecalculateStateFromCandidates()
    {
        CleanupInvalidCandidates();

        m_canPickUp = GetNearestSceneItem() != null;
        m_canInteract = GetNearestInteractable() != null;
        m_canSign = m_canPickUp || m_canInteract;

        UpdateHintByPriority();
        m_candidatesDirty = false;
    }

    private bool CleanupInvalidCandidates()
    {
        int removedCount = 0;
        removedCount += nearbySceneItems.RemoveWhere(item => item == null || !item.gameObject.activeInHierarchy);
        removedCount += nearbyInteractables.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy || col.GetComponent<IInteractable>() == null);
        removedCount += nearbyPuzzles.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy || col.GetComponent<IPuzzleInteractable>() == null);
        return removedCount > 0;
    }

    private SceneItem GetNearestSceneItem()
    {
        SceneItem nearest = null;
        float nearestSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (SceneItem item in nearbySceneItems)
        {
            if (item == null)
            {
                continue;
            }

            float sqrDist = (item.transform.position - myPos).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = item;
            }
        }

        return nearest;
    }

    private IInteractable GetNearestInteractable()
    {
        IInteractable nearest = null;
        float nearestSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (Collider2D interactableCollider in nearbyInteractables)
        {
            if (interactableCollider == null)
            {
                continue;
            }

            IInteractable candidate = interactableCollider.GetComponent<IInteractable>();
            if (candidate == null)
            {
                continue;
            }

            float sqrDist = (interactableCollider.transform.position - myPos).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool HasNearbyPuzzle()
    {
        foreach (Collider2D puzzleCollider in nearbyPuzzles)
        {
            if (puzzleCollider == null || !puzzleCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            IPuzzleInteractable puzzle = puzzleCollider.GetComponent<IPuzzleInteractable>();
            if (puzzle != null && !puzzle.IsSolved)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateHintByPriority()
    {
        // 成功提示淡出期间，不允许常规提示逻辑覆盖文本
        if (m_isPlayingSuccessHint)
        {
            return;
        }

        // 提示优先级：SceneItem > Puzzle > Interactable
        // 拾取时不强制显示提示；Puzzle 显示固定提示；其余隐藏。
        if (m_canPickUp)
        {
            SetHint(string.Empty, false);
            return;
        }

        if (HasNearbyPuzzle())
        {
            SetHint(puzzleHint, true);
            return;
        }

        SetHint(string.Empty, false);
    }

    private void ShowBagFullHintThrottled()
    {
        if (Time.time - m_lastFullBagHintTime < fullBagHintCooldown)
        {
            return;
        }

        m_lastFullBagHintTime = Time.time;
        SetHint(bagFullHint, true);
    }

    private void SetHint(string text, bool show)
    {
        if (hintText == null)
        {
            return;
        }

        if (m_hintFadeCoroutine != null)
        {
            StopCoroutine(m_hintFadeCoroutine);
            m_hintFadeCoroutine = null;
        }

        if (m_hintCanvasGroup != null)
        {
            m_hintCanvasGroup.alpha = 1f;
        }

        hintText.SetActive(show);

        if (m_hintLabel != null)
        {
            m_hintLabel.text = show ? text : string.Empty;
        }
    }

    private void PlayPuzzleSuccessHintFade()
    {
        if (hintText == null || m_hintLabel == null || m_hintCanvasGroup == null)
        {
            return;
        }

        if (m_hintFadeCoroutine != null)
        {
            StopCoroutine(m_hintFadeCoroutine);
        }

        hintText.SetActive(true);
        m_hintLabel.text = puzzleSuccessHint;
        m_hintCanvasGroup.alpha = 1f;
        m_isPlayingSuccessHint = true;

        m_hintFadeCoroutine = StartCoroutine(FadeHintAlphaRoutine(puzzleSuccessTargetAlpha, puzzleSuccessFadeDuration));
    }

    private IEnumerator FadeHintAlphaRoutine(float targetAlpha, float duration)
    {
        float startAlpha = m_hintCanvasGroup.alpha;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            m_hintCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        m_hintCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0.01f)
        {
            hintText.SetActive(false);
            m_hintLabel.text = string.Empty;
            m_hintCanvasGroup.alpha = 1f;
        }

        m_isPlayingSuccessHint = false;
        // 淡出结束后，按当前状态重新计算一次提示（例如附近仍有可拾取物）
        m_candidatesDirty = true;
        m_hintFadeCoroutine = null;
    }

    private void UpdateSignVisual()
    {
        if (signSprite == null)
        {
            return;
        }

        signSprite.SetActive(m_canSign);

        if (m_playerTrans != null)
        {
            signSprite.transform.localScale = m_playerTrans.localScale;
        }
    }
}
