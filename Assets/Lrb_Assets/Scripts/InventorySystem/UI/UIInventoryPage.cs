using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.UI
{
    public class UIInventoryPage : MonoBehaviour
    {
        [SerializeField]
        private UIInventoryItem itemPrefab;

        [SerializeField]
        private RectTransform contentPanel;

        [SerializeField]
        private UIInventoryDescription itemDescription;

        [SerializeField]
        private MouseFollower mouseFollower;

        [SerializeField]
        private ItemActionPanel actionPanel;

        List<UIInventoryItem> listOfUIItems = new List<UIInventoryItem>();

        /*//测试用的硬编码
        public Sprite image, image2;
        public int quantity;
        public string title, description;*/

        [SerializeField]
        private int currentlyDraggedItemIndex = -1;//当前拖拽对象的下标

        //事件
        public event Action<int> OnDescriptionRequested,
            OnItemActionRequested,
            OnStartDragging;

        public event Action<int, int> OnSwapItems;

        




        private void Awake()
        {
            Hide();//隐藏背包界面
            itemDescription.ResetDescription();//清空描述栏 
        }

        private void OnDestroy()
        {
            UnsubscribeInventoryUI();
        }

        #region Public Methods

        //初始化UI背包，根据背包大小创建对应数量的UI物品（空格），背包监听物品的事件
        public void InitializeInventoryUI(int inventorySize)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                UIInventoryItem uiItem =
                    Instantiate(itemPrefab, contentPanel);

                listOfUIItems.Add(uiItem);

                //背包订阅每个物品的事件，逻辑最初是在UI物品里触发开始的
                uiItem.OnItemClicked += HandleItemSelection;
                uiItem.OnItemBeginDrag += HandleBeginDrag;
                uiItem.OnItemEndDrag += HandleEndDrag;
                uiItem.OnItemDroppedOn += HandleSwap;
                uiItem.OnRightMouseBtnClick += HandleShowItemActions;
            }
        }

        public void UnsubscribeInventoryUI()
        {
            foreach (var uiItem in listOfUIItems)
            {
                // 取消订阅每个物品的事件
                uiItem.OnItemClicked -= HandleItemSelection;
                uiItem.OnItemBeginDrag -= HandleBeginDrag;
                uiItem.OnItemEndDrag -= HandleEndDrag;
                uiItem.OnItemDroppedOn -= HandleSwap;
                uiItem.OnRightMouseBtnClick -= HandleShowItemActions;
            }
        }

        public void Hide()
        {
            actionPanel.Toggle(false);//关闭物品行动面板
            gameObject.SetActive(false);//关闭整个背包UI对象
            itemDescription.HideDescription();//隐藏描述栏
            ResetDraggedItem();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            itemDescription.gameObject.SetActive(true);
            ResetSelection();
        }

        public void UpdateData(int itemIndex, Sprite itemImage, int itemQuantity)
        {
            if (listOfUIItems.Count > itemIndex)//更新对象的下标在范围内
            {
                listOfUIItems[itemIndex].SetData(itemImage, itemQuantity);
            }
        }

        //创建拖拽物品
        public void CreateDraggedItem(Sprite sprite, int quantity)
        {
            mouseFollower.Toggle(true);
            mouseFollower.SetData(sprite, quantity);
        }

        public void ResetSelection()
        {
            itemDescription.ResetDescription();
            DeselectAllItems();
        }

        public void AddAction(string actionName, Action performAction)
        {
            actionPanel.AddButton(actionName, performAction);
        }

        //在对应下标的Item的Slot显示行动button
        public void ShowItemAction(int itemIndex)
        {
            actionPanel.Toggle(true);
            actionPanel.transform.position = listOfUIItems[itemIndex].transform.position;
        }

        public void UpdateDescription(int itemIndex, Sprite itemImage, string name, string description)
        {
            itemDescription.ShowDescription();
            itemDescription.SetDescription(itemImage, name, description);
            DeselectAllItems();
            listOfUIItems[itemIndex].Select();
        }

        public void ResetAllItems()
        {
            foreach (var item in listOfUIItems)
            {
                item.ResetData();
                item.Deselect();
            }
        }

        public void SetDraggedItemName(int itemIndex, string itemName)
        {
            listOfUIItems[itemIndex].SetName(itemName);
        }

        //提供给InventoryController于设置描述拦的时候调用，将线索位置数据
        //从SceneItem-ItemSO-InventoryController-UIInventoryPage传递过来
        public void SetCurrentDesItemCluePos(Vector3 pos)
        {
            itemDescription.SetCluePos(pos);
        }

        #endregion

        #region Private Methods

        private void HandleShowItemActions(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;
            OnItemActionRequested?.Invoke(index);
        }

        private void HandleEndDrag(UIInventoryItem inventoryItemUI)
        {
            ResetDraggedItem();
        }

        private void HandleSwap(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
            {
                return;
            }

            //具体交换的逻辑，由Model执行
            
            OnSwapItems?.Invoke(currentlyDraggedItemIndex, index);
            listOfUIItems[index].Select();
        }

        private void ResetDraggedItem()
        {
            mouseFollower.Toggle(false);
            currentlyDraggedItemIndex = -1;
        }

        private void HandleBeginDrag(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;//如果点击的ItemUI不在列表里，返回

            currentlyDraggedItemIndex = index;
            listOfUIItems[index].Select();//只选中而不显示描述
            //HandleItemSelection(inventoryItemUI);
            OnStartDragging?.Invoke(index);
        }

        

        private void HandleItemSelection(UIInventoryItem inventoryItemUI)
        {
            int index = listOfUIItems.IndexOf(inventoryItemUI);
            if (index == -1)
                return;

            OnDescriptionRequested?.Invoke(index);
        }

        

        //取消选择所有物品
        private void DeselectAllItems()
        {
            foreach (UIInventoryItem item in listOfUIItems)
            {
                item.Deselect();
            }
            actionPanel.Toggle(false);
        }
        #endregion
    }
}