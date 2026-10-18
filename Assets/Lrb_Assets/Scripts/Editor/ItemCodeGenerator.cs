using UnityEngine;
using UnityEditor;
using System.IO; //用于文件读写
using System.Text; //用于拼接字符串
using System.Collections.Generic;
using Inventory.Model; // 引用你的命名空间

/// <summary>
/// 根据 ItemDatabaseSO 中的物品列表生成一个枚举类 ItemEnum.cs
/// 枚举每个物品的 Name
/// </summary>
public class ItemCodeGenerator
{
    private const string ENUM_NAME = "ItemEnum";
    private const string FILE_PATH = "Assets/Lrb_Assets/_Data/Inventory/Items/"; 
    private const string FILE_NAME = "ItemEnum.cs";

    // 在Unity上方菜单栏添加按钮 Tools -> Generate Item IDs
    [MenuItem("Tools/Inventory/Generate Item Enums")]
    public static void GenerateItemNames()
    {
        // 1. 找到 ItemDatabaseSO 
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabaseSO");
        if (guids.Length == 0)
        {
            Debug.LogError("No ItemDatabaseSO found!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        ItemDatabaseSO database = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(path);

        // 2. 获取所有物品ItemSO的文件
        List<ItemSO> allItems = database.GetAllItems();

        // 3. 开始构建代码内容
        StringBuilder code = new StringBuilder();

        code.AppendLine("// ========================================================");
        code.AppendLine("// 该文件点击 Tools/Inventory/Generate Item Enums 生成，请勿手动修改!");
        code.AppendLine("// ========================================================");
        code.AppendLine("namespace Inventory.Model"); // 保持命名空间一致
        code.AppendLine("{");
        code.AppendLine($"    public enum {ENUM_NAME}");
        code.AppendLine("    {");
        code.AppendLine("        None = 0,"); // 默认值

        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.ItemID))
            {
                // 处理 ID 中的非法字符（比如空格），Enum 名字不能有空格
                // 使用 ItemSO.Name 作为枚举成员名
                string validName = item.Name.Replace(" ", "_");
                code.AppendLine($"        {validName},");
            }
        }

        code.AppendLine("    }");
        code.AppendLine("}");

        // 4. 写入文件
        if (!Directory.Exists(FILE_PATH))
        {
            Directory.CreateDirectory(FILE_PATH);
        }

        File.WriteAllText(FILE_PATH + FILE_NAME, code.ToString());

        // 5. 刷新资源，让Unity编译新代码
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Item IDs Generated Successfully at {FILE_PATH}{FILE_NAME}</color>");
    }
}