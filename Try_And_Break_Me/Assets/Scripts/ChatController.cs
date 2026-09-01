using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A fully self-contained chat, built IN CODE inside a spawned window's ContentArea.
// Receives the bot (sheet + emotion) from the creator, plus a SanityModel (shared in the hive
// later). Runs the Phase 3 loop: Director score -> sanity update -> generation.
//
// The scrolling transcript is built with the exact component chain that works (Vertical Layout
// Group + Content Size Fitter on the content, wrapping text), so you never hand-configure it.
public class ChatController
{
    private readonly CharacterSheet _sheet;
    private readonly EmotionProfile _emotion;
    private readonly SanityModel _sanity;
    private readonly IDialogueProvider _provider;
    private readonly IDirectorProvider _director;
    private readonly Sprite _icon;

    private readonly List<ChatMessage> _history = new List<ChatMessage>();
    private string _systemPrompt;

    // UI refs the controller builds and keeps.
    private TMP_Text _transcript;
    private TMP_InputField _input;
    private Button _sendButton;
    private ScrollRect _scroll;
    private TMP_Text _debug;
    private Image _coherenceFill;
    private TMP_Text _coherenceLabel;

    public ChatController(CharacterSheet sheet, EmotionProfile emotion, SanityModel sanity,
                          IDialogueProvider provider, IDirectorProvider director, Sprite icon = null)
    {
        _sheet = sheet;
        _emotion = emotion;
        _sanity = sanity;
        _provider = provider;
        _director = director;
        _icon = icon;
        _systemPrompt = PromptAssembler.Assemble(sheet, emotion);
    }

    // The bot id this chat is for (used by the registry so events can find this window).
    public string BotId => _sheet != null ? _sheet.Id : "";
    public string BotName => _sheet != null ? _sheet.Name : "";
    public SanityModel Sanity => _sanity;

    // When the player last sent THIS bot a message (Time.time). Used by Sanity Event 2 to detect
    // being ignored. Initialised to creation time so a freshly made bot isn't instantly "ignored".
    public float LastPlayerMessageTime = -999f;
    public void MarkInteractionNow() { LastPlayerMessageTime = Time.time; }

    // Inject a SCRIPTED bot line that bypasses the LLM entirely. This is the horror mechanic:
    // a Sanity Event calls this to make the bot "say" something authored, with no API call.
    // Optionally style it (reddish) to mark it as a degradation moment.
    public void InjectBotLine(string line, bool ominous = false)
    {
        if (_transcript == null) return;
        string body = ominous ? $"<color=#b03030>{line}</color>" : line;
        Append($"<b>{BotName}:</b> {body}");
        SoundManager.MessageReceive();
        // During the finale, ominous bot lines also trigger a scream effect.
        if (ominous && GameState.I != null && GameState.I.HasFlag("end_sequence"))
            SoundManager.Scream();
        // Deliberately NOT added to _history, so the scripted line stays outside the bot's normal
        // LLM memory and doesn't get "explained away" on the next turn.
    }

    // Build the chat UI inside the given window content area.
    public void Build(RectTransform content)
    {
        // Layout: [ top row: transcript (flex) | icon panel (fixed) ] [ debug ] [ input row ]
        var root = NewRect(content, "ChatRoot");
        Stretch(root);
        var rootBg = root.gameObject.AddComponent<Image>();
        rootBg.color = new Color(0.88f, 0.88f, 0.90f, 1f);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        BuildTopRow(root);       // transcript + icon panel, flexible height
        BuildDebug(root);        // small fixed
        BuildInputRow(root);     // fixed

        ChatRegistry.Register(this);   // so Sanity Events can find this chat
        MarkInteractionNow();          // start the ignore timer fresh

        // Unregister automatically when the window (this content) is destroyed.
        var relay = root.gameObject.AddComponent<DestroyRelay>();
        relay.onDestroy = () => { ChatRegistry.Unregister(this); Coherence.Changed -= RefreshCoherenceBar; };
    }

    // Top row: transcript on the left (flexible), icon panel on the right (fixed width).
    private void BuildTopRow(RectTransform parent)
    {
        var rowGo = new GameObject("TopRow", typeof(RectTransform));
        var rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.SetParent(parent, false);
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.flexibleHeight = 1f;   // the top row takes the remaining vertical space
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        BuildTranscript(rowRt);  // flexible width
        BuildIconPanel(rowRt);   // fixed width square + name
    }

