using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A reusable full-screen fade-to-black overlay. Sits on top of everything (created under the
// Canvas as the last sibling). Call PlayTransition(midAction) to: fade to black, run midAction
// (e.g. wipe the inbox and load the next day), then fade back in.
public class DayTransition : MonoBehaviour
{
    public static DayTransition I { get; private set; }

    [Tooltip("The Canvas (or any full-screen RectTransform) to parent the black overlay under.")]
    public RectTransform overlayParent;

    [Tooltip("Seconds to fade out, hold, and fade in.")]
    public float fadeOut = 1.2f;
    public float hold = 0.8f;
    public float fadeIn = 1.2f;

    private Image _black;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void EnsureOverlay()
    {
        if (_black != null) return;
        var parent = overlayParent != null ? overlayParent : (RectTransform)transform;
        var go = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _black = go.GetComponent<Image>();
        _black.color = Color.black;
        _black.raycastTarget = true;        // blocks clicks during the transition
        SetAlpha(0f);
        go.transform.SetAsLastSibling();    // on top of everything
    }

    // Fade to black, run midAction at full black, fade back in.
    public void Play(Action midAction)
    {
        EnsureOverlay();
        StartCoroutine(Run(midAction));
    }

    private IEnumerator Run(Action midAction)
    {
        _black.transform.SetAsLastSibling();

        yield return Fade(0f, 1f, fadeOut);
        yield return new WaitForSeconds(hold * 0.5f);

        midAction?.Invoke();               // do the day switch while fully black

        yield return new WaitForSeconds(hold * 0.5f);
        yield return Fade(1f, 0f, fadeIn);
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        var c = _black.color; c.a = a; _black.color = c;
        _black.raycastTarget = a > 0.01f;
    }
}
