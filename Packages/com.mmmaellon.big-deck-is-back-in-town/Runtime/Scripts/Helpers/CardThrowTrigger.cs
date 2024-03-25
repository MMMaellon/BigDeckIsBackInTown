
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.BigDeckIsBackInTown
{
    public class CardThrowTrigger : SmartObjectSyncListener
    {
        public CardThrowTarget target;
        public bool respect_allow_throwing_setting = true;
        CardThrowing card;

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            sync.RemoveListener(this);
            if (!sync.IsHeld() && sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                card = sync.GetComponent<CardThrowing>();
                if (card && (!respect_allow_throwing_setting || target.allow_throwing))
                {
                    target.DealCard(card);
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            card = other.GetComponent<CardThrowing>();
            if (!card || !card.sync.IsOwnerLocal())
            {
                return;
            }
            if (target.deck && target.deck != card.card.deck)
            {
                return;
            }
            if (!card.sync.IsHeld() && card.sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                if (!respect_allow_throwing_setting || target.allow_throwing)
                {
                    target.DealCard(card);
                }
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
