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
    private static float _slotWidth = 500f;
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

    // Undock all bot windows and cluster them side by side near the centre of the screen. Used by
    // Sanity Event 3 \u2014 the bots leaving their neat docked slots to gather and face the player.
    // If an order (list of bot ids) is given, bots are placed left-to-right in that order so the
    // player reads their turn-taking naturally; unlisted bots follow in dock order.
    public static List<DraggableWindow> GatherToCentre(List<string> leftToRightBotIds = null)
    {
        var bots = new List<DraggableWindow>();
        for (int i = 0; i < MaxSlots; i++)
            if (_slots[i] != null) bots.Add(_slots[i]);

        // free the slots (they're no longer docked)
        for (int i = 0; i < MaxSlots; i++) _slots[i] = null;

        if (_windowLayer == null || bots.Count == 0) return bots;

        // Reorder to the requested left-to-right sequence when provided.
        if (leftToRightBotIds != null)
        {
            var ordered = new List<DraggableWindow>();
            foreach (var id in leftToRightBotIds)
            {
                var match = bots.Find(b => BotIdOf(b) == id);
                if (match != null && !ordered.Contains(match)) ordered.Add(match);
            }
            // append any bots not named in the order
            foreach (var b in bots) if (!ordered.Contains(b)) ordered.Add(b);
            bots = ordered;
        }

        Rect area = _windowLayer.rect;
        int n = bots.Count;
        float w = Mathf.Min(320f, (area.width - 40f) / n);
        float h = Mathf.Min(360f, area.height - 80f);
        float totalW = w * n + 12f * (n - 1);
        float startX = -totalW * 0.5f + w * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var win = bots[i];
            win.RectTransform.sizeDelta = new Vector2(w, h);
            float x = startX + i * (w + 12f);
            win.RectTransform.anchoredPosition = new Vector2(x, 0f);
            win.transform.SetAsLastSibling();
        }
        return bots;
    }

    // Resolve a window's bot id via the chat registry (matches the window's title to a bot).
    private static string BotIdOf(DraggableWindow win)
    {
        foreach (var c in ChatRegistry.All)
            if (c.BotName == win.Title) return c.BotId;
        return null;
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