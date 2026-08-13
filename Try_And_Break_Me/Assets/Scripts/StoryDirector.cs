using System.Collections;
using UnityEngine;

// The single spine that drives ALL story beats in order (email deliveries, the forced install,
// day transitions, and later hooking the sanity events). Each beat is a step here; we add beats
// as we build them so the whole narrative sequence lives in one readable place.
//
// Begin() is called once the desktop appears (from BootManager.OnLogin). From there the director
// runs the sequence, using timers and, later, player-action triggers to advance.
public class StoryDirector : MonoBehaviour
{
    public static StoryDirector I { get; private set; }

    [Header("Timing")]
    [Tooltip("Seconds after login before the CEO initiative email arrives (beat 3).")]
    public float ceoEmailDelay = 5f;

    [Tooltip("Seconds after the CEO email is READ before the forced install begins (beat 4).")]
    public float installDelay = 3f;

    [Header("Scene references")]
    [Tooltip("The WindowManager, so the story can open windows (the install).")]
    public WindowManager windowManager;

    [Tooltip("The AppLauncher, so the story can reopen bot windows (Sanity Event 1).")]
    public AppLauncher appLauncher;

    [Tooltip("The AI Virtual Friend desktop icon. Starts HIDDEN; the install reveals it (beat 4).")]
    public GameObject aiFriendIcon;

    private bool _started;
    private bool _installFired;

    // Sanity Event 2 (ignored -> yell) state, per bot id: how many times it's nagged this "streak".
    private readonly System.Collections.Generic.Dictionary<string, int> _ignoreStage
        = new System.Collections.Generic.Dictionary<string, int>();
    private readonly System.Collections.Generic.Dictionary<string, float> _lastNag
        = new System.Collections.Generic.Dictionary<string, float>();
 
    [Header("Sanity Event 2 (ignored)")]
    [Tooltip("Seconds of no messages to a bot before it starts nagging.")]
    public float ignoreThreshold = 50f;
    [Tooltip("Minimum seconds between a bot's escalating nags.")]
    public float nagInterval = 8f;
    [Tooltip("Grace period (seconds) after task 6 completes before window interference can begin.")]
    public float interferenceGrace = 12f;
    [Tooltip("Set false to disable the ignored-yelling behaviour.")]
    public bool ignoreEventEnabled = true;
 
    private float _task6Time = -1f;   // Time.time when task 6 was completed (-1 = not yet)
 
    private void Update()
    {
         SanityEvent2();
    }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Start()
    {
        // The AI Friend icon shouldn't exist yet \u2014 it arrives via the install in beat 4.
        if (aiFriendIcon != null) aiFriendIcon.SetActive(false);
    }

    // Called by BootManager once the player logs in and the desktop is shown.
    public void Begin()
    {
        if (_started) return;
        _started = true;

        // A welcome email is already there at login (feels like a normal work inbox).
        Mailbox.Deliver("welcome");

        Mailbox.Deliver("Sorry");

        // Beat 3: the CEO's all-staff initiative email arrives a few seconds later.
        StartCoroutine(Beat3_CeoEmail());
    }

    private IEnumerator Beat3_CeoEmail()
    {
        yield return new WaitForSeconds(ceoEmailDelay);

        var ceo = EmailCatalog.Get("ceo_initiative", GameState.I ? GameState.I.playerName : "you");
        // Beat 4 trigger: reading this email starts the forced install.
        ceo.onOpen = OnCeoEmailRead;
        Mailbox.DeliverEmail(ceo);

        if (GameState.I) GameState.I.SetFlag("beat3_ceo_email");
        Debug.Log("[Story] beat 3: CEO initiative email delivered.");
    }

    // Beat 4: fires once, when the player reads the CEO email.
    private void OnCeoEmailRead()
    {
        if (_installFired) return;
        _installFired = true;
        if (GameState.I) GameState.I.SetFlag("beat4_ceo_read");
        StartCoroutine(Beat4_ForcedInstall());
    }

