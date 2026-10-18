using ClueBoard.Model;
using Inventory.Model;
using System.Collections.Generic;
using UnityEngine;

//Todo：之后考虑实现与ItemDesClueInteractable类似的视觉反馈
/// <summary>
/// 连接对话系统与线索板的桥梁。
/// 挂载在 NPC 或对话触发器上，利用当前物体的坐标作为线索发现地点。
/// </summary>
public class DialogueClueBridge : MonoBehaviour
{
    [Header("线索背包与线索数据库")]
    [SerializeField] private ClueBoardSO clueBoardData;
    [SerializeField] private ClueDatabaseSO clueDatabase; // 新增：线索数据库

    [Header("视觉反馈设置")]
    [SerializeField]
    private List<Color> feedbackColors = new List<Color>
    {
        Color.cyan,
        Color.yellow,
        Color.magenta,
        Color.green
    };

    /// <summary>
    /// 供对话系统调用的API
    /// </summary>
    /// <param name="clueLink"></param>
    public void OnDialogueEvent(EClueLink clueLink)
    {
        if (clueLink == EClueLink.None) return;

        string linkString = clueLink.ToString();

        OnDialogueEvent(linkString);
    }

    /// <summary>
    /// 对话系统激活线索的核心逻辑
    /// </summary>
    /// <param name="triggerID">
    /// 这里的 triggerLink 直接就是线索 ClueSO 的 ClueLink，例如 "Clue_BloodyKnife"
    /// </param>
    public void OnDialogueEvent(string triggerLink)
    {
        if (clueBoardData == null || clueDatabase == null)
        {
            Debug.LogError($"[DialogueClueBridge] 缺少 ClueBoardSO 或 ClueDatabaseSO 配置！");
            return;
        }

        // 1. 直接去数据库查：这个字符串是不是一个线索 Link？
        ClueSO targetClue = clueDatabase.GetClueByLink(triggerLink);

        if (targetClue == null)
        {
            // 如果查不到，说明这个对话事件可能不是用来给线索的，或者ID填错了
            // 可以选择忽略，或者打印 Warning
            Debug.LogWarning($"对话事件 '{triggerLink}' 没有对应的线索配置，请检查 [ClueDatabaseSO] 是否配置好了线索。");
            return;
        }

        // 2. 查到了！执行原有的收集逻辑
        Vector3 dynamicPos = transform.position;

        // 使用 ClueSO 里的 Link 进行收集
        bool success = clueBoardData.TryCollectClue(targetClue.ClueLink, dynamicPos);

        if (success)
        {
            Debug.Log($"[Bridge] 自动匹配并收集线索:<color=cyan>{targetClue.ClueLink}</color>");
        }

        // 视觉反馈：调用 ClueFeedbackManager
        if (ClueFeedbackManager.Instance != null)
        {
            // 使用当前鼠标位置作为屏幕坐标
            Vector2 screenPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            // 随机选择一个背景颜色（若未配置则使用白色）
            Color chosenColor = Color.white;
            if (feedbackColors != null && feedbackColors.Count > 0)
            {
                int idx = Random.Range(0, feedbackColors.Count);
                chosenColor = feedbackColors[idx];
            }

            ClueFeedbackManager.Instance.PlayCollectionSequence(triggerLink, screenPos, chosenColor);
        }
        else
        {
            Debug.LogWarning("[DialogueClueBridge] ClueFeedbackManager.Instance 为 null，无法播放视觉反馈。");
        }
    }
}