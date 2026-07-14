using System.Collections.Generic;
using Newtonsoft.Json;

public class CharacterSheet
{
    [JsonProperty("id")]            public string Id = "";
    [JsonProperty("name")]          public string Name = "";
    [JsonProperty("backstory")]     public string Backstory = "";
    [JsonProperty("traits")]        public List<string> Traits = new List<string>();
    [JsonProperty("knows")]         public List<string> Knows = new List<string>();
    [JsonProperty("doesNotKnow")]   public List<string> DoesNotKnow = new List<string>();

    // Optional authored default for the sliders. The player's choices override this at
    // creation; it's only a fallback so a sheet is testable on its own.
    [JsonProperty("emotionBaseline")] public EmotionProfile EmotionBaseline = new EmotionProfile();
}
