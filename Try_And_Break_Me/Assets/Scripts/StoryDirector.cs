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

    [Tooltip("The AI Virtual Friend desktop icon. Starts HIDDEN; the install reveals it (beat 4).")]
    public GameObject aiFriendIcon;

    private bool _started;
    private bool _installFired;

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
        // Beat 7 hook will go here: after the first work day, the first bot starts spamming.
    }
}