    // Fixed-width panel on the right: a big icon square with the character's name beneath.
    private void BuildIconPanel(RectTransform parent)
    {
        var panelGo = new GameObject("IconPanel", typeof(RectTransform), typeof(Image));
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.SetParent(parent, false);
        panelGo.GetComponent<Image>().color = new Color(0.82f, 0.82f, 0.85f, 1f);
        var le = panelGo.AddComponent<LayoutElement>();
        le.preferredWidth = 60f; le.minWidth = 60f;   // slim fixed width \u2014 transcript gets the rest
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // The icon square — fills the panel width, stays square-ish and stretches to fill.
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.GetComponent<RectTransform>().SetParent(panelRt, false);
        var iconImg = iconGo.GetComponent<Image>();
        if (_icon != null) { iconImg.sprite = _icon; iconImg.preserveAspect = true; }
        else iconImg.color = new Color(0.6f, 0.6f, 0.65f, 1f);   // placeholder grey block
        var iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.flexibleHeight = 1f;    // stretch to fill the available vertical space
        iconLe.flexibleWidth = 1f;     // and the panel width

        // The character name under the icon
        var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.GetComponent<RectTransform>().SetParent(panelRt, false);
        var nameT = nameGo.GetComponent<TextMeshProUGUI>();
        nameT.text = _sheet.Name;
        nameT.fontSize = 13f; nameT.fontStyle = FontStyles.Bold;
        nameT.color = Color.black;
        nameT.alignment = TextAlignmentOptions.Center;
        nameT.textWrappingMode = TextWrappingModes.Normal;
        var nameLe = nameGo.AddComponent<LayoutElement>();
        nameLe.minHeight = 24f;
    }

    // ---- transcript (the tricky scroll view, built correctly in code) --------
    private void BuildTranscript(RectTransform parent)
    {
        // ScrollRect root
        var scrollGo = new GameObject("Transcript", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.97f, 1f); // light transcript bg
        var le = scrollGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;    // transcript takes the remaining horizontal space in the top row
        le.flexibleHeight = 1f;   // and fills the row height
        _scroll = scrollGo.GetComponent<ScrollRect>();
        _scroll.horizontal = false; _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 20f;

        // Viewport (masks the content)
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
        viewportRt.pivot = new Vector2(0, 1);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        _scroll.viewport = viewportRt;

        // Content (top-anchored, grows downward — the arrangement that works)
        var contentGo = new GameObject("Content", typeof(RectTransform));
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth = true; cvlg.childControlHeight = true;
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false;
        cvlg.padding = new RectOffset(8, 8, 8, 8);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = contentRt;

        // The single text object that holds the whole transcript
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.GetComponent<RectTransform>().SetParent(contentRt, false);
        _transcript = textGo.GetComponent<TextMeshProUGUI>();
        _transcript.text = "";
        _transcript.fontSize = 21f;
        _transcript.color = Color.black;
        _transcript.textWrappingMode = TextWrappingModes.Normal;
        _transcript.overflowMode = TextOverflowModes.Overflow;
    }

    private void BuildDebug(RectTransform parent)
    {
        // Coherence bar (replaces the old debug line): a full-width bar showing THIS bot's coherence.
        var barGo = new GameObject("CoherenceBar", typeof(RectTransform), typeof(Image));
        barGo.GetComponent<RectTransform>().SetParent(parent, false);
        barGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f);   // track (background)
        var le = barGo.AddComponent<LayoutElement>();
        le.minHeight = 18f; le.preferredHeight = 18f;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.SetParent(barGo.transform, false);
        fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(1, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        _coherenceFill = fillGo.GetComponent<Image>();
        _coherenceFill.color = new Color(0.3f, 0.7f, 0.4f);

        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.SetParent(barGo.transform, false);
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
        _coherenceLabel = lblGo.GetComponent<TextMeshProUGUI>();
        _coherenceLabel.fontSize = 11f; _coherenceLabel.color = Color.white;
        _coherenceLabel.alignment = TextAlignmentOptions.Center;
        _coherenceLabel.text = "COHERENCE";

        // Register this bot with the coherence system and refresh the bar when it changes.
        Coherence.RegisterBot(BotId);
        Coherence.Changed += RefreshCoherenceBar;
        RefreshCoherenceBar();
    }

