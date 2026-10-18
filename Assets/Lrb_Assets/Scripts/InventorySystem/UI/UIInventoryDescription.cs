using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


namespace Inventory.UI
{
    public class UIInventoryDescription : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.UI.Image itemImage;
        [SerializeField]
        private TMP_Text title;
        [SerializeField]
        private TMP_Text description;
        [SerializeField]
        private Vector3 currentItemCluePos;
        [TextArea]
        [SerializeField]
        private string verticalProcessingText;

        private CanvasGroup canvasGroup;

        // ���жԽ����ű������ã�����ת���ı�
        private ItemDesClueInteractable m_clueInteractable;

        private void Awake()
        {          
            canvasGroup = GetComponent<CanvasGroup>();
            m_clueInteractable = GetComponent<ItemDesClueInteractable>();
        }

        public void ResetDescription()
        {
            HideDescription();
            itemImage.gameObject.SetActive(false);//��
            title.text = "";
            description.text = "";
        }

        public void SetDescription(Sprite sprite, string itemName, string itemDescription)
        {
            itemImage.gameObject.SetActive(true);//��
            itemImage.sprite = sprite;

            // �������Ŵ�����ͨ�����ı���ǩʵ����ת���о����
            title.text = "<rotate=90><line-height=70>" + itemName + "</line-height></rotate>";
            itemDescription = "<rotate=90><line-height=70>" + itemDescription + "</line-height></rotate>";

            // ����н����ű�����ԭʼ�ı�������������������� m_originalText ��ˢ����ʾ��
            if (m_clueInteractable != null)
            {
                // �������ǽ� itemDescription (ԭʼ�ı�) ����ȥ
                // description.text �ĸ�ֵ����Ҳ���� clueInteractable ����߼�����
                m_clueInteractable.UpdateContent(itemDescription);
            }
            else
            {
                // ���û�н����ű������ֻ��һ����ͨ���ı���ʾ
                description.text = itemDescription;
            }

            //description.text = itemDescription;
        }

        public void HideDescription()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        public void ShowDescription()
        {
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
        
        public void SetCluePos(Vector3 cluePos)
        {
            currentItemCluePos = cluePos;
        }

        public Vector3 GetCluePos()
        {
            return currentItemCluePos;
        }
    }
}
