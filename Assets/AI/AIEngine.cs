using UdonSharp;
using UnityEngine;
using System.Text.RegularExpressions; // Required for Regex
using VRC.Udon;
using System; // Includes Linq, Collections.Generic, Tuple
using System.Collections.Generic; // Explicitly include for clarity
// using System.Linq; // Included via System

namespace AI.Engine // Ensure this matches the folder structure or intended namespace
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class AIEngine : UdonSharpBehaviour
    {
        [Header("Knowledge Base (Required)")]
        [Tooltip("Reference to the KnowledgeBase ScriptableObject asset. MUST be assigned.")]
        public KnowledgeBase knowledgeBase;

        [Header("AI Settings (Required)")]
        [Tooltip("Reference to the AIConfig ScriptableObject asset. MUST be assigned.")]
        public AIConfig aiConfig;

        [Header("Conversation Context (Optional)")]
        [Tooltip("Maximum number of turns (user message + bot response) to remember.")]
        [Range(1, 20)]
        public int conversationHistoryLength = 5; // Store 5 turns (10 messages)

        [Header("Debug")]
        [Tooltip("Enable detailed logging for AI processing.")]
        public bool debugMode = false;

        // Stores pairs of (User Message, Bot Response)
        private List<Tuple<string, string>> conversationHistory;
        private bool isInitialized = false;

        // Regex is potentially heavy, use static IsMatch which is generally safe in Udon.
        // Pre-compiling the Regex object itself (like wordBoundaryRegex below) might offer
        // minor gains but isn't strictly necessary for the current static IsMatch usage.
        // private static readonly Regex wordBoundaryRegex = new Regex(@"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // --- Critical Asset Checks ---
            // VRChat Best Practice: Ensure required assets are assigned in the Editor.
            // Do NOT create defaults at runtime as it hides setup errors.
            if (knowledgeBase == null)
            {
                LogError("Knowledge Base ScriptableObject is NOT assigned in the Inspector! AI Engine cannot function.");
                this.enabled = false; // Disable this component to prevent errors
                return;
            }
             if (aiConfig == null)
            {
                LogError("AI Config ScriptableObject is NOT assigned in the Inspector! AI Engine cannot function.");
                 this.enabled = false; // Disable this component
                return;
            }

            // Check if knowledge base has entries (warning, not disabling error)
            if (knowledgeBase.entries == null || knowledgeBase.entries.Length == 0) {
                 LogWarning("Knowledge Base is assigned but contains no entries. AI will only use default responses.");
            }

            // --- Initialize History ---
            // Ensure capacity is non-negative
            int historyCapacity = Mathf.Max(0, conversationHistoryLength);
            conversationHistory = new List<Tuple<string, string>>(historyCapacity);

            isInitialized = true;
            if (debugMode) Log("AIEngine Initialized successfully.");
        }

        /// <summary>
        /// Processes user input, finds the best response from the knowledge base,
        /// and updates conversation history. Returns a default response if no match is found
        /// or if the component is not initialized correctly.
        /// </summary>
        public string ProcessInput(string userInput)
        {
            // --- Initialization and Reference Checks ---
            // Ensure required components are assigned (already checked in Start, but good safety)
            if (!isInitialized || aiConfig == null || knowledgeBase == null) {
                 LogError("ProcessInput called but AIEngine is not initialized or core assets are missing.");
                 // Return a specific error message or a safe default
                 return "Sorry, the AI Engine is not configured correctly.";
            }

            // --- Handle Empty Input ---
            if (string.IsNullOrWhiteSpace(userInput))
            {
                if (debugMode) LogWarning("Received empty or whitespace input.");
                // Use the default response defined in the AIConfig asset
                return aiConfig.defaultResponse;
            }

            // --- Process Input ---
            string processedInput = userInput.Trim();
            if (debugMode) Log($"Processing input: '{processedInput}'");

            string response = FindResponse(processedInput);

            // Use default response if no specific match found
            if (string.IsNullOrEmpty(response))
            {
                response = aiConfig.defaultResponse;
                if (debugMode) Log($"No specific response found, using default: '{response}'");
            } else {
                if (debugMode) Log($"Found response: '{response}'");
            }

            // --- Update History ---
            UpdateConversationHistory(processedInput, response);

            if (debugMode) LogConversationHistory();

            return response;
        }

        /// <summary>
        /// Searches the knowledge base for the best matching entry based on keywords.
        /// </summary>
        private string FindResponse(string input)
        {
            // Safety checks (should be initialized, but belt-and-suspenders)
            if (knowledgeBase == null || knowledgeBase.entries == null || knowledgeBase.entries.Length == 0 || aiConfig == null)
            {
                if (debugMode) LogWarning("Knowledge Base or AIConfig missing/empty during FindResponse.");
                return null;
            }

            string bestMatchResponse = null;
            float bestScore = -1f; // Start below threshold

            // Prepare input for matching (lowercase, invariant culture)
            string lowerInput = input.ToLowerInvariant();

            // --- Iterate through Knowledge Base Entries ---
            for (int i = 0; i < knowledgeBase.entries.Length; i++)
            {
                KnowledgeBase.KnowledgeEntry entry = knowledgeBase.entries[i];

                // Skip entries that have no keywords defined
                if (entry.keywords == null || entry.keywords.Length == 0) continue;

                // Calculate match score for this entry
                float currentScore = CalculateKeywordMatchScore(lowerInput, entry.keywords);

                if (debugMode) {
                     // Avoid potential System.Linq reference for String.Join in log if problematic
                     // string keywordsString = "";
                     // for(int k=0; k < entry.keywords.Length; k++) keywordsString += (k > 0 ? ", " : "") + entry.keywords[k];
                     // Debug.Log($"Entry {i}, Keywords: [{keywordsString}], Score: {currentScore}");
                     // Using string.Join is generally fine in UdonSharp:
                     Debug.Log($"Entry {i}, Keywords: [{string.Join(", ", entry.keywords)}], Score: {currentScore}");
                }


                // Check if this is the best score so far AND meets the threshold
                if (currentScore > bestScore && currentScore >= aiConfig.keywordMatchThreshold)
                {
                    bestScore = currentScore;
                    bestMatchResponse = entry.response;

                    if (debugMode) Log($"New best match! Entry {i}, Score: {bestScore}, Response: '{bestMatchResponse}'");

                    // Optional Optimization: Early exit if a perfect score (1.0) is found
                    // if (bestScore >= 1.0f) break;
                }
            }

            return bestMatchResponse; // Returns null if no suitable match found
        }

        /// <summary>
        /// Calculates a match score (0-1) based on the percentage of keywords found in the input.
        /// Uses whole-word matching with Regex.
        /// </summary>
        private float CalculateKeywordMatchScore(string lowerInput, string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return 0f;

            int matchedKeywords = 0;
            int validKeywords = 0; // Count non-empty keywords

            for (int i = 0; i < keywords.Length; i++)
            {
                // Skip null or empty keywords in the definition
                if (string.IsNullOrWhiteSpace(keywords[i])) continue;

                validKeywords++; // This is a keyword we should check for
                string lowerKeyword = keywords[i].ToLowerInvariant().Trim();

                // --- Use static Regex.IsMatch for whole-word matching ---
                // Pattern: \b = word boundary, Regex.Escape handles special chars in keyword
                string pattern = @"\b" + Regex.Escape(lowerKeyword) + @"\b";

                try {
                    // Static Regex.IsMatch is generally safe in UdonSharp
                    if (Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase))
                    {
                        matchedKeywords++;
                        if (debugMode) Log($"Keyword '{lowerKeyword}' matched in input.");
                    }
                } catch (Exception e) {
                    // Log Regex errors if they occur, though unlikely for IsMatch with simple patterns
                    LogError($"Regex error matching keyword '{lowerKeyword}': {e.Message}");
                }
            }

            // Avoid division by zero if no valid keywords were defined
            if (validKeywords == 0) return 0f;

            // Normalize the score: (matched count) / (total *valid* keywords)
            return (float)matchedKeywords / validKeywords;
        }

        /// <summary>
        /// Adds the latest user message and bot response to the history, trimming old entries.
        /// </summary>
        private void UpdateConversationHistory(string userMessage, string botResponse)
        {
            // Ensure list exists (should be handled by Initialize)
            if (conversationHistory == null) conversationHistory = new List<Tuple<string, string>>();

            // Add the new turn (user message, bot response)
            conversationHistory.Add(Tuple.Create(userMessage, botResponse));

            // Remove oldest entries if history exceeds the desired length
            // Use conversationHistoryLength which is checked >= 0 in Initialize
            while (conversationHistory.Count > conversationHistoryLength)
            {
                conversationHistory.RemoveAt(0); // Remove the oldest turn (index 0)
            }
        }

        /// <summary>
        /// Gets the recent conversation history as a formatted multi-line string.
        /// </summary>
        public string GetConversationHistoryAsString()
        {
            if (conversationHistory == null || conversationHistory.Count == 0)
            {
                return "No conversation history yet.";
            }

            // Use StringBuilder for efficient string building in loops
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("--- Conversation History ---");
            foreach (var turn in conversationHistory) // Tuple<string, string>
            {
                sb.AppendLine($"User: {turn.Item1}"); // Item1 = userMessage
                sb.AppendLine($"Bot: {turn.Item2}");  // Item2 = botResponse
            }
            sb.AppendLine("--------------------------");
            return sb.ToString();
        }

        /// <summary>
        /// Logs the current conversation history to the Unity console if debugMode is enabled.
        /// </summary>
        private void LogConversationHistory()
        {
            if (!debugMode || conversationHistory == null) return;
            // Use the helper method to get the formatted string
            Log(GetConversationHistoryAsString());
        }

        // --- Centralized Logging Methods ---
        private void Log(string message) {
             Debug.Log($"[AIEngine] {message}");
         }
        private void LogError(string message) {
            Debug.LogError($"[AIEngine] {message}");
        }
         private void LogWarning(string message) {
            Debug.LogWarning($"[AIEngine] {message}");
        }
    }
} // End namespace AI.Engine