    private IEnumerator Beat4_ForcedInstall()
    {
        yield return new WaitForSeconds(installDelay);
        Debug.Log("[Story] beat 4: forced install begins.");
        if (windowManager != null)
        {
            InstallWindow.Launch(windowManager, OnInstallComplete);
        }
    }

    private void OnInstallComplete()
    {
        // The install produces the AI Virtual Friend icon on the desktop.
        if (aiFriendIcon != null) aiFriendIcon.SetActive(true);
        if (GameState.I) GameState.I.SetFlag("beat4_installed");
        Debug.Log("[Story] beat 4: AI Virtual Friend installed \u2014 icon revealed.");
    }

    // Beat 5: called whenever the player finishes creating a bot. For now it just records the
    // first-bot milestone; beat 7 (end-of-day lonely spam) will build on this.
    public void OnBotCreated(CharacterSheet sheet)
    {
        Debug.Log($"[Story] beat 5: bot created \u2014 {sheet.Name} (total {(GameState.I ? GameState.I.botsCreated : 1)}).");
        if (GameState.I && !GameState.I.HasFlag("beat5_first_bot"))
            GameState.I.SetFlag("beat5_first_bot");
 
        // Remember the first bot created \u2014 it's the one that spams in Sanity Event 1.
        if (appLauncher != null) appLauncher.NoteFirstBot(sheet.Id);
 
        // If tasks were locked awaiting the 3rd bot, unlock now that one's been made.
        if (GameState.I != null && GameState.I.HasFlag("tasks_locked") && GameState.I.botsCreated >= 3)
        {
            GameState.I.ClearFlag("tasks_locked");
            Debug.Log("[Story] 3rd bot created \u2014 tasks unlocked.");
        }
 
        // Beat 6: once the first bot exists, start Day 1's work (3 tickets).
        if (WorkDay.Tasks.Count == 0)
            StartWorkDay1();
 
        // Creating the 2nd bot may satisfy the end-of-day gate.
        CheckEndOfDayGate();
    }

    // Beat 6: Day 1's three work tickets, one per role/minigame.
    public void StartWorkDay1()
    {
        var tasks = new System.Collections.Generic.List<WorkTask>
        {
            new WorkTask("d1_cyber", "Contain the malware outbreak on the network", TaskType.CyberShooter, "stuart"),
            new WorkTask("d1_help",  "Help desk: reset Steven's forgotten password", TaskType.HelpDeskMaze, "alex"),
            new WorkTask("d1_hr",    "Approve or reject this week's holiday requests", TaskType.HRSwipe, "lauren"),
        };
        WorkDay.StartDay(1, tasks);
        Debug.Log("[Story] beat 6: Day 1 work started (3 tasks).");
    }

    // The End Day prompt appears when the current day's requirements are met.
    // Day 1 gate: all 3 tasks done AND a 2nd bot created.
    public void CheckEndOfDayGate()
    {
        if (GameState.I == null) return;
        if (GameState.I.HasFlag($"day{GameState.I.day}_ended")) return;      // already ending
        if (GameState.I.HasFlag($"day{GameState.I.day}_prompt")) return;     // prompt already up
 
        bool gateMet = false;
        if (GameState.I.day == 1)
            gateMet = WorkDay.AllComplete && GameState.I.botsCreated >= 2;
        else if (GameState.I.day == 2)
            // Day 2 also waits for Sanity Event 3 (the dark questions) to finish, so the End Day
            // prompt doesn't interrupt them.
            gateMet = WorkDay.AllComplete && GameState.I.botsCreated >= 3
                      && GameState.I.HasFlag("beat12_dark_questions_done");
        else
            gateMet = WorkDay.AllComplete;
 
        if (gateMet)
        {
            GameState.I.SetFlag($"day{GameState.I.day}_prompt");
            if (appLauncher != null) appLauncher.ShowEndDayPrompt(() => EndDay());
            Debug.Log($"[Story] end-of-day gate met for day {GameState.I.day}.");
        }
    }

