
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class Card : SmartObjectSyncState
    {
        [UdonSynced, FieldChangeCallback(nameof(visible_only_to_owner))]
        public bool _visible_only_to_owner = false;
        public bool visible_only_to_owner
        {
            get => _visible_only_to_owner;
            set
            {
                _visible_only_to_owner = value;
                SetVisibility(!IsActiveState(), !IsActiveState() && !value);
                if (sync.IsLocalOwner())
                {
                    RequestSerialization();
                }
            }
        }
        [UdonSynced, FieldChangeCallback(nameof(pickupable_only_by_owner))]
        public bool _pickupable_only_by_owner = false;
        public bool pickupable_only_by_owner
        {
            get => _pickupable_only_by_owner;
            set
            {
                _pickupable_only_by_owner = value;
                SetPickupable(true, IsActiveState() || !value);
                if (sync.IsLocalOwner())
                {
                    RequestSerialization();
                }
            }
        }
        public int id;
        public bool card_physics = true;
        public Renderer render_component;
        public GameObject child;
        public CardThrowing throwing;
        public Collider collider_component;
        public Deck deck;

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
            SetVisibility(false, false);
            SetPickupable(true, true);
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
            SetVisibility(true, !visible_only_to_owner);
            SetPickupable(true, !pickupable_only_by_owner);
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

        public void SetVisibility(bool visible_to_self, bool visible_to_others)
        {
            if ((visible_to_self && sync.IsLocalOwner()) || (visible_to_others && !sync.IsLocalOwner()))
            {
                if (deck.card_material)
                {
                    render_component.sharedMaterial = deck.card_material;
                }
                if (child)
                {
                    child.SetActive(true);
                }
            }
            else
            {
                if (deck.hidden_card_material)
                {
                    render_component.sharedMaterial = deck.hidden_card_material;
                }
                if (child)
                {
                    child.SetActive(false);
                }
            }
        }
        public void SetPickupable(bool pickupable_by_owner, bool pickupable_by_others)
        {
            sync.pickupable = (sync.IsLocalOwner() && pickupable_by_owner) || (sync.IsLocalOwner() && pickupable_by_others);
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (IsActiveState() || sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                SetVisibility(!IsActiveState(), !IsActiveState() && !visible_only_to_owner);
                SetPickupable(true, IsActiveState() || !pickupable_only_by_owner);
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            render_component = GetComponent<Renderer>();
            collider_component = GetComponent<Collider>();
            base.Reset();
        }
        public virtual void OnValidate()
        {
            throwing = GetComponent<CardThrowing>();
        }
#endif
    }
}
