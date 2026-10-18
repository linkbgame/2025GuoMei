using Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JustOne1 : MonoBehaviour
{
    public GameObject car;
    public NPCTakeSave npc;
    private bool ok;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //����¼�7�Ի�·�˼׼����ˣ�����Դ�������
        if (SystemEventManager.instance.LookSomeOneShow(7))
        {
            if (collision.CompareTag("Finish"))
                npc.StartTalk();
            //Debug.LogError("������");
        }
        //Debug.LogError(SystemEventManager.instance.LookSomeOneShow(7));
    }
    private void Update()
    {
        if (!ok)
        {
            if (InventoryController.Instance.GetItemQuantityByID("ChouMa") > 37) 
            {
                SystemEventManager.instance.SomeOneHasDown(3);
            }

            //�����Ѽ���ɼ��
        }
    }
    private void Start()
    {
        ok = false;
        StartCoroutine(FindCar());
        //car = GameObject.FindGameObjectWithTag("FlowerCar");
    }
    public void StartCar()
    {
        SystemEventManager.instance.SomeOneHasDown(7);
        BGMManager.Instance.PlayBGMByClipName("花车惊变");
        //Debug.LogError("1212");
        car.SetActive(true);
        //car.GetComponent<CarMove>().PlayFromBeginning();
        car.GetComponentInChildren<CarMove>().PlayFromBeginning();
        NpcScriptedMove[] npcScriptedMove = car.GetComponentsInChildren<NpcScriptedMove>();
        foreach (var item in npcScriptedMove)
        {
            item.Activate();
        }
        Destroy(gameObject);
    }
    IEnumerator FindCar()
    {
        yield return new WaitForSeconds(2f);
        if (car == null)
        {
            
            car = GameObject.FindGameObjectWithTag("FlowerCar");
            car.SetActive(false);
            Fy_DialogManager f = npc.GetComponent<Fy_DialogManager>();
            car.GetComponentInChildren<Fy_DialogManager>().spritePlayer = f.spritePlayer;
            car.GetComponentInChildren<Fy_DialogManager>().spriteMaiHua = f.spriteMaiHua;
            car.GetComponentInChildren<Fy_DialogManager>().content = f.content;
            car.GetComponentInChildren<Fy_DialogManager>().contentText = f.contentText;
            car.GetComponentInChildren<Fy_DialogManager>().contentName = f.contentName;
            yield return null;
        } else car.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        if (car == null)
        {

            car = GameObject.FindGameObjectWithTag("FlowerCar");
            car.SetActive(false);
            Fy_DialogManager f = npc.GetComponent<Fy_DialogManager>();
            car.GetComponentInChildren<Fy_DialogManager>().spritePlayer = f.spritePlayer;
            car.GetComponentInChildren<Fy_DialogManager>().spriteMaiHua = f.spriteMaiHua;
            car.GetComponentInChildren<Fy_DialogManager>().content = f.content;
            car.GetComponentInChildren<Fy_DialogManager>().contentText = f.contentText;
            car.GetComponentInChildren<Fy_DialogManager>().contentName = f.contentName;
            yield return null;
        } else car.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        if (car == null)
        {

            car = GameObject.FindGameObjectWithTag("FlowerCar");
            car.SetActive(false);
            Fy_DialogManager f = npc.GetComponent<Fy_DialogManager>();
            car.GetComponentInChildren<Fy_DialogManager>().spritePlayer = f.spritePlayer;
            car.GetComponentInChildren<Fy_DialogManager>().spriteMaiHua = f.spriteMaiHua;
            car.GetComponentInChildren<Fy_DialogManager>().content = f.content;
            car.GetComponentInChildren<Fy_DialogManager>().contentText = f.contentText;
            car.GetComponentInChildren<Fy_DialogManager>().contentName = f.contentName;
            yield return null;
        }else car.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        if (car == null)
        {

            car = GameObject.FindGameObjectWithTag("FlowerCar");
            car.SetActive(false);
            Fy_DialogManager f = npc.GetComponent<Fy_DialogManager>();
            car.GetComponentInChildren<Fy_DialogManager>().spritePlayer = f.spritePlayer;
            car.GetComponentInChildren<Fy_DialogManager>().spriteMaiHua = f.spriteMaiHua;
            car.GetComponentInChildren<Fy_DialogManager>().content = f.content;
            car.GetComponentInChildren<Fy_DialogManager>().contentText = f.contentText;
            car.GetComponentInChildren<Fy_DialogManager>().contentName = f.contentName;
            yield return null;
        } else car.SetActive(false);
    }
}
