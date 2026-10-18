using UnityEngine;

namespace ObserveMode
{
    /// <summary>
    /// 描边镜像：把源 SpriteRenderer 的 sprite/翻转/层级/alpha 在 LateUpdate
    /// 同步到克隆 SpriteRenderer，处理动画切帧、flip、排序变化。
    /// 源被销毁时本组件自动销毁宿主 GameObject。
    /// 不强制 follow position/rotation/scale，因为克隆已经作为子物体跟随。
    /// </summary>
    [DisallowMultipleComponent]
    public class ObserveOutlineMirror : MonoBehaviour
    {
        private SpriteRenderer _source;
        private SpriteRenderer _clone;

        /// <summary>描边相对源 sortingOrder 的偏移（保持构造时配置的偏移量）。</summary>
        private int _sortingOffset;

        /// <summary>描边主色（不含 alpha；alpha 跟随源 alpha 与全局淡入淡出）。</summary>
        private Color _baseColor = Color.white;

        /// <summary>额外的全局 alpha 倍率（observe controller 控制淡入淡出整体强度）。</summary>
        private float _globalAlpha = 1f;

        public void Bind(SpriteRenderer source, SpriteRenderer clone, Color baseColor, int sortingOffset)
        {
            _source = source;
            _clone = clone;
            _baseColor = baseColor;
            _sortingOffset = sortingOffset;
            ApplyColor();
        }

        /// <summary>
        /// 由 ObserveModeController 在淡入淡出过程中调用，统一调节描边整体透明度。
        /// </summary>
        public void SetGlobalAlpha(float alpha01)
        {
            _globalAlpha = Mathf.Clamp01(alpha01);
            ApplyColor();
        }

        private void LateUpdate()
        {
            if (_source == null || _clone == null)
            {
                if (this != null) Destroy(gameObject);
                return;
            }

            if (_clone.sprite != _source.sprite) _clone.sprite = _source.sprite;
            if (_clone.flipX != _source.flipX) _clone.flipX = _source.flipX;
            if (_clone.flipY != _source.flipY) _clone.flipY = _source.flipY;

            int targetOrder = _source.sortingOrder + _sortingOffset;
            if (_clone.sortingOrder != targetOrder) _clone.sortingOrder = targetOrder;
            if (_clone.sortingLayerID != _source.sortingLayerID) _clone.sortingLayerID = _source.sortingLayerID;

            // 跟随源精灵透明度变化（例如 ItemFader/TargetItemFader 的淡入淡出）
            float srcAlpha = _source.color.a;
            if (!Mathf.Approximately(_lastSrcAlpha, srcAlpha))
            {
                _lastSrcAlpha = srcAlpha;
                ApplyColor();
            }
        }

        private float _lastSrcAlpha = 1f;

        private void ApplyColor()
        {
            if (_clone == null) return;
            float srcAlpha = _source != null ? _source.color.a : 1f;
            Color c = _baseColor;
            c.a = _baseColor.a * srcAlpha * _globalAlpha;
            _clone.color = c;
        }
    }
}
