using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Phase 2: the hardcoded prompt is gone. The window now loads a developer-authored JSON
// character sheet and builds the system prompt via the CRE assembler from sheet + emotion.
// No character-builder UI yet (that's Phase 2 chunk 3) — you test the whole data path by
// choosing the bot id and slider values in the Inspector, then pressing Play.
public class ChatWindow : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_InputField inputField;
    public Button sendButton;
    public TMP_Text transcriptText;
    public ScrollRect scrollRect;

    [Header("Which bot to load")]
    [Tooltip("Filename (no .json) under Assets/Resources/Characters/")]
    public string characterId = "bartleby";

    [Header("Emotion source")]
    [Tooltip("If true, use the sliders below. If false, use the sheet's authored baseline.")]
    public bool overrideEmotion = false;

    [Header("Test sliders (used only if Override Emotion is on)")]
    [Range(1, 10)] public int mood = 5;          // Sad <-> Happy
    [Range(1, 10)] public int boldness = 5;      // Shy <-> Bold
    [Range(1, 10)] public int friendliness = 5;  // Cold <-> Friendly
    [Range(1, 10)] public int anger = 5;         // Calm <-> Angry
    [Range(1, 10)] public int trust = 5;         // Suspicious <-> Trusting
    [Range(1, 10)] public int playfulness = 5;   // Serious <-> Playful
    [Range(1, 10)] public int talkativeness = 5; // Quiet <-> Talkative
    [Range(1, 10)] public int confidence = 5;    // Insecure <-> Confident

    private IDialogueProvider _provider;
    private readonly List<ChatMessage> _history = new List<ChatMessage>();
    private CharacterSheet _sheet;
    private string _systemPrompt;

    private void Awake()
    {
        _provider = new RelayDialogueProvider("http://localhost:8000");

        _sheet = CharacterLoader.Load(characterId);
        if (_sheet == null)
        {
            Append($"<color=#cc4444><i>[could not load character '{characterId}']</i></color>");
        }
        else
        {
            EmotionProfile emotion = overrideEmotion ? BuildSliderProfile() : _sheet.EmotionBaseline;
            _systemPrompt = PromptAssembler.Assemble(_sheet, emotion);
            Debug.Log($"[assembled prompt]\n{_systemPrompt}");   // inspect what the model sees
        }

        sendButton.onClick.AddListener(OnSend);
        inputField.onSubmit.AddListener(_ => OnSend());
    }

    private EmotionProfile BuildSliderProfile()
    {
        return new EmotionProfile
        {
            Mood = mood,
            Boldness = boldness,
            Friendliness = friendliness,
            Anger = anger,
            Trust = trust,
            Playfulness = playfulness,
            Talkativeness = talkativeness,
            Confidence = confidence
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
            DialogueResult result = await _provider.GenerateAsync(_systemPrompt, _history);
            _history.Add(new ChatMessage("assistant", result.Reply));
            Append($"<b>{_sheet.Name}:</b> {result.Reply}");
            Debug.Log($"[tokens] in={result.InputTokens} out={result.OutputTokens}");
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
