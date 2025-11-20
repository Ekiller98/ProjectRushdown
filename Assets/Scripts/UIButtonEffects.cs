using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale")]
    public float hoverScale = 1.05f;
    public float clickScale = 0.9f;
    public float lerpSpeed = 12f;

    [Header("Glow / Tint")]
    public Image buttonImage;              // assign your ButtonArt child here
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 1f);   // start same, you can tint in Inspector

    [Header("Audio")]
    public AudioSource audioSource;        // can be on this object or parent
    public AudioClip hoverClip;
    public AudioClip clickClip;

    private Vector3 _baseScale;
    private Vector3 _targetScale;
    private Color _targetColor;
    private bool _isHovered;

    void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;

        if (buttonImage == null)
            buttonImage = GetComponentInChildren<Image>();

        if (buttonImage != null)
            buttonImage.color = normalColor;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // force 2D
    }
   

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * lerpSpeed);

        // Smooth color
        if (buttonImage != null)
        {
            Color current = buttonImage.color;
            Color target = _isHovered ? hoverColor : normalColor;
            buttonImage.color = Color.Lerp(current, target, Time.deltaTime * lerpSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _targetScale = _baseScale * hoverScale;

        if (hoverClip != null && audioSource != null)
        {
            // 3.0f = 3x louder than normal
            GlobalUISounds.Instance.PlayHover();
        }
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _targetScale = _baseScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Squish then pop back (respecting hover state)
        StopAllCoroutines();
        StartCoroutine(ClickSquishRoutine());

        if (clickClip != null && audioSource != null)
        {
            // 4.0f = 4x louder than normal
            GlobalUISounds.Instance.PlayClick();
        }
    }

    private IEnumerator ClickSquishRoutine()
    {
        // quick squish
        _targetScale = _baseScale * clickScale;
        yield return new WaitForSeconds(0.06f);

        // return to hover/base scale
        _targetScale = _isHovered ? _baseScale * hoverScale : _baseScale;
    }
}
