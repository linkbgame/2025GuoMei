using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JustButton : MonoBehaviour
{
    private bool isShow;
    public GameObject show;
    private void Start()
    {
        isShow = false;
    }
    public void TakeButton()
    {
        show.SetActive(!isShow);
        isShow=!isShow;
    }
}
