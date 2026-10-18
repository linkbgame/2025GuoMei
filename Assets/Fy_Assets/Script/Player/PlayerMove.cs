using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMove : MonoBehaviour
{
    protected Vector3 point_needToGo;//��ͨ�ƶ�Ŀ��
    private List<Node> path;//�洢·��
    public bool ReadyToHit, isStopMove, isDie;//״̬�л����
    public float currentSpeed; // ��ǰ�ƶ��ٶȣ���λ����λ/�룩

    private int i = 0;
    private float t = 0;
    private Vector3 startPos;

    public bool canMove = true;//�Ƿ�����ƶ�

    // ������SpriteRenderer ���Ʒ�ת
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ��������ǰ�εľ��������ʱ�䣨���ں㶨�ٶȲ�ֵ��
    private float segmentDistance = 0f;
    private float segmentDuration = 0f;

    // ���������ڸ��ɿ��ؽ������ӳ�䵽�����桱
    [SerializeField] private LayerMask groundLayer = ~0; // Ĭ�����в㣬���� Inspector ����Ϊ Ground ��
    [SerializeField] private bool preferRaycastToGround = true; // ����ʹ�����߼����棬�ٻ��˵�ƽ��
    [SerializeField] private float groundZ = 0f; // Tilemap ���ڵ� Z ƽ�棨Ĭ�� 0������������ײʱ���˵���ƽ��

    private Rigidbody2D rb2d;

    private void Awake()
    {
        path = new List<Node>();
        ani = GetComponent<Animator>();
        rb2d = GetComponent<Rigidbody2D>();
        // ����� Inspector û��ָ���������Զ���ȡ
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// ����Ҵ� Simulated �� Rigidbody2D���� MovePosition + FixedUpdate �ƽ���
    /// �����������������Ÿ� transform���� NPC ʱ������������
    /// </summary>
    private bool UseRigidbodyMove() => rb2d != null && rb2d.simulated;

    private Vector3 GetWorldPosition()
    {
        if (UseRigidbodyMove())
            return new Vector3(rb2d.position.x, rb2d.position.y, transform.position.z);
        return transform.position;
    }

    private void SetWorldPosition(Vector3 worldPos)
    {
        if (UseRigidbodyMove())
        {
            rb2d.MovePosition(new Vector2(worldPos.x, worldPos.y));
            if (Mathf.Abs(transform.position.z - worldPos.z) > 0.0001f)
                transform.position = new Vector3(rb2d.position.x, rb2d.position.y, worldPos.z);
        }
        else
        {
            transform.position = worldPos;
        }
    }

    private void Update()
    {
        if(canMove)
        if (Input.GetMouseButtonDown(0))//�������ƶ�
        {
            if (IsPointerOverUI())
            {
                // �������� UI �Ϸ��������¼��ѱ� UI ���ģ�ֱ�ӷ��أ���ִ���ƶ��߼���
                return;
            }

            Vector3 worldPos;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("PlayerMove: Camera.main is null. �޷�ת��������ꡣ");
                return;
            }

            // ����ʹ�������������ײ�彻�㣨���ɿ�����������б�������3D ������ Collider �� Tilemap��
            if (preferRaycastToGround)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
                {
                    worldPos = hit.point;
                }
                else
                {
                    // ���ˣ��������� Tilemap ���ڵ� Z ƽ���ཻ��z = groundZ��
                    // Tilemap �Ľڵ������� XY ƽ���� center.z Ĭ��Ϊ 0���� TilemapManager��
                    Plane groundPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, groundZ));
                    if (groundPlane.Raycast(ray, out float enter))
                    {
                        worldPos = ray.GetPoint(enter);
                    }
                    else
                    {
                        // �����ˣ�ʹ���� groundZ Ϊ��ȵ� ScreenToWorldPoint
                        Vector3 mousePos = Input.mousePosition;
                        float screenZ = cam.WorldToScreenPoint(new Vector3(0f, 0f, groundZ)).z;
                        mousePos.z = screenZ;
                        worldPos = cam.ScreenToWorldPoint(mousePos);
                    }
                }
            }
            else
            {
                // ���ּ��ݣ�ʹ�� ScreenToWorldPoint���� z ʹ������� groundZ �����
                Vector3 mousePos = Input.mousePosition;
                float screenZ = cam.WorldToScreenPoint(new Vector3(0f, 0f, groundZ)).z;
                mousePos.z = screenZ;
                worldPos = cam.ScreenToWorldPoint(mousePos);
            }

           // Debug.Log("�����������: " + worldPos);
            GetPointToGo(worldPos);
        }

        if (canMove && !UseRigidbodyMove())
            Move();
    }

    private void FixedUpdate()
    {
        if (canMove && UseRigidbodyMove())
            Move();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        return false;
    }

    public void Move()
    {
        if (isStopMove) return;

        if (path == null || path.Count == 0)
        {
            return;
        }

        // ��� i ������Χ����
        if (i >= path.Count)
        {
            // ��֤����λ�þ�ȷ��ʹ���������꣩
            if (path.Count > 0)
            {
                var last = path[path.Count - 1].Center;
                SetWorldPosition(new Vector3(last.x, last.y, last.z));
            }
            isStopMove = true;
            TakeAni(isStopMove);
            return;
        }

        // ��ÿ�ο�ʼʱȷ�� startPos ���ڵ�ǰ transform��������öεľ����ʱ��
        if (t <= 0f)
        {
            startPos = GetWorldPosition();

            // Ŀ��㣨Node.Center �� float3��
            Vector3 target = new Vector3(path[i].Center.x, path[i].Center.y, path[i].Center.z);

            // ���ⰲȫ��飺��� startPos ��Ŀ������쳣�󣨿�������Ϊ����Բ��ϣ���ʹ�õ�ǰ transform ��Ϊ startPos
            if (Vector3.Distance(startPos, target) > 100f) // ��ֵ�ɵ�
            {
                startPos = GetWorldPosition();
            }

            // ���㵱ǰ�εľ��������ʱ�䣬��֤�ƶ��ٶȺ㶨����λ��currentSpeed��
            segmentDistance = Vector3.Distance(startPos, target);
            // ��ֹ�����㣺�� currentSpeed <= 0 �� segmentDistance �ǳ�Сʱ��ֱ�Ӱ�ʱ����Ϊ��Сֵ�� 0����ζ��˲�ƣ�
            if (currentSpeed <= 0f)
            {
                segmentDuration = 0f;
            }
            else
            {
                segmentDuration = (segmentDistance <= 0.0001f) ? 0f : (segmentDistance / currentSpeed);
            }

            // �����ǰ�ηǳ��̣�ֱ�Ӱ�λ������ΪĿ�겢�ƽ�����һ�Σ����ⶶ��
            if (segmentDistance <= 0.001f)
            {
                SetWorldPosition(target);
                i++;
                t = 0f;
                // ������ɵ�ǰ�κ�����Ѿ��������˳�
                if (i >= path.Count)
                {
                    isStopMove = true;
                    TakeAni(isStopMove);
                    return;
                }
                // ����Ϊ��һ��׼������һ�� Update �����½��� t<=0 �ķ�֧���㣩
                startPos = GetWorldPosition();
                return;
            }
        }

        // ����ֵ��ɣ�ȷ��λ�þ�ȷ��Ŀ�겢�ƽ�����һ��
        if (t >= 1f)
        {
            // ǿ�ƽ���ǰλ������ΪĿ�����ģ������ۻ�����˲��
            var reached = path[i].Center;
            SetWorldPosition(new Vector3(reached.x, reached.y, reached.z));

            i++;
            t = 0f;

            // ����Ѿ��������нڵ㣬�����
            if (i >= path.Count)
            {
                isStopMove = true;
                TakeAni(isStopMove);
                return;
            }

            // Ϊ��һ��׼����㣨��һ֡������¶ε� duration��
            startPos = GetWorldPosition();
            return;
        }

        float moveDt = UseRigidbodyMove() ? Time.fixedDeltaTime : Time.deltaTime;

        // ���� t�����ڶ�ʱ�����㣬��֤�㶨�ٶȣ��� segmentDuration == 0��˲�ƻ� speed Ϊ 0������ֱ�����
        if (segmentDuration <= 0f)
        {
            // ���ʱ��Ϊ 0��ֱ������Ŀ�꣬���� t ������ɶ���
            var targetInstant = new Vector3(path[i].Center.x, path[i].Center.y, path[i].Center.z);
            SetWorldPosition(targetInstant);
            i++;
            t = 0f;
            if (i >= path.Count)
            {
                isStopMove = true;
                TakeAni(isStopMove);
                Debug.Log(4);
                return;
            }
            startPos = GetWorldPosition();
            return;
        }
        else
        {
            // ������ʱ���ƽ���һ������
            t += moveDt / segmentDuration;
        }

        // ��ֹ t ��һ֡���� 1������֡�ʼ��ͻ� currentSpeed �ܴ󣩣�����ضϲ���λ����ΪĿ��
        float tClamped = Mathf.Min(t, 1f);
        Vector3 targetPos = new Vector3(path[i].Center.x, path[i].Center.y, path[i].Center.z);
        SetWorldPosition(Vector3.Lerp(startPos, targetPos, tClamped));

        // ���ݵ�ǰ����㵽Ŀ���ˮƽ�������� Sprite ��ת��ֻ��ˮƽ��������ʱ��ת��
        float deltaX = targetPos.x - startPos.x;
        if (spriteRenderer != null && Mathf.Abs(deltaX) > 0.01f)
        {
            // deltaX < 0 ����flipX = true�����������������Դȷ���Ƿ���Ҫ��ת�߼���
            spriteRenderer.flipX = deltaX < 0f;
        }
    }//�����ƶ�����
    public void StopMove()
    {
        path.Clear();
        i = 0;
        t = 0;
        startPos = GetWorldPosition();
        isStopMove = true;
        TakeAni(isStopMove);
    }//ԭ״̬���˳��ƶ�״̬����
    public void GetPointToGo(Vector3 mouse)//������¼���world pos��
    {
       // Debug.Log("���λ��" + mouse);
        //��point_needToGo��ֵλ�� 
        point_needToGo = mouse;
        MoveToPoint();
    }
    public void MoveToPoint()
    {
        // �����˶�״̬����ֹ��һ���˶��� startPos/t ����˲��
        isStopMove = false;
        path.Clear();
        // �ؼ��������������꣨transform.position �� point_needToGo ���� world pos��
        path = TilemapManager.instance.FindPath(GetWorldPosition(), point_needToGo);

        // ��ʼ����ֵ������ȷ����һ��ƽ�����ɣ�startPos Ϊ��ǰ transform��
        i = 0;
        t = 0f;
        startPos = GetWorldPosition();

        // ���·��Ϊ�ջ��������ǰ�㣬��ֹͣ�ƶ�
        if (path == null || path.Count == 0)
        {
            isStopMove = true;
            TakeAni(isStopMove);
            return;
        }
        TakeAni(isStopMove);

        // ��ѡ�������һ��Ŀ���뵱ǰλ�÷ǳ��ӽ�����ѵ�ǰλ������Ϊ��һ���ڵ�����
        var firstCenter = path[0].Center;
        if (Vector3.Distance(GetWorldPosition(), new Vector3(firstCenter.x, firstCenter.y, firstCenter.z)) < 0.01f)
        {
            SetWorldPosition(new Vector3(firstCenter.x, firstCenter.y, firstCenter.z));
        }

        // ���ö���ؼ�ʱ����һ֡ Move �����¼��� segmentDistance/segmentDuration��
        segmentDistance = 0f;
        segmentDuration = 0f;
    }//�����ƶ�·��
    private Animator ani;
    public void TakeAni(bool isStopMove)
    {
        if (!isStopMove)
        {
            ani.CrossFade("walk", 0f);
        }
        else
        {
            ani.CrossFade("idle_down", 0f);
        }
    }
    public void TakeIdle(bool isUp)
    {
        point_needToGo = GetWorldPosition();
        GetPointToGo(GetWorldPosition());
        Debug.Log(111);
        if (!isUp)
        {
            ani.CrossFade("idle_up", 0f);
        }
        else
        {
            ani.CrossFade("idle_down", 0f);
        }
    }
}
