using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacingCamera : MonoBehaviour
{
    GameObject[] childs;

    void Start()
    {
        
        childs = GameObject.FindGameObjectsWithTag("45");
        Debug.Log(childs.Length);
    }

    void Update()
    {

        for (int i = 0; i < childs.Length; i++)
        {
            if(childs[i] != null)
                childs[i].GetComponent<Transform>().rotation = Camera.main.transform.rotation;
        }
    }
}
