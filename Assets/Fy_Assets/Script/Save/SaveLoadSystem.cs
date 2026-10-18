using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// 存档加载系统
/// </summary>
public class SaveLoadSystem : SingletonMono<SaveLoadSystem>
{
    public GameData gameData;  // 当前游戏数据
    private IDataService dataService; // 数据服务
    public static SaveLoadSystem instance;
    protected override void Awake()
    {
        base.Awake();
        // 初始化JSON数据服务
        dataService = new FileDataService(new JsonSerializer());
    }
    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EventManager.Instance.Dispatch<GameData>(EventNameEnum.Load, gameData);
    }

    /// <summary>开始新游戏</summary>
    public void NewGame()
    {
        gameData = new GameData
        {
            Name = "NewGame",
            CurrentLevelName = "Game"
        };
        SceneManager.LoadScene(gameData.CurrentLevelName);
    }

    /// <summary>保存当前游戏</summary>
    public void SaveGame()
    {
        EventManager.Instance.Dispatch(EventNameEnum.Save);

        //获取当前场景名
        gameData.CurrentLevelName = SceneManager.GetActiveScene().name;

        dataService.Save(gameData);
    }
    public void WaitSaveGame()
    {
        StartCoroutine(WaitToSave());
    }
    IEnumerator WaitToSave()
    {
        yield return new WaitForSeconds(0.5f);
        SaveGame();
    }
    /// <summary>加载指定名称的游戏</summary>
    public void LoadGame(string gameName)
    {
        gameData = dataService.Load(gameName);
        SmoothLoadingScreen.instance.StartLoadingScene(gameData.CurrentLevelName);
        //SceneManager.LoadScene(gameData.CurrentLevelName);
    }

    /// <summary>重新加载当前游戏</summary>
    public void ReloadGame() => LoadGame(gameData.Name);

    /// <summary>删除指定名称的游戏</summary>
    public void DeleteGame(string gameName) => dataService.Delete(gameName);

    /// <summary>删除所有存档</summary>
    public void DeleteAllGame() => dataService.DeleteAll();

    /// <summary>列出所有存档名称</summary>
    public void ListSavesGame()
    {
        foreach (string saveName in dataService.ListSaves())
        {
            Debug.Log($"存档名称: {saveName}");
        }
    }
    /// <summary>获取所有存档数据</summary>
    public List<GameData> GetAllSavesGame()
    {
        List<GameData> allSaves = new List<GameData>();
        foreach (string saveName in dataService.ListSaves())
        {
            GameData it= dataService.Load(saveName);
            allSaves.Add(it);
        }
        return allSaves;
    }
}

