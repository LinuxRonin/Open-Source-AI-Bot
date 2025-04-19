using UdonSharp;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VRC.Udon;
using System;

namespace AI.Engine
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // Local only
    public class AIEngine : UdonSharpBehaviour
    {
        [Header("Knowledge Base")]
        public KnowledgeBase knowledgeBase;

        [Header("AI Settings")]
        public AIConfig aiConfig;

        [Header("Debug")]
        public bool debugMode = false;

        private Queue<string> conversationHistory = new Queue<string>(10); // Limit history

        private Regex wordBoundaryRegex = new Regex("\\b", RegexOptions.Compiled); // Pre-compile regex

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (knowledgeBase == null)
            {
                LogError("Knowledge Base is not assigned!");
                return;
            }

            if (aiConfig == null)
            {
                LogError("AI Config is not assigned!");
                return;
            }
        }

        public string ProcessInput(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                if (debugMode) Debug.LogWarning("[AIEngine] Received empty input.");
                return aiConfig.defaultResponse;
            }

            string response = FindResponse(userInput);
            if (string.IsNullOrEmpty(response))
            {
                response = aiConfig.defaultResponse;
            }

            UpdateConversationHistory(userInput, response);

            if (debugMode) LogConversationHistory();
            return response;
        }

        private string FindResponse(string input)
        {
            if (knowledgeBase == null || knowledgeBase.entries == null || knowledgeBase.entries.Length == 0)
            {
                LogError("Knowledge Base is not assigned or is empty!");
                return string.Empty;
            }

            string bestMatch = null;
            float bestScore = 0f;

            string lowerInput = input.ToLower();

            for (int i = 0; i < knowledgeBase.entries.Length; i++)
            {
                var entry = knowledgeBase.entries[i];
                if (entry.keywords == null || entry.keywords.Length == 0) continue;

                float score = CalculateKeywordMatchScore(lowerInput, entry.keywords);
                if (score > bestScore && score >= aiConfig.keywordMatchThreshold)
                {
                    bestScore = score;
                    bestMatch = entry.response;
                }

                if (bestScore >= 1f) break; // Perfect match, no need to continue
            }

            return bestMatch;
        }

        private float CalculateKeywordMatchScore(string input, string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return 0f;

            int matchedKeywords = 0;
            for (int i = 0; i < keywords.Length; i++)
            {
                // Use pre-compiled Regex for efficiency
                if (wordBoundaryRegex.IsMatch(input, "\\b" + Regex.Escape(keywords[i].ToLower()) + "\\b"))
                {
                    matchedKeywords++;
                }
            }
            return (float)matchedKeywords / keywords.Length;
        }

        private void UpdateConversationHistory(string userMessage, string botResponse)
        {
            conversationHistory.Enqueue($"[User]: {userMessage}");
            conversationHistory.Enqueue($"[Bot]: {botResponse}");

            if (conversationHistory.Count > 10)
            {
                conversationHistory.Dequeue();
                conversationHistory.Dequeue(); // Remove both user and bot messages
            }
        }

        public string GetConversationHistory()
        {
            return string.Join("\n", conversationHistory.ToArray());
        }

        private void LogConversationHistory()
        {
            Debug.Log("--- Conversation History ---");
            foreach (string line in conversationHistory)
            {
                Debug.Log(line);
            }
            Debug.Log("--------------------------");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AIEngine] {message}");
        }
    }
}