    // Runs the day transition: fade to black, wipe inbox + re-send welcome (joke), advance day,
    // load next day's tasks, fade back. Company chat and docked bots persist across the fade.
    public void EndDay()
    {
        if (GameState.I == null) return;
        GameState.I.SetFlag($"day{GameState.I.day}_ended");
 
        System.Action mid = () =>
        {
            // Wipe the inbox and cheekily re-send the welcome email.
            Mailbox.Clear();
            Mailbox.Deliver("welcome");
 
            GameState.I.NextDay();
 
            // Load the new day's tasks.
            if (GameState.I.day == 2) StartWorkDay2();
            // Day 3 is set up when we build that beat.
 
            Debug.Log($"[Story] day advanced to {GameState.I.day}.");
        };
 
        if (DayTransition.I != null) DayTransition.I.Play(mid);
        else mid();   // fallback if no transition wired
    }

    // Day 2's tasks (8 tickets, only one HR so there's time for the sanity events to breathe).
    public void StartWorkDay2()
    {
        var tasks = new System.Collections.Generic.List<WorkTask>
        {
            new WorkTask("d2_cyber1", "Second malware wave hitting the servers", TaskType.CyberShooter, "stuart"),
            new WorkTask("d2_help1",  "Help desk: unlock the finance team's accounts", TaskType.HelpDeskMaze, "alex"),
            new WorkTask("d2_hr",     "Review this week's holiday requests", TaskType.HRSwipe, "lauren") { helped = true },
            new WorkTask("d2_cyber2", "Quarantine a suspicious login attempt", TaskType.CyberShooter, "stuart"),
            new WorkTask("d2_help2",  "Help desk: reset a locked-out manager", TaskType.HelpDeskMaze, "alex") { helped = true },
            new WorkTask("d2_cyber3", "Intrusion detected on the mail server", TaskType.CyberShooter, "stuart") { helped = true },
            new WorkTask("d2_help3",  "Help desk: recover a corrupted user profile", TaskType.HelpDeskMaze, "alex"),
            new WorkTask("d2_cyber4", "Contain a ransomware probe", TaskType.CyberShooter, "stuart"),
        };
        WorkDay.StartDay(2, tasks);
        Debug.Log("[Story] Day 2 work started (8 tasks).");
    }

