using ClueBoard.Model;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace ClueBoard.UI
{
    public class UIClueNode : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private TextMeshProUGUI clueDescriptionText;//TextMeshProUGUI贴在玩家屏幕上的，TextMeshPro则是长在游戏世界里的
        [SerializeField] private TextMeshProUGUI inkStickText;
        [SerializeField] private UnityEngine.UI.Image inkStickImage;
        [SerializeField] private CanvasGroup tipImage;
        [SerializeField] private RectTransform inkStickConnectRT;// 印泥连接�?
        [SerializeField] private bool hasBeenClicked = false;
        [SerializeField] private float canSeeTipScrollFloat = 16f;

        public RectTransform ConnectRectTransform => inkStickConnectRT; // 连线连接�?
        public Vector3 TargetWorldPos => _targetWorldPos; // 暴露给外部读取真实坐�?

        private Vector3 _targetWorldPos; // 真实激活线索的坐标
        private Camera _miniMapCam;      
        private RectTransform _mapRect;  
        private RectTransform _myRect;   
        private CanvasGroup _canvasGroup;// 视野外隐藏用

        private bool _isInitialized = false;
        private bool _isForceHidden = false;

        // 缓存当前的线索数据，方便内部逻辑判断
        private Clue _currentClueData;

        // 事件
        public event Action<UIClueNode> OnClueNodeClicked;

        /// <summary>
        /// 统一初始化，不要分散到Awake，避免时序问�?
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="cam"></param>
        /// <param name="mapRect"></param>
        public void Initialize(Vector3 worldPos, Camera cam, RectTransform mapRect)
        {
            _targetWorldPos = worldPos;
            _miniMapCam = cam;
            _mapRect = mapRect;
            _myRect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _myRect.anchorMin = new Vector2(0.5f, 0.5f);
            _myRect.anchorMax = new Vector2(0.5f, 0.5f);
            _myRect.pivot = new Vector2(0.5f, 0.5f);

            _isInitialized = true;

            // 初始化阶段不强制依赖相机
            if (EnsureMiniMapCamera())
            {
                UpdatePosition();
            }

            clueDescriptionText.gameObject.SetActive(false);//关闭描述

            Button button = GetComponentInChildren<Button>();//监听
            button.onClick.AddListener(OnClick);
        }

        /// <summary>
        /// 更新UI线索的显示状态（文本、图片或颜色�?
        /// </summary>
        /// <param name="clueData"></param>
        public void UpdateClueState(Clue clueData)
        {
            _currentClueData = clueData;

            hasBeenClicked = clueData.isRead;
            if (clueDescriptionText != null)
            {
                clueDescriptionText.gameObject.SetActive(clueData.isRead);
            }

            if (clueDescriptionText != null && inkStickText != null && inkStickImage != null)
            {
                clueDescriptionText.text = "<rotate=90><line-height=45>" + clueData.clueSO.ClueDesc + "</line-height></rotate>";
                inkStickText.text = "<rotate=90><line-height=45>" + clueData.clueSO.ClueLink + "</line-height></rotate>";
                inkStickImage.sprite = clueData.clueSO.ClueInkStickImage;// 根据SO设置印泥图片
            }
        }

        /// <summary>
        /// 被鼠标左键点击后广播自己
        /// </summary>
        /// <param name="eventData"></param>
        public void OnClick()
        {
            if (!hasBeenClicked)
            {
                OnClueNodeClicked?.Invoke(this);
                clueDescriptionText.gameObject.SetActive(true);
                hasBeenClicked = true;
            }  
        }

        private void LateUpdate()
        {
            if (!_isInitialized || _miniMapCam == null) return;
            UpdatePosition();
            UpdateNPCTip();
        }

        private void UpdatePosition()
        {
            // 1. 计算视口坐标
            Vector3 viewportPos = _miniMapCam.WorldToViewportPoint(_targetWorldPos);

            // 2. 判定是否在视野内
            bool isInViewport = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                                viewportPos.y >= 0 && viewportPos.y <= 1;

            // 3. 【核心优化】综合判断最终可见�?
            // 规则：只有在“相机视野内”且“没有被逻辑强制隐藏”时才显�?
            bool finalVisible = isInViewport && !_isForceHidden;

            // 应用显示状�?
            _canvasGroup.alpha = finalVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = finalVisible;

            // 4. 计算并设�?UI 坐标（即使隐藏也需要计算位置，以便连线逻辑正确获取屏幕坐标�?
            Vector2 mapSize = _mapRect.rect.size;
            Vector2 localPos = new Vector2(
                (viewportPos.x - 0.5f) * mapSize.x,
                (viewportPos.y - 0.5f) * mapSize.y
            );

            _myRect.anchoredPosition = localPos;
        }
        private void UpdateNPCTip()
        {
            float miniMapScrollSize = _miniMapCam.orthographicSize;
            if (miniMapScrollSize < canSeeTipScrollFloat)
            {
                //tipImage显示提前绑定好的图片
                tipImage.alpha = 1f;
            }
            else
            {
                //tipImage不显示图�?
                tipImage.alpha = 0f;
            }
        }

        private bool EnsureMiniMapCamera()
        {
            if (_miniMapCam != null) return true;

            // 从父级 UIClueBoard 动态重取当前有效相机
            UIClueBoard board = GetComponentInParent<UIClueBoard>();
            if (board != null)
            {
                _miniMapCam = board.GetMiniMapCamera();
            }

            return _miniMapCam != null;
        }

        /// <summary>
        /// 外部调用：设置节点的逻辑可见�?
        /// </summary>
        public void SetVisibility(bool isVisible)
        {
            // 如果 isVisible �?true，则不强制隐藏；反之则强制隐�?
            _isForceHidden = !isVisible;

            // 立即刷新一次位置和显隐，避免出现一帧的闪烁
            if (_isInitialized)
            {
                UpdatePosition();
            }
        }
    }
}
