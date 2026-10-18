using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClueBoard.UI
{
    public class MiniMapCamera : MonoBehaviour
    {
        // --- 状态枚举 ---
        private enum CameraState
        {
            FollowPlayer,       // 默认状态：跟随玩家，打开地图后固定在玩家位置
            ManualPan,          // 手动拖拽状态：由 UIClueBoard 控制位置
            FollowBrush,        // 跟随毛笔状态：跟随 UIClueBoard 传来的世界位置 (New!)
            SmoothReturn        // 平滑回归状态：Lerp回玩家位置
        }
        private CameraState currentState = CameraState.FollowPlayer;

        [Header("跟随与缩放")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float defaultZoom = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private bool autoReturnEnabled = true;

        [Header("平滑回归设置")]
        [SerializeField] private float returnSpeed = 5f; // Lerp速度
        [SerializeField] private float brushFollowSmooth = 0.125f; // 毛笔跟随平滑度 (越小越快)

        [HideInInspector]
        public Camera miniCamera;

        private Vector3 brushTargetWorldPos; // 缓存毛笔在世界空间的目标位置
        private Vector3 followVelocity = Vector3.zero; // 用于 SmoothDamp

        private void Awake()
        {
            miniCamera = GetComponent<Camera>();
            miniCamera.orthographicSize = defaultZoom;
        }

        private void LateUpdate()
        {
            switch (currentState)
            {
                case CameraState.FollowPlayer:
                    FollowPlayerLogic();
                    break;
                case CameraState.ManualPan:
                    // 在 ManualPan 状态下，位置由外部（UIClueBoard）直接控制，LateUpdate 不做额外处理
                    break;
                case CameraState.FollowBrush:
                    FollowBrushLogic();
                    break;
                case CameraState.SmoothReturn:
                    SmoothReturnLogic();
                    break;
            }
        }

        /// <summary>
        /// 跟随玩家
        /// </summary>
        private void FollowPlayerLogic()
        {
            if (playerTarget != null)
            {
                Vector3 targetPos = playerTarget.position;
                targetPos.z = transform.position.z;
                transform.position = targetPos;
            }
        }

        // 状态 B: 跟随毛笔逻辑 (New!)
        private void FollowBrushLogic()
        {
            Vector3 targetPos = brushTargetWorldPos;
            targetPos.z = transform.position.z;

            // 使用 SmoothDamp 实现比单纯 Lerp 更自然的跟随感
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref followVelocity, brushFollowSmooth);
        }

        /// <summary>
        /// 平滑回归主角位置
        /// </summary>
        private void SmoothReturnLogic()
        {
            if (playerTarget == null)
            {
                currentState = CameraState.FollowPlayer; // 没有玩家，直接切回跟随（虽然没目标）
                return;
            }

            Vector3 targetPos = playerTarget.position;
            targetPos.z = transform.position.z;

            // 使用 Lerp 进行平滑插值
            transform.position = Vector3.Lerp(transform.position, targetPos, returnSpeed * Time.deltaTime);

            // 判断是否足够接近目标，如果足够接近则切换回 FollowPlayer 状态
            if (Vector3.Distance(transform.position, targetPos) < 0.05f)
            {
                transform.position = targetPos; // 确保最终位置精确
                currentState = CameraState.FollowPlayer;
                //Debug.Log("小地图相机：平滑回归完成，进入跟随状态。");
            }
        }

        /// <summary>
        /// 设置手动拖拽/平移状态
        /// </summary>
        public void StartManualPan()
        {
            currentState = CameraState.ManualPan;
        }

        /// <summary>
        /// 结束手动拖拽，开始平滑回归
        /// </summary>
        public void StopManualPanAndStartReturn()
        {
            // 如果开启了自动返回，则进入平滑回归状态；否则保持手动位置
            if (autoReturnEnabled)
            {
                currentState = CameraState.SmoothReturn;
            }
            else
            {
                currentState = CameraState.ManualPan;
            }
        }

        /// <summary>
        ///  拖拽/平移命令：在 ManualPan 状态下，UIClueBoard 每帧调用
        /// </summary>
        /// <param name="worldPanDelta"></param>
        public void Pan(Vector3 worldPanDelta)
        {
            if (currentState == CameraState.ManualPan)
            {
                transform.position += worldPanDelta;
            }
        }

        /// <summary>
        /// 重置缩放
        /// </summary>
        public void ResetZoomToDefault()
        {
            miniCamera.orthographicSize = defaultZoom;
        }

        /// <summary>
        /// 缩放命令：接收一个缩放变化量并调整相机的 orthographicSize。
        /// zoomDelta 为正表示放大（orthographicSize 变小），为负表示缩小。
        /// </summary>
        public void Zoom(float zoomDelta)
        {
            // 计算新的 orthographicSize
            // orthographicSize 越小，画面越放大（Zoom In），信息量越少
            float newSize = miniCamera.orthographicSize - zoomDelta;

            // 使用 Mathf.Clamp 限制缩放值，确保它在 [minZoom, maxZoom] 范围内
            miniCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }

        /// <summary>
        /// 开始跟随毛笔（由 UIClueBoard 在划线开始时调用）
        /// </summary>
        public void StartFollowBrush(Vector3 initialWorldPos)
        {
            brushTargetWorldPos = initialWorldPos;
            currentState = CameraState.FollowBrush;
        }

        /// <summary>
        /// 更新毛笔位置（由 UIClueBoard 在划线过程中每帧调用）
        /// 需要 UIClueBoard 将 UI 坐标转换为对应的世界坐标后传入
        /// </summary>
        public void UpdateBrushPosition(Vector3 worldPos)
        {
            if (currentState == CameraState.FollowBrush)
            {
                brushTargetWorldPos = worldPos;
            }
        }

        /// <summary>
        /// 停止跟随毛笔，回归玩家（由 UIClueBoard 在划线结束时调用）
        /// </summary>
        public void StopFollowBrush()
        {
            if (currentState == CameraState.FollowBrush)
            {
                currentState = CameraState.SmoothReturn;
            }
        }

        /// <summary>
        /// 设置自动返回开关状态
        /// </summary>
        public void SetAutoReturn(bool enabled)
        {
            autoReturnEnabled = enabled;
        }

        public void ForceResetToPlayer()
        {
            if (playerTarget == null) return;

            // 立即停止所有状态，切换回跟随玩家
            currentState = CameraState.FollowPlayer;

            // 瞬间同步位置
            Vector3 targetPos = playerTarget.position;
            targetPos.z = transform.position.z;
            transform.position = targetPos;

            // 重置缩放
            miniCamera.orthographicSize = defaultZoom;
        }
    }
}