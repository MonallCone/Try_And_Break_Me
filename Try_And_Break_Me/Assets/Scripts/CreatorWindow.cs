using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Builds the character-creator UI IN CODE inside a spawned window's ContentArea.
// Player picks a template (your JSON bots) and sets the eight sliders, then clicks Create.
// On Create it fires OnCreate(sheet, emotion) — the launcher listens and opens a chat window.
//
// Nothing here is wired in the Inspector; it's all constructed at runtime, so it drops cleanly
// into a DraggableWindow.ContentArea.
public class CreatorWindow
{
    // The eight axes, in order, with their opposed-pair labels (left = 1, right = 10).
    private static readonly (string field, string left, string right)[] Axes =
    {
        ("mood",          "Sad",        "Happy"),
        ("boldness",      "Shy",        "Bold"),
        ("friendliness",  "Cold",       "Friendly"),
        ("anger",         "Calm",       "Angry"),
        ("trust",         "Suspicious", "Trusting"),
        ("playfulness",   "Serious",    "Playful"),
        ("talkativeness", "Quiet",      "Talkative"),
        ("confidence",    "Insecure",   "Confident"),
    };

    // The template ids the dropdown offers. Add JSON files under Resources/Characters and list them here.
    private static readonly string[] TemplateIds = { "bartleby", "vesper" };

    private readonly Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();
    private TMP_Dropdown _dropdown;

    public event Action<CharacterSheet, EmotionProfile> OnCreate;

    // Build the whole creator UI inside the given content area.
    public void Build(RectTransform content)
    {
        // A vertical scroll in case it's tall — but keep it simple: a vertical layout that fills.
        var root = MakeChild(content, "CreatorRoot");
        Stretch(root);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        MakeLabel(root, "Create an AI Virtual Friend", 18, FontStyles.Bold, 26);

        // Template picker
        MakeLabel(root, "Choose a personality:", 13, FontStyles.Normal, 20);
        _dropdown = MakeDropdown(root, new List<string>(TemplateIds));

        // Sliders
        MakeLabel(root, "Set their temperament:", 13, FontStyles.Normal, 20);
        foreach (var axis in Axes)
            _sliders[axis.field] = MakeSliderRow(root, axis.left, axis.right);

        // Create button
        MakeButton(root, "Create", OnCreateClicked, 36);
    }

    private void OnCreateClicked()
    {
        string id = TemplateIds[Mathf.Clamp(_dropdown.value, 0, TemplateIds.Length - 1)];
        CharacterSheet sheet = CharacterLoader.Load(id);
        if (sheet == null) { Debug.LogError($"[Creator] template '{id}' failed to load"); return; }

        var emotion = new EmotionProfile
        {
            Mood          = (int)_sliders["mood"].value,
            Boldness      = (int)_sliders["boldness"].value,
            Friendliness  = (int)_sliders["friendliness"].value,
            Anger         = (int)_sliders["anger"].value,
            Trust         = (int)_sliders["trust"].value,
            Playfulness   = (int)_sliders["playfulness"].value,
            Talkativeness = (int)_sliders["talkativeness"].value,
            Confidence    = (int)_sliders["confidence"].value,
        };

        Debug.Log($"[Creator] built {sheet.Name}: mood={emotion.Mood} bold={emotion.Boldness} " +
                  $"friend={emotion.Friendliness} anger={emotion.Anger} trust={emotion.Trust} " +
                  $"play={emotion.Playfulness} talk={emotion.Talkativeness} conf={emotion.Confidence}");

        OnCreate?.Invoke(sheet, emotion);
    }

    // ---- small UI builders ---------------------------------------------------

