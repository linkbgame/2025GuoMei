using System.Collections.Generic;
using UnityEngine;

namespace ClueBoard.Model
{
    /// <summary>
    /// 定义一条完整的推理链配置
    /// </summary>
    [CreateAssetMenu(fileName = "Chain_New", menuName = "ClueBoardSystem/Clue/DeductionChain")]
    public class DeductionChainSO : ScriptableObject
    {
        [Header("基础配置")]
        public string ChainID; // 唯一标识，例如 "Red_Murder_Chain"

        [Tooltip("这条链上所有线索激活后的统一样式颜色")]
        public Color ChainColor = Color.red;

        [Tooltip("这条链上所有线索激活后的统一样式颜色")]
        public Color InkColor = Color.black;

        [Header("拓扑结构")]
        [Tooltip("核心顺序列表。系统将按照此顺序生成连线：Index 0 -> 1 -> 2 ...")]
        public List<ClueSO> ClueChainSequence = new List<ClueSO>();

        [Tooltip("链条完成后的奖励：终点线索 (需在 ClueSO 中设置为 Destination 类型)")]
        public ClueSO DestinationClue;

        [Header("连线视觉样式")]
        public Sprite LineSprite;

        [Tooltip("连线的宽度")]
        public float LineWidth = 10f;
    }
}
