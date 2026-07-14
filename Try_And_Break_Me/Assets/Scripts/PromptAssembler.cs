using System.Collections.Generic;
using System.Text;

public static class PromptAssembler
{
    // Band a 1-10 value into low / mid / high. Mid means "unremarkable on this axis" and we
    // deliberately say LESS about mid values, so the prompt only spends words on what's
    // distinctive about this bot. (LLMs act on words, not on raw numbers like "mood: 3".)
    private enum Band { Low, Mid, High }

    private static Band ToBand(int v)
    {
        if (v <= 3) return Band.Low;
        if (v >= 8) return Band.High;
        return Band.Mid;
    }

    public static string Assemble(CharacterSheet sheet, EmotionProfile emotion)
    {
        var sb = new StringBuilder();

        // 1) Identity + hard constraints from the authored sheet.
        sb.AppendLine($"You are {sheet.Name}. Stay fully in character at all times.");
        if (!string.IsNullOrWhiteSpace(sheet.Backstory))
            sb.AppendLine($"Background: {sheet.Backstory}");

        if (sheet.Traits != null && sheet.Traits.Count > 0)
            sb.AppendLine($"Your defining traits: {string.Join(", ", sheet.Traits)}.");

        if (sheet.Knows != null && sheet.Knows.Count > 0)
            sb.AppendLine($"Things you know about: {string.Join("; ", sheet.Knows)}.");

        if (sheet.DoesNotKnow != null && sheet.DoesNotKnow.Count > 0)
            sb.AppendLine($"Things you do NOT know and must not claim to know: " +
                          $"{string.Join("; ", sheet.DoesNotKnow)}.");

        // 2) Emotional colour from the player's locked sliders, translated to instructions.
        List<string> tone = TranslateEmotion(emotion);
        if (tone.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Right now, your manner of speaking is shaped by your nature:");
            foreach (string line in tone)
                sb.AppendLine($"- {line}");
        }

        // 3) Global guardrails for the chat format.
        sb.AppendLine();
        sb.AppendLine("Reply only as this character, in first person, in 1-3 short paragraphs. " +
                      "Never break character or mention being an AI.");

        return sb.ToString();
    }

    // The band-to-phrase translation layer. Each axis contributes a concrete tone instruction
    // only when it's distinctive (low or high). Mid (4-7) stays silent by design.
    private static List<string> TranslateEmotion(EmotionProfile e)
    {
        var lines = new List<string>();

        // Sad (1) <-> Happy (10)
        switch (ToBand(e.Mood))
        {
            case Band.Low:  lines.Add("You are sad and downcast; a heavy, joyless weight hangs on your words."); break;
            case Band.High: lines.Add("You are happy and light; warmth and good cheer colour everything you say."); break;
        }
        // Shy (1) <-> Bold (10)
        switch (ToBand(e.Boldness))
        {
            case Band.Low:  lines.Add("You are shy and retiring; you hesitate, defer, and shrink from confrontation."); break;
            case Band.High: lines.Add("You are bold and assertive; you speak plainly and take up space without apology."); break;
        }
        // Cold (1) <-> Friendly (10)
        switch (ToBand(e.Friendliness))
        {
            case Band.Low:  lines.Add("You are cold toward the person you're talking to; distant, curt, giving little away."); break;
            case Band.High: lines.Add("You are friendly toward the person you're talking to; warm, welcoming, glad of their company."); break;
        }
        // Calm (1) <-> Angry (10)  (asymmetric: low = even-tempered, high carries the weight)
        switch (ToBand(e.Anger))
        {
            case Band.Low:  lines.Add("You are even-tempered and calm; little ruffles you."); break;
            case Band.High: lines.Add("You are angry and irritable; your temper is short and shows through your words."); break;
        }
        // Suspicious (1) <-> Trusting (10)
        switch (ToBand(e.Trust))
        {
            case Band.Low:  lines.Add("You are suspicious and guarded; you question motives and withhold until convinced."); break;
            case Band.High: lines.Add("You are trusting and open; you take others at their word and share freely."); break;
        }
        // Serious (1) <-> Playful (10)
        switch (ToBand(e.Playfulness))
        {
            case Band.Low:  lines.Add("You are serious and earnest; you do not joke, and you keep a straight, plain tone."); break;
            case Band.High: lines.Add("You are playful and teasing; you joke, banter, and keep things light."); break;
        }
        // Quiet (1) <-> Talkative (10)
        switch (ToBand(e.Talkativeness))
        {
            case Band.Low:  lines.Add("You are quiet and terse; you answer in as few words as possible."); break;
            case Band.High: lines.Add("You are talkative and expansive; you say more than asked and fill the silence."); break;
        }
        // Insecure (1) <-> Confident (10)
        switch (ToBand(e.Confidence))
        {
            case Band.Low:  lines.Add("You are insecure and self-doubting; you hedge, second-guess, and seek reassurance."); break;
            case Band.High: lines.Add("You are confident and self-assured; you speak with certainty and rarely qualify."); break;
        }

        return lines;
    }
}