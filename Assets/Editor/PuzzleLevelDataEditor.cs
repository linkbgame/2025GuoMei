using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// 为 PuzzleLevelData 增加一个自定义的 Inspector 界面
[CustomEditor(typeof(PuzzleLevelData))]
public class PuzzleLevelDataEditor : Editor
{
    private Transform sceneContainer; // 场景中的拼图容器

    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector 界面（保留原本的变量显示）
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自动化工具", EditorStyles.boldLabel);

        // 拖入场景中摆放好位置的父容器
        sceneContainer = (Transform)EditorGUILayout.ObjectField(
            "场景拼图容器", sceneContainer, typeof(Transform), true);

        PuzzleLevelData data = (PuzzleLevelData)target;

        if (GUILayout.Button("一键同步场景坐标至 SO"))
        {
            if (sceneContainer == null)
            {
                Debug.LogError("请先拖入场景中的拼图容器对象！");
                return;
            }

            SyncDataFromScene(data);
        }
    }

    private void SyncDataFromScene(PuzzleLevelData data)
    {
        // 1. 记录撤销操作，方便误操作还原
        Undo.RecordObject(data, "Sync Puzzle Data");

        // 2. 清空当前列表
        data.pieceSprites.Clear();
        data.correctPositions.Clear();

        // 3. 遍历容器下的所有子物体
        int count = sceneContainer.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = sceneContainer.GetChild(i);
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

            if (sr != null && sr.sprite != null)
            {
                data.pieceSprites.Add(sr.sprite);
                data.correctPositions.Add(child.localPosition);
            }
        }

        // 4. 保存更改
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log($"同步完成！成功记录了 {data.pieceSprites.Count} 个碎片的数据。");
    }
}