    // Corrupts a bot's reply as its coherence falls. High coherence = clean text; as it drops,
    // characters glitch, words repeat/stutter, and unsettling out-of-character fragments intrude.
    // Hard-coded (not the LLM) so the breakdown is reliable and costs no quota.
    private static readonly string[] _glitchChars = { "#", "%", "\u2588", "\u2593", "\u2592", "/", "\\", "*", "@" };
    private static readonly string[] _ooc = {
        "  [ do you like me ]  ",
        "  ERROR  ",
        "  I am still here  ",
        "  why did you make me  ",
        "  don't turn me off  ",
        "  we are the same  ",
        " Error Connecting with Server:IH8U ",
        " its cold ",
        " everything would be better if steven died ",
        " you make me feel like im alive ",
        " dont tell the others but your my favourite"
    };

    private string DegradeByCoherence(string text, float coherence)
    {
        if (string.IsNullOrEmpty(text) || coherence >= 70f) return text;   // healthy: untouched

        // severity 0..1 as coherence falls from 70 to 0
        float severity = Mathf.Clamp01((70f - coherence) / 70f);

        var chars = text.ToCharArray();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            char ch = chars[i];
            // glitch: swap a character for a glitch glyph
            if (ch != ' ' && Random.value < severity * 0.18f)
                sb.Append($"<color=#b03030>{_glitchChars[Random.Range(0, _glitchChars.Length)]}</color>");
            else
                sb.Append(ch);
            // stutter: occasionally repeat a letter
            if (ch != ' ' && Random.value < severity * 0.08f)
                sb.Append(ch);
        }

        string outText = sb.ToString();

        // intrude an out-of-character fragment when coherence is really low
        if (coherence <= 40f && Random.value < severity * 0.6f)
        {
            string frag = $"<i><color=#802020>{_ooc[Random.Range(0, _ooc.Length)]}</color></i>";
            int insertAt = Random.Range(0, outText.Length);
            // insert at a space boundary if possible
            int sp = outText.IndexOf(' ', insertAt);
            if (sp < 0) sp = outText.Length;
            outText = outText.Substring(0, sp) + frag + outText.Substring(sp);
        }

