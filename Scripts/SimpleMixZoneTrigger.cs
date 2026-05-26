using UdonSharp;
using VRC.SDKBase;

namespace LAuX.SimpleMix
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleMixZoneTrigger : UdonSharpBehaviour
    {
        public SimpleMixDirector director;
        public int enterSnapshotId = -1;
        public int exitSnapshotId = -1;
        public bool useExitSnapshot = true;
        public float fadeSecondsOverride = -1f;
        public int enterOneShotId = -1;
        public int exitOneShotId = -1;
        public bool triggerOnce;

        private bool hasTriggered;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!IsLocalPlayer(player))
            {
                return;
            }

            if (triggerOnce && hasTriggered)
            {
                return;
            }

            ApplySnapshot(enterSnapshotId);
            PlayOneShot(enterOneShotId);
            hasTriggered = true;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!useExitSnapshot || !IsLocalPlayer(player))
            {
                return;
            }

            ApplySnapshot(exitSnapshotId);
            PlayOneShot(exitOneShotId);
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void ApplySnapshot(int snapshotId)
        {
            if (director == null || snapshotId < 0)
            {
                return;
            }

            director.SetSnapshotWithFade(snapshotId, fadeSecondsOverride);
        }

        private void PlayOneShot(int oneShotId)
        {
            if (director == null || oneShotId < 0)
            {
                return;
            }

            director.PlayOneShot(oneShotId);
        }

        private bool IsLocalPlayer(VRCPlayerApi player)
        {
            if (player == null)
            {
                return false;
            }

            return player.isLocal;
        }
    }
}