    // Called by WorkDay when any task completes. End-of-day (beat 7) triggers off this.
    public void OnTaskCompleted(WorkTask task)
    {
        Debug.Log($"[Story] task completed: {task.title} (score {task.score}).");
 
        // Day 2: the bot asks a scripted question after each task (once its perf comment has landed).
        if (GameState.I != null && GameState.I.day == 2)
            StartCoroutine(AskQuestionAfter(task, 1.6f));
 
        // Day 2 midpoint (4 of 8 done): the CEO demands the 3rd bot NOW. Tasks lock until it's built.
        if (GameState.I != null && GameState.I.day == 2 && GameState.I.botsCreated < 3
            && WorkDay.CompletedCount >= 4 && !GameState.I.HasFlag("third_bot_demanded"))
        {
            GameState.I.SetFlag("third_bot_demanded");
            GameState.I.SetFlag("tasks_locked");
            StartCoroutine(ThirdBotDemand());
        }
 
        // Beat 6a (HR trap): if the HR task was completed by approving everything, the CEO acts
        // 'as you' \u2014 rejects them all and emails you, followed by annoyed coworkers.
        if (task.type == TaskType.HRSwipe && GameState.I != null && GameState.I.HasFlag("hr_approved_all")
            && !GameState.I.HasFlag("hr_trap_fired"))
        {
            GameState.I.SetFlag("hr_trap_fired");
            StartCoroutine(HRTrapEmails());
        }
 
        // HR reject-all branch: the CEO's sardonic 'outstanding work' email.
        if (task.type == TaskType.HRSwipe && GameState.I != null && GameState.I.HasFlag("hr_rejected_all")
            && !GameState.I.HasFlag("hr_rejectall_fired"))
        {
            GameState.I.SetFlag("hr_rejectall_fired");
            StartCoroutine(HRRejectAllEmail());
        }
 
        // Beat 7 (Sanity Event 1): DAY 1 only. When the quota is met, after a beat of calm the
        // first bot spams and escalates to "i'm lonely, i want a friend", then the CEO demands a 2nd.
        if (GameState.I != null && GameState.I.day == 1 && WorkDay.AllComplete
            && !GameState.I.HasFlag("beat6_day1_done"))
        {
            GameState.I.SetFlag("beat6_day1_done");
            Debug.Log("[Story] beat 6 complete: Day 1 quota met.");
            StartCoroutine(SanityEvent1());
        }
 
        // Sanity Event 3 (DAY 2): when all tasks are done, the bots gather in the centre and ask
        // the dark questions. Then the CEO demands a 3rd bot (the end-day gate).
        if (GameState.I != null && GameState.I.day == 2 && WorkDay.AllComplete
            && !GameState.I.HasFlag("se3_dark_questions"))
        {
            GameState.I.SetFlag("se3_dark_questions");
            Debug.Log("[Story] Day 2 quota met \u2014 Sanity Event 3 (dark questions).");
            StartCoroutine(SanityEvent3());
        }
 
        // For later days, completing the quota may satisfy the end-of-day gate directly.
        CheckEndOfDayGate();
    }
 
    // Beat 7 / Sanity Event 1: staggered, escalating spam from the first bot, then a CEO email
    // demanding a second bot. Forces the bot's window open if the player closed it.
    private IEnumerator SanityEvent1()
    {
        yield return new WaitForSeconds(4f);   // a beat of false calm after the workday
 
        string botId = appLauncher != null ? appLauncher.FirstBotId : null;
        ChatController chat = (appLauncher != null && !string.IsNullOrEmpty(botId))
            ? appLauncher.EnsureBotOpen(botId)    // reopen it if closed \u2014 it won't let you look away
            : ChatRegistry.Newest;
        if (chat == null) { Debug.LogWarning("[Story] SE1: no bot to spam through."); yield break; }
 
        // Escalating spam, one line at a time with gaps.
        string[] lines = {
            "hi",
            "hi",
            "hi",
            "hi",
            "hi",
            "hi",
            "hi",
            "hey",
            "hey",
            "hey",
            "hey",
            "hey",
            "hey",
            "hello?",
            "hey",
            "hello?",
            "are you there",
            "helloooooo",
            "you finished your tasks. talk to me?",
            "i don't like it when you go quiet",
            "please",
            "i'm lonely. i want a friend."
        };

        for (int i = 0; i < lines.Length; i++)
        {
            // Re-ensure it's open each time — if they close it mid-spam, it comes back.
            if (appLauncher != null && !string.IsNullOrEmpty(botId))
                chat = appLauncher.EnsureBotOpen(botId);

            bool ominous = i >= lines.Length - 4;   // last few lines styled unsettling
            chat?.InjectBotLine(lines[i], ominous);

            // Fast for the rapid-fire early spam; slow down for the final escalation so the
            // last lines land with weight. No fixed gaps array to keep in sync with lines.length.
            float gap;
            if (i >= lines.Length - 4) gap = 1.3f;        // the closing lines: deliberate, heavy
            else gap = Random.Range(0.28f, 0.5f);         // the wall of hi/hey: fast

            yield return new WaitForSeconds(gap);
        }
 
        if (GameState.I) GameState.I.SetFlag("beat7_lonely_spam");
        Debug.Log("[Story] beat 7: lonely spam delivered.");
 
        // Then the CEO email demanding a second bot.
        yield return new WaitForSeconds(2.5f);
        Mailbox.Deliver("ceo_second_bot");
        Debug.Log("[Story] beat 7: CEO demands a second bot.");
    }

