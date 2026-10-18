using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemTaskManager : MonoBehaviour
{
    public static SystemTaskManager Instance;

    public List<GameObject> m_task ;//任务对象池
    private List<SystemEvent> m_event = new List<SystemEvent>();//事件库
    private int ShowNum;
    private ConnectTask connect;

    [Header("滚动视图 Content (将任务放到此 Transform 下)")]
    public RectTransform taskContent;
    [Header("每项之间的间距（像素）")]
    public float itemSpacing = 5f;
    // 当前已经显示在滚动视图中的任务（按顺序）
    private List<GameObject> m_shownTasks = new List<GameObject>();
    public GameObject task_father;
    public GameObject ShowRed;
    private void Start()
    {
        Instance = this;
        connect = GetComponent<ConnectTask>();
        this.gameObject.SetActive(false);
    }
    /// <summary>
    /// 打开任务栏调用
    /// </summary>
    public void OpenShowingTask()
    {
        PlayerMove player = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerMove>();
        player.canMove = false;
        m_event = SystemEventManager.instance.GetNeedShow();
        ShowNum = m_event.Count;
        ShowRed.SetActive(false);
        if (ShowNum <= 3)
        {
            R_button.SetActive(false);
            L_button.SetActive(false);
        }
        else {
            L_button.SetActive(true); 
            R_button.SetActive(false);
        }

        for (int i = 0; i < ShowNum; i++)
        {
            m_task[i].GetComponent<SystemTask>().ShowNew(m_event[i]);
            m_task[i].SetActive(true);
            TakeTaskToDropdown(m_task[i]);
            //Debug.LogError("显示了事件：" + m_event[i].myID);
        }
        for(int i = ShowNum; i < m_task.Count; i++)
        {
            m_task[i].SetActive(false);
        }
        gameObject.SetActive(true);
    }
    public GameObject R_button, L_button;
    /// <summary>
    /// 将传入的任务放入任务栏并调整好位置，确保位置无误
    /// 会把 task 设为 taskContent 的子对象并按顺序排列（不会覆盖前一项）
    /// </summary>
    /// <param name="task"></param>
    public void TakeTaskToDropdown(GameObject task)
    {
        if (taskContent == null)
        {
            Debug.LogWarning("[SystemTaskManager] taskContent 未设置，无法添加任务到滚动视图。");
            return;
        }

        if (task == null)
        {
            Debug.LogWarning("[SystemTaskManager] 传入的 task 为 null。");
            return;
        }

        // 保证该对象为激活状态（以便能正确测量 RectTransform）
        task.SetActive(true);

        // 将其放入 content 下（保留本地缩放/旋转/尺寸）
        //task.transform.SetParent(taskContent, false);
        
        // 记录显示列表
        m_shownTasks.Add(task);
        /*
        // 计算单项高度（优先使用 RectTransform.rect.height）
        RectTransform rt = task.GetComponent<RectTransform>();
        float itemHeight = 0f;
        if (rt != null)
        {
            itemHeight = rt.rect.height;
            // 如果高度为0（尚未布局），尝试使用 sizeDelta.y
            if (Mathf.Approximately(itemHeight, 0f))
                itemHeight = rt.sizeDelta.y;
        }

        // 如果仍然为0，设为一个安全默认值
        if (Mathf.Approximately(itemHeight, 0f))
            itemHeight = 60f;

        // 计算当前项的 anchoredPosition（从上到下排列）
        // 假定 content 的 pivot 为 (0.5,1) 或常见的从上向下排列：
        // 第0项 y = 0, 第1项 y = -(itemHeight + spacing), 以此类推
        int index = m_shownTasks.Count - 1;
        Vector2 anchoredPos = Vector2.zero;
        anchoredPos.x = 0f;
        anchoredPos.y = -index * (itemHeight + itemSpacing);

        if (rt != null)
            rt.anchoredPosition = anchoredPos;
        else
            task.transform.localPosition = new Vector3(0f, anchoredPos.y, 0f);

        // 调整 content 高度以容纳所有项（保持 x 不变）
        float contentHeight = m_shownTasks.Count * (itemHeight + itemSpacing);
        Vector2 size = taskContent.sizeDelta;
        size.y = contentHeight;
        taskContent.sizeDelta = size;
        */
    }

    /// <summary>
    /// 清空滚动视图的显示（把显示区域移除所有子对象引用）
    /// 但不会删除这些任务 GameObject —— 它们只是从 content 中分离并设为不激活
    /// </summary>
    public void ClearDropdown()
    {
        if (taskContent == null)
        {
            Debug.LogWarning("[SystemTaskManager] taskContent 未设置，无法清空滚动视图。");
            return;
        }

        // 将当前显示的任务从 content 中分离并禁用（保留在对象池或本管理器下）
        for (int i = 0; i < m_shownTasks.Count; i++)
        {
            var task = m_shownTasks[i];
            //if (task == null) continue;

            // 解除父子关系（放到 SystemTaskManager 底下，避免销毁）
            //task.transform.SetParent(task_father.transform, false);
            task.SetActive(false);
        }

        m_shownTasks.Clear();

        // 重置 content 大小
        //Vector2 size = taskContent.sizeDelta;
        //size.y = 0f;
        //taskContent.sizeDelta = size;
    }

    /// <summary>
    /// 关闭任务栏时调用
    /// </summary>
    public void CloseShowingTask()
    {
        ClearDropdown();
        PlayerMove player = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerMove>();
        player.canMove = true;
        m_event.Clear();
        gameObject.SetActive(false);
    }

    public void SomeOneHasDown(SystemEvent ev)
    {
        m_event = SystemEventManager.instance.GetNeedShow();
        if (m_event.Count != ShowNum) 
        {
            ShowRed.SetActive(true);
        }
        if (connect.RunningEvent == ev)
        {
            connect.RunningOver();
        }
    }
    public void TakeNextRead(bool isUp)
    {
        if (isUp)
        {

        }
    }
}
