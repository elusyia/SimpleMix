using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UdonSharp;
using UdonSharpEditor;

namespace LAuX.SimpleMix.EditorTools
{
    internal static class SimpleMixEditorUtility
    {
        private const string HeaderPath = "Assets/LAuX/wip/SimpleMix/Editor/Header.png";

        private static Texture2D headerTexture;

        public static Texture2D HeaderTexture
        {
            get
            {
                if (headerTexture == null)
                {
                    headerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HeaderPath);
                }

                return headerTexture;
            }
        }

        public static void DrawHeader(string title, Object context, SimpleMixDirector director)
        {
            Texture2D header = HeaderTexture;
            if (header == null)
            {
                GUILayout.Space(4f);
                return;
            }

            float availableWidth = EditorGUIUtility.currentViewWidth - 36f;
            if (availableWidth < 120f)
            {
                availableWidth = 120f;
            }

            float height = availableWidth * ((float)header.height / header.width);
            Rect rect = GUILayoutUtility.GetRect(availableWidth, height, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, header, ScaleMode.ScaleToFit, true);
            GUILayout.Space(4f);
        }

        public static int GetLayerCount(SimpleMixDirector director)
        {
            return director != null && director.layers != null ? director.layers.Length : 0;
        }

        public static void SyncTopology(SimpleMixDirector director)
        {
            if (director == null)
            {
                return;
            }

            Transform root = director.transform;
            AudioSource[] foundLayers = CollectAudioSources(FindModuleRoot(root, "layers"));
            AudioSource[] foundOneShots = CollectAudioSources(FindModuleRoot(root, "sfx"));
            AudioSource[] oldLayers = director.layers;
            float[] oldSnapshotLayerVolumes = director.snapshotLayerVolumes;

            bool changed = false;
            if (!SameSources(director.layers, foundLayers))
            {
                Undo.RecordObject(director, "Sync SimpleMix Layer Topology");
                director.layers = foundLayers;
                changed = true;
            }

            if (!SameSources(director.oneShotSources, foundOneShots))
            {
                Undo.RecordObject(director, "Sync SimpleMix SFX Topology");
                director.oneShotSources = foundOneShots;
                changed = true;
            }

            if (changed)
            {
                EnsureSnapshotNames(director);
                if (!SameSources(oldLayers, foundLayers))
                {
                    ResizeMatrixForTopology(director, oldLayers, oldSnapshotLayerVolumes);
                }
                else
                {
                    ResizeMatrix(director);
                }

                MarkDirty(director);
            }
        }

