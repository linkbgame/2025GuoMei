using System;
using System.Collections;
using ClueBoard;
using Inventory.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ObserveMode
{
    /// <summary>
    /// 观察模式主控：监听 V 键 Toggle，进入时给画面叠 tint 滤镜+生成可见目标的描边，
    /// 持续 <see cref="ObserveModeSO.durationSeconds"/> 秒后自动退出，1 秒冷却。
    /// 背包/线索板/对话/小游戏场景中按 V 仅播放拒绝音。
    /// 玩家移动不受影响（不调 PlayerStateManager）。
    /// </summary>
    [DisallowMultipleComponent]
    public class ObserveModeController : MonoBehaviour
    {
        public static ObserveModeController Instance { get; private set; }

        public enum ObserveState
        {
            Idle,
            FadingIn,
            Active,
            FadingOut,
            Cooldown,
        }

        [Header("配置")]
        [Tooltip("观察模式参数 SO，建议放 Assets/Lrb_Assets/_Data/ObserveMode/ObserveModeConfig.asset。")]
        [SerializeField] private ObserveModeSO config;

        [Header("场景引用")]
        [Tooltip("独立 Overlay Canvas 下的全屏 Image，进入观察时驱动其 alpha。")]
        [SerializeField] private Image fullScreenTint;

        [Tooltip("用来播放进入/退出/拒绝音效的 AudioSource。")]
        [SerializeField] private AudioSource audioSource;

        [Header("调试")]
        [Tooltip("打印状态切换、扫描数量、屏蔽原因等日志。")]
        [SerializeField] private bool verboseLog = false;

        public event Action OnEnter;
        public event Action OnExit;

        public bool IsActive => _state == ObserveState.Active || _state == ObserveState.FadingIn;
        public ObserveState CurrentState => _state;

        private ObserveState _state = ObserveState.Idle;
        private Coroutine _runRoutine;
        private readonly ObserveOutlineFactory _outlineFactory = new ObserveOutlineFactory();

        // 对话/背包 UI 的引用都来自其他模块，可能在场景切换时被销毁/重建。
        // 这里全部延迟到使用时查找，并在 sceneLoaded 时清缓存。
        private Fy_DialogManager _dialogManagerCache;
        private UIInventoryPage _inventoryPageCache;

        #region Unity lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化 tint 颜色为配置颜色，但 alpha 强制为 0（避免出场即可见）
            if (fullScreenTint != null && config != null)
            {
                Color c = config.tintColor;
                c.a = 0f;
                fullScreenTint.color = c;
                fullScreenTint.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (config == null) return;
            if (!Input.GetKeyDown(config.toggleKey)) return;

            HandleToggleKey();
        }

        #endregion

        #region key handling

        private void HandleToggleKey()
        {
            switch (_state)
            {
                case ObserveState.Idle:
                {
                    if (IsBlocked(out string reason))
                    {
                        if (verboseLog) Debug.Log($"[ObserveMode] 屏蔽：{reason}");
                        PlayClip(config.rejectClip);
                        return;
                    }
                    BeginEnter();
                    break;
                }
                case ObserveState.Active:
                case ObserveState.FadingIn:
                {
                    // 观察期间再次按 V → 提前退出
                    BeginExit(immediate: false);
                    break;
                }
                case ObserveState.FadingOut:
                case ObserveState.Cooldown:
                {
                    // 退出/冷却期间按 V 不响应、不播拒绝音（避免连按打扰）
                    if (verboseLog) Debug.Log($"[ObserveMode] 状态 {_state}，忽略 V。");
                    break;
                }
            }
        }

        #endregion

        #region blocking

        /// <summary>
        /// 聚合屏蔽判断：背包打开 / 线索板打开 / 对话进行中 / 当前在屏蔽场景里 → 返回 true。
        /// </summary>
        public bool IsBlocked(out string reason)
        {
            // 1. 背包
            UIInventoryPage invPage = GetInventoryPage();
            if (invPage != null && invPage.gameObject.activeSelf)
            {
                reason = "Inventory UI is open";
                return true;
            }

            // 2. 线索板
            if (ClueBoardController.Instance != null
                && ClueBoardController.Instance.clueBoardUI != null
                && ClueBoardController.Instance.clueBoardUI.isOpen)
            {
                reason = "ClueBoard UI is open";
                return true;
            }

            // 3. 对话
            var dlg = GetDialogManager();
            if (dlg != null && dlg.IsTalking)
            {
                reason = "Dialog is talking";
                return true;
            }

            // 4. 场景白名单
            if (config != null && config.blockingScenes != null)
            {
                string scene = SceneManager.GetActiveScene().name;
                for (int i = 0; i < config.blockingScenes.Length; i++)
                {
                    if (config.blockingScenes[i] == scene)
                    {
                        reason = $"Scene '{scene}' in blockingScenes";
                        return true;
                    }
                }
            }

            reason = null;
            return false;
        }

        public bool IsBlocked()
        {
            return IsBlocked(out _);
        }

        // 不修改 InventoryController 的可见性，直接在场景里找 UIInventoryPage 实例缓存起来。
        private UIInventoryPage GetInventoryPage()
        {
            if (_inventoryPageCache != null) return _inventoryPageCache;
            _inventoryPageCache = FindObjectOfType<UIInventoryPage>(includeInactive: true);
            return _inventoryPageCache;
        }

        private Fy_DialogManager GetDialogManager()
        {
            if (_dialogManagerCache != null) return _dialogManagerCache;
            _dialogManagerCache = FindObjectOfType<Fy_DialogManager>();
            return _dialogManagerCache;
        }

        #endregion

        #region state transitions

        private void BeginEnter()
        {
            if (verboseLog) Debug.Log("[ObserveMode] BeginEnter");

            // 清掉旧协程
            if (_runRoutine != null) StopCoroutine(_runRoutine);
            _runRoutine = StartCoroutine(EnterAndRunRoutine());

            PlayClip(config.enterClip);
            try { OnEnter?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

        private void BeginExit(bool immediate)
        {
            if (verboseLog) Debug.Log($"[ObserveMode] BeginExit (immediate={immediate})");

            if (_runRoutine != null) StopCoroutine(_runRoutine);
            _runRoutine = StartCoroutine(ExitAndCooldownRoutine(immediate));

            PlayClip(config.exitClip);
            try { OnExit?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>
        /// 由 sceneLoaded 在切到屏蔽场景时调用；立即清理视觉，跳过淡出。
        /// </summary>
        private void ForceExit()
        {
            if (_state == ObserveState.Idle || _state == ObserveState.Cooldown) return;

            if (verboseLog) Debug.Log("[ObserveMode] ForceExit due to scene change");
            if (_runRoutine != null) StopCoroutine(_runRoutine);
            _runRoutine = StartCoroutine(ExitAndCooldownRoutine(immediate: true));

            try { OnExit?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
        }

        private IEnumerator EnterAndRunRoutine()
        {
            // ---- FadingIn ----
            _state = ObserveState.FadingIn;

            // 扫描+创建轮廓（在 FadeIn 开始时一次性建立，避免半透明阶段空一帧）
            var targets = ObserveHighlightScanner.ScanVisibleTargets();
            if (verboseLog) Debug.Log($"[ObserveMode] 扫描到可见目标 {targets.Count} 个");
            _outlineFactory.CreateOutlines(targets, config);
            _outlineFactory.SetGlobalAlpha(0f);

            float targetTintAlpha = config != null ? config.tintColor.a : 0.35f;
            yield return FadeAlphaRoutine(0f, targetTintAlpha, config != null ? config.fadeInDuration : 0.3f);

            // ---- Active ----
            _state = ObserveState.Active;
            float duration = config != null ? config.durationSeconds : 15f;
            float t = 0f;
            while (t < duration)
            {
                if (_state != ObserveState.Active)
                {
                    // 状态被外部改写（例如 ForceExit），跳出 active 等待
                    yield break;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // 自然退出
            yield return ExitInternal(immediate: false);
        }

        private IEnumerator ExitAndCooldownRoutine(bool immediate)
        {
            yield return ExitInternal(immediate);
        }

        private IEnumerator ExitInternal(bool immediate)
        {
            // ---- FadingOut ----
            _state = ObserveState.FadingOut;

            float currentAlpha = fullScreenTint != null ? fullScreenTint.color.a : 0f;
            float fadeOut = immediate ? 0f : (config != null ? config.fadeOutDuration : 0.3f);
            yield return FadeAlphaRoutine(currentAlpha, 0f, fadeOut);

            // 清理轮廓
            _outlineFactory.ClearAll();

            // ---- Cooldown ----
            _state = ObserveState.Cooldown;
            float cd = config != null ? config.cooldownSeconds : 1f;
            float t = 0f;
            while (t < cd)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            _state = ObserveState.Idle;
            _runRoutine = null;
            if (verboseLog) Debug.Log("[ObserveMode] 回到 Idle");
        }

        /// <summary>
        /// 在指定时长内把 fullScreenTint.alpha 从 from 渐变到 to，并同步驱动描边整体 alpha。
        /// duration <= 0 时立即设到目标值。
        /// </summary>
        private IEnumerator FadeAlphaRoutine(float from, float to, float duration)
        {
            if (fullScreenTint == null)
            {
                _outlineFactory.SetGlobalAlpha(to > 0f ? 1f : 0f);
                yield break;
            }

            Color baseColor = config != null ? config.tintColor : fullScreenTint.color;

            if (duration <= 0f)
            {
                Color end = baseColor;
                end.a = to;
                fullScreenTint.color = end;
                _outlineFactory.SetGlobalAlpha(NormalizeOutlineAlpha(to));
                yield break;
            }

            float t = 0f;
            float targetMax = Mathf.Max(0.0001f, baseColor.a);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float a = Mathf.Lerp(from, to, k);

                Color c = baseColor;
                c.a = a;
                fullScreenTint.color = c;

                // 描边整体 alpha 与 tint alpha 同进度（按相对最大值归一化）
                _outlineFactory.SetGlobalAlpha(NormalizeOutlineAlpha(a, targetMax));

                yield return null;
            }

            Color final = baseColor;
            final.a = to;
            fullScreenTint.color = final;
            _outlineFactory.SetGlobalAlpha(NormalizeOutlineAlpha(to, targetMax));
        }

        private static float NormalizeOutlineAlpha(float currentTintAlpha, float maxTintAlpha = 1f)
        {
            if (maxTintAlpha <= 0.0001f) return currentTintAlpha > 0f ? 1f : 0f;
            return Mathf.Clamp01(currentTintAlpha / maxTintAlpha);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 切到屏蔽场景时立即清理观察状态
            if (config != null && config.blockingScenes != null)
            {
                for (int i = 0; i < config.blockingScenes.Length; i++)
                {
                    if (config.blockingScenes[i] == scene.name)
                    {
                        ForceExit();
                        return;
                    }
                }
            }

            // 场景切换后老的引用可能已经销毁，缓存清空
            _dialogManagerCache = null;
            _inventoryPageCache = null;
        }

        #endregion
    }
}
