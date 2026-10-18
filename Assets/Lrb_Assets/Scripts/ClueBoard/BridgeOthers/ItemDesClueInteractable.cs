using ClueBoard.Model;
using Inventory.UI;
using System.Text.RegularExpressions;
using TMPro; 
using UnityEngine;
using UnityEngine.EventSystems; 


public class ItemDesClueInteractable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI descriptionText; // 显示描述的文本组件
    [SerializeField] private ClueBoardSO clueBoardData;       // 数据中心引用

    private UIInventoryDescription m_inventoryDescUI;

    [SerializeField] private string m_originalText;

    private void Awake()
    {
        // 自动获取挂载在同一个对象上的 UIInventoryDescription
        m_inventoryDescUI = GetComponent<UIInventoryDescription>();

        // 如果没有手动拖拽，也可以自动获取 Text 组件
        if (descriptionText == null) descriptionText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 新增：供 UIInventoryDescription.SetDescription 调用
    /// 解决了时序问题和数据污染问题
    /// </summary>
    /// <param name="rawDescription">带下划线的原始文本</param>
    public void UpdateContent(string rawDescription)
    {
        // 1. 更新数据源：这是解决“数据残留”的关键
        m_originalText = rawDescription;

        // 2. 立即基于新的数据源刷新视觉
        RefreshTextVisuals();
    }

    /// <summary>
    /// 【核心交互】当鼠标点击文本区域时触发
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 查找鼠标点击了哪个 Link
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(descriptionText, Input.mousePosition, null);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = descriptionText.textInfo.linkInfo[linkIndex];
            string targetClueName = linkInfo.GetLinkID();
            m_originalText = descriptionText.text;

            // 触发逻辑
            TriggerClueDiscovery(targetClueName, Input.mousePosition);
        }
    }

    private void TriggerClueDiscovery(string targetName, Vector2 mousePos)
    {
        if (m_inventoryDescUI == null) return;

        Vector3 worldPos = m_inventoryDescUI.GetCluePos();

        // 1. 数据逻辑
        bool success = clueBoardData.TryCollectClue(targetName, worldPos);

        // 2. 无论是否是"第一次"收集，只要点击了，我们都检查并刷新视觉
        // (因为可能之前收集了，但界面还没刷新)
        if (success)
        {
            Debug.Log($"<color=green>线索 [{targetName}] 激活成功！</color>");
            Color color = clueBoardData.GetClueReasonedColor(targetName);

            if (ClueFeedbackManager.Instance != null)
            {
                ClueFeedbackManager.Instance.PlayCollectionSequence(targetName, mousePos, color);
            }
        }

        // 3. 核心：点击后立即刷新文本显示 (去下划线)
        RefreshTextVisuals();
    }

    /// <summary>
    /// 核心方法：根据 Model 状态，动态生成不带下划线的文本
    /// </summary>
    private void RefreshTextVisuals()
    {
        //非空保护
        if (descriptionText == null || clueBoardData == null || string.IsNullOrEmpty(m_originalText))
        {
            if (descriptionText != null) descriptionText.text = m_originalText;
            return;
        }

        // 正则表达式：匹配 <link="ID">内容</link> 的结构
        // Group 1: <link="ID">
        // Group 2: ID (我们通过再解析获取，或者优化正则直接提取)
        // Group 3: 内容 (其中可能包含 <u>)
        // Group 4: </link>

        // 优化正则：直接捕获 ID
        string pattern = @"(<link=""?([^"">]+)""?>)(.*?)(</link>)";

        // 【优化点】如果文本中不包含 Link，则直接赋值，避免正则替换的性能消耗
        if (!Regex.IsMatch(m_originalText, pattern))
        {
            descriptionText.text = m_originalText;
            return;
        }

        string newText = Regex.Replace(m_originalText, pattern, (match) =>
        {
            string fullPrefix = match.Groups[1].Value; // <link="abc">
            string linkID = match.Groups[2].Value;     // abc
            string content = match.Groups[3].Value;    // <u>线索内容</u>
            string suffix = match.Groups[4].Value;     // </link>

            // 询问 Model：这个 ID 收集过吗？
            if (clueBoardData.IsClueCollected(linkID))
            {
                // 如果已收集：移除内容中的 <u> 和 </u> 标签
                string cleanContent = content.Replace("<u>", "").Replace("</u>", "");
                return fullPrefix + cleanContent + suffix;
            }
            else
            {
                // 如果未收集：保持原样 (带下划线)
                return match.Value;
            }
        });

        // 将处理过的文本赋值给 UI
        descriptionText.text = newText;
    }  
}