    private static RectTransform MakeChild(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void AddLayoutHeight(GameObject go, float h)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string text, float size, FontStyles style, float height)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        t.alignment = TextAlignmentOptions.Left;
        AddLayoutHeight(go, height);
        return t;
    }

    private static TMP_Dropdown MakeDropdown(RectTransform parent, List<string> options)
    {
        // Build a minimal TMP_Dropdown in code.
        var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);
        AddLayoutHeight(go, 30f);
        var dd = go.GetComponent<TMP_Dropdown>();

        // Label showing the current value
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10, 0); labelRt.offsetMax = new Vector2(-25, 0);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.color = Color.white; label.fontSize = 13f; label.alignment = TextAlignmentOptions.Left;
        dd.captionText = label;

        // Template (item) — required by TMP_Dropdown, built minimally
        var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Canvas), typeof(GraphicRaycaster));
        var templateRt = templateGo.GetComponent<RectTransform>();
        templateRt.SetParent(go.transform, false);
        templateRt.anchorMin = new Vector2(0, 0); templateRt.anchorMax = new Vector2(1, 0);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = new Vector2(0, 2);
        templateRt.sizeDelta = new Vector2(0, 120);
        templateGo.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);
        templateGo.SetActive(false);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(templateRt, false);
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        var itemRt = itemGo.GetComponent<RectTransform>();
        itemRt.SetParent(viewportRt, false);
        itemRt.anchorMin = new Vector2(0, 0.5f); itemRt.anchorMax = new Vector2(1, 0.5f);
        itemRt.sizeDelta = new Vector2(0, 24);

        var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var itemLabelRt = itemLabelGo.GetComponent<RectTransform>();
        itemLabelRt.SetParent(itemRt, false);
        itemLabelRt.anchorMin = Vector2.zero; itemLabelRt.anchorMax = Vector2.one;
        itemLabelRt.offsetMin = new Vector2(10, 0); itemLabelRt.offsetMax = new Vector2(-10, 0);
        var itemLabel = itemLabelGo.GetComponent<TextMeshProUGUI>();
        itemLabel.color = Color.white; itemLabel.fontSize = 13f;
        itemLabel.alignment = TextAlignmentOptions.Left;

        dd.template = templateRt;
        dd.itemText = itemLabel;
        dd.options = new List<TMP_Dropdown.OptionData>();
        foreach (var o in options) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.value = 0; dd.RefreshShownValue();

        return dd;
    }

    private static Slider MakeSliderRow(RectTransform parent, string leftLabel, string rightLabel)
    {
        // Row container
        var rowGo = new GameObject($"Row_{rightLabel}", typeof(RectTransform));
        var rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.SetParent(parent, false);
        AddLayoutHeight(rowGo, 30f);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        // Left label
        var l = MakeInlineLabel(rowRt, leftLabel, TextAlignmentOptions.Right, 70);
        // Slider
        var slider = MakeSlider(rowRt);
        // Right label
        var r = MakeInlineLabel(rowRt, rightLabel, TextAlignmentOptions.Left, 70);

        return slider;
    }

    private static TextMeshProUGUI MakeInlineLabel(RectTransform parent, string text, TextAlignmentOptions align, float width)
    {
        var go = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 12f; t.color = new Color(0.8f, 0.8f, 0.85f, 1f);
        t.alignment = align;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width; le.minWidth = width;
        return t;
    }

    private static Slider MakeSlider(RectTransform parent)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        var slider = go.GetComponent<Slider>();
        slider.minValue = 1; slider.maxValue = 10; slider.wholeNumbers = true; slider.value = 5;

        // Background
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(go.transform, false);
        bgRt.anchorMin = new Vector2(0, 0.25f); bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

        // Fill area + fill
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.SetParent(go.transform, false);
        fillAreaRt.anchorMin = new Vector2(0, 0.25f); fillAreaRt.anchorMax = new Vector2(1, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5, 0); fillAreaRt.offsetMax = new Vector2(-5, 0);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.SetParent(fillAreaRt, false);
        fillRt.sizeDelta = new Vector2(10, 0);
        fill.GetComponent<Image>().color = new Color(0.45f, 0.55f, 0.85f, 1f);

        // Handle area + handle
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.SetParent(go.transform, false);
        handleAreaRt.anchorMin = Vector2.zero; handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(5, 0); handleAreaRt.offsetMax = new Vector2(-5, 0);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.SetParent(handleAreaRt, false);
        handleRt.sizeDelta = new Vector2(16, 16);
        handle.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.9f, 1f);

        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private static void MakeButton(RectTransform parent, string text, Action onClick, float height)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.3f, 1f);
        AddLayoutHeight(go, height);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = 15f; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }
}
