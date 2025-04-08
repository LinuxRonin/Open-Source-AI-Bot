// --- Script 1: InteractableItem.cs ---
// Attach this script to each GameObject in your VRChat world that represents
// a sale item or an item that should trigger a notification on interaction.
// Requires UdonSharp: https://github.com/vrchat-community/UdonSharp
// Requires VRChat SDK3 Worlds.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces; // Required for SendCustomNetworkEvent

// Ensure UdonSharpBehaviour is the base class
[UdonBehaviourSyncMode(BehaviourSyncMode.None)] // No network syncing needed for this basic interaction
public class InteractableItem : UdonSharpBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("Unique identifier for this item (e.g., 'cool_sword_01', 'booth_poster_A')")]
    public string itemId = "default_item";

    [Tooltip("Display name for the item, used in notifications")]
    public string itemDisplayName = "Default Item";

    [Header("OSC Communication")]
    [Tooltip("Reference to the OSC Manager script in the scene")]
    public OSCManager oscManager; // Assign this in the Unity Inspector

    // This method is automatically called by VRChat when a player clicks on this object.
    public override void Interact()
    {
        // Get the player who interacted
        VRCPlayerApi interactingPlayer = Networking.LocalPlayer;
        if (interactingPlayer == null) return; // Should not happen, but safety check

        string playerName = interactingPlayer.displayName;
        int playerId = interactingPlayer.playerId; // Useful for unique identification

        Debug.Log($"Player '{playerName}' (ID: {playerId}) interacted with item '{itemDisplayName}' (ID: '{itemId}')");

        // Check if the OSC Manager is assigned
        if (oscManager != null)
        {
            // Tell the OSC Manager to send the interaction event to the backend
            oscManager.SendInteractionNotification(itemId, itemDisplayName, playerName, playerId);
        }
        else
        {
            Debug.LogError("OSCManager reference is not set on InteractableItem script!");
        }

        // Optional: Add some local feedback for the player (e.g., sound effect, particle effect)
        // PlaySoundEffect();
        // ShowVisualFeedback();
    }

    // --- Optional Feedback Methods ---
    // void PlaySoundEffect() { /* Add audio source logic here */ }
    // void ShowVisualFeedback() { /* Add particle system or animation logic here */ }
}


// --- Script 2: OSCManager.cs ---
// A central script to handle sending OSC messages to your backend service.
// Place one instance of this GameObject in your scene.
// Requires the VRChat OSC feature to be enabled by users in their VRChat settings.
// Requires an OSC library compatible with UdonSharp or manual OSC packet construction if needed.
// NOTE: Direct OSC sending from UdonSharp can be complex. Often, people use VRChat's built-in
// Avatar OSC features or OSCQuery services if available, or rely on relays.
// This example assumes a simplified direct OSC sending capability for illustration.
// You might need external tools or assets (like VRCOSC) or a different approach depending on feasibility.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase; // For Networking utilities if needed
// using VRC.OSC; // Hypothetical OSC library namespace - you'll need a real one

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OSCManager : UdonSharpBehaviour
{
    [Header("OSC Backend Configuration")]
    [Tooltip("The IP address of your backend server")]
    public string backendIpAddress = "127.0.0.1"; // Default to localhost for testing

    [Tooltip("The port your backend server is listening on for OSC messages")]
    public int backendPort = 9001; // Default VRChat OSC port is 9000, use a different one for backend

    // Placeholder for an actual OSC client instance.
    // You would need a specific OSC library implementation here.
    // Example: private OscClient oscClient;

    void Start()
    {
        // Initialize the OSC client here if using a library
        // Example: oscClient = new OscClient(backendIpAddress, backendPort);
        Debug.Log($"OSC Manager initialized. Target: {backendIpAddress}:{backendPort}");
    }

    // Method called by InteractableItem to send notification
    public void SendInteractionNotification(string itemId, string itemName, string playerName, int playerId)
    {
        // Define the OSC address pattern (like a URL path)
        string address = "/vrchat/interaction";

        // Construct the OSC message arguments
        // OSC messages contain an address pattern and a list of arguments (int, float, string, bool)
        object[] args = new object[] {
            itemId,
            itemName,
            playerName,
            playerId
        };

        // Send the OSC message
        SendOscMessage(address, args);

        Debug.Log($"Sent OSC interaction notification: {address} - {string.Join(", ", args)}");
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
        Debug.Log($"Sent OSC chat message: {address} - {playerName}: {message}");
    }

    // --- Core OSC Sending Logic ---
    // This is a placeholder! Actual implementation depends heavily on the OSC library used.
    // You might need to manually construct UDP packets if no direct UdonSharp library exists.
    private void SendOscMessage(string address, object[] args)
    {
        // --- !!! IMPORTANT !!! ---
        // Direct UDP socket manipulation or using third-party OSC libraries in Udon/UdonSharp
        // can be complex or restricted. VRChat's primary OSC support is focused on AVATAR parameters.
        // Sending arbitrary data TO an external server might require:
        // 1. Using VRChat's built-in Avatar OSC and having your backend listen to those parameters (limited).
        // 2. Using OSCQuery (if available/supported in your context).
        // 3. Relying on external helper applications running on the user's PC that relay messages.
        // 4. A custom game server architecture if building a Udon/Unity game outside VRChat's public instances.

        // This placeholder assumes a hypothetical `OscClient.Send(address, args)` method exists.
        Debug.LogWarning("Placeholder SendOscMessage called. Actual OSC sending implementation required!");
        // Example (Hypothetical):
        // if (oscClient != null) {
        //     oscClient.Send(address, args);
        // } else {
        //     Debug.LogError("OSC Client not initialized!");
        // }

        // --- Fallback/Alternative: Log to console for debugging ---
        // If direct OSC sending isn't feasible, you might log intended messages
        // and use an external tool reading VRChat logs to forward them (less ideal).
        Debug.Log($"[OSC Simulation] Address: {address}, Args: {string.Join(", ", args)}");
    }

    // --- Receiving OSC Messages (Conceptual) ---
    // Receiving OSC messages back from the backend into VRChat (e.g., chatbot responses)
    // is ALSO complex and usually relies on VRChat's Avatar OSC input feature.
    // The backend would send OSC messages formatted to change specific avatar parameters,
    // and Udon scripts would read these parameter changes.

    // Example placeholder for handling a received message (would likely be triggered by avatar parameter change)
    public void HandleBackendResponse(string responseType, string data)
    {
        if (responseType == "chatbot_response")
        {
            Debug.Log($"Received chatbot response from backend: {data}");
            // Find the ChatUIManager and display the message
            // Example: chatUIManager.DisplayBotMessage(data);
        }
        // Handle other potential response types
    }
}


