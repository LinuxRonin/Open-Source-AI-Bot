using UnityEngine;

namespace AI.Engine
{
    [CreateAssetMenu(fileName = "KnowledgeBase", menuName = "AI/KnowledgeBase", order = 1)]
    public class KnowledgeBase : ScriptableObject
    {
        [System.Serializable]
        public struct KnowledgeEntry
        {
            public string[] keywords;
            [TextArea(3, 10)]
            public string response;
        }

        public KnowledgeEntry[] entries;
    }
}