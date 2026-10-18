using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class InkSpreadUI : MonoBehaviour
{
    [Header("目标 Image（必须）")]
    public Image targetImage;

    [Header("扩散设置")]
    public float spreadDuration = 3f;       // 扩散耗时（秒）
    public float maxRadius = 0.8f;         // 最大半径（0..1，1 表示到达对角）
    [Range(0.001f, 0.5f)]
    public float feather = 0.08f;          // 边缘柔和度

    [Header("淡化设置")]
    public float fadeDuration = 1.2f;      // 整体淡出耗时（秒）

    [Header("墨色")]
    public Color inkColor = Color.black;
    
    [LabelText("可选曲线（控制“速度先快后慢”）")]
    public AnimationCurve spreadCurve = null; // 若为空，使用内置 ease-out quad

    [Header("Shader 名称（确保与 shader 文件一致）")]
    public string shaderName = "Custom/UI_InkSpread";

    [Header("测试开关（运行时自动播放，便于快速验证）")]
    public bool autoPlayForTest = false;

    // 在开始淡化（fade 开始的第一帧）会调用
    public UnityAction OnFadeStart;

    private Material instanceMat;
    private Coroutine playCoroutine;

    private static readonly string PROP_INK_COLOR = "_InkColor";
    private static readonly string PROP_RADIUS = "_Radius";
    private static readonly string PROP_FEATHER = "_Feather";
    private static readonly string PROP_ALPHA = "_Alpha";

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        Debug.Log($"[InkSpreadUI] Awake targetImage={(targetImage!=null)} shaderName={shaderName}");
        EnsureMaterial();
        ApplyInitialProperties();
    }

    private void Start()
    {
        if (autoPlayForTest)
        {
            Debug.Log("[InkSpreadUI] autoPlayForTest enabled -> Play()");
            Play();
        }
    }

    private void EnsureMaterial()
    {
        if (targetImage == null)
        {
            Debug.LogWarning("[InkSpreadUI] targetImage is null");
            return;
        }

        var currentShaderName = targetImage.material != null && targetImage.material.shader != null ? targetImage.material.shader.name : "null";
        Debug.Log($"[InkSpreadUI] targetImage.material.shader = {currentShaderName}");

        Shader s = Shader.Find(shaderName);
        if (s == null)
        {
            Debug.LogError($"[InkSpreadUI] Shader '{shaderName}' 未找到，请确认已添加并编译。运行时查看 Console 的 shader 编译错误。");
            return;
        }

        // 如果当前材质不是期望 shader 或没有材质，则创建实例材质并赋值
        if (targetImage.material == null || targetImage.material.shader == null || targetImage.material.shader.name != shaderName)
        {
            instanceMat = new Material(s);
            targetImage.material = instanceMat;
            Debug.Log($"[InkSpreadUI] 新建实例材质并赋给 Image：{instanceMat.shader.name}");
        }
        else
        {
            instanceMat = Instantiate(targetImage.material);
            targetImage.material = instanceMat;
            Debug.Log($"[InkSpreadUI] 克隆并替换现有材质实例：{instanceMat.shader.name}");
        }
    }

    private void ApplyInitialProperties()
    {
        if (instanceMat == null) return;
        if (instanceMat.HasProperty(PROP_INK_COLOR)) instanceMat.SetColor(PROP_INK_COLOR, inkColor);
        if (instanceMat.HasProperty(PROP_RADIUS)) instanceMat.SetFloat(PROP_RADIUS, 0f);
        if (instanceMat.HasProperty(PROP_FEATHER)) instanceMat.SetFloat(PROP_FEATHER, feather);
        if (instanceMat.HasProperty(PROP_ALPHA)) instanceMat.SetFloat(PROP_ALPHA, 1f);
        Debug.Log("[InkSpreadUI] ApplyInitialProperties 完成");
    }

    // 外部调用开始播放
    public void Play()
    {
        Debug.Log("[InkSpreadUI] Play() called");
        if (playCoroutine != null) StopCoroutine(playCoroutine);
        EnsureMaterial();
        ApplyInitialProperties();
        playCoroutine = StartCoroutine(PlayRoutine());
    }

    // 停止并重置（恢复到无墨状态）
    public void StopAndReset()
    {
        Debug.Log("[InkSpreadUI] StopAndReset()");
        if (playCoroutine != null) StopCoroutine(playCoroutine);
        if (instanceMat != null)
        {
            if (instanceMat.HasProperty(PROP_RADIUS)) instanceMat.SetFloat(PROP_RADIUS, 0f);
            if (instanceMat.HasProperty(PROP_ALPHA)) instanceMat.SetFloat(PROP_ALPHA, 0f);
        }
    }

    private IEnumerator PlayRoutine()
    {
        Debug.Log("[InkSpreadUI] PlayRoutine start - spread phase");
        // 扩散阶段
        float t = 0f;
        while (t < spreadDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / spreadDuration);
            float eased = EvaluateSpreadCurve(normalized); // 0..1
            float radius = eased * maxRadius;
            if (instanceMat != null)
            {
                if (instanceMat.HasProperty(PROP_RADIUS)) instanceMat.SetFloat(PROP_RADIUS, radius);
                if (instanceMat.HasProperty(PROP_FEATHER)) instanceMat.SetFloat(PROP_FEATHER, feather);
                if (instanceMat.HasProperty(PROP_INK_COLOR)) instanceMat.SetColor(PROP_INK_COLOR, inkColor);
                if (instanceMat.HasProperty(PROP_ALPHA)) instanceMat.SetFloat(PROP_ALPHA, 1f);
            }
            yield return null;
        }

        Debug.Log("[InkSpreadUI] spread complete -> invoking OnFadeStart and starting fade");
        // 扩散完成，立即触发淡化回调（只触发一次）
        OnFadeStart?.Invoke();

        // 淡化阶段（整体 alpha 从 1 -> 0）
        float ft = 0f;
        while (ft < fadeDuration)
        {
            ft += Time.deltaTime;
            float a = Mathf.Clamp01(1f - ft / fadeDuration);
            if (instanceMat != null && instanceMat.HasProperty(PROP_ALPHA))
            {
                instanceMat.SetFloat(PROP_ALPHA, a);
            }
            yield return null;
        }

        // 完成后确保 alpha 为 0
        if (instanceMat != null && instanceMat.HasProperty(PROP_ALPHA))
            instanceMat.SetFloat(PROP_ALPHA, 0f);

        Debug.Log("[InkSpreadUI] PlayRoutine finished");
        playCoroutine = null;
    }

    private float EvaluateSpreadCurve(float t)
    {
        if (spreadCurve != null && spreadCurve.keys != null && spreadCurve.keys.Length > 0)
            return spreadCurve.Evaluate(t);

        // 默认 ease-out quad（速度先快后慢）： f(t) = 1 - (1-t)^2
        return 1f - (1f - t) * (1f - t);
    }

    // 编辑器或运行时可更新参数并刷新材质
    private void OnValidate()
    {
        if (instanceMat != null)
        {
            if (instanceMat.HasProperty(PROP_FEATHER)) instanceMat.SetFloat(PROP_FEATHER, Mathf.Max(0.001f, feather));
            if (instanceMat.HasProperty(PROP_INK_COLOR)) instanceMat.SetColor(PROP_INK_COLOR, inkColor);
        }
    }
}
