using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClueBoard.Model
{
    [CreateAssetMenu(fileName = "ClueDatabaseSO", menuName = "ClueBoardSystem/Clue/ClueDatabaseSO")]
    public class ClueDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ClueSO> allClues;
        public List<ClueSO> GetAllClues() => allClues;
        /// <summary>
        /// 以ClueLink作为字符串，映射到对应的ClueSO对象
        /// </summary>
        [SerializeField] private Dictionary<string, ClueSO> clueLookup;

        public void Init()
        {
            clueLookup = new Dictionary<string, ClueSO>();
            foreach (var clue in allClues)
            {
                if (clue != null && !string.IsNullOrEmpty(clue.ClueLink))
                {
                    if (!clueLookup.ContainsKey(clue.ClueLink))
                    {
                        clueLookup.Add(clue.ClueLink, clue);
                        //Debug.Log($"[线索数据库] 成功添加线索，线索链接为: {clue.ClueLink}");
                    }
                    else
                    {
                        Debug.LogWarning($"[线索数据库] 复制线索链接: {clue.ClueLink}");
                    }
                }
            }
        }

        /// <summary>
        /// 通过字符串ClueLink来获取对应的ClueSO对象
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        public ClueSO GetClueByLink(string link)
        {
            if (clueLookup == null) Init();

            if (clueLookup.TryGetValue(link, out ClueSO clue)) return clue;

            Debug.LogError($"[线索数据库] Link为: {link} 的对象没有找到！！！");
            return null;
        }
    }
}