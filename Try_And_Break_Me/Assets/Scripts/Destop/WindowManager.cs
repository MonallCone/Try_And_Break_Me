using System.Collections.Generic;
using UnityEngine;

// Owns the desktop's window layer. Spawns DraggableWindows in code (so you don't hand-build
// window prefabs), tracks them, and handles focus / z-ordering: the focused window sits on top.
// This is the core of the fake-OS window system that the hive and the finale both rely on.
public class WindowManager : MonoBehaviour
{
    [Tooltip("The RectTransform under which windows live. Usually a full-screen panel on the Canvas.")]
    public RectTransform windowLayer;

    [Tooltip("Optional taskbar to notify when windows open/close.")]
    public Taskbar taskbar;

    private readonly List<DraggableWindow> _windows = new List<DraggableWindow>();
    private DraggableWindow _focused;

    private void Awake()
    {
        // Give the taskbar a reference back to us so its window buttons can focus windows.
        if (taskbar != null) taskbar.Bind(this);
    }

    public IReadOnlyList<DraggableWindow> Windows => _windows;

    // Create a new window with a title and a given pixel size, positioned with a cascade offset.
    public DraggableWindow OpenWindow(string title, Vector2 size, Sprite icon = null)
    {
        var win = DraggableWindow.Create(windowLayer, title, size, this, icon);

        // Cascade so new windows don't stack exactly on top of each other.
        float offset = _windows.Count * 30f;
        win.RectTransform.anchoredPosition = new Vector2(-size.x * 0.25f + offset, size.y * 0.15f - offset);

        _windows.Add(win);
        Focus(win);
        taskbar?.OnWindowOpened(win);
        return win;
    }

    public void CloseWindow(DraggableWindow win)
    {
        if (!_windows.Contains(win)) return;
        _windows.Remove(win);
        taskbar?.OnWindowClosed(win);
        if (_focused == win) _focused = null;
        Destroy(win.gameObject);
    }

    // Bring a window to the front by making it the last sibling (uGUI draws later siblings on top).
    public void Focus(DraggableWindow win)
    {
        if (win == null) return;
        _focused = win;
        win.transform.SetAsLastSibling();
        foreach (var w in _windows) w.SetFocused(w == win);
        taskbar?.OnFocusChanged(win);
    }

    public DraggableWindow Focused => _focused;
}
