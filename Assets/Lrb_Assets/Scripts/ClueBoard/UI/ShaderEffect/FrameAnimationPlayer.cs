using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace ClueBoard
{
    /// <summary>
    /// 纯代码序列帧播放器 (基于 DOTween)
    /// 用于在不使用 Animator 的情况下播放水墨绽开特效
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FrameAnimationPlayer : MonoBehaviour
    {
        [Header("资产配置")]
        [SerializeField] private List<Sprite> frames; // 存放你的 16 张图片
        [SerializeField] private float duration = 0.5f; // 动画时长
        [SerializeField] private Ease bloomEase = Ease.OutExpo; // 绽开缓动(建议先快后慢)
        public bool isStartToPlay = false;

        private Image m_image;
        private int m_currentIndex = -1;
        private Tween m_activeTween;

        private void Awake()
        {
            m_image = GetComponent<Image>();
            // 初始获取第一帧
            if (frames.Count > 0) m_image.sprite = frames[0];

            //隐藏图片直到播放
            m_image.color = new Color(m_image.color.r, m_image.color.g, m_image.color.b, 0);
        }

        private void Start()
        {
            if (isStartToPlay)
            {
                PlayBloom();
            }
        }

        /// <summary>
        /// 核心方法：播放绽开动画
        /// </summary>
        public void PlayBloom(System.Action onComplete = null)
        {
            if (frames == null || frames.Count == 0) return;

            m_image.color = new Color(m_image.color.r, m_image.color.g, m_image.color.b, 1);

            m_activeTween?.Kill();

            // 宏观思路：使用 DOTween 驱动一个虚拟的索引值
            // 从 0 变化到 最后一帧的索引
            int lastIndex = frames.Count - 1;
            float virtualIndex = 0;

            m_activeTween = DOTween.To(() => virtualIndex, x => {
                virtualIndex = x;
                int index = Mathf.FloorToInt(virtualIndex);
                if (index != m_currentIndex && index <= lastIndex)
                {
                    m_currentIndex = index;
                    m_image.sprite = frames[m_currentIndex];
                }
            }, lastIndex, duration)
            .SetEase(bloomEase)
            .OnComplete(() => onComplete?.Invoke());

        }

        /// <summary>
        /// 播放消散动画 (反向播放或淡出)
        /// </summary>
        public void PlayFadeOut(float fadeDuration)
        {
            m_image.DOFade(0, fadeDuration).OnComplete(() => {
                gameObject.SetActive(false);
            });
        }

        //按钮绑定事件，能够关闭和激活该对象
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                PlayBloom();
            }
        }

        private void OnDestroy()
        {
            m_activeTween?.Kill();
        }
    }
}