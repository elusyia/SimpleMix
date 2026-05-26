using UnityEditor;
using UnityEngine;

namespace LAuX.SimpleMix.EditorTools
{
    [CustomEditor(typeof(SimpleMixDirector))]
    public class SimpleMixDirectorEditor : Editor
    {
        private const float MixerValueWidth = 48f;
        private const float MixerClipWidth = 130f;
        private const float MixerIconButtonWidth = 24f;
        private const float TimelineHeight = 4f;

        private int selectedSnapshot;

        private void OnEnable()
        {
            selectedSnapshot = SessionState.GetInt(GetSessionKey("Snapshot"), 0);
            EditorApplication.update += RepaintWhileAudioIsPlaying;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhileAudioIsPlaying;
            SessionState.SetInt(GetSessionKey("Snapshot"), selectedSnapshot);
        }

        private void RepaintWhileAudioIsPlaying()
        {
            SimpleMixDirector director = target as SimpleMixDirector;
            if (director == null || director.layers == null)
            {
                return;
            }

            for (int i = 0; i < director.layers.Length; i++)
            {
                AudioSource source = director.layers[i];
                if (source != null && source.isPlaying)
                {
                    Repaint();
                    return;
                }
            }

            if (director.oneShotSources == null)
            {
                return;
            }

            for (int i = 0; i < director.oneShotSources.Length; i++)
            {
                AudioSource source = director.oneShotSources[i];
                if (source != null && source.isPlaying)
                {
                    Repaint();
                    return;
                }
            }
        }

        private void OnSceneGUI()
        {
            SimpleMixDirector director = (SimpleMixDirector)target;
            DrawAudioSourceLabels(director);

            SimpleMixZoneTrigger[] zones = Object.FindObjectsOfType<SimpleMixZoneTrigger>(true);
            for (int i = 0; i < zones.Length; i++)
            {
                SimpleMixZoneTrigger zone = zones[i];
                if (zone == null || zone.director != director)
                {
                    continue;
                }

                Color color = GetZoneColor(zone.enterSnapshotId, i);
                SimpleMixZoneTriggerEditor.DrawZoneSceneGUI(zone, color, true);
            }
        }

        private void DrawAudioSourceLabels(SimpleMixDirector director)
        {
            if (director == null)
            {
                return;
            }

            DrawAudioSourceLabels(director.layers, "Layer", new Color(0.28f, 0.65f, 1f, 1f));
            DrawAudioSourceLabels(director.oneShotSources, "SFX", new Color(1f, 0.72f, 0.18f, 1f));
        }

        private void DrawAudioSourceLabels(AudioSource[] sources, string labelPrefix, Color color)
        {
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                Vector3 position = source.transform.position + Vector3.up * (HandleUtility.GetHandleSize(source.transform.position) * 0.18f);
                DrawAudioSourceLabel(position, color, labelPrefix + " " + i + "\n" + GetAudioSourceName(source));
            }
        }

