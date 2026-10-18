using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

/// <summary>
/// Lightweight frame-by-frame animation player using Sprites (no Animator).
///
/// Runtime loading normally requires Resources/Addressables/AssetBundles.
///
/// This component supports two workflows:
/// 1) Recommended for "drop-in" usage: in the Editor it auto-collects sprites from
///    `Assets/Art/FlowerCar/` and serializes them into the scene/prefab, so runtime
///    playback does NOT rely on Resources.
/// 2) Fallback: load frames from Resources at `Assets/Resources/Art/FlowerCar/`.
///
/// Frame names expected: 花车_00001 ... 花车_00124
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class RunFlowerCar : MonoBehaviour
{
    [Header("Direct Frames (Auto)")]
    [Tooltip("Auto-populated in Editor from Assets/Art/FlowerCar. Runtime uses these without needing Resources.")]
    [SerializeField] private Sprite[] directFrames;

    [Tooltip("Editor-only: Source folder to auto-collect sprites. Default matches your art folder.")]
    [SerializeField] private string editorAssetFolder = "Assets/Art/FlowerCar";

    [Header("Frame Source (Resources)")]
    [Tooltip("Resources relative folder. Example: Art/FlowerCar")]
    [SerializeField] private string resourcesFolder = "Art/FlowerCar";

    [Tooltip("Sprite name prefix. Example: 花车_")]
    [SerializeField] private string namePrefix = "花车_";

    [Tooltip("How many frames to play.")]
    [Min(1)]
    [SerializeField] private int frameCount = 124;

    [Tooltip("Pad frame index to this digit count when generating names, e.g. 4 => 花车_0001.")]
    [Range(0, 8)]
    [SerializeField] private int indexZeroPadding = 5;

    [Header("Playback")]
    [Tooltip("Frames per second. Can be tuned at runtime.")]
    [Min(0.01f)]
    [SerializeField] private float framesPerSecond = 12f;

    [Tooltip("Play automatically on Start.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Loop animation.")]
    [SerializeField] private bool loop = true;

    [Tooltip("Use unscaled time (ignore Time.timeScale).")]
    [SerializeField] private bool unscaledTime = false;

    [Header("Performance")]
    [Tooltip("Use a SpriteAtlas if available (recommended) to reduce runtime overhead.")]
    [SerializeField] private SpriteAtlas spriteAtlas;

    [Tooltip("If true, preload all sprites into memory once (smoothest playback, more memory).")]
    [SerializeField] private bool preloadAllFrames = true;

    [Tooltip("If not preloading, keep this many frames cached around current frame.")]
    [Min(0)]
    [SerializeField] private int cacheWindow = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    [Tooltip("Preview first frame in editor when values change.")]
    [SerializeField] private bool previewInEditor = true;

    private SpriteRenderer _sr;

    // Preloaded mode: direct lookup.
    private Sprite[] _frames;

    // Streaming mode: small cache window.
    private readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>(32);

    private int _frameIndex;
    private float _accumulator;
    private bool _isPlaying;

    public float FramesPerSecond
    {
        get => framesPerSecond;
        set => framesPerSecond = Mathf.Max(0.01f, value);
    }

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
        // If user doesn't want to drag sprites, auto-collect them in editor and serialize.
        // This allows runtime playback without Resources.
        if ((directFrames == null || directFrames.Length == 0) && !Application.isPlaying)
        {
            TryAutoPopulateDirectFramesInEditor();
        }
#endif

        // Highest priority: direct frames assigned in inspector.
        if (directFrames != null && directFrames.Length > 0)
        {
            _frames = directFrames;
            frameCount = Mathf.Max(1, _frames.Length);
            preloadAllFrames = true; // frames already in memory
        }
        else if (preloadAllFrames)
        {
            _frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
                _frames[i] = LoadFrame(i);
        }

        // Show first frame immediately.
        SetFrame(0, force: true);

        if (_sr.sprite == null)
        {
            Debug.LogError(
                "[RunFlowerCar] First frame sprite is null. " +
                "This component can auto-collect sprites from Assets/Art/FlowerCar in the Editor. " +
                "If that folder is different, update 'Editor Asset Folder'. " +
                "Fallback runtime loading requires Resources: " +
                $"Assets/Resources/{resourcesFolder}/" +
                " (e.g. Assets/Resources/Art/FlowerCar/花车_00001.png).", this);
        }
    }

    private void OnValidate()
    {
        if (!previewInEditor) return;
        if (!Application.isPlaying)
        {
            _sr = GetComponent<SpriteRenderer>();
#if UNITY_EDITOR
            if ((directFrames == null || directFrames.Length == 0))
            {
                TryAutoPopulateDirectFramesInEditor();
            }
#endif
            if (_sr != null)
            {
                // Best-effort preview (won't log spam).
                Sprite s = null;
                if (directFrames != null && directFrames.Length > 0) s = directFrames[0];
                if (s == null) s = LoadFrame(0);
                if (s != null) _sr.sprite = s;
            }
        }
    }

