using System.Collections.Generic;
using UnityEngine;

// Owns the right-edge dock: up to 3 stacked slots where bot windows snap. Bots are draggable but
// can re-dock via a button. As more bots are created they stack down the right side, walling the
// player in. Static so the launcher and windows can reach it without wiring.
public static class BotDock
{
    public const int MaxSlots = 3;

    // Which window occupies each slot (null = free).
    private static readonly DraggableWindow[] _slots = new DraggableWindow[MaxSlots];
    private static RectTransform _windowLayer;

    // Dock geometry, computed from the window layer size.
    private static float _slotWidth = 360f;
    private static float _taskbarReserve = 44f;   // leave room at the bottom for the taskbar

    public static void Init(RectTransform windowLayer)
    {
        _windowLayer = windowLayer;
    }

    public static bool HasFreeSlot()
    {
        for (int i = 0; i < MaxSlots; i++) if (_slots[i] == null) return true;
        return false;
    }

    // Place a window into the first free slot. Returns the slot index, or -1 if full.
    public static int Dock(DraggableWindow win)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = win;
                PositionInSlot(win, i);
                return i;
            }
        }
        return -1;
    }

    // Re-dock a window that was dragged away, back into its slot (or first free one).
    public static void Redock(DraggableWindow win)
    {
        // already in a slot? re-snap it.
        for (int i = 0; i < MaxSlots; i++)
            if (_slots[i] == win) { PositionInSlot(win, i); return; }
        // otherwise take a free slot
        Dock(win);
    }

    public static void Release(DraggableWindow win)
    {
        for (int i = 0; i < MaxSlots; i++)
            if (_slots[i] == win) _slots[i] = null;
    }

    private static void PositionInSlot(DraggableWindow win, int slot)
    {
        if (_windowLayer == null) return;
        Rect area = _windowLayer.rect;
        float usableH = area.height - _taskbarReserve;
        float slotH = usableH / MaxSlots;

        // Size the window to the slot.
        win.RectTransform.sizeDelta = new Vector2(_slotWidth, slotH - 6f);

        // Position: right edge, stacked from the top down. Window pivot is centre (0.5,0.5),
        // and the layer is centre-anchored, so compute anchoredPosition from centre.
        float x = area.width * 0.5f - _slotWidth * 0.5f - 4f;          // hug right edge
        float topY = area.height * 0.5f - _taskbarReserve * 0f;         // top of usable area
        float slotCentreFromTop = slotH * (slot + 0.5f);
        float y = (area.height * 0.5f) - slotCentreFromTop;

        win.RectTransform.anchoredPosition = new Vector2(x, y);
        win.transform.SetAsLastSibling();
    }
}
