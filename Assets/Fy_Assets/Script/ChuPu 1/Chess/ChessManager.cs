using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChessManager : MonoBehaviour
{
    private PlayerChess player;
    private EnemyChess enemy;
    [LabelText("玩家获胜事件")]
    public UnityEvent win;
    [LabelText("玩家失败事件")]
    public UnityEvent lose;
    [LabelText("平局事件")]
    public UnityEvent noRe;
    private bool playerOK,enemyOK;
    public GameObject CantHit;
    private void Start()
    {
        player = GetComponent<PlayerChess>();
        enemy = GetComponent<EnemyChess>();
        playerOK = false;
        enemyOK = false;
    }
    private void Update()
    {
        if (playerOK && enemyOK)
        {
            GetWinLose();
            playerOK = false;
            enemyOK = false;
        }
    }
    public void StartEnemyChess()
    {
        enemy.StartToGet();
        StartCoroutine(WaitEnemyEnd());
    }
    IEnumerator WaitEnemyEnd()
    {
        yield return new WaitForSeconds(3.5f);
        enemyOK = true;
        if (!playerOK)
        {
            CantHit.SetActive(false);
        }
    }
    /// <summary>
    /// 先手-事件
    /// </summary>
    public void FightWin()
    {
        PlayerChessStart();
    }
    /// <summary>
    /// 后手-事件
    /// </summary>
    public void FightLose()
    {
        CantHit.SetActive(true);
        StartEnemyChess();
    }
    public void PlayerChessOver()
    {
        for(int i = 0; i < 5; i++)
        {
            if (player.allChess[i].GetComponent<Button>().enabled == true)
            {
                //有没有被点击过的就返回
                return;
            }
        }
        playerOK = true;
        if (!enemyOK)
        {
            StartEnemyChess();
        }
    }
    public void PlayerChessStart()
    {

    }
    /// <summary>
    /// 获得输赢结果
    /// </summary>
    public void GetWinLose()
    {
        if (player.num > enemy.num)
        {
            Debug.Log("win");
            win?.Invoke();
        }
        else if (player.num < enemy.num)
        {
            Debug.Log("lose");
            lose?.Invoke();
        }
        else
        {
            Debug.Log("平局");
            noRe?.Invoke();
        }
    }
}
