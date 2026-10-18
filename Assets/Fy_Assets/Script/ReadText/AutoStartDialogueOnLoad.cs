using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 进入场景后自动调用 <see cref="Fy_DialogManager.TakeMyText"/>，不依赖按 E 或点击 NPC。
/// 「本进程只播一次」按 Inspector 中的 <see cref="processSessionPlayOnceKey"/> 区分实例，互不影响。
/// 新游戏入口需在加载场景前调用 <see cref="ResetPersistentIntroDialogueFlagForNewGame"/> 清空已播记录。
/// </summary>
public class AutoStartDialogueOnLoad : MonoBehaviour
{
    private static readonly HashSet<string> s_PlayedIntroKeysThisRun = new HashSet<string>();

    /// <summary>
    /// 新游戏时清空本进程内所有「播放键」记录，使各实例可再次触发自动对话。
    /// </summary>
    public static void ResetPersistentIntroDialogueFlagForNewGame()
    {
        s_PlayedIntroKeysThisRun.Clear();
    }

    [Header("对话")]
    [Tooltip("Resources/Read/ 下的文本资源名（不含路径与扩展名），与 NPCTakeSave.nowTalkingName 一致。")]
    [SerializeField] string dialogueResourceName;

    [Tooltip("留空则从本物体上 GetComponent<Fy_DialogManager>()。")]
    [SerializeField] Fy_DialogManager dialogManager;

    [Header("时机")]
    [Tooltip("Start 之后再等待的帧数，用于让 TalkUI、SaveLoad 等先完成 Awake/Start。")]
    [SerializeField] [Min(0)] int delayFrames = 1;

    [Header("本进程只播一次（按实例键）")]
    [Tooltip("勾选：本游戏进程内，下方键对应的自动对话仅成功触发一次；Persistent、客栈等不同物体请填不同键。")]
    [SerializeField] bool limitOncePerProcessByKey = true;

    [Tooltip("须与别的实例区分；如 PersistentScenes_Auto、客栈内_Auto。留空则不做「只播一次」限制（每次进场景且其它条件满足都会尝试）。")]
    [SerializeField] string processSessionPlayOnceKey = "PersistentScenes_Auto";

    [Header("场景限制")]
    [Tooltip("勾选后仅当本物体所在场景的名称与下方一致时才触发（避免预制误带到其它场景）。")]
    [SerializeField] bool onlyRunInNamedScene = true;

    [Tooltip("默认 PersistentScenes；与 Unity 里该场景在 Build 中的名称一致（与 .unity 文件名通常相同）。")]
    [SerializeField] string requiredSceneName = "PersistentScenes";

    private void Start()
    {
        if (onlyRunInNamedScene)
        {
            if (string.IsNullOrEmpty(requiredSceneName))
            {
                Debug.LogWarning($"{nameof(AutoStartDialogueOnLoad)} on {name}: 已勾选场景限制但 {nameof(requiredSceneName)} 为空，已跳过触发。", this);
                return;
            }

            if (gameObject.scene.name != requiredSceneName)
                return;
        }

        if (string.IsNullOrEmpty(dialogueResourceName))
        {
            Debug.LogWarning($"{nameof(AutoStartDialogueOnLoad)} on {name}: {nameof(dialogueResourceName)} 未填写，已跳过。", this);
            return;
        }

        Fy_DialogManager dm = dialogManager != null ? dialogManager : GetComponent<Fy_DialogManager>();
        if (dm == null)
        {
            Debug.LogError($"{nameof(AutoStartDialogueOnLoad)} on {name}: 未找到 {nameof(Fy_DialogManager)}。", this);
            return;
        }

        bool registerKeyAfterPlay = false;
        string trimmedPlayOnceKey = null;
        if (limitOncePerProcessByKey)
        {
            if (string.IsNullOrWhiteSpace(processSessionPlayOnceKey))
            {
                Debug.LogWarning(
                    $"{nameof(AutoStartDialogueOnLoad)} on {name}: 已勾选 {nameof(limitOncePerProcessByKey)} 但 {nameof(processSessionPlayOnceKey)} 为空，将不启用「只播一次」限制。",
                    this);
            }
            else
            {
                trimmedPlayOnceKey = processSessionPlayOnceKey.Trim();
                if (s_PlayedIntroKeysThisRun.Contains(trimmedPlayOnceKey))
                    return;

                registerKeyAfterPlay = true;
            }
        }

        StartCoroutine(RunAfterFrames(dm, registerKeyAfterPlay, trimmedPlayOnceKey));
    }

    private IEnumerator RunAfterFrames(Fy_DialogManager dm, bool registerKeyAfterPlay, string playOnceKey)
    {
        for (int i = 0; i < delayFrames; i++)
            yield return null;

        TextAsset asset = SimpleFactory.FindText(dialogueResourceName);
        if (asset == null)
        {
            Debug.LogError($"{nameof(AutoStartDialogueOnLoad)}: 未找到 Resources/Read/{dialogueResourceName}。", this);
            yield break;
        }

        dm.TakeMyText(asset);
        if (registerKeyAfterPlay && !string.IsNullOrEmpty(playOnceKey))
            s_PlayedIntroKeysThisRun.Add(playOnceKey);
    }
}
