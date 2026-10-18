using System.Collections; // 引入协程/迭代器接口等（本脚本未直接使用协程，但保留常见引用）。
using System.Collections.Generic; // 引入泛型集合（本脚本未直接使用，但为常见模板引用）。
using UnityEngine; // 引入 Unity 核心类型（MonoBehaviour、GameObject、Collider2D 等）。
using UnityEngine.SceneManagement; // 引入场景管理相关 API（SceneManager、Scene 等）。

public class SceneLoadTrigger : MonoBehaviour // 场景加载触发器：当玩家进入触发器时，按配置加载/卸载场景。
{
    [SerializeField, Header("需要加载的场景数组")] private SceneField[] _scenesToLoad; // 进入触发器后需要加载的场景列表（Additive 方式叠加）。
    [SerializeField, Header("需要卸载的场景数组")] private SceneField[] _scenesToUnload; // 进入触发器后需要卸载的场景列表（释放不再需要的区域）。
    private GameObject _player; // 缓存玩家对象引用，用于判断进入触发器的是否是玩家。

    private void Awake() // Unity 生命周期：对象初始化阶段，适合获取场景内引用。
    {
        _player = GameObject.FindGameObjectWithTag("Player"); // 通过 Tag 查找玩家对象（要求玩家物体已设置为 Player 标签）。
    }

    private void OnTriggerEnter2D(Collider2D collision) // 2D 触发器回调：有 Collider2D 进入并触发时调用。
    {
        if (collision.gameObject == _player) // 判断进入者是否为玩家（避免 NPC/子弹等误触发）。
        {
            LoadScenes(); // 加载配置的场景（若尚未加载）。
            UnloadScenes(); // 卸载配置的场景（若当前已加载）。
        }
    }

    // 加载场景
    private void LoadScenes() // 内部方法：遍历 _scenesToLoad，确保目标场景被加载。
    {
        for (int i = 0; i < _scenesToLoad.Length; i++) // 遍历所有需要加载的场景配置。
        {
            bool isSceneLoaded = false; // 标记该场景是否已在当前已加载场景列表中。
            for (int j = 0; j < SceneManager.sceneCount; j++) // 遍历目前已经加载到内存中的所有场景。
            {
                Scene loadedScene = SceneManager.GetSceneAt(j); // 根据索引获取已加载的 Scene 对象。
                if (loadedScene.name == _scenesToLoad[i].SceneName) // 用场景名比对：如果已存在则不重复加载。
                {
                    isSceneLoaded = true; // 标记为已加载。
                    break; // 已找到目标场景，结束遍历。
                }
            }
            if (!isSceneLoaded) // 如果该场景尚未加载。
            {
                SceneManager.LoadSceneAsync(_scenesToLoad[i].SceneName, LoadSceneMode.Additive); // 异步叠加加载场景（不卸载当前场景）。
            }
        }
    }

    // 卸载场景
    private void UnloadScenes() // 内部方法：遍历 _scenesToUnload，如果已加载则卸载。
    {
        for (int i = 0; i < _scenesToUnload.Length; i++) // 遍历所有需要卸载的场景配置。
        {
            for (int j = 0; j < SceneManager.sceneCount; j++) // 遍历当前已加载场景，用于判断目标是否在内存中。
            {
                Scene loadedScene = SceneManager.GetSceneAt(j); // 获取当前索引对应的已加载场景。
                if (loadedScene.name == _scenesToUnload[i].SceneName) // 如果该已加载场景就是我们要卸载的目标。
                {
                    SceneManager.UnloadSceneAsync(_scenesToUnload[i].SceneName); // 异步卸载场景，释放资源并从 sceneCount 中移除。
                }
            }
        }
    }
}

