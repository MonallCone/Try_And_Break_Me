using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Phase 3: each player turn now does Director-scoring -> sanity update -> generation.
// Sanity is TRACKED and shown live in a debug panel, but does NOT yet change how the bot
// talks (that's Phase 4). Debug panel is visible for building; it gets hidden later.
public class ChatWindow : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_InputField inputField;
    public Button sendButton;
    public TMP_Text transcriptText;
    public ScrollRect scrollRect;

    [Header("Debug panel (Phase 3 - hidden later)")]
    [Tooltip("A TMP_Text somewhere on screen to show sanity + scores live. Optional but recommended.")]
    public TMP_Text debugText;

    [Header("Which bot to load")]
    public string characterId = "bartleby";

    [Header("Emotion source")]
    public bool overrideEmotion = false;
    [Range(1, 10)] public int mood = 5;
    [Range(1, 10)] public int boldness = 5;
    [Range(1, 10)] public int friendliness = 5;
    [Range(1, 10)] public int anger = 5;
    [Range(1, 10)] public int trust = 5;
    [Range(1, 10)] public int playfulness = 5;
    [Range(1, 10)] public int talkativeness = 5;
    [Range(1, 10)] public int confidence = 5;

    [Header("Sanity (tune these live in Play mode)")]
    public SanityModel sanity = new SanityModel();

    private IDialogueProvider _provider;
    private IDirectorProvider _director;
    private readonly List<ChatMessage> _history = new List<ChatMessage>();
    private CharacterSheet _sheet;
    private string _systemPrompt;

    private void Awake()
    {
        _provider = new RelayDialogueProvider("http://localhost:8000");
        _director = new RelayDirectorProvider("http://localhost:8000");

        _sheet = CharacterLoader.Load(characterId);
        if (_sheet == null)
        {
            Append($"<color=#cc4444><i>[could not load character '{characterId}']</i></color>");
        }
        else
        {
            EmotionProfile emotion = overrideEmotion ? BuildSliderProfile() : _sheet.EmotionBaseline;
            _systemPrompt = PromptAssembler.Assemble(_sheet, emotion);
        }

        sendButton.onClick.AddListener(OnSend);
        inputField.onSubmit.AddListener(_ => OnSend());
        RefreshDebug(null, default);
    }

    private EmotionProfile BuildSliderProfile()
    {
        return new EmotionProfile
        {
            Mood = mood, Boldness = boldness, Friendliness = friendliness, Anger = anger,
            Trust = trust, Playfulness = playfulness, Talkativeness = talkativeness, Confidence = confidence
        };
    }

    private async void OnSend()
    {
        if (_sheet == null) return;

        string userText = inputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        inputField.text = "";
        SetBusy(true);

        _history.Add(new ChatMessage("user", userText));
        Append($"<b>You:</b> {userText}");

        try
        {
            // 1) DIRECTOR scores the player's message (first API call).
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

            // 2) SANITY updates from the scores + time decay (local, no API).
            SanityModel.TurnResult turn =
                sanity.ApplyTurn(score.Rudeness, score.OffTopic, score.Contradiction);

            // 3) GENERATION produces the reply (second API call). Unchanged for now —
            //    Phase 4 will feed the sanity band in here as corruption modifiers.
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

    // Last few lines of transcript, for the Director to judge contradiction in context.
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
        if (debugText == null) return;
        var sb = new StringBuilder();
        sb.AppendLine($"<b>SANITY: {sanity.current:0.0} / {sanity.max:0}   [{sanity.Band}]</b>");
        if (score != null)
        {
            sb.AppendLine($"Director: rude={score.Rudeness} offTopic={score.OffTopic} contra={score.Contradiction}");
            sb.AppendLine($"reason: {score.Reasoning}");
            sb.AppendLine($"loss this turn: -{turn.totalLoss:0.0}  " +
                          $"(time -{turn.timeLoss:0.0}, rude -{turn.rudenessLoss:0.0}, " +
                          $"off -{turn.offTopicLoss:0.0}, contra -{turn.contradictionLoss:0.0})");
        }
        debugText.text = sb.ToString();
    }

    private void Append(string line)
    {
        transcriptText.text += (transcriptText.text.Length > 0 ? "\n\n" : "") + line;
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetBusy(bool busy)
    {
        sendButton.interactable = !busy;
        inputField.interactable = !busy;
    }
}
