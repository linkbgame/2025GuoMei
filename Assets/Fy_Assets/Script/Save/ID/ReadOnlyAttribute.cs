using UnityEditor;
using UnityEngine;

/// <summary>
/// 只读属性。
/// 此属性用于标记只读属性或字段。
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR

/// <summary>
/// 用于ReadOnly属性的自定义绘制器类。
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadonlyDrawer : PropertyDrawer
{
    /// <summary>
    /// 在编辑器中绘制GUI的Unity方法。
    /// </summary>
    /// <param name="position">位置。</param>
    /// <param name="property">属性。</param>
    /// <param name="label">标签。</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 保存之前的GUI启用状态
        var previousGUIState = GUI.enabled;

        // 禁用属性的编辑
        GUI.enabled = false;

        // 绘制属性
        EditorGUI.PropertyField(position, property, label);

        // 恢复之前的GUI启用状态
        GUI.enabled = previousGUIState;
    }
}
#endif
