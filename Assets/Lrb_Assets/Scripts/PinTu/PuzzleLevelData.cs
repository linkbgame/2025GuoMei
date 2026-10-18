using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewPuzzleLevel", menuName = "PuzzleGame/Level Data")]
public class PuzzleLevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;
    public Sprite fullImage; // ƴͼ��������ͼ�����ڲο���

    [Header("Grid Settings")]
    public int rows; // ����
    public int cols; // ����

    [Header("Pieces")]
    // �洢�� Sprite Editor �и���������� Sprite
    public List<Sprite> pieceSprites = new List<Sprite>();

    // �洢ÿ����Ƭ��Ӧ����ȷ���꣨�ֲ����꣩
    // ע�⣺�ڱ༭��������ʱ��˳������ pieceSprites һһ��Ӧ
    public List<Vector2> correctPositions = new List<Vector2>();

    [Header("Difficulty")]
    public float snapThreshold = 0.5f; // ������������

    [System.Serializable]
    public class ScatterArea
    {
        [Tooltip("该区域中心点（拼图父节点的局部坐标）")]
        public Vector2 center;

        [Tooltip("区域尺寸（宽高）")]
        public Vector2 size = new Vector2(2f, 2f);
    }

    [Header("Start Scatter Areas")]
    [Tooltip("开始时碎片可分布的多个区域（可配2个及以上）")]
    public List<ScatterArea> scatterAreas = new List<ScatterArea>();

    [Tooltip("true=轮询分配到各区域（更均匀）；false=随机分配区域")]
    public bool evenDistribution = true;
}



