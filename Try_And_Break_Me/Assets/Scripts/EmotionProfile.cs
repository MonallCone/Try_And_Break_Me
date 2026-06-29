using Newtonsoft.Json;

// The eight emotional axes. Each value is 1-10. The player sets these at creation and they
// LOCK for the session. Each field is named for its HIGH-end word; a LOW value means the
// OPPOSITE word (low Playfulness = serious, low Anger = even-tempered, low Confidence =
// insecure). PromptAssembler.TranslateEmotion honours that convention.
//
// Slider labels for the builder UI (left = 1, right = 10):
//   Sad <-> Happy        (mood)
//   Shy <-> Bold         (boldness)
//   Cold <-> Friendly    (friendliness)
//   Calm <-> Angry       (anger)
//   Suspicious <-> Trusting (trust)
//   Serious <-> Playful  (playfulness)
//   Quiet <-> Talkative  (talkativeness)
//   Insecure <-> Confident (confidence)
public class EmotionProfile
{
    [JsonProperty("mood")]          public int Mood = 5;
    [JsonProperty("boldness")]      public int Boldness = 5;
    [JsonProperty("friendliness")]  public int Friendliness = 5;
    [JsonProperty("anger")]         public int Anger = 5;
    [JsonProperty("trust")]         public int Trust = 5;
    [JsonProperty("playfulness")]   public int Playfulness = 5;
    [JsonProperty("talkativeness")] public int Talkativeness = 5;
    [JsonProperty("confidence")]    public int Confidence = 5;

    public EmotionProfile Clone()
    {
        return new EmotionProfile
        {
            Mood = Mood,
            Boldness = Boldness,
            Friendliness = Friendliness,
            Anger = Anger,
            Trust = Trust,
            Playfulness = Playfulness,
            Talkativeness = Talkativeness,
            Confidence = Confidence
        };
    }
}
