using UdonSharp;
using UnityEngine;

namespace LAuX.SimpleMix
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleMixDirector : UdonSharpBehaviour
    {
        [Header("Layers")]
        [HideInInspector]
        public AudioSource[] layers;

        [Header("Snapshots")]
        [HideInInspector]
        public string[] snapshotNames;
        [HideInInspector]
        public float[] snapshotLayerVolumes;
        [HideInInspector]
        public int defaultSnapshotId;
        public float defaultFadeSeconds = 1f;
        public bool autoApplyDefault = true;

        [Header("SFX")]
        [HideInInspector]
        public AudioSource[] oneShotSources;
        public float oneShotVolume = 1f;

        [Header("Debug")]
        public bool updateDebugValues = true;
        public int debugCurrentSnapshotId = -1;
        public int debugPendingSnapshotId = -1;
        public bool debugIsFading;
        public float debugFadeProgress;
        public bool debugLoopPlaying;
        public int debugLastOneShotId = -1;

        private float[] currentLayerVolumes;
        private float[] fadeStartVolumes;
        private float[] fadeTargetVolumes;
        private int currentSnapshotId = -1;
        private int pendingSnapshotId = -1;

        private bool fading;
        private float fadeStartTime;
        private float fadeEndTime;

        private void Start()
        {
            EnsureRuntimeArrays();
            StartLayerSources();

            if (autoApplyDefault)
            {
                SetSnapshotInstant(defaultSnapshotId);
            }
            else
            {
                ReadCurrentLayerVolumes();
            }

            UpdateDebugFields();
        }

        private void Update()
        {
            EnsureRuntimeArrays();
            ProcessFade();

            if (updateDebugValues)
            {
                UpdateDebugFields();
            }
        }

        public void SetSnapshot(int snapshotId)
        {
            SetSnapshotWithFade(snapshotId, defaultFadeSeconds);
        }

        public void SetSnapshotWithFade(int snapshotId, float fadeSeconds)
        {
            if (!IsValidSnapshot(snapshotId))
            {
                return;
            }

            float resolvedFadeSeconds = fadeSeconds;
            if (resolvedFadeSeconds < 0f)
            {
                resolvedFadeSeconds = defaultFadeSeconds;
            }

            if (resolvedFadeSeconds <= 0f)
            {
                SetSnapshotInstant(snapshotId);
                return;
            }

            EnsureRuntimeArrays();
            StartLayerSources();
            CaptureCurrentVolumesToFadeStart();
            CaptureSnapshotTargets(snapshotId, fadeTargetVolumes);

            currentSnapshotId = snapshotId;
            pendingSnapshotId = snapshotId;
            fadeStartTime = Time.time;
            fadeEndTime = fadeStartTime + resolvedFadeSeconds;
            fading = true;
            UpdateDebugFields();
        }

        public void SetSnapshotInstant(int snapshotId)
        {
            if (!IsValidSnapshot(snapshotId))
            {
                return;
            }

            EnsureRuntimeArrays();
            StartLayerSources();
            fading = false;
            currentSnapshotId = snapshotId;
            pendingSnapshotId = -1;
            CaptureSnapshotTargets(snapshotId, currentLayerVolumes);
            ApplyCurrentVolumesToLayers();
            UpdateDebugFields();
        }

        public void ResetToDefaultSnapshot()
        {
            SetSnapshot(defaultSnapshotId);
        }

        public void PlayOneShot(int oneShotId)
        {
            if (oneShotSources == null || oneShotId < 0 || oneShotId >= oneShotSources.Length)
            {
                return;
            }

            AudioSource source = oneShotSources[oneShotId];
            if (source == null || source.clip == null)
            {
                return;
            }

            source.loop = false;
            source.PlayOneShot(source.clip, Clamp01(oneShotVolume));
            debugLastOneShotId = oneShotId;
            UpdateDebugFields();
        }

        public void StopAllLayers()
        {
            fading = false;
            pendingSnapshotId = -1;
            currentSnapshotId = -1;

            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                AudioSource source = layers[i];
                if (source == null)
                {
                    continue;
                }

                source.volume = 0f;
                source.Stop();
                if (currentLayerVolumes != null && i < currentLayerVolumes.Length)
                {
                    currentLayerVolumes[i] = 0f;
                }
            }

            UpdateDebugFields();
        }

        public int GetCurrentSnapshotId()
        {
            return currentSnapshotId;
        }

        public int GetPendingSnapshotId()
        {
            return fading ? pendingSnapshotId : -1;
        }

        public bool IsFading()
        {
            return fading;
        }

        public bool IsLoopPlaying()
        {
            if (layers == null)
            {
                return false;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                AudioSource source = layers[i];
                if (source != null && source.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetLastOneShotId()
        {
            return debugLastOneShotId;
        }

        public float GetFadeProgress()
        {
            return debugFadeProgress;
        }

        private void ProcessFade()
        {
            if (!fading)
            {
                return;
            }

            float duration = fadeEndTime - fadeStartTime;
            if (duration <= 0f)
            {
                FinishFade();
                return;
            }

            float progress = (Time.time - fadeStartTime) / duration;
            if (progress >= 1f)
            {
                FinishFade();
                return;
            }

            if (progress < 0f)
            {
                progress = 0f;
            }

            for (int i = 0; i < currentLayerVolumes.Length; i++)
            {
                float startVolume = fadeStartVolumes[i];
                float targetVolume = fadeTargetVolumes[i];
                currentLayerVolumes[i] = startVolume + (targetVolume - startVolume) * progress;
            }

            debugFadeProgress = progress;
            ApplyCurrentVolumesToLayers();
        }

        private void FinishFade()
        {
            CopyArrayValues(fadeTargetVolumes, currentLayerVolumes);
            ApplyCurrentVolumesToLayers();
            fading = false;
            pendingSnapshotId = -1;
            debugFadeProgress = 1f;
            UpdateDebugFields();
        }

        private bool IsValidSnapshot(int snapshotId)
        {
            if (snapshotId < 0 || layers == null || layers.Length == 0)
            {
                return false;
            }

            int firstIndex = snapshotId * layers.Length;
            if (snapshotLayerVolumes == null || firstIndex >= snapshotLayerVolumes.Length)
            {
                return false;
            }

            return true;
        }

        private void CaptureSnapshotTargets(int snapshotId, float[] targetArray)
        {
            if (targetArray == null || layers == null)
            {
                return;
            }

            for (int i = 0; i < targetArray.Length; i++)
            {
                float fallback = currentLayerVolumes != null && i < currentLayerVolumes.Length ? currentLayerVolumes[i] : 0f;
                targetArray[i] = GetSnapshotVolume(snapshotId, i, fallback);
            }
        }

        private float GetSnapshotVolume(int snapshotId, int layerId, float fallback)
        {
            if (layers == null || layerId < 0 || layerId >= layers.Length)
            {
                return fallback;
            }

            int index = snapshotId * layers.Length + layerId;
            if (snapshotLayerVolumes == null || index < 0 || index >= snapshotLayerVolumes.Length)
            {
                return fallback;
            }

            return Clamp01(snapshotLayerVolumes[index]);
        }

        private void EnsureRuntimeArrays()
        {
            int layerCount = layers == null ? 0 : layers.Length;
            if (currentLayerVolumes == null || currentLayerVolumes.Length != layerCount)
            {
                currentLayerVolumes = new float[layerCount];
                fadeStartVolumes = new float[layerCount];
                fadeTargetVolumes = new float[layerCount];
                ReadCurrentLayerVolumes();
            }
        }

        private void StartLayerSources()
        {
            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                AudioSource source = layers[i];
                if (source == null)
                {
                    continue;
                }

                source.loop = true;
                if (source.clip != null && !source.isPlaying)
                {
                    source.Play();
                }
            }
        }

        private void ReadCurrentLayerVolumes()
        {
            if (layers == null || currentLayerVolumes == null)
            {
                return;
            }

            for (int i = 0; i < currentLayerVolumes.Length; i++)
            {
                AudioSource source = i < layers.Length ? layers[i] : null;
                currentLayerVolumes[i] = source == null ? 0f : source.volume;
            }
        }

        private void CaptureCurrentVolumesToFadeStart()
        {
            if (fadeStartVolumes == null || currentLayerVolumes == null)
            {
                return;
            }

            for (int i = 0; i < fadeStartVolumes.Length; i++)
            {
                fadeStartVolumes[i] = currentLayerVolumes[i];
            }
        }

        private void CopyArrayValues(float[] sourceArray, float[] targetArray)
        {
            if (sourceArray == null || targetArray == null)
            {
                return;
            }

            int count = sourceArray.Length < targetArray.Length ? sourceArray.Length : targetArray.Length;
            for (int i = 0; i < count; i++)
            {
                targetArray[i] = sourceArray[i];
            }
        }

        private void ApplyCurrentVolumesToLayers()
        {
            if (layers == null || currentLayerVolumes == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                AudioSource source = layers[i];
                if (source == null || i >= currentLayerVolumes.Length)
                {
                    continue;
                }

                source.volume = currentLayerVolumes[i];
                source.loop = true;
            }
        }

        private float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private void UpdateDebugFields()
        {
            debugCurrentSnapshotId = currentSnapshotId;
            debugPendingSnapshotId = fading ? pendingSnapshotId : -1;
            debugIsFading = fading;
            debugLoopPlaying = IsLoopPlaying();
            if (!fading)
            {
                debugFadeProgress = 0f;
            }
        }
    }
}
