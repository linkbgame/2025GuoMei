using UnityEngine;
using UnityEngine.UI;

namespace ClueBoard.UI
{
    /// <summary>
    /// 专门用于连接 Animator 与 Shader 属性的桥接脚本
    /// 挂载在带有 Image 的矩形对象上，让 Animator 可以 K 帧控制 _StrokeLength
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class BrushMaterialBridge : MonoBehaviour
    {
        [Header("Shader 属性控制")]
        [Tooltip("Animator 将会控制这个值 (-1.5 到 1)")]
        [Range(-1.5f, 1f)]
        public float strokeLength = -1.5f;

        [Tooltip("控制整体透明度 (0 到 1)")]
        [Range(0f, 1f)]
        public float brushAlpha = 1f;

        [Header("配置")]
        [SerializeField] private string lengthPropertyName = "_StrokeLength";
        [SerializeField] private string alphaPropertyName = "_Alpha";

        private Graphic m_graphic;
        private Material m_instanceMaterial;
        private int m_lengthId;
        private int m_alphaId;

        private void Awake()
        {
            m_graphic = GetComponent<Graphic>();
            m_lengthId = Shader.PropertyToID(lengthPropertyName);
            m_alphaId = Shader.PropertyToID(alphaPropertyName);

            // 运行时创建材质副本，防止 Animator 修改原始资源
            if (Application.isPlaying && m_graphic.material != null)
            {
                m_instanceMaterial = new Material(m_graphic.material);
                m_graphic.material = m_instanceMaterial;
            }
        }

        private void Update()
        {
            UpdateProperties();
        }

        /// <summary>
        /// 将 C# 属性值同步到材质球
        /// </summary>
        private void UpdateProperties()
        {
            // 优先使用运行时副本，编辑器模式下使用 sharedMaterial 预览
            Material targetMat = Application.isPlaying ? m_instanceMaterial : m_graphic.material;

            if (targetMat != null)
            {
                if (targetMat.HasProperty(m_lengthId))
                    targetMat.SetFloat(m_lengthId, strokeLength);

                if (targetMat.HasProperty(m_alphaId))
                    targetMat.SetFloat(m_alphaId, brushAlpha);
            }
        }

        // 确保在编辑器面板修改 Stroke Length 时也能实时看到效果
        private void OnValidate()
        {
            if (m_graphic == null) m_graphic = GetComponent<Graphic>();
            m_lengthId = Shader.PropertyToID(lengthPropertyName);
            m_alphaId = Shader.PropertyToID(alphaPropertyName);
            UpdateProperties();
        }

        private void OnDestroy()
        {
            if (m_instanceMaterial != null)
            {
                if (Application.isPlaying) Destroy(m_instanceMaterial);
                else DestroyImmediate(m_instanceMaterial);
            }
        }
    }
}