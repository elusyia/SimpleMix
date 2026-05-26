using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LAuX.SimpleMix.EditorTools
{
    [CustomEditor(typeof(SimpleMixZoneTrigger))]
    public class SimpleMixZoneTriggerEditor : Editor
    {
        private const float MinZoneSize = 0.05f;
        private const float ConeHandleScale = 0.2f;

        public override void OnInspectorGUI()
        {
            SimpleMixZoneTrigger trigger = (SimpleMixZoneTrigger)target;

            serializedObject.Update();
            SerializedProperty directorProperty = serializedObject.FindProperty("director");
            EditorGUILayout.PropertyField(directorProperty);
            SimpleMixDirector director = directorProperty.objectReferenceValue as SimpleMixDirector;

            SerializedProperty enterSnapshotProperty = serializedObject.FindProperty("enterSnapshotId");
            SerializedProperty exitSnapshotProperty = serializedObject.FindProperty("exitSnapshotId");
            SerializedProperty enterSfxProperty = serializedObject.FindProperty("enterOneShotId");
            SerializedProperty exitSfxProperty = serializedObject.FindProperty("exitOneShotId");

            enterSnapshotProperty.intValue = SimpleMixEditorUtility.DrawSnapshotIdPopup("Enter Snapshot", director, enterSnapshotProperty.intValue);
            exitSnapshotProperty.intValue = SimpleMixEditorUtility.DrawSnapshotIdPopup("Exit Snapshot", director, exitSnapshotProperty.intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useExitSnapshot"), new GUIContent("Use Exit Snapshot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeSecondsOverride"), new GUIContent("Fade Seconds Override"));
            enterSfxProperty.intValue = SimpleMixEditorUtility.DrawSfxIdPopup("Enter SFX", director, enterSfxProperty.intValue);
            exitSfxProperty.intValue = SimpleMixEditorUtility.DrawSfxIdPopup("Exit SFX", director, exitSfxProperty.intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerOnce"), new GUIContent("Trigger Once"));
            serializedObject.ApplyModifiedProperties();

            BoxCollider box = trigger.GetComponent<BoxCollider>();
            if (box == null)
            {
                EditorGUILayout.HelpBox("Scene handles require a BoxCollider trigger on this GameObject.", MessageType.Warning);
                if (GUILayout.Button("Add BoxCollider Trigger"))
                {
                    box = Undo.AddComponent<BoxCollider>(trigger.gameObject);
                    box.isTrigger = true;
                    EditorUtility.SetDirty(box);
                    EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
                }
            }
            else if (!box.isTrigger)
            {
                EditorGUILayout.HelpBox("This BoxCollider is not marked as trigger.", MessageType.Warning);
                if (GUILayout.Button("Set BoxCollider Is Trigger"))
                {
                    Undo.RecordObject(box, "Set SimpleMix Zone Trigger");
                    box.isTrigger = true;
                    EditorUtility.SetDirty(box);
                    EditorSceneManager.MarkSceneDirty(trigger.gameObject.scene);
                }
            }
        }

        private void OnSceneGUI()
        {
            SimpleMixZoneTrigger trigger = (SimpleMixZoneTrigger)target;
            DrawZoneSceneGUI(trigger, new Color(0.2f, 0.75f, 1f, 1f), true);
        }

        internal static void DrawZoneSceneGUI(SimpleMixZoneTrigger trigger, Color zoneColor, bool drawHandles)
        {
            if (trigger == null)
            {
                return;
            }

            BoxCollider box = trigger.GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Transform transform = box.transform;
            Color outline = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.95f);
            Color fill = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.08f);

            Matrix4x4 oldMatrix = Handles.matrix;
            Color oldColor = Handles.color;

            Handles.matrix = transform.localToWorldMatrix;
            Handles.color = fill;
            DrawSolidBox(box.center, box.size);
            Handles.color = outline;
            Handles.DrawWireCube(box.center, box.size);
            Handles.matrix = oldMatrix;

            DrawZoneLabel(trigger, box);
            if (drawHandles)
            {
                DrawResizeHandle(box, Vector3.right);
                DrawResizeHandle(box, Vector3.left);
                DrawResizeHandle(box, Vector3.up);
                DrawResizeHandle(box, Vector3.down);
                DrawResizeHandle(box, Vector3.forward);
                DrawResizeHandle(box, Vector3.back);
            }

            Handles.color = oldColor;
        }

        private static void DrawZoneLabel(SimpleMixZoneTrigger trigger, BoxCollider box)
        {
            Vector3 top = box.center + Vector3.up * (box.size.y * 0.5f + 0.15f);
            Vector3 worldTop = box.transform.TransformPoint(top);

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.normal.textColor = Color.white;
            style.fontSize = 11;
            style.alignment = TextAnchor.MiddleLeft;

            string label = trigger.name + "\n";
            label += "Enter: " + SimpleMixEditorUtility.GetSnapshotLabel(trigger.director, trigger.enterSnapshotId);
            if (trigger.useExitSnapshot)
            {
                label += "\nExit: " + SimpleMixEditorUtility.GetSnapshotLabel(trigger.director, trigger.exitSnapshotId);
            }
            else
            {
                label += "\nExit: Disabled";
            }

            label += "\nFade: " + (trigger.fadeSecondsOverride < 0f ? "Default" : trigger.fadeSecondsOverride.ToString("0.###") + "s");
            if (trigger.enterOneShotId >= 0 || trigger.exitOneShotId >= 0)
            {
                label += "\nSFX: " + SimpleMixEditorUtility.GetSfxLabel(trigger.director, trigger.enterOneShotId) +
                    " / " + SimpleMixEditorUtility.GetSfxLabel(trigger.director, trigger.exitOneShotId);
            }

            Handles.Label(worldTop, label, style);
        }

        private static void DrawResizeHandle(BoxCollider box, Vector3 localNormal)
        {
            Transform transform = box.transform;
            Vector3 size = box.size;
            Vector3 center = box.center;
            Vector3 extents = size * 0.5f;

            float localExtent = Mathf.Abs(Vector3.Dot(extents, Abs(localNormal)));
            Vector3 localFaceCenter = center + localNormal * localExtent;
            Vector3 worldFaceCenter = transform.TransformPoint(localFaceCenter);
            Vector3 worldDirection = transform.TransformDirection(localNormal).normalized;
            float handleSize = HandleUtility.GetHandleSize(worldFaceCenter) * ConeHandleScale;

            Color oldColor = Handles.color;
            Handles.color = GetResizeHandleColor(localNormal);
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldFaceCenter = Handles.Slider(worldFaceCenter, worldDirection, handleSize, Handles.ConeHandleCap, 0f);
            Handles.color = oldColor;
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Vector3 localDelta = transform.InverseTransformVector(newWorldFaceCenter - worldFaceCenter);
            float signedDelta = Vector3.Dot(localDelta, localNormal);
            int axis = GetAxis(localNormal);
            float currentSize = GetAxisValue(size, axis);
            float newSize = Mathf.Max(MinZoneSize, currentSize + signedDelta);
            signedDelta = newSize - currentSize;

            Undo.RecordObject(box, "Resize SimpleMix Zone");
            SetAxisValue(ref size, axis, newSize);
            center += localNormal * (signedDelta * 0.5f);
            box.size = size;
            box.center = center;
            EditorUtility.SetDirty(box);
            EditorSceneManager.MarkSceneDirty(box.gameObject.scene);
        }

        private static Color GetResizeHandleColor(Vector3 localNormal)
        {
            if (Mathf.Abs(localNormal.x) > 0.5f)
            {
                return new Color(1f, 0.18f, 0.16f, 1f);
            }

            if (Mathf.Abs(localNormal.y) > 0.5f)
            {
                return new Color(0.25f, 0.9f, 0.25f, 1f);
            }

            return new Color(0.2f, 0.55f, 1f, 1f);
        }

        private static void DrawSolidBox(Vector3 center, Vector3 size)
        {
            Vector3 e = size * 0.5f;
            Vector3 p000 = center + new Vector3(-e.x, -e.y, -e.z);
            Vector3 p001 = center + new Vector3(-e.x, -e.y, e.z);
            Vector3 p010 = center + new Vector3(-e.x, e.y, -e.z);
            Vector3 p011 = center + new Vector3(-e.x, e.y, e.z);
            Vector3 p100 = center + new Vector3(e.x, -e.y, -e.z);
            Vector3 p101 = center + new Vector3(e.x, -e.y, e.z);
            Vector3 p110 = center + new Vector3(e.x, e.y, -e.z);
            Vector3 p111 = center + new Vector3(e.x, e.y, e.z);

            Handles.DrawAAConvexPolygon(p000, p001, p011, p010);
            Handles.DrawAAConvexPolygon(p100, p110, p111, p101);
            Handles.DrawAAConvexPolygon(p000, p100, p101, p001);
            Handles.DrawAAConvexPolygon(p010, p011, p111, p110);
            Handles.DrawAAConvexPolygon(p000, p010, p110, p100);
            Handles.DrawAAConvexPolygon(p001, p101, p111, p011);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static int GetAxis(Vector3 normal)
        {
            if (Mathf.Abs(normal.x) > 0.5f)
            {
                return 0;
            }

            if (Mathf.Abs(normal.y) > 0.5f)
            {
                return 1;
            }

            return 2;
        }

        private static float GetAxisValue(Vector3 value, int axis)
        {
            if (axis == 0)
            {
                return value.x;
            }

            if (axis == 1)
            {
                return value.y;
            }

            return value.z;
        }

        private static void SetAxisValue(ref Vector3 value, int axis, float axisValue)
        {
            if (axis == 0)
            {
                value.x = axisValue;
            }
            else if (axis == 1)
            {
                value.y = axisValue;
            }
            else
            {
                value.z = axisValue;
            }
        }
    }
}
