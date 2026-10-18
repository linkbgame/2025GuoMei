using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetItemFader : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        ItemFader[] faders = collision.GetComponentsInChildren<ItemFader>();
        if(faders.Length > 0)
        {
            foreach (var item in faders)
            {
                item.Fadeout();
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        ItemFader[] faders = collision.GetComponentsInChildren<ItemFader>();
        if(faders.Length > 0)
        {
            foreach (var item in faders)
            {
                item.Fadein();
            }
        }
    }
    

}
