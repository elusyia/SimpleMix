using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LAuX.SimpleMix.EditorTools
{
    [CustomEditor(typeof(SimpleMixTriggerManager))]
    public class SimpleMixTriggerManagerEditor : Editor
    {
        private const string ZonesRootName = "Zones";
        private const string InteractsRootName = "Interacts";

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            SimpleMixTriggerManager manager = target as SimpleMixTriggerManager;
            if (manager != null)
            {
                SyncTopology(manager);
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            SimpleMixTriggerManager manager = (SimpleMixTriggerManager)target;
            SyncTopology(manager);

            SimpleMixEditorUtility.DrawHeader("SimpleMix Trigger Manager", manager, manager.director);
            DrawDirectorField(manager);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Zone Trigger"))
                {
                    CreateZoneTrigger(manager);
                }

                if (GUILayout.Button("Create Interact Trigger"))
                {
                    CreateInteractTrigger(manager);
                }
            }

            DrawZoneTriggers(manager);
            DrawInteractTriggers(manager);
        }

        private void OnSceneGUI()
        {
            SimpleMixTriggerManager manager = (SimpleMixTriggerManager)target;
            SyncTopology(manager);
            if (manager.zones == null)
            {
                return;
            }

            for (int i = 0; i < manager.zones.Length; i++)
            {
                SimpleMixZoneTrigger zone = manager.zones[i];
                if (zone == null)
                {
                    continue;
                }

                Color color = GetZoneColor(zone.enterSnapshotId, i);
                SimpleMixZoneTriggerEditor.DrawZoneSceneGUI(zone, color, true);
            }
        }

        private static void DrawDirectorField(SimpleMixTriggerManager manager)
        {
            EditorGUI.BeginChangeCheck();
            SimpleMixDirector newDirector = (SimpleMixDirector)EditorGUILayout.ObjectField("Director", manager.director, typeof(SimpleMixDirector), true);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(manager, "Set SimpleMix Trigger Manager Director");
            manager.director = newDirector;
            SimpleMixEditorUtility.MarkDirty(manager);
            SyncTopology(manager);
        }

        private static void DrawZoneTriggers(SimpleMixTriggerManager manager)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Zone Triggers", EditorStyles.boldLabel);
                if (manager.zones == null || manager.zones.Length == 0)
                {
                    EditorGUILayout.HelpBox("No SimpleMixZoneTrigger components found under the Zones child.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < manager.zones.Length; i++)
                {
                    SimpleMixZoneTrigger zone = manager.zones[i];
                    if (zone == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawObjectHeader(zone.gameObject, "Zone " + i);
                        DrawZoneFields(zone);
                    }
                }
            }
        }

        private static void DrawInteractTriggers(SimpleMixTriggerManager manager)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Interact Triggers", EditorStyles.boldLabel);
                if (manager.interactTriggers == null || manager.interactTriggers.Length == 0)
                {
                    EditorGUILayout.HelpBox("No SimpleMixEventTrigger components found under the Interacts child.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < manager.interactTriggers.Length; i++)
                {
                    SimpleMixEventTrigger trigger = manager.interactTriggers[i];
                    if (trigger == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawObjectHeader(trigger.gameObject, "Interact " + i);
                        DrawInteractFields(trigger);
                    }
                }
            }
        }

        private static void DrawZoneFields(SimpleMixZoneTrigger zone)
        {
            SimpleMixDirector director = zone.director;
            EditorGUI.BeginChangeCheck();
            int enterSnapshotId = SimpleMixEditorUtility.DrawSnapshotIdPopup("Enter", director, zone.enterSnapshotId);
            bool useExitSnapshot = EditorGUILayout.Toggle("Use Exit", zone.useExitSnapshot);
            int exitSnapshotId = SimpleMixEditorUtility.DrawSnapshotIdPopup("Exit", director, zone.exitSnapshotId);
            int enterSfxId = SimpleMixEditorUtility.DrawSfxIdPopup("Enter SFX", director, zone.enterOneShotId);
            int exitSfxId = SimpleMixEditorUtility.DrawSfxIdPopup("Exit SFX", director, zone.exitOneShotId);
            float fadeSecondsOverride = EditorGUILayout.FloatField("Fade (-1 = Default)", zone.fadeSecondsOverride);
            bool triggerOnce = EditorGUILayout.Toggle("Trigger Once", zone.triggerOnce);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(zone, "Edit SimpleMix Zone Trigger");
            zone.enterSnapshotId = enterSnapshotId;
            zone.useExitSnapshot = useExitSnapshot;
            zone.exitSnapshotId = exitSnapshotId;
            zone.enterOneShotId = enterSfxId;
            zone.exitOneShotId = exitSfxId;
            zone.fadeSecondsOverride = fadeSecondsOverride;
            zone.triggerOnce = triggerOnce;
            SimpleMixEditorUtility.MarkDirty(zone);
        }

        private static void DrawInteractFields(SimpleMixEventTrigger trigger)
        {
            SimpleMixDirector director = trigger.director;
            EditorGUI.BeginChangeCheck();
            int mode = DrawModePopup(trigger.playOnInteract, trigger.playOnEnter);
            bool useSnapshot = EditorGUILayout.Toggle("Use Snapshot", trigger.useSnapshot);
            int snapshotId = SimpleMixEditorUtility.DrawSnapshotIdPopup("Snapshot", director, trigger.snapshotId);
            bool useSfx = EditorGUILayout.Toggle("Use SFX", trigger.useOneShot);
            int sfxId = SimpleMixEditorUtility.DrawSfxIdPopup("SFX", director, trigger.oneShotId);
            float fadeSecondsOverride = EditorGUILayout.FloatField("Fade (-1 = Default)", trigger.fadeSecondsOverride);
            bool triggerOnce = EditorGUILayout.Toggle("Trigger Once", trigger.triggerOnce);
            float cooldownSeconds = EditorGUILayout.FloatField("Cooldown", trigger.cooldownSeconds);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(trigger, "Edit SimpleMix Interact Trigger");
            SetMode(mode, trigger);
            trigger.useSnapshot = useSnapshot;
            trigger.snapshotId = snapshotId;
            trigger.useOneShot = useSfx;
            trigger.oneShotId = sfxId;
            trigger.fadeSecondsOverride = fadeSecondsOverride;
            trigger.triggerOnce = triggerOnce;
            trigger.cooldownSeconds = cooldownSeconds;
            SimpleMixEditorUtility.MarkDirty(trigger);
        }

        private static int DrawModePopup(bool playOnInteract, bool playOnEnter)
        {
            int mode = playOnInteract && playOnEnter ? 2 : playOnEnter ? 1 : 0;
            string[] modes = { "Interact", "Player Enter", "Interact + Player Enter" };
            return EditorGUILayout.Popup("Mode", mode, modes);
        }

        private static void SetMode(int mode, SimpleMixEventTrigger trigger)
        {
            trigger.playOnInteract = mode == 0 || mode == 2;
            trigger.playOnEnter = mode == 1 || mode == 2;
        }

        private static void DrawObjectHeader(GameObject gameObject, string fallbackName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(string.IsNullOrEmpty(gameObject.name) ? fallbackName : gameObject.name, gameObject, typeof(GameObject), true);
                }

                if (GUILayout.Button("Select", GUILayout.Width(56f)))
                {
                    Selection.activeGameObject = gameObject;
                }

                if (GUILayout.Button("Frame", GUILayout.Width(52f)))
                {
                    FrameObject(gameObject);
                }
            }
        }

        private static void FrameObject(GameObject gameObject)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || gameObject == null)
            {
                return;
            }

            Bounds bounds = new Bounds(gameObject.transform.position, Vector3.one);
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                bounds = collider.bounds;
            }
            else
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                }
            }

            sceneView.Frame(bounds, false);
        }

        private static Color GetZoneColor(int snapshotId, int fallbackIndex)
        {
            int index = snapshotId >= 0 ? snapshotId : fallbackIndex;
            switch (Mathf.Abs(index) % 8)
            {
                case 0:
                    return new Color(0.2f, 0.75f, 1f, 1f);
                case 1:
                    return new Color(0.25f, 0.9f, 0.45f, 1f);
                case 2:
                    return new Color(1f, 0.72f, 0.18f, 1f);
                case 3:
                    return new Color(1f, 0.35f, 0.35f, 1f);
                case 4:
                    return new Color(0.75f, 0.45f, 1f, 1f);
                case 5:
                    return new Color(0.15f, 0.95f, 0.9f, 1f);
                case 6:
                    return new Color(1f, 0.45f, 0.75f, 1f);
                default:
                    return new Color(0.8f, 0.9f, 0.2f, 1f);
            }
        }

        private static void CreateZoneTrigger(SimpleMixTriggerManager manager)
        {
            Transform root = GetOrCreateChild(manager.transform, ZonesRootName);
            GameObject gameObject = new GameObject("New Zone Trigger");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create SimpleMix Zone Trigger");
            gameObject.transform.SetParent(root, false);

            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            SimpleMixZoneTrigger zone = gameObject.AddComponent<SimpleMixZoneTrigger>();
            zone.director = manager.director;
            SimpleMixEditorUtility.MarkDirty(zone);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Selection.activeGameObject = gameObject;
            SyncTopology(manager);
        }

        private static void CreateInteractTrigger(SimpleMixTriggerManager manager)
        {
            Transform root = GetOrCreateChild(manager.transform, InteractsRootName);
            GameObject gameObject = new GameObject("New Interact Trigger");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create SimpleMix Interact Trigger");
            gameObject.transform.SetParent(root, false);

            SimpleMixEventTrigger trigger = gameObject.AddComponent<SimpleMixEventTrigger>();
            trigger.director = manager.director;
            SimpleMixEditorUtility.MarkDirty(trigger);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Selection.activeGameObject = gameObject;
            SyncTopology(manager);
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = FindChild(parent, childName);
            if (child != null)
            {
                return child;
            }

            GameObject gameObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create SimpleMix Trigger Group");
            gameObject.transform.SetParent(parent, false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            return gameObject.transform;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            string lowerChildName = childName.ToLowerInvariant();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.ToLowerInvariant().Contains(lowerChildName))
                {
                    return child;
                }
            }

            return null;
        }

        private static void SyncTopology(SimpleMixTriggerManager manager)
        {
            if (manager == null)
            {
                return;
            }

            SimpleMixZoneTrigger[] zones = CollectZones(FindChild(manager.transform, ZonesRootName));
            SimpleMixEventTrigger[] interacts = CollectInteracts(FindChild(manager.transform, InteractsRootName));
            bool changed = false;

            if (!SameZones(manager.zones, zones))
            {
                Undo.RecordObject(manager, "Sync SimpleMix Zone Triggers");
                manager.zones = zones;
                changed = true;
            }

            if (!SameInteracts(manager.interactTriggers, interacts))
            {
                Undo.RecordObject(manager, "Sync SimpleMix Interact Triggers");
                manager.interactTriggers = interacts;
                changed = true;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].director != manager.director)
                {
                    Undo.RecordObject(zones[i], "Sync SimpleMix Zone Director");
                    zones[i].director = manager.director;
                    SimpleMixEditorUtility.MarkDirty(zones[i]);
                }
            }

            for (int i = 0; i < interacts.Length; i++)
            {
                if (interacts[i] != null && interacts[i].director != manager.director)
                {
                    Undo.RecordObject(interacts[i], "Sync SimpleMix Interact Director");
                    interacts[i].director = manager.director;
                    SimpleMixEditorUtility.MarkDirty(interacts[i]);
                }
            }

            if (changed)
            {
                SimpleMixEditorUtility.MarkDirty(manager);
            }
        }

        private static SimpleMixZoneTrigger[] CollectZones(Transform root)
        {
            return root == null ? new SimpleMixZoneTrigger[0] : root.GetComponentsInChildren<SimpleMixZoneTrigger>(true);
        }

        private static SimpleMixEventTrigger[] CollectInteracts(Transform root)
        {
            return root == null ? new SimpleMixEventTrigger[0] : root.GetComponentsInChildren<SimpleMixEventTrigger>(true);
        }

        private static bool SameZones(SimpleMixZoneTrigger[] a, SimpleMixZoneTrigger[] b)
        {
            int aLength = a == null ? 0 : a.Length;
            int bLength = b == null ? 0 : b.Length;
            if (aLength != bLength)
            {
                return false;
            }

            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameInteracts(SimpleMixEventTrigger[] a, SimpleMixEventTrigger[] b)
        {
            int aLength = a == null ? 0 : a.Length;
            int bLength = b == null ? 0 : b.Length;
            if (aLength != bLength)
            {
                return false;
            }

            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
