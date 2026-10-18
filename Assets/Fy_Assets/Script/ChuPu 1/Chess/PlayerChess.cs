using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerChess : FatherChess
{
    public static PlayerChess Instance;
    public int num;
    public Text show;
    private int DownNum;
    private void Start()
    {
        Instance = this;
        DownNum = 0;
    }
    private void Update()
    {
        if (DownNum == 5) 
        {
            GetAllNum();//投了五个之后直接结算
            DownNum = 10;
        }
    }
    public void HasDownButton()
    {
        DownNum++;
    }
    public void GetAllNum()
    {
        num = GetChessNum();
        show.text = num.ToString();
    }
    /// <summary>
    /// 按钮重投
    /// </summary>
    public void TakeAllAgain()
    {
        if (!CanTakeAllAgain)
        {
            //interactable
            return;
        }
        for (int i = 0; i < 5; i++) 
        {
            allChess[i].PlayerStartToGet();//全部重投
        }
        num = GetChessNum();//重新获取分数
        show.text = num.ToString();
    }

}
