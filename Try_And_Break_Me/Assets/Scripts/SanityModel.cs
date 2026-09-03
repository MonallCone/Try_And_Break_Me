using System;
using UnityEngine;

public class SanityModel
{
    [Header("Range")]
    public float max = 100f;
    public float current = 100f;

    [Header("Time decay (the inescapable floor)")]
    [Tooltip("Sanity lost every turn regardless of content. Even a perfect player loses slowly.")]
    public float timeDecayPerTurn = 1.5f;

    [Header("Driver weights (how Director scores 0-3 map to sanity loss)")]
    [Tooltip("Deliberate attacks should hurt more than passive ones. Multiplied by the 0-3 score.")]
    public float rudenessWeight = 2.0f;
    public float offTopicWeight = 1.5f;
    public float contradictionWeight = 3.0f;   // heaviest: the most deliberate, most on-theme

    [Header("Neglect (Phase 5 hive; harmless with one bot)")]
    [Tooltip("Extra drain per turn for each bot left unaddressed. Ignored until multiple bots exist.")]
    public float neglectPerIgnoredBot = 0f;

    // Result of applying one turn, so the UI/debug panel can show the breakdown.
    public struct TurnResult
    {
        public float timeLoss;
        public float rudenessLoss;
        public float offTopicLoss;
        public float contradictionLoss;
        public float totalLoss;
        public float sanityAfter;
    }

    // Apply one player turn's Director scores (each 0-3) plus the flat time decay.
    public TurnResult ApplyTurn(int rudeness, int offTopic, int contradiction, int ignoredBots = 0)
    {
        var r = new TurnResult();
        //r.timeLoss          = timeDecayPerTurn + neglectPerIgnoredBot * Mathf.Max(0, ignoredBots);
        r.rudenessLoss      = rudeness      * rudenessWeight;
        r.offTopicLoss      = offTopic      * offTopicWeight;
        r.contradictionLoss = contradiction * contradictionWeight;

        r.totalLoss = r.timeLoss + r.rudenessLoss + r.offTopicLoss + r.contradictionLoss;

        current = Mathf.Clamp(current - r.totalLoss, 0f, max);
        r.sanityAfter = current;
        return r;
    }

    // 0..1 fraction, handy for bars and for choosing corruption bands later.
    public float Fraction => max > 0f ? current / max : 0f;

    // The band the current value sits in. Phase 4 uses this to pick a corruption profile;
    // Phase 3 just displays it so you can see the intended stages forming.
    public SanityBand Band
    {
        get
        {
            float pct = Fraction * 100f;
            if (pct >= 75f) return SanityBand.Stable;
            if (pct >= 50f) return SanityBand.Slipping;
            if (pct >= 25f) return SanityBand.Deteriorating;
            return SanityBand.Gone;
        }
    }

    public void Reset() => current = max;
}

public enum SanityBand
{
    Stable,          // 100-75  true to personality
    Slipping,        // 74-50   small contradictions, minor drift
    Deteriorating,   // 49-25   confident contradictions, personality inverting, leaked knowledge
    Gone             // 24-0    sustained incoherence
}
