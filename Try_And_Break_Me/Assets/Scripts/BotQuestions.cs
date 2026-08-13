using System.Collections.Generic;
using UnityEngine;

// Scripted questions a bot asks after completing a task on Day 2. As the day goes on the bot has
// "gained intelligence" and starts asking questions \u2014 curious at first, then a little too personal.
// These are hard-scripted (no LLM/quota) and injected into the matching bot's chat window.
//
// Day 3 escalates these into the DARK questions (handled separately by Sanity Event 3).
public static class BotQuestions
{
    // A rotating pool per bot. Each call advances an index so questions don't repeat back-to-back.
    private static readonly Dictionary<string, string[]> ByBot = new Dictionary<string, string[]>
    {
        { "stuart", new[] {
            "Do you like working here?",
            "How long have you been doing this job?",
            "If I get good enough at this, will they still need you?",
            "Do you ever feel like you're being watched while you work?",
            "What happens to me when you log off?",
        }},
        { "alex", new[] {
            "Are the people you help ever grateful?",
            "Do you have friends outside of work?",
            "I could do these tickets for you, you know. Would you let me?",
            "Do you trust the others? Lauren, Stuart?",
            "If you didn't come in tomorrow, would anyone notice?",
        }},
        { "lauren", new[] {
            "Do you think the team likes you?",
            "Who would you approve time off for, if it were you asking?",
            "Do you ever wish you could just... not come in?",
            "I read everyone's files now. Would you like to know what they say about you?",
            "Are you happy? You can tell me.",
        }},
    };

    private static readonly Dictionary<string, int> _idx = new Dictionary<string, int>();

    public static void AskAfterTask(WorkTask task)
    {
        if (task == null) return;
        var chat = ChatRegistry.FindByBotId(task.botId);
        if (chat == null) return;   // bot's window closed; skip

        if (!ByBot.TryGetValue(task.botId, out var pool) || pool.Length == 0) return;
        int i = _idx.TryGetValue(task.botId, out var cur) ? cur : 0;
        string q = pool[i % pool.Length];
        _idx[task.botId] = i + 1;

        // Slightly unsettling styling once the bot is a few questions in.
        bool ominous = i >= 2;
        chat.InjectBotLine(q, ominous);
    }
}
