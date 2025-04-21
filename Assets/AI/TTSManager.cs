using UdonSharp;
using UnityEngine;
using System.Collections; // Required for IEnumerator
using System.Collections.Generic;
using System.IO; // Required for MemoryStream
using System;
using System.Linq; // May be needed for dictionary operations

// Conditional Namespaces for platform-specific TTS
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Speech.Synthesis; // Requires System.Speech.dll assembly reference in project
using NAudio.Wave; // Requires NAudio.dll assembly reference in project
#endif

namespace AI.Engine // Ensure this matches the folder structure or intended namespace
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class TTSManager : UdonSharpBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("The AudioSource component that will play the TTS audio.")]
        public AudioSource audioSource;

        [Header("TTS Settings (Windows Only)")]
        [Tooltip("Voice to use for TTS (platform-dependent, uses Windows SAPI). Index might vary.")]
        public int voiceIndex = 0; // Note: Selecting voice by index is fragile. Name/Gender is better if API allows.

        [Tooltip("Speech rate (0.5 = half speed, 1.0 = normal, 2.0 = double speed).")]
        [Range(0.5f, 2.0f)]
        public float speechRate = 1.0f;

        [Header("Lip Sync (Optional)")]
        [Tooltip("Animator component on the avatar model.")]
        public Animator avatarAnimator;
        [Tooltip("Name of the integer parameter in the Animator controller for visemes.")]
        public string visemeParameterName = "Viseme";
        [Tooltip("How often to update the viseme parameter (seconds). Smaller values = smoother but more checks.")]
        public float visemeUpdateInterval = 0.05f; // Reduced for potentially smoother sync

        [Header("Debug")]
        // FIX: Added missing debugMode variable
        [Tooltip("Enable debug logging for TTSManager.")]
        public bool debugMode = false;

        // Simple mapping from common English phonemes/letters to viseme IDs (adjust IDs based on your Animator)
        // This is a VERY basic approximation. Real phoneme extraction is complex.
        private readonly Dictionary<string, int> visemeMap = new Dictionary<string, int>()
        {
            // Silence
            {"sil", 0},
            // Vowel-like
            {"A", 1}, {"E", 2}, {"I", 2}, {"O", 3}, {"U", 9},
            // Bilabial (lips together)
            {"M", 4}, {"B", 4}, {"P", 4},
            // Labiodental (lip to teeth)
            {"F", 5}, {"V", 5},
            // Dental/Alveolar (tongue to teeth/ridge) - Grouped for simplicity
            {"TH", 6}, {"DH", 6},
            {"T", 7}, {"D", 7}, {"N", 7}, {"S", 7}, {"Z", 7}, {"SH", 7}, {"ZH", 7}, {"CH", 7}, {"JH", 7}, {"L", 7}, {"R", 7},
            // Velar (back of tongue)
            {"K", 8}, {"G", 8}, {"NG", 8},
            // Rounded/Other
            {"W", 9}, {"AW", 9}, {"OY", 9}
        };

        private string currentTextToSpeak;
        private int currentLipSyncCharIndex;
        private float nextVisemeUpdateTime;
        private bool isSpeaking = false;
        private AudioClip currentClip; // Store the generated clip to destroy later
        private Coroutine lipSyncCoroutine; // Store coroutine to stop it reliably

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (audioSource == null)
            {
                LogError("AudioSource is not assigned! TTS will be disabled.");
                enabled = false; // Disable the component if core requirement is missing
                return;
            }

            if (avatarAnimator == null)
            {
                LogWarning("Avatar Animator is not assigned. Lip sync will be disabled.");
            }
            // Ensure audioSource doesn't play on awake unless intended
            audioSource.playOnAwake = false;
        }

        /// <summary>
        /// Speaks the given text using platform-specific TTS and triggers lip sync.
        /// NOTE: Requires System.Speech.dll and NAudio.dll in the project for Windows Standalone/Editor.
        /// </summary>
        /// <param name="text">The text to speak.</param>
        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (debugMode) LogWarning("Received empty text to speak.");
                return;
            }

            // Stop any currently playing speech and lip sync
            StopSpeaking();

            currentTextToSpeak = text; // Store text for lip sync

