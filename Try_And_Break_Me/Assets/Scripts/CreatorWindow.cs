using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Two-page character creator, built in code inside a spawned window's ContentArea.
//   Page 1: pick a template (dropdown) + pick an icon (palette), and READ the character's
//           backstory, traits, knows / does-not-know. [Next]
//   Page 2: set the eight emotional sliders. [Back] [Create]
//
// Icons come from AppLauncher (Inspector-assigned sprites) and are passed in via SetIcons().
// On Create it fires OnCreate(sheet, emotion, icon).
public class CreatorWindow
{
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

    private static readonly string[] TemplateIds = { "lauren", "stuart", "alex" };

    // Display info for each role button.
    private static readonly Dictionary<string, string> RoleTitles = new Dictionary<string, string>
    {
        { "lauren", "Lauren \u2014 HR" },
        { "stuart", "Stuart \u2014 Cyber-Security" },
        { "alex",   "Alex \u2014 Customer Support" },
    };

    private readonly Dictionary<string, Slider> _sliders = new Dictionary<string, Slider>();
    private TMP_Text _infoText;

    // Per-bot icons, supplied by AppLauncher (id -> sprite). The selected role's icon travels
    // with the created bot; there is no separate icon picker.
    private Dictionary<string, Sprite> _iconById = new Dictionary<string, Sprite>();
    private string _selectedId = "lauren";
    private readonly List<(string id, Image bg)> _roleButtons = new List<(string, Image)>();

    private GameObject _page1;
    private GameObject _page2;

    public event Action<CharacterSheet, EmotionProfile, Sprite> OnCreate;

    // Called by AppLauncher before Build, to supply the per-bot icon sprites (by bot id).
    public void SetIconMap(Dictionary<string, Sprite> iconById)
    {
        _iconById = iconById ?? new Dictionary<string, Sprite>();
    }

    private Sprite IconFor(string id)
    {
        return (_iconById != null && _iconById.TryGetValue(id, out var s)) ? s : null;
    }

    public void Build(RectTransform content)
    {
        _page1 = BuildPage1(content).gameObject;
        _page2 = BuildPage2(content).gameObject;
        ShowPage(1);
        RefreshInfo();
    }

    private void ShowPage(int n)
    {
        _page1.SetActive(n == 1);
        _page2.SetActive(n == 2);
    }

    // ---- PAGE 1: template + icon + info -------------------------------------
    private RectTransform BuildPage1(RectTransform content)
    {
        var page = NewRect(content, "Page1");
        Stretch(page);
        // Light background so black text reads clearly.
        var bg = page.gameObject.AddComponent<Image>();
        bg.color = new Color(0.88f, 0.88f, 0.90f, 1f);
        var vlg = page.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 8f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        MakeLabel(page, "Train a new AI assistant", 22, FontStyles.Bold, 32);

        MakeLabel(page, "Which role should it learn?", 16, FontStyles.Normal, 24);
        BuildRoleButtons(page);

        MakeLabel(page, "About them:", 16, FontStyles.Normal, 24);
        _infoText = BuildScrollableInfo(page);   // read-only, scrollable

        MakeButton(page, "Next", () => ShowPage(2), 34,
                   new Color(0.25f, 0.4f, 0.6f));

        return page;
    }

