
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardResetTrigger : SmartObjectSyncListener
    {
        Card card;

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            sync.RemoveListener(this);
            if (!sync.IsHeld() && sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                sync.Respawn();
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            card = other.GetComponent<Card>();
            if (!card || !card.sync.IsOwnerLocal())
            {
                return;
            }
            if (!card.sync.IsHeld() && card.sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                card.sync.Respawn();
                return;
            }
            card.sync.AddListener(this);
        }
        SmartObjectSync sync;
        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            sync = other.GetComponent<SmartObjectSync>();
            if (!sync)
            {
                return;
            }
            sync.RemoveListener(this);
        }
    }
}
