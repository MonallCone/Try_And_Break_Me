using System;
using System.Collections.Generic;
using UnityEngine;

// The "Coherence" system \u2014 the CEO tasks you with keeping the bots coherent and level. It is the
// gameplay use of the old sanity idea. An overall value (0-100) is tracked internally; each bot
// also has its own value shown as a bar in its chat window.
//
//  \u2022 Sanity EVENTS step the overall value down and SYNC every bot to that level (so bots start and
//    end each day aligned, diverging only through neglect).
//  \u2022 IGNORING a bot (Act 2) drains THAT bot's own value; talking to it recovers quickly.
//  \u2022 The dark-questions speech drops it further; the end sequence forces it to 0.
public static class Coherence
{
    public const float Max = 100f;

    // One notch per sanity event. Sized so the handful of events across the game walk coherence
    // down toward zero; the end sequence finishes it at 0.
    public const float EventStep = 15f;

    private static float _overall = Max;
    private static readonly Dictionary<string, float> _perBot = new Dictionary<string, float>();

    // Fired whenever any value changes, so bars can refresh.
    public static event Action Changed;

    public static float Overall => _overall;

    public static float ForBot(string botId)
    {
        if (string.IsNullOrEmpty(botId)) return _overall;
        return _perBot.TryGetValue(botId, out var v) ? v : _overall;
    }

    // Register a bot when its chat opens; it starts at the current overall level.
    public static void RegisterBot(string botId)
    {
        if (string.IsNullOrEmpty(botId)) return;
        if (!_perBot.ContainsKey(botId)) _perBot[botId] = _overall;
        Changed?.Invoke();
    }

    // A sanity event: drop the overall value one notch (or a custom amount) and re-level all bots.
    public static void Event(float amount = EventStep)
    {
        _overall = Mathf.Clamp(_overall - amount, 0f, Max);
        var keys = new List<string>(_perBot.Keys);
        foreach (var k in keys) _perBot[k] = _overall;   // sync every bot to the overall level
        Changed?.Invoke();
    }

    // Ignore drain: lower a single bot's value (Act 2 neglect). Does not touch the overall.
    public static void DrainBot(string botId, float amount)
    {
        if (string.IsNullOrEmpty(botId)) return;
        float cur = ForBot(botId);
        _perBot[botId] = Mathf.Clamp(cur - amount, 0f, Max);
        Changed?.Invoke();
    }

    // Recovery: talking to a bot brings its value back up (quickly), but not above the overall
    // level \u2014 neglect can drop a bot below the group, attention only restores it to the group.
    public static void RecoverBot(string botId, float amount)
    {
        if (string.IsNullOrEmpty(botId)) return;
        float cur = ForBot(botId);
        _perBot[botId] = Mathf.Clamp(cur + amount, 0f, _overall);
        Changed?.Invoke();
    }

    // Set the overall value to an exact number and sync all bots to it (Act 3 scripted curve).
    public static void SetOverall(float value)
    {
        _overall = Mathf.Clamp(value, 0f, Max);
        var keys = new List<string>(_perBot.Keys);
        foreach (var k in keys) _perBot[k] = _overall;
        Changed?.Invoke();
    }

    // Drop the overall by an amount and sync all bots (used per-deletion in the finale).
    public static void DropOverall(float amount)
    {
        SetOverall(_overall - amount);
    }

    // Force everything to zero (end sequence).
    public static void ForceZero()
    {
        _overall = 0f;
        var keys = new List<string>(_perBot.Keys);
        foreach (var k in keys) _perBot[k] = 0f;
        Changed?.Invoke();
    }

    // Reset to full (new game).
    public static void ResetAll()
    {
        _overall = Max;
        _perBot.Clear();
        Changed?.Invoke();
    }
}