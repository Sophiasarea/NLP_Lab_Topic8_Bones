using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;

[Serializable]
public class AIContent
{
    public string text;
    public string action;
    public string expression;
}

[Serializable]
public class OllamaResponse
{
    public string response; // Ollama return the  JSON string
}

public class OllamaBridge : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TMP_Text aiDisplayText;
    public Animator characterAnimator; // drag to Bones

    private string url = "http://localhost:11434/api/generate";

    public void OnSendButtonClick()
    {
        string userText = userInputField.text;
        if (!string.IsNullOrEmpty(userText))
        {
            StartCoroutine(PostToAI(userText));
            userInputField.text = ""; // clear input after sending
        }
    }

    IEnumerator PostToAI(string userMessage)
    {
        aiDisplayText.text = "Bones is thinking..."; // waiting fir feedback

        // // format
        // string systemPrompt = "You are a professional empathetic researcher. Respond ONLY in JSON format: {\"text\": \"your words\", \"action\": \"JUMP/LOOSE\", \"expression\": \"ANGRY/SAD/NONE\"}. User message: ";
        // string fullPrompt = systemPrompt + userMessage;

        // string jsonPayload = "{\"model\": \"qwen2.5:1.5b\", \"prompt\": \"" + fullPrompt + "\", \"stream\": false}";
        // byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);

        string systemPrompt = "You are a small skeleton named Bones. You are empathetic and kind. " +
                      "Instructions: Respond to the user's feelings with a short sentence. " +
                      "Actions: Choose only from [JUMP, LOOSE, NONE]. If the user is sad or crying, action MUST be LOOSE. If the user is happy, action MUST be JUMP. Else, action MUST be NONE." +
                      "Expressions: Choose only from [ANGRY, SAD, NONE]. If the user is sad/crying, expression MUST be SAD. If the user is angry and complaining something, you have to show your empathy and choose ANGRY. If the user is happy, expression MUST be NONE. Else, expression MUST be NONE." +
                      "Format: You MUST output ONLY a valid JSON. " +
                      "Example: {\"text\": \"I am so sorry you feel this way.\", \"action\": \"LOOSE\", \"expression\": \"SAD\"}. " +
                      "Rule: If the user is sad/crying, expression MUST be SAD. If the user is happy, action MUST be JUMP. " +
                      "User says: ";
        string fullPrompt = systemPrompt + userMessage;

        // more stable JSON
        string escapedPrompt = fullPrompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        // string jsonPayload = "{\"model\": \"qwen2.5:1.5b\", \"prompt\": \"" + escapedPrompt + "\", \"stream\": false}";
        string jsonPayload = "{\"model\": \"qwen2.5:1.5b\", \"prompt\": \"" + escapedPrompt + "\", \"stream\": false, \"options\": {\"temperature\": 0.7}}"; // add temperature option to make the response more diverse

        byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ProcessResponse(request.downloadHandler.text);
        }
        else
        {
            aiDisplayText.text = "Connecting failed: " + request.error;
        }
    }


    public SkinnedMeshRenderer characterMesh;

    void ProcessResponse(string rawJson)
    {
        try
        {
            // 1. examine the raw JSON from Ollama
            OllamaResponse outer = JsonUtility.FromJson<OllamaResponse>(rawJson);
            string innerContent = outer.response.Trim();

            // remove code block markers if exist
            if (innerContent.Contains("```json"))
            {
                innerContent = innerContent.Replace("```json", "");
            }
            if (innerContent.Contains("```"))
            {
                innerContent = innerContent.Replace("```", "");
            }
            innerContent = innerContent.Trim();

            // 2. analyze the inner JSON content
            AIContent content = JsonUtility.FromJson<AIContent>(innerContent);

            Debug.Log("Expression: " + content.expression);

            // 3. apply the content to UI and character
            aiDisplayText.text = content.text;
            PlayAction(content.action);
            ApplyExpression(content.expression);
        }
        catch (Exception e)
        {
            // if parsing fails, fallback to showing raw response
            OllamaResponse outer = JsonUtility.FromJson<OllamaResponse>(rawJson);
            aiDisplayText.text = outer.response;
            Debug.LogWarning("AI didn't speak in the expected format, fallback to plain text mode: " + e.Message);
        }
    }

    void PlayAction(string actionName)
    {
        if (characterAnimator == null) return;

        characterAnimator.SetTrigger(actionName);
        Debug.Log("play action: " + actionName);
    }

    void ApplyExpression(string emotion)
    {
        Debug.Log("number of blend shapes: " + characterMesh.sharedMesh.blendShapeCount);
        if (characterMesh == null) return;

        // reset all expressions into 0
        characterMesh.SetBlendShapeWeight(0, 0); // 0 is Angry
        characterMesh.SetBlendShapeWeight(1, 0); // 1 is Sad

        // apply expression
        if (emotion == "ANGRY") characterMesh.SetBlendShapeWeight(0, 100);
        else if (emotion == "SAD") characterMesh.SetBlendShapeWeight(1, 100);
    }
}