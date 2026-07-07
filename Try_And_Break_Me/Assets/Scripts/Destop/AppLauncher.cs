using UnityEngine;

// Glue between the AI Virtual Friend icon and the window system.
// Point the icon's onOpen event at OpenCreator().
//
// Flow: icon -> creator (page 1: template+icon+info, page 2: sliders) -> Create -> chat window.
// The chosen icon travels to the chat window and shows on its title bar + taskbar button.
public class AppLauncher : MonoBehaviour
{
    public WindowManager windowManager;

    [Header("Character icons (assign sprites here)")]
    [Tooltip("The palette of icons the player can choose from in the creator. Drop your Kenney/etc sprites here.")]
    public Sprite[] characterIcons;

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
        creator.SetIcons(characterIcons);
        creator.Build(win.ContentArea);
        creator.OnCreate += (sheet, emotion, icon) =>
        {
            windowManager.CloseWindow(win);
            OpenChat(sheet, emotion, icon);
        };
    }

    public void OpenChat(CharacterSheet sheet, EmotionProfile emotion, Sprite icon)
    {
        var win = windowManager.OpenWindow(sheet.Name, chatSize, icon);

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
