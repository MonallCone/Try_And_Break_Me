using UnityEngine;

// Scripted bot comments on completed work. The matching bot speaks in ITS chat window (if open)
// via InjectBotLine, reacting to how well the task scored. Fast, no LLM/quota. These are normal
// (non-ominous) early-game lines; the bots are still helpful here.
public static class BotComments
{
    public static void Deliver(WorkTask task)
    {
        if (task == null) return;
        var chat = ChatRegistry.FindByBotId(task.botId);
        if (chat == null) return;   // bot's window not open; comment is simply skipped

        string line = LineFor(task.type, task.score);
        chat.InjectBotLine(line, ominous: false);
    }

    private static string LineFor(TaskType type, int score)
    {
        bool good = score >= 70;
        bool ok = score >= 40 && score < 70;

        switch (type)
        {
            case TaskType.HRSwipe:
                // score = % of requests approved.
                if (score >= 90) return "You approved almost everyone! That's very generous \u2014 hopefully we can cover the shifts.";
                if (score >= 60) return "Plenty of approvals there. A kind week for the team.";
                if (score >= 40) return "A fairly even split on the holiday requests \u2014 balanced, I like it.";
                if (score >= 10) return "You turned most of those down. The team might be a bit disappointed.";
                return "You rejected every request. That's... a lot of no's. Expect some grumbling.";

            case TaskType.CyberShooter:
                if (good) return "Threats neutralised, network's clean. Efficient. I'm impressed.";
                if (ok)   return "A few got through, but the system's stable. Watch your reaction time.";
                return "That was messy. Half of them breached. We'll be cleaning that up for a while.";

            case TaskType.HelpDeskMaze:
                if (good) return "Ticket resolved fast \u2014 you found the right path. The user's happy.";
                if (ok)   return "Sorted, eventually. Took a couple of wrong turns but you got there.";
                return "That took forever. The user filed a complaint about the wait, sorry.";

            default:
                if (good) return "Good work on that one.";
                if (ok)   return "That's done. Not bad.";
                return "That could've gone better.";
        }
    }
}