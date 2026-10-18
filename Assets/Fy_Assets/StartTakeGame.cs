using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StartTakeGame : MonoBehaviour
{
    public UnityEvent END;
    private bool hasDown = true;
    private void Update()
    {
        if(hasDown)
        if (Input.GetMouseButtonUp(0))
        {
            END?.Invoke();
            hasDown = false;
        }
    }
}
