using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTakeEvent : MonoBehaviour
{

    //public bool UseTrigger = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "45" && collision.gameObject.name == "Player")
        {
            if (gameObject.name == "寻找食肆事件完成器")
            {
                SystemEventManager.instance.SomeOneHasDown(2);
            }
            else if (gameObject.name == "Square37")
            {
                SystemEventManager.instance.SomeOneHasDown(5);
            }
            else if (gameObject.name == "Car_Father Variant")
            {
                SystemEventManager.instance.SomeOneHasDown(8);
                //寻找食肆任务(2)完成
            }
        }
    }
    public void OverEvent(int num)
    {
        SystemEventManager.instance.SomeOneHasDown(num);
    }
}