#if UNITY_EDITOR
    private void TryAutoPopulateDirectFramesInEditor()
    {
        // Only valid in editor, avoids AssetDatabase usage at runtime
        if (string.IsNullOrWhiteSpace(editorAssetFolder)) return;

        // Find sprites under folder (includes subfolders)
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { editorAssetFolder });
        if (guids == null || guids.Length == 0) return;

        var list = new List<Sprite>(guids.Length);
        for (int n = 0; n < guids.Length; n++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[n]);
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null && s.name.StartsWith(namePrefix, StringComparison.Ordinal))
                list.Add(s);
        }

        if (list.Count == 0) return;

        // Sort by name to ensure 花车_00001..花车_00124 order.
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        // If frameCount was specified, clamp to that count.
        if (frameCount > 0 && list.Count > frameCount)
            list.RemoveRange(frameCount, list.Count - frameCount);
        else
            frameCount = list.Count;

        directFrames = list.ToArray();

        // Mark dirty so it persists on scene/prefab.
        EditorUtility.SetDirty(this);
    }
#endif

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float spf = 1f / Mathf.Max(0.01f, framesPerSecond);

        _accumulator += dt;

        // Catch up if rendering hiccups.
        while (_accumulator >= spf)
        {
            _accumulator -= spf;
            Step(1);
        }
    }

    public void Play() => _isPlaying = true;

    public void Pause() => _isPlaying = false;

    public void Stop(bool resetToFirstFrame = true)
    {
        _isPlaying = false;
        _accumulator = 0f;
        if (resetToFirstFrame) SetFrame(0, force: true);
    }

    public void SetSpeed(float fps) => FramesPerSecond = fps;

    public void SetFrameIndex(int index) => SetFrame(index, force: true);

    private void Step(int delta)
    {
        int next = _frameIndex + delta;

        if (loop)
        {
            next %= frameCount;
            if (next < 0) next += frameCount;
        }
        else
        {
            if (next >= frameCount)
            {
                next = frameCount - 1;
                _isPlaying = false;
            }
            else if (next < 0)
            {
                next = 0;
                _isPlaying = false;
            }
        }

        SetFrame(next, force: false);
    }

    private void SetFrame(int index, bool force)
    {
        index = Mathf.Clamp(index, 0, frameCount - 1);
        if (!force && index == _frameIndex) return;

        _frameIndex = index;

        Sprite sprite;
        if (preloadAllFrames)
        {
            sprite = (_frames != null && index < _frames.Length) ? _frames[index] : null;
        }
        else
        {
            sprite = GetCachedOrLoad(index);
            TrimCacheAround(index);
        }

        if (sprite == null)
        {
            if (verboseLog)
                Debug.LogWarning($"[RunFlowerCar] Missing sprite at frame {index}: {BuildSpriteName(index)}");
            return;
        }

        if (_sr.sprite != sprite)
            _sr.sprite = sprite;
    }

    private Sprite GetCachedOrLoad(int index)
    {
        if (_cache.TryGetValue(index, out var s) && s != null)
            return s;

        s = LoadFrame(index);
        _cache[index] = s;
        return s;
    }

    private void TrimCacheAround(int center)
    {
        if (cacheWindow <= 0)
        {
            _cache.Clear();
            return;
        }

        // Keep [center-cacheWindow, center+cacheWindow]
        int min = center - cacheWindow;
        int max = center + cacheWindow;

        if (_cache.Count <= (cacheWindow * 2 + 1))
            return;

        var keys = ListPool<int>.Get();
        try
        {
            foreach (var k in _cache.Keys) keys.Add(k);
            for (int i = 0; i < keys.Count; i++)
            {
                int k = keys[i];
                if (k < min || k > max)
                    _cache.Remove(k);
            }
        }
        finally
        {
            ListPool<int>.Release(keys);
        }
    }

    private Sprite LoadFrame(int index)
    {
        string spriteName = BuildSpriteName(index);

        // Prefer atlas.
        if (spriteAtlas != null)
        {
            var s = spriteAtlas.GetSprite(spriteName);
            if (s != null) return s;
        }

        // Fallback: Resources.Load<Sprite>
        // Example: Assets/Resources/Art/FlowerCar/花车_0001.png => Resources.Load<Sprite>("Art/FlowerCar/花车_0001")
        string resPath = resourcesFolder + "/" + spriteName;
        var sprite = Resources.Load<Sprite>(resPath);

        if (sprite == null && verboseLog)
            Debug.LogWarning($"[RunFlowerCar] Resources sprite not found: {resPath}. Ensure frames are under Assets/Resources/{resourcesFolder} and imported as Sprite.");

        return sprite;
    }

    private string BuildSpriteName(int index)
    {
        int oneBased = index + 1;
        return indexZeroPadding > 0
            ? namePrefix + oneBased.ToString().PadLeft(indexZeroPadding, '0')
            : namePrefix + oneBased;
    }

    // Small non-alloc list pool to avoid GC in TrimCache.
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>(8);

        public static List<T> Get()
        {
            lock (Pool)
            {
                if (Pool.Count > 0)
                {
                    var l = Pool.Pop();
                    l.Clear();
                    return l;
                }
            }
            return new List<T>(32);
        }

        public static void Release(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            lock (Pool)
            {
                if (Pool.Count < 32) Pool.Push(list);
            }
        }
    }
}
