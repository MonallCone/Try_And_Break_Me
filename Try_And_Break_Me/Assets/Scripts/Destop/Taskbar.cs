using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bottom taskbar with a live clock and one button per open window (click to focus it).
// Buttons are created in code as windows open. Keeps the fake-OS feel and helps the player
// manage multiple bot windows in the hive.
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
        go.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.30f, 1f);
        go.GetComponent<LayoutElement>().preferredWidth = 140f;
        go.GetComponent<LayoutElement>().preferredHeight = 30f;
        go.GetComponent<Button>().onClick.AddListener(() => _manager?.Focus(win));

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8f, 0f); labelRt.offsetMax = new Vector2(-8f, 0f);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = win.Title;
        label.fontSize = 12f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.overflowMode = TextOverflowModes.Ellipsis;

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
            img.color = (kv.Key == win)
                ? new Color(0.30f, 0.34f, 0.45f, 1f)   // focused: lighter
                : new Color(0.22f, 0.24f, 0.30f, 1f);
        }
    }
}
