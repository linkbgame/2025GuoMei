using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ��̬������������
namespace ClueBoard.Model
{
    // �������Ͷ���
    public enum ClueType
    {
        Evidence,   // ��ͨ�������ɵ�������ռ�����������
        Destination // �յ�ص㣺����ֱ�ӵ��������������ɺ��Զ�����
    }

    [CreateAssetMenu(fileName = "ClueSO", menuName = "ClueBoardSystem/Clue/ClueSO")]
    public class ClueSO : ScriptableObject
    {
        public int ID => GetInstanceID();

        [field: SerializeField]
        public ClueType Type { get; private set; } = ClueType.Evidence;

        [field: SerializeField]
        public Vector3 StaticWorldPos { get; private set; }

        /// <summary>
        /// �ȶ� ���ı���д��Link �Լ� �Ի�ϵͳ���õ�TriggerLink
        /// ����������֤ID��1��1ʶ���ϵ
        /// </summary>
        [field: SerializeField]
        public string ClueLink { get; private set; }//���ڵ���ı��ͶԻ�ϵͳ���ֻ�ȡ������ʽ

        [field: SerializeField]
        [field: TextArea]
        public string ClueDesc { get; private set; }//������ʾ������   

        [field: SerializeField]
        //������������ɫ
        public Color ReasonedColor { get; private set; } = Color.green;

        [field: SerializeField]
        public Color DefaultColor { get; private set; } = Color.white;

        [field: SerializeField]
        public Sprite ClueInkStickImage { get; private set; }
    }
}
