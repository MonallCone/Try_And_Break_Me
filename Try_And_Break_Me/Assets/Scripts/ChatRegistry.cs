using System.Collections.Generic;

// A simple registry of open chat windows, so Sanity Events can find "the chat for Bartleby"
// and inject a scripted line (or later: spam it, move it, corrupt it). Chat controllers
// register on create and unregister on close.
//
// Static for simplicity in a single-scene game. If you ever need multiple independent desktops,
// this becomes an instance owned by a manager.
public static class ChatRegistry
{
    private static readonly List<ChatController> _chats = new List<ChatController>();

    public static IReadOnlyList<ChatController> All => _chats;

    public static void Register(ChatController chat)
    {
        if (chat != null && !_chats.Contains(chat)) _chats.Add(chat);
    }

    public static void Unregister(ChatController chat)
    {
        _chats.Remove(chat);
    }

    // Find the first open chat for a given bot id (e.g. "bartleby").
    public static ChatController FindByBotId(string botId)
    {
        foreach (var c in _chats)
            if (c.BotId == botId) return c;
        return null;
    }

    // Find by display name (e.g. "Bartleby").
    public static ChatController FindByName(string name)
    {
        foreach (var c in _chats)
            if (c.BotName == name) return c;
        return null;
    }

    // The most recently opened chat (handy for "the bot the player is currently building/using").
    public static ChatController Newest => _chats.Count > 0 ? _chats[_chats.Count - 1] : null;

    public static int Count => _chats.Count;

    public static void Clear() => _chats.Clear();
}