// --- Script 3: ChatUIManager.cs (Conceptual) ---
// Handles the in-world UI for chat input and displaying bot responses.
// This would involve Unity UI elements (InputField, Text/TextMeshPro).

using UdonSharp;
using UnityEngine;
using UnityEngine.UI; // For InputField, Text
using VRC.SDKBase; // For Networking.LocalPlayer
// using TMPro; // If using TextMeshPro

public class ChatUIManager : UdonSharpBehaviour
{
    [Header("UI References")]
    public InputField playerInputField; // Assign the input field UI element
    public Text chatDisplayArea; // Assign the text display UI element
    // public TMP_Text chatDisplayAreaTMP; // Alternative for TextMeshPro

    [Header("System References")]
    public OSCManager oscManager; // Assign the OSC Manager

    private const int MaxChatLength = 280; // Limit message length

    void Start()
    {
        if (playerInputField == null || chatDisplayArea == null || oscManager == null)
        {
            Debug.LogError("ChatUIManager is missing required references!");
            // Optionally disable the input field if setup is incomplete
            if (playerInputField != null) playerInputField.interactable = false;
        }
        else
        {
            // Clear the input field on start
            playerInputField.text = "";
            // Add listener for when the player finishes editing (presses Enter)
            playerInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        }
    }

    // Called when the player presses Enter or clicks away from the input field
    private void OnInputFieldSubmit(string message)
    {
        // Only send if the message isn't empty and Enter was likely pressed
        if (!string.IsNullOrWhiteSpace(message) && Input.GetKeyDown(KeyCode.Return))
        {
            SendMessageToBackend(message);

            // Clear the input field after sending
            playerInputField.text = "";

            // Optional: Refocus the input field for quicker subsequent messages
            // playerInputField.ActivateInputField();
        } else if (!Input.GetKeyDown(KeyCode.Return)) {
             // If the user just clicked away, keep the text but clear if they submit empty later
             // Or clear it regardless: playerInputField.text = "";
        }
    }

    // Public method to be called by a Send Button if you add one
    public void OnSendButtonPressed()
    {
         if (!string.IsNullOrWhiteSpace(playerInputField.text))
         {
             SendMessageToBackend(playerInputField.text);
             playerInputField.text = "";
             // playerInputField.ActivateInputField(); // Optional refocus
         }
    }


    private void SendMessageToBackend(string message)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        string playerName = localPlayer.displayName;
        int playerId = localPlayer.playerId;

        // Trim message if too long
        if (message.Length > MaxChatLength)
        {
            message = message.Substring(0, MaxChatLength);
        }

        // Display player's own message locally immediately
        DisplayChatMessage(playerName, message);

        // Send to backend via OSC Manager
        oscManager.SendChatMessage(playerName, playerId, message);
    }

    // Displays a message in the chat UI (could be from player or bot)
    public void DisplayChatMessage(string senderName, string message)
    {
        if (chatDisplayArea != null)
        {
            // Add new message, ensuring not to exceed UI limits (optional: scrolling logic)
            chatDisplayArea.text += $"\n[{senderName}]: {message}";
            // Simple auto-scroll to bottom (basic version)
            // Canvas.ForceUpdateCanvases(); // Ensure layout is updated
            // scrollRect.verticalNormalizedPosition = 0f; // If using a ScrollRect
        }
        // else if (chatDisplayAreaTMP != null) { /* TMP version */ }
    }

     // Method called by OSCManager (or parameter driver) when a bot response is received
    public void DisplayBotMessage(string message)
    {
         DisplayChatMessage("Bot", message);
    }
}
