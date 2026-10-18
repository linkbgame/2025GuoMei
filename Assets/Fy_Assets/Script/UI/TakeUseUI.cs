using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TakeUseUI : MonoBehaviour
{
    public List<GameObject> usedUI;
    public GameObject ShowStartUI;
    private GameObject NowShowUI;
    private PlayerMove player;
    private Button readDang;
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerMove>();
        readDang = GameObject.FindGameObjectWithTag("anNiu").GetComponent<Button>();
    }
    public void OpenOneMusic()
    {
        player.canMove = false;
        for (int i = 0; i < usedUI.Count; i++)
        {
            usedUI[i].SetActive(false);
        }
        usedUI[0].SetActive(true);
    }
    public void OpenTwoOher()
    {
        player.canMove = false;
        for (int i = 0; i < usedUI.Count; i++)
        {
            usedUI[i].SetActive(false);
        }
        usedUI[1].SetActive(true);
    }
    public void CloseUI()
    {
        player.canMove = true;
        for (int i = 0; i < usedUI.Count; i++)
        {
            usedUI[i].SetActive(false);
        }
    }
    public void ShowStart()
    {
        player.canMove = false;
        NowShowUI = Instantiate(ShowStartUI);
        _fadeCanvasGroup = NowShowUI.GetComponentInChildren<CanvasGroup>();
        StartTakeGame takeGame = NowShowUI.GetComponentInChildren<StartTakeGame>();

        takeGame.END.AddListener(CloseShowStart);
    }
    public void CloseShowStart()
    {
        StartCoroutine(FadeOutMenu());
    }
    /// <summary>
    /// 重新开始用
    /// </summary>
    public void BackStart()
    {
        SmoothLoadingScreen.instance.StartLoadingScene("Start");
    }
    public void OverGame()
    {
        Application.Quit();
    }
    public void OpenCunDang()
    {
        readDang.onClick.Invoke();
    }
    [Header("Main Menu Fade Out")]
    [Tooltip("用于淡出的遮罩(建议是全屏黑色 Image)，要求物体上带 CanvasGroup。")]
    private CanvasGroup _fadeCanvasGroup;

    [Tooltip("淡出时长(秒)。")]
    [SerializeField] private float _fadeOutDuration = 0.6f;
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
        player.canMove = true;
        Destroy(NowShowUI); // 淡出完成后销毁本物体，避免残留在场景中
    }
}
