using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// The only class that knows HTTP exists. Implements IDialogueProvider by POSTing to the
// FastAPI relay. If you later add a LocalModelProvider, the rest of the game does not change.
public class RelayDialogueProvider : IDialogueProvider
{
    private readonly string _endpoint;

    public RelayDialogueProvider(string baseUrl = "https://try-and-break-me-python-service.onrender.com")
    {
        _endpoint = baseUrl.TrimEnd('/') + "/generate";
    }

    public async Task<DialogueResult> GenerateAsync(string system, List<ChatMessage> history)
    {
        var payload = new GenerateRequestDTO
        {
            system = system,
            messages = history.ToArray(),
            max_tokens = 400
        };
        string json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(_endpoint, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.certificateHandler = new AcceptAllCertificates();  
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 60;

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();   // non-blocking; UI stays responsive

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"Relay error: {req.error} | {req.downloadHandler.text}");

        var dto = JsonUtility.FromJson<GenerateResponseDTO>(req.downloadHandler.text);
        return new DialogueResult
        {
            Reply = dto.reply,
            InputTokens = dto.input_tokens,
            OutputTokens = dto.output_tokens
        };
    }

    // JsonUtility needs concrete serializable DTOs (it can't do the interface types directly).
    [Serializable]
    private class GenerateRequestDTO
    {
        public string system;
        public ChatMessage[] messages;
        public int max_tokens;
    }

    [Serializable]
    private class GenerateResponseDTO
    {
        public string reply;
        public int input_tokens;
        public int output_tokens;
    }
}
