using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class AboutUI : MonoBehaviour
{
    [Header("小菜单相关")]
    [LabelText("需要打开的UI对象")]
    public GameObject Open;
    [LabelText("需要关闭的UI对象")]
    public GameObject Close;
    [LabelText("自动存档的UI对象")]
    public GameObject SaveAuto;

    [Header("翻页相关")]
    [LabelText("每一页内容对象")]
    public List<GameObject> Text;
    [LabelText("当前页数")]
    public int nowIndex = 0;
    [LabelText("当前页数显示")]
    public Text indewNum;
    public GameObject R_button, L_buton;

    [Header("存档覆盖按钮相关")]
    [LabelText("读取按钮对象")]
    public Button ButtonGet;
    [LabelText("覆盖按钮对象")]
    public Button ButtonDown;
    [LabelText("自动存档读取按钮对象")]
    public Button AutoButtonGet;

    //public List<Button> buttons;
    public List<GameObject> findButtons;

    [LabelText("当前选中的存档位")]
    public int SaveOneNum;

    public bool isFileUI;

    private void Start()
    {
        nowIndex = 0;
        //buttons = new List<Button>();
        if (isFileUI)
        {
            for (int i = 0; i < findButtons.Count; i++)
            {
                //buttons.Add(findButtons[i].GetComponentInChildren<Button>());
                //buttons[i].onClick.AddListener(AddDownSave);
                //if (i != 0) buttons[i].onClick.AddListener(() => OpenUI(Open));
               // else buttons[i].onClick.AddListener(() => OpenUI(SaveAuto));
                findButtons[i].GetComponent<SaveOne>().num = i;
                findButtons[i].GetComponent<SaveOne>().father = this;
                findButtons[i].GetComponent<SaveOne>().StartSaveOne();
            }
            if (Text != null)
            {
                //indewNum.text = (nowIndex + 1).ToString() + "/" + Text.Count.ToString();
                for (int i = 0; i < Text.Count; i++)
                {
                    if (i == nowIndex)
                    {
                        Text[i].SetActive(true);
                    }
                    else
                    {
                        Text[i].SetActive(false);
                    }
                }
            }
            this.gameObject.SetActive(false);
        }
    }
    public void ShowOne()
    {
        nowIndex = 0;
        if (Text != null)
        {
            //indewNum.text = (nowIndex + 1).ToString() + "/" + Text.Count.ToString();
            for (int i = 0; i < Text.Count; i++)
            {
                if (i == nowIndex)
                {
                    Text[i].SetActive(true);
                }
                else
                {
                    Text[i].SetActive(false);
                }
            }
        }
    }
    /// <summary>
    /// 下一页
    /// </summary>
    public void NextText()
    {
        int x = nowIndex;
        nowIndex++;
        if (nowIndex >= Text.Count)
        {
            nowIndex = Text.Count - 1;
        }
        if (x == nowIndex)
        {
            return;
        }
        R_button.SetActive(true);
        L_buton.SetActive(true);
        if (nowIndex == 0)
        {
            R_button.SetActive(false);
        }
        else if(nowIndex == SystemEventManager.instance.GetNeedShow().Count/3)
        {
            L_buton.SetActive(false);
        }
            //indewNum.text = (nowIndex + 1).ToString() + "/" + Text.Count.ToString();
            for (int i = 0; i < Text.Count; i++)
        {
            if (i == nowIndex)
            {
                Text[i].SetActive(true);
            }
            else
            {
                Text[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 上一页
    /// </summary>
    public void PrevText()
    {
        int x = nowIndex;
        nowIndex--;
        if (nowIndex < 0)
        {
            nowIndex = 0;
        }
        if (x == nowIndex)
        {
            return;
        }
        R_button.SetActive(true);
        L_buton.SetActive(true);
        if (nowIndex == 0)
        {
            R_button.SetActive(false);
        }
        else if (nowIndex == SystemEventManager.instance.GetNeedShow().Count / 3)
        {
            L_buton.SetActive(false);
        }
        //indewNum.text = (nowIndex + 1).ToString() + "/" + Text.Count.ToString();
        for (int i = 0; i < Text.Count; i++)
        {
            if (i == nowIndex)
            {
                Text[i].SetActive(true);
            }
            else
            {
                Text[i].SetActive(false);
            }
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void DeleteAllGame()
    {
        SaveLoadSystem.Instance.DeleteAllGame();
        TakeAllFile();
    }
    public void DeleteOneGame(int num)
    {
        if (findButtons[num].GetComponent<SaveOne>().nowFile != null)
        {
            if (File.Exists(GetScreenshotFullPath(findButtons[num].GetComponent<SaveOne>().nowFile.otherData.imageName)))
            {
                File.Delete(GetScreenshotFullPath(findButtons[num].GetComponent<SaveOne>().nowFile.otherData.imageName));
            }
        }
        SaveLoadSystem.Instance.DeleteGame(num.ToString());
        TakeAllFile();
    }

    // 读取存档按钮
    public void SetButtonGet(int num)
    {
        CloseAllFileUI();
        // 根据当前选中存档读取
        //SaveLoadSystem.Instance.LoadGame(SaveOneNum.ToString());
        SaveLoadSystem.Instance.LoadGame(num.ToString());
    }

    /// <summary>
    /// 关闭所有存档UI
    /// </summary>
    public void CloseAllFileUI()
    {
        Open.SetActive(false);
        Close.SetActive(false);
        SaveAuto.SetActive(false);
    }

    public void TakeCanMove()
    {
        GameObject it = GameObject.FindGameObjectWithTag("Player");
        if (it != null)
        {
            it.GetComponentInParent<PlayerMove>().canMove = true;
            //it.GetComponent<PlayerMove>().canMove = true;
        }
    }

    public void TakeCanNotMove()
    {
        GameObject it = GameObject.FindGameObjectWithTag("Player");
        if (it != null)
        {
            //it.GetComponent<PlayerMove>().canMove = false;
            it.GetComponentInParent<PlayerMove>().canMove = false;
        }
    }

    // 覆盖存档按钮
    public void SetButtonDown(int num)
    {
        if (num == -1) return;

        // 当前位置信息改为选中位置，并覆盖存档
        // SaveLoadSystem.Instance.DeleteGame(SaveOneNum.ToString());
        SaveLoadSystem.Instance.gameData.Name = num.ToString();
        SaveLoadSystem.Instance.gameData.num = num;
        SaveLoadSystem.Instance.gameData.otherData.ThisTime = DateTime.Now.ToString("yyyy/M/d/H：mm");
        PrintScreen(GetScreenshotDir(), num);
        SaveLoadSystem.Instance.SaveGame();

        // 刷新界面
        TakeAllFile();
    }
    private static string GetScreenshotDir()
    => Path.Combine(Application.persistentDataPath, "SaveScreenshots");
    private static string GetScreenshotFullPath(string fileName)
    => Path.Combine(GetScreenshotDir(), fileName);
    public void TakeSaveOne(SaveOne it, GameData now)
    {
        it.nowFile = now;
        // Debug.Log(now.Name);
        it.playTime.text = now.otherData.ThisTime;
        if(now.otherData.imageName != "no")
        {
            it.image.gameObject.SetActive(true);
            if(File.Exists(GetScreenshotFullPath(now.otherData.imageName)))
            {
                var fullPath = GetScreenshotFullPath(now.otherData.imageName);
                var bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes))
                {
                    Destroy(tex);
                    return;
                }
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                it.image.sprite = sprite;
                it.image.gameObject.SetActive(true);
            }
        }
        else
        {
            it.image.gameObject.SetActive(false);
        }

        
        /*
        int times = (int)now.otherData.playTime;
        times = times / 60;
        if (times >= 0)
            it.playTime.text = times.ToString();
        */
    }

    public void PrintScreen(string path,int num)//需要传入一个保存的路径
    {
        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 0);//渲染一张1920*1080的图
        Camera.main.targetTexture = rt;//传到主摄像机上
        Camera.main.Render();//渲染
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);//读像素
        screenShot.Apply();
        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        Directory.CreateDirectory(path);
        var fileName = $"slot_{num}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var fullPath = Path.Combine(path, fileName);

        
        if (findButtons[num].GetComponent<SaveOne>().nowFile != null)
        {
            if (File.Exists(GetScreenshotFullPath(findButtons[num].GetComponent<SaveOne>().nowFile.otherData.imageName)))
            {
                File.Delete(GetScreenshotFullPath(findButtons[num].GetComponent<SaveOne>().nowFile.otherData.imageName));
            }
        }
        SaveLoadSystem.Instance.gameData.otherData.imageName = fileName;
        
        var bytes = screenShot.EncodeToPNG();//写成JPG，还有另外两种格式
        /*传入的保存路径，也可以直接写死，但是写死不符合一般的需求*/
        File.WriteAllBytes(fullPath, bytes);//在path保存
    }


    // 显示所有存档时使用，给按钮赋值
    public void TakeAllFile()
    {
        List<GameData> datas = SaveLoadSystem.Instance.GetAllSavesGame();
        GameData nulll = new GameData();
        nulll.otherData = new OtherData();
        nulll.otherData.playTime = -100;
        nulll.otherData.ThisTime = " ";
        nulll.otherData.imageName = "no";
        for (int i = 1; i < findButtons.Count; i++)
        {
            TakeSaveOne(findButtons[i].GetComponent<SaveOne>(), nulll);
        }

        for (int i = 1; i < datas.Count; i++)
        {
            int a = int.Parse(datas[i].Name);
            TakeSaveOne(findButtons[a].GetComponent<SaveOne>(), datas[i]);
        }
    }

    /// <summary>
    /// 空白处关闭
    /// </summary>
    /// <param name="it">关闭对象</param>
    public void WhiteCloseUI(GameObject it)
    {
        it.SetActive(false);
    }

    /// <summary>
    /// 按钮点击事件，打开指定UI
    /// </summary>
    public void OpenUI(GameObject it)
    {
        it.SetActive(true);
    }

    /// <summary>
    /// SaveOne被选择时调用
    /// </summary>
    /// <param name="num">序号</param>
    public void ChoiceSaveOne(int num)
    {
        SaveOneNum = num;
        // Debug.Log(SaveOneNum);
        //ButtonGet.onClick.RemoveAllListeners();
        //AutoButtonGet.onClick.RemoveAllListeners();
        if (findButtons[num].GetComponent<SaveOne>().canRead)
        {
            //ButtonGet.onClick.AddListener(SetButtonGet);
            //AutoButtonGet.onClick.AddListener(SetButtonGet);
        }
        else
        {
            Debug.Log("无存档数据，无法读取");
        }
    }

    /// <summary>
    /// 按钮事件：添加覆盖监听
    /// </summary>
    public void AddDownSave()
    {
        //ButtonDown.onClick.RemoveAllListeners();
        //ButtonDown.onClick.AddListener(SetButtonDown);
    }

    public void NewGame()
    {
        AutoStartDialogueOnLoad.ResetPersistentIntroDialogueFlagForNewGame();
        SaveLoadSystem.Instance.gameData.Name = "0";
        SaveLoadSystem.Instance.gameData.num = 0;
        SaveLoadSystem.Instance.WaitSaveGame();
        //SceneManager.LoadScene("PersistentScenes");
        SmoothLoadingScreen.instance.StartLoadingScene("PersistentScenes");
    }
}
