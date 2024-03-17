
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync)), RequireComponent(typeof(CardPlacingState))]
    public class Card : SmartObjectSyncState
    {
        [UdonSynced, FieldChangeCallback(nameof(hidden))]
        public bool _hidden = false;
        public bool hidden
        {
            get => _hidden;
            set
            {
                _hidden = value;
                if (value)
                {
                    if (deck.hidden_card_material)
                    {
                        render_component.sharedMaterial = deck.hidden_card_material;
                    }
                }
                else
                {
                    if (deck.card_material)
                    {
                        render_component.sharedMaterial = deck.card_material;
                    }
                }
                if (child)
                {
                    child.SetActive(!value && !IsActiveState());
                }
            }
        }
        public int id;
        public bool card_physics = true;
        public Renderer render_component;
        public GameObject child;
        public Collider collider_component;
        public Deck deck;
        public CardPlacingState placingState;

        public override void OnDrop()
        {
            SendCustomEventDelayedFrames(nameof(AfterDrop), 2);
        }
        public void AfterDrop()
        {
            if (sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                sync.rigid.isKinematic = !card_physics;
            }
        }

        public override void OnEnterState()
        {
            collider_component.enabled = deck.next_card == id;
            collider_component.isTrigger = true;
            render_component.enabled = false;
            if (child)
            {
                child.SetActive(false);
            }
            transform.position = deck.cards_in_deck_parent.position;
            transform.rotation = deck.cards_in_deck_parent.rotation;
            sync.rigid.isKinematic = true;
            if (deck.reparent_cards)
            {
                transform.SetParent(deck.cards_in_deck_parent, true);
            }
        }

        public override void OnExitState()
        {
            collider_component.enabled = true;
            collider_component.isTrigger = false;
            render_component.enabled = true;
            hidden = hidden;
            sync.rigid.isKinematic = !card_physics;
            if (deck.reparent_cards)
            {
                transform.SetParent(deck.cards_outside_deck_parent, true);
            }
        }

        public override void OnSmartObjectSerialize()
        {

        }

        public override void OnInterpolationStart()
        {

        }

        public override void Interpolate(float interpolation)
        {

        }

        public override bool OnInterpolationEnd()
        {
            return false;
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            render_component = GetComponent<Renderer>();
            collider_component = GetComponent<Collider>();
            placingState = GetComponent<CardPlacingState>();
            base.Reset();
        }
#endif
    }
}
