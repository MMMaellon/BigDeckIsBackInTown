
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardPlacementSpot : SmartObjectSyncListener
    {
        public int id = -1001;
        public bool allow_throwing = true;
        public bool deal_on_enter_trigger = false;
        public bool deal_on_interact = true;
        public Deck deck;
        public Transform placement_transform;
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
            Place(card);
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
                Place(card);
            }
        }
        public virtual bool AllowsThrowing(){
            return allow_throwing;
        }
        public virtual void Place(CardPlacingState card){
            if (!card.sync.IsLocalOwner())
            {
                Networking.SetOwner(Networking.LocalPlayer, card.gameObject);
            }
            card.sync.pos = GetPlacementPosition(card);
            card.sync.rot = GetPlacementRotation(card);
            card.EnterState();
        }
        public virtual Vector3 GetPlacementPosition(CardPlacingState card){
            return placement_transform.position;
        }
        public virtual Quaternion GetPlacementRotation(CardPlacingState card){
            return placement_transform.rotation;
        }
        public virtual Vector3 GetAimPosition(CardPlacingState card){
            return placement_transform.position;
        }
        public virtual Quaternion GetAimRotation(CardPlacingState card){
            return placement_transform.rotation;
        }
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            //for extending if you make custom spots
        }

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            //for extending if you make custom spots
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            placement_transform = transform;
        }


#endif
    }
}
