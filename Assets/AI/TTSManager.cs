using UdonSharp;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace AI.Engine
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class TTSManager : UdonSharpBehaviour
    {
        [Header("Audio Source")]
        public AudioSource audioSource;

        [Header("TTS Settings")]
        [Tooltip("Voice to use for TTS (platform-dependent).")]
        public int voiceIndex = 0;

        [Tooltip("Speech rate (platform-dependent).")]
        [Range(0.5f, 2.0f)]
        public float speechRate = 1.0f;

        [Header("Lip Sync")]
        public Animator avatarAnimator;
        public string visemeParameterName = "Viseme"; // Animator parameter name
        public float visemeUpdateInterval = 0.1f;

        private Dictionary<string, int> visemeMap = new Dictionary<string, int>()
        {
            {"A", 1}, {"E", 2}, {"O", 3},  // Vowels
            {"M", 4}, {"B", 4}, {"P", 4},  // Bilabial
            {"F", 5}, {"V", 5},            // Labiodental
            {"TH", 6}, {"DH", 6},          // Dental
            {"T", 7}, {"D", 7}, {"N", 7}, {"S", 7}, {"Z", 7}, {"SH", 7}, {"ZH", 7}, {"CH", 7}, {"JH", 7}, {"L", 7}, {"R", 7}, // Alveolar/etc.
            {"K", 8}, {"G", 8}, {"NG", 8}, // Velar
            {"W", 9}, {"UH", 9}, {"AW", 9}, // Rounded
            {"sil", 0}                    // Silence
        };

        private string currentText;
        private int currentCharacterIndex;
        private float nextVisemeUpdateTime;
        private bool isSpeaking = false;
        private AudioClip currentClip;
        private float clipLength;
        private float elapsedTime;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (audioSource == null)
            {
                LogError("AudioSource is not assigned!");
                enabled = false;
                return;
            }

            if (avatarAnimator == null)
            {
                LogWarning("Avatar Animator is not assigned. Lip sync will be disabled.");
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (debugMode) Debug.LogWarning("[TTSManager] Received empty text to speak.");
                return;
            }

            StopSpeaking();

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL doesn't support System.Speech
            LogError("TTS is not supported in WebGL!");
            return;
#endif

#if UNITY_STANDALONE || UNITY_EDITOR
            try
            {
                // Initialize synthesizer (moved here for lazy initialization)
                var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                synthesizer.SelectVoiceByHints(System.Speech.Synthesis.VoiceGender.Male); // Or Female, or Neutral
                synthesizer.Rate = (int)((speechRate - 1.0f) * 10.0f); // Convert float to int range
                synthesizer.Volume = 100;

                // Generate and play audio
                MemoryStream stream = new MemoryStream();
                synthesizer.SetOutputToWaveStream(stream);
                synthesizer.Speak(text);
                stream.Position = 0;
                currentClip = NAudio.Wave.WaveExtensions.ToAudioClip(new NAudio.Wave.WaveFileReader(stream));
                audioSource.clip = currentClip;
                audioSource.Play();
                clipLength = currentClip.length;
                elapsedTime = 0f;
                synthesizer.Dispose(); // Release resources

                // Lip Sync Setup
                StartLipSync(text);
            }
            catch (Exception e)
            {
                LogError($"Error initializing or using System.Speech: {e}");
            }
#endif
        }

        private void StopSpeaking()
        {
            if (isSpeaking)
            {
                audioSource.Stop();
                isSpeaking = false;
                if (currentClip != null)
                {
                    Destroy(currentClip); // Release memory
                    currentClip = null;
                }
                StopAllCoroutines();
            }
        }

        private void StartLipSync(string text)
        {
            currentText = text;
            currentCharacterIndex = 0;
            nextVisemeUpdateTime = Time.time;
            isSpeaking = true;
            if (avatarAnimator != null)
            {
                StopAllCoroutines();
                StartCoroutine(UpdateVisemes());
            }
        }

        private IEnumerator UpdateVisemes()
        {
            while (currentCharacterIndex < currentText.Length && isSpeaking)
            {
                if (Time.time >= nextVisemeUpdateTime)
                {
                    UpdateVisemeParameter();
                    nextVisemeUpdateTime = Time.time + visemeUpdateInterval;
                }
                yield return null;
            }
            isSpeaking = false;
            if (avatarAnimator != null)
            {
                avatarAnimator.SetInteger(visemeParameterName, 0); // Reset to silence
            }
        }

        private void UpdateVisemeParameter()
        {
            if (avatarAnimator == null) return;
            if (!audioSource.isPlaying)
            {
                isSpeaking = false;
                return;
            }

            // Very basic phoneme-to-viseme mapping
            string phoneme = GetCurrentPhoneme();
            if (visemeMap.ContainsKey(phoneme))
            {
                avatarAnimator.SetInteger(visemeParameterName, visemeMap[phoneme]);
            }
            currentCharacterIndex++;
        }

        private string GetCurrentPhoneme()
        {
            if (currentCharacterIndex >= currentText.Length) return "sil";

            char c = currentText[currentCharacterIndex];
            switch (c)
            {
                case 'a':
                case 'A': return "A";
                case 'e':
                case 'E': return "E";
                case 'o':
                case 'O': return "O";
                case 'm':
                case 'M':
                case 'b':
                case 'B':
                case 'p':
                case 'P': return "M";
                case 'f':
                case 'F':
                case 'v':
                case 'V': return "F";
                case 't':
                case 'T':
                case 'd':
                case 'D':
                case 'n':
                case 'N':
                case 's':
                case 'S':
                case 'z':
                case 'Z':
                case 'l':
                case 'L':
                case 'r':
                case 'R': return "T";
                case 'k':
                case 'K':
                case 'g':
                case 'G': return "K";
                case 'w':
                case 'W':
                case 'u':
                case 'U': return "W";
                case 'i':
                case 'I':
                case 'y':
                case 'Y': return "E";
                case 'h':
                case 'H': return "sil";
                default: return "sil";
            }
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
}