using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Image序列帧动画播放器
/// 用于在Unity UI的Image组件上播放Sprite序列帧动画
/// </summary>
public class SpriteAnimator : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("目标Image组件，不设置则自动获取当前物体上的Image")]
    private Image targetImage;
    
    [Tooltip("要播放的Sprite序列")]
    public Sprite[] sprites;
    
    [Tooltip("每秒播放帧数")]
    [Range(1f, 60f)]
    public float frameRate = 10f;
    
    [Tooltip("是否循环播放")]
    public bool loop = true;
    
    [Tooltip("是否在Awake时自动播放")]
    public bool playOnAwake = true;

    [Header("状态信息（只读）")]
    [SerializeField] private int currentFrame;
    [SerializeField] private bool isPlaying;

    private float timer;

    public void RunWhite()
    {

    }
    public void RunBlack()
    {

    }
    /// <summary>
    /// 动画播放完成时的回调（仅在非循环模式下触发）
    /// </summary>
    public Action onComplete;

    /// <summary>
    /// 当前是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// 当前帧索引
    /// </summary>
    public int CurrentFrame => currentFrame;

    /// <summary>
    /// 总帧数
    /// </summary>
    public int TotalFrames => sprites?.Length ?? 0;

    void Start()
    {
        // 如果没有指定targetImage，尝试获取当前物体上的Image组件
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null)
        {
            Debug.LogError($"[SpriteAnimator] {gameObject.name}: 未找到Image组件！");
            enabled = false;
            return;
        }

        if (playOnAwake)
            Play();
    }

    void Update()
    {
        if (!isPlaying || sprites == null || sprites.Length == 0)
            return;

        timer += Time.deltaTime;

        // 当计时器超过帧间隔时间时，切换到下一帧
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame++;

            // 检查是否播放完毕
            if (currentFrame >= sprites.Length)
            {
                if (loop)
                {
                    // 循环模式：回到第一帧
                    currentFrame = 0;
                }
                else
                {
                    // 非循环模式：停在最后一帧并触发回调
                    currentFrame = sprites.Length - 1;
                    isPlaying = false;
                    onComplete?.Invoke();
                    return;
                }
            }

            // 更新Image的Sprite
            targetImage.sprite = sprites[currentFrame];
        }
    }

    /// <summary>
    /// 从头开始播放动画
    /// </summary>
    public void Play()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[SpriteAnimator] {gameObject.name}: Sprites数组为空！");
            return;
        }

        currentFrame = 0;
        timer = 0f;
        isPlaying = true;
        targetImage.sprite = sprites[0];
    }

    /// <summary>
    /// 从指定帧开始播放
    /// </summary>
    /// <param name="startFrame">起始帧索引</param>
    public void PlayFromFrame(int startFrame)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[SpriteAnimator] {gameObject.name}: Sprites数组为空！");
            return;
        }

        currentFrame = Mathf.Clamp(startFrame, 0, sprites.Length - 1);
        timer = 0f;
        isPlaying = true;
        targetImage.sprite = sprites[currentFrame];
    }

    /// <summary>
    /// 停止播放并重置到第一帧
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        currentFrame = 0;
        timer = 0f;
        
        if (sprites != null && sprites.Length > 0 && targetImage != null)
            targetImage.sprite = sprites[0];
    }

    /// <summary>
    /// 暂停播放（保持当前帧）
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 继续播放
    /// </summary>
    public void Resume()
    {
        if (sprites != null && sprites.Length > 0)
            isPlaying = true;
    }

    /// <summary>
    /// 跳转到指定帧
    /// </summary>
    /// <param name="frameIndex">帧索引</param>
    public void GoToFrame(int frameIndex)
    {
        if (sprites == null || sprites.Length == 0)
            return;

        currentFrame = Mathf.Clamp(frameIndex, 0, sprites.Length - 1);
        targetImage.sprite = sprites[currentFrame];
    }

    /// <summary>
    /// 设置新的Sprite序列并可选择是否立即播放
    /// </summary>
    /// <param name="newSprites">新的Sprite数组</param>
    /// <param name="autoPlay">是否自动播放</param>
    public void SetSprites(Sprite[] newSprites, bool autoPlay = true)
    {
        sprites = newSprites;
        
        if (autoPlay)
            Play();
        else
            Stop();
    }
}
