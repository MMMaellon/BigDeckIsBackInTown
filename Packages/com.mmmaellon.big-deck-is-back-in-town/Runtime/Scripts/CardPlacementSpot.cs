
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardPlacementSpot : SmartObjectSyncListener
    {
        public int id = -1001;
        public bool _allow_throwing = true;
        public bool deal_on_enter_trigger = false;
        public bool deal_on_interact = true;
        public Deck deck;
        public Transform placement_transform;
        public Vector3 incoming_vel = Vector3.down;
        CardPlacingState temp_card;
        public DataList placed_cards;
        public virtual void Start()
        {
            DisableInteractive = !deal_on_interact;
        }
        public void OnTriggerEnter(Collider other)
        {
            if (!deal_on_enter_trigger || !Utilities.IsValid(other) || !Networking.LocalPlayer.IsOwner(other.gameObject))
            {
                return;
            }
            temp_card = other.GetComponent<CardPlacingState>();
            if (!temp_card || temp_card.card.deck != deck || !temp_card.sync.IsHeld() || temp_card.sync.interpolation < 0.99f)
            {
                return;
            }
            Place(temp_card);
        }

        public virtual void Deal()
        {
            if (deck.next_card < 0 || deck.next_card >= deck.cards.Length)
            {
                if (deck.deck_sync && deck.deck_sync.IsHeld() && !deck.deck_sync.IsLocalOwner())
                {
                    //avoid stealing ownership if someone is holding the deck
                    return;
                }
                deck.PickNextCard();
            }
            if (deck.next_card < 0 || deck.next_card >= deck.cards.Length)
            {
                return;
            }

            temp_card = deck.cards[deck.next_card].GetComponent<CardPlacingState>();
            if (temp_card)
            {
                Place(temp_card);
            }
        }

        public Animator animator;
        public string active_parameter = "allow_throwing";
        public virtual bool allow_throwing
        {
            get => _allow_throwing;
            set
            {
                _allow_throwing = value;
                if (animator)
                {
                    animator.SetBool(active_parameter, value);
                }
            }
        }

        public virtual bool AllowsThrowing()
        {
            return allow_throwing;
        }
        public virtual void Place(CardPlacingState card)
        {
            if (!card.sync.IsLocalOwner())
            {
                Networking.SetOwner(Networking.LocalPlayer, card.gameObject);
            }
            card.sync.pos = GetPlacementPosition(card);
            card.sync.rot = GetPlacementRotation(card);
            card.sync.vel = GetPlacementVelocity(card);
            card.EnterState();

        }
        public virtual Vector3 GetPlacementPosition(CardPlacingState card)
        {
            if (cards_to_deal <= 0)
            {
                return placement_transform.position;
            }
            cards_to_deal--;
            offset = new Vector3(((cards_to_deal) % row_length) * -horizontal_spacing, Mathf.CeilToInt(cards_to_deal / row_length) * vertical_spacing, 0);
            return placement_transform.position + placement_transform.rotation * (offset + end_offset);
        }
        public virtual Quaternion GetPlacementRotation(CardPlacingState card)
        {
            return placement_transform.rotation;
        }
        public virtual Vector3 GetPlacementVelocity(CardPlacingState card)
        {
            return incoming_vel;
        }
        public virtual Vector3 GetAimPosition(CardPlacingState card)
        {
            return placement_transform.position;
        }
        public virtual Quaternion GetAimRotation(CardPlacingState card)
        {
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
        public int row_length = 4;
        public float horizontal_spacing = 0.08f;
        public float vertical_spacing = 0.15f;
        [System.NonSerialized]
        public int cards_to_deal = -1001;
        //when negative, we just deal a single card
        //when positive, we deal multiple cards, but offset them based off the settings above
        [System.NonSerialized]
        public int _capacity;
        public int capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                rows = Mathf.CeilToInt(value / (float)row_length);
                if (value < row_length)
                {
                    end_offset = Vector3.right * (value / 2f) * horizontal_spacing;
                }
                else
                {
                    end_offset = new Vector3((row_length - 1) / 2f * horizontal_spacing, (rows - 1) / -2f * vertical_spacing, 0);
                }
            }
        }
        Vector3 end_offset;
        Vector3 offset;
        int rows = 0;
        public virtual void DealMultiple(int count)
        {
            capacity = count;
            cards_to_deal = count;
            DealLoop();
        }

        public float deal_delay = 0.2f;
        public virtual void DealLoop()
        {
            if (!deck.automatically_pick_next_card)
            {
                deck.PickNextCard();
            }
            temp_card = deck.cards[deck.next_card].GetComponent<CardPlacingState>();
            if (temp_card)
            {
                Place(temp_card);
            }
            if (cards_to_deal > 0)
            {
                SendCustomEventDelayedSeconds(nameof(DealLoop), deal_delay);
            }
        }
        CardText text_card;
        public virtual void DealSpecificCard(int text_id)
        {
            if (deck.cards_in_decks.Count > 0)
            {
                text_card = ((Card)deck.cards_in_decks[0].Reference).GetComponent<CardText>();
                Place(text_card.GetComponent<CardPlacingState>());
                text_card.text_id = text_id;
            }
        }

        public void Deal2()
        {
            DealMultiple(2);
        }
        public void Deal3()
        {
            DealMultiple(3);
        }
        public void Deal4()
        {
            DealMultiple(4);
        }
        public void Deal5()
        {
            DealMultiple(5);
        }
        public void Deal6()
        {
            DealMultiple(6);
        }
        public void Deal7()
        {
            DealMultiple(7);
        }
        public void Deal8()
        {
            DealMultiple(8);
        }
        public void Deal9()
        {
            DealMultiple(9);
        }
        public void Deal10()
        {
            DealMultiple(10);
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            placement_transform = transform;
        }


#endif
    }
}
