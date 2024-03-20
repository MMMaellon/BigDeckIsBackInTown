
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class Card : SmartObjectSyncState
    {
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(visible_only_to_owner))]
        public bool _visible_only_to_owner = false;
        public bool visible_only_to_owner
        {
            get => _visible_only_to_owner;
            set
            {
                _visible_only_to_owner = value;
                SetVisibility(IsActiveState());
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
            SetVisibility(true);
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
            SetVisibility(false);
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

        public void SetVisibility(bool state_active)
        {
            if (state_active)
            {
                //invisible while in deck
                if (deck.hidden_card_material)
                {
                    render_component.sharedMaterial = deck.hidden_card_material;
                }
                if (child)
                {
                    child.SetActive(false);
                }
                return;
            }
            if (visible_only_to_owner && !sync.IsLocalOwner())
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
                child.SetActive(!visible_only_to_owner || sync.IsLocalOwner());
            }
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            Debug.LogWarning("OnOwnershipTransferred");
            SetVisibility(IsActiveState());
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
