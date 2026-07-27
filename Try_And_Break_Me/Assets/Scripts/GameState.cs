using System.Collections.Generic;
using UnityEngine;

// The story SPINE. One shared place that remembers who the player is and where they are in the
// narrative. Every later system (email, sanity events, quotas, finale) reads from and advances
// this rather than inventing its own tracking.
//
// Singleton so anything can reach it (GameState.I). Survives scene reloads if you add DontDestroyOnLoad.
public class GameState : MonoBehaviour
{
    public static GameState I { get; private set; }

    [Header("Player")]
    public string playerName = "user";

    [Header("Story position")]
    [Tooltip("1, 2 or 3. Which in-game work day we're on.")]
    public int day = 1;

    [Tooltip("How many bots the player has created so far.")]
    public int botsCreated = 0;

    // Named story flags: which beats have happened. Lets any system ask 'has beat X fired?'
    private readonly HashSet<string> _flags = new HashSet<string>();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        // Uncomment if you ever go multi-scene:
        // DontDestroyOnLoad(gameObject);
    }

    // ---- name ----
    public void SetPlayerName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name)) playerName = name.Trim();
        Debug.Log($"[GameState] player name = {playerName}");
    }

    // ---- flags ----
    public void SetFlag(string flag) { _flags.Add(flag); }
    public bool HasFlag(string flag) => _flags.Contains(flag);

    // ---- convenience ----
    public void NextDay() { day++; Debug.Log($"[GameState] now day {day}"); }
    public void RegisterBotCreated() { botsCreated++; Debug.Log($"[GameState] bots created = {botsCreated}"); }
}
