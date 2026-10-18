using Inventory;
using Inventory.Model;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// 图像相关改成存名字，根据名字分配索引
/// </summary>
public class Fy_DialogManager : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    private DialogueClueBridge dialogueClueBridge;
    ///<summary>
    ///对话文本文件，csv格式
    /// </summary>
    private TextAsset dialogDataFile;

    /// <summary>
    /// 玩家图像
    /// </summary>
    [LabelText("玩家图像")]
    public Image spritePlayer;

    /// <summary>
    /// 卖花郎图像
    /// </summary>
    [LabelText("卖花郎图像")]
    public Image spriteMaiHua;

    /// <summary>
    /// 当前对话索引值
    /// </summary>
    [LabelText("当前对话索引值int")]
    private int dialogIndex;

    /// <summary>
    /// 对话文本按行分割
    /// </summary> 
    public string[] dialogRows;

    public bool isChoice;

    private bool isOver;

    [Header("对话结束后x秒运行此事件")]
    public UnityEvent END;
    public float x;
    [LabelText("选项放置的位置")]
    public GameObject content;
    [LabelText("对话内容")]
    public Text contentText;
    [LabelText("说话人名称")]
    public Text contentName;

    public List<ItemSO> allofBagSth;


    /// <summary>
    /// 当前是否正在对话。对外只读，仅 Fy_DialogManager 内部可修改。
    /// 供观察模式等系统判断"对话进行中应屏蔽其它玩法输入"。
    /// </summary>
    public bool IsTalking { get; private set; }
    #region 附加功能委托
    private delegate void m_delegate();
    private m_delegate afterTalk;//每句话显示完成后调用
    private m_delegate afterEnd;//对话完成后调用
    /// <summary>
    /// 物品添加用于委托
    /// </summary>
    /// <param name="name"></param>
    /// <param name="num"></param>
    private void AddSomething(string name, int num)
    {
        Debug.LogError("背包");
        if (name == "筹码")
        {
            InventoryController.Instance.AddItemToInventory(ItemEnum.筹码, num);
            Debug.Log("添加了筹码");
        }
        else
        {
            InventoryController.Instance.AddItemToInventory(FindItemByName(name), num);
        }
    }
    /// <summary>
    /// 物品交付用于按键监听
    /// </summary>
    /// <param name="name"></param>
    /// <param name="num"></param>
    private void NeedGiveSth(string name, int num)
    {

    }
    /// <summary>
    /// 线索添加用于委托
    /// </summary>
    /// <param name="name"></param>
    private void AddClueLink(string name)
    {
        Debug.LogError("线索版");
        string key = name.Trim();

        EClueLink clueLink = key switch
        {
            "线索1" or "线索一" => EClueLink.线索一,

            "线索2" or "线索二" => EClueLink.线索二,
            "提示2" or "提示二" => EClueLink.提示二,

            "线索3" or "线索三" => EClueLink.线索三,
            "提示3" or "提示三" => EClueLink.提示三,

            "线索4" or "线索四" => EClueLink.线索四,
            "提示4" or "提示四" => EClueLink.提示四,

            "真线索1" or "真线索一" => EClueLink.真线索一,
            "真线索3" or "真线索三" => EClueLink.真线索三,
            "真线索4" or "真线索四" => EClueLink.真线索四,

            "提示5" or "提示五" => EClueLink.提示五,

            "花车线索一" or "花车线索1" => EClueLink.花车线索一,
            "花车线索二" or "花车线索2" => EClueLink.花车线索二,
            "花车线索三" or "花车线索3" => EClueLink.花车线索三,
            "客栈内线索一" or "客栈内线索1" => EClueLink.客栈内线索一,
            "客栈内线索二" or "客栈内线索2" => EClueLink.客栈内线索二,
            "调查线索一" or "调查线索1" => EClueLink.调查线索一,
            "调查线索三" or "调查线索3" => EClueLink.调查线索三,
            "调查线索二" or "调查线索2" => EClueLink.调查线索二,
            "食肆线索一" or "食肆线索1" => EClueLink.食肆线索一,
            "食肆线索二" or "食肆线索2" => EClueLink.食肆线索二,

            "卖花郎" => EClueLink.卖花郎,
            
            "客栈老板" => EClueLink.客栈老板,
            
            "调查结论一" or "调查结论1" => EClueLink.调查结论一,
            "调查结论二" or "调查结论2" => EClueLink.调查结论二,
            
            "客栈内结论" => EClueLink.客栈内结论,

            _ => EClueLink.None
        };

        if (clueLink == EClueLink.None)
        {
            Debug.LogWarning($"[AddClueLink] 未匹配的线索名: {name}");
            return;
        }

        dialogueClueBridge.OnDialogueEvent(clueLink);
    }
    /// <summary>
    /// 小游戏进入用于对话结束委托
    /// </summary>
    /// <param name="GameName"></param>
    private void IntoNewGame(string GameName)
    {
        SaveLoadSystem.Instance.SaveGame();
        if (GameName == "樗蒲")
            SmoothLoadingScreen.instance.StartLoadingScene("ChuPu");
        else
        if (GameName == "拼图_花车")
            SmoothLoadingScreen.instance.StartLoadingScene("PinTu_HuaChe");
        else
        if (GameName == "拼图_酒楼")
            SmoothLoadingScreen.instance.StartLoadingScene("PinTu_JiuLou");
        else
        if (GameName == "拼图_房子")
            SmoothLoadingScreen.instance.StartLoadingScene("PinTu_House");
        else
            SmoothLoadingScreen.instance.StartLoadingScene(GameName);
    }
    #endregion
    #region 逐字显示
    private float wordInterval = 0.037f; // 每个字出现的间隔时间
    private bool isReading;
    private string nowText;
    /// <summary>
    /// 开始逐字显示文本
    /// </summary>
    /// <param name="text"></param>
    private void StartReadText(string text)
    {
        int num = FindChText(text);
        if (num != -1)
        {
            nowText = text.Substring(0, num) + NowChText[0];
            if(num + 1 < text.Length)
            {
                nowText += text.Substring(num + 1);
            }
        }
        else
        nowText = text;
        StartCoroutine(ShowTextByWord());
    }
    private IEnumerator ShowTextByWord()
    {
        isReading = true;
        contentText.text = "";
        string showText = null;
        nowText += "  ";
        for (int i = 0; i < nowText.Length; i++) 
        {
            showText += nowText[i];
            if (i - 2 > 0)//1
            {
                contentText.text = showText.Substring(0,i - 1)
                    + "<color='#000000AA'>" + showText[i - 1] + "</color>"
                    + "<color='#00000055'>" + showText[i] + "</color>";
            }
            else if (i - 1 > 0)//0.6
            {
                contentText.text =
                    "<color='#000000AA'>" + showText[i - 1] + "</color>"
                    + "<color='#00000055'>" + showText[i] + "</color>";
            }
            else
            {
                contentText.text = "<color='#00000055'>" + showText[i] + "</color>";
            }
            yield return new WaitForSeconds(wordInterval); // 每个字出现的间隔时间
        }
        isReading = false;
        if (afterTalk != null)
            afterTalk();
        yield break;
    }
    /// <summary>
    /// 结束阅读直接显示
    /// </summary>
    private void OverReadText()
    {
        if (isChoice) return;
        StopAllCoroutines();
        contentText.text = nowText;
        isReading = false;
        if (afterTalk != null)
            afterTalk();
    }
    #endregion
    public IEnumerator End(float num)
    {
        yield return new WaitForSeconds(num);
        END?.Invoke();
        //Debug.LogError(11);
        if(afterEnd != null)
            afterEnd();
        yield break;
    }
    private void Awake()
    {
        StartCoroutine(StartInTake());
        dialogueClueBridge = GetComponent<DialogueClueBridge>();
    }
    IEnumerator StartInTake()
    {
        yield return  new WaitForSeconds(0.1f);
        TakeTheTakeUI();
    }
    private void TakeTheTakeUI()
    {
            if (spriteMaiHua == null)
            {
                spriteMaiHua = TalkUI.Instance.spriteMaiHua;
            }
            if (spritePlayer == null)
            {
                spritePlayer = TalkUI.Instance.spritePlayer;
            }
            if (content == null)
            {
                content = TalkUI.Instance.content;
            }
            if (contentText == null)
            {
                contentText = TalkUI.Instance.contentText;
            }
            if (contentName == null)
            {
                contentName = TalkUI.Instance.contentName;
            }
    }
    /// <summary>
    /// 物品添加时根据名字寻找物品
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private ItemSO FindItemByName(string name)
    {
        for (int i = 0; i < allofBagSth.Count; i++)
        {
            if (allofBagSth[i].Name == name)
            {
                return allofBagSth[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 对话结束分支：恢复 NPC 与玩家移动。AutoStartDialogueOnLoad 等未经过 Finish Trigger 时 ShowRoadUI.player 可能为 null，需判空。
    /// </summary>
    private void RestoreMovementAfterDialogueEnds()
    {
        NPCMovement npcMovement = GetComponent<NPCMovement>(); // 取得本物体上的 NPCMovement（代理对话体上可能没有该组件）
        if (npcMovement != null) // 仅当组件存在时才启用，避免对 null 访问 .enabled
            npcMovement.enabled = true; // 与原先逻辑一致：对话结束后重新允许 NPC 移动

        ShowRoadUI showRoadUI = GetComponent<ShowRoadUI>(); // 取得本物体上的 ShowRoadUI（未挂载则为 null）
        if (showRoadUI != null && showRoadUI.player != null) // player 仅在带 Finish 的 Trigger 停留时赋值；自动开场对话常未赋值
            showRoadUI.player.canMove = true; // 与原先逻辑一致：对话结束后重新允许玩家移动
    }

    private void Update()
    {
        if (!IsTalking) { return; }
        if (Input.GetMouseButtonUp(0))
        {
            if (isChoice)
                return;
            else if (isReading)
            {
                OverReadText();
            }
            else if (isOver)
            {
                //Debug.Log("对话结束"); // 原日志保留，未改语义
                RestoreMovementAfterDialogueEnds(); // 替换原先两行 GetComponent：内部判空，覆盖无 NPC 交互 / 自动对话
                StartCoroutine(End(x)); // 原协程调用保留，未改语义
            }
            else OnClickNext();
        }
    }
    #region 初始化调用
    /// <summary>
    /// 外部调用，初始化并开启对话
    /// </summary>
    /// <param name="it"></param>
    public void TakeMyText(TextAsset it)
    {
        TakeTheTakeUI();
        afterEnd = null;
        dialogDataFile = SimpleFactory.FindText(it.name);
        isOver = false;
        TakeStart();
    }
    public void TakeStart()
    {
        content.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
        content.GetComponentInChildren<Button>().onClick.AddListener(OverText);
        dialogIndex = 0;
        IsTalking = true;
        TalkUI.Instance.gameObject.SetActive(true);
        TalkUI.Instance.TakeCanNotMove();
        buttonText = new List<string>();
        NowChText = new List<string>();
        ReadText(dialogDataFile);
        //now = new Vector3(0,0, 0);
        OnClickNext();
        //HeadOriginalScale = 1;
    }
    public void ReadText(TextAsset _textAsset)
    {
        dialogRows = _textAsset.text.Split('\n');
        dialogRows[dialogRows.Length - 1] = null;
    }
    #endregion

    /// <summary>
    /// 挂载END
    /// </summary>
    public void OverText()
    {
        //Debug.Log("播放UI消失动画");
        NPCTakeSave it = GetComponent<NPCTakeSave>();
        if (it != null) 
            GetComponent<NPCTakeSave>().EndTalk();
        TalkUI.Instance.TakeCanMove();
            TalkUI.Instance.gameObject.SetActive(false);
        IsTalking = false;
    }
    //[LabelText("头像弹出比例")]
    //public float HeadPopUpRate;
    //private float HeadOriginalScale;

    /// <summary>
    /// 对话头像弹出
    /// </summary>
    /// <param name="_name"></param>
    public void UpdateImage(string _name)
    {

        if (_name == "白泽")//主角在右边
        {
            //spriteLeft.color = new Color(255, 255, 255);
            spritePlayer.gameObject.SetActive(true);
            spriteMaiHua.gameObject.SetActive(false);
        }
        else if(_name == "卖花郎")//卖花郎在左边
        {
            //spriteLeft.color = new Color(73, 73, 73);
            spriteMaiHua.gameObject.SetActive(true);
            spritePlayer.gameObject.SetActive(false);
        }
        else
        {
            spriteMaiHua.gameObject.SetActive(false);
            spritePlayer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 有人说话调用
    /// </summary>
    /// <param name="_name">名字</param>
    /// <param name="_text">文本内容</param>
    public void UpdateText(string _name, string _text)
    {
        if (_name != "缇戴尔")//留着的其他选项
        {
            StartReadText(_text);
            contentName.text = _name;
        }
    }
    /// <summary>
    /// 存在选项时生成的按钮对象，避免重复生成
    /// </summary>
    private GameObject choice;
    /// <summary>
    /// 遍历文本寻找当前索引对应行，进行相应操作
    /// </summary>
    public void ShowDialogRow()
    {
        for (int i = 1; i < dialogRows.Length; i++)
        {
            if (dialogRows[i] == null || dialogRows[i] == "")
            {
                break;
            }
            string[] cells = dialogRows[i].Split(',');

            //Debug.Log(dialogRows[i]);
            if (cells[1] == dialogIndex.ToString() && cells[0] == "#")
            {
                afterTalk = null;//重置委托，防止之前的对话内容对后续对话造成影响
                //判断是否有物品添加
                if (cells[6] != "")
                    afterTalk += delegate { AddSomething(cells[6], int.Parse(cells[7])); };//物品信息添加到委托中，等这句话结束时执行
                if (cells[8] != "")
                    afterTalk += delegate { AddClueLink(cells[8]); };//线索信息添加到委托中，等这句话结束时执行
                if (cells[9] != "")
                    afterEnd = delegate { IntoNewGame(cells[9]); };//小游戏信息添加到结束委托中，等对话结束时和END一起执行
                UpdateText(cells[3], cells[4]);//传入名字和内容
                UpdateImage(cells[3]);//传入名字修改图像
                dialogIndex = int.Parse(cells[5]);//当前序号
                break;
            }
            else if (cells[0] == "&" && cells[1] == dialogIndex.ToString())
            {
                if (choice != null)
                {
                    Debug.Log(11111);
                    break;
                }
                //运行按钮生成 补充问答状态不会读取
                choice = SimpleFactory.CreateModel("ChoiceTalk", content.transform);
                Button[] buttons = choice.GetComponentsInChildren<Button>();
                if (buttonText != null)
                    buttonText.Clear();
                if (NowChText != null)
                    NowChText.Clear();
                isChoice = true;
                GenerateOption(i, buttons, 0);
                break;
            }
            else if (cells[0] == "#END" && cells[1] == dialogIndex.ToString())
            {
                afterTalk = null;//重置委托，防止之前的对话内容对后续对话造成影响
                if (cells[4] == "")
                {
                    Debug.Log("对话结束"); // 原日志保留，未改语义
                    RestoreMovementAfterDialogueEnds(); // 与 Update 中 isOver 分支相同：#END 空内容直接结束也做安全恢复
                    StartCoroutine(End(x)); // 原协程调用保留，未改语义
                    break; // 原流程：跳出 for，未改语义
                }
                //判断是否有物品添加
                if (cells[6] != "")
                    afterTalk += delegate { AddSomething(cells[6], int.Parse(cells[7])); };//物品信息添加到委托中，等这句话结束时执行
                if (cells[8] != "")
                    afterTalk += delegate { AddClueLink(cells[8]); };//线索信息添加到委托中，等这句话结束时执行
                if (cells[9] != "")
                    afterEnd = delegate { IntoNewGame(cells[9]); };//小游戏信息添加到结束委托中，等对话结束时和END一起执行
                UpdateText(cells[3], cells[4]);//传入名字和内容
                UpdateImage(cells[3]);//传入名字修改图像
                dialogIndex = int.Parse(cells[5]);//当前序号
                isOver = true;
                break;
            }
        }
    }
    /// <summary>
    /// 点击下一个
    /// </summary>
    public void OnClickNext()
    {
        ShowDialogRow();
    }
    /// <summary>
    /// 按钮内容，生成函数前重制过
    /// </summary>
    private List<string> buttonText;
    /// <summary>
    /// 生成选项按钮  
    /// </summary>
    /// <param name="_index">文案id索引</param>
    /// <param name="_button">需要加监听的按钮们</param>
    /// <param name="now">已经监听的按钮数量计数</param>
    public void GenerateOption(int _index, Button[] _button, int now)
    {
        string[] cells = dialogRows[_index].Split(',');

        if (cells[0] == "&")
        {
            if (now == 2)//代表第三个选项位置是否显示
            {
                _button[2].gameObject.SetActive(true);
            }
            if(now == 3)
            {
                _button[3].gameObject.SetActive(true);
            }
            buttonText.Add(cells[4]);
            NowChText.Add(cells[12]);
            if(cells[12] != "")
                _button[now].gameObject.GetComponentInChildren<Text>().text = cells[12];
            else
                _button[now].gameObject.GetComponentInChildren<Text>().text = cells[4];
            _button[now].onClick.AddListener(delegate { OnOptionClick(int.Parse(cells[5]), now); });
            if (cells[10] != "")
                _button[now].onClick.AddListener(delegate { NeedGiveSth(cells[10], int.Parse(cells[11])); });
            //此处适合添加物品交付监听
            if (now < 2)
            {
                _button[2].gameObject.SetActive(false);
            }
            if(now != 3)
            {
                _button[3].gameObject.SetActive(false);
            }
            GenerateOption(_index + 1, _button, now + 1);
        }
    }
    /// <summary>
    /// 选项后文本内容赋值
    /// </summary>
    /// <param name="_id"></param>
    /// <param name="num"></param>
    public void OnOptionClick(int _id, int num)
    {
        UpdateImage("player");
        dialogIndex = _id;//记录该选项对应的文本索引，即下次点击时从这个索引开始读取文本
        StartReadText(buttonText[num]);
        NowChText[0] = NowChText[num];
        contentName.text = "白泽";
        Destroy(choice);//销毁选项
        StartCoroutine(Stop());
    }
    private List<string> NowChText;
    private int FindChText(string it)
    {
        for(int i = 0; i < it.Length; i++)
        {
            if (it[i] == '@')
            {
                return i;
            }
        }
        return -1;
    }
    /// <summary>
    /// 延时意义不明，暂时放在这里
    /// </summary>
    /// <returns></returns>
    public IEnumerator Stop()
    {
        yield return new WaitForSeconds(0.1f);
        isChoice = false;
    }

}
