using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Phase 1 deliverable: one window, one bot, hardcoded character, round-trip proven.
// Wire in the Inspector: an InputField (TMP), a Send Button, and a TMP_Text transcript
// inside a ScrollRect. Nothing here knows about sanity, the Director, or multiple bots yet.
public class ChatWindow : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_InputField inputField;
    public Button sendButton;
    public TMP_Text transcriptText;
    public ScrollRect scrollRect;

    // Phase 1: hardcoded. Phase 2 replaces this with the assembled character context
    // from a developer-authored sheet + the player's slider values.
    [TextArea(3, 8)]
    public string systemPrompt =
        "You are Bartleby, a cheerful shopkeeper in a small fantasy town. " +
        "You are warm, a little chatty, and proud of your wares. Stay in character.";

    private IDialogueProvider _provider;
    private readonly List<ChatMessage> _history = new List<ChatMessage>();

    private void Awake()
    {
        // The ONE wiring line. Swap this for a different IDialogueProvider to change backends.
        _provider = new RelayDialogueProvider("http://localhost:8000");

        sendButton.onClick.AddListener(OnSend);
        inputField.onSubmit.AddListener(_ => OnSend());
    }

    private async void OnSend()
    {
        string userText = inputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        inputField.text = "";
        SetBusy(true);

        _history.Add(new ChatMessage("user", userText));
        Append($"<b>You:</b> {userText}");

        try
        {
            DialogueResult result = await _provider.GenerateAsync(systemPrompt, _history);
            _history.Add(new ChatMessage("assistant", result.Reply));
            Append($"<b>Bartleby:</b> {result.Reply}");
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
        // Phase 6 will replace this with the "bot is typing / destabilising" treatment.
    }
}
