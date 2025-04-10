using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class InteractableItem : UdonSharpBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("Unique identifier for this item (e.g., 'cool_sword_01', 'booth_poster_A')")]
    public string itemId = "default_item";

    [Tooltip("Display name for the item, used in notifications")]
    public string itemDisplayName = "Default Item";

    [Header("OSC Communication")]
    [Tooltip("Reference to the OSC Manager script in the scene")]
    public OSCManager oscManager;

    [Header("Feedback Settings")]
    [Tooltip("Optional sound effect to play on interaction")]
    public AudioSource interactionSound;
    
    [Tooltip("Optional particle system to trigger on interaction")]
    public ParticleSystem interactionParticles;
    
    [Tooltip("How long to wait between allowed interactions (seconds)")]
    public float interactionCooldown = 2.0f;
    
    private float lastInteractionTime = -10f; // Initialize to allow immediate first interaction

    public override void Interact()
    {
        // Check cooldown to prevent spam
        if (Time.time - lastInteractionTime < interactionCooldown)
            return;
            
        lastInteractionTime = Time.time;
        
        // Get the player who interacted
        VRCPlayerApi interactingPlayer = Networking.LocalPlayer;
        if (interactingPlayer == null) return;

        string playerName = interactingPlayer.displayName;
        int playerId = interactingPlayer.playerId;

        Debug.Log($"Player '{playerName}' (ID: {playerId}) interacted with item '{itemDisplayName}' (ID: '{itemId}')");

        // Send notification through OSC
        if (oscManager != null)
        {
            oscManager.SendInteractionNotification(itemId, itemDisplayName, playerName, playerId);
        }
        else
        {
            Debug.LogError("OSCManager reference is not set on InteractableItem script!");
        }

        // Provide feedback
        PlayFeedback();
    }

    private void PlayFeedback()
    {
        // Play sound if assigned
        if (interactionSound != null && !interactionSound.isPlaying)
        {
            interactionSound.Play();
        }
        
        // Play particles if assigned
        if (interactionParticles != null)
        {
            interactionParticles.Play();
        }
    }
}
