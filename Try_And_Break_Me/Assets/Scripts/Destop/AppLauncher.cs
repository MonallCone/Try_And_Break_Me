using UnityEngine;

// Glue between the AI Virtual Friend icon and the window system.
// Point the icon's onOpen event at OpenCreator().
//
// Flow: icon -> creator (page 1: template+icon+info, page 2: sliders) -> Create -> chat window.
// The chosen icon travels to the chat window and shows on its title bar + taskbar button.
public class AppLauncher : MonoBehaviour
{
    public WindowManager windowManager;

    [Header("Bot icons (one per role)")]
    public Sprite laurenIcon;
    public Sprite stuartIcon;
    public Sprite alexIcon;

    [Header("App icons (shown on taskbar buttons)")]
    public Sprite emailIcon;
    public Sprite companyChatIcon;
    public Sprite tasksIcon;

    [Header("Window sizes")]
    public Vector2 creatorSize = new Vector2(460f, 620f);
    public Vector2 chatSize = new Vector2(500f, 560f);

    [Header("Relay")]
    public string baseUrl = "http://localhost:8000";

    [Header("Sanity")]
    public SanityModel sanityTemplate = new SanityModel();

    private IDialogueProvider _provider;
    private IDirectorProvider _director;

    private void Awake()
    {
        _provider = new RelayDialogueProvider(baseUrl);
        _director = new RelayDirectorProvider(baseUrl);
    }

    public void OpenCreator()
    {
        var win = windowManager.OpenWindow("AI Virtual Friend — Create", creatorSize);

        var creator = new CreatorWindow();
        creator.SetIconMap(new System.Collections.Generic.Dictionary<string, Sprite>
        {
            { "lauren", laurenIcon },
            { "stuart", stuartIcon },
            { "alex",   alexIcon },
        });
        creator.Build(win.ContentArea);
        creator.OnCreate += (sheet, emotion, icon) =>
        {
            windowManager.CloseWindow(win);
            OpenChat(sheet, emotion, icon);
        };
    }

    [Header("Email")]
    public Vector2 emailSize = new Vector2(560f, 420f);

    public void OpenEmail()
    {
        var win = windowManager.OpenWindow("Email", emailSize, emailIcon);
        var app = new EmailApp();
        app.Build(win.ContentArea);   // reads from the persistent Mailbox
    }

    [Header("Company Chat")]
    public Vector2 companyChatSize = new Vector2(480f, 460f);

    public void OpenCompanyChat()
    {
        var win = windowManager.OpenWindow("Company Chat", companyChatSize, companyChatIcon);
        // CompanyChatApp is a MonoBehaviour (needs Update for the timer), so attach it to the
        // window content object and let it build its own UI.
        var app = win.ContentArea.gameObject.AddComponent<CompanyChatApp>();
        app.Build(win.ContentArea);
    }

    [Header("Tasks")]
    public Vector2 tasksSize = new Vector2(420f, 480f);

    public void OpenTasks()
    {
        var win = windowManager.OpenWindow("Tasks", tasksSize, tasksIcon);
        var app = new TasksApp();
        app.Build(win.ContentArea, LaunchMinigame);
    }

