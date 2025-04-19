using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

namespace Interactions
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] // Local only
    public class InteractableItem : UdonSharpBehaviour
    {
        [Header("Item Configuration")]
        [Tooltip("Unique identifier for this item (e.g., 'cool_sword_01', 'booth_poster_A')")]
        public string itemId = "default_item";

        [Tooltip("Display name for the item, used in notifications")]
        public string itemDisplayName = "Default Item";

        [Header("AI Interaction")]
        [Tooltip("Send this text to the AI when interacted with (optional). If empty, itemDisplayName is sent.")]
        [TextArea(2, 4)]
        public string interactionText = "";

        [Header("Feedback Settings")]
        [Tooltip("Optional sound effect to play on interaction")]
        public AudioSource interactionSound;

        [Tooltip("Optional particle system to trigger on interaction")]
        public ParticleSystem interactionParticles;

        [Tooltip("How long to wait between allowed interactions (seconds)")]
        public float interactionCooldown = 2.0f;

        [Header("Debug")]
        public bool debugMode = false;

        private float lastInteractionTime = -10f; // Initialize to allow immediate first interaction
        private ChatUIManager chatUIManager;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Find ChatUIManager on Start (more robust)
            chatUIManager = GameObject.FindObjectOfType<ChatUIManager>();
            if (chatUIManager == null)
            {
                LogError("ChatUIManager not found in the scene! Cannot send interaction to AI.");
            }
        }

        public override void Interact()
        {
            if (!CanInteract()) return;

            MarkInteractionTime();

            VRCPlayerApi interactingPlayer = Networking.LocalPlayer;
            if (interactingPlayer == null) return;

            LogInteraction(interactingPlayer);

            SendInteractionToAI();
            PlayFeedback();
        }

        private bool CanInteract()
        {
            return Time.time - lastInteractionTime >= interactionCooldown;
        }

        private void MarkInteractionTime()
        {
            lastInteractionTime = Time.time;
        }

        private void LogInteraction(VRCPlayerApi interactingPlayer)
        {
            if (!debugMode) return;

            string playerName = interactingPlayer.displayName;
            int playerId = interactingPlayer.playerId;
            Debug.Log($"Player '{playerName}' (ID: {playerId}) interacted with item '{itemDisplayName}' (ID: '{itemId}')");
        }

        private void SendInteractionToAI()
        {
            if (chatUIManager == null)
            {
                LogError("ChatUIManager is not assigned! Cannot send interaction to AI.");
                return;
            }

            string aiInput = string.IsNullOrEmpty(interactionText) ? itemDisplayName : interactionText;
            chatUIManager.SendMessageToAI(aiInput);
        }

        private void PlayFeedback()
        {
            if (interactionSound != null && !interactionSound.isPlaying)
            {
                interactionSound.Play();
            }

            if (interactionParticles != null)
            {
                interactionParticles.Play();
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[InteractableItem] {message}");
        }
    }
}