        private static Transform FindModuleRoot(Transform root, string moduleName)
        {
            if (root == null)
            {
                return null;
            }

            string lowerModuleName = moduleName.ToLowerInvariant();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.ToLowerInvariant().Contains(lowerModuleName))
                {
                    return child;
                }
            }

            return null;
        }

        private static AudioSource[] CollectAudioSources(Transform root)
        {
            if (root == null)
            {
                return new AudioSource[0];
            }

            return root.GetComponentsInChildren<AudioSource>(true);
        }

        private static bool SameSources(AudioSource[] a, AudioSource[] b)
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

        private static void ResizeMatrixForTopology(SimpleMixDirector director, AudioSource[] oldLayers, float[] oldValues)
        {
            int snapshotCount = GetSnapshotCount(director);
            int oldLayerCount = oldLayers == null ? 0 : oldLayers.Length;
            int newLayerCount = GetLayerCount(director);
            float[] newValues = new float[snapshotCount * newLayerCount];

            for (int snapshot = 0; snapshot < snapshotCount; snapshot++)
            {
                for (int newLayer = 0; newLayer < newLayerCount; newLayer++)
                {
                    AudioSource source = director.layers[newLayer];
                    int oldLayer = IndexOfSource(oldLayers, source);
                    int newIndex = snapshot * newLayerCount + newLayer;

                    if (oldLayer >= 0 && oldValues != null)
                    {
                        int oldIndex = snapshot * oldLayerCount + oldLayer;
                        if (oldIndex >= 0 && oldIndex < oldValues.Length)
                        {
                            newValues[newIndex] = Mathf.Clamp01(oldValues[oldIndex]);
                            continue;
                        }
                    }

                    newValues[newIndex] = source == null ? 0f : Mathf.Clamp01(source.volume);
                }
            }

            director.snapshotLayerVolumes = newValues;
        }

        private static int IndexOfSource(AudioSource[] sources, AudioSource source)
        {
            if (sources == null || source == null)
            {
                return -1;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == source)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void EnsureSnapshotNames(SimpleMixDirector director)
        {
            if (director.snapshotNames != null && director.snapshotNames.Length > 0)
            {
                return;
            }

            Undo.RecordObject(director, "Create SimpleMix Default Snapshot");
            director.snapshotNames = new[] { "Default" };
        }

        public static int GetSnapshotCount(SimpleMixDirector director)
        {
            return director != null && director.snapshotNames != null ? director.snapshotNames.Length : 0;
        }

        public static int GetExpectedMatrixSize(SimpleMixDirector director)
        {
            return GetLayerCount(director) * GetSnapshotCount(director);
        }

        public static bool IsMatrixHealthy(SimpleMixDirector director)
        {
            if (director == null)
            {
                return false;
            }

            int expected = GetExpectedMatrixSize(director);
            int actual = director.snapshotLayerVolumes == null ? 0 : director.snapshotLayerVolumes.Length;
            return expected == actual;
        }

        public static string[] GetSnapshotPopupNames(SimpleMixDirector director)
        {
            int count = GetSnapshotCount(director);
            if (count <= 0)
            {
                return new[] { "No snapshots" };
            }

            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = GetSnapshotLabel(director, i);
            }

            return names;
        }

        public static int DrawSnapshotIdPopup(string label, SimpleMixDirector director, int currentId)
        {
            int count = GetSnapshotCount(director);
            bool isKnown = currentId >= -1 && currentId < count;
            int extra = isKnown ? 0 : 1;
            string[] names = new string[count + 1 + extra];
            names[0] = "None";
            for (int i = 0; i < count; i++)
            {
                names[i + 1] = GetSnapshotLabel(director, i);
            }

            if (!isKnown)
            {
                names[names.Length - 1] = "Missing Snapshot (" + currentId + ")";
            }

            int selected = currentId < 0 ? 0 : currentId + 1;
            if (!isKnown)
            {
                selected = names.Length - 1;
            }

            int newSelected = EditorGUILayout.Popup(label, selected, names);
            if (!isKnown && newSelected == names.Length - 1)
            {
                return currentId;
            }

            return newSelected - 1;
        }

        public static string GetSnapshotLabel(SimpleMixDirector director, int snapshotId)
        {
            if (director == null || snapshotId < 0)
            {
                return "None";
            }

            string name = null;
            if (director.snapshotNames != null && snapshotId < director.snapshotNames.Length)
            {
                name = director.snapshotNames[snapshotId];
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Snapshot";
            }

            return name + " (" + snapshotId + ")";
        }

        public static int GetSfxCount(SimpleMixDirector director)
        {
            return director != null && director.oneShotSources != null ? director.oneShotSources.Length : 0;
        }

        public static string GetSfxLabel(SimpleMixDirector director, int sfxId)
        {
            if (director == null || sfxId < 0)
            {
                return "None";
            }

            if (director.oneShotSources == null || sfxId >= director.oneShotSources.Length)
            {
                return "Missing SFX (" + sfxId + ")";
            }

            AudioSource source = director.oneShotSources[sfxId];
            if (source == null)
            {
                return "SFX " + sfxId + " - Missing Source";
            }

            string name = source.clip != null ? source.clip.name : source.gameObject.name;
            if (string.IsNullOrEmpty(name))
            {
                name = "SFX";
            }

            return name + " (" + sfxId + ")";
        }

        public static int DrawSfxIdPopup(string label, SimpleMixDirector director, int currentId)
        {
            int count = GetSfxCount(director);
            bool isKnown = currentId >= -1 && currentId < count;
            int extra = isKnown ? 0 : 1;
            string[] names = new string[count + 1 + extra];
            names[0] = "None";
            for (int i = 0; i < count; i++)
            {
                names[i + 1] = GetSfxLabel(director, i);
            }

            if (!isKnown)
            {
                names[names.Length - 1] = "Missing SFX (" + currentId + ")";
            }

            int selected = currentId < 0 ? 0 : currentId + 1;
            if (!isKnown)
            {
                selected = names.Length - 1;
            }

            int newSelected = EditorGUILayout.Popup(label, selected, names);
            if (!isKnown && newSelected == names.Length - 1)
            {
                return currentId;
            }

            return newSelected - 1;
        }

        public static int ClampSnapshotIndex(SimpleMixDirector director, int selectedSnapshot)
        {
            int count = GetSnapshotCount(director);
            if (count <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(selectedSnapshot, 0, count - 1);
        }

        public static void ResizeMatrix(SimpleMixDirector director)
        {
            if (director == null)
            {
                return;
            }

            int expected = GetExpectedMatrixSize(director);
            if (expected < 0)
            {
                expected = 0;
            }

            float[] oldValues = director.snapshotLayerVolumes;
            if (oldValues != null && oldValues.Length == expected)
            {
                return;
            }

            Undo.RecordObject(director, "Resize SimpleMix Snapshot Matrix");
            float[] newValues = new float[expected];
            int copyCount = oldValues == null ? 0 : Mathf.Min(oldValues.Length, newValues.Length);
            for (int i = 0; i < copyCount; i++)
            {
                newValues[i] = Mathf.Clamp01(oldValues[i]);
            }

            director.snapshotLayerVolumes = newValues;
            MarkDirty(director);
        }

        public static void SetSnapshotName(SimpleMixDirector director, int snapshotId, string snapshotName)
        {
            if (director == null || director.snapshotNames == null || snapshotId < 0 || snapshotId >= director.snapshotNames.Length)
            {
                return;
            }

            Undo.RecordObject(director, "Rename SimpleMix Snapshot");
            director.snapshotNames[snapshotId] = snapshotName;
            MarkDirty(director);
        }

        public static int AddSnapshot(SimpleMixDirector director)
        {
            if (director == null)
            {
                return 0;
            }

            int oldSnapshotCount = GetSnapshotCount(director);
            int layerCount = GetLayerCount(director);
            Undo.RecordObject(director, "Add SimpleMix Snapshot");

            string[] oldNames = director.snapshotNames;
            string[] newNames = new string[oldSnapshotCount + 1];
            for (int i = 0; i < oldSnapshotCount; i++)
            {
                newNames[i] = oldNames == null ? null : oldNames[i];
            }

            newNames[oldSnapshotCount] = "Snapshot " + oldSnapshotCount;
            director.snapshotNames = newNames;

            float[] oldValues = director.snapshotLayerVolumes;
            float[] newValues = new float[(oldSnapshotCount + 1) * layerCount];
            int copyCount = oldValues == null ? 0 : Mathf.Min(oldValues.Length, oldSnapshotCount * layerCount);
            for (int i = 0; i < copyCount; i++)
            {
                newValues[i] = Mathf.Clamp01(oldValues[i]);
            }

            for (int i = 0; i < layerCount; i++)
            {
                AudioSource source = director.layers == null ? null : director.layers[i];
                newValues[oldSnapshotCount * layerCount + i] = source == null ? 0f : Mathf.Clamp01(source.volume);
            }

            director.snapshotLayerVolumes = newValues;
            MarkDirty(director);
            return oldSnapshotCount;
        }

        public static int RemoveSnapshot(SimpleMixDirector director, int snapshotId)
        {
            int oldSnapshotCount = GetSnapshotCount(director);
            if (director == null || oldSnapshotCount <= 0 || snapshotId < 0 || snapshotId >= oldSnapshotCount)
            {
                return 0;
            }

            int layerCount = GetLayerCount(director);
            Undo.RecordObject(director, "Remove SimpleMix Snapshot");

            string[] oldNames = director.snapshotNames;
            string[] newNames = new string[oldSnapshotCount - 1];
            int writeName = 0;
            for (int i = 0; i < oldSnapshotCount; i++)
            {
                if (i == snapshotId)
                {
                    continue;
                }

                newNames[writeName] = oldNames == null ? null : oldNames[i];
                writeName++;
            }

            float[] oldValues = director.snapshotLayerVolumes;
            float[] newValues = new float[(oldSnapshotCount - 1) * layerCount];
            int writeSnapshot = 0;
            for (int snapshot = 0; snapshot < oldSnapshotCount; snapshot++)
            {
                if (snapshot == snapshotId)
                {
                    continue;
                }

                for (int layer = 0; layer < layerCount; layer++)
                {
                    int oldIndex = snapshot * layerCount + layer;
                    int newIndex = writeSnapshot * layerCount + layer;
                    newValues[newIndex] = oldValues != null && oldIndex < oldValues.Length ? Mathf.Clamp01(oldValues[oldIndex]) : 0f;
                }

                writeSnapshot++;
            }

            director.snapshotNames = newNames;
            director.snapshotLayerVolumes = newValues;
            if (director.defaultSnapshotId >= newNames.Length)
            {
                director.defaultSnapshotId = Mathf.Max(0, newNames.Length - 1);
            }
            else if (director.defaultSnapshotId > snapshotId)
            {
                director.defaultSnapshotId--;
            }

            MarkDirty(director);
            return Mathf.Clamp(snapshotId, 0, Mathf.Max(0, newNames.Length - 1));
        }

        public static bool HasValidSnapshot(SimpleMixDirector director, int snapshotId)
        {
            return director != null &&
                snapshotId >= 0 &&
                snapshotId < GetSnapshotCount(director) &&
                GetLayerCount(director) > 0 &&
                director.snapshotLayerVolumes != null;
        }

        public static float GetSnapshotVolume(SimpleMixDirector director, int snapshotId, int layerId, float fallback)
        {
            if (director == null || director.layers == null || layerId < 0 || layerId >= director.layers.Length)
            {
                return fallback;
            }

            int index = snapshotId * director.layers.Length + layerId;
            if (director.snapshotLayerVolumes == null || index < 0 || index >= director.snapshotLayerVolumes.Length)
            {
                return fallback;
            }

            return Mathf.Clamp01(director.snapshotLayerVolumes[index]);
        }

        public static void SaveCurrentVolumesToSnapshot(SimpleMixDirector director, int snapshotId)
        {
            if (director == null || director.layers == null || snapshotId < 0 || snapshotId >= GetSnapshotCount(director))
            {
                return;
            }

            ResizeMatrix(director);
            if (director.snapshotLayerVolumes == null)
            {
                return;
            }

            Undo.RecordObject(director, "Save SimpleMix Snapshot");
            for (int i = 0; i < director.layers.Length; i++)
            {
                int index = snapshotId * director.layers.Length + i;
                if (index < 0 || index >= director.snapshotLayerVolumes.Length)
                {
                    continue;
                }

                AudioSource source = director.layers[i];
                director.snapshotLayerVolumes[index] = source == null ? 0f : Mathf.Clamp01(source.volume);
            }

            MarkDirty(director);
        }

        public static void SaveCurrentPreviewToSnapshot(SimpleMixDirector director, int snapshotId)
        {
            if (!SimpleMixPreviewState.IsPreviewing)
            {
                return;
            }

            SaveCurrentVolumesToSnapshot(director, snapshotId);
            StopLayerPlayback(director);
            SimpleMixPreviewState.CommitPreview();
        }

        public static void SaveSnapshotEdit(SimpleMixDirector director)
        {
            SimpleMixSnapshotEditState.Commit();
            StopLayerPlayback(director);
        }

        private static void StopLayerPlayback(SimpleMixDirector director)
        {
            if (director == null || director.layers == null)
            {
                return;
            }

            for (int i = 0; i < director.layers.Length; i++)
            {
                AudioSource source = director.layers[i];
                if (source == null)
                {
                    continue;
                }

                Undo.RecordObject(source, "Stop SimpleMix Layer Playback");
                source.Stop();
                SimpleMixEditorUtility.MarkDirty(source);
            }
        }

        public static void AuditionSnapshot(SimpleMixDirector director, int snapshotId)
        {
            if (director == null || director.layers == null || snapshotId < 0)
            {
                return;
            }

            if (SimpleMixPreviewState.IsPreviewing)
            {
                SimpleMixPreviewState.RestorePreview();
            }

            SimpleMixPreviewState.BeginPreview(director);
            for (int i = 0; i < director.layers.Length; i++)
            {
                AudioSource source = director.layers[i];
                if (source == null)
                {
                    continue;
                }

                float volume = GetSnapshotVolume(director, snapshotId, i, source.volume);
                SetLayerVolume(source, volume, "Audition SimpleMix Snapshot");
            }
        }

        public static void SetLayerVolume(AudioSource source, float volume, string undoName)
        {
            if (source == null)
            {
                return;
            }

            SimpleMixPreviewState.CaptureSource(source);
            Undo.RecordObject(source, undoName);
            source.volume = Mathf.Clamp01(volume);
            source.loop = true;
            if (source.clip != null && !source.isPlaying)
            {
                source.Play();
            }

            MarkDirty(source);
        }

        public static void PlayOneShotPreview(SimpleMixDirector director, AudioSource source)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            Undo.RecordObject(source, "Preview SimpleMix SFX");
            source.loop = false;
            source.Stop();
            source.time = 0f;
            source.Play();
            MarkDirty(source);
        }

        public static void MarkDirty(Object obj)
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

            Component component = obj as Component;
            if (component != null && component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
        }
    }

    [InitializeOnLoad]
    internal static class SimpleMixPreviewState
    {
        private static AudioSource[] sources = new AudioSource[0];
        private static float[] originalVolumes = new float[0];
        private static bool[] originalPlayingStates = new bool[0];
        private static float[] originalTimes = new float[0];
        private static SimpleMixDirector activeDirector;

        static SimpleMixPreviewState()
        {
            AssemblyReloadEvents.beforeAssemblyReload += RestorePreview;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool IsPreviewing
        {
            get { return sources != null && sources.Length > 0; }
        }

        public static void BeginPreview(SimpleMixDirector director)
        {
            if (activeDirector != null && activeDirector != director)
            {
                RestorePreview();
            }

            activeDirector = director;
        }

        public static void CaptureSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == source)
                {
                    return;
                }
            }

            int oldLength = sources.Length;
            AudioSource[] newSources = new AudioSource[oldLength + 1];
            float[] newVolumes = new float[oldLength + 1];
            bool[] newPlayingStates = new bool[oldLength + 1];
            float[] newTimes = new float[oldLength + 1];

            for (int i = 0; i < oldLength; i++)
            {
                newSources[i] = sources[i];
                newVolumes[i] = originalVolumes[i];
                newPlayingStates[i] = originalPlayingStates[i];
                newTimes[i] = originalTimes[i];
            }

            newSources[oldLength] = source;
            newVolumes[oldLength] = source.volume;
            newPlayingStates[oldLength] = source.isPlaying;
            newTimes[oldLength] = source.clip == null ? 0f : source.time;
            sources = newSources;
            originalVolumes = newVolumes;
            originalPlayingStates = newPlayingStates;
            originalTimes = newTimes;
        }

        public static float GetCapturedVolume(AudioSource source, float fallback)
        {
            if (source == null)
            {
                return fallback;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == source && i < originalVolumes.Length)
                {
                    return originalVolumes[i];
                }
            }

            return fallback;
        }

        public static void CommitPreview()
        {
            sources = new AudioSource[0];
            originalVolumes = new float[0];
            originalPlayingStates = new bool[0];
            originalTimes = new float[0];
            activeDirector = null;
        }

        public static void RestorePreview()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                Undo.RecordObject(source, "Restore SimpleMix Preview");
                source.volume = originalVolumes[i];
                if (i < originalPlayingStates.Length && originalPlayingStates[i])
                {
                    if (source.clip != null && i < originalTimes.Length)
                    {
                        source.time = Mathf.Clamp(originalTimes[i], 0f, source.clip.length);
                    }

                    if (!source.isPlaying)
                    {
                        source.Play();
                    }
                }
                else
                {
                    source.Stop();
                }

                SimpleMixEditorUtility.MarkDirty(source);
            }

            sources = new AudioSource[0];
            originalVolumes = new float[0];
            originalPlayingStates = new bool[0];
            originalTimes = new float[0];
            activeDirector = null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                RestorePreview();
            }
        }
    }

    internal static class SimpleMixSnapshotEditState
    {
        private static SimpleMixDirector activeDirector;
        private static float[] originalSnapshotLayerVolumes;

        public static bool IsEditing
        {
            get { return activeDirector != null && originalSnapshotLayerVolumes != null; }
        }

        public static void BeginEdit(SimpleMixDirector director)
        {
            if (director == null)
            {
                return;
            }

            if (activeDirector != null && activeDirector != director)
            {
                Restore();
            }

            if (activeDirector == director)
            {
                return;
            }

            activeDirector = director;
            float[] current = director.snapshotLayerVolumes;
            if (current == null)
            {
                originalSnapshotLayerVolumes = new float[0];
                return;
            }

            originalSnapshotLayerVolumes = new float[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                originalSnapshotLayerVolumes[i] = current[i];
            }
        }

        public static void Commit()
        {
            activeDirector = null;
            originalSnapshotLayerVolumes = null;
        }

        public static void Restore()
        {
            if (activeDirector == null || originalSnapshotLayerVolumes == null)
            {
                Commit();
                return;
            }

            Undo.RecordObject(activeDirector, "Reset SimpleMix Snapshot Edit");
            activeDirector.snapshotLayerVolumes = new float[originalSnapshotLayerVolumes.Length];
            for (int i = 0; i < originalSnapshotLayerVolumes.Length; i++)
            {
                activeDirector.snapshotLayerVolumes[i] = originalSnapshotLayerVolumes[i];
            }

            SimpleMixEditorUtility.MarkDirty(activeDirector);
            Commit();
        }
    }
}