// --- Platform Specific TTS ---
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // --- Windows TTS using System.Speech and NAudio ---
            try
            {
                // Initialize synthesizer (consider making this a class member if frequently used)
                using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
                {
                    // Attempt to select voice - this might need more robust selection based on installed voices
                    var voices = synthesizer.GetInstalledVoices().Where(v => v.Enabled).ToList();
                    if (voices.Count > 0) {
                        synthesizer.SelectVoice(voices[Mathf.Clamp(voiceIndex, 0, voices.Count - 1)].VoiceInfo.Name);
                    } else {
                        LogWarning("No enabled voices found for System.Speech.");
                    }

                    // Rate: SAPI uses -10 to 10. Map our 0.5-2.0 range.
                    // Rate = 0 is normal speed in SAPI.
                    // Rate = -10 is slowest, Rate = 10 is fastest.
                    // Linear mapping: sapiRate = (ourRate - 1.0f) * 10.0f
                    synthesizer.Rate = Mathf.Clamp((int)((speechRate - 1.0f) * 10.0f), -10, 10);
                    synthesizer.Volume = 100; // Max volume

                    if (debugMode) Debug.Log($"[TTSManager] Speaking: '{text}' with Rate: {synthesizer.Rate}");

                    // Generate audio into memory stream
                    using (MemoryStream stream = new MemoryStream())
                    {
                        synthesizer.SetOutputToWaveStream(stream);
                        synthesizer.Speak(text); // Synchronous speak call

                        if (stream.Length > 0)
                        {
                            stream.Position = 0; // Reset stream position for reading

                            // Convert MemoryStream (Wave format) to AudioClip using NAudio
                            // Ensure NAudio.dll is present in the project
                            using (WaveFileReader waveReader = new WaveFileReader(stream))
                            {
                                // Destroy previous clip if it exists
                                if (currentClip != null) Destroy(currentClip);

                                // Create AudioClip
                                currentClip = NAudioPlayer.FromWaveFileReader(waveReader); // Using a helper or direct conversion

                                if (currentClip != null)
                                {
                                     audioSource.clip = currentClip;
                                     audioSource.Play();
                                     isSpeaking = true;
                                     StartLipSync(text); // Start lip sync after successfully starting audio
                                } else {
                                     LogError("Failed to create AudioClip from TTS stream.");
                                }
                            }
                        }
                        else
                        {
                            LogWarning("TTS generated empty audio stream.");
                        }
                    } // Dispose MemoryStream
                } // Dispose SpeechSynthesizer
            }
            catch (PlatformNotSupportedException) {
                 LogError("System.Speech is not supported on this platform (requires Windows).");
            }
            catch (Exception e)
            {
                LogError($"Error during TTS generation or playback: {e.Message}\n{e.StackTrace}");
                // Ensure state is reset if error occurs
                isSpeaking = false;
                 if (currentClip != null) {
                    Destroy(currentClip);
                    currentClip = null;
                 }
            }
#elif UNITY_WEBGL && !UNITY_EDITOR
            // --- WebGL TTS (using browser's SpeechSynthesis API via Javascript interop) ---
            // This requires a Javascript library/plugin in your project to bridge the gap.
            // Example (pseudo-code, requires JS implementation):
            // Application.ExternalCall("window.unityTTS.speak", text, speechRate);
            // isSpeaking = true; // Assume JS handles playback
            // StartLipSync(text); // Lip sync might need timing from JS events
            LogError("TTSManager: WebGL TTS not implemented. Requires Javascript interop.");
            isSpeaking = false; // Can't speak

#else
            // --- Other Platforms (Android, iOS, Mac, Linux) ---
            // Require platform-specific TTS plugins or assets from the Unity Asset Store.
            LogError($"TTSManager: TTS not supported on this platform ({Application.platform}). Requires a specific plugin.");
            isSpeaking = false; // Can't speak
