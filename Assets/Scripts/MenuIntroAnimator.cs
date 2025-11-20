using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuIntroAnimator : MonoBehaviour
{
    [Header("Frame Reveal")]
    public Image frameImage;
    public float frameFillDuration = 0.8f;

    [Header("Buttons Fade In")]
    public CanvasGroup buttonsGroup;
    public float buttonsFadeDelay = 0.1f;
    public float buttonsFadeDuration = 0.5f;

    [Header("Logo Fade (Image Only)")]
    public Image logoImage;        // <-- FIXED: now uses Image
    public float logoFadeDuration = 0.6f;

    private void Start()
    {
        // Frame starts invisible
        if (frameImage != null)
            frameImage.fillAmount = 0f;

        // Buttons start invisible + disabled
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        // Logo starts invisible (via alpha)
        if (logoImage != null)
        {
            Color c = logoImage.color;
            c.a = 0f;
            logoImage.color = c;
        }

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // 1) Fill frame animation
        if (frameImage != null)
        {
            float t = 0f;
            while (t < frameFillDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / frameFillDuration);
                frameImage.fillAmount = lerp;
                yield return null;
            }
            frameImage.fillAmount = 1f;
        }

        // 2) Fade in logo (Image alpha)
        if (logoImage != null)
        {
            float t = 0f;
            while (t < logoFadeDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / logoFadeDuration);

                Color c = logoImage.color;
                c.a = lerp;
                logoImage.color = c;

                yield return null;
            }

            // Ensure full alpha
            Color final = logoImage.color;
            final.a = 1f;
            logoImage.color = final;
        }

        // 3) Fade in buttons
        yield return new WaitForSeconds(buttonsFadeDelay);

        if (buttonsGroup != null)
        {
            float t = 0f;
            while (t < buttonsFadeDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / buttonsFadeDuration);
                buttonsGroup.alpha = lerp;
                yield return null;
            }

            buttonsGroup.alpha = 1f;
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }
    }
}