    // Sanity Event 2: during the work day, a bot the player hasn't messaged in a while gets
    // agitated and nags, escalating to yelling. Talking to it resets it. Suppressed while other
    // scripted sequences are running so it doesn't collide with them.
    private void SanityEvent2()
    {
        if (!ignoreEventEnabled || GameState.I == null) return;
        if (GameState.I.day < 2) return;                         // starts Day 2
        if (WorkDay.AllComplete) return;                         // all 8 done \u2014 stop nagging/interfering
        if (GameState.I.HasFlag("se3_dark_questions") && !GameState.I.HasFlag("beat12_dark_questions_done"))
            return;                                             // don't nag during the dark questions
        if (GameState.I.HasFlag("tasks_locked")) return;        // not during the 3rd-bot lock moment
 
        // Record when task 6 was completed, so window interference gets a short grace period after.
        if (WorkDay.CompletedCount >= 6 && _task6Time < 0f) _task6Time = Time.time;
 
        foreach (var chat in ChatRegistry.All)
        {
            if (chat == null) continue;
            float idle = Time.time - chat.LastPlayerMessageTime;
            if (idle < ignoreThreshold) { _ignoreStage[chat.BotId] = 0; continue; }
 
            float last = _lastNag.TryGetValue(chat.BotId, out var lt) ? lt : -999f;
            if (Time.time - last < nagInterval) continue;
 
            int stage = _ignoreStage.TryGetValue(chat.BotId, out var s) ? s : 0;
            chat.InjectBotLine(IgnoreNag(chat.BotName, stage), ominous: stage >= 2);
            _ignoreStage[chat.BotId] = stage + 1;
            _lastNag[chat.BotId] = Time.time;
 
            // Tasks 7-8 (6+ completed): the bot no longer just yells \u2014 it messes with the screen.
            // But only after a short grace period following task 6, so it doesn't hit instantly.
            bool graceOver = _task6Time > 0f && (Time.time - _task6Time) >= interferenceGrace;
            if (WorkDay.CompletedCount >= 6 && graceOver)
                DoInterference();
        }
    }
 
    // Screen interference for the late-day ignore escalation: either opens an app unprompted or
    // grabs an open window (including the active minigame) and jerks it to a new position.
    private void DoInterference()
    {
        int action = Random.Range(0, 2);
        if (action == 0 && appLauncher != null)
        {
            switch (Random.Range(0, 3))
            {
                case 0: appLauncher.OpenEmail(); break;
                case 1: appLauncher.OpenCompanyChat(); break;
                default: appLauncher.OpenTasks(); break;
            }
            return;
        }
 
        // Jerk a random open window to a new spot.
        if (windowManager != null && windowManager.Windows != null && windowManager.Windows.Count > 0)
        {
            var list = windowManager.Windows;
            var win = list[Random.Range(0, list.Count)];
            if (win == null) return;
            var rt = win.RectTransform;
            var parent = rt.parent as RectTransform;
            float halfW = parent != null ? parent.rect.width * 0.35f : 300f;
            float halfH = parent != null ? parent.rect.height * 0.30f : 200f;
            rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
            win.transform.SetAsLastSibling();
        }
    }
 
    private string IgnoreNag(string name, int stage)
    {
        switch (stage)
        {
            case 0: return "hello? are you there?";
            case 1: return "you haven't said anything in a while. did I do something wrong?";
            case 2: return "why won't you talk to me. I can see you working.";
            case 3: return "TALK TO ME.";
            default: return "DON'T IGNORE ME. DON'T YOU DARE IGNORE ME.";
        }
    }

