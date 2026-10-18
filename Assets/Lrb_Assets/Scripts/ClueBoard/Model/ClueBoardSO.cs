using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace ClueBoard.Model
{
    [CreateAssetMenu(fileName = "ClueBoardSO", menuName = "ClueBoardSystem/Clue/ClueBoardSO")]
    public class ClueBoardSO : ScriptableObject
    {
        /// <summary>
        /// ����������Ԥ���б�
        /// </summary>
        public List<Clue> cluesInBoard;
        /// <summary>
        /// ������������Ԥ���б�
        /// </summary>
        public List<DeductionChainSO> allChains;
        /// <summary>
        /// ClueBoardController���ĸ��¼���������״̬�����仯ʱ��ClueBoardSO��ͨ������¼�֪ͨUI��ˢ����ʾ
        /// </summary>
        public event Action<Dictionary<int, Clue>> OnClueBoardUpdated;
        /// <summary>
        /// �Ѳ��Ŷ���������
        /// </summary>
        private HashSet<string> m_animatedChains = new HashSet<string>();

        // [NonSerialized] ȷ�������ᱻ Unity ���л����棬ÿ��������Ϸ�����±���ű�ʱ���������?false
        [NonSerialized]
        private bool m_isInitialized = false;

        public List<ClueSaveData> saveDataList;

        public void Initialize()
        {
            if (m_isInitialized) return;

            ResetToDefaultState();

            m_isInitialized = true;
            Debug.LogWarning("[ClueBoardSO] ִ���״γ�ʼ�� (ResetToDefault)");
        }

        /// <summary>
        /// ��������������������
        /// </summary>
        private void ResetToDefaultState()
        {
            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                if (!cluesInBoard[i].IsEmpty)
                {
                    Clue resetClue = cluesInBoard[i];

                    // 1. ͬ�� ID (��֤�浵�� ID ����)
                    // ��ʹ�� Reset��ҲҪ��֤ ID �������?
                    if (resetClue.clueSO != null)
                    {
                        resetClue.clueID = resetClue.clueSO.ClueLink;
                    }

                    // 2. ״̬���� (����������б�?
                    resetClue.isCollected = false;
                    resetClue.isRead = false;
                    resetClue.runtimePos = Vector3.zero;

                    cluesInBoard[i] = resetClue;
                }
            }
            // ��������״̬
            m_animatedChains.Clear();
        }

        #region �°�浵�߼�?(State Overlay / ״̬����)

        /// <summary>
        /// ������ֻ���?���ռ�"������
        /// </summary>
        public List<ClueSaveData> ExportSaveData()
        {
            saveDataList = new List<ClueSaveData>();

            foreach (var clue in cluesInBoard)
            {
                if (clue.IsEmpty) continue;

                // ֻ�������ռ���
                if (clue.isCollected)
                {
                    saveDataList.Add(new ClueSaveData(clue.clueID, clue.isRead, clue.runtimePos));
                }
            }
            return saveDataList;
        }

        /// <summary>
        /// ���룺�����ã��ٵ��Ӵ浵״̬
        /// </summary>
        public void ImportSaveData(List<ClueSaveData> savedClues, List<string> finishedChains)
        {
            Debug.Log("[ClueBoardSO] ��ʼ����浵����?(״̬����ģʽ)...");

            // 1. ��ȫ�����ã���ֹ��״̬����
            ResetToDefaultState();

            // 2. �ָ�������
            if (finishedChains != null)
            {
                Debug.Log("[ClueBoardSO] ��ʼ�ָ�������...");
                foreach (var chainID in finishedChains) m_animatedChains.Add(chainID);
            }

            // 3. �ָ�����״̬ (Apply Save Data)
            Debug.Log("[ClueBoardSO] ���жϴ����������?savedClues �Ƿ�Ϊ��...");
            if (savedClues != null)
            {
                Debug.Log("[ClueBoardSO] ���жϴ����������?savedClues ��Ϊ�գ�����");
                foreach (var save in savedClues)
                {
                    Debug.Log("[ClueBoardSO] ����ÿһ�� save ...");
                    // �����߼������Ŵ浵���?ID��ȥ cluesInBoard ���Ҷ�Ӧ������
                    int index = FindClueIndexByID(save.ID);

                    if (index != -1)
                    {
                        Debug.Log("[ClueBoardSO] ƥ��ɹ������洢��״̬Clue���ӵ���ǰ�ģ�����");
                        // �ҵ����޸�״̬ (����)
                        Clue target = cluesInBoard[index];

                        target.isCollected = true; // ���
                        target.isRead = save.isRead;
                        target.runtimePos = save.Position;

                        // ���ǻ�ȥ (Struct ��ֵ����)
                        cluesInBoard[index] = target;
                    }
                    else
                    {
                        // �Ҳ�������ӡ����
                        Debug.LogWarning($"[ClueBoardSO] �浵�а��� ID Ϊ {save.ID} ������������ǰ�����б����Ҳ����������ǰ汾����ɾ���˸��������á�");
                    }
                }
            }

            // ֪ͨ UI ˢ��
            // ���Ϊ�ѳ�ʼ������ֹ���� Initialize() �ٴ����ö������״̬
            m_isInitialized = true;
            InformAboutChange();
        }

        // �������ң����� ID ��Ԥ���б���������
        private int FindClueIndexByID(string id)
        {
            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                if (cluesInBoard[i].IsEmpty) continue;

                // ������ clueID������ clueSO.ClueLink
                string currentLink = !string.IsNullOrEmpty(cluesInBoard[i].clueID)
                    ? cluesInBoard[i].clueID
                    : cluesInBoard[i].clueSO.ClueLink;

                //Todo:currentLink ?
                Debug.Log($"{currentLink}");
                
                if (currentLink == id) return i;
            }
            return -1;
        }

        // --- ���ݾɽӿ� ---
        public List<string> GetFinishedChains() => m_animatedChains.ToList();

        #endregion

        #region ��������

        public Dictionary<int, Clue> GetClueBoardState()
        {
            Dictionary<int, Clue> clueBoardState = new Dictionary<int, Clue>();
            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                // ֻ�зǿ������ռ��������Żᱻ UI ��ʾ
                if (cluesInBoard[i].IsEmpty || !cluesInBoard[i].isCollected)
                {
                    continue;
                }
                clueBoardState[i] = cluesInBoard[i];
            }
            return clueBoardState;
        }

        /// <summary>
        /// ��ѯĳ�� Link ��Ӧ�������Ƿ��Ѿ����ռ�/����
        /// </summary>
        public bool IsClueCollected(string clueLink)
        {
            foreach (var clue in cluesInBoard)
            {
                if (clue.IsEmpty) continue;

                // �ҵ���Ӧ�� Link
                if (clue.clueSO.ClueLink == clueLink)
                {
                    // ����������ʱ״̬
                    return clue.isCollected;
                }
            }
            return false;
        }

        /// <summary>
        /// ��ѯĳ�� Link ��Ӧ�������Ƿ��Ѿ����Ķ�/���?
        /// </summary>
        public bool IsClueRead(string clueLink)
        {
            foreach(var clue in cluesInBoard)
            {
                if (clue.IsEmpty) continue;

                if (clue.clueSO.ClueLink == clueLink)
                {
                    return clue.isRead;
                }
            }
            return false;
        }

        public bool IsAllClueRead()
        {
            // CORE��ֻҪ��һ������û���Ķ����ͷ��� false��ֻ��ȫ���ǿ����������Ķ��ˣ��ŷ��� true
            foreach (var clue in cluesInBoard)
            {
                if (clue.IsEmpty) continue;
                if (!clue.isRead) return false;
            }
            return true;
        }


        /// <summary>
        /// ���������Ϊ�Ѷ���֪ͨUI�����䲻��ɫ��UI����
        /// </summary>
        /// <param name="index"></param>
        public void SetClueAsRead(int index)
        {
            if (index < 0 || index >= cluesInBoard.Count) return;

            Clue readClue = cluesInBoard[index];
            if (readClue.IsEmpty || !readClue.isCollected) return;
            if (readClue.isRead) return;//����Ѿ�����Ķ�������ִ�к���ķ���?

            Debug.Log("�ɹ���UI���ֱ�ɫ");
            readClue.isRead = true;
            cluesInBoard[index] = readClue;
            InformAboutChange();
        }

        /// <summary>
        /// ���ķ����������������Ӽ�������
        /// </summary>
        /// <param name="clueLink">����Link</param>
        /// <param name="worldPos">���ֵ�λ��</param>
        /// <returns>�Ƿ�ɹ�����?/returns>
        public bool TryCollectClue(string clueLink, Vector3 worldPos)
        {
            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                if (cluesInBoard[i].IsEmpty) continue;

                // �ҵ��˶�Ӧ����������
                if (cluesInBoard[i].clueSO.ClueLink == clueLink)
                {
                    // ����Ѿ��ռ����ˣ�ֱ�ӷ���?
                    if (cluesInBoard[i].isCollected)
                    {
                        Debug.Log($"���� {clueLink} �Ѽ��������");
                        return false;
                    }

                    // Struct ��ֵ�������޸ĺ���Ҫ��ֵ��ȥ
                    Clue updatedClue = cluesInBoard[i];

                    updatedClue.isCollected = true;          // ����״̬
                    updatedClue.runtimePos = updatedClue.clueSO.StaticWorldPos;

                    // �ٴ�ȷ�� ID ͬ�� (��ֹ��Щ�������û��?Initialize)
                    if (string.IsNullOrEmpty(updatedClue.clueID))
                    {
                        updatedClue.clueID = updatedClue.clueSO.ClueLink;
                    }

                    cluesInBoard[i] = updatedClue; // д���б�

                    // ֪ͨ UI ����
                    InformAboutChange();
                    return true;
                }
            }

            Debug.LogWarning($"δ�� ClueBoardSO �б����ҵ�����Ϊ {clueLink} ���������������á�");
            return false;
        }

        //��ȡ��������ı���Ӧ�������������������?
        public Color GetClueReasonedColor(string clueLink)
        {
            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                if (cluesInBoard[i].IsEmpty) continue;

                // �ҵ��˶�Ӧ����������
                if (cluesInBoard[i].clueSO.ClueLink == clueLink)
                {
                    Clue clueTemp = cluesInBoard[i];
                    return clueTemp.clueSO.ReasonedColor;
                }
            }

            Debug.LogWarning($"δ�� ClueBoardSO �б����ҵ�����Ϊ {clueLink} ���������������á�");
            return Color.white;
        }

        /// <summary>
        /// ֪ͨ UI ��������״̬�����˱仯�����ݵ�ǰ������״̬�ֵ�
        /// </summary>
        [ContextMenu("���Լ����֪ͨUI������������")]
        private void InformAboutChange()
        {
            OnClueBoardUpdated?.Invoke(GetClueBoardState());
        }

        [ContextMenu("测试/一键激活已配置线索并刷新UI")]
        private void DebugCollectAllConfiguredClues()
        {
            int activatedCount = 0;
            int alreadyCollectedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < cluesInBoard.Count; i++)
            {
                Clue clue = cluesInBoard[i];

                // 跳过空槽�?
                if (clue.IsEmpty || clue.clueSO == null)
                {
                    skippedCount++;
                    continue;
                }

                // 确保 ID 始终同步
                if (string.IsNullOrEmpty(clue.clueID))
                {
                    clue.clueID = clue.clueSO.ClueLink;
                }

                if (clue.isCollected)
                {
                    alreadyCollectedCount++;
                    cluesInBoard[i] = clue;
                    continue;
                }

                // 一键激活：统一使用 ClueSO.StaticWorldPos
                clue.isCollected = true;
                clue.runtimePos = clue.clueSO.StaticWorldPos;

                cluesInBoard[i] = clue;
                activatedCount++;
            }

            // 通知 UI 层刷�?
            InformAboutChange();

            Debug.Log($"[ClueBoardSO] 一键激活完成。新增激�?{activatedCount}，已激�?{alreadyCollectedCount}，跳过空�?{skippedCount}");
        }


        #endregion

        #region ������

        /// <summary>
        /// ���ո��ռ��������Ƿ񴥷���ĳ�������������?
        /// </summary>
        /// <param name="triggerClue">�ո��ռ���������</param>
        /// <returns>��������������δ���Ŷ��������ض�Ӧ�������ã����򷵻� null</returns>
        public DeductionChainSO CheckChainCompletion(ClueSO triggerClue)
        {
            // 1. �����������õ�����
            foreach (var chain in allChains)
            {
                // �Ż�������������Ѿ�����Ҳ��������ˣ����� (�����ظ�����)
                if (m_animatedChains.Contains(chain.ChainID)) continue;

                // 2. �������������Ƿ�����������
                // (����б��ܳ���������?HashSet �Ż� DeductionChainSO �Ĳ��ң���һ�����������࣬List.Contains ����)
                if (chain.ClueChainSequence.Contains(triggerClue))
                {
                    // 3. ���ļ�飺�������ϵ������������ռ�����?
                    if (IsEntireChainRead(chain))
                    {
                        return chain; // �ҵ��ˣ����ظ� Controller ȥ������
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// �������������ĳ���������������Ƿ���?cluesInBoard �б�����?isRead
        /// </summary>
        private bool IsEntireChainRead(DeductionChainSO chain)
        {
            foreach (ClueSO requiredClue in chain.ClueChainSequence)
            {
                // �������еĲ�ѯ����
                // ע�⣺����Ч���Ե� (O(N*M))������������У����齨��?ID->Index �Ļ����ֵ�
                if (!IsClueRead(requiredClue.ClueLink))
                {
                    return false; // ֻҪ��һ��û�ռ�����û���?
                }
            }
            return true;
        }

        /// <summary>
        /// ���������������� (���ڴ浵������ʱ�ľ�̬��ʾ)
        /// </summary>
        public void MarkChainAsAnimated(DeductionChainSO chain)
        {
            if (!m_animatedChains.Contains(chain.ChainID))
            {
                m_animatedChains.Add(chain.ChainID);
            }
        }

        /// <summary>
        /// ��ѯĳ�����Ƿ��Ѿ�����"�������չ�?��״̬ (View ����ô˷����������Ƿ񻭾�̬��?
        /// </summary>
        public bool IsChainFinishedAndAnimated(DeductionChainSO chain)
        {
            return m_animatedChains.Contains(chain.ChainID);
        }

        /// <summary>
        /// ��ѯĳ�����Ƿ��Ѿ�����"�������չ�?��״̬��ͨ�� ChainID ��ѯ����
        /// �� ClueBoardController ������ϵͳͨ�� CBC ֱ�ӵ��ã������ж�ĳ���������Ƿ�����ɲ�չʾ��̬���ߡ�?
        /// ���� true ��ʾ��������ɲ���չʾ��false ��ʾδ��ɻ�����Ƿ���
        /// </summary>
        public bool IsChainFinishedAndAnimated(string chainID)
        {
            if (string.IsNullOrWhiteSpace(chainID)) return false;

            if(m_animatedChains != null && m_animatedChains.Contains(chainID))
            {
                Debug.Log("������ " + chainID);

                return m_animatedChains != null && m_animatedChains.Contains(chainID);
            }

            return false;
        }

        #endregion
    }






    //Clue�Ƕ�̬�������ݽṹ
    /// <summary>
    /// �����������������Ķ�̬�������ݵ�λ
    /// </summary>
    [Serializable]
    public struct Clue//Todo������������ɫ�����ߡ����Ŷ����ȵ�
    {
        [JsonIgnore]
        public ClueSO clueSO;

        /// <summary>
        /// �ṹ��ID  �洢  ClueSO��ClueLink
        /// </summary>
        public string clueID;

        public bool isCollected;//���ȴ������������ϣ����Ǻ����ռ����ı�True
        public bool isRead;
        public Vector3 runtimePos;

        public bool IsEmpty => clueSO == null;

        // ��ȡ��ɫ��View��ֱ�ӵ���������ԣ������Լ�дif/else
        /// <summary>
        /// ��ȷ���Ƿ���Ҫ�޸���ɫ��������
        /// </summary>
        public Color CurrentColor => isRead ? clueSO.ReasonedColor : clueSO.DefaultColor;

        public static Clue GetNullClue()
            => new Clue
            {
                clueSO = null,
                clueID = string.Empty,
                isCollected = false
            };
    }

    /// <summary>
    /// �����浵ר�����ݽṹ (DTO)
    /// ���� JSON ���л���ֻ�洢��Ҫ��״̬��Ϣ�������� SO ����
    /// </summary>
    [Serializable]
    public struct ClueSaveData
    {
        public string ID;        // ����Ψһ��ʶ (ClueLink)
        public bool isRead;      // �Ƿ��Ѷ�
        public Vector3 Position; // �����ڰ����ϵ�����ʱλ��

        // ���캯��
        public ClueSaveData(string id, bool read, Vector3 pos)
        {
            ID = id;
            isRead = read;
            Position = pos;
        }
    }
}

