
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Card))]
    public class CardHolderState : LightSyncState
    {
        public CardHolderManager manager;
        public Card card;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(holder_id))]
        public short _holder_id = -1001;
        public short holder_id
        {
            get => _holder_id;
            set
            {
                _holder_id = value;
                if (value < 0 || value >= manager.holders.Length)
                {
                    holder = null;
                    Detach();
                }
                else
                {
                    holder = manager.holders[value];
                    AttachIfSynced();
                }
                if (sync.IsOwner())
                {
                    RequestSerialization();
                }
            }
        }
        [System.NonSerialized]
        public CardHolder holder = null;
        public override bool OnLerp(float elapsedTime, float autoSmoothedLerp)
        {
            if (holder != null)
            {
                sync.ApplyRelativeTransforms(holder.transform.position, holder.transform.rotation, sync.HermiteInterpolatePosition(start_pos, Vector4.zero, sync.pos, Vector3.zero, autoSmoothedLerp, elapsedTime), Quaternion.Slerp(start_rot, sync.rot, autoSmoothedLerp));
            }
            return autoSmoothedLerp >= 0;
        }

        Vector3 start_pos;
        Quaternion start_rot;
        public override void OnEnterState()
        {
            AttachIfSynced();
        }

        public override void OnExitState()
        {
            Detach();
        }

        public void Attach(CardHolder holder)
        {
            if (!sync.IsOwner())
            {
                sync.TakeOwnership();
            }
            holder_id = holder.id;
            EnterState();
        }

        public void AttachIfSynced()
        {
            if (holder != null && IsActiveState())
            {
                transform.SetParent(holder.transform, true);
                sync.rigid.isKinematic = true;
                start_pos = transform.localPosition;
                start_rot = transform.localRotation;

                card.SetVisibility(true, !holder.visible_only_to_owner);
                card.SetPickupable(true, !holder.pickupable_only_by_owner);
            }
        }
        public void Detach()
        {
            if (holder_id >= 0)
            {
                holder_id = -1001;
            }
            transform.SetParent(card.deck.cards_outside_deck_parent);
            sync.rigid.isKinematic = !card.card_physics;
            card.SetVisibility(true, !card.visible_only_to_owner);
            card.SetPickupable(true, !card.pickupable_only_by_owner);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {
            manager = GameObject.FindObjectOfType<CardHolderManager>();
        }
        public override void Reset()
        {
            card = GetComponent<Card>();
            base.Reset();
        }

#endif
    }
}
