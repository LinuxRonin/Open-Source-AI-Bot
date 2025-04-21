using UdonSharp;
using UnityEngine;

// --- IMPORTANT VRChat / UdonSharp Notes ---
// 1. System.Speech & NAudio are NOT included with Unity/VRChat and rely on Windows-specific features.
// 2. This means TTS WILL NOT WORK on Quest, Mac, Linux, or potentially even some Windows PC builds
//    if the required DLLs (System.Speech.dll, NAudio.dll) are not correctly included AND loaded.
// 3. Including external DLLs in VRChat worlds can be unreliable and is generally discouraged.
// 4. THEREFORE: This script is modified to ONLY attempt TTS functionality within the
//    Windows Unity Editor (#if UNITY_EDITOR_WIN). In builds or other platforms, Speak() will do nothing.
// 5. Lip Sync is also disabled outside the Windows Editor as it relies on TTS starting.
// 6. If you need TTS in VRChat, you would need a completely different approach (e.g., web service, Asset Store plugin designed for VRChat).

// Conditional Namespaces - Only include these in Windows Editor context
#if UNITY_EDITOR_WIN
using System.Collections; // Required for IEnumerator
using System.Collections.Generic;
using System.IO; // Required for MemoryStream
using System;
using System.Linq;
using System.Speech.Synthesis; // Requires System.Speech.dll assembly reference in project (Assets/Plugins)
using NAudio.Wave; // Requires NAudio.dll assembly reference in project (Assets/Plugins)
#endif

