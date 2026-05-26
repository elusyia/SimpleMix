using UdonSharp;
using UnityEngine;

namespace LAuX.SimpleMix
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SimpleMixTriggerManager : UdonSharpBehaviour
    {
        public SimpleMixDirector director;

        [HideInInspector]
        public SimpleMixZoneTrigger[] zones;

        [HideInInspector]
        public SimpleMixEventTrigger[] interactTriggers;
    }
}
