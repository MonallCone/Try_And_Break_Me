using System;
using System.Collections.Generic;

// The persistent mailbox. Emails exist here whether or not the inbox WINDOW is open, so the
// desktop badge and the story both work without the app being on screen. The EmailApp becomes a
// VIEW of this. Static singleton for simplicity in a single-scene game.
public static class Mailbox
{
    private static readonly List<EmailData> _emails = new List<EmailData>();

    // Fired whenever the mailbox changes (delivery or read), so the badge/inbox can refresh.
    public static event Action Changed;

    public static IReadOnlyList<EmailData> Emails => _emails;

    public static int UnreadCount
    {
        get { int n = 0; foreach (var e in _emails) if (e.unread) n++; return n; }
    }

    // Deliver an authored email by id (the story calls this). Newest goes to the top.
    public static void Deliver(string id)
    {
        string playerName = GameState.I != null ? GameState.I.playerName : "you";
        var email = EmailCatalog.Get(id, playerName);
        DeliverEmail(email);
    }

    // Deliver a pre-built email (lets the story attach an onOpen hook before delivering).
    public static void DeliverEmail(EmailData email)
    {
        if (email == null) return;
        _emails.Insert(0, email);
        Changed?.Invoke();
    }

    public static void MarkRead(EmailData email)
    {
        if (email != null && email.unread)
        {
            email.unread = false;
            Changed?.Invoke();
        }
    }

    public static void Clear()
    {
        _emails.Clear();
        Changed?.Invoke();
    }
}
