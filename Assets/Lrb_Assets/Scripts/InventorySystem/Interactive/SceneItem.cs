using Inventory.Model;
using System.Collections;
using UnityEngine;

public class SceneItem : MonoBehaviour
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }

    [field: SerializeField]
    public int Quantity { get; set; } = 1;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private float duration = 0.3f;

    private SpriteRenderer m_spriteRenderer;
    private Collider2D m_collider;

    private void Awake()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (InventoryItem == null)
        {
            Debug.LogError($"[SceneItem] {name} 没有配置 InventoryItem");
            return;
        }

        if (m_spriteRenderer == null)
        {
            Debug.LogError($"[SceneItem] {name} 没有配置 SpriteRenderer");
            return;
        }

        m_spriteRenderer.sprite = InventoryItem.ItemImage;

        // 将场景中的位置赋值给物品存储的数据
        InventoryItem.OriginalWorldPos = transform.position;
    }

    public void DestroyItem()
    {
        if (m_collider != null)
        {
            m_collider.enabled = false;
        }
        StartCoroutine(AnimateItemPickup());
    }

    private IEnumerator AnimateItemPickup()
    {
        // 放音乐、缩小、删除
        if (audioSource != null)
        {
            audioSource.Play();
        }

        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, currentTime / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
