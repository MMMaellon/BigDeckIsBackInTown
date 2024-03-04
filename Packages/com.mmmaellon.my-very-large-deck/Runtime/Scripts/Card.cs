
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

        Transform starting_parent;
        bool start_parent_set = false;
        public void Start()
        {
            if (!start_parent_set)
            {
                starting_parent = transform.parent;
                start_parent_set = true;
            }
        }

        bool last_kinematic;
        public override void OnEnterState()
        {
            collider_component.enabled = _selected;
            collider_component.isTrigger = true;
            render_component.enabled = _selected;
            transform.position = deck.card_attach_point.position;
            transform.rotation = deck.card_attach_point.rotation;
            last_kinematic = sync.rigid.isKinematic;
            sync.rigid.isKinematic = true;
            if (deck.reparent_cards_to_attach_point)
            {
                if (!start_parent_set)
                {
                    starting_parent = transform.parent;
                    start_parent_set = true;
                }
                transform.SetParent(deck.card_attach_point, true);
            }
        }

        public override void OnExitState()
        {
            collider_component.enabled = true;
            collider_component.isTrigger = false;
            render_component.enabled = true;
            sync.rigid.isKinematic = last_kinematic;
            _selected = false;
            if (deck.reparent_cards_to_attach_point)
            {
                transform.SetParent(starting_parent, true);
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
