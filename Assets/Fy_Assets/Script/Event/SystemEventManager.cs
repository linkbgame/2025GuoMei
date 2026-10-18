using Spine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemEventManager : SingletonMono<ReadDown>
{
    public List<SystemEvent> systemEvents;
    public static SystemEventManager instance;
    private Dictionary<int, bool> EventKey = new Dictionary<int, bool>();

    protected override void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        dialogDataFile = SimpleFactory.FindText("Event");
        ReadText(dialogDataFile);
        GetAllEvents();
    }

    #region 存档相关内容
    void OnEnable()
    {
        EventManager.Instance.AddEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, Load);
    }
    void OnDisable()
    {
        EventManager.Instance.RemoveEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, Load);
    }
    private void Save()
    {
        List<bool> key = SaveLoadSystem.Instance.gameData.eventKey;
        key.Clear();
        for (int i = 1; i <= EventKey.Count; i++) 
        {
            key.Add(EventKey[i]);
        }
        /*
        if (!key.ContainsKey(1))
        {
            Debug.Log(EventKey.Count);
            dialogDataFile = SimpleFactory.FindText("Event");
            ReadText(dialogDataFile);
            GetAllEvents();
        }
        */
        // 更新事件完成程度到数据
        SaveLoadSystem.Instance.gameData.eventKey = key;
    }
    private void Load(GameData gameData)
    {
        if (gameData.eventKey.Count == 0) return;
        for(int i= 1; i <= EventKey.Count; i++)
        {
            EventKey[i] = gameData.eventKey[i - 1];
        }
        //EventKey = gameData.eventKey;
        if (EventKey.Count == 0) return;
        for(int i = 0; i < systemEvents.Count; i++)
        {
            systemEvents[i].eventHasDown = EventKey[systemEvents[i].myID];
        }
    }
    #endregion
    /// <summary>
    /// 给事件类自查用的，全完成则true
    /// </summary>
    /// <param name="m_event"></param>
    /// <returns></returns>
    public bool CheckEventShow(SystemEvent m_event)
    {
        //if (m_event.GetDownContent()) return true;
        int[] s_event = m_event.GetNextEvent();
        for(int i = 0; i < s_event.Length; i++)
        {
            if (!EventKey[s_event[i]]) return false;
        }
        return true;
    }

    /// <summary>
    /// 任意事件完成时调用
    /// </summary>
    /// <param name="event_id">事件序号</param>
    public void SomeOneHasDown(int event_id)
    {
        //Debug.Log(1);
        SystemEvent it = new SystemEvent();
        EventKey[event_id] = true;
        for(int i = 0; i < systemEvents.Count; i++)
        {
            if (systemEvents[i].myID == event_id)
            {
                systemEvents[i].eventHasDown = true;
                it = systemEvents[i];
            }
        }
        SystemTaskManager.Instance.SomeOneHasDown(it);
    }

    /// <summary>
    /// 获取需要展示的事件
    /// </summary>
    /// <returns></returns>
    public List<SystemEvent> GetNeedShow()
    {
        List<SystemEvent> result = new List<SystemEvent>();
        for(int i = 0;i< systemEvents.Count; i++)
        {
            if (systemEvents[i].GetDownContent())
            {
                if (systemEvents[i].eventHasDown)//已经完成的事件放在后面
                {
                    result.Insert(result.Count, systemEvents[i]);
                }
                else
                {
                    result.Add(systemEvents[i]);
                }
            }
        }
        return result;
    }
    public bool LookSomeOneShow(int num)
    {
        if (systemEvents[num-1].GetDownContent())
        {
            return true;
        }
        else
            return false;
    }
    #region 读取事件表格相关
    ///<summary>
    ///事件文本文件，csv格式
    /// </summary>
    private TextAsset dialogDataFile;

    /// <summary>
    /// 按行分割事件
    /// </summary> 
    private string[] dialogRows;

    /// <summary>
    /// 按行拆分整个表格
    /// </summary>
    /// <param name="_textAsset"></param>
    private void ReadText(TextAsset _textAsset)
    {
        dialogRows = _textAsset.text.Split('\n');
        dialogRows[dialogRows.Length - 1] = " ";
    }

    /// <summary>
    /// 构造并加入列表
    /// </summary>
    /// <param name="num"></param>
    /// <param name="show_name"></param>
    /// <param name="EnglishName"></param>
    /// <param name="event_content"></param>
    /// <param name="eventNum"></param>
    private void TakeEvent(int num, string show_name, string EnglishName, string event_content, int[] eventNum)
    {
        SystemEvent m_system = new SystemEvent(num, show_name, EnglishName, event_content, eventNum);
        systemEvents.Add(m_system);
    }

    /// <summary>
    /// 读取表格，初始化调用
    /// </summary>
    private void GetAllEvents()
    {
        if(EventKey.Count != 0)
        {
            return;
        }
        if(systemEvents.Count != 0)
        {
            return;
        }
        for (int i = 1; i < dialogRows.Length; i++)
        {
            if (dialogRows[i] == null || dialogRows[i] == " " )
            {
                return;
            }
            string[] cells = dialogRows[i].Split(',');
            if (cells[1] == null || cells[1]== "")
            {
                return;
            }
            int num = int.Parse(cells[1]);
            TakeEvent(num, cells[0], cells[2], cells[5], GetNumAnd(cells[3]));
            EventKey.Add(num, false);
            //Debug.Log(EventKey.Count);
        }

    }

    /// <summary>
    /// 分割前置事件int方法
    /// </summary>
    /// <param name="numAnd"></param>
    /// <returns></returns>
    private int[] GetNumAnd(string numAnd)
    {
        string[] it = numAnd.Split('+');
        int[] nums = new int[it.Length];
        for(int i = 0; i < it.Length; i++)
        {
            nums[i] = int.Parse(it[i]);
        }
        return nums;
    }
    #endregion
}
