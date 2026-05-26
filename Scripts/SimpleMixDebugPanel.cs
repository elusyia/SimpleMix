using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace LAuX.SimpleMix
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleMixDebugPanel : UdonSharpBehaviour
    {
        public SimpleMixDirector director;
        public Text statusText;
        public Text snapshotText;
        public float refreshSeconds = 0.1f;

        private float nextRefreshTime;

        private void Start()
        {
            RefreshPanel();
        }

        private void Update()
        {
            if (Time.time < nextRefreshTime)
            {
                return;
            }

            float interval = refreshSeconds;
            if (interval < 0.02f)
            {
                interval = 0.02f;
            }

            nextRefreshTime = Time.time + interval;
            RefreshPanel();
        }

        public void RefreshPanel()
        {
            if (director == null)
            {
                if (statusText != null)
                {
                    statusText.text = "SimpleMix Debug\nDirector: missing";
                }

                if (snapshotText != null)
                {
                    snapshotText.text = "";
                }

                return;
            }

            if (statusText != null)
            {
                statusText.text =
                    "SimpleMix Debug\n" +
                    "Loop: " + (director.IsLoopPlaying() ? "playing" : "stopped") + "\n" +
                    "Snapshot: " + director.GetCurrentSnapshotId() + "\n" +
                    "Fading: " + (director.IsFading() ? "yes" : "no") + "\n" +
                    "Fade: " + director.GetFadeProgress() + "\n" +
                    "SFX: " + director.GetLastOneShotId();
            }

            if (snapshotText != null)
            {
                string text = "Current: " + director.GetCurrentSnapshotId();
                if (director.IsFading())
                {
                    text += "\nPending: " + director.GetPendingSnapshotId();
                }
                else
                {
                    text += "\nPending: none";
                }

                snapshotText.text = text;
            }
        }
    }
}
