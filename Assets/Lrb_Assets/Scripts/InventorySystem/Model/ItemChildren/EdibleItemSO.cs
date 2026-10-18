using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//消耗型物品


namespace Inventory.Model
{
    //[CreateAssetMenu(fileName = "EdibleItemSO", menuName = "InventorySystem/Item/EdibleItemSO")]
    public class EdibleItemSO : ItemSO, IDestroyableItem, IItemAction
    {
        [SerializeField]
        private List<ModifierData> modifiersData = new List<ModifierData>();//modifiers是复数的修改器

        public string ActionName => "Consume";

        [field: SerializeField]
        public AudioClip ActionSFX { get; private set; }

        public bool PerformAction(GameObject character, List<ItemParameter> itemState = null)
        {
            foreach (ModifierData data in modifiersData)
            {
                data.statModifier.AffectCharacter(character, data.value);
            }
            return true;
        }
    }

    

    [Serializable]
    public class ModifierData
    {
        public CharacterStatModifierSO statModifier;
        public float value;
    }
}

