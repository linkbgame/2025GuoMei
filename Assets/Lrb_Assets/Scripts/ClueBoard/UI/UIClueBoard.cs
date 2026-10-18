using ClueBoard.Model;
using System;
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.EventSystems;


namespace ClueBoard.UI
{
    public class UIClueBoard : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
    {
        [Header("核心引用")]
        [SerializeField] private MiniMapCamera miniMapCamera;
        [SerializeField] private RectTransform mapContentRect;
        [SerializeField] private RectTransform lineAndInkContainer;// 红线和墨水存放的位置，由于实时生成的墨水绽开效果在Hierarchy的最下方，那么它永远被渲染在最上方

        [Header("交互设置")]
        [SerializeField] private bool isAnimating = false;
        public float openAnimationDuration = 0.5f;
        public float closeAnimationDuration = 0.5f;
        [SerializeField, Range(0f, 100f)] private float zoomSpeed = 2f;
        [SerializeField, Range(0f, 2f)] private float panSpeed = 1f;

        [Header("线索资产")]
        [SerializeField] private GameObject clueNodePrefab;
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private GameObject inkAnimationPrefab;

        [Header("绘线动画设置")]
        [SerializeField] private float drawSpeed = 500f; // 每秒绘制的像素长度
        [SerializeField] private AnimationCurve drawCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float nodePauseDuration = 0.8f; // 每个节点处的停顿时间
        [SerializeField] private float destPauseDuration = 1.5f; // 终点处的停顿时间
        [SerializeField] private float inkSpreadDuration = 1f; // 终点处的水墨动画生命周期时间



        [HideInInspector] public bool isOpen;

        private Animator m_animator;

        /// <summary>
        /// Key: clueSO.ID, Value: UIClueNode 实例
        /// </summary>
        private Dictionary<int, UIClueNode> m_clueNodesDict = new Dictionary<int, UIClueNode>();
        private List<GameObject> m_activeLines = new List<GameObject>();
        public event Action<int> OnClueNodeRequest;


        /// <summary>
        /// 用 int ID 来获得 线索节点UI 对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public UIClueNode GetNodeByID(int id)
        {
            if (m_clueNodesDict.TryGetValue(id, out UIClueNode node))
            {
                return node;
            }
            return null;
        }

        // UI 开启时的状态重置
        private void OnEnable()
        {
            if (miniMapCamera == null || mapContentRect == null || lineAndInkContainer == null)
            {
                Debug.LogError("核心引用没有设置好！");
                return;
            }

            //重置缩放
            miniMapCamera.ResetZoomToDefault();
            //重置位置
            miniMapCamera.StopManualPanAndStartReturn();
        }

        private void Awake()
        {
            // 开始默认将线索板禁用
            gameObject.SetActive(false);
            isOpen = false;   
        }

        private void Start()
        {
            miniMapCamera.ForceResetToPlayer();
            m_animator = GetComponent<Animator>();
        }



        #region 小地图功能
        /// <summary>
        /// 按下鼠标，相机进入手动拖拽模式
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (miniMapCamera == null || isAnimating == true) return;
            
            miniMapCamera.StartManualPan();
        }

        /// <summary>
        /// 持续拖拽计算世界空间的拖拽位移向量，再让相机根据向量位移
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if (miniMapCamera == null || isAnimating == true) return;

