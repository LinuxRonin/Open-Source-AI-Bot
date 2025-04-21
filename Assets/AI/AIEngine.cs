using UdonSharp;
using UnityEngine;
// using System.Collections.Generic; // Included via using System; below
// using System.Linq; // Included via using System; below
using System.Text.RegularExpressions; // Required for Regex
using VRC.Udon;
using System; // Includes Linq, Collections.Generic

namespace AI.Engine // Ensure this matches the folder structure or intended namespace
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class AIEngine : UdonSharpBehaviour
    {
        [Header("Knowledge Base")]
        [Tooltip("Reference to the KnowledgeBase ScriptableObject asset.")]
        public KnowledgeBase knowledgeBase;

        [Header("AI Settings")]
        [Tooltip("Reference to the AIConfig ScriptableObject asset.")]
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

        // Pre-compile Regex for word boundary matching (improves performance slightly)
        // We use the static Regex.IsMatch, so this isn't strictly necessary for the current logic,
        // but good practice if more complex regex operations were added.
        private static readonly Regex wordBoundaryRegex = new Regex(@"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Initialize conversation history list
            conversationHistory = new List<Tuple<string, string>>(conversationHistoryLength);

            if (knowledgeBase == null)
            {
                LogError("Knowledge Base ScriptableObject is not assigned!");
                // Consider disabling the component or providing a default state
            }
            else if (knowledgeBase.entries == null || knowledgeBase.entries.Length == 0) {
                 LogWarning("Knowledge Base is assigned but contains no entries.");
            }


            if (aiConfig == null)
            {
                LogError("AI Config ScriptableObject is not assigned! Using default settings.");
                // Create a temporary default config to prevent null reference errors
                aiConfig = ScriptableObject.CreateInstance<AIConfig>();
                // You might want to assign default values here if needed
                // aiConfig.defaultResponse = "Internal AI Error: Config missing.";
            }

             if (debugMode) Debug.Log("[AIEngine] Initialized.");
        }

        /// <summary>
        /// Processes user input, finds the best response from the knowledge base,
        /// and updates conversation history.
        /// </summary>
        /// <param name="userInput">The raw input string from the user.</param>
        /// <returns>The AI's response string.</returns>
        public string ProcessInput(string userInput)
        {
            if (aiConfig == null) {
                 LogError("AIConfig is missing, cannot process input.");
                 return "AI Error: Configuration missing.";
            }
             if (knowledgeBase == null) {
                 LogError("KnowledgeBase is missing, cannot process input.");
                 return aiConfig.defaultResponse; // Use default response from config
            }


            if (string.IsNullOrWhiteSpace(userInput))
            {
                if (debugMode) LogWarning("Received empty or whitespace input.");
                // Optionally return a specific response for empty input or just the default
                return aiConfig.defaultResponse;
            }

            string processedInput = userInput.Trim(); // Basic preprocessing
            if (debugMode) Debug.Log($"[AIEngine] Processing input: '{processedInput}'");

            string response = FindResponse(processedInput);

            // If no specific response found, use the default
            if (string.IsNullOrEmpty(response))
            {
                response = aiConfig.defaultResponse;
                 if (debugMode) Debug.Log($"[AIEngine] No specific response found, using default: '{response}'");
            } else {
                 if (debugMode) Debug.Log($"[AIEngine] Found response: '{response}'");
            }


            UpdateConversationHistory(processedInput, response);

            if (debugMode) LogConversationHistory();

            return response;
        }

        /// <summary>
        /// Searches the knowledge base for the best matching entry based on keywords.
        /// </summary>
        /// <param name="input">The processed user input string.</param>
        /// <returns>The response string from the best matching entry, or null if no match found.</returns>
        private string FindResponse(string input)
        {
             // Handled in Initialize and ProcessInput, but double-check
            if (knowledgeBase == null || knowledgeBase.entries == null || knowledgeBase.entries.Length == 0)
            {
                if (debugMode) LogWarning("Knowledge Base is missing or empty during FindResponse.");
                return null; // Return null instead of string.Empty for clarity
            }
             if (aiConfig == null) {
                 LogError("AIConfig missing during FindResponse.");
                 return null;
            }


            string bestMatchResponse = null;
            float bestScore = -1f; // Start below threshold

            string lowerInput = input.ToLowerInvariant(); // Use InvariantCulture for consistency

            // Iterate through all entries in the knowledge base
            for (int i = 0; i < knowledgeBase.entries.Length; i++)
            {
                KnowledgeBase.KnowledgeEntry entry = knowledgeBase.entries[i];

                // Skip entries with no keywords
                if (entry.keywords == null || entry.keywords.Length == 0) continue;

                // Calculate how well the input matches the keywords for this entry
                float currentScore = CalculateKeywordMatchScore(lowerInput, entry.keywords);

                 if (debugMode) Debug.Log($"[AIEngine] Entry {i}, Keywords: [{string.Join(", ", entry.keywords)}], Score: {currentScore}");


                // Check if this score is better than the best score found so far
                // AND meets the minimum threshold defined in AIConfig
                if (currentScore > bestScore && currentScore >= aiConfig.keywordMatchThreshold)
                {
                    bestScore = currentScore;
                    bestMatchResponse = entry.response;

                     if (debugMode) Debug.Log($"[AIEngine] New best match found! Entry {i}, Score: {bestScore}, Response: '{bestMatchResponse}'");

                    // Optional: Early exit if a perfect score (1.0) is found
                    // if (bestScore >= 1.0f) break;
                }
            }

            return bestMatchResponse; // Returns null if no match met the threshold
        }

        /// <summary>
        /// Calculates a match score based on how many keywords are present in the input.
        /// Score is normalized between 0 and 1 (percentage of keywords found).
        /// </summary>
        /// <param name="lowerInput">The user input converted to lowercase.</param>
        /// <param name="keywords">The array of keywords for a knowledge base entry.</param>
        /// <returns>A score between 0.0 and 1.0.</returns>
        private float CalculateKeywordMatchScore(string lowerInput, string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return 0f;

            int matchedKeywords = 0;
            for (int i = 0; i < keywords.Length; i++)
            {
                // Skip empty keywords in the definition
                if (string.IsNullOrWhiteSpace(keywords[i])) continue;

                string lowerKeyword = keywords[i].ToLowerInvariant().Trim();

                // FIX: Use static Regex.IsMatch correctly.
                // Check if the keyword exists as a whole word in the input.
                // The pattern looks for word boundaries (\b) around the keyword.
                // Regex.Escape handles special characters within the keyword.
                string pattern = @"\b" + Regex.Escape(lowerKeyword) + @"\b";

                // Use the static method: Regex.IsMatch(string input, string pattern, RegexOptions options)
                if (Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase)) // IgnoreCase might be redundant if input/keyword already lowercased
                {
                    matchedKeywords++;
                     if (debugMode) Debug.Log($"[AIEngine] Keyword '{lowerKeyword}' matched in input.");
                }
            }

            // Normalize the score: (number of matched keywords) / (total number of keywords)
            return (float)matchedKeywords / keywords.Length;
        }

        /// <summary>
        /// Adds the latest user message and bot response to the history,
        /// removing the oldest entry if the history exceeds the maximum length.
        /// </summary>
        private void UpdateConversationHistory(string userMessage, string botResponse)
        {
            if (conversationHistory == null) conversationHistory = new List<Tuple<string, string>>();

            // Add the new turn
            conversationHistory.Add(Tuple.Create(userMessage, botResponse));

            // Maintain history length limit
            while (conversationHistory.Count > conversationHistoryLength)
            {
                conversationHistory.RemoveAt(0); // Remove the oldest turn
            }
        }

        /// <summary>
        /// Gets the recent conversation history as a formatted string.
        /// </summary>
        /// <returns>A string containing the conversation history.</returns>
        public string GetConversationHistoryAsString() // Renamed for clarity
        {
            if (conversationHistory == null || conversationHistory.Count == 0)
            {
                return "No conversation history yet.";
            }

            // Use StringBuilder for efficient string concatenation
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("--- Conversation History ---");
            foreach (var turn in conversationHistory)
            {
                sb.AppendLine($"User: {turn.Item1}");
                sb.AppendLine($"Bot: {turn.Item2}");
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
            Debug.Log(GetConversationHistoryAsString()); // Use the formatted string method
        }

        // Centralized logging methods
        private void LogError(string message)
        {
            Debug.LogError($"[AIEngine] {message}");
        }
         private void LogWarning(string message)
        {
            Debug.LogWarning($"[AIEngine] {message}");
        }
    }
} // End namespace AI.Engine
