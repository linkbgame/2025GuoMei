using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OneChess : MonoBehaviour
{
    private Image it;
    public bool isWhite;
    private SpriteAnimator ani;
    public Sprite White, Black;
    private void Start()
    {
        it = GetComponent<Image>();
        ani = GetComponent<SpriteAnimator>();
        isWhite = true;
        ani.enabled = false;
    }

    /// <summary>
    /// 使用棋子 按钮或者ai使用
    /// </summary>
    public void PlayerStartToGet()
    {
        GetComponent<Button>().enabled = false;
        PlayerChess.Instance.HasDownButton();
        StartCoroutine(ReadTime());
    }
    public void EnemyStartToGet()
    {
        StartCoroutine(ReadTime());
    }
    IEnumerator ReadTime()
    {
        //it.sprite = SimpleFactory.FindImageChuPu("Reading");
        ani.enabled = true;
        //二分之一随机
        int w = Random.Range(0, 20);
        if (w <= 10)//小于10是白色
        {
            isWhite = true;
        }
        else isWhite = false;//大于10是黑色
        yield return new WaitForSeconds(1);
        ani.enabled = false;
        if (isWhite)
            it.sprite = White;
        else
            it.sprite = Black;
        yield break;
    }
}
