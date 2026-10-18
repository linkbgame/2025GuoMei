using System.Collections; // 引入协程/迭代器等集合相关类型（IEnumerator 等）。
using System.Collections.Generic; // 引入泛型集合类型（List<T> 等）。
using TMPro;
using UnityEngine; // 引入 Unity 引擎核心 API（MonoBehaviour、GameObject 等）。
using UnityEngine.SceneManagement; // 引入场景管理 API（SceneManager、AsyncOperation 等）。
using UnityEngine.UI; // 引入 UI 组件 API（Image 等）。

public class MainMenuManager : MonoBehaviour // 主菜单管理器：负责点击“开始游戏”后的 UI 隐藏与场景异步加载。
{
    [SerializeField, Header("加载进度条的父对象")] private GameObject _loadingBarObject; // 进度条的父物体（通常是一个面板/容器），用于整体显示/隐藏。
    [SerializeField, Header("加载进度条的图像")] private Image _loadingBar; // 进度条填充图片（通常通过 fillAmount 体现进度）。
    [SerializeField, Header("需要隐藏的对象")] private GameObject[] _objectsToHide; // 点击开始后需要隐藏的主菜单 UI/物体数组。

    [Space] // Inspector 中插入间距，提升字段分组的可读性。
    [SerializeField, Header("主要持久化场景")] private SceneField _persistentGameplay; // 持久化场景（例如：全局管理器/音频/玩家等），通常作为基础场景。
    [SerializeField, Header("游戏关卡场景")] private SceneField[] _levelScene; // 具体关卡场景（通过 Additive 叠加到持久化场景之上）。

    [Header("伪进度参数")]
    [SerializeField, Tooltip("显示进度向真实进度靠拢的速度（越大越快）")]
    private float _displayProgressSpeed = 4.0f;

    [SerializeField, Tooltip("显示进度达到1且真实进度达到1后，额外延迟（秒）")]
    private float _finishDelay = 0.3f;

    [Header("Main Menu Fade Out")]
    [Tooltip("用于淡出的遮罩(建议是全屏黑色 Image)，要求物体上带 CanvasGroup。")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;

    [Tooltip("淡出时长(秒)。")]
    [SerializeField] private float _fadeOutDuration = 0.6f;

    private readonly List<AsyncOperation> _scenesToLoad = new List<AsyncOperation>(); // 保存本次需要加载的异步操作列表，用于汇总加载进度与判断完成。

    public Slider progressSlider; // 进度条Slider
    public TMP_Text ShowText; // 进度百分比文本
    private void Awake() // Unity 生命周期：脚本实例加载时调用（早于 Start），适合做引用初始化。
    {
        //_loadingBarObject.SetActive(false); // 初始时隐藏/禁用加载进度条界面，避免进入菜单就显示。
    }

    //开始游戏
    public void StartGame() // 对外公开方法：通常绑定到按钮 OnClick，用于开始加载游戏场景。
    {
        _scenesToLoad.Clear();

        //HideMenu(); // 隐藏主菜单相关 UI，避免加载时仍可交互或遮挡。

        //_loadingBarObject.SetActive(true); // 显示加载进度条界面，提示玩家正在进入游戏。

        // 加载持久化场景和游戏关卡场景，并将它们添加到待加载场景列表中
        _scenesToLoad.Add(SceneManager.LoadSceneAsync(_persistentGameplay)); // 异步加载持久化场景（依赖 SceneField 的隐式 string 转换得到场景名/路径）。
        for (int i = 0; i < _levelScene.Length; i++) // 遍历所有指定的关卡场景。
            _scenesToLoad.Add(SceneManager.LoadSceneAsync(_levelScene[i], LoadSceneMode.Additive)); // 以叠加方式异步加载关卡场景，使其与持久化场景同时存在。

        StartCoroutine(ProgressLoadingBar()); // 启动协程：在每一帧更新进度条显示，直到所有场景加载完成。
    }

    // 隐藏界面
    private void HideMenu() // 内部方法：集中处理需要隐藏的 UI/物体。
    {
        for (int i = 0; i < _objectsToHide.Length; i++) // 遍历所有需要隐藏的对象。
        {
            _objectsToHide[i].SetActive(false); // 将对象设为不激活（隐藏并禁止其脚本/碰撞等）。
        }

        if (_loadingBarObject != null)
            _loadingBarObject.SetActive(false);
    }

    private void SetDisplayedProgress(float normalized01)
    {
        normalized01 = Mathf.Clamp01(normalized01);

        if (progressSlider != null){
            progressSlider.value = normalized01;
            ShowText.text = Mathf.RoundToInt(normalized01 * 100f)+"";
        }

        if (_loadingBar != null)
            _loadingBar.fillAmount = normalized01;
    }

    private float GetTotalRealProgress01()
    {
        if (_scenesToLoad.Count == 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < _scenesToLoad.Count; i++)
        {
            // Unity 的 AsyncOperation.progress 通常到 0.9 代表“加载完成(但未激活)”阶段。
            // 这里将 0~0.9 映射到 0~1，确保“真实进度”最终能到 100%。
            float p = Mathf.Clamp01(_scenesToLoad[i].progress / 0.9f);
            sum += p;
        }
        return sum / _scenesToLoad.Count;
    }

    private IEnumerator FadeOutMenu()
    {
        if (_fadeCanvasGroup == null)
            yield break;

        float duration = Mathf.Max(0.0001f, _fadeOutDuration);
        float t = 0f;

        // alpha 1 -> 0（与 SmoothLoadingScreen 的 FadeInNewScene 风格一致）
        _fadeCanvasGroup.alpha = 1f;
        _fadeCanvasGroup.blocksRaycasts = true;
        _fadeCanvasGroup.interactable = true;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            _fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;
        _fadeCanvasGroup.interactable = false;
    }

    // 异步加载进度条的协程（伪进度：显示值快速线性向真实值靠拢）
    private IEnumerator ProgressLoadingBar()
    {
        float displayed = 0f;
        SetDisplayedProgress(displayed);

        while (true)
        {
            float real = GetTotalRealProgress01();

            // 伪进度：当前显示进度向真实进度做线性移动（MoveTowards 是线性的，不是平滑曲线）。
            displayed = Mathf.MoveTowards(displayed, real, _displayProgressSpeed * Time.deltaTime);
            SetDisplayedProgress(displayed);

            // 只有“真实=100%”且“显示=100%”才退出。
            if (real >= 1f && displayed >= 1f)
                break;

            yield return null;
        }

        if (_finishDelay > 0f)
            yield return new WaitForSeconds(_finishDelay);

        // 淡出后再隐藏菜单对象
        yield return FadeOutMenu();
        HideMenu();
    }
}

