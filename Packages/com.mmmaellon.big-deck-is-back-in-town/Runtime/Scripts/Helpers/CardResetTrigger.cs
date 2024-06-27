
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardResetTrigger : LightSyncListener
    {
        public Deck deck;
        Card card;
        public bool only_while_pickup_use_down = false;

        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            if (only_while_pickup_use_down)
            {
                if (pickup_use_down)
                {
                    card = other.GetComponent<Card>();
                    if (!card)
                    {
                        return;
                    }
                    if (deck && deck != card.deck)
                    {
                        return;
                    }
                    if (card.sync.state == LightSync.LightSync.STATE_PHYSICS)
                    {
                        card.sync.Respawn();
                    }
                    else if (card.sync.IsHeld)
                    {
                        card.sync.AddClassListener(this);
                    }
                }
                return;
            }
            card = other.GetComponent<Card>();
            if (!card || !card.sync.IsOwner())
            {
                return;
            }
            if (deck && deck != card.deck)
            {
                return;
            }
            if (card.sync.state == LightSync.LightSync.STATE_PHYSICS)
            {
                card.sync.Respawn();
            }
            else if (card.sync.IsHeld)
            {
                card.sync.AddClassListener(this);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            card = other.GetComponent<Card>();
            if (!card)
            {
                return;
            }
            card.sync.RemoveClassListener(this);
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

        public override void OnChangeState(LightSync.LightSync sync, int prevState, int currentState)
        {
            sync.RemoveClassListener(this);
            if (currentState == LightSync.LightSync.STATE_PHYSICS)
            {
                sync.Respawn();
            }
        }

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi prevOwner, VRCPlayerApi currentOwner)
        {
            sync.RemoveClassListener(this);
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            deck = GetComponent<Deck>();
        }

#endif


    }
}
