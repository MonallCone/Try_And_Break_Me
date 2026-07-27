using System;
using System.Collections.Generic;

// A single email. Authored in C# so the story can deliver specific ones at specific beats,
// and so an email can carry an optional onOpen action later (e.g. enabling a task).
[Serializable]
public class EmailData
{
    public string id;            // unique, e.g. "ceo_initiative"
    public string from;          // e.g. "Steven (CEO)"
    public string subject;
    public string body;
    public bool unread = true;

    // Optional: fires when the player opens/reads this email (story hooks later).
    public Action onOpen;

    public EmailData(string id, string from, string subject, string body)
    {
        this.id = id; this.from = from; this.subject = subject; this.body = body;
    }
}

// The authored catalogue of story emails. Keyed by id. The story delivers them by id at the
// right beat via EmailApp.Deliver(...). Keeping the text here (not scattered) makes the whole
// narrative's email content easy to read and edit in one place.
public static class EmailCatalog
{
    // Build a fresh copy each time so 'unread' state doesn't leak between playthroughs.
    public static EmailData Get(string id, string playerName = "you")
    {
        switch (id)
        {
            case "welcome":
                return new EmailData("welcome", "IT Onboarding",
                    "Welcome to AI International Inc",
                    $"Hi {playerName},\n\nYour workstation is ready. Please check your inbox regularly \u2014 " +
                    "task assignments and company announcements arrive here.\n\nHave a productive first day!\n\n\u2014 IT");

            case "ceo_initiative":
                return new EmailData("ceo_initiative", "Steven (CEO)",
                    "An exciting new initiative \u2014 all staff",
                    $"Team,\n\nI'm thrilled to announce our new AI Partner Programme. Each of you will help train " +
                    "an AI assistant to support \u2014 and eventually take over \u2014 your day-to-day tasks. This is a huge " +
                    "step forward for the company and for all of us.\n\nYour training software will install automatically. " +
                    "Please build your first assistant today.\n\nOnwards and upwards,\nSteven");

            // More story emails get added here as we reach their beats (holiday trap, create-another-bot,
            // tone-shift emails, DELETE THE BOTS, etc.).

            default:
                return new EmailData(id, "Unknown", "(missing email)", $"[No email authored for id '{id}']");
        }
    }
}
