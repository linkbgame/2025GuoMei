using Inventory.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    [Header("自动获取对象引用")]
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private UIInventoryItem item;

    private void Awake()
    {
        item = GetComponentInChildren<UIInventoryItem>();
    }

    public void SetData(Sprite sprite, int quantity)
    {
        item.SetData(sprite, quantity);
    }

    //核心逻辑
    //只涉及UI显示，具体的数据交互在其他脚本中处理
    private void Update()
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            Input.mousePosition,
            canvas.worldCamera,
            out position);
        transform.position = canvas.transform.TransformPoint(position);
    }

    //切换开启和关闭的状态
    public void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }
}
