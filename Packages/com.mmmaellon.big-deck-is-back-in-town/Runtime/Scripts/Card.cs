
using MMMaellon.LightSync;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(LightSync.LightSync))]
    public class Card : LightSyncState
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
                if (sync.IsOwner())
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
                if (sync.IsOwner())
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
            sync.rigid.isKinematic = sync.kinematicFlag;
            if (deck.reparent_cards)
            {
                transform.SetParent(deck.cards_outside_deck_parent, true);
            }
        }

        public override bool OnLerp(float elapsedTime, float autoSmoothedLerp)
        {
            return false;
        }

        public void SetVisibility(bool visible_to_self, bool visible_to_others)
        {
            if ((visible_to_self && sync.IsOwner()) || (visible_to_others && !sync.IsOwner()))
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
            sync.pickup.pickupable = (pickupable_by_owner && sync.IsOwner()) || (pickupable_by_others && !sync.IsOwner());
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (IsActiveState())
            {
                SetVisibility(!IsActiveState(), !IsActiveState() && !visible_only_to_owner);
                SetPickupable(true, IsActiveState() || !pickupable_only_by_owner);
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public virtual void Setup()
        {
            if (!render_component)
            {
                render_component = GetComponent<Renderer>();
            }
            if (!collider_component)
            {
                collider_component = GetComponent<Collider>();
            }
            if (!throwing)
            {
                throwing = GetComponent<CardThrowing>();
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
