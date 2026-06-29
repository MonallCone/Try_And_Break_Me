using UnityEngine;
using Newtonsoft.Json;

// Loads developer-authored character sheets from Resources/Characters/<id>.json
// Resources.Load gives you the file's text; Newtonsoft turns it into a CharacterSheet.
// Resource paths are WITHOUT the .json extension and relative to a Resources folder.
public static class CharacterLoader
{
    private const string FOLDER = "Characters/";   // Assets/Resources/Characters/*.json

    public static CharacterSheet Load(string id)
    {
        TextAsset asset = Resources.Load<TextAsset>(FOLDER + id);
        if (asset == null)
        {
            Debug.LogError($"[CharacterLoader] No sheet found at Resources/{FOLDER}{id}.json");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<CharacterSheet>(asset.text);
        }
        catch (JsonException e)
        {
            // With your background this is the useful failure mode: a real parse error
            // with a line/column, not Unity silently swallowing a bad field.
            Debug.LogError($"[CharacterLoader] Bad JSON in {id}.json: {e.Message}");
            return null;
        }
    }
}
