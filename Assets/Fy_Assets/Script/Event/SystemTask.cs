using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SystemTask : MonoBehaviour
{
    private TextMeshProUGUI content_text;
    private TextMeshProUGUI name_text;
    private Button task_buttton;
    //private Image task_background;
    private Image Zo, Oo, Ll, MoNo, MoOver;

    public SystemTaskManager father;
    public ConnectTask connect;
    public SystemEvent my_event;
    private Image num_image;
    private void Start()
    {
        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == "01") Zo = images[i];
            else if (images[i].name == "11") Oo = images[i];
            else if (images[i].name == "longlong") Ll = images[i];
            else if (images[i].name == "MoNo") MoNo = images[i];
            else if (images[i].name == "MoOver") MoOver = images[i];
            else if (images[i].name == "Num") num_image = images[i];
        }
        if (gameObject.name.Length == 5)
        {
            string aa = gameObject.name[4] + "";
            num_image.sprite = SimpleFactory.FindImageChuPu(aa);
        }
        else
        {
            num_image.sprite = SimpleFactory.FindImageChuPu("10");
        }
        //task_background = GetComponent<Image>();
        task_buttton = GetComponentInChildren<Button>();

        // 用名字查找，避免 GetComponentsInChildren 返回顺序不稳定
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i].name == "name") name_text = tmps[i];
            else if (tmps[i].name == "content") content_text = tmps[i];
        }
    }

    /// <summary>
    /// 从对象池捞出时调用
    /// </summary>
    public void ShowNew(SystemEvent new_event)
    {
        task_buttton.onClick.RemoveAllListeners();
        my_event = new_event;
        if (name_text != null) name_text.text = "<rotate=90>" + my_event.event_showName;
        if (content_text != null) content_text.text = "<rotate=90>" + my_event.event_content;
        TakeShow(my_event.eventHasDown);
        task_buttton.onClick.AddListener(() => { connect.ChangeShow(my_event); });
    }
    public void TakeShow(bool isOver)
    {
        if (isOver)
        {
            MoNo.gameObject.SetActive(false);
            MoOver.gameObject.SetActive(true);
            Ll.gameObject.SetActive(true);
            Zo.gameObject.SetActive(false);
            Oo.gameObject.SetActive(true);
            task_buttton.interactable = false;
        }
        else
        {
            MoNo.gameObject.SetActive(true);
            MoOver.gameObject.SetActive(false);
            Ll.gameObject.SetActive(false);
            Zo.gameObject.SetActive(true);
            Oo.gameObject.SetActive(false);
            task_buttton.interactable = true;
        }
    }
}
