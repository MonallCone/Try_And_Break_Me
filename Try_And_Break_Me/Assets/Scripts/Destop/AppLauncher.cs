using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The glue between a DesktopIcon and the WindowManager. Point the icon's onOpen event at
// OpenAIFriend(). For now it opens a simple placeholder window so you can test drag/focus/close.
// In Phase 2-chunk-3 / later, this is where the creator screen or a chat window gets placed
// inside the new window's ContentArea instead of the placeholder.
public class AppLauncher : MonoBehaviour
{
    public WindowManager windowManager;

    [Tooltip("Default size for the AI Virtual Friend window.")]
    public Vector2 windowSize = new Vector2(420f, 520f);

    private int _counter = 0;

    public void OpenAIFriend()
    {
        _counter++;
        var win = windowManager.OpenWindow($"AI Virtual Friend {_counter}", windowSize);
        AddPlaceholderContent(win);
    }

    // Temporary: a label so the window isn't empty. Replace with real content later.
    private void AddPlaceholderContent(DraggableWindow win)
    {
        var go = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(win.ContentArea, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f, 12f); rt.offsetMax = new Vector2(-12f, -12f);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = "AI Virtual Friend\n\n(placeholder window)\n\nDrag my title bar. Click to focus.\nClose with the X. Open several and\nwatch them stack and focus.";
        t.fontSize = 15f;
        t.color = new Color(0.85f, 0.85f, 0.9f, 1f);
        t.alignment = TextAlignmentOptions.TopLeft;
    }
}
