using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(fileName = "ConsumptiveInteractionItemSO", menuName = "InventorySystem/Item/ConsumptiveInteractionItemSO")]
    public class ConsumptiveInteractionItemSO : ItemSO, IItemAction, IDestroyableItem
    {
        public string ActionName => "Use";

        [field: SerializeField]
        public AudioClip ActionSFX { get; private set; }

        public bool PerformAction(GameObject character, List<ItemParameter> itemState)
        {
            InteractiveSystem interactiveSystem = character.GetComponentInChildren<InteractiveSystem>();
            interactiveSystem.TrySolvePuzzle(Name);
            return true;
        }
    }
}