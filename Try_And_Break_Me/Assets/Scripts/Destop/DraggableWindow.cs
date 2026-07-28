using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// A draggable, closable, focusable window whose chrome is built ENTIRELY IN CODE so you never
// hand-assemble window prefabs in the Inspector. Call DraggableWindow.Create(...) and you get a
// title bar (draggable), a close button, and an empty Content area to put anything in.
//
// Your chat UI, the creator screen, etc. all become "content" placed inside ContentArea.
public class DraggableWindow : MonoBehaviour, IPointerDownHandler
{
    public RectTransform RectTransform { get; private set; }
    public RectTransform ContentArea { get; private set; }
    public string Title { get; private set; }
    public Sprite Icon { get; private set; }

    private WindowManager _manager;
    private Image _titleBarImage;
    private static readonly Color FocusedBar   = new Color(0.16f, 0.20f, 0.34f, 1f);
    private static readonly Color UnfocusedBar = new Color(0.28f, 0.30f, 0.36f, 1f);

    public static DraggableWindow Create(RectTransform parent, string title, Vector2 size, WindowManager manager, Sprite icon = null)
    {
        // Root
        var go = new GameObject($"Window_{title}", typeof(RectTransform), typeof(Image), typeof(DraggableWindow));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);   // centre-anchored, moved by drag
        rt.pivot = new Vector2(0.5f, 0.5f);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 1f);           // window body

        var win = go.GetComponent<DraggableWindow>();
        win.RectTransform = rt;
        win.Title = title;
        win.Icon = icon;
        win._manager = manager;

        // Title bar (top strip)
        const float barH = 28f;
        var barGo = new GameObject("TitleBar", typeof(RectTransform), typeof(Image), typeof(WindowDragHandle));
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.SetParent(rt, false);
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.sizeDelta = new Vector2(0f, barH);
        barRt.anchoredPosition = Vector2.zero;
        win._titleBarImage = barGo.GetComponent<Image>();
        win._titleBarImage.color = FocusedBar;
        barGo.GetComponent<WindowDragHandle>().Init(win);

        // Optional title-bar icon (left side)
        float titleLeftPad = 10f;
        if (icon != null)
        {
            var iconGo = new GameObject("TitleIcon", typeof(RectTransform), typeof(Image));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(barRt, false);
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(20f, 20f);
            iconRt.anchoredPosition = new Vector2(6f, 0f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            titleLeftPad = 30f;   // push the title text right of the icon
        }

        // Title text
        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.SetParent(barRt, false);
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(titleLeftPad, 0f);
        titleRt.offsetMax = new Vector2(-30f, 0f);
        var titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 14f;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = Color.white;

        // Close button
        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.SetParent(barRt, false);
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(24f, 24f);
        closeRt.anchoredPosition = new Vector2(-2f, 0f);
        closeGo.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 1f);
        var closeLabelGo = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        var closeLabelRt = closeLabelGo.GetComponent<RectTransform>();
        closeLabelRt.SetParent(closeRt, false);
        closeLabelRt.anchorMin = Vector2.zero; closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero; closeLabelRt.offsetMax = Vector2.zero;
        var closeLabel = closeLabelGo.GetComponent<TextMeshProUGUI>();
        closeLabel.text = "X"; closeLabel.fontSize = 14f; closeLabel.color = Color.white;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeGo.GetComponent<Button>().onClick.AddListener(() => manager.CloseWindow(win));

        // Content area (everything below the title bar)
        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(RectMask2D));
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(rt, false);
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.offsetMin = new Vector2(0f, 0f);
        contentRt.offsetMax = new Vector2(0f, -barH);   // leave room for the title bar
        win.ContentArea = contentRt;

        // Resize grip (bottom-right corner). Sits above content so it's grabbable.
        var gripGo = new GameObject("ResizeGrip", typeof(RectTransform), typeof(Image), typeof(WindowResizeHandle));
        var gripRt = gripGo.GetComponent<RectTransform>();
        gripRt.SetParent(rt, false);
        gripRt.anchorMin = new Vector2(1f, 0f);
        gripRt.anchorMax = new Vector2(1f, 0f);
        gripRt.pivot = new Vector2(1f, 0f);
        gripRt.sizeDelta = new Vector2(18f, 18f);
        gripRt.anchoredPosition = Vector2.zero;
        gripGo.GetComponent<Image>().color = new Color(0.4f, 0.42f, 0.5f, 0.8f);
        gripGo.GetComponent<WindowResizeHandle>().Init(win);

        return win;
    }

    // Clicking anywhere on the window focuses it.
    public void OnPointerDown(PointerEventData eventData) => _manager?.Focus(this);

    // Adds a small "dock" button to the title bar (used by bot windows so the player can snap
    // a dragged-away window back to the right edge). Call after Create.
    public void AddDockButton()
    {
        var barRt = _titleBarImage != null ? _titleBarImage.rectTransform : null;
        if (barRt == null) return;

        var dockGo = new GameObject("Dock", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        var dockRt = dockGo.GetComponent<RectTransform>();
        dockRt.SetParent(barRt, false);
        dockRt.anchorMin = new Vector2(1f, 0.5f);
        dockRt.anchorMax = new Vector2(1f, 0.5f);
        dockRt.pivot = new Vector2(1f, 0.5f);
        dockRt.sizeDelta = new Vector2(24f, 24f);
        dockRt.anchoredPosition = new Vector2(-30f, 0f);   // sits left of the close button
        dockGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.5f, 0.4f, 1f);
        dockGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => BotDock.Redock(this));

        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.SetParent(dockRt, false);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        var lbl = lblGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "\u25E8"; lbl.fontSize = 13f; lbl.color = Color.white;   // a little dock glyph
        lbl.alignment = TextAlignmentOptions.Center;
    }

    public void SetFocused(bool focused)
    {
        if (_titleBarImage != null)
            _titleBarImage.color = focused ? FocusedBar : UnfocusedBar;
    }
}
