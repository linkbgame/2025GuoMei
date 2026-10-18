using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InkSpreadAutoStarter : MonoBehaviour
{
    void Start()
    {
        var ink = GetComponent<InkSpreadUI>();
        if (ink != null)
            ink.Play();
    }
}
