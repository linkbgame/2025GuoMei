using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyChess : FatherChess
{
    public int num;
    public Text show;
    private void Start()
    {
        //StartToGet();
    }

    /// <summary>
    /// 地方开始投掷
    /// </summary>
    public void StartToGet()
    {
        StartCoroutine(GetAllChess());
    }
    IEnumerator GetAllChess()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 5; i++)
        {
            allChess[i].EnemyStartToGet();
            yield return new WaitForSeconds(0.5f);
        }
        num = GetChessNum();
        show.text = num.ToString();
        yield break;
    }
}
