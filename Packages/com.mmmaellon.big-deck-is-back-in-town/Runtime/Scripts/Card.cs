
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class Card : SmartObjectSyncState
    {
        public int id;
        public bool card_physics = true;
        public Renderer render_component;
        public Collider collider_component;
        [NonSerialized, UdonSynced, FieldChangeCallback(nameof(selected))]
        public bool _selected = false;
        public bool selected
        {
            get => _selected;
            set
            {
                _selected = value;

                if (IsActiveState())
                {
                    collider_component.enabled = _selected;
                    render_component.enabled = _selected;
                }

                if (sync.IsOwnerLocal())
                {
                    RequestSerialization();
                }
            }
        }
        public Deck deck;

        public override void OnDrop(){
            SendCustomEventDelayedFrames(nameof(AfterDrop), 2);
        }
        public void AfterDrop(){
            if(sync.state < SmartObjectSync.STATE_CUSTOM){
                sync.rigid.isKinematic = !card_physics;
            }
        }

        public override void OnEnterState()
        {
            collider_component.enabled = _selected;
            collider_component.isTrigger = true;
            render_component.enabled = _selected;
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
            sync.rigid.isKinematic = !card_physics;
            _selected = false;
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
            base.Reset();
        }
#endif
    }
}
