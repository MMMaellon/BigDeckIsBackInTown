
using MMMaellon.LightSync;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    public class CardThrowTarget : UdonSharpBehaviour
    {
        public int id;
        public Deck deck;
        public Transform target_center;
        [Tooltip("0 or negative means unlimited")]
        public int card_limit = 0;
        [Header("Visibility")]
        public bool change_card_visibility = false;
        public bool visible_only_to_owner = false;
        public bool persist_visibility_change = true;
        public bool change_card_pickupable = false;
        public bool pickupable_only_by_owner = false;
        public bool persist_pickupable_change = true;
        [Header("Throw arc properties")]
        public Vector3 start_vel = Vector3.up * 10f;
        public Vector3 end_vel = Vector3.down * 5f;
        public float throw_duration = 0.25f;
        [Header("Aiming properties")]
        public float velocity_threshold = 1.5f;
        public float power_multiplier = 1.0f;
        public float power_threshold = 0.25f;
        public string active_parameter = "allow_throwing";
        public bool _allow_throwing = true;
        public bool _limit_reached = false;
        [Header("Delay between cards when multiple cards are dealt.")]
        public float deal_delay = 0.2f;

        [System.NonSerialized]
        public int deal_multiple_count;
        [Header("Optional")]
        public Animator animator;
        public TextMeshPro nameplate;
        public virtual bool allow_throwing
        {
            get => _allow_throwing && !_limit_reached;
            set
            {
                _allow_throwing = value;
                if (animator)
                {
                    animator.SetBool(active_parameter, value);
                }
            }
        }

        public virtual Vector3 GetTargetPosition(int index, int capacity)
        {
            if (index < 0 || capacity <= 0 || index >= capacity)
            {
                return target_center.position;
            }
            if (capacity != capacity_cache)
            {
                //forces some calculations, we want to skip this if we can
                capacity_cache = capacity;
            }
            offset = new Vector3(index % row_length * -horizontal_spacing, Mathf.CeilToInt(index / row_length) * -vertical_spacing, 0);
            return target_center.position + target_center.rotation * (offset + start_offset);
        }

        public virtual Quaternion GetTargetRotation(int index, int capacity)
        {
            return target_center.rotation;
        }

        public virtual Vector3 GetStartVelocity(int index, int capacity)
        {
            return start_vel;
        }
        public virtual Vector3 GetEndVelocity(int index, int capacity)
        {
            return end_vel;
        }
        public virtual Vector3 GetAimPosition(Vector3 position, Vector3 forward)
        {
            return GetTargetPosition(deal_multiple_count - cards_to_deal, deal_multiple_count);
        }
        [System.NonSerialized]
        public int cards_dealt = 0;
        public virtual void DealCard(CardThrowing card)
        {
            if (!card)
            {
                cards_to_deal--;
                return;
            }
            if (!card.sync.IsOwner())
            {
                card.sync.TakeOwnership();
            }
            card.sync.pos = GetTargetPosition(deal_multiple_count - cards_to_deal, deal_multiple_count);
            card.sync.rot = GetTargetRotation(deal_multiple_count - cards_to_deal, deal_multiple_count);
            card.sync.vel = GetEndVelocity(deal_multiple_count - cards_to_deal, deal_multiple_count);
            card.sync.rigid.velocity += GetStartVelocity(deal_multiple_count - cards_to_deal, deal_multiple_count);
            card.target_id = id;
            card.EnterState();
            cards_to_deal--;
            if (card_limit > 0)
            {
                cards_dealt++;
                // card.sync.AddClassListener((LightSyncListener)this);
                _limit_reached = cards_dealt >= card_limit;
            }
        }

        public void RemoveCard()
        {
            cards_dealt--;
            _limit_reached = card_limit <= 0 || cards_dealt >= card_limit;
        }

        // public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        // {
        //
        // }
        public int row_length = 4;
        public float horizontal_spacing = 0.08f;
        public float vertical_spacing = 0.15f;
        [System.NonSerialized]
        public int cards_to_deal = -1001;
        //when negative, we just deal a single card
        //when positive, we deal multiple cards, but offset them based off the settings above
        [System.NonSerialized]
        public int _capacity_cache;
        public int capacity_cache
        {
            get => _capacity_cache;
            set
            {
                _capacity_cache = value;
                rows = Mathf.CeilToInt(value / (float)row_length);
                if (value < row_length)
                {
                    start_offset = Vector3.right * ((value - 1) / 2f) * horizontal_spacing;
                }
                else
                {
                    start_offset = new Vector3((row_length - 1) / 2f * horizontal_spacing, (rows - 1) / 2f * vertical_spacing, 0);
                }
            }
        }

        Vector3 start_offset;
        Vector3 offset;
        int rows = 0;
        public virtual void DealMultiple(int count)
        {
            deal_multiple_count = count;
            cards_to_deal = count;
            DealLoop();
        }

        Card temp_card;
        public virtual void DealLoop()
        {
            temp_card = deck.RandomCard(false);
            if (!temp_card)
            {
                return;
            }
            DealCard(temp_card.throwing);
            if (cards_to_deal > 0)
            {
                SendCustomEventDelayedSeconds(nameof(DealLoop), deal_delay);
            }
        }

        public void SendDeal1()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal1));
        }
        public void SendDeal2()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal2));
        }
        public void SendDeal3()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal3));
        }
        public void SendDeal4()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal4));
        }
        public void SendDeal5()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal5));
        }
        public void SendDeal6()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal6));
        }
        public void SendDeal7()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal7));
        }
        public void SendDeal8()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal8));
        }
        public void SendDeal9()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal9));
        }
        public void SendDeal10()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Deal10));
        }

        public void Deal1()
        {
            DealMultiple(-1001);
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
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            target_center = transform;
        }
#endif
    }
}
