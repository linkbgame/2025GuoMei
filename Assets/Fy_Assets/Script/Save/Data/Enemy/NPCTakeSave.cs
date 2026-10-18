using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCTakeSave : MonoBehaviour
{
    public bool isInThisScene;
    public string nowTalkingName;
    private ShowRoadUI showRoadUI;
    private Fy_DialogManager fy_DialogManager;
    private bool hasTakeE;
    private void Start()
    {
        showRoadUI = GetComponent<ShowRoadUI>();
        fy_DialogManager = GetComponent<Fy_DialogManager>();
    }
    private void Update()
    {
        if (showRoadUI.CanTake && !hasTakeE)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                StartTalk();
            }
        }
    }
    public void EndTalk()
    {
        hasTakeE = false;
    }
    public void StartTalk()
    {
        if (fy_DialogManager == null)
            fy_DialogManager = GetComponent<Fy_DialogManager>();
        if(showRoadUI == null)
            showRoadUI = GetComponent<ShowRoadUI>();
        fy_DialogManager.TakeMyText(SimpleFactory.FindText(nowTalkingName));
        hasTakeE = true;
        NPCMovement it = GetComponent<NPCMovement>();
        if(it != null) 
            GetComponent<NPCMovement>().enabled = false;
        if (showRoadUI.player != null)
        {
            Debug.Log(transform.position.y - showRoadUI.playerGO.transform.position.y);
            showRoadUI.player.canMove = false;
            showRoadUI.player.TakeIdle(transform.position.y - showRoadUI.playerGO.transform.position.y > 0 ? true : false);
        }
    }
}
