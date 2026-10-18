using UnityEngine;
using UnityEngine.EventSystems;

public class ShowMyChoice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject myChoice;

    private void Awake()
    {
        if (myChoice != null)
            myChoice.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myChoice != null)
            myChoice.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (myChoice != null)
            myChoice.SetActive(false);
    }
}
