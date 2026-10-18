using UnityEngine;

// 用于生成唯一ID标识。
public class UniqueID : MonoBehaviour
{

    // 使用ReadOnly和SerializeField属性，将_id标记为只读字段，并在Unity编辑器中显示。
    [ReadOnly, SerializeField] private string _id;

    // 获取_id字段的值。
    public string ID => _id;

    // 当对象在Unity编辑器中被修改时触发的事件函数。
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(_id))
        {
            _id = System.Guid.NewGuid().ToString();
        }
    }

    [ContextMenu("重新生成ID")]
    public void ResetID()
    {
        _id = System.Guid.NewGuid().ToString();
    }
}

