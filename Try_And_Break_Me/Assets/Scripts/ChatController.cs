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

    private readonly List<ChatMessage> _history = new List<ChatMessage>();
    private string _systemPrompt;

    // UI refs the controller builds and keeps.
    private TMP_Text _transcript;
    private TMP_InputField _input;
    private Button _sendButton;
    private ScrollRect _scroll;
    private TMP_Text _debug;

    public ChatController(CharacterSheet sheet, EmotionProfile emotion, SanityModel sanity,
                          IDialogueProvider provider, IDirectorProvider director)
    {
        _sheet = sheet;
        _emotion = emotion;
        _sanity = sanity;
        _provider = provider;
        _director = director;
        _systemPrompt = PromptAssembler.Assemble(sheet, emotion);
    }

    // Build the chat UI inside the given window content area.
    public void Build(RectTransform content)
    {
        // Layout: [ transcript scroll (flex) ] [ debug (fixed) ] [ input row (fixed) ]
        var root = NewRect(content, "ChatRoot");
        Stretch(root);
        var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        BuildTranscript(root);   // flexible height
        BuildDebug(root);        // small fixed
        BuildInputRow(root);     // fixed

        RefreshDebug(null, default);
    }

    // ---- transcript (the tricky scroll view, built correctly in code) --------
    private void BuildTranscript(RectTransform parent)
    {
        // ScrollRect root
        var scrollGo = new GameObject("Transcript", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);
        var le = scrollGo.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;   // takes remaining space
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
        _transcript.fontSize = 14f;
        _transcript.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        _transcript.textWrappingMode = TextWrappingModes.Normal;
        _transcript.overflowMode = TextOverflowModes.Overflow;
    }

    private void BuildDebug(RectTransform parent)
    {
        var go = new GameObject("Debug", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 70f; le.preferredHeight = 70f;
        _debug = go.GetComponent<TextMeshProUGUI>();
        _debug.fontSize = 11f;
        _debug.color = new Color(0.6f, 0.85f, 0.6f, 1f);
        _debug.alignment = TextAlignmentOptions.TopLeft;
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
        inputGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.2f, 1f);
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
        placeholder.text = "Say something..."; placeholder.fontSize = 13f;
        placeholder.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        placeholder.alignment = TextAlignmentOptions.Left;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.GetComponent<RectTransform>().SetParent(textAreaRt, false);
        Stretch(textGo.GetComponent<RectTransform>());
        var inputText = textGo.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 13f; inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Left;

        _input.textViewport = textAreaRt;
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.onSubmit.AddListener(_ => OnSend());

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
        btnLabel.text = "Send"; btnLabel.fontSize = 13f; btnLabel.color = Color.white;
        btnLabel.alignment = TextAlignmentOptions.Center;
    }

    // ---- the Phase 3 loop ----------------------------------------------------
    private async void OnSend()
    {
        string userText = _input.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        _input.text = "";
        SetBusy(true);
        _history.Add(new ChatMessage("user", userText));
        Append($"<b>You:</b> {userText}");

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
            Append($"<b>{_sheet.Name}:</b> {result.Reply}");

            RefreshDebug(score, turn);
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
