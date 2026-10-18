using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class CameraMove : MonoBehaviour
{
    [Header("Target (玩家)")]
    public Transform target;

    [Header("死区(不追踪的范围)")]
    public Vector2 deadZoneSize = new Vector2(4f, 2f); // 宽度、高度（世界单位，相对于相机的本地轴）
    public Vector2 deadZoneOffset = Vector2.zero;     // 相对于相机中心的偏移（本地 X=右, Y=上）

    //[Header("可选：在场景中直接调整 DeadZone")]
    //[Tooltip("如果指定了 DeadZoneHandle，则会使用它的 localPosition/localScale 来替代 deadZoneOffset/deadZoneSize")]
    private Transform deadZoneHandle;

    [Header("追踪设置")]
    [Range(0f, 50f)]
    public float followSpeed = 6f; // 越大越快，0 表示瞬移

    [Header("Gizmos 显示")]
    public Color gizmoColor = new Color(0f, 0.7f, 1f, 0.25f);
    public Color gizmoWireColor = new Color(0f, 0.9f, 1f, 1f);

    [LabelText("自动缩放设置（启用后生效）")]
    public bool enableAutoZoom = true;                 // 公共开关：是否启用该功能
    [LabelText("进入缩小时间")]
    public float idleDelay = 1.5f;                     // 停止移动等待时间（秒）
    [LabelText("缩小目标大小（黄色）")]
    public float targetOrthographicSize = 3f;          // 目标缩小后的 ortho size
    [LabelText("检测玩家是否在移动的阈值")]
    public float minMoveThreshold = 0.01f;             // 判定为移动的最小距离
    [LabelText("相机缩小速度")]
    public float zoomInSpeed = 2f;                     // 缩小（靠近玩家）速度
    [LabelText("相机放大速度")]
    public float zoomOutSpeed = 4f;                    // 放大（恢复）速度

    [Header("相机边界限制（世界坐标）")]
    [LabelText("启用边界限制")]
    public bool enableBounds = false;

    [LabelText("边界（世界坐标矩形：xMin,yMin,width,height）")]
    public Rect bounds = new Rect(-10f, -10f, 20f, 20f);

    [LabelText("显示边界 Gizmos")]
    public bool drawBoundsGizmos = true;

    [LabelText("边界 Gizmos 颜色")]
    public Color boundsGizmoColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("类 CinemachineConfiner（2D Collider 约束）")]
    [LabelText("使用 Collider2D 作为边界（推荐）")]
    public bool useColliderConfiner = false;

    [LabelText("边界形状（Polygon/Composite/BoxCollider2D 等）")]
    public Collider2D confinerCollider;

    [LabelText("Confinement 迭代次数")]
    [Range(1, 8)]
    public int confinerIterations = 3;

    [LabelText("约束平面 Z（不填则使用 target.z）")]
    public bool overrideConfinePlaneZ = false;

    [ShowIf("overrideConfinePlaneZ")]
    [LabelText("平面 Z")]
    public float confinePlaneZ = 0f;

    [LabelText("边界内缩/外扩（世界单位，正值=更严格）")]
    public float confinerMargin = 0f;

    [LabelText("显示透视视野与平面交线 Gizmos")]
    public bool drawPerspectivePlaneQuadGizmos = false;

    public Color perspectivePlaneQuadGizmoColor = new Color(0.2f, 1f, 0.2f, 1f);

    // 私有状态
    private Camera cam;
    private float originalOrthoSize;
    private float idleTimer = 0f;
    private Vector3 lastTargetPosition;
    // private bool isZoomed = false;

    // 新增：在检测到一次移动后持续恢复到原始大小的标志
    private bool shouldRestoreToOriginal = false;

    void Awake()
    {
        // 尝试获取 Camera（通常脚本挂在 Camera 上）
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            originalOrthoSize = cam.orthographicSize;

        if (target != null)
            lastTargetPosition = target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 锁定当前 transform.position.z，后续移动保持该值不变
        float fixedZ = transform.position.z;

        // --- 计算死区（使用相机本地轴：right/up） ---
        Vector2 halfSize;
        Vector2 offsetLocal;
        if (deadZoneHandle != null)
        {
            // handle 的 localScale/localPosition 视为相机本地的值
            halfSize = new Vector2(Mathf.Abs(deadZoneHandle.localScale.x), Mathf.Abs(deadZoneHandle.localScale.y)) * 0.5f;
            offsetLocal = deadZoneHandle.localPosition;
        }
        else
        {
            halfSize = deadZoneSize * 0.5f;
            offsetLocal = deadZoneOffset;
        }

        Vector3 camPos = transform.position;

        // zoneCenter 在世界空间：相机位置沿本地 right/up 偏移
        Vector3 zoneCenterWorld = camPos + transform.right * offsetLocal.x + transform.up * offsetLocal.y;

        // 计算目标相对于 zoneCenter 在相机本地 right/up 的投影
        Vector3 toTarget = target.position - zoneCenterWorld;
        float projX = Vector3.Dot(toTarget, transform.right); // 在相机本地右轴上的距离
        float projY = Vector3.Dot(toTarget, transform.up);    // 在相机本地上轴上的距离

        // 根据投影判断是否超出死区并计算需要沿本地轴移动的量
        float moveAlongRight = 0f;
        if (projX < -halfSize.x)
            moveAlongRight = projX + halfSize.x;   // 负值 -> 向左移动相机
        else if (projX > halfSize.x)
            moveAlongRight = projX - halfSize.x;   // 正值 -> 向右移动相机

        float moveAlongUp = 0f;
        if (projY < -halfSize.y)
            moveAlongUp = projY + halfSize.y;      // 负值 -> 向下移动相机
        else if (projY > halfSize.y)
            moveAlongUp = projY - halfSize.y;      // 正值 -> 向上移动相机

        // 目标相机位置（仅沿相机本地 right/up 移动；保持相机的旋转不变）
        Vector3 desiredPos = camPos + transform.right * moveAlongRight + transform.up * moveAlongUp;

        // 保持 z 不变
        desiredPos.z = fixedZ;

        // 新增：边界限制（正交相机）。确保“相机视野边缘”不越过 bounds
        if (enableBounds)
        {
            desiredPos = ClampToBounds(desiredPos);
            desiredPos.z = fixedZ;
        }

        if (followSpeed <= 0f)
        {
            transform.position = desiredPos;
        }
        else
        {
            Vector3 newPos = Vector3.Lerp(camPos, desiredPos, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            // 确保 z 分量保持固定
            newPos.z = fixedZ;

            // 新增：插值后同样做一次边界限制，避免越界
            if (enableBounds)
            {
                newPos = ClampToBounds(newPos);
                newPos.z = fixedZ;
            }

            transform.position = newPos;
        }

        // --- 新增：判断玩家是否静止并控制相机缩放（仅在运行时生效） ---
        if (!enableAutoZoom || cam == null)
        {
            // 若功能关闭或无相机则确保恢复为原始大小（平滑恢复）
            if (cam != null)
                cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, originalOrthoSize, zoomOutSpeed * Time.deltaTime);
            // 更新 lastTargetPosition 以避免误判
            lastTargetPosition = target.position;
            idleTimer = 0f;
            //isZoomed = false;
            shouldRestoreToOriginal = false;
            return;
        }

        // 仅在播放时计算移动/定时；编辑模式下只显示 Gizmos
        if (!Application.isPlaying) return;

        float moveDist = Vector2.Distance(new Vector2(target.position.x, target.position.y),
                                          new Vector2(lastTargetPosition.x, lastTargetPosition.y));

        if (moveDist > minMoveThreshold)
        {
            // 玩家有一次移动：开始恢复到原始大小（持续进行），重置静止计时
            idleTimer = 0f;
            //isZoomed = false;
            shouldRestoreToOriginal = true; // 关键：检测到移动就启动恢复过程（并持续直到恢复完成）
        }

        // 先处理恢复（放大/回原始大小）。恢复进行时，不累加 idleTimer，也不触发缩小逻辑
        if (shouldRestoreToOriginal)
        {
            cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, originalOrthoSize, zoomOutSpeed * Time.deltaTime);

            // 恢复未完成时，不开始计时进入缩小
            if (!Mathf.Approximately(cam.orthographicSize, originalOrthoSize) && cam.orthographicSize < originalOrthoSize - 1e-5f)
            {
                // 仍在恢复中，保持 idleTimer = 0（放大所用时间不计入进入缩小的时间）
                idleTimer = 0f;
            }
            else
            {
                // 恢复完成：确保值精确、清除恢复标志，并从 0 开始计时下一次静止
                cam.orthographicSize = originalOrthoSize;
                shouldRestoreToOriginal = false;
                idleTimer = 0f;
            }

            // 更新 lastTargetPosition 并结束本帧缩放判断（因为恢复优先）
            lastTargetPosition = target.position;
            return;
        }

        // 如果不在恢复阶段，则可以开始/累加静止计时，超过 idleDelay 后开始缩小
        // （此处保证放大所用时间不被计入 idleTimer）
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDelay)
        {
            //isZoomed = true;
            cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, targetOrthographicSize, zoomInSpeed * Time.deltaTime);
        }
        else
        {
            // 在等待期间保持或平滑回到原始大小（非强制恢复）
            cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, originalOrthoSize, zoomOutSpeed * Time.deltaTime);
        }

        lastTargetPosition = target.position;
    }

    private Vector3 ClampToBounds(Vector3 camWorldPos)
    {
        if (cam == null) return camWorldPos;

        // 优先使用 Collider2D 约束（类 CinemachineConfiner 思路）
        if (useColliderConfiner && confinerCollider != null)
        {
            // 2D 项目：玩家在 XY 平面移动（Z 不变）。
            // - Orthographic: 用投影视野矩形 corners 直接约束
            // - Perspective: 计算视锥与平面(z=const)的交点四边形，再约束
            if (cam.orthographic)
                return ConfineOrthoByCollider2D(camWorldPos, confinerCollider);

            return ConfinePerspectiveByCollider2D(camWorldPos, confinerCollider);
        }

        // 没有 collider 时，才使用 rect（仅对 orthographic 合理；透视会直接返回不处理）
        if (!cam.orthographic) return camWorldPos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector2 rightXY = new Vector2(transform.right.x, transform.right.y);
        Vector2 upXY = new Vector2(transform.up.x, transform.up.y);

        if (rightXY.sqrMagnitude < 1e-8f) rightXY = Vector2.right;
        if (upXY.sqrMagnitude < 1e-8f) upXY = Vector2.up;

        float extentX = halfW * Mathf.Abs(rightXY.x) + halfH * Mathf.Abs(upXY.x);
        float extentY = halfW * Mathf.Abs(rightXY.y) + halfH * Mathf.Abs(upXY.y);

        float minX = bounds.xMin + extentX;
        float maxX = bounds.xMax - extentX;
        float minY = bounds.yMin + extentY;
        float maxY = bounds.yMax - extentY;

        camWorldPos.x = (minX <= maxX) ? Mathf.Clamp(camWorldPos.x, minX, maxX) : bounds.center.x;
        camWorldPos.y = (minY <= maxY) ? Mathf.Clamp(camWorldPos.y, minY, maxY) : bounds.center.y;
        return camWorldPos;
    }

    private float GetConfinePlaneZ()
    {
        if (overrideConfinePlaneZ) return confinePlaneZ;
        if (target != null) return target.position.z;
        return 0f;
    }

    private Vector3 ConfineOrthoByCollider2D(Vector3 camWorldPos, Collider2D collider)
    {
        if (cam == null) return camWorldPos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector2 rightXY = new Vector2(transform.right.x, transform.right.y);
        Vector2 upXY = new Vector2(transform.up.x, transform.up.y);

        if (rightXY.sqrMagnitude < 1e-8f) rightXY = Vector2.right;
        if (upXY.sqrMagnitude < 1e-8f) upXY = Vector2.up;

        rightXY.Normalize();
        upXY.Normalize();

        for (int iter = 0; iter < confinerIterations; iter++)
        {
            Vector2 center2 = new Vector2(camWorldPos.x, camWorldPos.y);

            Vector2[] corners = new Vector2[4]
            {
                center2 + (-rightXY * halfW) + (-upXY * halfH),
                center2 + (-rightXY * halfW) + ( upXY * halfH),
                center2 + ( rightXY * halfW) + (-upXY * halfH),
                center2 + ( rightXY * halfW) + ( upXY * halfH),
            };

            Vector2 totalDelta = Vector2.zero;
            int outsideCount = 0;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 corner = corners[i];
                Vector2 closest = collider.ClosestPoint(corner);
                Vector2 delta = closest - corner;
                if (delta.sqrMagnitude > 1e-8f)
                {
                    totalDelta += delta;
                    outsideCount++;
                }
            }

            if (outsideCount == 0)
                break;

            Vector2 avgDelta = totalDelta / outsideCount;
            camWorldPos.x += avgDelta.x;
            camWorldPos.y += avgDelta.y;
        }

        return camWorldPos;
    }

    private Vector3 ConfinePerspectiveByCollider2D(Vector3 camWorldPos, Collider2D collider)
    {
        if (cam == null) return camWorldPos;

        float planeZ = GetConfinePlaneZ();

        for (int iter = 0; iter < confinerIterations; iter++)
        {
            Vector2[] quad = GetPerspectiveViewportQuadOnPlaneXY(camWorldPos, planeZ);
            if (quad == null) return camWorldPos;

            Vector2 totalDelta = Vector2.zero;
            int outsideCount = 0;

            for (int i = 0; i < quad.Length; i++)
            {
                Vector2 p = quad[i];
                Vector2 closest = collider.ClosestPoint(p);

                // delta 指向边界内部（点在内部时为 0）
                Vector2 delta = closest - p;

                // margin：让约束更“严格”（像把边界向里收缩）
                if (delta.sqrMagnitude > 1e-8f)
                {
                    if (confinerMargin > 0f)
                    {
                        float mag = delta.magnitude;
                        // 将推回量增加 margin（避免视觉上“刚好碰到还会动/穿一点”）
                        delta = delta * ((mag + confinerMargin) / mag);
                    }

                    totalDelta += delta;
                    outsideCount++;
                }
            }

            if (outsideCount == 0)
                break;

            Vector2 avgDelta = totalDelta / outsideCount;
            camWorldPos.x += avgDelta.x;
            camWorldPos.y += avgDelta.y;
        }

        return camWorldPos;
    }

    private Vector2[] GetPerspectiveViewportQuadOnPlaneXY(Vector3 candidateCamPos, float planeZ)
    {
        // 用 ViewportPointToRay (0..1) 避免 pixelWidth/Height 在某些编辑器状态下为 0 的问题
        Vector3 oldPos = transform.position;
        transform.position = new Vector3(candidateCamPos.x, candidateCamPos.y, candidateCamPos.z);

        try
        {
            Vector2 p00 = RaycastViewportToPlaneXY(new Vector2(0f, 0f), planeZ, out bool ok00);
            Vector2 p01 = RaycastViewportToPlaneXY(new Vector2(0f, 1f), planeZ, out bool ok01);
            Vector2 p10 = RaycastViewportToPlaneXY(new Vector2(1f, 0f), planeZ, out bool ok10);
            Vector2 p11 = RaycastViewportToPlaneXY(new Vector2(1f, 1f), planeZ, out bool ok11);

            if (!ok00 || !ok01 || !ok10 || !ok11)
                return null;

            return new Vector2[4] { p00, p01, p11, p10 };
        }
        finally
        {
            transform.position = oldPos;
        }
    }

    private Vector2 RaycastViewportToPlaneXY(Vector2 viewport, float planeZ, out bool ok)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));

        float dz = ray.direction.z;
        if (Mathf.Abs(dz) < 1e-6f)
        {
            ok = false;
            return default;
        }

        float t = (planeZ - ray.origin.z) / dz;
        if (t < 0f)
        {
            ok = false;
            return default;
        }

        Vector3 hit = ray.origin + ray.direction * t;
        ok = true;
        return new Vector2(hit.x, hit.y);
    }

    // 旧接口保留（避免外部引用报错），内部转到 viewport 版本
    private Vector2 RaycastScreenToPlaneXY(Vector2 screen, float planeZ, out bool ok)
    {
        // 尝试把 screen 当作 viewport(0..1) 使用时的兼容：若传入像素值则效果不保证。
        return RaycastViewportToPlaneXY(screen, planeZ, out ok);
    }

    void OnDrawGizmosSelected()
    {
        // 绘制死区（支持 handle 或 Inspector 数值），考虑相机旋转（将矩阵设置为相机的旋转）
        Vector2 halfSize;
        Vector2 offsetLocal;
        if (deadZoneHandle != null)
        {
            halfSize = new Vector2(Mathf.Abs(deadZoneHandle.localScale.x), Mathf.Abs(deadZoneHandle.localScale.y)) * 0.5f;
            offsetLocal = deadZoneHandle.localPosition;
        }
        else
        {
            halfSize = deadZoneSize * 0.5f;
            offsetLocal = deadZoneOffset;
        }

        Vector3 camPos = transform.position;
        Vector3 center = camPos + transform.right * offsetLocal.x + transform.up * offsetLocal.y;

        // 保存原矩阵
        Matrix4x4 oldMat = Gizmos.matrix;
        // 将 Gizmos 矩阵调整为以相机旋转为方向，这样 DrawCube 可以按相机本地轴绘制
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(Vector3.zero, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0.01f));
        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0.01f));

        // 恢复矩阵
        Gizmos.matrix = oldMat;

        // 在场景视图显示“最终缩小的相机视野大小”（仅当指定了 target 时）
        if (target == null) return;

        // 尽量获取相机（组件或 Camera.main）
        Camera sceneCam = cam != null ? cam : GetComponent<Camera>() ?? Camera.main;
        if (sceneCam == null) return;

        // 仅支持正交相机的可视化（2D）
        if (!sceneCam.orthographic) return;

        // 原始大小的蓝色边框（当前相机大小）
        float curHalfHeight = sceneCam.orthographicSize;
        float curHalfWidth = curHalfHeight * sceneCam.aspect;

        Vector3 targetCenter = new Vector3(target.position.x, target.position.y, transform.position.z);

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.25f);
        Gizmos.DrawCube(targetCenter, new Vector3(curHalfWidth * 2f, curHalfHeight * 2f, 0.01f));
        Gizmos.color = new Color(0f, 0.9f, 1f, 1f);
        Gizmos.DrawWireCube(targetCenter, new Vector3(curHalfWidth * 2f, curHalfHeight * 2f, 0.01f));

        // 目标缩小后的黄色边框（显示最终缩小的大小）
        float targetHalfHeight = targetOrthographicSize;
        float targetHalfWidth = targetHalfHeight * sceneCam.aspect;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.2f);
        Gizmos.DrawCube(targetCenter, new Vector3(targetHalfWidth * 2f, targetHalfHeight * 2f, 0.01f));
        Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
        Gizmos.DrawWireCube(targetCenter, new Vector3(targetHalfWidth * 2f, targetHalfHeight * 2f, 0.01f));

        // 新增：绘制相机边界（世界坐标矩形）
        if (drawBoundsGizmos && enableBounds)
        {
            Vector3 p1 = new Vector3(bounds.xMin, bounds.yMin, transform.position.z);
            Vector3 p2 = new Vector3(bounds.xMax, bounds.yMin, transform.position.z);
            Vector3 p3 = new Vector3(bounds.xMax, bounds.yMax, transform.position.z);
            Vector3 p4 = new Vector3(bounds.xMin, bounds.yMax, transform.position.z);

            Color old = Gizmos.color;
            Gizmos.color = boundsGizmoColor;
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
            Gizmos.color = old;
        }
    }

    void OnDrawGizmos()
    {
        // 新增：未选中相机时也绘制边界，方便在场景里明确看到大小与位置
        if (!drawBoundsGizmos || !enableBounds) return;

        Vector3 p1 = new Vector3(bounds.xMin, bounds.yMin, transform.position.z);
        Vector3 p2 = new Vector3(bounds.xMax, bounds.yMin, transform.position.z);
        Vector3 p3 = new Vector3(bounds.xMax, bounds.yMax, transform.position.z);
        Vector3 p4 = new Vector3(bounds.xMin, bounds.yMax, transform.position.z);

        Color old = Gizmos.color;
        Gizmos.color = boundsGizmoColor;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        Gizmos.color = old;
    }
}