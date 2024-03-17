
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerSpot : CardPlacementSpot
    {
        public int spot_id;
        public TextMeshPro nameplate;
        public GameSubmissionListener listener;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(activated))]
        public bool _activated;
        public bool activated
        {
            get => _activated;
            set
            {
                if (listener && value != _activated)
                {
                    listener.OnPlayerSpotActivation(value, this);
                }
                _activated = value;
                if (animator)
                {
                    animator.SetBool("active", value);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                if (value)
                {
                    if (nameplate)
                    {
                        nameplate.text = Networking.GetOwner(gameObject).displayName;
                    }
                    if (listener && Networking.LocalPlayer.IsOwner(gameObject))
                    {
                        if (listener.local_spot)
                        {
                            listener.local_spot.Deactivate();
                        }
                        listener.local_spot = this;
                    }
                }
                else
                {
                    if (nameplate)
                    {
                        nameplate.text = "";
                    }
                    if (listener && listener.local_spot == this && Networking.LocalPlayer.IsOwner(gameObject))
                    {
                        listener.local_spot = null;
                    }
                }
            }
        }

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(selection))]
        public int _selection;
        public int selection
        {
            get => _selection;
            set
            {
                _selection = value;
                if (listener)
                {
                    listener.OnPlayerSubmit(value, this);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
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
        public override Vector3 GetPlacementPosition(CardPlacingState card)
        {
            if (cards_to_deal <= 0)
            {
                return base.GetPlacementPosition(card);
            }
            cards_to_deal--;
            offset = new Vector3(((cards_to_deal) % row_length) * -horizontal_spacing, Mathf.CeilToInt(cards_to_deal / row_length) * vertical_spacing, 0);
            return placement_transform.position + placement_transform.rotation * (offset + end_offset);
        }

        public void DealMultiple(int count)
        {
            capacity = count;
            cards_to_deal = count;
            DealLoop();
        }

        public float deal_delay = 0.2f;
        CardPlacingState temp_card;
        public void DealLoop()
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

        public void OnEnable()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                activated = false;
            }
            activated = activated;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                if (activated)
                {
                    activated = false;
                }
            }
        }

        public void Activate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            activated = true;
        }

        public void Deactivate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            activated = false;
        }

        public void ToggleActivation()
        {
            if (activated && Networking.LocalPlayer.IsOwner(gameObject))
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }
    }
}
