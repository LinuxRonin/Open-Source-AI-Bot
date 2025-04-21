using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using AI.Engine; // Assuming AIEngine is in this namespace
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.EventSystems; // Added for BaseEventData

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
public class ChatUIManager : UdonSharpBehaviour
{
    [Header("UI References")]
    public InputField playerInputField;
    public Text chatDisplayArea;
    public GameObject chatPanel; // Panel containing chat UI elements
    public ScrollRect chatScrollRect;

    [Header("System References")]
    [Tooltip("Reference to the AIEngine UdonBehaviour.")]
    public AIEngine aiEngine;
    [Tooltip("Reference to the TTSManager UdonBehaviour (Optional, Windows Editor TTS only).")]
    public TTSManager ttsManager; // TTS will likely NOT work in VRChat builds

    [Header("Proximity Settings")]
    public bool useProximity = true;
    public float proximityDistance = 3.0f;
    [Tooltip("The GameObject whose position is used as the center for proximity checks.")]
    public Transform proximityOrigin;

    [Header("Chat Settings")]
    public int maxChatHistory = 20;
    public string playerMessagePrefix = "[You]: ";
    // Bot prefix/suffix will be fetched from AIConfig via AIEngine
    private string botMessagePrefix = "[Bot]: "; // Default fallback
    private string botMessageSuffix = "";

    private List<string> chatHistory = new List<string>();
    private VRCPlayerApi localPlayer;
    private StringBuilder stringBuilder = new StringBuilder();
    private bool isInputFocused = false;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        localPlayer = Networking.LocalPlayer; // Can be null initially

        // --- Essential Reference Checks ---
        if (playerInputField == null) {
            LogError("Player Input Field is not assigned!");
            this.enabled = false; // Disable if core UI is missing
            return;
        }
        if (chatDisplayArea == null) {
             LogError("Chat Display Area is not assigned!");
             this.enabled = false;
             return;
        }
         if (chatPanel == null) {
             LogWarning("Chat Panel is not assigned! UI will always be visible if active.");
             useProximity = false; // Disable proximity if panel is missing
         }
        if (aiEngine == null)
        {
            LogError("AIEngine reference is not assigned! Chatbot cannot function.");
            // Display error message to user?
            DisplayBotMessage("Error: AI Engine not connected.");
            this.enabled = false; // Disable component if AI is missing
            return;
        }

        // --- Optional Reference Checks ---
        if (ttsManager == null) {
             LogWarning("TTSManager is not assigned. Text-to-Speech will be unavailable.");
        } else {
             #if !UNITY_EDITOR_WIN
             LogWarning("TTSManager is assigned, but TTS functionality is generally NOT available in VRChat builds or on non-Windows platforms.");
             #endif
        }
         if (useProximity && proximityOrigin == null) {
             LogWarning("useProximity is enabled, but Proximity Origin is not assigned! Disabling proximity.");
             useProximity = false;
         }
         if (chatScrollRect == null) {
             LogWarning("Chat Scroll Rect is not assigned. Auto-scrolling will be disabled.");
         }


        // --- Setup ---
        // Fetch bot name style from AIConfig via AIEngine (check if aiConfig exists first)
        if (aiEngine.aiConfig != null)
        {
            botMessagePrefix = aiEngine.aiConfig.botNamePrefix;
            botMessageSuffix = aiEngine.aiConfig.botNameSuffix;
        }
        else
        {
            LogWarning("AIConfig is not assigned in the referenced AIEngine. Using default bot prefix/suffix.");
            // Keep the default fallback values assigned above
        }

        playerInputField.text = "";
        // Ensure listeners are cleared before adding (prevents duplicates on re-compile/re-enable)
        playerInputField.onEndEdit.RemoveListener(OnInputFieldSubmit);
        playerInputField.onSelect.RemoveListener(OnInputFieldSelect);
        playerInputField.onDeselect.RemoveListener(OnInputFieldDeselect);
        // Add listeners
        playerInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        playerInputField.onSelect.AddListener(OnInputFieldSelect);
        playerInputField.onDeselect.AddListener(OnInputFieldDeselect);


        if (chatPanel != null) {
             chatPanel.SetActive(!useProximity); // Start inactive if using proximity
        }

