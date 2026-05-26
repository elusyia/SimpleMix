using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace LAuX.SimpleMix
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleMixEventTrigger : UdonSharpBehaviour
    {
        public SimpleMixDirector director;
        public int snapshotId = -1;
        public bool useSnapshot;
        public float fadeSecondsOverride = -1f;
        public int oneShotId = -1;
        public bool useOneShot = true;
        public bool playOnEnter;
        public bool playOnInteract = true;
        public bool triggerOnce;
        public float cooldownSeconds;

        private bool hasTriggered;
        private float nextAllowedTime;

        public override void Interact()
        {
            if (!playOnInteract)
            {
                return;
            }

            Trigger();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!playOnEnter || !IsLocalPlayer(player))
            {
                return;
            }

            Trigger();
        }

        public void Trigger()
        {
            if (director == null)
            {
                return;
            }

            if (triggerOnce && hasTriggered)
            {
                return;
            }

            if (Time.time < nextAllowedTime)
            {
                return;
            }

            if (useSnapshot && snapshotId >= 0)
            {
                director.SetSnapshotWithFade(snapshotId, fadeSecondsOverride);
            }

            if (useOneShot && oneShotId >= 0)
            {
                director.PlayOneShot(oneShotId);
            }

            hasTriggered = true;
            if (cooldownSeconds > 0f)
            {
                nextAllowedTime = Time.time + cooldownSeconds;
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
            nextAllowedTime = 0f;
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
