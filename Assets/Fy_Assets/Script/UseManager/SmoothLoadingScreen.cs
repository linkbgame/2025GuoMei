using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 假设使用Unity原生UI

public class SmoothLoadingScreen : MonoBehaviour
{
    public Slider progressSlider; // 进度条Slider
    public TMP_Text progressText; // 进度百分比文本
    public Image hintText; // 提示文本，如“正在加载场景资源”
    public float simulationSpeed = 0.5f; // 模拟进度追赶真实进度的速度

    [Header("Fade In New Scene")]
    [Tooltip("用于淡入的遮罩(建议是全屏黑色 Image)，要求物体上带 CanvasGroup。")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("场景激活后，淡入时长(秒)。")]
    public float fadeInDuration = 0.6f;

    [Tooltip("激活新场景后，是否自动把加载界面隐藏。")]
    public bool hideAfterFadeIn = true;

    private AsyncOperation _loadingOperation;
    private float _currentSliderValue = 0f;
    private float _targetProgress = 0f;

    public static SmoothLoadingScreen instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        gameObject.SetActive(false); // 初始时隐藏加载界面
    }

    private void OnEnable()
    {
        // 确保重新显示时是“黑屏”(alpha=1)，用于后续淡入
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = true;
        }
    }

    public void StartLoadingScene(string sceneName)
    {
        if (sceneName == "PersistentScenes")
        {
            progressText.gameObject.SetActive(true);
            progressSlider.gameObject.SetActive(true);
            hintText.gameObject.SetActive(false);
            gameObject.SetActive(true); // 显示加载界面
            gameObject.GetComponent<MainMenuManager>().StartGame();
        }
        else
        {
            SceneManager.LoadScene(sceneName);//只要不是去PersistentScenes场景，就不显示加载界面
            //StartCoroutine(LoadSceneWithSmoothProgress(sceneName));
        }

    }

    IEnumerator LoadSceneWithSmoothProgress(string sceneName)
    {
        // 重置UI
        _currentSliderValue = 0f;
        progressSlider.value = 0f;
        progressText.text = "0%";
        hintText.gameObject.SetActive(false);
        //hintText.text = "正在初始化...";

        // 确保开始时遮罩为黑色
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = true;
        }

        // 开始真正的异步加载
        _loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        _loadingOperation.allowSceneActivation = false;

        // 第一阶段：追赶真实加载进度 (0% -> 90%)
        while (_loadingOperation.progress < 0.9f)
        {
            // 真实进度是目标
            _targetProgress = _loadingOperation.progress;
            // 让显示的进度平滑地趋向于真实进度
            _currentSliderValue = Mathf.MoveTowards(_currentSliderValue, _targetProgress, simulationSpeed * Time.deltaTime);

            UpdateProgressUI(_currentSliderValue);
            hintText.gameObject.SetActive(false);
            //hintText.text = "正在加载场景资源...";
            yield return null;
        }

        // 此时真实进度已达0.9，资源加载完毕
        hintText.gameObject.SetActive(false);
        //hintText.text = "资源加载完毕，准备激活场景...";
        _targetProgress = 0.9f;

        // 第二阶段：模拟最后10%的加载（例如，播放一段简短动画，或等待网络数据）
        // 我们让进度条从90%平滑走到100%
        while (_currentSliderValue < 1.0f)
        {
            // 缓慢增加进度，模拟“最后处理”
            _currentSliderValue = Mathf.MoveTowards(_currentSliderValue, 1.0f, simulationSpeed * Time.deltaTime * 0.3f); // 最后阶段慢一点
            UpdateProgressUI(_currentSliderValue);

            // 这里可以加入一些动态的提示语，让等待不枯燥
            if (_currentSliderValue > 0.95f)
            {
                hintText.gameObject.SetActive(false);
                //hintText.text = "即将完成...";
            }
            yield return null;
        }

        // 进度条显示100%，等待一小会儿让玩家看清，或等待某个条件（如任意键）
        progressText.text = "100%";
        yield return new WaitForSeconds(0.3f);
        //hintText.gameObject.SetActive(true);
        //progressText.gameObject.SetActive(false);
        //progressSlider.gameObject.SetActive(false);
        //hintText.text = "按任意键继续...";
        //yield return new WaitUntil(() => Input.anyKeyDown);

        // 最终，激活场景
        _loadingOperation.allowSceneActivation = true;

        // 等待新场景加载完成
        while (!_loadingOperation.isDone)
        {
            yield return null;
        }

        // 场景已激活：执行淡入(黑 -> 透明)
        yield return FadeInNewScene();

        if (hideAfterFadeIn)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeInNewScene()
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.0001f, fadeInDuration);
        float t = 0f;

        // alpha 1 -> 0
        fadeCanvasGroup.alpha = 1f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }

    void UpdateProgressUI(float progress)
    {
        progressSlider.value = progress;
        // 显示为整数百分比，更美观
        progressText.text = $"{(progress * 100):F0}";
    }
}
