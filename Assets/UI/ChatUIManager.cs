using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using AI.Engine;
using System.Collections.Generic;
using System.Text;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
public class ChatUIManager : UdonSharpBehaviour
{
    [Header("UI References")]
    public InputField playerInputField;
    public Text chatDisplayArea;
    public GameObject chatPanel;
    public ScrollRect chatScrollRect;

    [Header("System References")]
    public AIEngine aiEngine;
    public TTSManager ttsManager;

    [Header("Proximity Settings")]
    public bool useProximity = true;
    public float proximityDistance = 3.0f;
    public Transform proximityOrigin;

    [Header("Chat Settings")]
    public int maxChatHistory = 20;
    public string playerMessagePrefix = "[You]: ";
    public string botMessagePrefix; // Set via AIConfig
    public string botMessageSuffix;

    private List<string> chatHistory = new List<string>();
    private VRCPlayerApi localPlayer;
    private StringBuilder stringBuilder = new StringBuilder();
    private bool isInputFocused = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        localPlayer = Networking.LocalPlayer;

        if (playerInputField == null || chatDisplayArea == null || aiEngine == null)
        {
            LogError("ChatUIManager is missing references!");
            if (playerInputField != null) playerInputField.interactable = false;
            return;
        }

        if (aiEngine.aiConfig != null)
        {
            botMessagePrefix = aiEngine.aiConfig.botNamePrefix;
            botMessageSuffix = aiEngine.aiConfig.botNameSuffix;
        }
        else
        {
            botMessagePrefix = "[Bot]: "; // Default if AIConfig is missing
            botMessageSuffix = "";
            LogWarning("AIConfig is not assigned. Using default bot prefix.");
        }

        playerInputField.text = "";
        playerInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        playerInputField.onSelect.AddListener((string text) => { isInputFocused = true; });
        playerInputField.onDeselect.AddListener((string text) => { isInputFocused = false; });

        if (useProximity && chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        DisplayBotMessage("Hello! Ask me anything.");
    }

    private void Update()
    {
        HandleProximity();
        HandleInputActivation();
    }

    private void HandleProximity()
    {
        if (!useProximity || chatPanel == null || proximityOrigin == null || localPlayer == null) return;

        float distance = Vector3.Distance(localPlayer.GetPosition(), proximityOrigin.position);
        bool shouldBeActive = distance <= proximityDistance;

        if (chatPanel.activeSelf != shouldBeActive)
        {
            chatPanel.SetActive(shouldBeActive);
            if (shouldBeActive)
            {
                RefreshChatDisplay();
            }
        }
    }

    private void HandleInputActivation()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !isInputFocused)
        {
            playerInputField.ActivateInputField();
        }
    }

    public void OnInputFieldSubmit(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && Input.GetKeyDown(KeyCode.Return))
        {
            SendMessageToAI(message);
            playerInputField.text = "";
            playerInputField.ActivateInputField();
        }
    }

    public void OnSendButtonPressed()
    {
        if (!string.IsNullOrWhiteSpace(playerInputField.text))
        {
            SendMessageToAI(playerInputField.text);
            playerInputField.text = "";
            playerInputField.ActivateInputField();
        }
    }

    private void SendMessageToAI(string message)
    {
        if (aiEngine == null)
        {
            LogError("AIEngine is not assigned!");
            return;
        }

        string response = aiEngine.ProcessInput(message);
        DisplayPlayerMessage(message);
        DisplayBotMessage(response);

        if (ttsManager != null)
        {
            ttsManager.Speak(response);
        }
    }

    private void DisplayPlayerMessage(string message)
    {
        AddChatMessage($"{playerMessagePrefix}{message}");
    }

    public void DisplayBotMessage(string message)
    {
        AddChatMessage($"{botMessagePrefix}{message}{botMessageSuffix}");
    }

    private void AddChatMessage(string message)
    {
        if (chatHistory.Count >= maxChatHistory)
        {
            chatHistory.RemoveAt(0); // Remove oldest
        }
        chatHistory.Add(message);
        RefreshChatDisplay();
    }

    private void RefreshChatDisplay()
    {
        if (chatDisplayArea == null) return;

        stringBuilder.Clear();
        foreach (string chat in chatHistory)
        {
            stringBuilder.AppendLine(chat);
        }
        chatDisplayArea.text = stringBuilder.ToString();

        if (chatScrollRect != null)
        {
            // Delay scroll to end of frame for layout to update
            SendCustomEventDelayedFrames(nameof(UpdateScrollRect), 1);
        }
    }

    public void UpdateScrollRect()
    {
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ChatUIManager] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ChatUIManager] {message}");
    }
}