        // DisplayBotMessage("Hello! Ask me anything."); // Optional initial message
        isInitialized = true;
         Log("ChatUIManager Initialized successfully.");
    }

    void Update()
    {
        if (!isInitialized) return; // Don't run update if initialization failed

        // Ensure localPlayer is valid (it might be null briefly on startup)
        if (localPlayer == null) {
             localPlayer = Networking.LocalPlayer;
             if(localPlayer == null) return; // Still null, wait for next frame
        }

        HandleProximity();
        HandleInputActivation();
    }

    private void HandleProximity()
    {
        if (!useProximity || chatPanel == null || proximityOrigin == null || localPlayer == null) return;

        // Use squared distance for slight performance improvement (avoids sqrt)
        float sqrDistance = Vector3.SqrMagnitude(localPlayer.GetPosition() - proximityOrigin.position);
        float sqrProximityDistance = proximityDistance * proximityDistance;
        bool shouldBeActive = sqrDistance <= sqrProximityDistance;

        if (chatPanel.activeSelf != shouldBeActive)
        {
            chatPanel.SetActive(shouldBeActive);
            if (shouldBeActive)
            {
                RefreshChatDisplay(); // Refresh chat content when panel appears
                ScrollToBottom(); // Ensure latest message is visible
            } else {
                 // Optional: Deactivate input field if player leaves proximity?
                 // if (isInputFocused) playerInputField.DeactivateInputField();
            }
        }
    }

    // Allows activating the chat input field by pressing Enter when not focused
    private void HandleInputActivation()
    {
        if (playerInputField == null || !playerInputField.interactable) return;

        // Check if the chat panel is active (or if proximity isn't used)
        bool canInteract = (chatPanel != null && chatPanel.activeSelf) || !useProximity;
        if (!canInteract) return;

        // Use GetKeyDown for single activation press
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
             if (!isInputFocused)
             {
                playerInputField.ActivateInputField(); // Focus the input field
                // Select all text? playerInputField.Select();
             }
             // Submission is handled by OnInputFieldSubmit when Enter is pressed while focused
        }
    }

    // Listener for InputField onSelect event
    public void OnInputFieldSelect(BaseEventData data) {
         isInputFocused = true;
         // Optional: Disable player movement while typing? (Requires PlayerMod permissions or specific setup)
    }

    // Listener for InputField onDeselect event
    public void OnInputFieldDeselect(BaseEventData data) {
         isInputFocused = false;
         // Optional: Re-enable player movement
    }


    // Called when Enter is pressed while Input Field is focused, or when deselected
    public void OnInputFieldSubmit(string message)
    {
        // Only submit if Enter was pressed (check GetKeyDown again as onEndEdit triggers on deselect too)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                SendMessageToAI(message);
                playerInputField.text = ""; // Clear input field AFTER sending
                // Reactivate to allow continuous typing without clicking again
                playerInputField.ActivateInputField();
            } else {
                 // If message is empty/whitespace, just clear and keep focus
                 playerInputField.text = "";
                 playerInputField.ActivateInputField();
            }
        }
    }

    // Optional: Handler for a dedicated Send Button
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

    // Public method accessible by other scripts (like InteractableItem)
    public void SendMessageToAI(string message)
    {
        if (!isInitialized || aiEngine == null) // Double check initialization and reference
        {
            LogError("Cannot send message, AIEngine not available or not initialized.");
            return;
        }

        // Display player message immediately for responsiveness
        DisplayPlayerMessage(message);

        // Get response from AI Engine
        string response = aiEngine.ProcessInput(message); // ProcessInput should handle its own errors/defaults
        DisplayBotMessage(response);

        // Trigger TTS only if manager exists AND we are in the Windows Editor
        #if UNITY_EDITOR_WIN
        if (ttsManager != null)
        {
            ttsManager.Speak(response);
        }
        #endif
    }

    private void DisplayPlayerMessage(string message)
    {
        AddChatMessage($"{playerMessagePrefix}{message}");
    }

    // Public in case other scripts need to display bot messages directly
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

        // Scroll to bottom after layout rebuilds (using delayed event)
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
         if (chatScrollRect != null)
        {
            // Using SendCustomEventDelayedFrames is generally the most reliable way
            // to scroll after the UI layout has updated in the same frame.
            SendCustomEventDelayedFrames(nameof(UpdateScrollRect), 1);
        }
    }

    // Public method callable by SendCustomEventDelayedFrames
    public void UpdateScrollRect()
    {
        if (chatScrollRect != null) {
            // Ensure it scrolls all the way down
            chatScrollRect.normalizedPosition = new Vector2(0, 0);
            // Or chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Centralized logging
     private void Log(string message) {
         Debug.Log($"[ChatUIManager] {message}");
     }
    private void LogError(string message) {
        Debug.LogError($"[ChatUIManager] {message}");
    }
    private void LogWarning(string message) {
        Debug.LogWarning($"[ChatUIManager] {message}");
    }
}
