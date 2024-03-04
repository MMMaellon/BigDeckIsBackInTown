
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Enums;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardPlacementSpot : UdonSharpBehaviour
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
            if(hold_slider){
                hold_slider.gameObject.SetActive(false);
            }
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
        public float hold_duration = 0.5f;
        public UnityEngine.UI.Slider hold_slider;
        bool holding_left = false;
        bool holding_right = false;
        public override void Interact()
        {
            StartDealHold();
        }

        float hold_start = -1001f;
        public void StartDealHold()
        {
            if (hold_start >= 0)
            {
                return;
            }
            holding_left = true;
            holding_right = true;
            if (hold_duration == 0)
            {
                Deal();
                return;
            }
            hold_start = Time.timeSinceLevelLoad;
            SendCustomEventDelayedFrames(nameof(HoldLoop), 2, EventTiming.Update);//2 frames to account for vrchat weirdness
        }
        public void HoldLoop()
        {
            if (hold_start < 0)
            {
                return;
            }
            holding_left = holding_left && left_trigger;
            holding_right = holding_right && right_trigger;
            if (hold_start + hold_duration < Time.timeSinceLevelLoad)
            {
                AbortHold();
                Deal();
            }
            else if (!holding_right && !holding_left)
            {
                AbortHold();
            }
            else
            {
                SendCustomEventDelayedFrames(nameof(HoldLoop), 1, EventTiming.Update);
                if (hold_slider != null)
                {
                    hold_slider.gameObject.SetActive(true);
                    hold_slider.value = (Time.timeSinceLevelLoad - hold_start) / hold_duration;
                }
            }
        }
        public void AbortHold()
        {
            hold_start = -1001f;
            if (hold_slider)
            {
                hold_slider.gameObject.SetActive(false);
                hold_slider.value = 0;
            }
        }
        bool left_trigger;
        bool right_trigger;
        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (args.handType == HandType.LEFT)
            {
                left_trigger = value;
            }

            if (args.handType == HandType.RIGHT)
            {
                right_trigger = value;
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            placement_transform = transform;
        }
#endif
    }
}
