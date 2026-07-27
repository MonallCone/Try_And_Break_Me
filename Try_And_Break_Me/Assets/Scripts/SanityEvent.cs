using UnityEngine;

// Base class for an authored Sanity Event. Each event is a small C# class with a Fire() method
// that can do ANYTHING — the first ones just inject a scripted chat line, but later ones will
// spawn spam windows, move the player's windows, alter the desktop, or interfere with work tasks.
//
// Events are authored in C# (not data) precisely so they can run arbitrary code like that.
public abstract class SanityEvent
{
    // A short name for logs / the debug panel.
    public abstract string Name { get; }

    // Do the thing. Called by the SanityEventDirector when this event's turn comes up.
    public abstract void Fire();
}

// The simplest event: make a specific bot "say" an authored line, bypassing the LLM.
// This is the core dissonance mechanic — a sharp, wrong, scripted line erupting into an
// otherwise fluid LLM conversation.
public class ScriptedLineEvent : SanityEvent
{
    private readonly string _botId;     // which bot speaks (by id, e.g. "bartleby"); null = newest chat
    private readonly string _line;
    private readonly bool _ominous;

    public ScriptedLineEvent(string line, string botId = null, bool ominous = true)
    {
        _line = line;
        _botId = botId;
        _ominous = ominous;
    }

    public override string Name => $"ScriptedLine({(_botId ?? "newest")})";

    public override void Fire()
    {
        ChatController chat = string.IsNullOrEmpty(_botId)
            ? ChatRegistry.Newest
            : ChatRegistry.FindByBotId(_botId);

        if (chat == null)
        {
            Debug.LogWarning($"[SanityEvent] {Name}: no chat window found to speak through.");
            return;
        }
        chat.InjectBotLine(_line, _ominous);
    }
}
