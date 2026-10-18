using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectTask : MonoBehaviour
{
    public SystemEvent NowEvent;
    //public bool Choise;
    public SystemEvent RunningEvent;
    //private Text ShowRunningText;
    //private Text ShowName;
    private TextMeshProUGUI ShowContent;
    private Button ShowButton;
    public GameObject AllShowGO; //不是自己，自己当是一个管理者
    public GameObject Searching, ToShearch;
    public GameObject ShowSearchWhat;

    private Coroutine _fadeCoroutine;

    //private Text ShowGet;
    //private Text ShowTransform;
    private void Start()
    {
        Text[] it= AllShowGO.GetComponentsInChildren<Text>();
        for(int i = 0; i < it.Length; i++)
        {
            if (it[i].name == "ShowName")
            {
                //ShowName = it[i];
            }
            else if (it[i].name == "ShowContent")
            {
                //ShowContent = it[i];
            }
            else if (it[i].name == "ShowRunning")
            {
                //ShowRunningText = it[i];
            }
        }
        ShowContent = AllShowGO.GetComponentInChildren<TextMeshProUGUI>();
        Button[] itt = AllShowGO.GetComponentsInChildren<Button>();
        for(int i = 0; i < itt.Length; i++)
        {
            if (itt[i].name == "TakeIn")
            {
                ShowButton = itt[i];
            }
        }
    }

    /// <summary>
    /// 切换点击显示方法
    /// </summary>
    public void ButtonDown()
    {
        //如果显示的是开始追踪
        if (NowEvent != RunningEvent)
        {
            RunningEvent = NowEvent;
            ShowButton.gameObject.SetActive(false);
            Searching.SetActive(true);
            ToShearch.SetActive(false);
            ShowSearchWhat.SetActive(true);
            ShowSearchWhat.GetComponentInChildren<Text>().text = RunningEvent.event_showName + "任务追踪中...";
            //ShowRunningText.text = "追踪中";
        }
        else
        {
            RunningEvent = null;
            ShowButton.gameObject.SetActive(true);
            Searching.SetActive(false);
            ToShearch.SetActive(true);
            ShowSearchWhat.SetActive(false);
            //ShowRunningText.text = "开始追踪";
        }
    }
    public void RunningOver()
    {
        ShowSearchWhat.GetComponentInChildren<Text>().text = RunningEvent.event_showName + "任务完成";
        RunningEvent = null;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _fadeCoroutine = StartCoroutine(waitTimeToSleep());
    }

    IEnumerator waitTimeToSleep()
    {
        if (ShowSearchWhat == null)
            yield break;

        ShowSearchWhat.SetActive(true);

        // 用 CanvasGroup 做淡出（没有就自动加一个）
        var cg = ShowSearchWhat.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = ShowSearchWhat.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        const float duration = 2f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }

        cg.alpha = 0f;
        ShowSearchWhat.SetActive(false);

        // 还原，避免下次直接是透明的
        cg.alpha = 1f;
        _fadeCoroutine = null;
    }

    /// <summary>
    /// 展示页面与修改其中内容，任务按钮被点击时调用
    /// </summary>
    public void ChangeShow(SystemEvent into_event)
    {
        if(NowEvent == into_event)
        {
            NowEvent = null;
            AllShowGO.SetActive(false);
            return;
        }
        AllShowGO.SetActive(true);
        NowEvent = into_event;
        //ShowName.text = NowEvent.event_showName;
        ShowContent.text = "<rotate=90>" + NowEvent.event_content;
        if (NowEvent == RunningEvent)
        {
            Searching.SetActive(true);
            ToShearch.SetActive(false);
            //ShowRunningText.text = "追踪中";
        }
        else 
        {
            Searching.SetActive(false);
            ToShearch.SetActive(true);
            //ShowRunningText.text = "开始追踪"; 
        }
    }
    public void OnEnable()
    {
        NowEvent = null;
        AllShowGO.SetActive(false);
    }
}
