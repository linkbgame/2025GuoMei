using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(fileName = "ItemSO", menuName = "InventorySystem/Item/ItemSO")]
    public abstract class ItemSO : ScriptableObject
    {
        [field: SerializeField]
        public bool IsStackable { get; private set; }

        public int ID => GetInstanceID();

        [field: SerializeField]
        public int MaxStackSize { get; private set; } = 1;

        //唯一的String ID，用于外部查找（默认为创建SO时候的名字）
        [field: SerializeField, Header("供外部查找ID名称")]
        public string ItemID { get; private set; }

        // ItemID 没有填写时自动将其设置为 Project 名字
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(ItemID))
            {
                ItemID = this.name;//name 资源文件名（在 Project 视图里显示的名字）
            }
        }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        [field: TextArea]
        public string Description { get; private set; }

        [field: SerializeField]
        public Sprite ItemImage { get; private set; }

        [field: SerializeField]
        public List<ItemParameter> DefaultParametersList { get; set; }

        [field: SerializeField]
        public Vector3 OriginalWorldPos { get; set; }
    }

    //实现了该接口的物品可以被丢弃
    public interface IDroppableItem
    {

    }

    //实现了该接口的物品Action后被-1，被销毁
    public interface IDestroyableItem
    {

    }

    //实现了该接口的物品可以Action
    public interface IItemAction
    {
        public string ActionName { get; }//如果set的话，接口实现类必须实现set方法，试一试
        public AudioClip ActionSFX { get; }

        //接口方法无修饰符同时无方法体，隐式public
        bool PerformAction(GameObject character, List<ItemParameter> itemState);
    }

    //武器SO用到
    [Serializable]
    public struct ItemParameter : IEquatable<ItemParameter>
    {
        public ItemParameterSO itemParameter;
        public float value;

        public bool Equals(ItemParameter other)
        {
            return other.itemParameter == itemParameter;
        }
    }
}
