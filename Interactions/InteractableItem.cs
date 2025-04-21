using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System; // Included for Debug, potentially other uses

namespace Interactions // Assuming this is the correct namespace based on folder
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] // Interaction logic is local
    public class InteractableItem : UdonSharpBehaviour
    {
        [Header("Item Configuration")]
        [Tooltip("Unique identifier for this item (e.g., 'cool_sword_01', 'booth_poster_A'). Optional, mainly for potential future logic.")]
        public string itemId = "default_item";

        [Tooltip("Display name for the item, used as default AI input if 'Interaction Text' is empty.")]
        public string itemDisplayName = "Default Item";

        [Header("AI Interaction")]
        [Tooltip("Reference to the ChatUIManager in the scene. MUST be assigned in the Inspector.")]
        public ChatUIManager chatUIManager; // *** CHANGED: Public field for Inspector assignment ***

        [Tooltip("Send this specific text to the AI when interacted with. If empty, the 'Item Display Name' is sent instead.")]
        [TextArea(2, 4)]
        public string interactionText = "";

        [Header("Feedback Settings (Optional)")]
        [Tooltip("AudioSource to play a sound effect on interaction.")]
        public AudioSource interactionSound;

        [Tooltip("ParticleSystem to trigger on interaction.")]
        public ParticleSystem interactionParticles;

        [Tooltip("Minimum time (seconds) between allowed interactions with this specific item.")]
        public float interactionCooldown = 1.0f; // Reduced default cooldown slightly

        [Header("Debug")]
        [Tooltip("Enable logging for interactions with this item.")]
        public bool debugMode = false;

        private float lastInteractionTime = -999f; // Initialize far in the past to allow immediate first interaction
        private bool isInitialized = false;

        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // --- VRChat Best Practice: Check Inspector References ---
            if (chatUIManager == null)
            {
                LogError($"ChatUIManager is not assigned in the Inspector for item '{itemDisplayName}' ({gameObject.name})! Interactions will not trigger the AI.");
                // Optionally disable interaction? this.DisableInteractive = true;
                // Or just let it fail silently in Interact() check below.
                isInitialized = false;
                return; // Stop initialization if critical reference is missing
            }

            // Optional checks for feedback components
            if (interactionSound == null) {
                 if(debugMode) Debug.Log($"[InteractableItem] Note: No interaction sound assigned for '{itemDisplayName}'.");
            }
             if (interactionParticles == null) {
                 if(debugMode) Debug.Log($"[InteractableItem] Note: No interaction particles assigned for '{itemDisplayName}'.");
            }


            isInitialized = true;
             if (debugMode) Debug.Log($"[InteractableItem] Initialized: '{itemDisplayName}'");
        }

        // This method is called by VRChat when a player interacts with this object
        public override void Interact()
        {
            // --- Check Initialization and Cooldown ---
            if (!isInitialized || chatUIManager == null) // Double-check UIManager reference
            {
                 LogError($"Interact called on '{itemDisplayName}' but it's not initialized or ChatUIManager is missing.");
                 return;
            }

            float currentTime = Time.time;
            if (currentTime < lastInteractionTime + interactionCooldown)
            {
                // Interaction is on cooldown
                if (debugMode) Debug.Log($"[InteractableItem] Interaction cooldown active for '{itemDisplayName}'. Time remaining: {((lastInteractionTime + interactionCooldown) - currentTime):F1}s");
                return;
            }

            // --- Update Cooldown ---
            lastInteractionTime = currentTime;

            // --- Get Interacting Player (Local) ---
            VRCPlayerApi interactingPlayer = Networking.LocalPlayer;
            if (interactingPlayer == null) {
                 // Should generally not happen if Interact() is called, but good practice
                 LogWarning("Interact() called but Networking.LocalPlayer is null.");
                 return;
            }

            // --- Log Interaction (if debug enabled) ---
            LogInteractionDetails(interactingPlayer);

            // --- Send Interaction to AI ---
            SendInteractionToAI();

            // --- Play Feedback Effects ---
            PlayFeedback();
        }

        private void LogInteractionDetails(VRCPlayerApi player) {
             if (!debugMode) return;

            string playerName = player.displayName;
            int playerId = player.playerId;
            Debug.Log($"[InteractableItem] Player '{playerName}' (ID: {playerId}) interacted with item '{itemDisplayName}' (GameObject: {gameObject.name})");
        }


        private void SendInteractionToAI()
        {
            // Determine the text to send: use specific interactionText if provided, otherwise use itemDisplayName
            string aiInput = string.IsNullOrWhiteSpace(interactionText) ? itemDisplayName : interactionText;

            if (string.IsNullOrWhiteSpace(aiInput)) {
                 LogWarning($"Interaction for '{itemDisplayName}' resulted in empty AI input text. Check itemDisplayName and interactionText fields.");
                 return;
            }

            // Call the public method on the assigned ChatUIManager instance
            if (debugMode) Debug.Log($"[InteractableItem] Sending to AI: '{aiInput}'");
            chatUIManager.SendMessageToAI(aiInput);
        }

        private void PlayFeedback()
        {
            // Play sound if assigned and not already playing (prevents overlapping sounds from rapid clicks)
            if (interactionSound != null && !interactionSound.isPlaying)
            {
                interactionSound.Play();
            }

            // Play particle effect if assigned
            if (interactionParticles != null)
            {
                // Use Play() to restart the effect. Use Emit() for single bursts if needed.
                interactionParticles.Play();
            }
        }

        // Centralized logging
        private void LogError(string message) {
            Debug.LogError($"[InteractableItem] {message}");
        }
        private void LogWarning(string message) {
             Debug.LogWarning($"[InteractableItem] {message}");
         }
    }
}
