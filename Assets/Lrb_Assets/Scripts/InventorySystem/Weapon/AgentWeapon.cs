using UnityEngine;
using Inventory.Model;
using System.Collections.Generic;


public class AgentWeapon : MonoBehaviour
{
    [SerializeField]
    private EquippableItemSO weapon;

    [SerializeField]
    private InventorySO inventoryData;

    [SerializeField]
    private List<ItemParameter> parametersToModify, itemCurrentState;

    //记录：背包里有两把武器，装备武器时，一是把装备选择的武器，二是把装备的武器进行数值的修改
    //三，若是EquippableItemSO weapon已经有对象了，则把它放回背包并进行行为一二
    public void SetWeapon(EquippableItemSO weaponItemSO, List<ItemParameter> itemState)
    {
        if (weapon != null)
        {
            inventoryData.AddItem(weapon, 1, itemCurrentState);
        }

        this.weapon = weaponItemSO;
        this.itemCurrentState = new List<ItemParameter>(itemState);
        ModifyParameters();
    }

    //修改参数
    private void ModifyParameters()
    {
        foreach(var parameter in parametersToModify)//遍历要修改的参数
        {
            if (itemCurrentState.Contains(parameter))//如果当前状态包含该参数
            {
                int index = itemCurrentState.IndexOf(parameter);
                float newValue = itemCurrentState[index].value + parameter.value;
                itemCurrentState[index] = new ItemParameter
                {
                    itemParameter = parameter.itemParameter,
                    value = newValue
                };
            }
        }
    }
}
