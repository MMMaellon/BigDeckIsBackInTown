
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.Udon.Common;
using VRC.Udon.Common.Enums;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), DefaultExecutionOrder(69)]
    public class Deck : SmartObjectSyncListener
    {
        public GameObject deck_model;
        public GameObject empty_deck_model;
        public Material card_material;
        public Material hidden_card_material;
        public Transform cards_in_deck_parent;
        public Transform cards_outside_deck_parent;
        public CardPlacementSpot[] placement_spots;
        public bool reparent_cards = false;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(next_card))]
        public int _next_card = -1001;
        public int next_card
        {
            get => _next_card;
            set
            {
                if (_next_card >= 0 && _next_card < cards.Length)
                {
                    if (cards[_next_card].IsActiveState())
                    {
                        cards[_next_card].OnEnterState();
                    }
                }
                _next_card = value;
                if (_next_card >= 0 && _next_card < cards.Length)
                {
                    if (Networking.LocalPlayer.IsOwner(gameObject) && !cards[_next_card].IsActiveState())
                    {
                        cards[_next_card].EnterState();
                    }
                    else
                    {
                        cards[_next_card].OnEnterState();
                    }
                    if (deck_model)
                    {
                        deck_model.SetActive(true);
                    }
                    if (empty_deck_model)
                    {
                        empty_deck_model.SetActive(false);
                    }
                }
                else
                {
                    if (deck_model)
                    {
                        deck_model.SetActive(false);
                    }
                    if (empty_deck_model)
                    {
                        empty_deck_model.SetActive(true);
                    }
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public Card[] cards;
        [System.NonSerialized]
        public readonly DataList cards_in_decks = new DataList();
        [HideInInspector]
        public SmartObjectSync deck_sync;
        bool on_enable_ran = false;
        public void OnEnable()
        {
            if (on_enable_ran)
            {
                return;
            }
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].sync.state == cards[i].stateID)
                {
                    cards_in_decks.Add(new DataToken(cards[i]));
                }
            }
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                ResetDeck();
            }
            on_enable_ran = true;
        }

        DataToken temp_token;
        public bool automatically_pick_next_card = true;
        public void PickNextCard()
        {
            pick_next_card_requested = false;
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            if (!cards_in_decks.TryGetValue(Random.Range(0, cards_in_decks.Count), out temp_token))
            {
                Debug.LogError("Could not get next Card. Cards left in deck: " + cards_in_decks.Count);
                next_card = -1001;
                return;
            }
            next_card = ((Card)temp_token.Reference).id;
        }

        public void ResetDeck()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            //put all cards back in the deck and select a new card
            foreach (var card in cards)
            {
                if (!card.IsActiveState())
                {
                    card.EnterState();
                }
            }
        }

        Card temp_card;
        bool pick_next_card_requested = false;
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {

            temp_card = sync.GetComponent<Card>();
            if (temp_card == null)
            {
                return;
            }
            if (newState == temp_card.stateID + SmartObjectSync.STATE_CUSTOM)
            {
                //sync is entering card state, which means it's being returned to the deck
                if (!cards_in_decks.Contains(new DataToken(temp_card)))
                {
                    cards_in_decks.Add(new DataToken(temp_card));
                    if (automatically_pick_next_card && !pick_next_card_requested)
                    {
                        if (deck_sync && deck_sync.IsHeld())
                        {
                            if (deck_sync.IsOwnerLocal())
                            {
                                pick_next_card_requested = true;
                                SendCustomEventDelayedFrames(nameof(PickNextCard), 1);
                            }
                        }
                        else
                        {
                            if (sync.IsOwnerLocal())
                            {
                                pick_next_card_requested = true;
                                SendCustomEventDelayedFrames(nameof(PickNextCard), 1);
                            }
                        }
                    }
                }
            }
            else if (oldState == temp_card.stateID + SmartObjectSync.STATE_CUSTOM)
            {
                //card just left the deck; pick a new next card if we need to
                cards_in_decks.Remove(new DataToken(temp_card));
                if (next_card == temp_card.id)
                {
                    if (deck_sync && deck_sync.IsHeld())
                    {
                        if (automatically_pick_next_card && deck_sync.IsOwnerLocal())
                        {
                            PickNextCard();
                        }
                    }
                    else
                    {
                        if (automatically_pick_next_card && sync.IsOwnerLocal())
                        {
                            Networking.SetOwner(Networking.LocalPlayer, gameObject);
                            PickNextCard();
                        }
                    }
                }
            }
        }

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {
            deck_sync = GetComponent<SmartObjectSync>();
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].id = i;
                cards[i].deck = this;
                cards[i].sync.AddListener(this);
            }
            for (int i = 0; i < placement_spots.Length; i++)
            {
                placement_spots[i].id = i;
                placement_spots[i].deck = this;
            }
        }
        public void Reset()
        {
            cards_in_deck_parent = transform;
        }

#endif
    }
}
