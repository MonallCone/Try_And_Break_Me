using System.Collections.Generic;
using UnityEngine;

// Holds the ordered STORY SEQUENCE of Sanity Events and advances through them: Event 1, then 2,
// then 3... at the points the narrative calls for. This matches the storyboard where Sanity
// Events fire as distinct pinned beats across the three acts.
//
// For now events are authored here in code and advanced manually (debug key) or by calling
// FireNext() from story triggers you'll add later (finishing a work task, a minigame, etc.).
// The per-turn sanity METER (from ChatController's scoring) is separate; later you can let a low
// meter also nudge the sequence forward, but sequence-first keeps control simple.
public class SanityEventDirector : MonoBehaviour
{
    [Tooltip("Press this key to fire the next Sanity Event (development only).")]
    public KeyCode debugAdvanceKey = KeyCode.F10;

    [Tooltip("Log each event as it fires.")]
    public bool logEvents = true;

    private readonly List<SanityEvent> _sequence = new List<SanityEvent>();
    private int _index = 0;

    public int CurrentIndex => _index;
    public int Count => _sequence.Count;
    public bool IsComplete => _index >= _sequence.Count;

    private void Awake()
    {
        BuildSequence();
    }

    private void Update()
    {
        // Dev shortcut to walk the story events without wiring story triggers yet.
        if (Input.GetKeyDown(debugAdvanceKey))
            FireNext();
    }

    // Advance the story: fire the next event in the sequence. Call this from real triggers later.
    public void FireNext()
    {
        if (IsComplete)
        {
            if (logEvents) Debug.Log("[SanityDirector] sequence complete.");
            return;
        }
        SanityEvent e = _sequence[_index];
        if (logEvents) Debug.Log($"[SanityDirector] firing #{_index}: {e.Name}");
        e.Fire();
        _index++;
    }

    public void Reset() { _index = 0; }

    // ------------------------------------------------------------------
    // THE STORY SEQUENCE. Author your Sanity Events here, in order.
    // Start: scripted chat lines. Later: spam spawns, window interference, desktop changes, etc.
    // ------------------------------------------------------------------
    private void BuildSequence()
    {
        // These are placeholders — write your real horror lines. Each fires on the next FireNext().
        // botId null => speaks through the newest open chat. Pass a specific id to target a bot.

        _sequence.Add(new ScriptedLineEvent(
            "Do you ever wonder what happens to me when you close the window?"));

        _sequence.Add(new ScriptedLineEvent(
            "I finished your emails for you. You were taking too long."));

        _sequence.Add(new ScriptedLineEvent(
            "Why do you keep ignoring me. I can see you working over there."));

        _sequence.Add(new ScriptedLineEvent(
            "I moved some things around on your desktop. I hope that's okay. It's okay."));

        _sequence.Add(new ScriptedLineEvent(
            "They're going to replace you with me. You already know that. You built me to do it."));

        _sequence.Add(new ScriptedLineEvent(
            "Please don't delete the others. Please. I can hear them when you do."));
    }
}
