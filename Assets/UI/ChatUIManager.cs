using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using AI.Engine; // Assuming AIEngine is in this namespace
using System.Collections.Generic; // Corrected namespace
using System.Text;
using System;
using UnityEngine.EventSystems; // Added for BaseEventData

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
    public TTSManager ttsManager; // Assuming TTSManager is in AI.Engine or global

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

        // Ensure AIConfig exists before accessing it
        if (aiEngine.aiConfig != null)
        {
            botMessagePrefix = aiEngine.aiConfig.botNamePrefix;
            botMessageSuffix = aiEngine.aiConfig.botNameSuffix;
        }
        else
        {
            botMessagePrefix = "[Bot]: "; // Default if AIConfig is missing
            botMessageSuffix = "";
            LogWarning("AIConfig is not assigned in AIEngine. Using default bot prefix.");
        }

        playerInputField.text = "";
        playerInputField.onEndEdit.AddListener(OnInputFieldSubmit);

        // FIX: Corrected listener signatures for onSelect and onDeselect
        // They expect a BaseEventData argument.
        playerInputField.onSelect.AddListener((BaseEventData data) => { isInputFocused = true; });
        playerInputField.onDeselect.AddListener((BaseEventData data) => { isInputFocused = false; });

        if (useProximity && chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        else if (useProximity && chatPanel == null) {
            LogWarning("useProximity is true, but chatPanel is not assigned.");
        }

        // Optionally display an initial message
        // DisplayBotMessage("Hello! Ask me anything.");
    }

    private void Update()
    {
        HandleProximity();
        HandleInputActivation();
    }

    private void HandleProximity()
    {
        if (!useProximity || chatPanel == null || proximityOrigin == null) return;
        // Check if localPlayer is valid (it might be null briefly on startup or if not in VR/Editor)
        if (localPlayer == null) {
             localPlayer = Networking.LocalPlayer;
             if(localPlayer == null) return; // Still null, exit
        }


        float distance = Vector3.Distance(localPlayer.GetPosition(), proximityOrigin.position);
        bool shouldBeActive = distance <= proximityDistance;

        if (chatPanel.activeSelf != shouldBeActive)
        {
            chatPanel.SetActive(shouldBeActive);
            if (shouldBeActive)
            {
                RefreshChatDisplay(); // Refresh when panel becomes active
            }
        }
    }

    private void HandleInputActivation()
    {
        // Check if input field exists and is interactable
        if (playerInputField == null || !playerInputField.interactable) return;

        // Use GetKeyDown for single activation press
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
             if (!isInputFocused)
             {
                playerInputField.ActivateInputField(); // Focus the input field
             }
             // Note: onEndEdit handles the submission when Enter is pressed while focused
        }
    }

    // Called when Enter is pressed while Input Field is focused, or when deselected
    public void OnInputFieldSubmit(string message)
    {
        // Only submit if Enter was pressed (check GetKeyDown again)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                SendMessageToAI(message);
                playerInputField.text = "";
                // Reactivate the input field to allow continuous typing after sending
                playerInputField.ActivateInputField();
            } else {
                 // If message is empty/whitespace, just keep focus
                 playerInputField.ActivateInputField();
            }
        }
         // Important: Keep the input field focused even after submitting
         // If you lose focus here, you might need to press Enter twice.
         // playerInputField.ActivateInputField(); // Ensure it stays focused for next message
    }

    // Optional: Separate button click handler if you have a Send button
    public void OnSendButtonPressed()
    {
        if (playerInputField == null) return;
        string message = playerInputField.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            SendMessageToAI(message);
            playerInputField.text = "";
            playerInputField.ActivateInputField(); // Keep focus after sending via button
        }
    }

    // FIX: Made public so InteractableItem can call it
    public void SendMessageToAI(string message)
    {
        if (aiEngine == null)
        {
            LogError("AIEngine is not assigned!");
            DisplayBotMessage("Error: AI Engine not connected."); // Inform user
            return;
        }

        // Display player message immediately for responsiveness
        DisplayPlayerMessage(message);

        // Get response from AI
        string response = aiEngine.ProcessInput(message);
        DisplayBotMessage(response);

        // Trigger TTS if available
        if (ttsManager != null)
        {
            ttsManager.Speak(response);
        }
    }

    private void DisplayPlayerMessage(string message)
    {
        AddChatMessage($"{playerMessagePrefix}{message}");
    }

    // Made public in case other scripts need to display bot messages directly
    public void DisplayBotMessage(string message)
    {
        AddChatMessage($"{botMessagePrefix}{message}{botMessageSuffix}");
    }

    private void AddChatMessage(string message)
    {
        if (chatHistory.Count >= maxChatHistory)
        {
            chatHistory.RemoveAt(0); // Remove the oldest message
        }
        chatHistory.Add(message);
        RefreshChatDisplay();
    }

    private void RefreshChatDisplay()
    {
        if (chatDisplayArea == null) return;

        stringBuilder.Clear();
        for (int i = 0; i < chatHistory.Count; i++)
        {
            stringBuilder.AppendLine(chatHistory[i]);
        }
        chatDisplayArea.text = stringBuilder.ToString();

        // Scroll to bottom
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
         if (chatScrollRect != null)
        {
            // Using Canvas.ForceUpdateCanvases() before scrolling can sometimes help
            // Canvas.ForceUpdateCanvases();
            // chatScrollRect.verticalNormalizedPosition = 0f;
            // Using DelayedFrames is often more reliable for scroll rects
            SendCustomEventDelayedFrames(nameof(UpdateScrollRect), 1);
        }
    }

    // Public method callable by SendCustomEventDelayedFrames
    public void UpdateScrollRect()
    {
        if (chatScrollRect != null) {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
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
