using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Calls the relay's /score endpoint. Same pattern as RelayDialogueProvider: the only place
// that knows HTTP exists for scoring. Swappable for a mock or a different backend later.
public class RelayDirectorProvider : IDirectorProvider
{
    private readonly string _endpoint;

    public RelayDirectorProvider(string baseUrl = "http://localhost:8000")
    {
        _endpoint = baseUrl.TrimEnd('/') + "/score";
    }

    public async Task<DirectorScore> ScoreAsync(DirectorContext ctx)
    {
        var payload = new ScoreRequestDTO
        {
            bot_name = ctx.BotName,
            bot_traits = ctx.BotTraits,
            bot_knows = ctx.BotKnows,
            bot_does_not_know = ctx.BotDoesNotKnow,
            player_message = ctx.PlayerMessage,
            recent_context = ctx.RecentContext
        };
        string json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(_endpoint, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"Score relay error: {req.error} | {req.downloadHandler.text}");

        var dto = JsonUtility.FromJson<ScoreResponseDTO>(req.downloadHandler.text);
        return new DirectorScore
        {
            Rudeness = dto.rudeness,
            OffTopic = dto.off_topic,
            Contradiction = dto.contradiction,
            Reasoning = dto.reasoning,
            InputTokens = dto.input_tokens,
            OutputTokens = dto.output_tokens
        };
    }

    [Serializable]
    private class ScoreRequestDTO
    {
        public string bot_name;
        public string bot_traits;
        public string bot_knows;
        public string bot_does_not_know;
        public string player_message;
        public string recent_context;
    }

    [Serializable]
    private class ScoreResponseDTO
    {
        public int rudeness;
        public int off_topic;
        public int contradiction;
        public string reasoning;
        public int input_tokens;
        public int output_tokens;
    }
}
