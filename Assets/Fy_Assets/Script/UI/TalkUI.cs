using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TalkUI : MonoBehaviour
{
    public static TalkUI Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        gameObject.SetActive(false);
    }
    [LabelText("玩家图像")]
    public Image spritePlayer;

    /// <summary>
    /// 卖花郎图像
    /// </summary>
    [LabelText("卖花郎图像")]
    public Image spriteMaiHua;

    [LabelText("选项放置的位置")]
    public GameObject content;
    [LabelText("对话内容")]
    public Text contentText;
    [LabelText("说话人名称")]
    public Text contentName;
    public void CloseIt()
    {

    }
    public void TakeCanMove()
    {
        GameObject it = GameObject.FindGameObjectWithTag("Player");
        if (it != null)
        {
            it.GetComponentInParent<PlayerMove>().canMove = true;
        }
    }
    public void TakeCanNotMove()
    {
        GameObject it = GameObject.FindGameObjectWithTag("Player");
        if (it != null)
        {
            it.GetComponentInParent<PlayerMove>().canMove = false;
        }
    }
}