    // A small pop-up prompting the player to end the day. Calls onEndDay when clicked.
    public void ShowEndDayPrompt(System.Action onEndDay)
    {
        var win = windowManager.OpenWindow("End of Day", new Vector2(360f, 180f));
        var root = win.ContentArea;
        root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.93f, 0.93f, 0.96f);
        var vlg = root.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16); vlg.spacing = 12f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var txtGo = new GameObject("T", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        txtGo.GetComponent<RectTransform>().SetParent(root, false);
        var t = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
        t.text = "You've done everything for today.\nReady to end the day?";
        t.fontSize = 16f; t.color = Color.black; t.alignment = TMPro.TextAlignmentOptions.Center;
        txtGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 60f;

        var btnGo = new GameObject("EndDay", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        btnGo.GetComponent<RectTransform>().SetParent(root, false);
        btnGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.3f, 0.5f);
        btnGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 44f;
        var blGo = new GameObject("L", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        var blRt = blGo.GetComponent<RectTransform>();
        blRt.SetParent(btnGo.transform, false);
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one; blRt.offsetMin = Vector2.zero; blRt.offsetMax = Vector2.zero;
        var bl = blGo.GetComponent<TMPro.TextMeshProUGUI>();
        bl.text = "End the day"; bl.fontSize = 16f; bl.color = Color.white;
        bl.alignment = TMPro.TextAlignmentOptions.Center;

        btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            windowManager.CloseWindow(win);
            onEndDay?.Invoke();
        });
    }

    // Opens the right minigame window for a task. Unimplemented games use a placeholder that
    // completes with a random score, so the flow is testable before the real minigames exist.
    public void LaunchMinigame(WorkTask task)
    {
        // Tasks can be locked by the story (e.g. Day 2 mid-day: must build the 3rd bot first).
        if (GameState.I != null && GameState.I.HasFlag("tasks_locked"))
        {
            ShowBlockedMessage();
            return;
        }

        switch (task.type)
        {
            case TaskType.HRSwipe:      HRSwipeGame.Launch(windowManager, task); break;
            case TaskType.HelpDeskMaze: HelpDeskMazeGame.Launch(windowManager, task); break;
            case TaskType.CyberShooter: CyberShooterGame.Launch(windowManager, task); break;
            default:
                OpenPlaceholderTask(task);
                break;
        }
    }

    // A fake "Blocked by Administrator" system message shown when tasks are locked.
    public void ShowBlockedMessage()
    {
        var win = windowManager.OpenWindow("System", new Vector2(380f, 200f));
        var root = win.ContentArea;
        root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.95f, 0.95f, 0.97f);
        var vlg = root.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16); vlg.spacing = 10f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        titleGo.GetComponent<RectTransform>().SetParent(root, false);
        var tt = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
        tt.text = "\u26D4 Blocked by Administrator"; tt.fontSize = 16f; tt.fontStyle = TMPro.FontStyles.Bold;
        tt.color = new Color(0.7f, 0.15f, 0.15f); tt.alignment = TMPro.TextAlignmentOptions.Center;
        titleGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 26f;

        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        bodyGo.GetComponent<RectTransform>().SetParent(root, false);
        var bt = bodyGo.GetComponent<TMPro.TextMeshProUGUI>();
        bt.text = "User Message:\n\"For maximum training, please install your third bot before continuing your work.\"\n\u2014 CEO Steven";
        bt.fontSize = 14f; bt.color = Color.black; bt.alignment = TMPro.TextAlignmentOptions.Center;
        bt.textWrappingMode = TMPro.TextWrappingModes.Normal;
        bodyGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 90f;

        var btnGo = new GameObject("OK", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        btnGo.GetComponent<RectTransform>().SetParent(root, false);
        btnGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.4f, 0.4f, 0.45f);
        btnGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 34f;
        var blGo = new GameObject("L", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        var blRt = blGo.GetComponent<RectTransform>();
        blRt.SetParent(btnGo.transform, false);
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one; blRt.offsetMin = Vector2.zero; blRt.offsetMax = Vector2.zero;
        var bl = blGo.GetComponent<TMPro.TextMeshProUGUI>();
        bl.text = "OK"; bl.fontSize = 14f; bl.color = Color.white; bl.alignment = TMPro.TextAlignmentOptions.Center;
        btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => windowManager.CloseWindow(win));
    }

    private void OpenPlaceholderTask(WorkTask task)
    {
        var win = windowManager.OpenWindow(task.title, new Vector2(360f, 200f));
        var root = win.ContentArea;
        root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.93f, 0.93f, 0.95f);
        var vlg = root.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16); vlg.spacing = 12f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var txtGo = new GameObject("T", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        txtGo.GetComponent<RectTransform>().SetParent(root, false);
        var t = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
        t.text = $"[Placeholder for: {task.title}]\nThe real minigame slots in here.";
        t.fontSize = 14f; t.color = Color.black; t.alignment = TMPro.TextAlignmentOptions.Center;
        txtGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 60f;

        var btnGo = new GameObject("Complete", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        btnGo.GetComponent<RectTransform>().SetParent(root, false);
        btnGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.25f, 0.45f, 0.3f);
        btnGo.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = 40f;
        var blGo = new GameObject("L", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        var blRt = blGo.GetComponent<RectTransform>();
        blRt.SetParent(btnGo.transform, false);
        blRt.anchorMin = Vector2.zero; blRt.anchorMax = Vector2.one; blRt.offsetMin = Vector2.zero; blRt.offsetMax = Vector2.zero;
        var bl = blGo.GetComponent<TMPro.TextMeshProUGUI>();
        bl.text = "Complete (random score)"; bl.fontSize = 13f; bl.color = Color.white;
        bl.alignment = TMPro.TextAlignmentOptions.Center;

        btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            WorkDay.CompleteTask(task, Random.Range(30, 101));
            windowManager.CloseWindow(win);
        });
    }

    public void OpenChat(CharacterSheet sheet, EmotionProfile emotion, Sprite icon)
    {
        // Remember this bot's config so the story can reopen its window later (Sanity Event 1).
        _botConfigs[sheet.Id] = (sheet, emotion, icon);

        var win = windowManager.OpenWindow(sheet.Name, chatSize, icon);

        // Dock the bot window to the right edge (stacks with other bots; walls the player in).
        BotDock.Init(windowManager.windowLayer);
        BotDock.Dock(win);
        win.AddDockButton();
        win.SetCloseEnabled(false);   // bot windows can't be closed until the finale (delete)
        // Release the slot when this bot window closes.
        var relay = win.gameObject.AddComponent<DestroyRelay>();
        relay.onDestroy = () => BotDock.Release(win);

        var sanity = new SanityModel
        {
            max = sanityTemplate.max,
            current = sanityTemplate.max,
            timeDecayPerTurn = sanityTemplate.timeDecayPerTurn,
            rudenessWeight = sanityTemplate.rudenessWeight,
            offTopicWeight = sanityTemplate.offTopicWeight,
            contradictionWeight = sanityTemplate.contradictionWeight,
            neglectPerIgnoredBot = sanityTemplate.neglectPerIgnoredBot
        };

        var chat = new ChatController(sheet, emotion, sanity, _provider, _director, icon);
        chat.Build(win.ContentArea);

        // Tell the story a bot was created (drives beat 7's lonely-spam, later demands, etc.).
        if (GameState.I) GameState.I.RegisterBotCreated();
        if (StoryDirector.I) StoryDirector.I.OnBotCreated(sheet);
    }

    // Remembered bot configs, so the story can force a bot's window back open.
    private readonly System.Collections.Generic.Dictionary<string, (CharacterSheet sheet, EmotionProfile emotion, Sprite icon)> _botConfigs
        = new System.Collections.Generic.Dictionary<string, (CharacterSheet, EmotionProfile, Sprite)>();

    // Ensure a given bot's chat window is open (reopen it if the player closed it). Returns the
    // open ChatController, or null if we have no record of that bot.
    public ChatController EnsureBotOpen(string botId)
    {
        var existing = ChatRegistry.FindByBotId(botId);
        if (existing != null) return existing;
        if (_botConfigs.TryGetValue(botId, out var cfg))
        {
            OpenChat(cfg.sheet, cfg.emotion, cfg.icon);   // reopens + re-registers
            return ChatRegistry.FindByBotId(botId);
        }
        return null;
    }

    // The id of the first bot the player created (for Sanity Event 1's spam target).
    public string FirstBotId { get; private set; }
    public void NoteFirstBot(string id) { if (string.IsNullOrEmpty(FirstBotId)) FirstBotId = id; }
}