#endif
        }

        /// <summary>
        /// Stops the currently playing speech and lip sync.
        /// </summary>
        public void StopSpeaking()
        {
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
                // Reset viseme parameter if animator exists
                if (avatarAnimator != null)
                {
                    avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]); // Reset to silence
                }

                // Clean up generated AudioClip
                if (currentClip != null)
                {
                    // DestroyImmediate might be needed in editor, but Destroy is generally safer
                    Destroy(currentClip);
                    currentClip = null;
                }

                 if (debugMode) Debug.Log("[TTSManager] Stopped speaking.");
            }
        }

        private void StartLipSync(string text)
        {
            if (avatarAnimator == null || !isSpeaking) return; // Need animator and speech

            currentLipSyncCharIndex = 0;
            nextVisemeUpdateTime = Time.time; // Start updating immediately

            // Stop existing coroutine before starting a new one
            if (lipSyncCoroutine != null)
            {
                StopCoroutine(lipSyncCoroutine);
            }
            lipSyncCoroutine = StartCoroutine(UpdateVisemesCoroutine());
             if (debugMode) Debug.Log("[TTSManager] Started Lip Sync.");
        }

        // Coroutine for updating visemes based on simple character mapping
        private IEnumerator UpdateVisemesCoroutine()
        {
            // Keep running as long as audio is playing (or supposed to be playing)
            while (isSpeaking)
            {
                // Check if audio stopped unexpectedly
                if (audioSource != null && !audioSource.isPlaying && currentClip != null && audioSource.time >= currentClip.length - 0.1f) // Check if audio finished
                {
                     if (debugMode) Debug.Log("[TTSManager] Audio finished, stopping lip sync.");
                     isSpeaking = false; // Mark as not speaking
                     break; // Exit coroutine
                }


                if (Time.time >= nextVisemeUpdateTime)
                {
                    UpdateVisemeParameter();
                    nextVisemeUpdateTime = Time.time + visemeUpdateInterval;
                }
                yield return null; // Wait for the next frame
            }

             // Ensure viseme is reset to silence when done or stopped
             if (avatarAnimator != null)
             {
                 avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]);
             }
             lipSyncCoroutine = null; // Clear the stored coroutine reference
             if (debugMode) Debug.Log("[TTSManager] Lip Sync Coroutine finished.");
             StopSpeaking(); // Ensure full cleanup
        }


        // Updates the animator parameter based on the current character
        private void UpdateVisemeParameter()
        {
            if (avatarAnimator == null || !isSpeaking) return;

            // Estimate phoneme based on current character (very basic)
            string approxPhoneme = GetApproximatePhoneme();
            int visemeId = visemeMap["sil"]; // Default to silence

            if (visemeMap.TryGetValue(approxPhoneme, out visemeId))
            {
                avatarAnimator.SetInteger(visemeParameterName, visemeId);
            } else {
                 avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]); // Fallback
            }

            // Advance character index (simple approach, doesn't account for real speech timing)
            // A better approach would sync based on audio playback position vs estimated phoneme timings.
            currentLipSyncCharIndex++;
            if (currentLipSyncCharIndex >= currentTextToSpeak.Length) {
                 // Reached end of text, but audio might still be playing (e.g., silence at end)
                 // Keep coroutine running until audio stops or StopSpeaking is called.
                 // Optionally set to silence here if preferred:
                 // avatarAnimator.SetInteger(visemeParameterName, visemeMap["sil"]);
            }
        }

        // Very basic character-to-phoneme approximation
        private string GetApproximatePhoneme()
        {
            if (currentLipSyncCharIndex >= currentTextToSpeak.Length) return "sil";

            char c = char.ToUpper(currentTextToSpeak[currentLipSyncCharIndex]);

            // Simple direct mapping (expand as needed)
            switch (c)
            {
                case 'A': return "A";
                case 'E': case 'I': case 'Y': return "E";
                case 'O': return "O";
                case 'U': return "U";
                case 'M': case 'B': case 'P': return "M";
                case 'F': case 'V': return "F";
                case 'T': case 'D': case 'N': case 'S': case 'Z': case 'L': case 'R': return "T"; // Grouped
                case 'K': case 'G': case 'C': return "K"; // Approx C as K
                case 'W': return "W";
                case ' ': case '.': case ',': case '?': case '!': return "sil"; // Treat punctuation/space as silence
                default: return "sil"; // Default to silence for unmapped chars
            }
        }

        // Make sure to stop TTS and clean up when the object is destroyed
        private void OnDestroy()
        {
            StopSpeaking();
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TTSManager] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[TTSManager] {message}");
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // Helper class for NAudio conversion (if needed, or do it directly)
    // Ensure NAudio.dll is in your project
    public static class NAudioPlayer
    {
        public static AudioClip FromWaveFileReader(WaveFileReader reader)
        {
            // Based on NAudio documentation/examples for converting WaveStream to float array
            ISampleProvider sampleProvider = reader.ToSampleProvider();
            float[] buffer = new float[reader.Length / (reader.WaveFormat.BitsPerSample / 8)]; // Calculate samples needed
            int samplesRead = sampleProvider.Read(buffer, 0, buffer.Length);

            AudioClip audioClip = AudioClip.Create("TTS_Clip", samplesRead / reader.WaveFormat.Channels, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, false);
            audioClip.SetData(buffer, 0);
            return audioClip;
        }
    }
#endif
} // End namespace AI.Engine
