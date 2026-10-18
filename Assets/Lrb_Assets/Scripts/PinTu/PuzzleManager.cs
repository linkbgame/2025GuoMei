using DG.Tweening;
using Inventory;
using Inventory.Model;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    public static PuzzleLevelData NextLevelToLoad;
    public static event Action OnPuzzleExit;

    [Header("关卡配置")]
    public PuzzleLevelData currentLevel; // 当前关卡数据
    public GameObject piecePrefab;       // 碎片预制体（需挂载 PuzzlePiece 脚本和 SpriteRenderer）
    public Transform puzzleParent;       // 碎片的父容器

    [Header("视觉反馈")]
    [Range(0, 1)] public float referenceAlpha = 0.2f;

    [Header("拼图层级设置")]
    public string sortingLayerName = "Default";
    public int baseOrder = 0;
    public int lockedOrder = 5;
    public int draggingOrder = 100;

    [Header("Debug Gizmos")]
    public bool showScatterAreasGizmos = true;
    public Color scatterAreaGizmosColor = new Color(0.2f, 0.9f, 1f, 0.7f);

    [BoxGroup("UI设置")]
    [Required] public RectTransform victoryTextRect;
    [BoxGroup("UI设置")]
    [Required] public CanvasGroup victoryTextGroup;
    [BoxGroup("UI设置")]
    [Required] public CanvasGroup returnButtonCanvasGroup;
    [BoxGroup("UI设置")]
    public float textRiseDuration = 1.0f;
    [BoxGroup("UI设置")]
    public float textDisplayDuration = 1.5f;

    //Todo：玩拼图需要筹码？获取多少筹码？
    [LabelText("花费的筹码")]
    public int GuDingChouMa;

    private SpriteRenderer referenceSR;
    private List<GameObject> spawnedPieces = new List<GameObject>();
    private int lockedPiecesCount = 0;

    #region 音效
    [BoxGroup("音效设置")]
    [InlineProperty, HideLabel]
    public AudioSettings audioSettings;

    private AudioSource sfxSource;
    private AudioSource bgmSource;

    [System.Serializable]
    public class AudioSettings
    {
        [Title("交互音效")]
        [AssetSelector(Filter = "t:AudioClip")]
        public AudioClip pickUpClip;

        [AssetSelector(Filter = "t:AudioClip")]
        public AudioClip snapClip;

        [Title("氛围音效")]
        [AssetSelector(Filter = "t:AudioClip")]
        public AudioClip victoryMusic;
    }

    // 供外部调用的播放方法
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    // 播放胜利乐曲 (只播放一次)
    public void PlayVictoryBGM()
    {
        if (audioSettings.victoryMusic != null)
        {
            bgmSource.Stop(); // 先停止之前的声音
            bgmSource.clip = audioSettings.victoryMusic;
            bgmSource.Play(); // 播放当前剪辑，播放完会自动停止
        }
    }

    /*public void PlayVictoryBGM()
    {
        if (audioSettings.victoryMusic != null)
        {
            bgmSource.clip = audioSettings.victoryMusic;
            bgmSource.Play();
        }
    }*/
    #endregion

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DOTween.Init();

        // 动态创建音源
        sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = false;
        bgmSource.volume = 0.1f;

        // 初始化 UI 状态
        InitUI();
    }

    void Start()
    {
        // --- 核心简化：自动检查是否有外部传入的关卡 ---
        if (NextLevelToLoad != null)
        {
            currentLevel = NextLevelToLoad;
        }

        if (currentLevel != null)
        {
            InitializeLevel();
        }
        else
        {
            Debug.LogWarning("没有找到可加载的拼图关卡数据！");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AutoCompleteLevel();
        }
    }

    void OnEnable()
    {
        EventManager.Instance.AddEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, Load);
    }
    void OnDisable()
    {
        EventManager.Instance.RemoveEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, Load);
    }



    /// <summary>
    /// 根据 SO 数据初始化关卡
    /// </summary>
    public void InitializeLevel()
    {
        // 清理旧碎片
        foreach (var p in spawnedPieces)
        {
            p.transform.DOKill();
            Destroy(p);
        }
        spawnedPieces.Clear();
        lockedPiecesCount = 0;

        // 重置 UI
        InitUI();

        // 1. 先生成碎片（为了获取坐标数据）
        for (int i = 0; i < currentLevel.pieceSprites.Count; i++)
        {
            GameObject piece = Instantiate(piecePrefab, puzzleParent);
            piece.name = $"Piece_{i}";
            SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
            sr.sprite = currentLevel.pieceSprites[i];

            // 初始化层级
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = baseOrder;

            var pieceScript = piece.GetComponent<PuzzlePiece>();
            pieceScript.SetTarget(currentLevel.correctPositions[i], currentLevel.snapThreshold);
            spawnedPieces.Add(piece);
        }

        // 2. 根据碎片位置生成并对齐底图
        CreateReferenceImage();

        // 3. 打乱
        ScramblePieces();
    }

    // 由碎片脚本调用，通知有一块拼对了
    public void OnPieceLocked()
    {
        lockedPiecesCount++;

        //全部完成执行胜利逻辑
        if (lockedPiecesCount >= spawnedPieces.Count)
        {
            HandleVictory();
        }
    }

    /// <summary>
    /// 一键通关公开方法
    /// </summary>
    public void AutoCompleteLevel()
    {
        // 防止在已获胜的情况下重复触发
        if (lockedPiecesCount >= spawnedPieces.Count) return;

        // 针对未锁定的碎片进行处理
        foreach (var pieceObj in spawnedPieces)
        {
            PuzzlePiece piece = pieceObj.GetComponent<PuzzlePiece>();
            if (piece != null && !piece.IsLocked)
            {
                // 调用瞬移吸附
                piece.InstantSnap();

                // 增加已锁定计数
                lockedPiecesCount++;
            }
        }

        // 确保计数完全准确（防止逻辑遗漏）
        lockedPiecesCount = spawnedPieces.Count;

        // 触发完整的结算逻辑
        HandleVictory();

        // 获得奖励
        Save();
    }

    // --- 按钮回调逻辑 ---

    [Button("Restart Game"), GUIColor(0.8f, 1f, 0.8f)]
    public void OnClickRestart()
    {
        // 停止背景音，重置关卡
        bgmSource.Stop();
        InitializeLevel();
    }

    public void OnClickReturnToMainGame()
    {
        // 触发外部监听事件
        OnPuzzleExit?.Invoke();

        SaveLoadSystem.Instance.SaveGame();
        SmoothLoadingScreen.instance.StartLoadingScene("PersistentScenes");
    }

    private void InitUI()
    {
        victoryTextGroup.alpha = 0;
        returnButtonCanvasGroup.alpha = 0;
        returnButtonCanvasGroup.interactable = false;
        returnButtonCanvasGroup.blocksRaycasts = false;
    }

    private void CreateReferenceImage()
    {
        if (currentLevel.correctPositions.Count == 0) return;

        if (referenceSR != null)
        {
            // 注意：要销毁的是整个 GameObject，而不仅仅是组件
            Destroy(referenceSR.gameObject);
            referenceSR = null;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector2 pos in currentLevel.correctPositions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        Vector3 centerPos = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);

        // 生成底图
        GameObject refObj = new GameObject("ReferenceImage");
        refObj.transform.SetParent(puzzleParent);

        // 将底图坐标设置为计算出的中心点
        refObj.transform.localPosition = centerPos;

        referenceSR = refObj.AddComponent<SpriteRenderer>();
        referenceSR.sprite = currentLevel.fullImage;

        // 设置底图层级：始终低于碎片
        referenceSR.sortingLayerName = sortingLayerName;
        referenceSR.sortingOrder = -10;

        // 淡入效果
        Color c = referenceSR.color;
        c.a = 0;
        referenceSR.color = c;
        referenceSR.DOFade(referenceAlpha, 1.5f);
    }

    private void ScramblePieces()
    {
        var areas = currentLevel.scatterAreas;

        // 兼容：如果没配置区域，回退到你原来的单区域逻辑
        if (areas == null || areas.Count == 0)
        {
            foreach (var piece in spawnedPieces)
            {
                float randomX = UnityEngine.Random.Range(2f, 8f);
                float randomY = UnityEngine.Random.Range(-3f, 3f);
                piece.transform.DOLocalMove(new Vector2(randomX, randomY), 0.8f).SetEase(Ease.OutCubic);
            }
            return;
        }

        int areaCount = areas.Count;

        for (int i = 0; i < spawnedPieces.Count; i++)
        {
            int areaIndex;
            if (currentLevel.evenDistribution)
            {
                // 均匀分布：第0块到第0区，第1块到第1区 ... 循环
                areaIndex = i % areaCount;
            }
            else
            {
                // 随机分配到任意区域
                areaIndex = UnityEngine.Random.Range(0, areaCount);
            }

            Vector2 targetPos = GetRandomPointInArea(areas[areaIndex]);
            spawnedPieces[i].transform.DOLocalMove(targetPos, 0.8f).SetEase(Ease.OutCubic);
        }
    }

    private Vector2 GetRandomPointInArea(PuzzleLevelData.ScatterArea area)
    {
        float x = UnityEngine.Random.Range(area.center.x - area.size.x * 0.5f, area.center.x + area.size.x * 0.5f);
        float y = UnityEngine.Random.Range(area.center.y - area.size.y * 0.5f, area.center.y + area.size.y * 0.5f);
        return new Vector2(x, y);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showScatterAreasGizmos || currentLevel == null || currentLevel.scatterAreas == null || currentLevel.scatterAreas.Count == 0)
            return;

        Transform anchor = puzzleParent != null ? puzzleParent : transform;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;

        Gizmos.matrix = anchor.localToWorldMatrix;
        Gizmos.color = scatterAreaGizmosColor;

        for (int i = 0; i < currentLevel.scatterAreas.Count; i++)
        {
            var area = currentLevel.scatterAreas[i];
            if (area == null) continue;

            Vector3 center = new Vector3(area.center.x, area.center.y, 0f);
            Vector3 size = new Vector3(Mathf.Abs(area.size.x), Mathf.Abs(area.size.y), 0.01f);

            Gizmos.DrawWireCube(center, size);
            Gizmos.DrawSphere(center, 0.05f);
        }

        Gizmos.color = oldColor;
        Gizmos.matrix = oldMatrix;
    }

    

    private void HandleVictory()
    {
        // 播放胜利音乐
        PlayVictoryBGM();

        // 在播放动画前，统一所有碎片的层级，防止边缘“凸起”重叠冲突
        foreach (var piece in spawnedPieces)
        {
            var sr = piece.GetComponent<SpriteRenderer>();
            sr.sortingOrder = lockedOrder; // 统一到一个固定的平面
        }       

        // 1. 背景图高亮闪烁
        if (referenceSR != null)
        {
            referenceSR.DOFade(1.0f, 0.4f).SetLoops(4, LoopType.Yoyo).OnComplete(() => {
                // 最终停留在清晰度较高的状态或保持
                referenceSR.DOFade(0.8f, 0.5f);
            });
        }

        // 2. 全体碎片“欢呼”
        foreach (var piece in spawnedPieces)
        {
            var script = piece.GetComponent<PuzzlePiece>();
            if (script != null) script.PlayVictoryCheer();
        }

        // 触发 UI 胜利序列
        PlayVictoryUI();
    }

    private void PlayVictoryUI()
    {
        // 1. “完成！”字样动画
        victoryTextRect.anchoredPosition = new Vector2(0, -500); // 从下方开始
        victoryTextGroup.alpha = 1;

        Sequence textSeq = DOTween.Sequence();
        textSeq.Append(victoryTextRect.DOAnchorPos(Vector2.zero, textRiseDuration).SetEase(Ease.OutBack));
        textSeq.AppendInterval(textDisplayDuration);
        textSeq.Append(victoryTextGroup.DOFade(0, 0.5f));

        // 2. 返回按钮淡入
        returnButtonCanvasGroup.DOFade(1, 1.0f).SetDelay(0.5f).OnComplete(() => {
            returnButtonCanvasGroup.interactable = true;
            returnButtonCanvasGroup.blocksRaycasts = true;
        });
    }

    private void Save()
    {
        // 尝试查找现有数据
        data = SaveLoadSystem.Instance.gameData.bagData;
        if (data == null)
        {
            data = new BagData { };
            //data.ChouMa = 100;
            SaveLoadSystem.Instance.gameData.bagData = data;
        }
        // 增加固定筹码数量
        InventoryController.Instance.AddItemToInventory(FindItemByName("筹码"), GuDingChouMa);
        //data.ChouMa += ChouMaChance;
        //SaveLoadSystem.Instance.gameData.bagData.ChouMa += ChouMaChance;
    }

    public void Load(GameData gameData)
    {
        this.data = gameData.bagData;
        if (data == null) return;
    }

    private ItemSO FindItemByName(string name)
    {
        for (int i = 0; i < allofBagSth.Count; i++)
        {
            if (allofBagSth[i].Name == name)
            {
                return allofBagSth[i];
            }
        }
        return null;
    }
    public List<ItemSO> allofBagSth;
    public BagData data;
}
