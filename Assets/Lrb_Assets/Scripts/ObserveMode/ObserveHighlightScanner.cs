using System.Collections.Generic;
using UnityEngine;

namespace ObserveMode
{
    /// <summary>
    /// 候选目标扫描器：进入观察模式时一次性扫描场景里的可交互目标，
    /// 按 <see cref="Renderer.isVisible"/> 过滤出当前屏幕上能看到的对象，
    /// 返回需要画描边的源 SpriteRenderer 列表（已去重）。
    /// </summary>
    /// <remarks>
    /// 候选口径（按确认）：
    /// 1. 挂 NPCTakeSave 且组件启用、nowTalkingName 不为空 → 视为"还有未完成对话/事件"；
    /// 2. 挂 SceneItem（场景拾取物）；
    /// 3. 任意 MonoBehaviour 实现 IInteractable / IPuzzleInteractable（含 Chest）。
    ///
    /// 注：Renderer.isVisible 依赖被相机渲染过至少一帧。
    /// 如果目标进入相机后立刻按 V，可能漏一两帧——按确认接受这一边界。
    /// </remarks>
    public static class ObserveHighlightScanner
    {
        /// <summary>
        /// 扫描当前激活场景里的可见可交互目标。
        /// 调用方负责保留返回的列表，扫描器自身无状态。
        /// </summary>
        public static List<SpriteRenderer> ScanVisibleTargets()
        {
            var result = new HashSet<SpriteRenderer>();

            CollectFromNPCs(result);
            CollectFromSceneItems(result);
            CollectFromInteractables(result);

            var list = new List<SpriteRenderer>(result.Count);
            foreach (var sr in result) list.Add(sr);
            return list;
        }

        /// <summary>
        /// 在原列表上仅保留依然可见的目标。可由 controller 周期性调用以做"中途消失"清理，
        /// 但默认实现是进入时一次扫描，运行期不重扫，因此这是 optional。
        /// </summary>
        public static void FilterVisible(List<SpriteRenderer> targets)
        {
            if (targets == null) return;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var sr = targets[i];
                if (sr == null || !sr.isVisible)
                {
                    targets.RemoveAt(i);
                }
            }
        }

        #region private collectors

        private static void CollectFromNPCs(HashSet<SpriteRenderer> set)
        {
            var npcs = Object.FindObjectsOfType<NPCTakeSave>();
            for (int i = 0; i < npcs.Length; i++)
            {
                NPCTakeSave npc = npcs[i];
                if (npc == null) continue;
                if (!npc.isActiveAndEnabled) continue;
                // "未完成的对话/事件" 简化口径：nowTalkingName 不为空就算还有要谈的
                if (string.IsNullOrEmpty(npc.nowTalkingName)) continue;

                TryAddVisibleSpriteRenderer(npc.gameObject, set);
            }
        }

        private static void CollectFromSceneItems(HashSet<SpriteRenderer> set)
        {
            var items = Object.FindObjectsOfType<SceneItem>();
            for (int i = 0; i < items.Length; i++)
            {
                SceneItem item = items[i];
                if (item == null) continue;
                if (!item.isActiveAndEnabled) continue;
                TryAddVisibleSpriteRenderer(item.gameObject, set);
            }
        }

        private static void CollectFromInteractables(HashSet<SpriteRenderer> set)
        {
            // 注：FindObjectsOfType<MonoBehaviour>() 比专用接口扫描慢，
            // 但因为只在按 V 进入观察模式时执行一次，可以接受。
            var allMb = Object.FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < allMb.Length; i++)
            {
                MonoBehaviour mb = allMb[i];
                if (mb == null) continue;
                if (!mb.isActiveAndEnabled) continue;

                if (mb is IInteractable || mb is IPuzzleInteractable)
                {
                    TryAddVisibleSpriteRenderer(mb.gameObject, set);
                }
            }
        }

        /// <summary>
        /// 在 GameObject（含子物体）上找第一个 SpriteRenderer，并按 isVisible 过滤后加入集合。
        /// </summary>
        private static void TryAddVisibleSpriteRenderer(GameObject go, HashSet<SpriteRenderer> set)
        {
            if (go == null) return;
            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;
            if (!sr.enabled) return;
            if (!sr.isVisible) return;
            if (sr.sprite == null) return;
            set.Add(sr);
        }

        #endregion
    }
}
