using ObserveMode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一次性的场景搭建工具：在当前激活场景里生成一套完整的观察模式所需对象结构，
/// 包含 ObserveModeManager（带 ObserveModeController + AudioSource）、独立 Overlay Canvas、
/// 全屏 Image，并把引用一次性拖好。
///
/// 使用方法：
/// 1. 在 Unity 顶部菜单 Tools/ObserveMode/Setup In Active Scene 点一下；
/// 2. 在选中的 ObserveModeManager 物体上把 Inspector 的 Config 字段拖入 ObserveModeConfig.asset；
/// 3. 在 AudioSource 上配置 RejectClip（"咔"）；
/// 4. 保存场景。
/// </summary>
public static class ObserveModeSceneSetup
{
    private const string MENU_PATH = "Tools/ObserveMode/Setup In Active Scene";
    private const string MANAGER_NAME = "ObserveModeManager";
    private const string CANVAS_NAME = "ObserveModeOverlayCanvas";
    private const string TINT_NAME = "FullScreenTint";

    /// <summary>
    /// 默认 sortOrder：让滤镜盖在世界 UI（一般 0）之上，但低于背包/线索板/对话弹窗（一般 100+）。
    /// 如果项目有不同约定，运行后可以在 Inspector 自行调整。
    /// </summary>
    private const int DEFAULT_OVERLAY_SORT_ORDER = 50;

    [MenuItem(MENU_PATH)]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("ObserveMode Setup", "请先打开一个有效的场景。", "OK");
            return;
        }

        var existing = Object.FindObjectOfType<ObserveModeController>();
        if (existing != null)
        {
            EditorGUIUtility.PingObject(existing);
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(
                "ObserveMode Setup",
                $"场景里已经存在 ObserveModeController：{GetHierarchyPath(existing.transform)}\n如需重新搭建请先删除旧物体再运行。",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("ObserveMode Setup");

        // 1. Manager 物体
        var managerGO = new GameObject(MANAGER_NAME);
        Undo.RegisterCreatedObjectUndo(managerGO, "Create ObserveModeManager");
        var controller = Undo.AddComponent<ObserveModeController>(managerGO);
        var audio = Undo.AddComponent<AudioSource>(managerGO);
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        // 2. 独立 Overlay Canvas
        var canvasGO = new GameObject(CANVAS_NAME, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create ObserveModeOverlayCanvas");
        canvasGO.transform.SetParent(managerGO.transform, false);
        var canvas = Undo.AddComponent<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = DEFAULT_OVERLAY_SORT_ORDER;
        canvas.pixelPerfect = false;

        var scaler = Undo.AddComponent<CanvasScaler>(canvasGO);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = Undo.AddComponent<GraphicRaycaster>(canvasGO);
        raycaster.ignoreReversedGraphics = true;

        // 3. 全屏 Image
        var tintGO = new GameObject(TINT_NAME, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(tintGO, "Create FullScreenTint");
        tintGO.transform.SetParent(canvasGO.transform, false);
        var tintRT = tintGO.GetComponent<RectTransform>();
        tintRT.anchorMin = Vector2.zero;
        tintRT.anchorMax = Vector2.one;
        tintRT.offsetMin = Vector2.zero;
        tintRT.offsetMax = Vector2.zero;

        var tintImage = Undo.AddComponent<Image>(tintGO);
        tintImage.color = new Color(0.92f, 0.78f, 0.42f, 0f);
        tintImage.raycastTarget = false;

        // 4. 把引用拖到 controller 上
        var so = new SerializedObject(controller);
        SetObjectField(so, "fullScreenTint", tintImage);
        SetObjectField(so, "audioSource", audio);
        TryAssignDefaultConfig(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 5. 选中并保存
        Selection.activeGameObject = managerGO;
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "ObserveMode Setup",
            "搭建完成！\n请在选中的 ObserveModeManager 上：\n1) 把 Config 字段拖入 ObserveModeConfig.asset；\n2) 在 AudioSource 配置 rejectClip（被屏蔽时的拒绝音）。\n\n滤镜 Canvas 的 Sort Order 默认为 50，请确认它低于背包/线索板/对话 UI。",
            "OK");
    }

    /// <summary>
    /// 第一次搭建时，尝试把工程里已经存在的 ObserveModeSO 资产自动赋值到 controller.config。
    /// 若有多个则不自动赋值，由用户手动选择。
    /// </summary>
    private static void TryAssignDefaultConfig(SerializedObject so)
    {
        string[] guids = AssetDatabase.FindAssets("t:ObserveModeSO");
        if (guids == null || guids.Length != 1) return;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var so_asset = AssetDatabase.LoadAssetAtPath<ObserveModeSO>(path);
        if (so_asset != null) SetObjectField(so, "config", so_asset);
    }

    private static void SetObjectField(SerializedObject so, string fieldName, Object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "(null)";
        string path = t.name;
        Transform p = t.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }
}
