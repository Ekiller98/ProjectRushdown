using UnityEngine;
using UnityEngine.UI;

public class TitleLogoAnimator : MonoBehaviour
{
    [Header("Intro")]
    public float introDuration = 0.8f;
    public float overshootAmount = 0.1f;        // 10% extra on first pop
    public float startScaleMultiplier = 0.6f;   // starts at 60% size

    [Header("Pulse (Scale)")]
    public bool enablePulse = true;
    public float pulseAmount = 0.03f;           // 3% in/out scale
    public float pulseSpeed = 1.5f;             // how fast it breathes

    [Header("Glow Color Pulse (Fill)")]
    public bool enableGlowPulse = true;
    public Color glowColorA = new Color(0.4f, 1f, 1f); // cyan
    public Color glowColorB = new Color(1f, 0.4f, 1f); // magenta
    public float glowSpeed = 1.5f;
    public float glowIntensity = 0.5f; // how much we lerp toward glow colors

    [Header("Outline Pulse (Edge Glow)")]
    public bool enableOutlinePulse = true;
    public Color outlineColorA = new Color(0.4f, 1f, 1f);
    public Color outlineColorB = new Color(1f, 0.4f, 1f);
    public float outlineSpeed = 2.0f;
    public float outlineMinAlpha = 0.2f;
    public float outlineMaxAlpha = 0.9f;

    private RectTransform _rect;
    private Graphic _graphic;
    private Outline _outline;
    private float _timer;

    private Vector3 finalScale;
    private Vector3 startScale;
    private Vector3 overshootScale;

    private Color baseColor;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _graphic = GetComponent<Graphic>();
        _outline = GetComponent<Outline>();

        // Final size = whatever you set in the RectTransform
        finalScale = _rect.localScale;

        startScale = finalScale * startScaleMultiplier;
        overshootScale = finalScale * (1f + overshootAmount);

        _rect.localScale = startScale;

        if (_graphic != null)
        {
            baseColor = _graphic.color;
            var c = baseColor;
            c.a = 0f;  // fade in from 0
            _graphic.color = c;
        }
    }

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer < introDuration)
        {
            RunIntro();
        }
        else
        {
            RunPulse();
        }
    }

    private void RunIntro()
    {
        float t = Mathf.Clamp01(_timer / introDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out

        Vector3 targetScale = Vector3.Lerp(startScale, overshootScale, eased);
        if (t >= 1f)
            targetScale = finalScale;

        _rect.localScale = targetScale;

        if (_graphic != null)
        {
            var c = baseColor;
            c.a = t;
            _graphic.color = c;
        }
    }

    private void RunPulse()
    {
        // -------- SCALE PULSE (breathing) --------
        Vector3 scale = finalScale;

        if (enablePulse)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            scale = finalScale * s;
        }

        _rect.localScale = scale;

        // -------- FILL COLOR GLOW --------
        if (_graphic != null)
        {
            Color c = baseColor;
            c.a = 1f;

            if (enableGlowPulse)
            {
                float t = (Mathf.Sin(Time.time * glowSpeed) + 1f) * 0.5f; // 0..1
                Color glowColor = Color.Lerp(glowColorA, glowColorB, t);

                // Blend between base color and the glow color
                c = Color.Lerp(c, glowColor, glowIntensity);
            }

            _graphic.color = c;
        }

        // -------- OUTLINE EDGE GLOW --------
        if (_outline != null && enableOutlinePulse)
        {
            float t = (Mathf.Sin(Time.time * outlineSpeed) + 1f) * 0.5f;

            Color edge = Color.Lerp(outlineColorA, outlineColorB, t);
            float alpha = Mathf.Lerp(outlineMinAlpha, outlineMaxAlpha, t);
            edge.a = alpha;

            _outline.effectColor = edge;
        }
    }
}
