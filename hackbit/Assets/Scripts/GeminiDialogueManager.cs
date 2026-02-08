using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

public class GeminiDialogueManager : MonoBehaviour
{
    public TMP_Text dialogueText;
    public TMP_Text buttonText;

    [SerializeField] private string apiKey = "AIzaSyALJUX0ypbgEpo1xsiN5gPMt9tLPU5awFA";

    private string endpoint =
"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.0-pro:generateContent?key=";


    private string gamePrompt =
        "You are an NPC in a fantasy game. " +
        "Reply with TWO lines only:\n" +
        "Line 1: NPC dialogue\n" +
        "Line 2: Short button text\n" +
        "Keep it immersive and concise.";

    void Start()
    {
        StartCoroutine(CallGemini("Start the conversation."));
    }

    public void OnInteract()
    {
        StartCoroutine(CallGemini("Player clicked the button."));
    }

    IEnumerator CallGemini(string playerAction)
    {
        string fullPrompt = gamePrompt + "\n\nPlayer action: " + playerAction;

        string json =
        "{ \"contents\": [ { \"parts\": [ { \"text\": \"" +
        fullPrompt.Replace("\"", "\\\"") +
        "\" } ] } ] }";

        UnityWebRequest request = new UnityWebRequest(endpoint + apiKey, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ParseResponse(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Gemini Error: " + request.downloadHandler.text);
            Debug.LogError("Result: " + request.result);
            Debug.LogError("Error: " + request.error);
            dialogueText.text = "NPC is silent...";
        }

    }

    void ParseResponse(string json)
    {
        int first = json.IndexOf("\"text\":");
        if (first == -1) return;

        int start = json.IndexOf("\"", first + 7) + 1;
        int end = json.IndexOf("\"", start);
        string text = json.Substring(start, end - start);

        string[] lines = text.Split('\n');

        dialogueText.text = lines[0];
        buttonText.text = lines.Length > 1 ? lines[1] : "Continue";
    }
}
