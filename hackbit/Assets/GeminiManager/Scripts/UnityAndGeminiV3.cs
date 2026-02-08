using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

[System.Serializable]
public class InlineData
{
    public string mimeType;
    public string data;
}

[System.Serializable]
public class TextPart
{
    public string text;
}

[System.Serializable]
public class ImagePart
{
    public string text;
    public InlineData inlineData;
}

[System.Serializable]
public class TextContent
{
    public string role;
    public TextPart[] parts;
}

[System.Serializable]
public class TextCandidate
{
    public TextContent content;
}

[System.Serializable]
public class TextResponse
{
    public TextCandidate[] candidates;
}

[System.Serializable]
public class ChatRequest
{
    public TextContent[] contents;
    public TextContent system_instruction;
}

public class UnityAndGeminiV3 : MonoBehaviour
{
    [Header("JSON API Configuration")]
    public TextAsset jsonApi;

    private string apiKey = "";
    private string apiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("ChatBot Function")]
    public TMP_InputField inputField;
    public TMP_Text uiText;
    public string botInstructions;
    private TextContent[] chatHistory;

    [Header("Prompt Function")]
    public string prompt = "";

    [Header("Audio Feedback")]
    public AudioSource audioSource;

    void Start()
    {
        UnityAndGeminiKey jsonApiKey =
            JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);

        apiKey = jsonApiKey.key;
        chatHistory = new TextContent[] { };

        if (prompt != "")
        {
            StartCoroutine(SendPromptRequestToGemini(prompt));
        }
    }

    // 🔊 PLAY RESPONSE AUDIO
    void PlayResponseAudio()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.Play();
        }
    }

    private IEnumerator SendPromptRequestToGemini(string promptText)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        string jsonData =
            "{\"contents\": [{\"parts\": [{\"text\": \"" + promptText + "\"}]}]}";

        byte[] jsonToSend =
            new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                TextResponse response =
                    JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);

                if (response.candidates.Length > 0 &&
                    response.candidates[0].content.parts.Length > 0)
                {
                    string text =
                        response.candidates[0].content.parts[0].text;

                    Debug.Log(text);
                    PlayResponseAudio();
                }
            }
        }
    }

    public void SendChat()
    {
        string userMessage = inputField.text;
        inputField.text = "";
        StartCoroutine(SendChatRequestToGemini(userMessage));
    }

    private IEnumerator SendChatRequestToGemini(string newMessage)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        TextContent userContent = new TextContent
        {
            role = "user",
            parts = new TextPart[]
            {
                new TextPart { text = newMessage }
            }
        };

        TextContent instruction = new TextContent
        {
            parts = new TextPart[]
            {
                new TextPart { text = botInstructions }
            }
        };

        List<TextContent> contentsList =
            new List<TextContent>(chatHistory);

        contentsList.Add(userContent);
        chatHistory = contentsList.ToArray();

        ChatRequest chatRequest = new ChatRequest
        {
            contents = chatHistory,
            system_instruction = instruction
        };

        string jsonData = JsonUtility.ToJson(chatRequest);
        byte[] jsonToSend =
            new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                TextResponse response =
                    JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);

                if (response.candidates.Length > 0 &&
                    response.candidates[0].content.parts.Length > 0)
                {
                    string reply =
                        response.candidates[0].content.parts[0].text;

                    uiText.text = reply;
                    Debug.Log(reply);

                    PlayResponseAudio();

                    TextContent botContent = new TextContent
                    {
                        role = "model",
                        parts = new TextPart[]
                        {
                            new TextPart { text = reply }
                        }
                    };

                    contentsList.Add(botContent);
                    chatHistory = contentsList.ToArray();
                }
            }
        }
    }
}
