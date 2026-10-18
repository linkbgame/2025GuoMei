using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowRoadUI : MonoBehaviour
{
    public GameObject it;
    public bool CanTake;
    
    public PlayerMove player;
    public GameObject playerGO;
    private void Start()
    {
        it.SetActive(false);
        CanTake = false;
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Finish"))
        {
            it.SetActive(true);
            playerGO = collision.gameObject;
            player = collision.gameObject.GetComponentInParent<PlayerMove>();
            CanTake = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Finish"))
        {
            it.SetActive(false);
            player = null;
            playerGO = null;
            CanTake = false;
        }
    }
}
