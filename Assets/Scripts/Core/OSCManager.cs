using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OSCManager : UdonSharpBehaviour
{
    [Header("OSC Backend Configuration")]
    [Tooltip("The IP address of your backend server")]
    public string backendIpAddress = "127.0.0.1";

    [Tooltip("The port your backend server is listening on for OSC messages")]
    public int backendPort = 9001;
    
    [Header("Debug Settings")]
    [Tooltip("Enable detailed logging for troubleshooting")]
    public bool verboseLogging = false;
    
    [Tooltip("Simulate OSC communication in editor (for testing without OSC)")]
    public bool simulateOscInEditor = true;
    
    [Header("References")]
    [Tooltip("Reference to the chat UI manager")]
    public ChatUIManager chatUIManager;

    private bool isInitialized = false;

    void Start()
    {
        // VRChat-specific initialization would happen here
        // This is largely placeholder since direct OSC from UdonSharp is limited
        isInitialized = true;
        
        // VRChat requires OSC to be enabled in the client settings
        LogMessage("OSC Manager initialized. Target: " + backendIpAddress + ":" + backendPort);
        LogMessage("Note: VRChat must have OSC enabled in Settings > OSC");
    }

    // Method called by InteractableItem to send notification
    public void SendInteractionNotification(string itemId, string itemName, string playerName, int playerId)
    {
        // Define the OSC address pattern
        string address = "/vrchat/interaction";

        // Construct the OSC message arguments
        object[] args = new object[] {
            itemId,
            itemName,
            playerName,
            playerId
        };

        // Send the OSC message
        SendOscMessage(address, args);
    }

    // Method to send chat messages from VRChat player to the backend AI
    public void SendChatMessage(string playerName, int playerId, string message)
    {
        string address = "/vrchat/chat";
        object[] args = new object[] {
            playerName,
            playerId,
            message
        };
        
        SendOscMessage(address, args);
        LogMessage($"Sent chat message to backend: {playerName}: {message}");
    }

    // --- Core OSC Sending Logic ---
    private void SendOscMessage(string address, object[] args)
    {
        // VRChat Udon has limited support for direct OSC message creation
        // In a production environment, this would likely use:
        // 1. VRChat Avatar Parameters for simpler cases
        // 2. Or proprietary methods if available to the VRChat creator
        
        // For testing in editor
        if (Application.isEditor && simulateOscInEditor)
        {
            LogMessage($"[OSC Simulation] Address: {address}, Args: {string.Join(", ", args)}");
            
            // If this is a chat message and we're in editor, simulate a bot response
            if (address == "/vrchat/chat" && args.Length >= 3 && chatUIManager != null)
            {
                // Wait a moment to simulate processing time
                SendCustomEventDelayedSeconds(nameof(_SimulateResponse), 1.0f);
            }
            return;
        }
        
        // --- IMPORTANT IMPLEMENTATION NOTE ---
        // There are several approaches to implement actual OSC sending in VRChat:
        //
        // 1. Use VRChat's built-in Avatar Parameter OSC system:
        //    - Set specific avatar parameters that trigger OSC
        //    - These get automatically sent via OSC when the parameter changes
        //    - Example: animator.SetBool("SendChat", true);
        //
        // 2. Use community tools like VRCOSC as a bridge:
        //    - External tool running on user's PC that bridges VRC and custom endpoints
        //    - https://github.com/VolcanicArts/VRCOSC
        
        LogMessage("Sending OSC message: " + address);
    }
    
    // Simulates a response from the backend (for Editor testing)
    public void _SimulateResponse()
    {
        if (chatUIManager != null)
        {
            string simulatedResponse = "Hello! I'm simulating a response since we're in the Unity Editor. In VRChat, I would respond based on your actual backend AI.";
            chatUIManager.DisplayBotMessage(simulatedResponse);
        }
    }
    
    // Public method to handle OSC responses coming from the backend
    // This would typically be called by a parameter change listener in VRChat
    public void HandleOscResponse(string messageType, string content)
    {
        LogMessage($"Received OSC response: {messageType} - {content}");
        
        switch (messageType)
        {
            case "chat":
                if (chatUIManager != null)
                {
                    chatUIManager.DisplayBotMessage(content);
                }
                break;
                
            case "notification":
                // Handle notification type responses
                break;
                
            default:
                LogMessage($"Unknown message type: {messageType}");
                break;
        }
    }
    
    private void LogMessage(string message)
    {
        if (verboseLogging || Application.isEditor)
        {
            Debug.Log($"[OSCManager] {message}");
        }
    }
}