namespace AI.Engine // Ensure this matches the folder structure or intended namespace
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class TTSManager : UdonSharpBehaviour
    {
        [Header("Audio Source (Required)")]
        [Tooltip("The AudioSource component that will play the TTS audio *in the Windows Editor only*.")]
        public AudioSource audioSource;

        #if UNITY_EDITOR_WIN
        // --- Settings relevant only for Windows Editor TTS ---
        [Header("TTS Settings (Windows Editor Only)")]
        [Tooltip("Voice to use for TTS (platform-dependent, uses Windows SAPI). Index might vary.")]
        public int voiceIndex = 0;
        [Tooltip("Speech rate (0.5 = half speed, 1.0 = normal, 2.0 = double speed).")]
        [Range(0.5f, 2.0f)]
        public float speechRate = 1.0f;

        [Header("Lip Sync (Optional, Windows Editor Only)")]
        [Tooltip("Animator component on the avatar model.")]
        public Animator avatarAnimator;
        [Tooltip("Name of the integer parameter in the Animator controller for visemes.")]
        public string visemeParameterName = "Viseme";
        [Tooltip("How often to update the viseme parameter (seconds).")]
        public float visemeUpdateInterval = 0.05f;

        // Simple mapping from common English letters/sounds to viseme IDs
        private readonly Dictionary<string, int> visemeMap = new Dictionary<string, int>()
        {
            {"sil", 0}, {"A", 1}, {"E", 2}, {"I", 2}, {"O", 3}, {"U", 9},
            {"M", 4}, {"B", 4}, {"P", 4}, {"F", 5}, {"V", 5}, {"TH", 6}, {"DH", 6},
            {"T", 7}, {"D", 7}, {"N", 7}, {"S", 7}, {"Z", 7}, {"SH", 7}, {"ZH", 7}, {"CH", 7}, {"JH", 7}, {"L", 7}, {"R", 7},
            {"K", 8}, {"G", 8}, {"NG", 8}, {"W", 9}, {"AW", 9}, {"OY", 9}
            // Add more mappings based on your animator's viseme setup
        };

        private string currentTextToSpeak;
        private int currentLipSyncCharIndex;
        private float nextVisemeUpdateTime;
        private bool isSpeaking = false; // Tracks if TTS is *supposedly* active (Editor only)
        private AudioClip currentClip; // Store the generated clip to destroy later
        private Coroutine lipSyncCoroutine; // Store coroutine to stop it reliably
        private SpeechSynthesizer synthesizer; // Keep synthesizer instance for reuse? Check disposal needs.
        #endif

        [Header("Debug")]
        [Tooltip("Enable debug logging for TTSManager.")]
        public bool debugMode = false;


        void Start()
        {
            // Basic initialization needed regardless of platform
            if (audioSource == null)
            {
                LogError("AudioSource is not assigned! TTSManager cannot function.");
                enabled = false; // Disable the component
                return;
            }
            audioSource.playOnAwake = false; // Ensure it doesn't play automatically

            #if UNITY_EDITOR_WIN
                // Editor-only initialization
                if (avatarAnimator == null) {
                    LogWarning("Avatar Animator is not assigned. Lip sync will be disabled (even in Editor).");
                }
                // Pre-initialize synthesizer? Or create on demand in Speak()?
                // Consider potential overhead vs. initialization delay. For now, create on demand.
                // synthesizer = new SpeechSynthesizer();
            #else
                // Log warning if running outside Windows Editor context
                LogWarning("TTS functionality is disabled outside of Windows Editor.");
            #endif
        }

        /// <summary>
        /// Attempts to speak the given text using Windows SAPI TTS.
        /// IMPORTANT: This will ONLY work in the Windows Unity Editor and requires
        /// System.Speech.dll and NAudio.dll to be present in Assets/Plugins.
        /// In VRChat builds or other platforms, this method does nothing.
        /// </summary>
        /// <param name="text">The text to attempt to speak.</param>
        public void Speak(string text)
        {
            // --- Gatekeep: Only run TTS logic in Windows Editor ---
            #if UNITY_EDITOR_WIN
            if (audioSource == null) return; // Already checked in Start, but good practice

            if (string.IsNullOrEmpty(text))
            {
                if (debugMode) LogWarning("Received empty text to speak.");
                return;
            }

            // Stop any previous speech/lip sync first
            StopSpeaking(); // Call the Windows Editor specific StopSpeaking

            currentTextToSpeak = text; // Store for lip sync

            try
            {
                // --- Synthesizer Initialization (On Demand) ---
                // Using 'using' ensures disposal even if errors occur
                using (synthesizer = new SpeechSynthesizer())
                {
                    // --- Voice Selection ---
                    try {
                         var voices = synthesizer.GetInstalledVoices().Where(v => v.Enabled).ToList();
                         if (voices.Count > 0) {
                             synthesizer.SelectVoice(voices[Mathf.Clamp(voiceIndex, 0, voices.Count - 1)].VoiceInfo.Name);
                         } else {
                             LogWarning("No enabled voices found for System.Speech.");
                         }
                    } catch (Exception e) {
                        LogWarning($"Failed to select voice (using default): {e.Message}");
                    }

                    // --- Rate & Volume ---
                    synthesizer.Rate = Mathf.Clamp((int)((speechRate - 1.0f) * 10.0f), -10, 10);
                    synthesizer.Volume = 100;

                    if (debugMode) Debug.Log($"[TTSManager EDITOR] Attempting to speak: '{text}' Rate: {synthesizer.Rate}");

                    // --- Generate Audio Stream ---
                    using (MemoryStream stream = new MemoryStream())
                    {
                        synthesizer.SetOutputToWaveStream(stream);
                        synthesizer.Speak(text); // Synchronous call

                        if (stream.Length > 0)
                        {
                            stream.Position = 0;
                            // --- Convert Stream to AudioClip using NAudio ---
                            using (WaveFileReader waveReader = new WaveFileReader(stream))
                            {
                                if (currentClip != null) DestroyImmediate(currentClip); // Use DestroyImmediate in Editor

                                // Helper function or direct code to convert WaveFileReader to AudioClip
                                currentClip = NAudioToAudioClip.FromWaveFileReader(waveReader, "TTS_Clip_Editor");

                                if (currentClip != null)
                                {
                                     audioSource.clip = currentClip;
                                     audioSource.Play();
                                     isSpeaking = true;
                                     StartLipSync(text); // Start lip sync only if audio plays
                                } else {
                                     LogError("Failed to create AudioClip from TTS stream (NAudio).");
                                     isSpeaking = false;
                                }
                            }
                        }
                        else {
                            LogWarning("TTS generated empty audio stream.");
                            isSpeaking = false;
                        }
                    } // Dispose MemoryStream
                } // Dispose SpeechSynthesizer
                synthesizer = null; // Clear reference after disposal
            }
            catch (PlatformNotSupportedException) {
                 LogError("System.Speech is not supported on this platform (Requires Windows Editor). Ensure DLLs are present.");
                 isSpeaking = false;
            }
            catch (Exception e)
            {
                LogError($"Error during TTS generation/playback (Editor): {e.Message}\n{e.StackTrace}");
                isSpeaking = false; // Ensure state is reset
                 if (currentClip != null) {
                     DestroyImmediate(currentClip); // Use DestroyImmediate in Editor
                     currentClip = null;
                 }
            }

            #else
            // --- If not in Windows Editor ---
            if (debugMode) Debug.Log("[TTSManager] Speak called, but TTS is disabled outside Windows Editor.");
            // Do nothing in builds or other platforms
            #endif
        }

        /// <summary>
        /// Stops the currently playing TTS audio and lip sync.
        /// IMPORTANT: Only functional within the Windows Unity Editor.
        /// </summary>
        public void StopSpeaking()
        {
             #if UNITY_EDITOR_WIN
             if (isSpeaking)
             {
                 if (audioSource != null && audioSource.isPlaying)
                 {
                     audioSource.Stop();
                 }
                 isSpeaking = false;

                 // Stop lip sync coroutine
                 if (lipSyncCoroutine != null)
                 {
                     StopCoroutine(lipSyncCoroutine);
                     lipSyncCoroutine = null;
                 }
                 // Reset viseme parameter
                 if (avatarAnimator != null)
                 {
                     // Check if parameter exists before setting
                     if (Array.Exists(avatarAnimator.parameters, param => param.name == visemeParameterName && param.type == AnimatorControllerParameterType.Integer)) {
                        avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]);
                     }
                 }

                 // Clean up generated AudioClip
                 if (currentClip != null)
                 {
                     DestroyImmediate(currentClip); // Use DestroyImmediate in Editor
                     currentClip = null;
                 }

                  if (debugMode) Debug.Log("[TTSManager EDITOR] Stopped speaking.");
             }
             #endif
             // No action needed outside Windows editor
        }

        #if UNITY_EDITOR_WIN // --- Lip Sync Logic (Windows Editor Only) ---
        private void StartLipSync(string text)
        {
            if (avatarAnimator == null || !isSpeaking) return;

            // Check if viseme parameter exists
            if (!Array.Exists(avatarAnimator.parameters, param => param.name == visemeParameterName && param.type == AnimatorControllerParameterType.Integer)) {
                LogWarning($"Viseme parameter '{visemeParameterName}' not found or not an Integer in Animator. Lip sync disabled.");
                return;
            }


            currentLipSyncCharIndex = 0;
            nextVisemeUpdateTime = Time.time;

            if (lipSyncCoroutine != null) StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = StartCoroutine(UpdateVisemesCoroutine());
             if (debugMode) Debug.Log("[TTSManager EDITOR] Started Lip Sync.");
        }

        private IEnumerator UpdateVisemesCoroutine()
        {
            while (isSpeaking)
            {
                // Check if audio stopped playing
                if (audioSource != null && !audioSource.isPlaying)
                {
                     // Small delay to ensure it wasn't just a frame skip
                     yield return new WaitForSeconds(0.1f);
                     if (audioSource != null && !audioSource.isPlaying) {
                         if (debugMode) Debug.Log("[TTSManager EDITOR] Audio stopped, stopping lip sync.");
                         isSpeaking = false;
                         break; // Exit coroutine
                     }
                }

                if (Time.time >= nextVisemeUpdateTime)
                {
                    UpdateVisemeParameter();
                    nextVisemeUpdateTime = Time.time + visemeUpdateInterval;
                }
                yield return null; // Wait for the next frame
            }

             // Ensure viseme is reset when done
             if (avatarAnimator != null && Array.Exists(avatarAnimator.parameters, param => param.name == visemeParameterName))
             {
                 avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]);
             }
             lipSyncCoroutine = null;
             if (debugMode) Debug.Log("[TTSManager EDITOR] Lip Sync Coroutine finished.");
             // Call StopSpeaking again to ensure full cleanup, just in case
             StopSpeaking();
        }

        private void UpdateVisemeParameter()
        {
            if (avatarAnimator == null || !isSpeaking) return;

            string approxPhoneme = GetApproximatePhoneme();
            int visemeId = visemeMap["sil"]; // Default to silence

            if (visemeMap.TryGetValue(approxPhoneme, out visemeId))
            {
                 if (Array.Exists(avatarAnimator.parameters, param => param.name == visemeParameterName)) { // Check again just before setting
                    avatarAnimator.SetInteger(visemeParameterName, visemeId);
                 }
            }

            currentLipSyncCharIndex++;
            // No need to explicitly stop here, coroutine checks isSpeaking flag
        }

        // Very basic character-to-phoneme approximation
        private string GetApproximatePhoneme()
        {
            if (currentLipSyncCharIndex >= currentTextToSpeak.Length) return "sil";
            char c = char.ToUpper(currentTextToSpeak[currentLipSyncCharIndex]);
            switch (c) { // Simplified mapping
                case 'A': return "A"; case 'E': case 'I': case 'Y': return "E"; case 'O': return "O"; case 'U': return "U";
                case 'M': case 'B': case 'P': return "M"; case 'F': case 'V': return "F";
                case 'T': case 'D': case 'N': case 'S': case 'Z': case 'L': case 'R': return "T";
                case 'K': case 'G': case 'C': return "K"; case 'W': return "W";
                case ' ': case '.': case ',': case '?': case '!': return "sil";
                default: return "sil";
            }
        }

        // Ensure cleanup when the object is destroyed or disabled in editor
        private void OnDisable() {
            StopSpeaking();
        }
        private void OnDestroy() {
             StopSpeaking();
             // Dispose synthesizer if it was kept as a member?
             // if(synthesizer != null) synthesizer.Dispose();
        }

        #endif // End of UNITY_EDITOR_WIN block

        // --- Logging (Available on all platforms) ---
        private void LogError(string message) { Debug.LogError($"[TTSManager] {message}"); }
        private void LogWarning(string message) { Debug.LogWarning($"[TTSManager] {message}"); }
    } // End Class


    #if UNITY_EDITOR_WIN
    // --- Helper Class for NAudio to AudioClip Conversion (Editor Only) ---
    // Place this outside the main class or in a separate utility script if preferred
    public static class NAudioToAudioClip
    {
        /// <summary>
        /// Creates a Unity AudioClip from an NAudio WaveFileReader.
        /// Make sure the WaveFileReader is disposed after calling this.
        /// </summary>
        public static AudioClip FromWaveFileReader(WaveFileReader reader, string clipName = "NAudioClip")
        {
            if (reader == null) return null;

            // Get audio data as float samples
            ISampleProvider sampleProvider = reader.ToSampleProvider();
            int channels = sampleProvider.WaveFormat.Channels;
            int sampleRate = sampleProvider.WaveFormat.SampleRate;
            long totalSamplesLong = reader.SampleCount * channels; // Total float values needed

            // Check for excessively large files (e.g., > 500MB) to prevent memory issues
             if (totalSamplesLong > 100_000_000) { // Approx 400-500MB for float[]
                 Debug.LogError("[NAudioToAudioClip] Audio file is too large to load into memory as AudioClip.");
                 return null;
             }
             int totalSamples = (int)totalSamplesLong;


            float[] buffer = new float[totalSamples];
            int samplesRead = sampleProvider.Read(buffer, 0, totalSamples);

            if (samplesRead <= 0) {
                 Debug.LogError("[NAudioToAudioClip] Failed to read samples from WaveFileReader.");
                 return null;
            }

            // Create AudioClip
            AudioClip audioClip = AudioClip.Create(clipName,
                                                   samplesRead / channels, // Length in samples per channel
                                                   channels,
                                                   sampleRate,
                                                   false); // false = not streaming

            // Set data
            audioClip.SetData(buffer, 0);
            return audioClip;
        }
    }
    #endif

} // End namespace AI.Engine
