using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//交互物品不会被消耗，预留丢弃

namespace Inventory.Model
{
    [CreateAssetMenu(fileName = "InteractiveItemSO", menuName = "InventorySystem/Item/InteractiveItemSO")]
    public class InteractiveItemSO : ItemSO, IItemAction
    {
        public string ActionName => "Interact";

        [field: SerializeField]
        public AudioClip ActionSFX { get; private set; }

        //执行行为不会销毁该物品
        public bool PerformAction(GameObject character, List<ItemParameter> itemState)
        {
            InteractiveSystem interactiveSystem = character.GetComponentInChildren<InteractiveSystem>();
            interactiveSystem.TrySolvePuzzle(Name);
            return true;
        }
    }
}