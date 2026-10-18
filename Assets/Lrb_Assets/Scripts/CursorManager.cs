using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [Header("1) 替换系统光标")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("2) 问号Sprite（建议放在Screen Space Canvas下）")]
    [SerializeField] private RectTransform questionMarkUI; // 问号UI对象
    [SerializeField] private Vector2 questionOffset = new Vector2(28f, -8f); // 在鼠标右侧的偏移

    [Header("检测配置")]
    [SerializeField] private Camera targetCamera; // 不填则用Camera.main
    [SerializeField] private LayerMask detectMask = ~0; // 可检测层

    private void Start()
    {
        // 功能1：替换光标
        Cursor.SetCursor(cursorTexture, cursorHotspot, cursorMode);

        if (targetCamera == null) targetCamera = Camera.main;
        if (questionMarkUI != null) questionMarkUI.gameObject.SetActive(false);
    }

    private void Update()
    {

    }

    private bool IsMouseOverFyDialogManager()
    {
        if (targetCamera == null) return false;

        // 先做2D检测
        Vector2 worldPos2D = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPos2D, Vector2.zero, 0f, detectMask);
        if (hit2D.collider != null && hit2D.collider.GetComponent<Fy_DialogManager>() != null)
            return true;

        // 再做3D检测
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit3D, 1000f, detectMask))
        {
            if (hit3D.collider.GetComponentInParent<Fy_DialogManager>() != null)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        // 还原系统光标，避免场景切换后残留
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}
