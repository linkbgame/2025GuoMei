using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SystemEvent
{
    public int myID;
    /// <summary>
    /// 是否在任务栏优先展示该事件
    /// </summary>
    public bool NeedShowEvent;
    /// <summary>
    /// 直接展示的中文名
    /// </summary>
    public string event_showName;
    /// <summary>
    /// 事件英文名
    /// </summary>
    private string event_name;
    /// <summary>
    /// 该任务是否已经完成
    /// </summary>
    public bool eventHasDown;
    /// <summary>
    /// 前置事件序列号，用于在库中查询
    /// </summary>
    public int[] nextEventNum;
    /// <summary>
    /// 事件描述
    /// </summary>
    public string event_content;

    //private string event_gain;
    public int[] GetNextEvent() {  return nextEventNum; }
    /// <summary>
    /// 事件构造
    /// </summary>
    /// <param name="event_name">事件名称</param>
    /// <param name="event_content">事件简介内容</param>
    /// <param name="events">前置条件</param>
    public SystemEvent(int num,string show_name,string event_name,string event_content, int[] nextEventNum)
    {
        myID = num;
        event_showName = show_name;
        eventHasDown = false;
        this.event_content = event_content;
        this.event_name = event_name;
        this.nextEventNum = nextEventNum;
        if(nextEventNum == null || nextEventNum[0] == -1)
            NeedShowEvent = true;
        else
            NeedShowEvent = false;
    }
    public SystemEvent() { }
    /// <summary>
    /// 检测前置是否尽数完成，完成则可显
    /// </summary>
    private void CheckCanShow()
    {
        if (NeedShowEvent) return;
        //if (eventHasDown) return;
        if (SystemEventManager.instance.CheckEventShow(this))
        NeedShowEvent = true;
    }

    /// <summary>
    /// 事件完成时调用
    /// </summary>
    public void TakeThisDown()
    {
        NeedShowEvent = false;
        eventHasDown = true;
    }

    /// <summary>
    /// 获取该事件是否显示，自行检测
    /// </summary>
    /// <returns></returns>
    public bool GetDownContent()
    {
        CheckCanShow();
        if (NeedShowEvent)
        {
            return true;
        }return false;
    }
}
