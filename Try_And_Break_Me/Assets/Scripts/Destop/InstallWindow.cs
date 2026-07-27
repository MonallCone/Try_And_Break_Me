using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A forced install window. It appears on its own (the player didn't ask for it), fills a progress
// bar by itself, and closes when done. No buttons, no choice \u2014 the quiet wrongness of a computer
// doing things to itself. On completion it invokes onComplete (used to reveal the AI Friend icon).
//
// MonoBehaviour because it animates the progress bar over time.
public class InstallWindow : MonoBehaviour
{
    private Image _barFill;
    private TMP_Text _status;
    private DraggableWindow _window;
    private WindowManager _manager;
    private System.Action _onComplete;
    private float _progress;

    public static InstallWindow Launch(WindowManager manager, System.Action onComplete)
    {
        var win = manager.OpenWindow("Installing\u2026", new Vector2(420f, 180f));
        var comp = win.ContentArea.gameObject.AddComponent<InstallWindow>();
        comp._window = win;
        comp._manager = manager;
        comp._onComplete = onComplete;
        comp.Build(win.ContentArea);
        return comp;
    }

    private void Build(RectTransform content)
    {
        var root = NewRect(content, "InstallRoot");
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.95f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 18, 18); vlg.spacing = 12f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var title = MakeText(root, "AI Virtual Friend", 20, FontStyles.Bold, 30);
        title.alignment = TextAlignmentOptions.Center;

        _status = MakeText(root, "Preparing installation\u2026", 14, FontStyles.Normal, 22);
        _status.alignment = TextAlignmentOptions.Center;

        // Progress bar (track + fill)
        var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackGo.GetComponent<RectTransform>().SetParent(root, false);
        trackGo.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.83f);
        var trackLe = trackGo.AddComponent<LayoutElement>();
        trackLe.minHeight = 22f; trackLe.preferredHeight = 22f;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.SetParent(trackGo.transform, false);
        fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = new Vector2(0, 0);
        fillRt.sizeDelta = new Vector2(0, 0);
        _barFill = fillGo.GetComponent<Image>();
        _barFill.color = new Color(0.3f, 0.55f, 0.85f);

        StartCoroutine(RunInstall(trackGo.GetComponent<RectTransform>(), fillRt));
    }

    private IEnumerator RunInstall(RectTransform track, RectTransform fill)
    {
        yield return new WaitForSeconds(0.6f);

        string[] stages = {
            "Downloading components\u2026",
            "Installing AI Virtual Friend\u2026",
            "Configuring your assistant\u2026",
            "Almost done\u2026"
        };

        while (_progress < 1f)
        {
            // fills itself, in uneven little jumps like a real installer
            _progress = Mathf.Min(1f, _progress + Random.Range(0.02f, 0.09f));
            _status.text = stages[Mathf.Min(stages.Length - 1, Mathf.FloorToInt(_progress * stages.Length))];

            float trackW = track.rect.width;
            fill.sizeDelta = new Vector2(trackW * _progress, 0);

            yield return new WaitForSeconds(Random.Range(0.08f, 0.22f));
        }

        _status.text = "Installation complete.";
        yield return new WaitForSeconds(1.0f);

        _onComplete?.Invoke();
        if (_manager != null && _window != null) _manager.CloseWindow(_window);
    }

    // ---- helpers ----
    private static RectTransform NewRect(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    private static TextMeshProUGUI MakeText(RectTransform parent, string text, float size, FontStyles style, float h)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = Color.black;
        t.textWrappingMode = TextWrappingModes.Normal;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
        return t;
    }
}
