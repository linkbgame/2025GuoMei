using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatHealthModifierSO", menuName = "InventorySystem/Modifier/CharacterStatHealthModifierSO")]
public class CharacterStatHealthModifierSO : CharacterStatModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        /*Health health = character.GetComponent<Health>();
        if (health != null)
        {
            health.AddHealth((int)val);//强转(转换类型)object
        }*/
    }
}
