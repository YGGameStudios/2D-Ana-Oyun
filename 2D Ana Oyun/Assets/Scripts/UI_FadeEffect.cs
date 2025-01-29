using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_FadeEffect : MonoBehaviour
{
    [SerializeField] private Image fadeImage;

    public void ScreenFade(float targetAlpha, float duration, System.Action onComplate)
    {
        StartCoroutine(FadeCoroutine(targetAlpha, duration, onComplate));
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration, System.Action onComplate)
    {
        float time = 0f;

        Color currentColor = fadeImage.color;
        float startAlpha = currentColor.a;

        while (time < duration)
        {
            time += Time.deltaTime;

            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);

        onComplate?.Invoke();
    }
}
