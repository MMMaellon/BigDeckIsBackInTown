
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
        [System.NonSerialized]
        public PlayerSpot local_spot;
        public abstract void OnSubmit(CardPlacingState card, GameSubmissionSpot spot);
        public abstract void OnTextSubmitted(string text, GameSubmissionSpot spot);
        public abstract void OnPlayerSpotActivation(bool activated, PlayerSpot spot);
        public abstract void OnPlayerSubmit(int selection, PlayerSpot spot);
    }
}
