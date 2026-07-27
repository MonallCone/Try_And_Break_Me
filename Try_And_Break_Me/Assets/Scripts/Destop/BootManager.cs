using UnityEngine;
using TMPro;

// Handles the fake-OS boot flow: a login screen (enter a name) that transitions to the desktop.
// The name is captured but unimportant for now. Wire the two screen GameObjects and the input.
public class BootManager : MonoBehaviour
{
    [Header("Screens (full-screen panels)")]
    public GameObject loginScreen;
    public GameObject desktopScreen;

    [Header("Login")]
    public TMP_InputField nameField;

    public string UserName { get; private set; } = "user";

    private void Start()
    {
        // Start at login.
        if (loginScreen != null) loginScreen.SetActive(true);
        if (desktopScreen != null) desktopScreen.SetActive(false);
    }

    // Hook this to a "Log In" button's onClick.
    public void OnLogin()
    {
        if (nameField != null && !string.IsNullOrWhiteSpace(nameField.text))
            UserName = nameField.text.Trim();

        // Store the name in the story spine so the whole game can use it (emails, finale...).
        if (GameState.I != null) GameState.I.SetPlayerName(UserName);

        if (loginScreen != null) loginScreen.SetActive(false);
        if (desktopScreen != null) desktopScreen.SetActive(true);

        // Kick off the story sequence (beat 3 onward: emails arrive, etc.).
        if (StoryDirector.I != null) StoryDirector.I.Begin();
    }
}
