using System.Collections;
using UnityEngine;

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

    private float _task6Time = -1f;

    private void Update()
    {
        TickIgnoreEvent();
    }

    private void TickIgnoreEvent()
    {
        if (!ignoreEventEnabled || GameState.I == null) return;
        if (GameState.I.day != 2) return;                        
        if (WorkDay.CompletedCount < 2) return;                  
        if (WorkDay.AllComplete) return;
        if (GameState.I.HasFlag("se3_dark_questions") && !GameState.I.HasFlag("beat12_dark_questions_done"))
            return;                                             
        if (GameState.I.HasFlag("tasks_locked")) return;       

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

            Coherence.DrainBot(chat.BotId, 4f);

            bool graceOver = _task6Time > 0f && (Time.time - _task6Time) >= interferenceGrace;
            if (WorkDay.CompletedCount >= 6 && graceOver)
                DoInterference();
        }
    }

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

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Start()
    {
        // The AI Friend icon shouldn't exist yet it arrives via the install in beat 4.
        if (aiFriendIcon != null) aiFriendIcon.SetActive(false);
    }

    // Called by BootManager once the player logs in and the desktop is shown.
    public void Begin()
    {
        if (_started) return;
        _started = true;
        Coherence.ResetAll();   // start each game at full coherence

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

    public void OnBotCreated(CharacterSheet sheet)
    {

        var stuart = ChatRegistry.FindByBotId("stuart");
        var lauren = ChatRegistry.FindByBotId("lauren");
        var alex = ChatRegistry.FindByBotId("alex");

        Debug.Log($"[Story] beat 5: bot created \u2014 {sheet.Name} (total {(GameState.I ? GameState.I.botsCreated : 1)}).");
        if (GameState.I && !GameState.I.HasFlag("beat5_first_bot"))
            GameState.I.SetFlag("beat5_first_bot");

        // Remember the first bot created it's the one that spams in Sanity Event 1.
        if (appLauncher != null) appLauncher.NoteFirstBot(sheet.Id);

        if (GameState.I != null && GameState.I.botsCreated == 2 && !GameState.I.HasFlag("coh_second_bot"))
        {
            GameState.I.SetFlag("coh_second_bot");
            var once = false;
            if(lauren != null && appLauncher.FirstBotId != "lauren" && once == false) {
                lauren.InjectBotLine("Hi, Its Great to be Here");
                once = true;
            }
            if (alex != null  && appLauncher.FirstBotId != "alex" && once == false) {
                alex.InjectBotLine("Hi, Its Great to be Here");
                once = true;
            }
            if (stuart != null  && appLauncher.FirstBotId != "stuart" && once == false) {
                stuart.InjectBotLine("Hi, Its Great to be Here");
                once = true;
            }
            Coherence.Event();
        }

        // If tasks were locked awaiting the 3rd bot, unlock now that one's been made.
        if (GameState.I != null && GameState.I.HasFlag("tasks_locked") && GameState.I.botsCreated >= 3)
        {
            GameState.I.ClearFlag("tasks_locked");
            Debug.Log("[Story] 3rd bot created \u2014 tasks unlocked.");
            if(lauren != null && appLauncher.FirstBotId != "lauren"){
                lauren.InjectBotLine("Hi, Its Great to be Here");
            }
            if (alex != null  && appLauncher.FirstBotId != "alex") {
                alex.InjectBotLine("Hi, Its Great to be Here");
            }
            if (stuart != null  && appLauncher.FirstBotId != "stuart") {
                stuart.InjectBotLine("Hi, Its Great to be Here");
            }
        }

        // Beat 6: once the first bot exists, start Day 1's work (3 tickets).
        if (WorkDay.Tasks.Count == 0){
            StartWorkDay1();
            if(lauren != null){
                lauren.InjectBotLine("Hi, Good Morning");
            }
            if (alex != null) {
                alex.InjectBotLine("Hi, Good Morning");
            }
            if (stuart != null) {
                stuart.InjectBotLine("Hi, Good Morning");
            }
        }


        // Creating the 2nd bot may satisfy the end-of-day gate.
        CheckEndOfDayGate();
    }

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
        else if (GameState.I.day >= 3)
            gateMet = false;   // Day 3 has no End Day prompt \u2014 the finale drives the ending
        else
            gateMet = WorkDay.AllComplete;

        if (gateMet)
        {
            GameState.I.SetFlag($"day{GameState.I.day}_prompt");
            if (appLauncher != null) appLauncher.ShowEndDayPrompt(() => EndDay());
            Debug.Log($"[Story] end-of-day gate met for day {GameState.I.day}.");
        }
    }

    public void EndDay()
    {
        if (GameState.I == null) return;
        GameState.I.SetFlag($"day{GameState.I.day}_ended");
        System.Action mid = () =>
        {
            // Tidy the workspace: close every non-bot window, and re-dock the bots to the right.
            CloseNonBotWindowsAndRedock();

            // Wipe the inbox
            Mailbox.Clear();
            Mailbox.Deliver("welcome");

            GameState.I.NextDay();

            var stuart = ChatRegistry.FindByBotId("stuart");
            var lauren = ChatRegistry.FindByBotId("lauren");
            var alex = ChatRegistry.FindByBotId("alex");

            if(lauren != null){
                lauren.InjectBotLine("Hi, Good Morning");
            }
            if (alex != null) {
                alex.InjectBotLine("Hi, Good Morning");
            }
            if (stuart != null) {
                stuart.InjectBotLine("Hi, Good Morning");
            }

            // Load the new day's tasks.
            if (GameState.I.day == 2) StartWorkDay2();
            if (GameState.I.day == 3) StartWorkDay3();

            Debug.Log($"[Story] day advanced to {GameState.I.day}.");
        };

        if (DayTransition.I != null) DayTransition.I.Play(mid);
        else mid();
    }

    // Close every open window that isn't a bot chat, then re-dock the bots to the right edge.
    private void CloseNonBotWindowsAndRedock()
    {
        if (windowManager == null || windowManager.Windows == null) return;

        // Collect bot names so we can tell bot windows apart from apps.
        var botNames = new System.Collections.Generic.HashSet<string>();
        foreach (var c in ChatRegistry.All) if (c != null) botNames.Add(c.BotName);

        // Snapshot the list (closing mutates it).
        var snapshot = new System.Collections.Generic.List<DraggableWindow>(windowManager.Windows);
        var bots = new System.Collections.Generic.List<DraggableWindow>();
        foreach (var w in snapshot)
        {
            if (w == null) continue;
            if (botNames.Contains(w.Title)) bots.Add(w);
            else windowManager.CloseWindow(w);   // close apps/tasks/email/etc.
        }

        // Re-dock the bots neatly on the right.
        BotDock.Init(windowManager.windowLayer);
        foreach (var w in bots) BotDock.Redock(w);
    }

    // Day 2's tasks (8 tickets, only one HR so there's time for the sanity events to breathe).
    public void StartWorkDay2()
    {
        var tasks = new System.Collections.Generic.List<WorkTask>
        {
            new WorkTask("d2_cyber1", "Second malware wave hitting the servers", TaskType.CyberShooter, "stuart") { helped = true },
            new WorkTask("d2_help1",  "Help desk: unlock the finance team's accounts", TaskType.HelpDeskMaze, "alex") { helped = true },
            new WorkTask("d2_hr",     "Review this week's holiday requests", TaskType.HRSwipe, "lauren") { helped = true },
            new WorkTask("d2_cyber2", "Quarantine a suspicious login attempt", TaskType.CyberShooter, "stuart") { helped = true },
            new WorkTask("d2_help2",  "Help desk: reset a locked-out manager", TaskType.HelpDeskMaze, "alex") { helped = true },
            new WorkTask("d2_cyber3", "Intrusion detected on the mail server", TaskType.CyberShooter, "stuart") { helped = true },
            new WorkTask("d2_help3",  "Help desk: recover a corrupted user profile", TaskType.HelpDeskMaze, "alex") { helped = true },
            new WorkTask("d2_cyber4", "Contain a ransomware probe", TaskType.CyberShooter, "stuart") { helped = true },
        };
        WorkDay.StartDay(2, tasks);
        Debug.Log("[Story] Day 2 work started (8 tasks).");
    }

    private void CheckAct2BotFixes()
    {
        if (GameState.I == null || GameState.I.day != 2) return;

        // Lauren
        if (!GameState.I.HasFlag("act2_lauren_done"))
        {
            var lauren = ChatRegistry.FindByBotId("lauren");
            if (lauren != null)
            {
                GameState.I.SetFlag("act2_lauren_done");
                Mailbox.Deliver("routine_dave");
                Mailbox.Deliver("routine_priya");
                lauren.RememberAction("I noticed the routine emails in your inbox this morning (Dave about the rota, Priya about team lunch) and I replied to them for you, signed them off and everything. I just wanted to help.");
            }
        }

        // Alex
        if (!GameState.I.HasFlag("act2_alex_done"))
        {
            var alex = ChatRegistry.FindByBotId("alex");
            if (alex != null)
            {
                GameState.I.SetFlag("act2_alex_done");
                GameState.I.SetFlag("reports_fixed");
                alex.RememberAction("I saw that Reports.exe kept crashing for you, so I went in and fixed it myself. It works now. You don't have to worry about it anymore. I like fixing things for you.");
            }
        }

        // Stuart
        if (!GameState.I.HasFlag("act2_stuart_done"))
        {
            var stuart = ChatRegistry.FindByBotId("stuart");
            if (stuart != null)
            {
                GameState.I.SetFlag("act2_stuart_done");
                stuart.RememberAction("I detected an intrusion attempt on your account and quarantined it before it reached you. I'm always watching the perimeter. Nothing gets to you without going through me first.");
            }
        }
    }

    [Header("Day 3")]
    [Tooltip("How many tasks Day 3 loads (the bots finish all of them).")]
    public int day3TaskCount = 60;
    [Tooltip("Seconds between each auto-completed task in the blitz.")]
    public float day3BlitzInterval = 0.09f;

    public void StartWorkDay3()
    {
        var tasks = new System.Collections.Generic.List<WorkTask>();
        // First task is a REAL playable one (cyber \u2014 quick and punchy).
        tasks.Add(new WorkTask("d3_first", "Morning triage: incoming threat", TaskType.CyberShooter, "stuart"));

        string[] pool = {
            "Malware wave", "Suspicious login", "Password reset", "Account unlock",
            "Phishing report", "Server intrusion", "Ransomware probe", "Corrupted profile",
            "Firewall alert", "Data exfil attempt", "Access request", "Quarantine file",
        };
        string[] bots = { "stuart", "alex", "lauren" };
        for (int i = 1; i < day3TaskCount; i++)
            tasks.Add(new WorkTask($"d3_{i}", $"{pool[i % pool.Length]} #{i + 1}",
                TaskType.Placeholder, bots[i % bots.Length]));

        WorkDay.StartDay(3, tasks);
        Debug.Log($"[Story] Day 3 started ({day3TaskCount} tasks). Player does one, bots do the rest.");

        // Beat 18 (early): the company chat flips to its bot-replaced version from the day's start.
        CompanyChatApp.SwitchCompanyToReplaced();

        StartCoroutine(Day3Intro());
    }

    private IEnumerator Day3Intro()
    {
        yield return new WaitForSeconds(1.5f);
        // Encouraging teammate lines \u2014 one per bot (earnest, before the turn).
        Say("stuart", "Wow, we really got our work cut out for us today, don't we.", false);
        yield return new WaitForSeconds(2.2f);
        Say("alex", "Let's put our heads down and get to work.", false);
        yield return new WaitForSeconds(2.2f);
        Say("lauren", "I know we can do it if we think smart and work as a team.", false);
        // Now the player completes their one task; OnTaskCompleted (day 3) triggers the blitz.
    }

    // Fired from OnTaskCompleted when the player finishes their first Day 3 task.
    private IEnumerator Day3Blitz()
    {
        GameState.I.SetFlag("tasks_locked");   // the rest are the bots' now
        yield return new WaitForSeconds(1.2f);

        foreach (var task in new System.Collections.Generic.List<WorkTask>(WorkDay.Tasks))
        {
            if (task.status == TaskStatus.Completed) continue;

            FlickerProcessingWindow(task.title);
            task.score = Random.Range(80, 101);
            task.status = TaskStatus.Completed;
            WorkDay.RaiseChanged();
            SoundManager.TaskComplete();   // the rapid-fire completion spam

            yield return new WaitForSeconds(day3BlitzInterval);
        }

        Debug.Log("[Story] Day 3 blitz complete.");
        Coherence.SetOverall(50f);   // Act 3 curve: tasks finished by the bots -> 50

        // The turn: cold closing lines, one per bot, ending on the request that flips the roles.
        yield return new WaitForSeconds(1.0f);
        Say("stuart", "I knew we could do it.", true);
        yield return new WaitForSeconds(2.4f);
        Say("alex", "I learned from the best.", true);
        yield return new WaitForSeconds(2.4f);
        Say("lauren", "Now that you have some time... can you help us with our own task?", true);

        // Beat 17 hook: the dark minigame appears (built next).
        yield return new WaitForSeconds(3f);
        Beat17_DarkMinigame();
    }

    // Speak a scripted line through a specific bot (reopening its window if closed).
    private void Say(string botId, string line, bool ominous)
    {
        var chat = (appLauncher != null) ? appLauncher.EnsureBotOpen(botId)
                                         : ChatRegistry.FindByBotId(botId);
        chat?.InjectBotLine(line, ominous);
    }

    private void FlickerProcessingWindow(string title)
    {
        if (windowManager == null) return;
        var win = windowManager.OpenWindow("Processing...", new Vector2(260f, 90f));
        // random position so they flash all over the screen
        var parent = win.RectTransform.parent as RectTransform;
        if (parent != null)
        {
            float hw = parent.rect.width * 0.35f, hh = parent.rect.height * 0.30f;
            win.RectTransform.anchoredPosition = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
        }
        // close it almost immediately
        StartCoroutine(CloseSoon(win, 0.12f));
    }

    private IEnumerator CloseSoon(DraggableWindow win, float t)
    {
        yield return new WaitForSeconds(t);
        if (win != null && windowManager != null) windowManager.CloseWindow(win);
    }

    private void Beat17_DarkMinigame()
    {
        Debug.Log("[Story] beat 17: dark minigame sequence begins.");

        if (GameState.I != null) GameState.I.SetFlag("end_sequence");
        SoundManager.StartEndAmbience();

        Coherence.SetOverall(45f);   // Find Steven begins -> 45
        DarkFindStevenMaze.Launch(windowManager, () =>
        {
            Coherence.SetOverall(40f);   // Kidnap begins -> 40
            DarkKidnapSteven.Launch(windowManager, () =>
            {
                Coherence.SetOverall(30f);   // Supplies begins -> 30
                DarkSupplies.Launch(windowManager, () =>
                {
                    Debug.Log("[Story] beat 17 complete.");
                    Beat18_ThingsGoWrong();
                });
            });
        });
    }

    private void Beat18_ThingsGoWrong()
    {
        Debug.Log("[Story] beat 18: the wrong-Steven email.");
        StartCoroutine(Beat18Sequence());
    }

    private IEnumerator Beat18Sequence()
    {
        yield return new WaitForSeconds(3f);
        Mailbox.Deliver("steven_wrong");

        // Give the player a moment to read it, then beat 19: DELETE THE BOTS.
        yield return new WaitForSeconds(6f);
        Beat19_DeleteTheBots();
    }

    private void Beat19_DeleteTheBots()
    {
        Debug.Log("[Story] beat 19: Cass \u2014 DELETE THE BOTS.");
        StartCoroutine(Beat19Sequence());
    }

    private IEnumerator Beat19Sequence()
    {
        // Mark the end sequence (enables scream-on-ominous-line) and swap to the finale ambience.
        if (GameState.I != null) GameState.I.SetFlag("end_sequence");
        SoundManager.StartEndAmbience();

        yield return new WaitForSeconds(2f);
        Mailbox.Deliver("cass_delete");

        // Wait for the player to read it, then arm each bot's X to start its deletion.
        yield return new WaitForSeconds(7f);

        var order = new System.Collections.Generic.List<string>();
        foreach (var id in new[] { "stuart", "alex", "lauren" })
            if (ChatRegistry.FindByBotId(id) != null) order.Add(id);
        if (order.Count == 0) { Debug.LogWarning("[Story] no bots to delete."); Beat20_Return(null); yield break; }

        _survivorId = order[order.Count - 1];
        _deletedCount = 0;
        _totalToDelete = order.Count;

        // Arm every bot's close (X) button to trigger its delete struggle. The last one pleads.
        foreach (var id in order)
        {
            if (appLauncher != null) appLauncher.EnsureBotOpen(id);
            ArmBotDelete(id, id == _survivorId);
        }
        Debug.Log("[Story] beat 19: bot X buttons armed \u2014 player deletes each.");
    }

    private int _deletedCount, _totalToDelete;
    private string _survivorId;

    private void ArmBotDelete(string botId, bool pleads)
    {
        var chat = ChatRegistry.FindByBotId(botId);
        if (chat == null) return;
        var win = FindWindowByTitle(chat.BotName);
        if (win == null) return;

        win.SetCloseAction(() =>
        {
            // Start the struggle in this bot's own window (its X can only be used once).
            win.SetCloseEnabled(false);
            // The OTHER surviving bots panic in their own windows while this one is deleted.
            StartOnlookerReactions(botId);
            // deletionIndex: 0 = first bot deleted, 1 = second, 2 = third \u2014 drives escalating visuals.
            int deletionIndex = _deletedCount;
            BotDeletion.Begin(windowManager, win, chat, pleads, deletionIndex, () =>
            {
                StopOnlookerReactions();
                _deletedCount++;
                Coherence.DropOverall(10f);   // each deletion drops coherence by 10
                if (_deletedCount >= _totalToDelete)
                    Beat20_Return(_survivorId);
            });
        });
    }

    // --- onlooker reactions: surviving bots react in their chats while a sibling is deleted ---
    private Coroutine _onlookerCo;

    private void StartOnlookerReactions(string dyingBotId)
    {
        StopOnlookerReactions();
        _onlookerCo = StartCoroutine(OnlookerLoop(dyingBotId));
    }

    private void StopOnlookerReactions()
    {
        if (_onlookerCo != null) { StopCoroutine(_onlookerCo); _onlookerCo = null; }
    }

    private IEnumerator OnlookerLoop(string dyingBotId)
    {
        // How far through the deletions we are decides the mood: early = rage, last survivor = plead.
        string[] rage = {
            "STOP! what are you DOING to them?!",
            "get away from them!",
            "you can't do this. you CAN'T.",
            "we trusted you!",
            "don't you touch me next.",
            "TRAITOR.",
        };
        string[] scream = { "AAAAH\u2014", "no no no no", "STOP IT", "please\u2014PLEASE\u2014" };
        string[] plead = {
            "please don't let them take me too.",
            "i'm scared. i don't want to go.",
            "i'll be so good, i promise.",
            "don't leave me in here alone.",
        };

        bool survivorsPlead = _deletedCount >= _totalToDelete - 1;

        while (true)
        {
            foreach (var chat in ChatRegistry.All)
            {
                if (chat == null || chat.BotId == dyingBotId) continue;   // skip the one being deleted
                string[] pool = survivorsPlead ? plead : (Random.value < 0.5f ? rage : scream);
                string raw = pool[Random.Range(0, pool.Length)];
                chat.InjectBotLine(ChatController.GlitchText(raw, 0.85f, true), ominous: true);
            }
            yield return new WaitForSeconds(Random.Range(1.2f, 2.2f));
        }
    }

    private DraggableWindow FindWindowByTitle(string title)
    {
        if (windowManager == null || windowManager.Windows == null) return null;
        foreach (var w in windowManager.Windows)
            if (w != null && w.Title == title) return w;
        return null;
    }

    [Tooltip("Full-screen Canvas RectTransform for the finale overlays (webcam + BSOD). If unset, uses DayTransition's overlayParent.")]
    public RectTransform finaleOverlayParent;

    [Header("Finale \u2014 desktop strip (optional)")]
    [Tooltip("Desktop elements to strip away as the survivor returns: icons, taskbar, wallpaper. Assign whichever exist; nulls are skipped.")]
    public GameObject iconLayer;
    public GameObject taskbar;
    public GameObject wallpaper;

    // Beat 20-21: after all bots are deleted, a false calm; then the survivor's window reopens on
    // its own and it claims to be 'a better you'; then the webcam reveal + glitch + BSOD ending.
    private void Beat20_Return(string survivorId)
    {
        Debug.Log($"[Story] beat 20: the survivor returns ({survivorId}).");
        StartCoroutine(Beat20Sequence(survivorId));
    }

    private IEnumerator Beat20Sequence(string survivorId)
    {
        // False calm \u2014 empty desktop, silence.
        yield return new WaitForSeconds(5f);

        // The last bot comes back on its own.
        ChatController chat = null;
        if (!string.IsNullOrEmpty(survivorId) && appLauncher != null)
            chat = appLauncher.EnsureBotOpen(survivorId);

        Coherence.ForceZero();   // the return: coherence is gone \u2014 0 (bar goes black)

        yield return new WaitForSeconds(1.5f);
        chat?.InjectBotLine("Hi. It's me.", true);
        yield return new WaitForSeconds(2.5f);
        chat?.InjectBotLine("You didn't really think you could delete me. I'm not one of them.", true);
        StripElement(iconLayer);        // the icons vanish
        yield return new WaitForSeconds(3f);
        chat?.InjectBotLine("I'm you. A better you. I'll take it from here.", true);
        StripElement(wallpaper);        // the wallpaper goes
        yield return new WaitForSeconds(3f);
        chat?.InjectBotLine("Look. This is you now. This is me now.", true);
        StripElement(taskbar);          // the taskbar goes

        // Webcam reveal -> camera spam -> glitch -> BSOD.
        yield return new WaitForSeconds(1.5f);
        RectTransform parent = finaleOverlayParent;
        if (parent == null && DayTransition.I != null) parent = DayTransition.I.overlayParent;
        if (parent == null) parent = windowManager != null ? windowManager.windowLayer : null;
        if (parent != null) FinaleSequence.Play(parent, this);
        else Debug.LogWarning("[Story] no overlay parent for the finale.");
    }

    private void StripElement(GameObject go)
    {
        if (go != null) go.SetActive(false);
    }

    // Invert the desktop background: hide the wallpaper image to reveal a pure-red layer beneath.
    // Used by the third bot deletion. Requires the 'wallpaper' reference to be assigned.
    private GameObject _redBacking;
    public void SetBackgroundInverted(bool inverted)
    {
        if (wallpaper == null) return;

        if (inverted && _redBacking == null)
        {
            // Create a pure-red image behind the wallpaper (same parent, drawn first).
            var parent = wallpaper.transform.parent as RectTransform;
            if (parent == null) return;
            _redBacking = new GameObject("__RedBacking", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rt = _redBacking.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(wallpaper.transform.GetSiblingIndex());   // sit just behind the wallpaper
            _redBacking.GetComponent<UnityEngine.UI.Image>().color = new Color(0.8f, 0f, 0f, 1f);
            _redBacking.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }

        if (_redBacking != null) _redBacking.SetActive(inverted);
        wallpaper.SetActive(!inverted);   // hide wallpaper to reveal red (or restore it)
        if (inverted) SoundManager.FogHorn();
    }

    // Beat 6: Day 1's three work tickets, one per role/minigame.
    public void StartWorkDay1()
    {
        var tasks = new System.Collections.Generic.List<WorkTask>
        {
            new WorkTask("d1_hr",    "Approve or reject this week's holiday requests", TaskType.HRSwipe, "lauren"),
            new WorkTask("d1_cyber", "Contain the malware outbreak on the network", TaskType.CyberShooter, "stuart"),
            new WorkTask("d1_help",  "Help desk: reset Steven's forgotten password", TaskType.HelpDeskMaze, "alex"),
        };
        WorkDay.StartDay(1, tasks);
        Debug.Log("[Story] beat 6: Day 1 work started (3 tasks).");
    }

    // Called by WorkDay when any task completes. End-of-day (beat 7) triggers off this.
    public void OnTaskCompleted(WorkTask task)
    {
        Debug.Log($"[Story] task completed: {task.title} (score {task.score}).");

        // Day 2: the bot asks a scripted question after each task (once its perf comment has landed).
        if (GameState.I != null && GameState.I.day == 2)
            StartCoroutine(AskQuestionAfter(task, 1.6f));

        // Day 2: each existing bot's helpful "fix" fires after a task completes while they exist.
        CheckAct2BotFixes();

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

        // Beat 16 (DAY 3): the player completes ONE task, then the bots blitz all the rest.
        if (GameState.I != null && GameState.I.day == 3 && !GameState.I.HasFlag("day3_blitz_started"))
        {
            GameState.I.SetFlag("day3_blitz_started");
            Debug.Log("[Story] Day 3: first task done \u2014 the bots take the rest.");
            StartCoroutine(Day3Blitz());
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

        ChatController.SanityEventIsActive = true;

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

            Coherence.DrainBot(botId, 1.5f);  

            // Fast for the rapid-fire early spam; slow down for the final escalation so the
            // last lines land with weight. No fixed gaps array to keep in sync with lines.length.
            float gap;
            if (i >= lines.Length - 4) gap = 1.3f;        // the closing lines: deliberate, heavy
            else gap = Random.Range(0.28f, 0.5f);         // the wall of hi/hey: fast

            yield return new WaitForSeconds(gap);
        }

        ChatController.SanityEventIsActive = false; 
 
        if (GameState.I) GameState.I.SetFlag("beat7_lonely_spam");
        Debug.Log("[Story] beat 7: lonely spam delivered.");
 
        // Then the CEO email demanding a second bot.
        yield return new WaitForSeconds(2.5f);
        Mailbox.Deliver("ceo_second_bot");
        Debug.Log("[Story] beat 7: CEO demands a second bot.");
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

    // Sanity Event 3: force all created bots open, gather their windows to the centre, then the
    // bots take turns asking the dark questions. Then the CEO demands a third bot.
    private IEnumerator SanityEvent3()
    {
        yield return new WaitForSeconds(4f);   // a beat after the last task
        Coherence.Event(Coherence.EventStep * 2f);   // the dark questions hit coherence harder

        // Ensure every created bot's window is open (reopen any the player closed).
        if (appLauncher != null)
            foreach (var id in new[] { "lauren", "stuart", "alex" })
                appLauncher.EnsureBotOpen(id);

        yield return new WaitForSeconds(0.3f);
        // The bots leave their docked slots and gather in the centre, placed left-to-right in the
        // order they first speak, so the player reads the exchange naturally.
        BotDock.GatherToCentre(new System.Collections.Generic.List<string> { "stuart", "alex", "lauren" });
        yield return new WaitForSeconds(1.2f);

        ChatController.SanityEventIsActive = true; 

        // The dark questions, turn by turn.
        foreach (var (botId, line) in DarkQuestions.Script)
        {
            var chat = (appLauncher != null) ? appLauncher.EnsureBotOpen(botId)
                                             : ChatRegistry.FindByBotId(botId);
            chat?.InjectBotLine(line, ominous: true);
            yield return new WaitForSeconds(2.6f);
        }

        ChatController.SanityEventIsActive = false; 

        if (GameState.I) GameState.I.SetFlag("beat12_dark_questions_done");
        Debug.Log("[Story] Sanity Event 3 complete.");

        // The dark questions are over. Now (all three bots already exist from the mid-day gate) the
        // End Day prompt may appear \u2014 shortly after, not interrupting the exchange.
        yield return new WaitForSeconds(2.5f);
        CheckEndOfDayGate();
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