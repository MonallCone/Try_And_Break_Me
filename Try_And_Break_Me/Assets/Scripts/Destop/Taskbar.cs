using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Taskbar : MonoBehaviour
{
    [Tooltip("A horizontal strip (RectTransform) where window buttons are added.")]
    public RectTransform buttonContainer;

    [Tooltip("TMP text for the clock, e.g. bottom-right of the taskbar.")]
    public TMP_Text clockText;

    private WindowManager _manager;
    private readonly Dictionary<DraggableWindow, GameObject> _buttons = new Dictionary<DraggableWindow, GameObject>();

    public void Bind(WindowManager manager) => _manager = manager;

    private void Update()
    {
        if (clockText != null)
            clockText.text = System.DateTime.Now.ToString("HH:mm");
    }

    public void OnWindowOpened(DraggableWindow win)
    {
        if (buttonContainer == null) return;

        var go = new GameObject($"Task_{win.Title}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(buttonContainer, false);
        var buttonBg = go.GetComponent<Image>();
        go.GetComponent<LayoutElement>().preferredWidth = 140f;
        go.GetComponent<LayoutElement>().preferredHeight = 30f;
        go.GetComponent<Button>().onClick.AddListener(() => _manager?.Focus(win));

        // The icon (if any) is the button BACKGROUND, filling the button. The label sits on top.
        if (win.Icon != null)
        {
            buttonBg.sprite = win.Icon;
            buttonBg.type = Image.Type.Simple;
            buttonBg.preserveAspect = false;   // fill the whole button like a desktop taskbar tile
            buttonBg.color = Color.white;
        }
        else
        {
            buttonBg.color = new Color(0.22f, 0.24f, 0.30f, 1f);
        }

        // Label overlaid on top of the background, full-stretch, centred, with a readable shadow.
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(6f, 0f); labelRt.offsetMax = new Vector2(-6f, 0f);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = win.Title;
        label.fontSize = 12f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.fontStyle = FontStyles.Bold;
        // Slight dark outline so white text stays legible over any icon.
        label.enableVertexGradient = false;
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color(0f, 0f, 0f, 0.9f);

        _buttons[win] = go;
    }

    public void OnWindowClosed(DraggableWindow win)
    {
        if (_buttons.TryGetValue(win, out var go))
        {
            Destroy(go);
            _buttons.Remove(win);
        }
    }

    public void OnFocusChanged(DraggableWindow win)
    {
        foreach (var kv in _buttons)
        {
            var img = kv.Value.GetComponent<Image>();
            bool focused = (kv.Key == win);
            if (kv.Key.Icon != null)
            {
                // Icon background: full brightness when focused, dimmed when not.
                img.color = focused ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            }
            else
            {
                // Plain coloured button (no icon).
                img.color = focused
                    ? new Color(0.30f, 0.34f, 0.45f, 1f)
                    : new Color(0.22f, 0.24f, 0.30f, 1f);
            }
        }
    }
}
