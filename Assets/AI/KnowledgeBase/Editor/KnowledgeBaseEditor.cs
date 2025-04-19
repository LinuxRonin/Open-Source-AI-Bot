#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace AI.Engine.Editor
{
    [CustomEditor(typeof(KnowledgeBase))]
    public class KnowledgeBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty entriesProp;
        private ReorderableList reorderableList;
        private Vector2 scrollPos;

        private void OnEnable()
        {
            entriesProp = serializedObject.FindProperty("entries");

            reorderableList = new ReorderableList(serializedObject, entriesProp, true, true, true, true);
            reorderableList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Knowledge Base Entries");
            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = entriesProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                float singleLineHeight = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, singleLineHeight),
                                    element.FindPropertyRelative("keywords"), GUIContent.none);
                rect.y += singleLineHeight + 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, singleLineHeight * 3),
                                    element.FindPropertyRelative("response"), GUIContent.none);
            };
            reorderableList.elementHeightCallback = (int index) => {
                return EditorGUIUtility.singleLineHeight * 4 + 4;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (entriesProp != null && entriesProp.isArray)
            {
                reorderableList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif