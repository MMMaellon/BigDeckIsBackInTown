
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class GameSubmissionListener : UdonSharpBehaviour
    {
        public abstract void OnSubmit(CardPlacingState card, GameSubmissionSpot spot);
    }
}
