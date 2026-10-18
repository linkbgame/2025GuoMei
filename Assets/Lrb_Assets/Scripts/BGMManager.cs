using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 数据配置层：定义每个 BGM 的属性
[System.Serializable]
public class BGMTrack
{
    [Header("场景与音乐配置")]
    [Tooltip("需要播放此音乐的场景名称")]
    public string sceneName;
    public AudioClip clip;
    [Range(0f, 1f)] public float targetVolume = 1f;

    [Header("淡入设置 (Fade In)")]
    public bool useFadeIn = true;
    public float fadeInDuration = 1.5f;

    [Header("淡出设置 (Fade Out)")]
    public bool useFadeOut = true;
    public float fadeOutDuration = 1.5f;
}

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("BGM 列表配置")]
    [Tooltip("在这里配置不同场景对应的音乐和过渡参数")]
    public List<BGMTrack> bgmList = new List<BGMTrack>();

    [Header("主音量(可由Slider控制)")]
    [Range(0f, 1f)][SerializeField] private float masterBGMVolume = 1f;

    private AudioSource audioSource;
    private BGMTrack currentTrack;
    private Coroutine transitionCoroutine; // 用于保存当前的过渡协程，防止冲突

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化组件
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true; // BGM 默认循环
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // 监听场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 取消监听，防止内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 当新场景加载完成时自动触发
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    /// <summary>
    /// 根据场景名查找并播放对应的 BGM
    /// </summary>
    public void PlayBGMForScene(string sceneName)
    {
        BGMTrack targetTrack = bgmList.Find(track => track.sceneName == sceneName);//TODO：理解Find方法的用法，lambda表达式的语法

        if (targetTrack != null)
        {
            PlayBGM(targetTrack);
        }
        else
        {
            Debug.LogWarning($"[BGMManager] 未找到场景 {sceneName} 对应的 BGM 配置。");
        }
    }

    /// <summary>
    /// 核心播放调度器 (外部也可调用此方法直接切歌)
    /// </summary>
    public void PlayBGM(BGMTrack newTrack)
    {
        // 如果新歌和当前正在播放的歌是同一首，则不作处理
        if (currentTrack != null && currentTrack.clip == newTrack.clip && audioSource.isPlaying)
            return;

        // 如果当前有正在进行的过渡，先停止它
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        // 启动新的过渡逻辑
        transitionCoroutine = StartCoroutine(TransitionCoroutine(newTrack));
    }

    /// <summary>
    /// 事件系统切换BGM调用的接口
    /// </summary>
    /// <param name="clipName"></param>
    public void PlayBGMByClipName(string clipName)
    {
        BGMTrack targetTrack = bgmList.Find(track => track.clip != null && track.clip.name == clipName);
        PlayBGM(targetTrack);
    }

    public void SetBGMSliderVolume(float value)
    {
        masterBGMVolume = Mathf.Clamp01(value);
        if (audioSource != null && audioSource.isPlaying && currentTrack != null)
        {
            audioSource.volume = GetBGMSliderVolume(currentTrack);
        }
    }

    private float GetBGMSliderVolume(BGMTrack track)
    {
        if (track == null) return 0f;
        return Mathf.Clamp01(track.targetVolume * masterBGMVolume);
    }

    public float GetMasterBGMVolume()
    {
        return masterBGMVolume;
    }

    // 单轨道过渡协程
    private IEnumerator TransitionCoroutine(BGMTrack newTrack)
    {
        // STEP 1 处理旧音乐 (淡出/停止) 
        if (audioSource.isPlaying)
        {
            if (currentTrack != null && currentTrack.useFadeOut)
            {
                float startVol = audioSource.volume;
                float timer = 0f;

                // 随着时间逐渐降低音量
                while (timer < currentTrack.fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVol, 0f, timer / currentTrack.fadeOutDuration);
                    yield return null; // 等待下一帧
                }
            }
            audioSource.Stop();
            audioSource.volume = 0f;
        }

        // STEP 2 更新当前数据 
        currentTrack = newTrack;

        if (newTrack.clip == null)
            yield break;

        audioSource.clip = newTrack.clip;

        // STEP 3 处理新音乐 (淡入/直接播放) 
        if (newTrack.useFadeIn)
        {
            audioSource.volume = 0f; // 从 0 开始
            audioSource.Play();

            float timer = 0f;

            // 随着时间逐渐提升音量
            while (timer < newTrack.fadeInDuration)
            {
                timer += Time.deltaTime;
                float baseTrackVol = Mathf.Lerp(0f, newTrack.targetVolume, timer / newTrack.fadeInDuration);
                audioSource.volume = Mathf.Clamp01(baseTrackVol * masterBGMVolume);
                yield return null;
            }

            // 最终到达目标值
            audioSource.volume = GetBGMSliderVolume(newTrack);
        }
        else
        {
            // 不使用淡入，直接设置目标音量并播放
            audioSource.volume = GetBGMSliderVolume(newTrack);
            audioSource.Play();
        }

        transitionCoroutine = null;
    }
}