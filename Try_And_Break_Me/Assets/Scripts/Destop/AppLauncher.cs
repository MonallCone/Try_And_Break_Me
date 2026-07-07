using UnityEngine;

// Glue between the AI Virtual Friend icon and the window system.
// Point the icon's onOpen event at OpenCreator().
//
// Flow: icon -> creator window -> Create -> real chat window running the Phase 3 loop.
public class AppLauncher : MonoBehaviour
{
    public WindowManager windowManager;

    [Tooltip("Size of the creator window.")]
    public Vector2 creatorSize = new Vector2(460f, 560f);

    [Tooltip("Size of a chat window.")]
    public Vector2 chatSize = new Vector2(420f, 560f);

    [Header("Relay")]
    public string baseUrl = "http://localhost:8000";

    [Header("Sanity")]
    [Tooltip("Starting sanity settings applied to each new bot's meter. In the hive (Phase 5) " +
             "this becomes ONE shared meter; for now each chat gets its own copy of these values.")]
    public SanityModel sanityTemplate = new SanityModel();

    // Providers are created once and reused by every chat window.
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
        creator.Build(win.ContentArea);
        creator.OnCreate += (sheet, emotion) =>
        {
            windowManager.CloseWindow(win);
            OpenChat(sheet, emotion);
        };
    }

    public void OpenChat(CharacterSheet sheet, EmotionProfile emotion)
    {
        var win = windowManager.OpenWindow(sheet.Name, chatSize);

        // Each bot gets its own sanity meter for now, seeded from the template.
        // Phase 5 swaps this for a single shared meter passed to all chats.
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

        var chat = new ChatController(sheet, emotion, sanity, _provider, _director);
        chat.Build(win.ContentArea);
    }
}
