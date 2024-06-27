
using MMMaellon.LightSync;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    public class CardThrowTrigger : LightSyncListener
    {
        public CardThrowTarget target;
        public bool respect_allow_throwing_setting = true;
        CardThrowing card;

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(LightSync.LightSync sync, int oldState, int newState)
        {
            sync.RemoveClassListener(this);
            if (card.sync.state == LightSync.LightSync.STATE_PHYSICS)
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
            if (!card || !card.sync.IsOwner())
            {
                return;
            }
            if (target.deck && target.deck != card.card.deck)
            {
                return;
            }
            if (card.sync.state == LightSync.LightSync.STATE_PHYSICS)
            {
                if (!respect_allow_throwing_setting || target.allow_throwing)
                {
                    target.DealCard(card);
                }
                return;
            }
            card.sync.AddClassListener(this);
        }
        LightSync.LightSync sync;
        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            sync = other.GetComponent<LightSync.LightSync>();
            if (!sync)
            {
                return;
            }
            sync.RemoveClassListener(this);
        }


    }
}
