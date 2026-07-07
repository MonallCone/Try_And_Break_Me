using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// A desktop shortcut icon. Double-click opens something (wired via the onOpen event in the
// Inspector, or in code). This is your "AI Virtual Friend" shortcut that launches the creator.
public class DesktopIcon : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("What happens when the icon is double-clicked.")]
    public UnityEvent onOpen;

    [Tooltip("Max seconds between two clicks to count as a double-click.")]
    public float doubleClickTime = 0.35f;

    private float _lastClickTime = -1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.unscaledTime;
        if (now - _lastClickTime <= doubleClickTime)
        {
            _lastClickTime = -1f;      // consume, so a triple-click isn't two doubles
            onOpen?.Invoke();
        }
        else
        {
            _lastClickTime = now;
        }
    }
}
