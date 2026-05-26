using UnityEditor;
using UnityEngine;

namespace LAuX.SimpleMix.EditorTools
{
    [CustomEditor(typeof(SimpleMixEventTrigger))]
    public class SimpleMixEventTriggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty directorProperty = serializedObject.FindProperty("director");
            SerializedProperty snapshotProperty = serializedObject.FindProperty("snapshotId");
            SerializedProperty sfxProperty = serializedObject.FindProperty("oneShotId");
            SerializedProperty playOnEnterProperty = serializedObject.FindProperty("playOnEnter");
            SerializedProperty playOnInteractProperty = serializedObject.FindProperty("playOnInteract");
            SimpleMixDirector director = directorProperty.objectReferenceValue as SimpleMixDirector;

            EditorGUILayout.PropertyField(directorProperty);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useSnapshot"), new GUIContent("Use Snapshot"));
                snapshotProperty.intValue = SimpleMixEditorUtility.DrawSnapshotIdPopup("Snapshot", director, snapshotProperty.intValue);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeSecondsOverride"), new GUIContent("Fade Seconds Override"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useOneShot"), new GUIContent("Use SFX"));
                sfxProperty.intValue = SimpleMixEditorUtility.DrawSfxIdPopup("SFX", director, sfxProperty.intValue);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Trigger Mode", EditorStyles.boldLabel);
                int mode = GetMode(playOnInteractProperty.boolValue, playOnEnterProperty.boolValue);
                string[] modes = { "Interact", "Player Enter", "Interact + Player Enter" };
                int newMode = EditorGUILayout.Popup("Mode", mode, modes);
                SetMode(newMode, playOnInteractProperty, playOnEnterProperty);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"), new GUIContent("Trigger Once"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownSeconds"), new GUIContent("Cooldown Seconds"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static int GetMode(bool playOnInteract, bool playOnEnter)
        {
            if (playOnInteract && playOnEnter)
            {
                return 2;
            }

            return playOnEnter ? 1 : 0;
        }

        private static void SetMode(int mode, SerializedProperty playOnInteractProperty, SerializedProperty playOnEnterProperty)
        {
            playOnInteractProperty.boolValue = mode == 0 || mode == 2;
            playOnEnterProperty.boolValue = mode == 1 || mode == 2;
        }
    }
}
