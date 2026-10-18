using UnityEngine;
using UnityEngine.UI;

namespace ClueBoard.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class UILineConnector : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _lineImage;
        private CanvasGroup _canvasGroup; // 自己的 CanvasGroup

        private RectTransform _lineRect;
        private Canvas _rootCanvas;
        [SerializeField] private Camera _uiCamera;

        // --- 动态跟随的目标 ---
        private RectTransform _targetA;
        private RectTransform _targetB;

        // --- 端点的可见性引用 ---
        private CanvasGroup _groupA;
        private CanvasGroup _groupB;

        private bool _isDynamic = false; // 标记是否开启实时跟随

        // 缓存当前的样式参数，用于实时重绘
        private float _currentLineWidth;

        // 生长进度控制 ---
        private float _growthProgress = 1f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _lineImage = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();

            // 关键：强制设置轴心在左侧垂直居中
            // 这样 Position 就是起点，Width 就是长度，Rotation 就是方向
            _rectTransform.pivot = new Vector2(0f, 0.5f);

            // 确保 RaycastTarget 关闭，避免连线阻挡鼠标点击线索
            _lineImage.raycastTarget = false;
        }

        /// <summary>
        /// 设置当前的生长进度 (0-1)
        /// </summary>
        public void SetGrowth(float progress)
        {
            _growthProgress = Mathf.Clamp01(progress);
            // 立即刷新一次防止视觉延迟
            if (_isDynamic && _targetA != null && _targetB != null) Refresh();
        }

        /// <summary>
        /// 这里先用两点起点和终点，连接确立了线段，之后再来动画连线
        /// </summary>
        /// <param name="nodeA">起点的 UI 节点</param>
        /// <param name="nodeB">终点的 UI 节点</param>
        public void ConnectDynamic(RectTransform nodeRTA, RectTransform nodeRTB, RectTransform lineContainer, float lineWidth, Sprite customSprite = null)
        {
            if (_rectTransform == null) Awake();

            _targetA = nodeRTA;
            _targetB = nodeRTB;
            _lineRect = lineContainer;

            // 获取两个端点的 CanvasGroup，用于后续判断它们是否在视野内
            _groupA = nodeRTA.GetComponent<CanvasGroup>();
            _groupB = nodeRTB.GetComponent<CanvasGroup>();

            _currentLineWidth = lineWidth;
            _isDynamic = true;

            // 初始化外观 (Sprite)
            if (customSprite != null) _lineImage.sprite = customSprite;
            if (_lineImage.type == Image.Type.Tiled) _lineImage.pixelsPerUnitMultiplier = 1f;

            _rootCanvas = _lineRect.GetComponentInParent<Canvas>();
            _uiCamera = (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _rootCanvas.worldCamera
                : null;

            // 立即刷新一次
            Refresh();
        }

        // 每帧刷新
        private void LateUpdate()
        {
            if (_isDynamic && _targetA != null && _targetB != null)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 实时计算形状 + 实时更新可见性
        /// </summary>
        private void Refresh()
        {
            if (!_isDynamic) return;

            Vector2 startLocal = GetLocalPointInLineContainer(_targetA);
            Vector2 endLocal = GetLocalPointInLineContainer(_targetB);

            // 实时计算几何形状 (位置、旋转、长度)
            UpdateLineGeometry(startLocal, endLocal, _currentLineWidth);

            // 实时更新可见性 (Culling)
            UpdateVisibility();
        }

        private Vector2 GetLocalPointInLineContainer(RectTransform source)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_uiCamera, source.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _lineRect,// 在这个Rect里计算坐标，统一计算连线的UGUI坐标系的关键
                screenPoint,
                _uiCamera,
                out Vector2 localPoint
            );
            return localPoint;
        }

        /// <summary>
        /// 只要有一个点在视野内(Alpha > 0)，线就应该显示
        /// </summary>
        private void UpdateVisibility()
        {
            if (_groupA != null && _groupB != null)
            {
                // 策略：只要有一个点在视野内(Alpha > 0)，线就应该显示，连接到屏幕外
                // 只有当两个点都完全隐藏时，线才隐藏
                bool isVisible = _groupA.alpha > 0.01f || _groupB.alpha > 0.01f;
                _canvasGroup.alpha = isVisible ? 1f : 0f;
            }
            else
            {
                // 如果无法获取端点状态，默认保持显示
                _canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// 输入参数为起点终点和宽度，其他方法修改长度，最终更新UI效果
        /// </summary>
        private void UpdateLineGeometry(Vector2 startPos, Vector2 endPos, float lineWidth)
        {
            _rectTransform.anchoredPosition = startPos;

            Vector2 dir = endPos - startPos;
            float distance = dir.magnitude * _growthProgress;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _rectTransform.sizeDelta = new Vector2(distance, lineWidth);
        }
    }
}
