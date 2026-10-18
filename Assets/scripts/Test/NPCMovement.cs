using System;
using System.Collections;
using System.Collections.Generic;
using MFarm.AStar;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NPCMovement : MonoBehaviour
{
    public ScheduleDataList_SO scheduleData;
    private SortedSet<ScheduleDetails> scheduleSet;
    private ScheduleDetails currentSchedule;

    //��ʱ�洢��Ϣ
    public string currentScene;
    private string targetScene;
    private Vector3Int currentGridPosition;
    private Vector3Int tragetGridPosition;
    private Vector3Int nextGridPosition;
    private Vector3 nextWorldPosition;

    public string StartScene { set => currentScene = value; }

    /// <summary>
    /// 当前是否正在执行 BuildPath 排出来的寻路任务（被 schedule / NpcAttractPoint 等驱动）。
    /// 外部组件（如 NpcScriptedMove）可据此让位，避免与本组件争夺 rb.MovePosition。
    /// </summary>
    public bool IsScheduledActive => npcMove || (movementSteps != null && movementSteps.Count > 0);

    [Header("�ƶ�����")]
    public float normalSpeed = 2f;
    private float minSpeed = 1;
    private float maxSpeed = 3;
    private Vector2 dir;
    public bool isMoving;

    //Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D coll;
    private Animator anim;
    private Grid gird;
    private Stack<MovementStep> movementSteps;
    private Coroutine npcMoveRoutine;

    private bool isInitialised;
    private bool npcMove;
    private bool sceneLoaded;
    public bool interactable;
    public bool isFirstLoad;

    //������ʱ��
    private float animationBreakTime;
    private bool canPlayStopAnimaiton;
    private AnimationClip stopAnimationClip;
    public AnimationClip blankAnimationClip;
    private AnimatorOverrideController animOverride;

    [Header("精灵翻转（仅针对单朝向素材的 NPC）")]
    [Tooltip("勾上后，按移动方向自动翻转 SpriteRenderer.flipX。仅用于素材只有一个朝向（左/右）的 NPC；如果该 NPC 已有上下左右四方向独立动画，保持关闭即可。")]
    public bool flipSpriteByHorizontal = false;

    [Tooltip("素材原图朝向：默认 false = 朝右；勾上 = 朝左。决定 flipX 的判断方向。")]
    public bool spriteOriginallyFacesLeft = false;

    [Tooltip("水平死区：|dir.x| 低于此值不触发翻转，防止纯垂直移动或斜走时 flipX 来回抖。")]
    [Range(0f, 1f)]
    public float flipHorizontalDeadZone = 0.1f;

    [Header("调试")]
    [Tooltip("打开后，会把动画/移动/Schedule 的关键节点输出到 Console，帮助定位“为什么一直在播同一个动画”等问题。")]
    public bool verboseLog = false;
    private string _lastAnimDebugSnapshot;

    private TimeSpan GameTime => TimeManager.Instance.GameTime;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        movementSteps = new Stack<MovementStep>();

        scheduleSet = new SortedSet<ScheduleDetails>();

        foreach (var schedule in scheduleData.scheduleList)
        {
            scheduleSet.Add(schedule);
        }
        animOverride = new AnimatorOverrideController(anim.runtimeAnimatorController);
        anim.runtimeAnimatorController = animOverride;
        gird = FindObjectOfType<Grid>();
    }

    // 【修复】添加 Start 方法，确保单场景架构中正确初始化
    private void Start()
    {
        // 延迟一帧初始化，确保所有组件都已就绪
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return null; // 等待一帧

        // 如果还没初始化，手动初始化
        if (!isInitialised)
        {
            gird = FindObjectOfType<Grid>();
            if (gird != null)
            {
                InitNPC();
                isInitialised = true;
                sceneLoaded = true;
                Debug.Log($"[NPC] {gameObject.name} 通过 Start 初始化完成");
            }
            else
            {
                Debug.LogError($"[NPC] {gameObject.name} 找不到 Grid！");
            }
        }
    }

    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.BeforeSceneUnloadEvent += OnBeforeSceneUnloadEvent;

        EventHandler.GameMinuteEvent += OnGameMinuteEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
    }

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.BeforeSceneUnloadEvent -= OnBeforeSceneUnloadEvent;

        EventHandler.GameMinuteEvent -= OnGameMinuteEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
    }


    private void Update()
    {
        //��ʱ��
        animationBreakTime -= Time.deltaTime;
        canPlayStopAnimaiton = animationBreakTime <= 0;

        // ������һ��
        SwitchAnimation();
    }

    private void FixedUpdate()
    {
        if (sceneLoaded)
            Movement();
    }
    private void OnEndGameEvent()
    {
        sceneLoaded = false;
        npcMove = false;
        if (npcMoveRoutine != null)
            StopCoroutine(npcMoveRoutine);
    }

    private void OnStartNewGameEvent(int obj)
    {
        isInitialised = false;
        isFirstLoad = true;
    }

    private void OnGameMinuteEvent(int minute, int hour, int day)
    {
        int time = (hour * 100) + minute;

        if (verboseLog)
            Debug.Log($"[NPCMovement][{name}] OnGameMinuteEvent {hour:D2}:{minute:D2} (time={time}), day={day}, scheduleCount={scheduleSet?.Count ?? 0}");

        ScheduleDetails matchSchedule = null;
        foreach (var schedule in scheduleSet)
        {
            if (schedule.Time == time)
            {
                if (schedule.day != day && schedule.day != 0)
                {
                    if (verboseLog)
                        Debug.Log($"[NPCMovement][{name}] schedule 命中时间但 day 不符: schedule.day={schedule.day}, currentDay={day}");
                    continue;
                }
                matchSchedule = schedule;
            }
            else if (schedule.Time > time)
            {
                break;
            }
        }

        if (matchSchedule != null)
        {
            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] 执行 schedule -> targetScene={matchSchedule.targetScene}, targetGrid={matchSchedule.targetGridPosition}, clipAtStop={(matchSchedule.clipAtStop != null ? matchSchedule.clipAtStop.name : "null")}");
            BuildPath(matchSchedule);
        }
        else if (verboseLog)
        {
            Debug.Log($"[NPCMovement][{name}] 当前 time={time} 没有匹配 schedule");
        }
    }

    private void OnBeforeSceneUnloadEvent()
    {
        sceneLoaded = false;
    }

    private void OnAfterSceneLoadedEvent()
    {
        gird = FindObjectOfType<Grid>();
        CheckVisiable();

        if (!isInitialised)
        {
            InitNPC();
            isInitialised = true;
        }

        sceneLoaded = true;

        if (!isFirstLoad)
        {
            currentGridPosition = gird.WorldToCell(transform.position);
            var schedule = new ScheduleDetails(0, 0, 0, 0, targetScene, (Vector2Int)tragetGridPosition, stopAnimationClip, interactable);
            BuildPath(schedule);
            isFirstLoad = true;
        }
    }

    private void CheckVisiable()
    {
        if (currentScene == SceneManager.GetActiveScene().name)
            SetActiveInScene();
        else
            SetInactiveInScene();
    }


    private void InitNPC()
    {
        targetScene = currentScene;

        // 【修复】确保 Grid 引用存在
        if (gird == null)
        {
            gird = FindObjectOfType<Grid>();
        }

        if (gird == null)
        {
            Debug.LogError($"[InitNPC] 找不到 Grid！NPC: {gameObject.name}");
            return;
        }

        // 保持在当前坐标的网格中心点
        currentGridPosition = gird.WorldToCell(transform.position);
        transform.position = new Vector3(currentGridPosition.x + Settings.gridCellSize / 2f, currentGridPosition.y + Settings.gridCellSize / 2f, 0);

        tragetGridPosition = currentGridPosition;
    }

    /// <summary>
    /// ��Ҫ�ƶ�����
    /// </summary>
    private void Movement()
    {
        if (!npcMove)
        {
            if (movementSteps.Count > 0)
            {
                MovementStep step = movementSteps.Pop();

                currentScene = step.sceneName;

                CheckVisiable();

                nextGridPosition = (Vector3Int)step.gridCoordinate;
                TimeSpan stepTime = new TimeSpan(step.hour, step.minute, step.second);

                if (verboseLog)
                    Debug.Log($"[NPCMovement][{name}] Movement Pop -> nextGrid=({nextGridPosition.x},{nextGridPosition.y}) stepsLeft={movementSteps.Count} | rbValid={(rb != null)} rbType={(rb != null ? rb.bodyType.ToString() : "null")} simulated={(rb != null ? rb.simulated.ToString() : "null")} | gird={(gird != null ? "OK" : "NULL")} sceneLoaded={sceneLoaded}");

                MoveToGridPosition(nextGridPosition, stepTime);
            }
            else if (!isMoving && canPlayStopAnimaiton)
            {
                StartCoroutine(SetStopAnimation());
            }
        }
    }

    private void MoveToGridPosition(Vector3Int gridPos, TimeSpan stepTime)
    {
        npcMoveRoutine = StartCoroutine(MoveRoutine(gridPos, stepTime));
    }

    private IEnumerator MoveRoutine(Vector3Int gridPos, TimeSpan stepTime)
    {
        if (verboseLog)
            Debug.Log($"[NPCMovement][{name}] MoveRoutine ENTER step->grid={gridPos.x},{gridPos.y} | gird={(gird != null ? "OK" : "NULL")} rb={(rb != null ? "OK" : "NULL")}");

        npcMove = true;
        nextWorldPosition = GetWorldPostion(gridPos);

        float distance = Vector3.Distance(transform.position, nextWorldPosition);

        bool dbgEnteredWhile = false;
        Vector3 dbgStartPos = transform.position;
        Vector3 dbgRbStartPos = rb != null ? (Vector3)rb.position : transform.position;

        if (verboseLog)
            Debug.Log($"[NPCMovement][{name}] MoveRoutine START step->grid={gridPos.x},{gridPos.y} target={nextWorldPosition} | tf={dbgStartPos} rb={dbgRbStartPos} distance={distance:F4} pixelSize={Settings.pixelSize}");

        // 【修复】先更新方向，确保动画能正确播放
        if (distance > Settings.pixelSize)
        {
            dir = (nextWorldPosition - transform.position).normalized;
        }

        // 还有时间用来移动
        if (stepTime > GameTime)
        {
            // 用来移动的时间差，以秒为单位
            float timeToMove = (float)(stepTime.TotalSeconds - GameTime.TotalSeconds);
            // 实际移动速度
            float speed = Mathf.Max(minSpeed, (distance / timeToMove / Settings.secondThreshold));

            // 【修复】即使速度超过maxSpeed，也使用maxSpeed移动，而不是瞬移
            speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

            while (Vector3.Distance(transform.position, nextWorldPosition) > Settings.pixelSize)
            {
                dbgEnteredWhile = true;
                dir = (nextWorldPosition - transform.position).normalized;

                Vector2 posOffset = new Vector2(dir.x * speed * Time.fixedDeltaTime, dir.y * speed * Time.fixedDeltaTime);
                rb.MovePosition(rb.position + posOffset);
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            // 【修复】时间已过，使用最大速度移动而不是瞬移
            while (Vector3.Distance(transform.position, nextWorldPosition) > Settings.pixelSize)
            {
                dbgEnteredWhile = true;
                dir = (nextWorldPosition - transform.position).normalized;

                Vector2 posOffset = new Vector2(dir.x * maxSpeed * Time.fixedDeltaTime, dir.y * maxSpeed * Time.fixedDeltaTime);
                rb.MovePosition(rb.position + posOffset);
                yield return new WaitForFixedUpdate();
            }
        }

        // 确保到达精确位置
        rb.position = nextWorldPosition;
        currentGridPosition = gridPos;
        nextGridPosition = currentGridPosition;

        npcMove = false;

        if (verboseLog)
        {
            Vector3 dbgEndTf = transform.position;
            Vector3 dbgEndRb = rb != null ? (Vector3)rb.position : transform.position;
            Debug.Log($"[NPCMovement][{name}] MoveRoutine END | enteredWhile={dbgEnteredWhile} | tf={dbgStartPos} -> {dbgEndTf} (Δ={(dbgEndTf - dbgStartPos).magnitude:F4}) | rb={dbgRbStartPos} -> {dbgEndRb} (Δ={(dbgEndRb - dbgRbStartPos).magnitude:F4}) | rbType={(rb != null ? rb.bodyType.ToString() : "null")} simulated={(rb != null ? rb.simulated.ToString() : "null")}");
        }
    }


    /// <summary>
    /// ����Schedule����·��
    /// </summary>
    /// <param name="schedule"></param>
    public void BuildPath(ScheduleDetails schedule)
    {
        // 强制刷新当前网格位置
        Grid grid = FindObjectOfType<Grid>();
        if (grid != null)
        {
            currentGridPosition = grid.WorldToCell(transform.position);
        }
        else
        {
            Debug.LogError("[NPCMovement] BuildPath: Grid not found");
        }

        if (verboseLog)
            Debug.Log($"[NPCMovement][{name}] BuildPath START | currentScene='{currentScene}', currentGrid={(Vector2Int)currentGridPosition}, target='{schedule.targetScene}' {schedule.targetGridPosition}, worldPos={transform.position}");

        movementSteps.Clear();
        currentSchedule = schedule;
        currentSchedule = schedule;
        targetScene = schedule.targetScene;
        tragetGridPosition = (Vector3Int)schedule.targetGridPosition;
        stopAnimationClip = schedule.clipAtStop;
        this.interactable = schedule.interactable;

        movementSteps.Clear();
        if (schedule.targetScene == currentScene)
        {
            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] BuildPath same-scene -> AStar.BuildPath('{schedule.targetScene}', from={(Vector2Int)currentGridPosition}, to={schedule.targetGridPosition})");

            AStar.Instance.BuildPath(schedule.targetScene, (Vector2Int)currentGridPosition, schedule.targetGridPosition, movementSteps);

            if (verboseLog)
            {
                Debug.Log($"[NPCMovement][{name}] BuildPath AStar 返回 movementSteps.Count={movementSteps.Count}");

                // 进一步诊断：起点/终点是否在 GridMapManager 的 tileDetailsDict 里、是否被标成 isNPCObstacle
                if (movementSteps.Count == 0 && MFarm.Map.GridMapManager.Instance != null)
                {
                    var gm = MFarm.Map.GridMapManager.Instance;
                    var startTile = gm.GetTileDetailsByPosition(currentGridPosition.x, currentGridPosition.y);
                    var endTile = gm.GetTileDetailsByPosition(schedule.targetGridPosition.x, schedule.targetGridPosition.y);
                    Debug.LogWarning(
                        $"[NPCMovement][{name}] AStar 0 步诊断:" +
                        $" startTile({currentGridPosition.x},{currentGridPosition.y}) = {(startTile == null ? "NULL(不在地图)" : (startTile.isNPCObstacle ? "存在但 isNPCObstacle=true" : "可走"))}," +
                        $" endTile({schedule.targetGridPosition.x},{schedule.targetGridPosition.y}) = {(endTile == null ? "NULL(不在地图)" : (endTile.isNPCObstacle ? "存在但 isNPCObstacle=true" : "可走"))}");
                }
            }
        }
        else if (schedule.targetScene != currentScene)
        {
            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] BuildPath multi-scene branch: from='{currentScene}' to='{schedule.targetScene}' (注意：单场景项目走到这里通常意味着 currentScene 没设对)");

            SceneRoute sceneRoute = NPCManager.Instance.GetSceneRoute(currentScene, schedule.targetScene);

            if (sceneRoute == null)
            {
                if (verboseLog)
                    Debug.LogWarning($"[NPCMovement][{name}] BuildPath: 没找到 SceneRoute('{currentScene}' -> '{schedule.targetScene}')，本次寻路放弃。");
            }
            else
            {
                for (int i = 0; i < sceneRoute.scenePathList.Count; i++)
                {
                    Vector2Int fromPos, gotoPos;
                    ScenePath path = sceneRoute.scenePathList[i];

                    if (path.fromGridCell.x >= Settings.maxGridSize)
                    {
                        fromPos = (Vector2Int)currentGridPosition;
                    }
                    else
                    {
                        fromPos = path.fromGridCell;
                    }

                    if (path.gotoGridCell.x >= Settings.maxGridSize)
                    {
                        gotoPos = schedule.targetGridPosition;
                    }
                    else
                    {
                        gotoPos = path.gotoGridCell;
                    }

                    AStar.Instance.BuildPath(path.sceneName, fromPos, gotoPos, movementSteps);
                }
            }
        }

        if (movementSteps.Count > 1)
        {
            UpdateTimeOnPath();
        }

        if (movementSteps.Count > 0)
        {
            if (gird == null)
            {
                gird = FindObjectOfType<Grid>();
            }
            sceneLoaded = true;

            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] BuildPath END OK | steps={movementSteps.Count}, sceneLoaded=true");
        }
        else if (verboseLog)
        {
            Debug.LogWarning($"[NPCMovement][{name}] BuildPath END 但路径为空。可能原因：1) currentGrid 与 targetGrid 相同 2) 目标格不可达 / 被 isNPCObstacle 阻挡 3) currentScene 与 targetScene 不一致导致走多场景分支但没有 SceneRoute。");
        }
    }


    /// <summary>
    /// ����·����ÿһ����ʱ��
    /// </summary>
    private void UpdateTimeOnPath()
    {
        MovementStep previousSetp = null;

        TimeSpan currentGameTime = GameTime;

        foreach (MovementStep step in movementSteps)
        {
            if (previousSetp == null)
                previousSetp = step;

            step.hour = currentGameTime.Hours;
            step.minute = currentGameTime.Minutes;
            step.second = currentGameTime.Seconds;

            TimeSpan gridMovementStepTime;

            if (MoveInDiagonal(step, previousSetp))
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellDiagonalSize / normalSpeed / Settings.secondThreshold));
            else
                gridMovementStepTime = new TimeSpan(0, 0, (int)(Settings.gridCellSize / normalSpeed / Settings.secondThreshold));

            //�ۼӻ����һ����ʱ���
            currentGameTime = currentGameTime.Add(gridMovementStepTime);
            //ѭ����һ��
            previousSetp = step;
        }
    }

    /// <summary>
    /// �ж��Ƿ���б����
    /// </summary>
    /// <param name="currentStep"></param>
    /// <param name="previousStep"></param>
    /// <returns></returns>
    private bool MoveInDiagonal(MovementStep currentStep, MovementStep previousStep)
    {
        return (currentStep.gridCoordinate.x != previousStep.gridCoordinate.x) && (currentStep.gridCoordinate.y != previousStep.gridCoordinate.y);
    }

    /// <summary>
    /// �������귵�������������ĵ�
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    private Vector3 GetWorldPostion(Vector3Int gridPos)
    {
        Vector3 worldPos = gird.CellToWorld(gridPos);
        return new Vector3(worldPos.x + Settings.gridCellSize / 2f, worldPos.y + Settings.gridCellSize / 2);
    }


    private void SwitchAnimation()
    {
        // 【修复】如果 Grid 为空，不执行动画切换
        if (gird == null)
        {
            anim.SetBool("isMoving", false);
            DebugAnimSnapshot("Grid==null");
            return;
        }

        // 【修复】使用距离判断代替直接比较，避免浮点精度问题
        Vector3 targetWorldPos = GetWorldPostion(tragetGridPosition);
        float distanceToTarget = Vector3.Distance(transform.position, targetWorldPos);

        // 【修复】只有在有有效方向时才认为正在移动
        bool hasValidDirection = dir.sqrMagnitude > 0.01f;
        isMoving = distanceToTarget > Settings.pixelSize && hasValidDirection;

        anim.SetBool("isMoving", isMoving);
        if (isMoving)
        {
            anim.SetBool("Exit", true);
            anim.SetFloat("DirX", dir.x);
            anim.SetFloat("DirY", dir.y);
        }
        else
        {
            anim.SetBool("Exit", false);
        }

        DebugAnimSnapshot("SwitchAnimation");

        // 仅对单朝向素材的 NPC 翻 SpriteRenderer.flipX。
        // 与 Animator/BlendTree 不冲突：BlendTree 决定播哪份 Clip，flipX 只影响最终贴图的水平镜像；
        // 项目内所有 NPC 的 .anim 都未给 flipX 打关键帧，因此不会被动画反向覆盖。
        if (flipSpriteByHorizontal && spriteRenderer != null)
        {
            if (isMoving && Mathf.Abs(dir.x) > flipHorizontalDeadZone)
            {
                bool faceLeft = dir.x < 0f;
                spriteRenderer.flipX = spriteOriginallyFacesLeft ? !faceLeft : faceLeft;
            }
            // |dir.x| 落在死区内 或 不在移动：保持上一次的 flipX，避免抖动。
        }
    }

    /// <summary>
    /// 把当前 Animator 的关键参数 + 状态名打印出来，仅在“快照变化时”输出，避免刷屏。
    /// 用来回答：是不是脚本根本没把 isMoving / EventAnimation 切到对的值？
    /// </summary>
    private void DebugAnimSnapshot(string tag)
    {
        if (!verboseLog || anim == null) return;

        var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        string stateHash = stateInfo.fullPathHash.ToString();

        // 把当前正在播的 clip 名打出来，方便确认到底播的是 BlankAnimation 还是 children。
        string clipNames = "(no-clip)";
        var clipInfos = anim.GetCurrentAnimatorClipInfo(0);
        if (clipInfos != null && clipInfos.Length > 0)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < clipInfos.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(clipInfos[i].clip != null ? clipInfos[i].clip.name : "null");
            }
            clipNames = sb.ToString();
        }

        // 仅在“快照变化”时输出，避免刷屏（normalizedTime 不计入比较）。
        string snapshot =
            $"isMoving={anim.GetBool("isMoving")}, " +
            $"Exit={anim.GetBool("Exit")}, " +
            $"EventAnimation={anim.GetBool("EventAnimation")}, " +
            $"DirX={anim.GetFloat("DirX"):F2}, DirY={anim.GetFloat("DirY"):F2}, " +
            $"npcMove={npcMove}, steps={(movementSteps != null ? movementSteps.Count : 0)}, " +
            $"stateHash={stateHash}, clip={clipNames}, inTransition={anim.IsInTransition(0)}";

        if (snapshot != _lastAnimDebugSnapshot)
        {
            Debug.Log($"[NPCMovement][{name}][{tag}] {snapshot}");
            _lastAnimDebugSnapshot = snapshot;
        }
    }

    private IEnumerator SetStopAnimation()
    {
        //ǿ������ͷ
        anim.SetFloat("DirX", 0);
        anim.SetFloat("DirY", -1);

        animationBreakTime = Settings.animationBreakTime;

        if (verboseLog)
            Debug.Log($"[NPCMovement][{name}] SetStopAnimation 进入: stopAnimationClip={(stopAnimationClip != null ? stopAnimationClip.name : "null")}, blankAnimationClip={(blankAnimationClip != null ? blankAnimationClip.name : "null")}");

        if (stopAnimationClip != null && blankAnimationClip != null)
        {
            // 把占位 clip 替换成本次 schedule 想播的 stop 动画
            animOverride[blankAnimationClip] = stopAnimationClip;
            anim.SetBool("EventAnimation", true);
            yield return null;
            anim.SetBool("EventAnimation", false);

            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] SetStopAnimation 已触发覆盖: blank({blankAnimationClip.name}) -> {stopAnimationClip.name}");
        }
        else if (blankAnimationClip != null)
        {
            // 没有自定义 stop 动画 → 把覆盖恢复成 blank（自我替换 = 还原默认）
            animOverride[blankAnimationClip] = blankAnimationClip;
            anim.SetBool("EventAnimation", false);

            if (verboseLog)
                Debug.Log($"[NPCMovement][{name}] SetStopAnimation 无 stopClip, 已恢复 blank.");
        }
        else if (verboseLog)
        {
            Debug.LogWarning($"[NPCMovement][{name}] SetStopAnimation: blankAnimationClip 未配置，无法做 Override，请在 Inspector 指定。");
        }
    }

    #region ����NPC��ʾ���
    private void SetActiveInScene()
    {
        spriteRenderer.enabled = true;
        coll.enabled = true;

        // ���޸�������Ƿ����Ӷ���
        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    private void SetInactiveInScene()
    {
       // spriteRenderer.enabled = false;
        coll.enabled = false;

        // ���޸�������Ƿ����Ӷ���
        if (transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
#endregion