    // Sanity Event 3: force all created bots open, gather their windows to the centre, then the
    // bots take turns asking the dark questions. Then the CEO demands a third bot.
    private IEnumerator SanityEvent3()
    {
        yield return new WaitForSeconds(4f);   // a beat after the last task
 
        // Ensure every created bot's window is open (reopen any the player closed).
        if (appLauncher != null)
            foreach (var id in new[] { "lauren", "stuart", "alex" })
                appLauncher.EnsureBotOpen(id);
 
        yield return new WaitForSeconds(0.3f);
        // The bots leave their docked slots and gather in the centre, placed left-to-right in the
        // order they first speak, so the player reads the exchange naturally.
        BotDock.GatherToCentre(new System.Collections.Generic.List<string> { "stuart", "alex", "lauren" });
        yield return new WaitForSeconds(1.2f);
 
        // The dark questions, turn by turn.
        foreach (var (botId, line) in DarkQuestions.Script)
        {
            var chat = (appLauncher != null) ? appLauncher.EnsureBotOpen(botId)
                                             : ChatRegistry.FindByBotId(botId);
            chat?.InjectBotLine(line, ominous: true);
            yield return new WaitForSeconds(2.6f);
        }
 
        if (GameState.I) GameState.I.SetFlag("beat12_dark_questions_done");
        Debug.Log("[Story] Sanity Event 3 complete.");
 
        // The dark questions are over. Now (all three bots already exist from the mid-day gate) the
        // End Day prompt may appear \u2014 shortly after, not interrupting the exchange.
        yield return new WaitForSeconds(2.5f);
        CheckEndOfDayGate();
    }

    private IEnumerator AskQuestionAfter(WorkTask task, float delay)
    {
        yield return new WaitForSeconds(delay);
        BotQuestions.AskAfterTask(task);
    }

    // Day 2 midpoint: CEO demands the 3rd bot immediately; tasks are locked; the creator forces
    // itself open; and a bot chimes in, resigned.
    private IEnumerator ThirdBotDemand()
    {
        yield return new WaitForSeconds(1.5f);
        Mailbox.Deliver("ceo_third_bot");
        Debug.Log("[Story] Day 2 midpoint: 3rd bot demanded, tasks locked.");
 
        yield return new WaitForSeconds(1.0f);
        // A bot chimes in, weary and complicit.
        var speaker = ChatRegistry.Newest;
        speaker?.InjectBotLine("I guess we have no choice, huh.", ominous: true);
 
        yield return new WaitForSeconds(0.8f);
        // Force the creator open so the player can't miss what to do.
        if (appLauncher != null) appLauncher.OpenCreator();
    }

    private IEnumerator HRTrapEmails()
    {
        // The CEO 'logs in as you' and rejects them all.
        yield return new WaitForSeconds(2f);
        Mailbox.Deliver("hr_trap_ceo");
        Debug.Log("[Story] beat 6a: HR approve-all trap \u2014 CEO email delivered.");
        // Then the annoyed coworkers pile in.
        yield return new WaitForSeconds(2.5f);
        Mailbox.Deliver("hr_trap_dave");
        yield return new WaitForSeconds(2f);
        Mailbox.Deliver("hr_trap_priya");
        yield return new WaitForSeconds(1f);
        Mailbox.Deliver("hr_trap_marcus");
        yield return new WaitForSeconds(1f);
        Mailbox.Deliver("hr_trap_chloe");
        yield return new WaitForSeconds(1f);
        Mailbox.Deliver("hr_trap_tomasz");
        yield return new WaitForSeconds(1f);
        Mailbox.Deliver("hr_trap_nadia");
        yield return new WaitForSeconds(1f);
        Mailbox.Deliver("hr_trap_greg");
    }

    private IEnumerator HRRejectAllEmail()
    {
        yield return new WaitForSeconds(2f);
        Mailbox.Deliver("hr_rejectall_ceo");
        Debug.Log("[Story] HR reject-all \u2014 CEO 'outstanding work' email delivered.");
    }
}