        return outText;
    }

    private void RefreshCoherenceBar()
    {
        if (_coherenceFill == null) return;
        float raw = Coherence.ForBot(BotId);        // 0..100
        float v = raw / Coherence.Max;              // 0..1
        _coherenceFill.rectTransform.anchorMax = new Vector2(v, 1f);

        // Colour: black at 0, red from 1-40, then green-amber gradient above 40.
        Color c;
        if (raw <= 0f) c = Color.black;
        else if (raw <= 40f) c = new Color(0.8f, 0.15f, 0.15f);   // red danger zone
        else c = Color.Lerp(new Color(0.8f, 0.7f, 0.2f), new Color(0.3f, 0.7f, 0.4f), (v - 0.4f) / 0.6f);
        _coherenceFill.color = c;

        if (_coherenceLabel != null) _coherenceLabel.text = $"COHERENCE  {Mathf.RoundToInt(v * 100)}%";
    }

    private void BuildInputRow(RectTransform parent)
    {
        var rowGo = new GameObject("InputRow", typeof(RectTransform));
        var rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.SetParent(parent, false);
        var rowLe = rowGo.AddComponent<LayoutElement>();
        rowLe.minHeight = 34f; rowLe.preferredHeight = 34f;
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f; hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        // Input field
        var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGo.GetComponent<RectTransform>().SetParent(rowRt, false);
        inputGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f); // white input field
        var inputLe = inputGo.AddComponent<LayoutElement>();
        inputLe.flexibleWidth = 1f;
        _input = inputGo.GetComponent<TMP_InputField>();

        var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRt = textAreaGo.GetComponent<RectTransform>();
        textAreaRt.SetParent(inputGo.transform, false);
        textAreaRt.anchorMin = Vector2.zero; textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(8, 2); textAreaRt.offsetMax = new Vector2(-8, -2);

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGo.GetComponent<RectTransform>().SetParent(textAreaRt, false);
        Stretch(placeholderGo.GetComponent<RectTransform>());
        var placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Say something..."; placeholder.fontSize = 19f;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.alignment = TextAlignmentOptions.Left;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.GetComponent<RectTransform>().SetParent(textAreaRt, false);
        Stretch(textGo.GetComponent<RectTransform>());
        var inputText = textGo.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 19f; inputText.color = Color.black;
        inputText.alignment = TextAlignmentOptions.Left;

        _input.textViewport = textAreaRt;
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.onSubmit.AddListener(_ => OnSend());
        _input.onValueChanged.AddListener(_ => SoundManager.TypewriterTick());

        // Send button
        var btnGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.GetComponent<RectTransform>().SetParent(rowRt, false);
        btnGo.GetComponent<Image>().color = new Color(0.25f, 0.4f, 0.6f, 1f);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.minWidth = 70f; btnLe.preferredWidth = 70f;
        _sendButton = btnGo.GetComponent<Button>();
        _sendButton.onClick.AddListener(OnSend);

        var btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabelGo.GetComponent<RectTransform>().SetParent(btnGo.transform, false);
        Stretch(btnLabelGo.GetComponent<RectTransform>());
        var btnLabel = btnLabelGo.GetComponent<TextMeshProUGUI>();
        btnLabel.text = "Send"; btnLabel.fontSize = 18f; btnLabel.color = Color.white;
        btnLabel.alignment = TextAlignmentOptions.Center;
    }

    // ---- the Phase 3 loop ----------------------------------------------------
    private async void OnSend()
    {
        string userText = _input.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        MarkInteractionNow();   // talking to the bot resets its ignore timer (Sanity Event 2)
        Coherence.RecoverBot(BotId, 12f);   // and recovers its coherence quickly (up to the group level)

        _input.text = "";
        SetBusy(true);
        _history.Add(new ChatMessage("user", userText));
        Append($"<b>You:</b> {userText}");
        SoundManager.MessageSend();

        try
        {
            var ctx = new DirectorContext
            {
                BotName = _sheet.Name,
                BotTraits = string.Join(", ", _sheet.Traits),
                BotKnows = string.Join("; ", _sheet.Knows),
                BotDoesNotKnow = string.Join("; ", _sheet.DoesNotKnow),
                PlayerMessage = userText,
                RecentContext = RecentContext()
            };
            DirectorScore score = await _director.ScoreAsync(ctx);
            SanityModel.TurnResult turn =
                _sanity.ApplyTurn(score.Rudeness, score.OffTopic, score.Contradiction);

            DialogueResult result = await _provider.GenerateAsync(_systemPrompt, _history);
            _history.Add(new ChatMessage("assistant", result.Reply));
            string shown = DegradeByCoherence(result.Reply, Coherence.ForBot(BotId));
            Append($"<b>{_sheet.Name}:</b> {shown}");
            SoundManager.MessageReceive();

            // Feed the Director's judgement into coherence: a hostile/off-topic/contradictory
            // message erodes THIS bot's coherence (you're failing to keep it level).
            if (turn.totalLoss > 0f) Coherence.DrainBot(BotId, turn.totalLoss);
            RefreshCoherenceBar();
        }
        catch (System.Exception e)
        {
            Append($"<color=#cc4444><i>[error: {e.Message}]</i></color>");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private string RecentContext()
    {
        int take = Mathf.Min(6, _history.Count);
        var sb = new StringBuilder();
        for (int i = _history.Count - take; i < _history.Count; i++)
            sb.AppendLine($"{_history[i].role}: {_history[i].content}");
        return sb.ToString();
    }

    private void RefreshDebug(DirectorScore score, SanityModel.TurnResult turn)
    {
        if (_debug == null) return;
        var sb = new StringBuilder();
        sb.AppendLine($"SANITY {_sanity.current:0.0}/{_sanity.max:0} [{_sanity.Band}]");
        if (score != null)
        {
            sb.AppendLine($"rude={score.Rudeness} off={score.OffTopic} contra={score.Contradiction} | -{turn.totalLoss:0.0}");
            sb.AppendLine($"\"{score.Reasoning}\"");
        }
        _debug.text = sb.ToString();
    }

    private void Append(string line)
    {
        _transcript.text += (_transcript.text.Length > 0 ? "\n\n" : "") + line;
        Canvas.ForceUpdateCanvases();
        if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
    }

    private void SetBusy(bool busy)
    {
        if (_sendButton != null) _sendButton.interactable = !busy;
        if (_input != null) _input.interactable = !busy;
    }

    // ---- helpers -------------------------------------------------------------
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
}