    // Three role buttons (name + job title, with the bot's icon). Selecting one updates the info.
    private void BuildRoleButtons(RectTransform parent)
    {
        _roleButtons.Clear();
        foreach (var id in TemplateIds)
        {
            var rowGo = new GameObject($"Role_{id}", typeof(RectTransform), typeof(Image), typeof(Button));
            rowGo.GetComponent<RectTransform>().SetParent(parent, false);
            var bgImg = rowGo.GetComponent<Image>();
            bgImg.color = new Color(0.97f, 0.97f, 0.99f);
            AddLayoutHeight(rowGo, 48f);
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f; hlg.padding = new RectOffset(6, 6, 4, 4);
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // icon
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.GetComponent<RectTransform>().SetParent(rowGo.transform, false);
            var iconImg = iconGo.GetComponent<Image>();
            var sprite = IconFor(id);
            if (sprite != null) { iconImg.sprite = sprite; iconImg.preserveAspect = true; }
            else iconImg.color = new Color(0.7f, 0.7f, 0.75f);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 40; iconLe.minWidth = 40; iconLe.preferredHeight = 40;

            // label
            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.GetComponent<RectTransform>().SetParent(rowGo.transform, false);
            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.text = RoleTitles.TryGetValue(id, out var title) ? title : id;
            lbl.fontSize = 17f; lbl.color = Color.black; lbl.alignment = TextAlignmentOptions.Left;
            var lblLe = lblGo.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1f;

            string captured = id;
            bool alreadyMade = ChatRegistry.FindByBotId(id) != null;
            var button = rowGo.GetComponent<Button>();
            if (alreadyMade)
            {
                bgImg.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);   // greyed
                lbl.text += "  (created)";
                lbl.color = new Color(0.5f, 0.5f, 0.5f);
                button.interactable = false;
            }
            else
            {
                button.onClick.AddListener(() => SelectRole(captured));
            }
            _roleButtons.Add((id, bgImg));
        }
        // Select the first role that isn't already made.
        string firstFree = System.Array.Find(TemplateIds, t => ChatRegistry.FindByBotId(t) == null);
        if (firstFree != null) SelectRole(firstFree);
    }

    private void SelectRole(string id)
    {
        _selectedId = id;
        foreach (var (rid, bg) in _roleButtons)
            bg.color = (rid == id) ? new Color(0.8f, 0.88f, 1f) : new Color(0.97f, 0.97f, 0.99f);
        RefreshInfo();
    }

    private TMP_Text BuildScrollableInfo(RectTransform parent)
    {
        // A small scroll view so a long backstory doesn't overflow.
        var scrollGo = new GameObject("Info", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.GetComponent<RectTransform>().SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.97f, 1f);
        var le = scrollGo.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f; le.minHeight = 120f;
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(2, 0); viewportRt.offsetMax = new Vector2(-2, 0);
        viewportRt.pivot = new Vector2(0, 1);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        scroll.viewport = viewportRt;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;                       // pin (avoids left-drift clipping)
        contentRt.sizeDelta = new Vector2(0, contentRt.sizeDelta.y);
        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.padding = new RectOffset(12, 10, 8, 8);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRt;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.GetComponent<RectTransform>().SetParent(contentRt, false);
        var t = textGo.GetComponent<TextMeshProUGUI>();
        t.fontSize = 17f; t.color = Color.black;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private void RefreshInfo()
    {
        if (_infoText == null) return;
        string id = _selectedId;
        CharacterSheet s = CharacterLoader.Load(id);
        if (s == null) { _infoText.text = $"(could not load '{id}')"; return; }

        string knows = (s.Knows != null && s.Knows.Count > 0) ? string.Join("\n  \u2022 ", s.Knows) : "\u2014";
        string dunno = (s.DoesNotKnow != null && s.DoesNotKnow.Count > 0) ? string.Join("\n  \u2022 ", s.DoesNotKnow) : "\u2014";
        string traits = (s.Traits != null && s.Traits.Count > 0) ? string.Join(", ", s.Traits) : "\u2014";

        _infoText.text =
            $"<b>{s.Name}</b>\n\n" +
            $"{s.Backstory}\n\n" +
            $"<b>Traits:</b> {traits}\n\n" +
            $"<b>Knows:</b>\n  \u2022 {knows}\n\n" +
            $"<b>Does not know:</b>\n  \u2022 {dunno}";
    }

    // ---- PAGE 2: sliders ----------------------------------------------------
    private RectTransform BuildPage2(RectTransform content)
    {
        var page = NewRect(content, "Page2");
        Stretch(page);
        var bg = page.gameObject.AddComponent<Image>();
        bg.color = new Color(0.88f, 0.88f, 0.90f, 1f);
        var vlg = page.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 6f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        MakeLabel(page, "Calibrate for a human touch", 22, FontStyles.Bold, 32);

        foreach (var axis in Axes)
            _sliders[axis.field] = MakeSliderRow(page, axis.left, axis.right);

        // Back + Create row
        var rowGo = new GameObject("NavRow", typeof(RectTransform));
        rowGo.GetComponent<RectTransform>().SetParent(page, false);
        AddLayoutHeight(rowGo, 38f);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        MakeButtonIn(rowGo.GetComponent<RectTransform>(), "Back", () => ShowPage(1), new Color(0.4f, 0.4f, 0.45f));
        MakeButtonIn(rowGo.GetComponent<RectTransform>(), "Create", OnCreateClicked, new Color(0.25f, 0.45f, 0.3f));

        return page;
    }

    private void OnCreateClicked()
    {
        string id = _selectedId;
        if (ChatRegistry.FindByBotId(id) != null)
        {
            Debug.LogWarning($"[Creator] {id} already exists — not creating a duplicate.");
            return;
        }
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

        SoundManager.BotCreated();
        OnCreate?.Invoke(sheet, emotion, IconFor(_selectedId));
    }

    // ---- small UI builders --------------------------------------------------
    private static RectTransform NewRect(RectTransform parent, string name)
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
        le.preferredHeight = h; le.minHeight = h;
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string text, float size, FontStyles style, float height)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = Color.black;
        t.alignment = TextAlignmentOptions.Left;
        AddLayoutHeight(go, height);
        return t;
    }

    private static Slider MakeSliderRow(RectTransform parent, string leftLabel, string rightLabel)
    {
        var rowGo = new GameObject($"Row_{rightLabel}", typeof(RectTransform));
        rowGo.GetComponent<RectTransform>().SetParent(parent, false);
        AddLayoutHeight(rowGo, 32f);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        MakeInlineLabel(rowGo.GetComponent<RectTransform>(), leftLabel, TextAlignmentOptions.Right, 88);
        var slider = MakeSlider(rowGo.GetComponent<RectTransform>());
        MakeInlineLabel(rowGo.GetComponent<RectTransform>(), rightLabel, TextAlignmentOptions.Left, 88);
        return slider;
    }

    private static TextMeshProUGUI MakeInlineLabel(RectTransform parent, string text, TextAlignmentOptions align, float width)
    {
        var go = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 16f; t.color = Color.black;
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

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(go.transform, false);
        bgRt.anchorMin = new Vector2(0, 0.25f); bgRt.anchorMax = new Vector2(1, 0.75f);
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

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

    private static void MakeButton(RectTransform parent, string text, Action onClick, float height, Color color)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        AddLayoutHeight(go, height);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        AddButtonLabel(go, text);
    }

    private static void MakeButtonIn(RectTransform parent, string text, Action onClick, Color color)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        AddButtonLabel(go, text);
    }

    private static void AddButtonLabel(GameObject go, string text)
    {
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = 18f; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }
}