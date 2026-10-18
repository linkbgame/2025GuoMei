using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IPuzzleInteractable
{
    [field: SerializeField]
    public string PuzzleID { get; private set; }

    [field: SerializeField]
    public bool IsSolved { get; private set; }


    public void SuccessfullySolve()
    {
        Debug.Log($"Puzzle {PuzzleID} Successfully Solved!");
        IsSolved = true;
        this.gameObject.tag = "Untagged";
    }
}
