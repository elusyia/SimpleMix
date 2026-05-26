using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LAuX.SimpleMix.EditorTools
{
    internal static class SimpleMixMenuItems
    {
        private const string RootName = "LAuX SimpleMix Runtime";
        private const string DirectorName = "SimpleMix Director";
        private const string LayersName = "Layers";
        private const string SfxName = "SFX";
        private const string TriggerManagerName = "SimpleMix Trigger Manager";
        private const string ZonesName = "Zones";
        private const string InteractsName = "Interacts";
        private const string DebugPanelName = "SimpleMix Debug Panel";

        [MenuItem("LAuX/Simple Mix/Setup Scene")]
        private static void SetupScene()
        {
            GameObject root = GetOrCreateRoot();
            SimpleMixDirector director = GetOrCreateDirector(root.transform);
            GetOrCreateChild(director.transform, LayersName);
            GetOrCreateChild(director.transform, SfxName);

            SimpleMixTriggerManager manager = GetOrCreateTriggerManager(root.transform, director);
            GetOrCreateChild(manager.transform, ZonesName);
            GetOrCreateChild(manager.transform, InteractsName);

            MarkDirty(director);
            MarkDirty(manager);
            SelectAndFrame(root);
            SaveActiveScene();
        }

        [MenuItem("LAuX/Simple Mix/Place Debug Panel")]
        private static void PlaceDebugPanel()
        {
            GameObject root = GetOrCreateRoot();
            SimpleMixDirector director = FindDirector();
            if (director == null)
            {
                director = GetOrCreateDirector(root.transform);
            }

            SimpleMixDebugPanel panel = GetOrCreateDebugPanel(root.transform, director);
            MarkDirty(panel);
            SelectAndFrame(panel.gameObject);
            SaveActiveScene();
        }

        private static GameObject GetOrCreateRoot()
        {
            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create SimpleMix Runtime");
            return root;
        }

        private static SimpleMixDirector GetOrCreateDirector(Transform root)
        {
            SimpleMixDirector director = FindDirector();
            if (director != null)
            {
                if (director.transform.parent == null)
                {
                    Undo.SetTransformParent(director.transform, root, "Parent SimpleMix Director");
                }

                return director;
            }

            Transform directorTransform = FindChild(root, DirectorName);
            GameObject directorObject = directorTransform == null ? new GameObject(DirectorName) : directorTransform.gameObject;
            if (directorTransform == null)
            {
                Undo.RegisterCreatedObjectUndo(directorObject, "Create SimpleMix Director");
                directorObject.transform.SetParent(root, false);
            }

            director = directorObject.GetComponent<SimpleMixDirector>();
            if (director == null)
            {
                Undo.AddComponent<SimpleMixDirector>(directorObject);
                director = directorObject.GetComponent<SimpleMixDirector>();
            }

            if (director.snapshotNames == null || director.snapshotNames.Length == 0)
            {
                director.snapshotNames = new[] { "Default" };
            }

            if (director.snapshotLayerVolumes == null)
            {
                director.snapshotLayerVolumes = new float[0];
            }

            director.defaultSnapshotId = Mathf.Max(0, director.defaultSnapshotId);
            if (director.defaultFadeSeconds <= 0f)
            {
                director.defaultFadeSeconds = 1.25f;
            }

            return director;
        }

        private static SimpleMixTriggerManager GetOrCreateTriggerManager(Transform root, SimpleMixDirector director)
        {
            SimpleMixTriggerManager manager = Object.FindObjectOfType<SimpleMixTriggerManager>(true);
            if (manager != null)
            {
                if (manager.transform.parent == null)
                {
                    Undo.SetTransformParent(manager.transform, root, "Parent SimpleMix Trigger Manager");
                }

                manager.director = director;
                return manager;
            }

            Transform managerTransform = FindChild(root, TriggerManagerName);
            GameObject managerObject = managerTransform == null ? new GameObject(TriggerManagerName) : managerTransform.gameObject;
            if (managerTransform == null)
            {
                Undo.RegisterCreatedObjectUndo(managerObject, "Create SimpleMix Trigger Manager");
                managerObject.transform.SetParent(root, false);
            }

            manager = managerObject.GetComponent<SimpleMixTriggerManager>();
            if (manager == null)
            {
                Undo.AddComponent<SimpleMixTriggerManager>(managerObject);
                manager = managerObject.GetComponent<SimpleMixTriggerManager>();
            }

            manager.director = director;
            return manager;
        }

        private static SimpleMixDebugPanel GetOrCreateDebugPanel(Transform root, SimpleMixDirector director)
        {
            SimpleMixDebugPanel existingPanel = Object.FindObjectOfType<SimpleMixDebugPanel>(true);
            if (existingPanel != null)
            {
                existingPanel.director = director;
                return existingPanel;
            }

            GameObject panelObject = new GameObject(DebugPanelName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelObject, "Create SimpleMix Debug Panel");
            panelObject.transform.SetParent(root, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(360f, 180f);
            panelRect.localPosition = new Vector3(0f, 1.6f, 0f);
            panelRect.localScale = Vector3.one * 0.01f;

            Canvas canvas = panelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.68f);

            Text statusText = CreateText(panelObject.transform, "Status Text", new Vector2(12f, -12f), new Vector2(336f, 104f), TextAnchor.UpperLeft);
            Text snapshotText = CreateText(panelObject.transform, "Snapshot Text", new Vector2(12f, -120f), new Vector2(336f, 48f), TextAnchor.UpperLeft);

            SimpleMixDebugPanel panel = panelObject.AddComponent<SimpleMixDebugPanel>();
            panel.director = director;
            panel.statusText = statusText;
            panel.snapshotText = snapshotText;
            panel.refreshSeconds = 0.1f;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textObject, "Create SimpleMix Debug Text");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = alignment;
            text.text = string.Empty;
            return text;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = FindChild(parent, childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, "Create SimpleMix Group");
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static SimpleMixDirector FindDirector()
        {
            return Object.FindObjectOfType<SimpleMixDirector>(true);
        }

        private static void MarkDirty(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            EditorUtility.SetDirty(obj);
            UdonSharpBehaviour udonSharpBehaviour = obj as UdonSharpBehaviour;
            if (udonSharpBehaviour != null)
            {
                UdonSharpEditorUtility.CopyProxyToUdon(udonSharpBehaviour, ProxySerializationPolicy.All);
            }
        }

        private static void SelectAndFrame(GameObject gameObject)
        {
            Selection.activeGameObject = gameObject;
            SceneView.FrameLastActiveSceneView();
        }

        private static void SaveActiveScene()
        {
            if (EditorSceneManager.GetActiveScene().IsValid())
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
