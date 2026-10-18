using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMove : MonoBehaviour
{
    [System.Serializable]
    public class MovePoint
    {
        [Tooltip("Ŀ������")]
        public Vector3 position;

        [Tooltip("�ӵ�ǰ���ƶ����õ�����ʱ��(��)��<=0 ����������")]
        public float moveTime = 1f;
    }

    [Header("Path")]
    [Tooltip("����ǰ����Ŀ����б�")]
    public List<MovePoint> points = new List<MovePoint>();

    [Tooltip("��ʼʱ�Ƿ��Զ��ƶ�")]
    public bool playOnStart = true;

    [Tooltip("������Ƿ�ѭ��")]
    public bool loop = false;

    [Tooltip("�����Ƿ�ʹ�ñ�������(LocalPosition)������ʹ����������(Position)��")]
    public bool useLocalSpace = false;

    [Header("Debug")]
    [SerializeField] private int currentIndex = 0;
    [SerializeField] private bool isPlaying = false;

    private Coroutine _moveRoutine;

    private void Start()
    {
        if (playOnStart)
        {
            PlayFromBeginning();
        }
    }

    /// <summary>
    /// �ӵ�һ���㿪ʼ����·����
    /// </summary>
    public void PlayFromBeginning()
    {
        currentIndex = 0;
        Play();
    }

    /// <summary>
    /// �ӵ�ǰ������������·����
    /// </summary>
    public void Play()
    {
        if (points == null || points.Count == 0)
        {
            Stop();
            return;
        }

        isPlaying = true;
        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveSequence());
    }

    public void Stop()
    {
        isPlaying = false;
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }
    }

    /// <summary>�Ƿ�������·���ƶ���<see cref="Stop"/>����ѭ��·�����ꡢ��·��Ϊ��ʱΪ false����</summary>
    public bool IsPathPlaying => isPlaying;

    private IEnumerator MoveSequence()
    {
        while (isPlaying)
        {
            if (points == null || points.Count == 0)
            {
                Stop();
                yield break;
            }

            if (currentIndex < 0) currentIndex = 0;

            if (currentIndex >= points.Count)
            {
                if (loop)
                    currentIndex = 0;
                else
                {
                    Stop();
                    yield break;
                }
            }

            var p = points[currentIndex];
            yield return MoveTo(p.position, p.moveTime);
            currentIndex++;
        }
    }

    private IEnumerator MoveTo(Vector3 target, float moveTime)
    {
        Vector3 start = useLocalSpace ? transform.localPosition : transform.position;

        if (moveTime <= 0f)
        {
            if (useLocalSpace) transform.localPosition = target;
            else transform.position = target;
            yield break;
        }

        // ���٣��ٶ� = ���� / ʱ��
        float distance = Vector3.Distance(start, target);
        float speed = distance / moveTime;

        float remaining = distance;
        while (remaining > 0.0001f)
        {
            float step = speed * Time.deltaTime;
            remaining -= step;

            Vector3 current = useLocalSpace ? transform.localPosition : transform.position;
            Vector3 next = Vector3.MoveTowards(current, target, step);

            if (useLocalSpace) transform.localPosition = next;
            else transform.position = next;

            yield return null;
        }

        // ǿ�ƶ���
        if (useLocalSpace) transform.localPosition = target;
        else transform.position = target;
    }

    private void OnDisable()
    {
        // ��ֹ����/����ʱЭ������
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }
        isPlaying = false;
    }
}
