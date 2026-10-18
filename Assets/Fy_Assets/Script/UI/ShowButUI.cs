using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowButUI : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private Image targetImage;

    [Tooltip("Also apply the same pulse to all child UI Graphics (Image/Text/etc.).")]
    [SerializeField] private bool affectChildren = true;

    [Tooltip("Include the current GameObject's non-Image Graphics as well (if any).")]
    [SerializeField] private bool includeSelfGraphics = true;

    [Tooltip("Pulses per second (fade in->out counts as 1 pulse).")]
    [Min(0.01f)]
    [SerializeField] private float pulseFrequency = 1f;

    [Tooltip("Minimum alpha during pulse.")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.2f;

    [Tooltip("Maximum alpha during pulse.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Tooltip("Use unscaled time so it still pulses when Time.timeScale = 0.")]
    [SerializeField] private bool useUnscaledTime = true;

    private float _t;

    private struct GraphicCache
    {
        public Graphic graphic;
        public Color baseColor;
    }

    private readonly List<GraphicCache> _graphics = new List<GraphicCache>();

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp01(maxAlpha);
        if (maxAlpha < minAlpha)
        {
            var tmp = maxAlpha;
            maxAlpha = minAlpha;
            minAlpha = tmp;
        }

        CacheGraphics();
    }

    private void OnTransformChildrenChanged()
    {
        if (affectChildren)
            CacheGraphics();
    }

    private void OnEnable()
    {
        // In case colors changed while disabled.
        CacheGraphics();
    }

    private void CacheGraphics()
    {
        _graphics.Clear();

        // Always keep the main image behavior (for backwards compatibility)
        if (targetImage != null)
        {
            _graphics.Add(new GraphicCache { graphic = targetImage, baseColor = targetImage.color });
        }

        if (!affectChildren && !includeSelfGraphics)
            return;

        // Cache other Graphics so children can pulse too (Text, RawImage, etc.)
        // Note: Image is also a Graphic; we handle duplicates safely.
        var all = GetComponentsInChildren<Graphic>(true);
        foreach (var g in all)
        {
            if (g == null)
                continue;

            bool isSelf = g.gameObject == gameObject;
            if (isSelf && !includeSelfGraphics)
                continue;
            if (!isSelf && !affectChildren)
                continue;

            // Avoid duplicates (e.g., targetImage already added)
            bool exists = false;
            for (int i = 0; i < _graphics.Count; i++)
            {
                if (_graphics[i].graphic == g)
                {
                    exists = true;
                    break;
                }
            }
            if (exists)
                continue;

            _graphics.Add(new GraphicCache { graphic = g, baseColor = g.color });
        }
    }

    private void Update()
    {
        if (_graphics.Count == 0)
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _t += dt;

        // Sine wave: (sin+1)/2 => 0..1
        float wave = (Mathf.Sin(_t * Mathf.PI * 2f * pulseFrequency) + 1f) * 0.5f;
        float a = Mathf.Lerp(minAlpha, maxAlpha, wave);

        for (int i = 0; i < _graphics.Count; i++)
        {
            var entry = _graphics[i];
            if (entry.graphic == null)
                continue;

            var c = entry.baseColor;
            c.a *= a;
            entry.graphic.color = c;
        }
    }
}
