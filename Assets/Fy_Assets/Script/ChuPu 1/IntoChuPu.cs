using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntoChuPu : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (Input.GetKey(KeyCode.X))
            {
                IntoGame();
                if(GetComponent<NPCMovement>() != null){
                    GetComponent<NPCMovement>().enabled = false;
                }
            }
        }
    }
    public void IntoGame()
    {
        SaveLoadSystem.Instance.SaveGame();
        SceneManager.LoadScene("ChuPu");
    }
}
