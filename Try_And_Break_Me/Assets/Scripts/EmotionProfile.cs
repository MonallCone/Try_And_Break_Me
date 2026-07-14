using Newtonsoft.Json;

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
