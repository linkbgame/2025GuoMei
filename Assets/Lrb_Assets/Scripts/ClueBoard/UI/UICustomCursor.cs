using UnityEngine;
using UnityEngine.UI;

namespace ClueBoard.UI
{
    public class UICustomCursor : MonoBehaviour
    {
        public static UICustomCursor Instance;

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private Image _cursorImage;
        private bool _isOverrideMode = false; // 标记：是否处于动画接管模式

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();
            _cursorImage = GetComponent<Image>();

            // 初始隐藏软件光标，只有在需要时才显示
            _cursorImage.enabled = false;
            _cursorImage.raycastTarget = false;

            /*// 隐藏系统硬件鼠标
            // 注意：在编辑器里测试时按 ESC 键可以切回普通鼠标
            Cursor.visible = true;*/
        }

        private void Update()
        {
            // 如果软件光标开启了，让它实时跟随鼠标
            if (_cursorImage.enabled && !_isOverrideMode)
            {
                UpdateCursorPosition(Input.mousePosition);
            }
        }

        // 动画期间强制设置光标位置
        public void SetOverridePosition(Vector2 screenPos)
        {
            _isOverrideMode = true;
            UpdateCursorPosition(screenPos);
        }

        // 结束动画，还回控制权
        public void ReleaseOverride()
        {
            _isOverrideMode = false;
        }

        private void UpdateCursorPosition(Vector2 screenPos)
        {
            Vector2 localPos;
            // 将鼠标屏幕坐标转换为 UI 局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvas.transform as RectTransform,
                screenPos,
                _parentCanvas.worldCamera,
                out localPos
            );
            _rectTransform.anchoredPosition = localPos;
        }

        // 提供给 UIClueNode 调用：开启毛笔
        public void SetBrushActive(bool active)
        {
            _cursorImage.enabled = active;
            Cursor.visible = !active; // 开启毛笔时隐藏系统指针
        }
    }
}