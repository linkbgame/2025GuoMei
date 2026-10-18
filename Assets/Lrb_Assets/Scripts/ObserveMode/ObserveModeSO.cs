using UnityEngine;

namespace ObserveMode
{
    /// <summary>
    /// 观察模式（Observe Mode）配置：进入后给画面叠一层"古画做旧"偏黄滤镜，
    /// 同时为屏幕可见的可交互目标生成黄色描边。所有参数 Inspector 可调。
    /// </summary>
    [CreateAssetMenu(fileName = "ObserveModeSO", menuName = "ObserveMode/Config")]
    public class ObserveModeSO : ScriptableObject
    {
        [Header("时序")]
        [Tooltip("一次观察持续多少秒；到点自动退出。")]
        [Min(0.1f)] public float durationSeconds = 15f;

        [Tooltip("两次观察之间的最小冷却（秒）。冷却中按 V 不响应也不播拒绝音。")]
        [Min(0f)] public float cooldownSeconds = 1f;

        [Tooltip("进入观察时滤镜淡入时长（秒）。")]
        [Range(0f, 2f)] public float fadeInDuration = 0.3f;

        [Tooltip("退出观察时滤镜淡出时长（秒）。")]
        [Range(0f, 2f)] public float fadeOutDuration = 0.3f;

        [Header("画面色调（古画做旧，单色 tint+偏黄）")]
        [Tooltip("全屏 Image 叠色目标颜色。alpha 决定最终强度，建议 0.25~0.45。")]
        public Color tintColor = new Color(0.92f, 0.78f, 0.42f, 0.35f);

        [Header("描边（放大克隆版本）")]
        [Tooltip("是否在 Light2D 之上叠加放大克隆描边。" +
                 "开 = Light2D 自体发光 + 放大克隆描边并存；" +
                 "关 = 只保留 Light2D 自体发光。")]
        public bool outlineCloneEnabled = true;

        [Tooltip("可交互目标描边颜色。建议偏暖黄、alpha=1。")]
        public Color outlineColor = new Color(1f, 0.86f, 0.2f, 1f);

        [Tooltip("描边相对源精灵的整体放大系数（实现\"假描边\"）。1.05~1.15 之间最自然。")]
        [Range(1.0f, 1.5f)] public float outlineScale = 1.08f;

        [Tooltip("描边相对源 SpriteRenderer.sortingOrder 的偏移；负值表示描边在源后面。")]
        public int outlineSortingOffset = -1;

        [Tooltip("是否每帧把源精灵的 sprite/flipX/flipY/sortingOrder/color.a 同步到描边克隆，处理动画切帧/翻转。")]
        public bool followSpriteEachFrame = true;

        [Header("自体发光（URP 2D Light）")]
        [Tooltip("是否给目标加一个 Point Light2D 当自体发光。需工程使用 URP 2D Renderer（本工程已确认）。")]
        public bool useLight2DGlow = true;

        [Tooltip("发光颜色。建议偏暖黄；勾选 HDR 时可超 1。")]
        [ColorUsage(true, true)] public Color glowColor = new Color(1f, 0.86f, 0.2f, 1f);

        [Tooltip("发光强度（Light2D.intensity）。Additive 混合下 0.8~2.0 比较常用。")]
        [Range(0f, 5f)] public float glowIntensity = 1.2f;

        [Tooltip("Outer 半径相对源 Sprite 包围盒半径的倍数。1.0~2.5 比较自然。")]
        [Range(0.1f, 5f)] public float glowOuterRadiusMultiplier = 1.5f;

        [Tooltip("Inner 半径相对 Outer 的比例（0=点光，1=硬边光）。0.1~0.4 比较像高光。")]
        [Range(0f, 1f)] public float glowInnerRadiusRatio = 0.2f;

        [Tooltip("Light2D 的 falloff 强度（0=线性硬边，1=极柔软）。0.6~0.9 比较像高光。")]
        [Range(0f, 1f)] public float glowFalloff = 0.7f;

        [Tooltip("Light2D 使用的 Blend Style 索引。本工程 URP_Renderer.asset 内：0=Multiply, 1=Additive, 2=MultiplyMask, 3=AdditiveMask。Additive 最像\"自体发光\"。")]
        [Range(0, 3)] public int glowBlendStyleIndex = 1;

        [Header("自动 Global Light（修复 URP 2D 已知坑）")]
        [Tooltip("URP 2D Renderer 在场景出现任意 Light2D 时会启动光照管线，若没有 Global Light，" +
                 "Multiply 通道全黑会让 Sprite-Lit-Default 背景全部变黑/灰。" +
                 "勾选后进入观察自动注入一个临时 Global Light 2D，退出时销毁。建议保持开启。")]
        public bool autoInjectGlobalLight = true;

        [Tooltip("自动注入的 Global Light 2D 颜色，建议白色。")]
        public Color autoGlobalLightColor = Color.white;

        [Tooltip("自动注入的 Global Light 2D 强度。1 ≈ 完全还原原色；> 1 会过曝；< 1 偏暗。")]
        [Range(0f, 2f)] public float autoGlobalLightIntensity = 1f;

        [Tooltip("自动注入的 Global Light 用的 Blend Style。必须是 Multiply 通道，本工程为 0。")]
        [Range(0, 3)] public int autoGlobalLightBlendStyleIndex = 0;

        [Header("交互")]
        [Tooltip("观察模式触发键（Toggle 开/关）。")]
        public KeyCode toggleKey = KeyCode.V;

        [Tooltip("处于这些场景时按 V 直接屏蔽（仅播拒绝音效）。区分大小写，按场景名匹配。")]
        public string[] blockingScenes = new string[]
        {
            "PinTu_HuaChe",
            "PinTu_JiuLou",
            "PinTu_House",
            "ChuPu",
        };

        [Header("音效")]
        [Tooltip("被屏蔽时按 V 的拒绝音效（\"咔\"）。建议必填。")]
        public AudioClip rejectClip;

        [Tooltip("进入观察的提示音效（可选）。")]
        public AudioClip enterClip;

        [Tooltip("退出观察的提示音效（可选）。")]
        public AudioClip exitClip;
    }
}
