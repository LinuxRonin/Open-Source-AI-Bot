using UnityEngine;

namespace AI.Engine
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "AI/AIConfig", order = 2)]
    public class AIConfig : ScriptableObject
    {
        [Header("Keyword Matching")]
        [Tooltip("Minimum score (0-1) for a keyword match to be considered valid.")]
        [Range(0, 1)]
        public float keywordMatchThreshold = 0.5f;

        [Header("Responses")]
        [Tooltip("Default response if no matching keyword is found.")]
        [TextArea(3, 10)]
        public string defaultResponse = "I'm not sure I understand.";

        [Header("Personality")]
        [Tooltip("Prefix added to the bot's name in chat messages.")]
        public string botNamePrefix = "[Bot]: ";

        [Tooltip("Suffix added to the bot's name in chat messages.")]
        public string botNameSuffix = "";
    }
}