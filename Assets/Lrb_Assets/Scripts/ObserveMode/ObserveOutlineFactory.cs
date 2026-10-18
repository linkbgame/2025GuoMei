using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ObserveMode
{
    /// <summary>
    /// 高亮工厂：为目标 SpriteRenderer 生成两类视觉产物，由 <see cref="ObserveModeSO"/> 的两个开关独立控制：
    /// 1) <c>outlineCloneEnabled</c> = 放大克隆精灵当"假描边"（保留原方案）；
    /// 2) <c>useLight2DGlow</c>     = URP Light2D Point 灯做"自体发光"。
    /// 两者可任意组合：默认两者都开 → 厚发光 + 暖色描边；只关掉 outlineClone → 纯 Light2D 高光。
    ///
    /// 另外修复 URP 2D Renderer 的一个坑：场景里出现任意 Light2D 后，光照管线启动，
    /// 若没有 Global Light 2D，Multiply 通道全黑会让所有 Sprite-Lit-Default 背景被 ×0 变黑/灰。
    /// 通过 <see cref="ObserveModeSO.autoInjectGlobalLight"/> 在进入观察时注入一个临时白色 Global Light。
    /// </summary>
    public class ObserveOutlineFactory
    {
        /// <summary>放大克隆描边子物体名，便于场景调试和清理。</summary>
        private const string OUTLINE_GO_NAME = "__ObserveOutline";

        /// <summary>Light2D 子物体名。</summary>
        private const string LIGHT_GO_NAME = "__ObserveLight2D";

        /// <summary>临时全局光名。</summary>
        private const string GLOBAL_LIGHT_GO_NAME = "__ObserveGlobalLight2D";

        private readonly Dictionary<SpriteRenderer, GameObject> _outlines
            = new Dictionary<SpriteRenderer, GameObject>();

        private readonly Dictionary<SpriteRenderer, GameObject> _lights
            = new Dictionary<SpriteRenderer, GameObject>();

        /// <summary>记录每个 Light2D 的 base intensity，用于 SetGlobalAlpha 时按比例缩放（实现淡入淡出）。</summary>
        private readonly Dictionary<SpriteRenderer, float> _lightBaseIntensity
            = new Dictionary<SpriteRenderer, float>();

        /// <summary>临时注入的 Global Light 2D（如果场景里没有现存的 Global Light）。</summary>
        private GameObject _injectedGlobalLight;
        private float _injectedGlobalLightBaseIntensity;

        /// <summary>反射访问 Light2D.m_ApplyToSortingLayers，让新创建的灯能照亮所有 sortingLayer。</summary>
        private static readonly FieldInfo s_ApplyToSortingLayersField
            = typeof(Light2D).GetField("m_ApplyToSortingLayers",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>当前已高亮目标数量（按"任一类型存在的目标数"，两类都开时通常等同）。</summary>
        public int Count => Mathf.Max(_outlines.Count, _lights.Count);

        /// <summary>
        /// 为一组目标批量生成视觉。重复调用会跳过已经创建过的目标。
        /// </summary>
        public void CreateOutlines(IList<SpriteRenderer> targets, ObserveModeSO cfg)
        {
            if (targets == null || cfg == null) return;

            // 关键 fix：在创建任何 Point Light2D 之前，确保场景里至少有一个 Global Light 2D，
            // 否则 URP 2D Renderer 会把 Multiply 通道全黑，所有 Sprite-Lit-Default 变黑/灰。
            if (cfg.useLight2DGlow && cfg.autoInjectGlobalLight)
            {
                EnsureGlobalLightInjected(cfg);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                SpriteRenderer src = targets[i];
                if (src == null) continue;
                if (src.sprite == null) continue;

                if (cfg.outlineCloneEnabled && !_outlines.ContainsKey(src))
                {
                    CreateOutlineCloneForOne(src, cfg);
                }

                if (cfg.useLight2DGlow && !_lights.ContainsKey(src))
                {
                    CreateLight2DForOne(src, cfg);
                }
            }
        }

        /// <summary>
        /// 同步设置所有视觉产物的全局 alpha（供观察控制器淡入淡出使用）。
        /// 对放大克隆 → 调描边 alpha；对 Light2D → 按 base intensity 缩放 intensity。
        /// 临时 Global Light 也跟随淡入淡出，避免观察淡出时背景突然变灰。
        /// </summary>
        public void SetGlobalAlpha(float alpha01)
        {
            float a = Mathf.Clamp01(alpha01);

            foreach (var kv in _outlines)
            {
                if (kv.Value == null) continue;
                var mirror = kv.Value.GetComponent<ObserveOutlineMirror>();
                if (mirror != null) mirror.SetGlobalAlpha(a);
            }

            foreach (var kv in _lights)
            {
                GameObject lightGO = kv.Value;
                if (lightGO == null) continue;
                var l2d = lightGO.GetComponent<Light2D>();
                if (l2d == null) continue;
                if (_lightBaseIntensity.TryGetValue(kv.Key, out float baseI))
                {
                    l2d.intensity = baseI * a;
                }
            }

            if (_injectedGlobalLight != null)
            {
                var gl = _injectedGlobalLight.GetComponent<Light2D>();
                if (gl != null)
                {
                    // 淡入淡出阶段，全局光也按 alpha 平滑过渡，避免"突然变灰"。
                    // 但下界给 0.0001 以上以避开 BlendStyle Multiply 在 0 强度时被裁剪的边缘情况。
                    gl.intensity = Mathf.Lerp(0f, _injectedGlobalLightBaseIntensity, a);
                }
            }
        }

        /// <summary>
        /// 销毁所有已创建的视觉产物并清空缓存。
        /// </summary>
        public void ClearAll()
        {
            foreach (var kv in _outlines)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            _outlines.Clear();

            foreach (var kv in _lights)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            _lights.Clear();
            _lightBaseIntensity.Clear();

            if (_injectedGlobalLight != null)
            {
                Object.Destroy(_injectedGlobalLight);
                _injectedGlobalLight = null;
                _injectedGlobalLightBaseIntensity = 0f;
            }
        }

        #region creators

        private void CreateOutlineCloneForOne(SpriteRenderer src, ObserveModeSO cfg)
        {
            var go = new GameObject(OUTLINE_GO_NAME);
            // 关键：把克隆挂到源 SpriteRenderer 自身的 transform 下，
            // 这样源是 NPC 的子节点（独立精灵）也能正确跟随。
            go.transform.SetParent(src.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * Mathf.Max(1f, cfg.outlineScale);
            go.layer = src.gameObject.layer;

            var clone = go.AddComponent<SpriteRenderer>();
            clone.sprite = src.sprite;
            clone.flipX = src.flipX;
            clone.flipY = src.flipY;
            clone.sortingLayerID = src.sortingLayerID;
            clone.sortingOrder = src.sortingOrder + cfg.outlineSortingOffset;
            clone.maskInteraction = src.maskInteraction;
            clone.drawMode = src.drawMode;
            clone.color = cfg.outlineColor;

            if (cfg.followSpriteEachFrame)
            {
                var mirror = go.AddComponent<ObserveOutlineMirror>();
                mirror.Bind(src, clone, cfg.outlineColor, cfg.outlineSortingOffset);
                mirror.SetGlobalAlpha(1f);
            }

            _outlines[src] = go;
        }

        private void CreateLight2DForOne(SpriteRenderer src, ObserveModeSO cfg)
        {
            var go = new GameObject(LIGHT_GO_NAME);
            go.transform.SetParent(src.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = src.gameObject.layer;

            var l2d = go.AddComponent<Light2D>();
            l2d.lightType = Light2D.LightType.Point;
            l2d.color = cfg.glowColor;
            l2d.intensity = cfg.glowIntensity;
            l2d.falloffIntensity = cfg.glowFalloff;
            l2d.blendStyleIndex = Mathf.Clamp(cfg.glowBlendStyleIndex, 0, 3);

            // 用源 Sprite 的世界包围盒半径作为 Light2D 半径基准，使大目标光大、小目标光小。
            float worldRadius = ComputeSpriteWorldRadius(src);
            float outer = Mathf.Max(0.05f, worldRadius * cfg.glowOuterRadiusMultiplier);
            float inner = Mathf.Clamp(outer * cfg.glowInnerRadiusRatio, 0f, outer);
            l2d.pointLightOuterRadius = outer;
            l2d.pointLightInnerRadius = inner;

            // 关键 fix：Light2D 默认 m_ApplyToSortingLayers = new int[1]，
            // 只照亮 ID=0 的 sortingLayer，其他图层（NPC/Tilemap）会被忽略。
            // 把它强制设为所有 sortingLayer 的 ID。
            ApplyToAllSortingLayers(l2d);

            _lights[src] = go;
            _lightBaseIntensity[src] = cfg.glowIntensity;
        }

        private void EnsureGlobalLightInjected(ObserveModeSO cfg)
        {
            if (_injectedGlobalLight != null) return;

            // 场景中已存在 Global Light 2D 就不注入，避免双倍亮度。
            var existing = Object.FindObjectsOfType<Light2D>();
            for (int i = 0; i < existing.Length; i++)
            {
                Light2D l = existing[i];
                if (l == null) continue;
                if (!l.enabled || !l.gameObject.activeInHierarchy) continue;
                if (l.lightType == Light2D.LightType.Global)
                {
                    return;
                }
            }

            var go = new GameObject(GLOBAL_LIGHT_GO_NAME);
            var gl = go.AddComponent<Light2D>();
            gl.lightType = Light2D.LightType.Global;
            gl.color = cfg.autoGlobalLightColor;
            gl.intensity = cfg.autoGlobalLightIntensity;
            gl.blendStyleIndex = Mathf.Clamp(cfg.autoGlobalLightBlendStyleIndex, 0, 3);
            ApplyToAllSortingLayers(gl);

            _injectedGlobalLight = go;
            _injectedGlobalLightBaseIntensity = cfg.autoGlobalLightIntensity;
        }

        /// <summary>
        /// 估算 SpriteRenderer 在世界空间下的"半径"，用于驱动 Light2D 的 outer radius。
        /// 取 bounds.extents.magnitude 作为对角线半径，足以覆盖大部分 2D Sprite 形状。
        /// </summary>
        private static float ComputeSpriteWorldRadius(SpriteRenderer src)
        {
            if (src == null) return 0.5f;
            Vector3 ext = src.bounds.extents;
            float r = ext.magnitude;
            // 兜底：刚生成/未渲染时 bounds 可能为 0，给个最小值避免 Light2D 完全不可见
            if (r < 0.05f) r = 0.5f;
            return r;
        }

        /// <summary>
        /// 把 Light2D 强制应用到当前所有 sortingLayer。
        /// URP 默认创建的 Light2D 只覆盖 1 个图层，会出现"灯只照亮某些精灵"的诡异情况。
        /// </summary>
        private static void ApplyToAllSortingLayers(Light2D light)
        {
            if (light == null || s_ApplyToSortingLayersField == null) return;
            var layers = SortingLayer.layers;
            int[] ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                ids[i] = layers[i].id;
            }
            s_ApplyToSortingLayersField.SetValue(light, ids);
        }

        #endregion
    }
}
