using UnityEngine;
using TMPro;
using DG.Tweening; // 必须引入 DOTween
using UnityEngine.UI;

/// <summary>
/// 负责管理线索收集时的所有视觉反馈（幻影飞行、目标震动、+1特效）
/// </summary>
public class ClueFeedbackManager : MonoBehaviour
{
    public static ClueFeedbackManager Instance;

    [Header("场景引用")]
    [SerializeField] private RectTransform targetButtonUI; // 左上角的线索本图标
    [SerializeField] private Canvas mainCanvas;            // 主 Canvas，用于坐标转换

    [Header("预制体资源")]
    [SerializeField] private GameObject phantomPrefab;     // 线索幻影预制体
    [SerializeField] private GameObject plusOnePrefab;     // +1 提示预制体

    [Header("动画参数")]
    [SerializeField] private float spawnDuration = 0.3f;   // 幻影生成时间
    [SerializeField] private float flyDuration = 0.8f;     // 飞行时间
    [SerializeField] private float stayDuration = 0.2f;    // 生成后停留多久再飞
    [SerializeField] private Ease flyEase = Ease.InBack;   // 飞行缓动：起步慢(阻力感)，后加速

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip plusOneClip;

    private void Awake()
    {
        Instance = this;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 执行完整的线索收集特效流程
    /// </summary>
    /// <param name="content">线索文字内容</param>
    /// <param name="screenPosition">鼠标点击的屏幕坐标</param>
    public void PlayCollectionSequence(string content, Vector2 screenPosition, Color backImageColor)
    {
        // 播放点击音效 (阶段 2: 点击瞬间)
        PlayClip(clickClip);

        // 1. 在点击处生成幻影
        GameObject phantom = CreatePhantom(content, backImageColor ,screenPosition);
        RectTransform phantomRect = phantom.GetComponent<RectTransform>();

        // 创建动画序列
        Sequence seq = DOTween.Sequence();

        // --- 阶段 A: 生成反馈 (从纸面抓取) ---
        // 初始状态：缩放为0 (配合 Pivot 左上角，实现从左上到右下展开)
        phantomRect.localScale = Vector3.zero;

        // 动画：缩放至 1.0
        seq.Append(phantomRect.DOScale(1f, spawnDuration).SetEase(Ease.OutBack));

        // 停留一小会儿，让玩家看清
        seq.AppendInterval(stayDuration);

        // --- 阶段 B: 飞行轨迹 (线性 + 阻力感) ---
        // 将目标 UI 的世界坐标转为 Canvas 坐标 (如果 Canvas 模式不同，转换方式可能需微调)
        // 这里简化处理：直接移动世界坐标，因为 UI 都在 Canvas 下
        Vector3 targetWorldPos = targetButtonUI.position;

        // 移动动画
        seq.Append(phantomRect.DOMove(targetWorldPos, flyDuration).SetEase(flyEase));

        // 飞行同时缩小一点 (可选)
        //seq.Join(phantomRect.DOScale(0.5f, flyDuration).SetEase(Ease.InQuad));

        // --- 阶段 C: 命中反馈 & 销毁 ---
        seq.OnComplete(() =>
        {
            // 1. 销毁幻影
            Destroy(phantom);

            // 2. 目标受击反馈 (震动/弹跳)
            PlayTargetImpactEffect();

            // 3. +1 提示特效
            PlayPlusOneEffect();

            // 4. 播放确认音效 (阶段 4: 命中反馈)
            PlayClip(plusOneClip);
        });
    }

    private GameObject CreatePhantom(string content, Color backgroundColor, Vector2 screenPos)
    {
        GameObject obj = Instantiate(phantomPrefab, mainCanvas.transform);
        obj.name = "Clue_Phantom";

        // 设置文字
        var textComp = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = content;
        }
            
        // 设置背景图片颜色
        var backgroundImage = obj.GetComponent<CluePhantomView>().backImage;
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }

        // 设置位置 (将屏幕坐标转为 UI 世界坐标)
        // 注意：如果是 Overlay Canvas，屏幕坐标直接对应位置；如果是 Camera 模式，需转换
        if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            obj.transform.position = screenPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                mainCanvas.GetComponent<RectTransform>(),
                screenPos,
                mainCanvas.worldCamera,
                out Vector3 worldPos);
            obj.transform.position = worldPos;
        }

        return obj;
    }

    private void PlayTargetImpactEffect()
    {
        // 停止之前的动画，防止连点鬼畜
        targetButtonUI.DOKill(true);
        // 瞬间复原
        targetButtonUI.localScale = Vector3.one;
        // 弹跳效果 (Punch Scale)
        targetButtonUI.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1);
    }

    private void PlayPlusOneEffect()
    {
        // 在目标上方生成 +1
        GameObject plusOne = Instantiate(plusOnePrefab, targetButtonUI.parent);
        RectTransform rect = plusOne.GetComponent<RectTransform>();

        // 定位到目标上方 (假设 Pivot 是 0.5, 0)
        rect.position = targetButtonUI.position + Vector3.up * 50f; // 向上偏移一些

        // 动画序列
        Sequence seq = DOTween.Sequence();

        // 1. 瞬间出现 (Scale 0 -> 1.5) 膨胀
        rect.localScale = Vector3.zero;
        seq.Append(rect.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack));

        // 2. 回缩 (Scale 1.5 -> 1.0)
        seq.Append(rect.DOScale(1.0f, 0.1f));

        // 3. 逐渐透明并上浮
        seq.Append(rect.DOAnchorPosY(rect.anchoredPosition.y + 30f, 0.5f)); // 上浮
        seq.Join(rect.GetComponent<CanvasGroup>().DOFade(0f, 0.5f)); // 需加 CanvasGroup 组件

        // 销毁
        seq.OnComplete(() => Destroy(plusOne));
    }
}