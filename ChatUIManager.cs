using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class ChatUIManager : UdonSharpBehaviour
{
    [Header("UI References")]
    [Tooltip("UI Input field for player to type in")]
    public InputField playerInputField;
    
    [Tooltip("Text display for chat history")]
    public Text chatDisplayArea;
    
    [Tooltip("Optional UI panel that contains the chat elements")]
    public GameObject chatPanel;
    
    [Tooltip("Optional scrollRect for auto-scrolling chat history")]
    public ScrollRect chatScrollRect;
    
    [Header("Chat UI Settings")]
    [Tooltip("Maximum number of characters in each message")]
    public int maxChatLength = 280;
    
    [Tooltip("Maximum number of messages to display in history")]
    public int maxChatHistory = 20;
    
    [Tooltip("Bot's display name in chat")]
    public string botName = "AI Assistant";
    
    [Tooltip("Prefix to show before bot messages")]
    public string botPrefix = "[🤖] ";
    
    [Header("Proximity Settings")]
    [Tooltip("Enable chat UI only when player is near")]
    public bool useProximity = true;
    
    [Tooltip("Distance at which chat becomes available")]
    public float proximityDistance = 3.0f;
    
    [Tooltip("Object to measure proximity from")]
    public Transform proximityOrigin;

    [Header("System References")]
    [Tooltip("Reference to the OSC Manager")]
    public OSCManager oscManager;
    
    private string[] chatHistory;
    private int chatHistoryIndex = 0;
    private bool chatHistoryFull = false;

    void Start()
    {
        // Initialize chat history array
        chatHistory = new string[maxChatHistory];
        
        // Check required references
        if (playerInputField == null || chatDisplayArea == null || oscManager == null)
        {
            Debug.LogError("ChatUIManager is missing required references!");
            if (playerInputField != null) playerInputField.interactable = false;
            return;
        }
        
        // Clear the input field on start
        playerInputField.text = "";
        
        // Add listener for when the player finishes editing
        playerInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        
        // Handle initial visibility
        if (useProximity && chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
        
        // Display welcome message
        string welcomeMessage = "Welcome! I'm your AI assistant. Ask me about businesses and attractions in this world!";
        DisplayBotMessage(welcomeMessage);
    }
    
    void Update()
    {
        // Handle proximity-based visibility if enabled
        if (useProximity && chatPanel != null && proximityOrigin != null)
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                float distance = Vector3.Distance(localPlayer.GetPosition(), proximityOrigin.position);
                bool shouldBeActive = distance <= proximityDistance;
                
                // Only update when state changes to minimize unnecessary UI redraws
                if (chatPanel.activeSelf != shouldBeActive)
                {
                    chatPanel.SetActive(shouldBeActive);
                    
                    // If newly visible, refresh the display
                    if (shouldBeActive)
                    {
                        RefreshChatDisplay();
                    }
                }
            }
        }
    }

    // Called when the player presses Enter or clicks away from the input field
    public void OnInputFieldSubmit(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && Input.GetKeyDown(KeyCode.Return))
        {
            SendMessageToBackend(message);
            playerInputField.text = "";
            playerInputField.ActivateInputField();  // Refocus for convenience
        }
    }

    // Public method for UI buttons
    public void OnSendButtonPressed()
    {
        if (!string.IsNullOrWhiteSpace(playerInputField.text))
        {
            SendMessageToBackend(playerInputField.text);
            playerInputField.text = "";
            playerInputField.ActivateInputField();
        }
    }

    private void SendMessageToBackend(string message)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        string playerName = localPlayer.displayName;
        int playerId = localPlayer.playerId;

        // Trim message if too long
        if (message.Length > maxChatLength)
        {
            message = message.Substring(0, maxChatLength);
        }

        // Display player's message in chat
        AddChatMessage($"[You]: {message}");

        // Send to backend via OSC Manager
        oscManager.SendChatMessage(playerName, playerId, message);
    }

    // Adds a message to chat history array
    private void AddChatMessage(string message)
    {
        // Store in circular buffer
        chatHistory[chatHistoryIndex] = message;
        chatHistoryIndex = (chatHistoryIndex + 1) % maxChatHistory;
        
        // Mark history as full once we've wrapped around
        if (chatHistoryIndex == 0)
        {
            chatHistoryFull = true;
        }
        
        // Update the display
        RefreshChatDisplay();
    }
    
    // Rebuilds the chat display from history
    private void RefreshChatDisplay()
    {
        if (chatDisplayArea == null) return;
        
        System.Text.StringBuilder chatText = new System.Text.StringBuilder();
        
        // Determine how many messages to show and where to start
        int count = chatHistoryFull ? maxChatHistory : chatHistoryIndex;
        int startIdx = chatHistoryFull ? chatHistoryIndex : 0;
        
        // Build chat text in chronological order
        for (int i = 0; i < count; i++)
        {
            int idx = (startIdx + i) % maxChatHistory;
            if (!string.IsNullOrEmpty(chatHistory[idx]))
            {
                if (chatText.Length > 0)
                    chatText.Append("\n");
                chatText.Append(chatHistory[idx]);
            }
        }
        
        // Update display
        chatDisplayArea.text = chatText.ToString();
        
        // Auto-scroll to bottom if we have a ScrollRect
        if (chatScrollRect != null)
        {
            // Force layout update to ensure new content size
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f; // 0 = bottom
        }
    }

    // Called by OSC Manager when a bot response is received
    public void DisplayBotMessage(string message)
    {
        // Add formatted bot message to chat
        AddChatMessage($"{botPrefix}{botName}: {message}");
    }
    
    // Optional method to clear chat history
    public void ClearChat()
    {
        // Reset history
        chatHistory = new string[maxChatHistory];
        chatHistoryIndex = 0;
        chatHistoryFull = false;
        
        // Update display
        RefreshChatDisplay();
    }
}
