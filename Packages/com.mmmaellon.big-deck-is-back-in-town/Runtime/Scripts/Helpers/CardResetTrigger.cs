
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Deck))]
    public class CardResetTrigger : SmartObjectSyncListener
    {
        Card card;
        public bool only_while_pickup_use_down = false;

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
            if (!Utilities.IsValid(other) || (only_while_pickup_use_down && !pickup_use_down))
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

        bool pickup_use_down;
        public override void OnPickupUseDown()
        {
            pickup_use_down = true;
        }


        public override void OnPickupUseUp()
        {
            pickup_use_down = false;
        }
    }
}