            Vector3 panVector = CalculateWorldPanVector(eventData);
            miniMapCamera.Pan(panVector);
        }

        /// <summary>
        /// 鼠标弹起结束手动拖拽，并通知相机开始平滑回归
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (miniMapCamera == null || isAnimating == true) return;

            miniMapCamera.StopManualPanAndStartReturn();
        }

        // --- 缩放 (Zoom) 逻辑 ---
        /// <summary>
        /// 接受鼠标滚轮输入，计算缩放增量，并调用相机的缩放方法
        /// </summary>
        /// <param name="eventData"></param>
        public void OnScroll(PointerEventData eventData)
        {
            if (miniMapCamera == null || isAnimating == true) return;

            float zoomAmount = eventData.scrollDelta.y * zoomSpeed * Time.unscaledDeltaTime;
            miniMapCamera.Zoom(zoomAmount);
        }

        // --- 辅助方法 ---

        /// <summary>
        /// 核心：将屏幕空间的拖拽增量转换为世界空间的位移向量。
        /// </summary>
        private Vector3 CalculateWorldPanVector(PointerEventData eventData)
        {
            // 优先使用 RawImage(RectTransform) 的本地坐标差值来计算比例 —— 这样不会受 RenderTexture 大小或 Canvas Scaler 的影响
            if (mapContentRect == null || miniMapCamera == null)
                return Vector3.zero;

            Camera uiCamera = eventData.pressEventCamera;
            RectTransform rect = mapContentRect;

            // 把当前点击位置和上一个位置都映射到 RawImage 的UI坐标
            Vector2 localCurr;
            Vector2 localPrev;
            bool okCurr = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, uiCamera, out localCurr);
            bool okPrev = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position - eventData.delta, uiCamera, out localPrev);

            Vector2 localDelta;
            if (okCurr && okPrev)
            {
                localDelta = localCurr - localPrev;// UI
            }
            else
            {
                localDelta = eventData.delta;// 屏幕
            }

            // TODO: 和转化为视口坐标有什么区别
            // 归一化到 rect 尺寸 -> [-0.5,0.5] 区间的相对位移比例
            Vector2 rectSize = rect.rect.size;
            if (rectSize.x <= 0f || rectSize.y <= 0f) return Vector3.zero;
            Vector2 normalizedDelta = new Vector2(localDelta.x / rectSize.x, localDelta.y / rectSize.y) * panSpeed;

            // 将归一化的 UI 增量转换为相机在世界空间的尺寸（正交相机）
            Camera cam = miniMapCamera.GetComponent<Camera>();
            if (cam == null) return Vector3.zero;

            // 计算相机在目标平面上的世界宽高（正交）
            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;

            // 把 UI 比例映射到世界位移（注意方向与原实现保持一致：拖拽方向与摄像机平移相反）
            Vector3 panWorld = new Vector3(-normalizedDelta.x * worldWidth, -normalizedDelta.y * worldHeight, 0f);

            return panWorld;
        }



        #endregion

        #region 线索节点和连线渲染

        // ----------------------------------------------------------------
        //  数据驱动核心：渲染逻辑
        // ----------------------------------------------------------------
        public Camera GetMiniMapCamera()
        {
            if (miniMapCamera == null) return null;

            // 兼容场景切换后 Camera 引用失效
            if (miniMapCamera.miniCamera == null)
            {
                miniMapCamera.miniCamera = miniMapCamera.GetComponent<Camera>();
            }

            return miniMapCamera.miniCamera;
        }
        public void RenderClues(Dictionary<int, Clue> clueState)
        {
            // 策略：我们不暴力销毁，而是采用“同步”策略，或者简单地清空重建
            // 考虑到连线系统，如果节点被销毁，连线也会断。
            // 但为了简化当前逻辑，我们先只做增量更新（已有节点更新状态，新节点创建）
            // 如果要支持删除线索，需要额外的 Diff 算法，暂时假设线索只会增加不会减少。

            // 1. 遍历最新的数据状态
            foreach (var kvp in clueState)
            {
                int indexInList = kvp.Key; // 这里的 Key 是 List 中的索引，建议后续重构为 ClueSO.ID 作为 Key 会更稳健
                Clue clue = kvp.Value;

                if (clue.IsEmpty || !clue.isCollected) continue;

                // 使用 ClueSO 的 ID 作为字典 Key，因为它是全局唯一的
                int clueID = clue.clueSO.ID;

                // 2. 检查节点是否已存在
                if (m_clueNodesDict.TryGetValue(clueID, out UIClueNode existingNode))
                {
                    // A. 如果存在：仅刷新视觉 (变色)
                    existingNode.UpdateClueState(clue);
                }
                else
                {
                    // B. 如果不存在：创建新节点并初始化
                    CreateClueNode(clueID, clue, indexInList);
                }
            }

            // Todo: 如果未来支持移除线索，需要反向遍历 _clueNodesDict 移除不在 clueState 里的节点
        }

        private void CreateClueNode(int clueID, Clue clue, int indexInList)
        {
            if (clueNodePrefab == null) return;

            GameObject newClueObj = Instantiate(clueNodePrefab, mapContentRect);
            newClueObj.name = $"Clue_{clue.clueSO.ClueLink}";

            UIClueNode node = newClueObj.GetComponent<UIClueNode>();
            if (node != null)
            {
                // 1. 物理/逻辑初始化 (位置、引用)
                Camera miniCam = GetMiniMapCamera();
                node.Initialize(clue.runtimePos, miniCam, mapContentRect);

                // 2. 视觉初始化 (颜色、图标)
                node.UpdateClueState(clue);

                // 3. 事件绑定
                node.OnClueNodeClicked += (n) => HandlerClueNodeClicked(n, indexInList);

                // 4. 加入字典管理
                m_clueNodesDict.Add(clueID, node);
            }
        }

        /// <summary>
        /// 修改事件回调，传递 List 索引给 Controller
        /// UI层 播放点击动画，Controller层 处理数据请求和界面跳转，两者职责分离
        /// </summary>
        /// <param name="clueNodeUI"></param>
        /// <param name="indexInList"></param>
        private void HandlerClueNodeClicked(UIClueNode clueNodeUI, int indexInList)
        {
            OnClueNodeRequest?.Invoke(indexInList);

            var rectT = clueNodeUI.GetComponent<RectTransform>();
            SpawnInkAnimation(rectT.anchoredPosition, 2f);
        }

        public void ClearAllClues()
        {
            foreach (var node in m_clueNodesDict.Values)
            {
                if (node != null) Destroy(node.gameObject);
            }
            m_clueNodesDict.Clear();
        }


        







        /// <summary>
        /// 渲染所有已完成的推理链连线 (静态)
        /// </summary>
        public void RenderLines(ClueBoardSO boardData)
        {
            // 清理旧线
            ClearAllLines();

            // 遍历所有链条配置
            foreach (var chain in boardData.allChains)
            {
                // 只有当链条既完成了，又播放过动画了，才画静态线
                if (boardData.IsChainFinishedAndAnimated(chain))
                {
                    DrawStaticChain(chain);
                }
            }
        }

        /// <summary>
        /// 画一条已完成的静态推理链
        /// </summary>
        /// <param name="chain"></param>
        private void DrawStaticChain(DeductionChainSO chain)
        {
            // 遍历链条中的节点：0-1, 1-2, 2-3...
            for (int i = 0; i < chain.ClueChainSequence.Count - 1; i++)
            {
                ClueSO startClue = chain.ClueChainSequence[i];
                ClueSO endClue = chain.ClueChainSequence[i + 1];

                DrawLineBetweenNodes(startClue.ID, endClue.ID, chain);
            }

            // 画终点引线 (如果配置了终点)
            if (chain.DestinationClue != null)
            {
                ClueSO lastClue = chain.ClueChainSequence[chain.ClueChainSequence.Count - 1];
                DrawLineBetweenNodes(lastClue.ID, chain.DestinationClue.ID, chain);
            }
        }

        /// <summary>
        /// 辅助方法：在两点之间生成连线物体
        /// </summary>
        private void DrawLineBetweenNodes(int startID, int endID, DeductionChainSO chainStyle)
        {
            // 尝试从字典获取两个 UI 节点
            if (m_clueNodesDict.TryGetValue(startID, out UIClueNode nodeA) &&
                m_clueNodesDict.TryGetValue(endID, out UIClueNode nodeB))
            {
                // 【修改点】不再这里计算坐标，而是直接传入节点引用
                CreateLineObject(nodeA, nodeB, chainStyle);
            }
        }

        /// <summary>
        /// 打开关闭线索板时，要生成已完成线索词条的静态连线物体的方法
        /// </summary>
        private void CreateLineObject(UIClueNode nodeA, UIClueNode nodeB, DeductionChainSO style)
        {
            if (linePrefab == null) return;

            GameObject lineObj = Instantiate(linePrefab, lineAndInkContainer);
            lineObj.transform.SetAsFirstSibling();

            UILineConnector connector = lineObj.GetComponent<UILineConnector>();
            if (connector != null)
            {
                // 传入 RectTransform，让线条自己去 LateUpdate 里跟随
                connector.ConnectDynamic(
                    nodeA.ConnectRectTransform,
                    nodeB.ConnectRectTransform,
                    lineAndInkContainer,
                    style.LineWidth,
                    style.LineSprite
                );
            }

            m_activeLines.Add(lineObj);
        }

        private void ClearAllLines()
        {
            foreach (var line in m_activeLines)
            {
                if (line != null) Destroy(line);
            }
            m_activeLines.Clear();
        }

        #endregion

        #region 连线动画

        // ----------------------------------------------------------------
        //  动画逻辑
        // ----------------------------------------------------------------

        /// <summary>
        /// 核心：播放连线动画
        /// TODO：终点事件如果想要实现不激活但连线，同时画圈，该怎么办
        /// </summary>
        public IEnumerator PlayChainAnimation(DeductionChainSO chain, Action onDestinationTrigger, Action onAllComplete)
        {
            /*// 调度相机连线动画
            UICustomCursor.Instance.SetBrushActive(true);
            if (m_clueNodesDict.TryGetValue(chain.ClueChainSequence[0].ID, out var firstNode))
            {
                miniMapCamera.StartFollowBrush(firstNode.TargetWorldPos);
            }*/

            // 绘制普通链条段
            for (int i = 0; i < chain.ClueChainSequence.Count - 1; i++)
            {
                yield return StartCoroutine(DrawSingleSegment(
                    chain.ClueChainSequence[i].ID, 
                    chain.ClueChainSequence[i+1].ID, 
                    chain, false));
            }

            // 绘制最后一段（向终点延伸）
            if (chain.DestinationClue != null)
            {
                // 先通知 Controller 激活终点数据
                onDestinationTrigger?.Invoke();

                // 关键点：等待一帧，并循环检查字典，直到终点 UI 节点被 RenderClues 实例化出来
                yield return new WaitUntil(() => m_clueNodesDict.ContainsKey(chain.DestinationClue.ID));
                
                var destNode = m_clueNodesDict[chain.DestinationClue.ID];

                // 先隐藏终点节点，等线画完再显示
                destNode.SetVisibility(false); 

                yield return StartCoroutine(DrawSingleSegment(
                    chain.ClueChainSequence[chain.ClueChainSequence.Count - 1].ID,
                    chain.DestinationClue.ID,
                    chain, true));
                destNode.SetVisibility(true);
            }

            onAllComplete?.Invoke();
        }

        /// <summary>
        /// 动态绘制两点之间的单段连线动画
        /// </summary>
        /// <param name="startID"></param>
        /// <param name="endID"></param>
        /// <param name="style"></param>
        /// <param name="isDest"></param>
        /// <returns></returns>
        private IEnumerator DrawSingleSegment(int startID, int endID, DeductionChainSO style, bool isDest)
        {
            // 获取UI节点
            if (!m_clueNodesDict.TryGetValue(startID, out var nodeA) ||
                !m_clueNodesDict.TryGetValue(endID, out var nodeB)) yield break;

            isAnimating = true;

            // 实例化连线并初始化
            GameObject lineObj = Instantiate(linePrefab, lineAndInkContainer);
            lineObj.transform.SetAsFirstSibling();

            var connector = lineObj.GetComponent<UILineConnector>();
            var rectA = nodeA.ConnectRectTransform;
            var rectB = nodeB.ConnectRectTransform;

            Vector3 startPos = nodeA.TargetWorldPos;

            ///-------///

            // 初始状态：进度为 0，防止闪烁
            connector.ConnectDynamic(rectA, rectB, lineAndInkContainer, style.LineWidth, style.LineSprite);
            connector.SetGrowth(0f);

            m_activeLines.Add(lineObj);         
            yield return null;// 等待一帧确保 UI 布局计算完成

            // 统一坐标系：将两点的 RectTransform 位置转换到 lineContainer 的本地坐标系下，计算总距离
            Vector2 startLocal = GetPointInLineContainer(rectA);
            Vector2 endLocal = GetPointInLineContainer(rectB);
            float totalDist = Vector2.Distance(startLocal, endLocal);// 总距离

            // 距离过短时直接跳过动画，避免闪烁和不必要的计算
            if (totalDist < 0.1f)
            {
                connector.SetGrowth(1f);
                yield break;
            }

            float duration = totalDist / Mathf.Max(1f, drawSpeed);// 总时长
            float elapsed = 0;

            // 动画循环
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = drawCurve.Evaluate(t); // 使用配置的动画曲线

                // 更新生长进度
                connector.SetGrowth(t);
                yield return null;
            }

            connector.SetGrowth(1f);

            ///-------///

            // 视听效果
            //SpawnInkShader(rectB.anchoredPosition);
            SpawnInkAnimation(endLocal, 2f);//TODO: 这里的缩放可以根据需求调整
            ClueBoardController.Instance.PlayConnectSound(isDest);

            // 不同节点不同停顿的处理
            if (isDest)
            {
                var destNode = m_clueNodesDict[style.DestinationClue.ID];

                // 先隐藏终点节点，等线画完再显示
                destNode.SetVisibility(true);

                yield return new WaitForSeconds(destPauseDuration);

                isAnimating = false;

                yield break;
            }

            yield return new WaitForSeconds(nodePauseDuration);

            isAnimating = false;
        }

        private Vector2 GetPointInLineContainer(RectTransform source)
        {
            Canvas canvas = lineAndInkContainer.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, source.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lineAndInkContainer,
                screenPoint,
                cam,
                out Vector2 localPoint
            );
            return localPoint;
        }

       
        /// <summary>
        /// 墨水绽开动画：在指定位置实例化墨水绽开动画预制体，并设置缩放
        /// </summary>

        private void SpawnInkAnimation(Vector2 localPos, float animScale)
        {
            if (inkAnimationPrefab == null || lineAndInkContainer == null) return;

            //实例化
            GameObject ink = Instantiate(inkAnimationPrefab, lineAndInkContainer);

            //设置位置
            RectTransform inkRect = ink.GetComponent<RectTransform>();
            if (inkRect != null)
            {
                inkRect.anchoredPosition = localPos;
            }

            //设置缩放
            inkRect.localScale = Vector3.one * animScale;

            StartCoroutine(DestroyInkTask(ink, inkSpreadDuration));
        }
        

        /// <summary>
        /// 延迟销毁特效对象的任务协程
        /// </summary>
        private IEnumerator DestroyInkTask(GameObject obj, float waitToDestory)
        {
            yield return new WaitForSeconds(waitToDestory);
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        #endregion

        public void AnimateOpen()
        {
            if (m_animator != null)
            {
                m_animator.SetTrigger("Open");
            }

            var rectUIClueBoard = GetComponent<RectTransform>();
            var inkPos = rectUIClueBoard.anchoredPosition + new Vector2(mapContentRect.rect.width / 2, mapContentRect.rect.height / 2);
            SpawnInkAnimation(inkPos, 5f);
        }

        public void AnimateClose()
        {
            if (m_animator != null)
            {
                m_animator.SetTrigger("Close");
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            isOpen = false;

            miniMapCamera.ForceResetToPlayer();
            SetMiniCam(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            isOpen = true;

            miniMapCamera.ForceResetToPlayer();
            SetMiniCam(true);

            // TODO：播放音效
        }

        public void SetMiniCam(bool state)
        {
            miniMapCamera.gameObject.SetActive(state);
        }
    }
}