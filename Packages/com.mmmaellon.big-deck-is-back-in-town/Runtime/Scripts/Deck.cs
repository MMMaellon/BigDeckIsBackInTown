
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Deck : SmartObjectSyncListener
    {
        public Card[] cards;
        public CardThrowingHandler throwing_handler;
        public bool draw_cards_with_grab = true;
        public bool reparent_cards = false;
        [Header("Optional")]
        public GameObject deck_model;
        public GameObject empty_deck_model;
        public Material card_material;
        public Material hidden_card_material;
        public Transform cards_in_deck_parent;
        public Transform cards_outside_deck_parent;
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
        float last_sync_time;
        int last_next_card_value;
        public override void OnDeserialization(VRC.Udon.Common.DeserializationResult result)
        {
            if (result.sendTime < last_sync_time)
            {
                next_card = last_next_card_value;
                return;
            }
            last_sync_time = result.sendTime;
            last_next_card_value = next_card;
        }

        [System.NonSerialized]
        public DataList cards_in_decks = new DataList();
        [HideInInspector]
        public SmartObjectSync deck_sync;
        public void OnEnable()
        {
            next_card = next_card;
        }

        public void Start()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].sync.state == cards[i].stateID)
                {
                    cards_in_decks.Add(cards[i]);
                }
            }
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                ResetDeck();
            }
            else
            {
                next_card = next_card;
            }
        }

        DataToken temp_token;
        public void PickNextCard()
        {
            pick_next_card_requested = false;
            if (!draw_cards_with_grab)
            {
                return;
            }
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            next_card = RandomCardId(false);
        }

        public int RandomCardId(bool include_drawn_cards)
        {

            if (!cards_in_decks.TryGetValue(Random.Range(0, cards_in_decks.Count), out temp_token))
            {
                Debug.LogError("Could not get random Card id. Cards left in deck: " + cards_in_decks.Count);
                if (include_drawn_cards)
                {
                    return Random.Range(0, cards.Length);
                }
                return -1001;
            }
            return ((Card)temp_token.Reference).id;
        }

        public Card RandomCard(bool include_drawn_cards)
        {
            int random_id = RandomCardId(include_drawn_cards);
            if (random_id < 0 || random_id >= cards.Length)
            {
                return null;
            }
            return cards[random_id];
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
                if (!cards_in_decks.Contains(temp_card))
                {
                    cards_in_decks.Add(temp_card);
                    if (draw_cards_with_grab && !pick_next_card_requested)
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
                cards_in_decks.Remove(temp_card);
                if (next_card == temp_card.id)
                {
                    if (deck_sync && deck_sync.IsHeld())
                    {
                        if (draw_cards_with_grab && deck_sync.IsOwnerLocal())
                        {
                            PickNextCard();
                        }
                    }
                    else
                    {
                        if (draw_cards_with_grab && sync.IsOwnerLocal())
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

            if (cards.Length > 0)
            {
                HashSet<Card> valid_cards = new HashSet<Card>();

                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i])
                    {
                        valid_cards.Add(cards[i]);
                    }
                }
                cards = valid_cards.ToArray<Card>();
            }
            else
            {
                cards = GetComponentsInChildren<Card>();
            }

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].id = i;
                cards[i].deck = this;
                cards[i].sync.AddListener(this);
            }
        }
        public void Reset()
        {
            cards_in_deck_parent = transform;
        }

#endif
    }
}
