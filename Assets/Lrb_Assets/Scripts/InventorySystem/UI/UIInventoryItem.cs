using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

namespace Inventory.UI
{
    public class UIInventoryItem : MonoBehaviour,
    IPointerClickHandler,   // 处理点击
    IBeginDragHandler,      // 开始拖拽
    IEndDragHandler,        // 结束拖拽
    IDropHandler,           // 在自身释放拖拽的物品
    IDragHandler            // 拖拽过程中持续调用
    {
        [SerializeField]
        private Image itemImage;
        [SerializeField]
        private TMP_Text quantityText;
        [SerializeField]
        private Image borderImage;

        private string m_itemName;

        //事件：点击物品、丢弃物品、拖动物品、右键鼠标一系列使用丢弃的行为
        public event Action<UIInventoryItem> 
            OnItemClicked,
            OnItemDroppedOn, 
            OnItemBeginDrag, 
            OnItemEndDrag,
            OnRightMouseBtnClick;

        private bool empty = true;

        public void Awake()
        {
            ResetData();
            Deselect();
        }

        public void ResetData()
        {
            itemImage.gameObject.SetActive(false);
            empty = true;
        }

        //取消选中，关闭边框图片
        public void Deselect()
        {
            borderImage.enabled = false;
        }

        public void SetData(Sprite sprite, int quantity)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = sprite;
            //整数转字符串再赋值quantityText.text = quantity.ToString();
            quantityText.text = quantity + "";//将整数转字符串的一个手段
            empty = false;
        }

        public void SetName(string name)
        {
            m_itemName = name;
        }

        public void Select()
        {
            borderImage.enabled = true;
        }

        public void OnPointerClick(PointerEventData pointerData)//pointerData意为指针数据
        {
            //实现Unity提供的点击接口，根据点击的鼠标键不同，触发设置的不同事件
            if (pointerData.button == PointerEventData.InputButton.Right)//按下右键
            {
                OnRightMouseBtnClick?.Invoke(this);
            }
            else if (pointerData.button == PointerEventData.InputButton.Left)
            {
                OnItemClicked?.Invoke(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (empty)
                return;
            OnItemBeginDrag?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnItemEndDrag?.Invoke(this);
        }



        public void OnDrag(PointerEventData eventData)
        {

        }

        public void OnDrop(PointerEventData eventData)
        {
            OnItemDroppedOn?.Invoke(this);
        }
    }
}