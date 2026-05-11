using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    private CanvasGroup _fadeGroup;

    void Awake()
    {
        Instance = this;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        _fadeGroup = gameObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 1f;
        _fadeGroup.blocksRaycasts = false;

        var img = gameObject.AddComponent<Image>();
        img.color = Color.black;
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = Vector2.zero;
        img.rectTransform.offsetMax = Vector2.zero;
    }

    public void FadeIn(float duration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(DoFade(1f, 0f, duration));
    }

    public void FadeOut(float duration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(DoFade(0f, 1f, duration));
    }

    public IEnumerator DoFade(float from, float to, float duration)
    {
        float elapsed = 0f;
        _fadeGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _fadeGroup.alpha = to;
    }
}