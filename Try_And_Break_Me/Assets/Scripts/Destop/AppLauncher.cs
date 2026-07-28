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
    public Vector2 chatSize = new Vector2(420f, 560f);

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

    public void OpenChat(CharacterSheet sheet, EmotionProfile emotion, Sprite icon)
    {
        var win = windowManager.OpenWindow(sheet.Name, chatSize, icon);

        // Dock the bot window to the right edge (stacks with other bots; walls the player in).
        BotDock.Init(windowManager.windowLayer);
        BotDock.Dock(win);
        win.AddDockButton();
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
}
