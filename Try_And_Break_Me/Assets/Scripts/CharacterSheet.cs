using System.Collections.Generic;
using Newtonsoft.Json;

// Plain data class (NOT MonoBehaviour / ScriptableObject) so it maps 1:1 to JSON and stays
// reusable for the relay payloads and the dark-profile swap later. One representation end to end.
//
// This is the lightweight WKB from the plan: a defined character to be in or out of, plus a
// few facts that can be contradicted. The developer authors these as .json files; the player
// only sets the sliders (EmotionProfile, in its own file).
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