        private void DrawAudioSourceLabel(Vector3 position, Color color, string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.normal.textColor = Color.white;
            style.fontSize = 11;
            style.alignment = TextAnchor.MiddleLeft;

            Color oldColor = Handles.color;
            Handles.color = color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * 0.08f, EventType.Repaint);
            Handles.color = oldColor;
            Handles.Label(position, label, style);
        }

        private string GetAudioSourceName(AudioSource source)
        {
            if (source == null)
            {
                return "Missing";
            }

            if (source.clip != null && !string.IsNullOrEmpty(source.clip.name))
            {
                return source.clip.name;
            }

            return source.gameObject.name;
        }

        public override void OnInspectorGUI()
        {
            SimpleMixDirector director = (SimpleMixDirector)target;
            SimpleMixEditorUtility.SyncTopology(director);
            selectedSnapshot = SimpleMixEditorUtility.ClampSnapshotIndex(director, selectedSnapshot);

            SimpleMixEditorUtility.DrawHeader("SimpleMix Authoring", director, director);
            DrawAuthoringPanel(director);
        }

        private void DrawAuthoringPanel(SimpleMixDirector director)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);

                if (!SimpleMixEditorUtility.IsMatrixHealthy(director))
                {
                    EditorGUILayout.HelpBox("Snapshot matrix length does not match snapshot count x layer count.", MessageType.Warning);
                    if (GUILayout.Button("Resize Matrix"))
                    {
                        SimpleMixEditorUtility.ResizeMatrix(director);
                    }
                }

                string[] snapshotNames = SimpleMixEditorUtility.GetSnapshotPopupNames(director);
                using (new EditorGUI.DisabledScope(SimpleMixEditorUtility.GetSnapshotCount(director) == 0))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Default", EditorStyles.boldLabel);

                        EditorGUI.BeginChangeCheck();
                        int newDefaultSnapshot = EditorGUILayout.Popup("Default Snapshot", SimpleMixEditorUtility.ClampSnapshotIndex(director, director.defaultSnapshotId), snapshotNames);
                        float newDefaultFadeSeconds = EditorGUILayout.FloatField("Default Fade", director.defaultFadeSeconds);
                        bool newAutoApplyDefault = EditorGUILayout.Toggle("Auto Apply Default", director.autoApplyDefault);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(director, "Edit SimpleMix Default Snapshot");
                            director.defaultSnapshotId = newDefaultSnapshot;
                            director.defaultFadeSeconds = newDefaultFadeSeconds;
                            director.autoApplyDefault = newAutoApplyDefault;
                            SimpleMixEditorUtility.MarkDirty(director);
                        }
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);

                        selectedSnapshot = EditorGUILayout.Popup("Selected Snapshot", selectedSnapshot, snapshotNames);

                        string currentName = director.snapshotNames == null || selectedSnapshot >= director.snapshotNames.Length ? string.Empty : director.snapshotNames[selectedSnapshot];
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUILayout.TextField("Snapshot Name", currentName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SimpleMixEditorUtility.SetSnapshotName(director, selectedSnapshot, newName);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Add Snapshot"))
                            {
                                selectedSnapshot = SimpleMixEditorUtility.AddSnapshot(director);
                            }

                            if (GUILayout.Button("Remove Snapshot"))
                            {
                                selectedSnapshot = SimpleMixEditorUtility.RemoveSnapshot(director, selectedSnapshot);
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Audition Snapshot"))
                            {
                                SimpleMixEditorUtility.AuditionSnapshot(director, selectedSnapshot);
                            }

                            using (new EditorGUI.DisabledScope(!SimpleMixPreviewState.IsPreviewing && !SimpleMixSnapshotEditState.IsEditing))
                            {
                                if (GUILayout.Button("Save"))
                                {
                                    if (SimpleMixPreviewState.IsPreviewing)
                                    {
                                        SimpleMixEditorUtility.SaveCurrentPreviewToSnapshot(director, selectedSnapshot);
                                    }
                                    else
                                    {
                                        SimpleMixEditorUtility.SaveSnapshotEdit(director);
                                    }
                                }
                            }

                            using (new EditorGUI.DisabledScope(!SimpleMixPreviewState.IsPreviewing && !SimpleMixSnapshotEditState.IsEditing))
                            {
                                if (GUILayout.Button("Reset"))
                                {
                                    if (SimpleMixPreviewState.IsPreviewing)
                                    {
                                        SimpleMixPreviewState.RestorePreview();
                                    }
                                    else
                                    {
                                        SimpleMixSnapshotEditState.Restore();
                                    }
                                }
                            }
                        }
                    }
                }

            }

            DrawLayerMixer(director);
            DrawSfxModules(director);
        }

        private void DrawLayerMixer(SimpleMixDirector director)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Layer Mixer", EditorStyles.boldLabel);
                if (director.layers == null || director.layers.Length == 0)
                {
                    EditorGUILayout.HelpBox("No layers assigned.", MessageType.Info);
                    return;
                }

                bool canEditSnapshot = SimpleMixEditorUtility.GetSnapshotCount(director) > 0 && SimpleMixEditorUtility.IsMatrixHealthy(director);

                for (int i = 0; i < director.layers.Length; i++)
                {
                    AudioSource source = director.layers[i];
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Layer " + i, GUILayout.Width(58f));
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.ObjectField(source, typeof(AudioSource), true);
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(source == null || source.clip == null))
                            {
                                string buttonLabel = source != null && source.isPlaying ? "\u25a0" : "\u25b6";
                                if (GUILayout.Button(buttonLabel, GUILayout.Width(MixerIconButtonWidth)))
                                {
                                    ToggleLayerPreview(director, source, SimpleMixEditorUtility.GetSnapshotVolume(director, selectedSnapshot, i, 0f));
                                }
                            }

                            string clipName = source != null && source.clip != null ? source.clip.name : "No Clip";
                            GUILayout.Label(clipName, EditorStyles.miniLabel, GUILayout.Width(MixerClipWidth));

                            using (new EditorGUI.DisabledScope(!canEditSnapshot))
                            {
                                bool isPreviewing = SimpleMixPreviewState.IsPreviewing;
                                float current = isPreviewing && source != null ? source.volume : SimpleMixEditorUtility.GetSnapshotVolume(director, selectedSnapshot, i, 0f);
                                EditorGUI.BeginChangeCheck();
                                float sliderValue = GUILayout.HorizontalSlider(current, 0f, 1f, GUILayout.MinWidth(80f), GUILayout.ExpandWidth(true));
                                sliderValue = EditorGUILayout.FloatField(sliderValue, GUILayout.Width(MixerValueWidth));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (isPreviewing && source != null)
                                    {
                                        SimpleMixEditorUtility.SetLayerVolume(source, sliderValue, "Preview SimpleMix Snapshot Volume");
                                    }
                                    else
                                    {
                                        SimpleMixSnapshotEditState.BeginEdit(director);
                                        SetSnapshotVolume(director, selectedSnapshot, i, sliderValue);
                                    }
                                }
                            }
                        }

                        DrawLayerTimeline(source);
                    }
                }
            }
        }

        private void ToggleLayerPreview(SimpleMixDirector director, AudioSource source, float volume)
        {
            if (source == null)
            {
                return;
            }

            if (source.isPlaying)
            {
                Undo.RecordObject(source, "Stop SimpleMix Layer Preview");
                source.Stop();
                SimpleMixEditorUtility.MarkDirty(source);
                return;
            }

            SimpleMixPreviewState.BeginPreview(director);
            SimpleMixEditorUtility.SetLayerVolume(source, volume, "Play SimpleMix Layer Preview");
        }

        private void DrawLayerTimeline(AudioSource source)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, TimelineHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            if (source == null || source.clip == null || source.clip.length <= 0f)
            {
                return;
            }

            float progress = Mathf.Clamp01(source.time / source.clip.length);
            Rect fillRect = rect;
            fillRect.width *= progress;
            EditorGUI.DrawRect(fillRect, source.isPlaying ? new Color(0.28f, 0.65f, 1f, 1f) : new Color(0.35f, 0.35f, 0.35f, 1f));
        }

        private void DrawSfxModules(SimpleMixDirector director)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("SFX Modules", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                float newOneShotVolume = EditorGUILayout.Slider("Master Volume", director.oneShotVolume, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(director, "Adjust SimpleMix SFX Master Volume");
                    director.oneShotVolume = Mathf.Clamp01(newOneShotVolume);
                    SimpleMixEditorUtility.MarkDirty(director);
                }

                if (director.oneShotSources == null || director.oneShotSources.Length == 0)
                {
                    EditorGUILayout.HelpBox("No SFX AudioSources found under the SFX module.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < director.oneShotSources.Length; i++)
                {
                    AudioSource source = director.oneShotSources[i];
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(source == null || source.clip == null))
                            {
                                if (GUILayout.Button("\u25b6", GUILayout.Width(MixerIconButtonWidth)))
                                {
                                    SimpleMixEditorUtility.PlayOneShotPreview(director, source);
                                }
                            }

                            GUILayout.Label("SFX " + i, GUILayout.Width(72f));
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.ObjectField(source, typeof(AudioSource), true);
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            string clipName = source != null && source.clip != null ? source.clip.name : "No Clip";
                            GUILayout.Label(clipName, EditorStyles.miniLabel, GUILayout.Width(MixerClipWidth));

                            using (new EditorGUI.DisabledScope(source == null))
                            {
                                float current = source == null ? 0f : source.volume;
                                EditorGUI.BeginChangeCheck();
                                float sliderValue = GUILayout.HorizontalSlider(current, 0f, 1f, GUILayout.MinWidth(80f), GUILayout.ExpandWidth(true));
                                sliderValue = EditorGUILayout.FloatField(sliderValue, GUILayout.Width(MixerValueWidth));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObject(source, "Adjust SimpleMix SFX Volume");
                                    source.volume = Mathf.Clamp01(sliderValue);
                                    SimpleMixEditorUtility.MarkDirty(source);
                                }
                            }
                        }

                        DrawLayerTimeline(source);
                    }
                }
            }
        }

        private void SetSnapshotVolume(SimpleMixDirector director, int snapshotId, int layerId, float volume)
        {
            if (director == null || director.layers == null || snapshotId < 0 || layerId < 0 || layerId >= director.layers.Length)
            {
                return;
            }

            SimpleMixEditorUtility.ResizeMatrix(director);
            int index = snapshotId * director.layers.Length + layerId;
            if (director.snapshotLayerVolumes == null || index < 0 || index >= director.snapshotLayerVolumes.Length)
            {
                return;
            }

            Undo.RecordObject(director, "Adjust SimpleMix Snapshot Volume");
            director.snapshotLayerVolumes[index] = Mathf.Clamp01(volume);
            SimpleMixEditorUtility.MarkDirty(director);
        }

        private Color GetZoneColor(int snapshotId, int fallbackIndex)
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

        private string GetSessionKey(string suffix)
        {
            return "LAuX.SimpleMix.DirectorEditor." + target.GetInstanceID() + "." + suffix;
        }
    }
}
