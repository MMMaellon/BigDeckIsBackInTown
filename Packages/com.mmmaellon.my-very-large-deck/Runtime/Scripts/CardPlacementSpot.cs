
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardPlacementSpot : UdonSharpBehaviour
    {
        public int id = -1001;
        public bool allow_throwing = false;
        public bool deal_on_enter_trigger = true;
        public bool deal_on_interact = true;
        public Deck deck;
        public Transform placement_point;
        CardPlacingState card;
        public void Start(){
            DisableInteractive = !deal_on_interact;
        }
        public void OnTriggerEnter(Collider other)
        {
            if (!deal_on_enter_trigger || !Utilities.IsValid(other) || !Networking.LocalPlayer.IsOwner(other.gameObject))
            {
                return;
            }
            card = other.GetComponent<CardPlacingState>();
            if (!card || card.card.deck != deck || !card.sync.IsHeld() || card.sync.interpolation < 0.99f)
            {
                return;
            }
            card.Place(this);
        }

        public override void Interact()
        {
            Deal();
        }

        public void Deal(){
            if (deck.next_card < 0 || deck.next_card >= deck.cards.Length)
            {
                if(deck.deck_sync && deck.deck_sync.IsHeld() && !deck.deck_sync.IsLocalOwner()){
                    //avoid stealing ownership if someone is holding the deck
                    return;
                }
                deck.PickNextCard();
            }
            if (deck.next_card < 0 || deck.next_card >= deck.cards.Length)
            {
                return;
            }

            card = deck.cards[deck.next_card].GetComponent<CardPlacingState>();
            if (card)
            {
                card.Place(this);
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            placement_point = transform;
        }
#endif
    }
}
