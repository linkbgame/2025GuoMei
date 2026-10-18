using UnityEngine;
using UnityEditor;
using System.IO; //用于文件读写
using System.Text; //用于拼接字符串
using System.Collections.Generic;
using Inventory.Model;
using ClueBoard.Model; // 引用你的命名空间

public class ClueCodeGenerator
{
    // 生成文件的路径，根据你的项目结构修改
    private const string ENUM_NAME = "EClueLink";
    private const string FILE_PATH = "Assets/Lrb_Assets/_Data/ClueBoard/Clues/";
    private const string FILE_NAME = "EClueLink.cs";

    // 在Unity上方菜单栏添加按钮 
    [MenuItem("Tools/ClueBoard/Generate Enum Clue Links")]
    public static void GenerateClueLinks()
    {
        // 1.找到数据库
        string[] guids = AssetDatabase.FindAssets("t:ClueDatabaseSO");
        if (guids.Length == 0)
        {
            Debug.LogError("没有找到 ClueDatabaseSO ！");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        ClueDatabaseSO database = AssetDatabase.LoadAssetAtPath<ClueDatabaseSO>(path);

        // 2.获取所有线索
        List<ClueSO> allClues = database.GetAllClues();

        // 3.开始构建代码内容
        StringBuilder code = new StringBuilder();

        code.AppendLine("// ========================================================");
        code.AppendLine("// THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.");
        code.AppendLine("// ========================================================");
        code.AppendLine("namespace Inventory.Model"); // 保持命名空间一致
        code.AppendLine("{");
        code.AppendLine($"    public enum {ENUM_NAME}");
        code.AppendLine("    {");
        code.AppendLine("        None = 0,"); // 默认值

        foreach (var clue in allClues)
        {
            if (clue != null && !string.IsNullOrWhiteSpace(clue.ClueLink))
            {
                // 处理 ID 中的非法字符（比如空格），Enum 名字不能有空格
                string validName = clue.ClueLink.Replace(" ", "_");
                code.AppendLine($"        {validName},");
            }
        }

        code.AppendLine("    }");
        code.AppendLine("}");

        // 4.写入文件
        if (!Directory.Exists(FILE_PATH))
        {
            Directory.CreateDirectory(FILE_PATH);
        }

        File.WriteAllText(FILE_PATH + FILE_NAME, code.ToString());

        // 5. 刷新资源，让Unity编译新代码
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Clue Links Generated Successfully at {FILE_PATH}{FILE_NAME}</color>");
    }

}
