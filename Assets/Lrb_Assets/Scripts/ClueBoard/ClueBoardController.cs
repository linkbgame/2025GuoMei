using ClueBoard.Model;
using ClueBoard.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClueBoard
{
    /// <summary>
    /// 单例线索板控制器
    /// </summary>
    public class ClueBoardController : MonoBehaviour
    {
        public static ClueBoardController Instance;

        /// <summary>
        /// 线索板UI
        /// </summary>
        public UIClueBoard clueBoardUI;
        /// <summary>
        /// 线索板数据
        /// </summary>
        public ClueBoardSO clueBoardData;
        /// <summary>
        /// 线索数据库
        /// </summary>
        public ClueDatabaseSO clueDatabase;

        // 初始线索列表 
        public List<Clue> initialClues = new List<Clue>();

        [Header("音效资源")]
        public AudioSource audioSource;
        public AudioClip openSound;
        public AudioClip normalClueClickSound;
        public AudioClip drawConclusionSound;
        [SerializeField] private AudioClip normalConnectClip;
        [SerializeField] private AudioClip destinationConnectClip;

        // 全局动画锁，供 UIClueNode 查询
        public bool IsAnimating { get; private set; }

        private PlayerStateManager m_stateManager;
        private bool isAnimTransit = false; // 防止重复点击的锁

        //public bool isFreshStart = true;



        #region 存档系统订阅相关
        private void OnEnable()
        {
            // 1. 订阅“保存”事件：别人要保存时，我把我的数据交出去
            EventManager.Instance.AddEventListener(EventNameEnum.Save, OnSaveGame);

            // 2. 订阅“加载”事件：别人读好档了，我把数据拿回来
            EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, OnLoadGame);
        }

        private void OnDisable()
        {
            // 记得取消订阅，防止报错
            EventManager.Instance.RemoveEventListener(EventNameEnum.Save, OnSaveGame);
            EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, OnLoadGame);
        }

        /// <summary>
        /// 响应保存事件
        /// 遍历筛选 isCollected -> 存入 GameData
        /// (这一步逻辑已封装在 clueBoardData.ExportSaveData 内部)
        /// </summary>
        private void OnSaveGame()
        {
            if (SaveLoadSystem.Instance == null) return;

            var gameData = SaveLoadSystem.Instance.gameData;

            // 1. 导出"已收集"的线索状态 (List<ClueSaveData>)
            gameData.collectedClues = clueBoardData.ExportSaveData();

            // 2. 保存已完成的推理链状态
            gameData.finishedChains = clueBoardData.GetFinishedChains();

            Debug.Log($"[ClueBoardController] 存档同步完毕。已保存线索数: {gameData.collectedClues.Count}");
        }

        /// <summary>
        /// 响应加载事件
        /// 对应您要求的：从 GameData 拿数据 -> 调用 ApplySaveData (这里对应 ImportSaveData)
        /// </summary>
        private void OnLoadGame(GameData loadedData)
        {
            if (loadedData == null) return;

            Debug.Log("[ClueBoardController] 接收到读档事件，开始恢复状态...");

            // 核心逻辑：调用 Import 进行"状态叠加"
            // 方法内部会先 ResetToDefault，然后根据 ID 将 isCollected 设为 true
            clueBoardData.ImportSaveData(loadedData.collectedClues, loadedData.finishedChains);

            // 强制刷新 UI，确保界面显示最新的连线和线索
            UpdateClueBoardUI(clueBoardData.GetClueBoardState());

            // 重新绘制连线 (处理静态线)
            clueBoardUI.RenderLines(clueBoardData);
        }
        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            m_stateManager = GetComponent<PlayerStateManager>();
        }

        private void Start()
        {
            PrepareUI();
            PrepareClueBoardData();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Toggle();
            }
        }

        private void PrepareUI()
        {
            clueBoardUI.Hide();
            clueBoardUI.OnClueNodeRequest += HandleClueNodeRequest;
        }

        private void PrepareClueBoardData()
        {
            if (clueBoardData == null) return;

            clueBoardData.Initialize();
            clueBoardData.OnClueBoardUpdated += UpdateClueBoardUI;

            // C. 激活初始线索 (仅当列表为空/新游戏时才添加，避免覆盖存档)
            // 简单判断：如果 cluesInBoard 为空，否则全为空线索，说明是初始状态
            //bool isFreshStart = true;
            //foreach (var c in clueBoardData.cluesInBoard) { if (!c.IsEmpty) { isFreshStart = false; break; } }

            /*if (isFreshStart)
            {
                Debug.Log("执行添加初始线索的逻辑");
                foreach (Clue clue in initialClues)
                {
                    if (clue.IsEmpty) continue;
                    if (clue.isCollected)
                    {
                        clueBoardData.TryCollectClue(clue.clueSO.ClueLink, clue.runtimePos);
                    }
                }
            }*/

            // D. 强制刷新一次 UI，确保显示当前状态 (防止 initialClues 为空时 UI 不刷新)
            UpdateClueBoardUI(clueBoardData.GetClueBoardState());
        }

        /// <summary>
        /// 数据更新后回调更新UI
        /// </summary>
        /// <param name="clueBoardState"></param>
        private void UpdateClueBoardUI(Dictionary<int, Clue> clueBoardState)
        {
            clueBoardUI.RenderClues(clueBoardState);
        }

        //UIClueNode->UIClueBoard->ClueBoardController，最终点击逻辑在这里被调用
        /// <summary>
        /// 处理 UI 节点的点击请求
        /// </summary>
        /// <param name="clueIndex">数据列表中的索引</param>
        private void HandleClueNodeRequest(int clueIndex)
        {
            if (IsAnimating) return; // 动画期间不响应任何点击

            // 获取当前点击的线索结构体数据
            if (clueIndex < 0 || clueIndex >= clueBoardData.cluesInBoard.Count) return;       
            Clue targetClue = clueBoardData.cluesInBoard[clueIndex];

            // 终点类型播放音效后退出方法
            if (targetClue.clueSO.Type == ClueType.Destination)
            {
                Debug.Log($"点击了终点地点: {targetClue.clueSO.ClueDesc}");
                PlaySound(drawConclusionSound);
                return;
            }
            else
            {
                PlaySound(normalClueClickSound);
            }

            // 设置为已读 (View 会变色)
            clueBoardData.SetClueAsRead(clueIndex);

            // 检查：这次点击是否触发了某条推理链的完成？
            DeductionChainSO completedChain = clueBoardData.CheckChainCompletion(targetClue.clueSO);

            // MARKER：检查clueBoardData里面的所有线索是否都已读，如果是触发Fy事件
            if (clueBoardData.IsAllClueRead())
            {
                SystemEventManager.instance.SomeOneHasDown(5);
            }

            if (completedChain != null)
            {
                Debug.Log($"<color=yellow>推理链完成: {completedChain.ChainID}！开始播放连线动画...</color>");
                StartCoroutine(PlayDeductionSequence(completedChain));
            }
        }

        /// <summary>
        /// 执行完整的推理演出流程
        /// </summary>
        private IEnumerator PlayDeductionSequence(DeductionChainSO chain)
        {
            IsAnimating = true;

            // 彻底禁用整个 Canvas 的交互，防止鼠标悬停检测
            var group = clueBoardUI.GetComponent<CanvasGroup>();
            if (group) group.interactable = false;

            bool isDone = false;
            // TODO:搞懂核心回调：当连线连接到一半，需要激活终点时触发
            StartCoroutine(clueBoardUI.PlayChainAnimation(chain, () => {
                if (chain.DestinationClue != null)
                {
                    // 激活数据层的终点
                    clueBoardData.TryCollectClue(chain.DestinationClue.ClueLink, chain.DestinationClue.StaticWorldPos);
                    Debug.Log("Destination Activated via Callback");
                }
            }, () => isDone = true));

            yield return new WaitUntil(() => isDone);

            // 数据后续处理 (标记完成等)
            clueBoardData.MarkChainAsAnimated(chain);
            clueBoardUI.RenderLines(clueBoardData);

            // 解锁交互
            if (group) group.interactable = true;
            IsAnimating = false; // 解锁
        }











        /// <summary>
        /// 默认线索板是关闭的，按钮调用此方法切换线索板的显示状态
        /// </summary>
        public void Toggle()
        {
            // 如果正在播放动画，不响应新的点击
            if (isAnimTransit) return;

            bool targetOpening = !clueBoardUI.isOpen;

            if (targetOpening)
            {
                clueBoardUI.Show();
                PlaySound(openSound);
                //StartCoroutine(OpenClueBoard());
            }
            else
            {
                clueBoardUI.Hide();
                //StartCoroutine(CloseClueBoard());
            }

        }

        /// <summary>
        /// 开启线索板协程视听流程
        /// </summary>
        /// <returns></returns>
        private IEnumerator OpenClueBoard()
        {
            isAnimTransit = true;
            if (m_stateManager != null) m_stateManager.SetMovementLock(true, this);

            //更新数据，显示UI，播放动画，播放音效，等候时间
            UpdateClueBoardUI(clueBoardData.GetClueBoardState());
            clueBoardUI.Show();
            //clueBoardUI.AnimateOpen();
            PlaySound(openSound);
            yield return new WaitForSecondsRealtime(clueBoardUI.openAnimationDuration);

            isAnimTransit = false;
        }

        /// <summary>
        /// .关闭线索板视听流程
        /// </summary>
        /// <returns></returns>
        private IEnumerator CloseClueBoard()
        {
            isAnimTransit = true;
            if (m_stateManager != null) m_stateManager.SetMovementLock(false, this);

            //播放动画，播放音效，等候时间再隐藏UI
            //clueBoardUI.AnimateClose();
            //PlaySound(closeSound);
            yield return new WaitForSecondsRealtime(clueBoardUI.closeAnimationDuration);
            clueBoardUI.Hide();

            isAnimTransit = false;
        }

        // 供 UI 调用播放音效
        public void PlayConnectSound(bool isDestination)
        {
            AudioClip clip = isDestination ? destinationConnectClip : normalConnectClip;
            if (audioSource && clip) PlaySound(clip);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                Debug.LogWarning("[ClueBoardController] 无法播放音效，请配置 AudioSource 或 AudioClip");
                return;
            }
            audioSource.PlayOneShot(clip);
        }

        //供外部查询是否某个ChainID已经完成
        public bool IsChainFinished(string chainID)
        {
            return clueBoardData.IsChainFinishedAndAnimated(chainID);
        }
    }
}