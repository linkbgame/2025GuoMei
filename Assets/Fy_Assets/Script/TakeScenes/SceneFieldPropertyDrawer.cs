using System.Collections; // 引入基础集合/迭代器相关命名空间（本文件主要用于序列化与编辑器绘制）。
using System.Collections.Generic; // 引入泛型集合命名空间（本文件未直接使用，但为常见模板引用）。
using UnityEngine; // 引入 Unity 核心 API（Object、SerializeField、Rect 等）。

#if UNITY_EDITOR // 仅在 Unity 编辑器环境编译以下代码，打包到 Player 时会被剔除。
using UnityEditor; // 引入 Unity 编辑器 API（PropertyDrawer、EditorGUI、SceneAsset 等）。
#endif

[System.Serializable] // 允许该类在 Inspector 中被序列化显示/保存。
public class SceneField // 自定义“场景字段”：用场景资源引用 + 场景名字符串，既便于选择也便于运行时加载。
{
    [SerializeField] // 序列化私有字段，使其可在 Inspector 中显示并保存。
    private Object m_SceneAsset; // 场景资源引用（Editor 下通常是 SceneAsset），用于在 Inspector 里拖拽选择。

    [SerializeField] // 序列化私有字段，用于运行时读取。
    private string m_SceneName = ""; // 场景名缓存：用于运行时通过 SceneManager.LoadScene(场景名) 加载。

    public string SceneName // 只读属性：对外暴露场景名，避免外部任意修改序列化值。
    {
        get { return m_SceneName; } // 返回保存的场景名字符串。
    }

    // 使 SceneField 可以隐式转换为 string：这样在调用 LoadScene/LoadSceneAsync 时可直接传 SceneField。
    public static implicit operator string(SceneField sceneField) // 隐式类型转换运算符：SceneField -> string。
    {
        return sceneField.SceneName; // 转换结果为该 SceneField 保存的场景名。
    }
}

#if UNITY_EDITOR // 以下自定义绘制器仅在编辑器存在，用于让 SceneField 在 Inspector 里显示为“场景选择框”。
[CustomPropertyDrawer(typeof(SceneField))] // 指定为 SceneField 类型提供自定义 Inspector 绘制逻辑。
public class SceneFieldPropertyDrawer : PropertyDrawer // 属性绘制器：控制 SceneField 在 Inspector 中如何显示与写入序列化数据。
{
    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) // Unity 编辑器回调：绘制该属性的 GUI。
    {
        EditorGUI.BeginProperty(_position, GUIContent.none, _property); // 开始绘制属性（处理 prefab override 等编辑器状态）。
        SerializedProperty sceneAsset = _property.FindPropertyRelative("m_SceneAsset"); // 找到 SceneField 内部的 m_SceneAsset 序列化字段。
        SerializedProperty sceneName = _property.FindPropertyRelative("m_SceneName"); // 找到 SceneField 内部的 m_SceneName 序列化字段。
        _position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label); // 绘制标签并返回去掉标签后的控件区域。
        if (sceneAsset != null) // 确保序列化字段存在（防止字段改名等导致 FindPropertyRelative 失败）。
        {
            // 显示对象选择框：限制类型为 SceneAsset，且不允许选择场景中的对象（false 表示不允许 SceneObject）。
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(_position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

            // 如果用户已经选择了场景资源，则把场景资源名同步保存到 m_SceneName（用于运行时加载）。
            if (sceneAsset.objectReferenceValue != null) // 仅当引用不为空时同步名字。
            {
                sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name; // 将 SceneAsset 的 name 写入字符串字段。
            }
        }
        EditorGUI.EndProperty(); // 结束绘制属性。
    }
}
#endif

