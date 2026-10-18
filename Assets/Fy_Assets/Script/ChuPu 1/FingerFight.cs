using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FingerFight : MonoBehaviour
{
    private FingerType playerFinger;
    private FingerType enemyFinger;
    [Header("胜率设置（总和应小于1），败率是1-胜-平")]
    [LabelText("玩家胜率")]
    public float winFloat = 0.45f; // 玩家胜率
    [LabelText("玩家平局率")]
    public float drawFloat = 0.30f; // 平局率
    public Image playerImage;
    public Image enemyImage;
    public GameObject choice;
    Animator playerAni, enemyAni;
    // 三个可在 Inspector 中挂接的 UnityEvent：玩家胜、玩家负、平局
    public UnityEvent onPlayerWin;
    public UnityEvent onPlayerLose;
    public UnityEvent onDraw;

    private void Start()
    {
        playerAni = playerImage.GetComponent<Animator>();
        enemyAni = enemyImage.GetComponent<Animator>();
        playerImage.gameObject.SetActive(false);
        enemyImage.gameObject.SetActive(false);
    }
    /// <summary>
    /// 进行石头剪刀布的对比
    /// </summary>
    /// <param name="playerFinger">我方出的拳脚</param>
    /// <param name="enemyFinger">敌方出的拳脚</param>
    /// <returns>1: 我方胜, 0: 平局, -1: 我方输</returns>
    private int PK_Finger(FingerType playerFinger, FingerType enemyFinger)
    {
        if (playerFinger == enemyFinger)
        {
            //平局
            return 0;
        }
        if(playerFinger == FingerType.Bu)
        {
            if(enemyFinger == FingerType.JianDao)
            {
                //我方输了
                return -1;
            }
            else
            {
                //我方赢了
                return 1;
            }
        }
        else if(playerFinger == FingerType.JianDao)
        {
            if(enemyFinger == FingerType.QuanTou)
            {
                //我方输了
                return -1;
            }
            else
            {
                //我方赢了
                return 1;
            }
        }
        else //(playerFinger == FingerType.QuanTou)
        {
            if (enemyFinger == FingerType.Bu)
            {
                //我方输了
                return -1;
            }
            else
            {
                //我方赢了
                return 1;
            }
        }//(playerFinger == FingerType.QuanTou)
    }
    /// <summary>
    /// 修改石头剪刀布图片
    /// </summary>
    /// <param name="playerFinger"></param>
    /// <param name="enemyFinger"></param>
    public void ShowFingerImage(FingerType playerFinger, FingerType enemyFinger)
    {
        playerImage.gameObject.SetActive(true);
        enemyImage.gameObject.SetActive(true);
        if (playerFinger == FingerType.QuanTou)
        {
            playerImage.sprite = SimpleFactory.FindImageChuPu("QuanTou");
        }
        else if(playerFinger == FingerType.Bu)
        {
            playerImage.sprite = SimpleFactory.FindImageChuPu("Bu");
        }
        else //(playerFinger == FingerType.JianDao)
        {
            playerImage.sprite = SimpleFactory.FindImageChuPu("JianDao");
        }
        //playerAni.CrossFade("Null", 0);
        playerAni.CrossFade("Show", 0.1f);
        if (enemyFinger == FingerType.QuanTou)
        {
            enemyImage.sprite = SimpleFactory.FindImageChuPu("QuanTou");
        }
        else if(enemyFinger == FingerType.Bu)
        {
            enemyImage.sprite = SimpleFactory.FindImageChuPu("Bu");
        }
        else //(enemyFinger == FingerType.JianDao)
        {
            enemyImage.sprite = SimpleFactory.FindImageChuPu("JianDao");
        }
        //enemyAni.CrossFade("Null", 0);
        enemyAni.CrossFade("Show", 0.1f);
        choice.SetActive(false);
    }
    /// <summary>
    /// 开始一次石头剪刀布（用于 UI Button 的 OnClick）
    /// 随机给 playerFinger 和 enemyFinger 赋值，保证玩家胜率略大。
    /// 胜/平/负分别触发 onPlayerWin/onDraw/onPlayerLose。
    /// </summary>
    public void StartFingerFight(int The_playerFinger)
    {
        playerFinger = (FingerType)The_playerFinger;
        // 随机决定敌方出拳，带有略微偏向玩家胜利的概率分配：
        // 胜率 45%，平局 30%，失败 25%（可根据需要调整）
        float r = Random.value;
        if (r < winFloat) 
        {
            // 选择会输给玩家的手
            enemyFinger = GetLosingFinger(playerFinger);
        }
        else if (r < winFloat + drawFloat) 
        {
            // 平局
            enemyFinger = playerFinger;
        }
        else
        {
            // 选择能赢玩家的手
            enemyFinger = GetWinningFinger(playerFinger);
        }

        // 判断结果并触发对应事件
        int result = PK_Finger(playerFinger, enemyFinger);
        ShowFingerImage(playerFinger, enemyFinger);
        StartCoroutine(LastEnd(result));
    }
    public GameObject EndWinUI;
    public GameObject EndLoseUI;
    IEnumerator LastEnd(int result)
    {
        yield return new WaitForSeconds(1);
        playerImage.gameObject.SetActive(false);
        enemyImage.gameObject.SetActive(false);
        if (result >= 0)
        {
            EndWinUI.SetActive(true);
        }
        else
        {
            EndLoseUI.SetActive(true);
        }
        yield return new WaitForSeconds(1);
        if (result > 0)
        {
            EndWinUI.SetActive(false);
            onPlayerWin?.Invoke();

        }
        else if (result < 0)
        {
            EndLoseUI.SetActive(false);
            onPlayerLose?.Invoke();

        }
        else
        {
            EndWinUI.SetActive(false);
            onDraw?.Invoke();

        }
        yield break;
    }
    IEnumerator OpenUI(GameObject it)
    {
        yield return new WaitForSeconds(1);

    }
    // 返回能打败 player 的手
    private FingerType GetWinningFinger(FingerType player)
    {
        switch (player)
        {
            case FingerType.Bu: return FingerType.QuanTou;    // 石头胜布
            case FingerType.QuanTou: return FingerType.JianDao; // 剪刀胜石头
            case FingerType.JianDao: return FingerType.Bu;    // 布胜剪刀
            default:
                Debug.Log("返回失败");
                return FingerType.Bu;
        }
    }

    // 返回会被 player 打败的手
    private FingerType GetLosingFinger(FingerType player)
    {
        switch (player)
        {
            case FingerType.Bu: return FingerType.JianDao;    // 剪刀输给布
            case FingerType.QuanTou: return FingerType.Bu;    // 布输给石头
            case FingerType.JianDao: return FingerType.QuanTou; // 石头输给剪刀
            default:
                Debug.Log("返回失败");
                return FingerType.Bu;
        }
    }
}

public enum FingerType
{
    QuanTou,//石头
    JianDao,//剪刀
    Bu,//布
}
