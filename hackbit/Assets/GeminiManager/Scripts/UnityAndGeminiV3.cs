using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI; // Added for Button
using System;

// --- JSON Data Structures ---

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

// The specific structure we expect from Gemini for the game
[System.Serializable]
public class StoryResponse
{
    public string story;
    public string[] options;
}

[System.Serializable]
public class TextPart
{
    public string text;
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
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("UI Components")]
    public TMP_Text storyText;          // The main story text on screen
    public Button[] optionButtons;      // Array of your 3 Buttons
    public TMP_Text[] optionButtonLabels; // Array of the Text inside those 3 Buttons

    [Header("Bot Settings")]
    [TextArea(3, 10)]
    public string botInstructions = "You are a text adventure engine. You MUST reply with valid JSON only. Format: { \"story\": \"Your story description here...\", \"options\": [\"Option 1\", \"Option 2\", \"Option 3\"] }. Do not use Markdown blocks.";

    [Header("Game Start Prompt")]
    public string initialPrompt = "Start a random mystery story at IIIT Jabalpur.";

    private List<TextContent> chatHistory = new List<TextContent>();

    void Start()
    {
        // Load API Key
        if (jsonApi != null)
        {
            UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
            apiKey = jsonApiKey.key;
        }
        else
        {
            Debug.LogError("API Key JSON file is missing!");
            return;
        }

        // Setup Buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // Capture index for closure
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            optionButtons[i].gameObject.SetActive(false); // Hide buttons initially
        }

        // Start the game automatically
        StartCoroutine(SendChatRequestToGemini(initialPrompt));
    }

    // --- UI Interaction ---

    // Called when a button is clicked
    public void OnOptionSelected(int index)
    {
        if (index < optionButtonLabels.Length)
        {
            string choice = optionButtonLabels[index].text;

            // Disable buttons to prevent double clicking
            foreach (var btn in optionButtons) btn.interactable = false;

            // Send the choice to Gemini
            StartCoroutine(SendChatRequestToGemini(choice));
        }
    }

    // --- API Logic ---

    private IEnumerator SendChatRequestToGemini(string userMessage)
    {
        string url = $"{apiEndpoint}?key={apiKey}";

        // 1. Add User Message to History
        TextContent userContent = new TextContent
        {
            role = "user",
            parts = new TextPart[] { new TextPart { text = userMessage } }
        };
        chatHistory.Add(userContent);

        // 2. Prepare System Instruction
        TextContent instruction = new TextContent
        {
            parts = new TextPart[] { new TextPart { text = botInstructions } }
        };

        // 3. Build Request
        ChatRequest chatRequest = new ChatRequest
        {
            contents = chatHistory.ToArray(),
            system_instruction = instruction
        };

        string jsonData = JsonUtility.ToJson(chatRequest);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // 4. Send Request
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                storyText.text = "Error: " + www.error;
            }
            else
            {
                Debug.Log("Response received!");
                HandleGeminiResponse(www.downloadHandler.text);
            }
        }
    }

    private void HandleGeminiResponse(string jsonResponse)
    {
        try
        {
            TextResponse response = JsonUtility.FromJson<TextResponse>(jsonResponse);

            if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
            {
                string rawText = response.candidates[0].content.parts[0].text;

                // Add Bot reply to history so it remembers the context
                TextContent botContent = new TextContent
                {
                    role = "model",
                    parts = new TextPart[] { new TextPart { text = rawText } }
                };
                chatHistory.Add(botContent);

                // --- JSON CLEANUP & PARSING ---
                // Gemini sometimes wraps JSON in markdown like ```json ... ```. We remove that.
                string cleanJson = rawText.Replace("```json", "").Replace("```", "").Trim();

                Debug.Log("Clean JSON: " + cleanJson);

                // Parse the game data
                StoryResponse gameData = JsonUtility.FromJson<StoryResponse>(cleanJson);

                // Update UI
                UpdateGameUI(gameData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Parsing Error: " + e.Message);
            storyText.text = "Error parsing game data. Check console.";
        }
    }

    private void UpdateGameUI(StoryResponse data)
    {
        // 1. Set the story text
        storyText.text = data.story;

        // 2. Set the buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < data.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtonLabels[i].text = data.options[i];
            }
            else
            {
                // Hide unused buttons (e.g., if only 2 options provided)
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }
}