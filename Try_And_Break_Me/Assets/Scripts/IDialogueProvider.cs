using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDialogueProvider
{
    // system  = assembled character context (+ corruption modifiers, from Phase 4)
    // history = running transcript for this bot
    Task<DialogueResult> GenerateAsync(string system, List<ChatMessage> history);
}

// Mirrors the relay's message shape. role is "user" or "assistant".
[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

public class DialogueResult
{
    public string Reply;
    public int InputTokens;
    public int OutputTokens;
}
