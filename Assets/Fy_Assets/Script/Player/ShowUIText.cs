using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShowUIText : MonoBehaviour
{
    public float minNum;
    //public float maxNum;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (minNum > GetComponent<CircleCollider2D>().radius) 
        {
            return;
        }
        if (collision.gameObject.CompareTag("ShowUI"))
        {
            //Debug.Log(11);
            Text it = collision.gameObject.GetComponentInChildren<Text>();
            if (it == null) return;
            Vector3 m_long = collision.gameObject.transform.position - transform.position;
            float num = m_long.magnitude;
            if (num > minNum)
            {
                it.color = new Color(1,1,1,(1- (num - minNum) / (GetComponent<CircleCollider2D>().radius - minNum)));
            }
            else
            {
                it.color = new Color(1, 1, 1, 1);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("ShowUI"))
        {
            Text it = collision.gameObject.GetComponentInChildren<Text>();
            it.color = new Color(1, 1, 1, 0);
        }
    }
    public void ChangeScenes()
    {
        SceneManager.LoadScene("PersistentScenes");
    }
}
