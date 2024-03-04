using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Card))]
    public class CardPlacingState : SmartObjectSyncState
    {
        public float deal_duration = 0.25f;
        public Vector3 deal_power = Vector3.up * 10f;
        public bool deal_on_throw = true;
        [Tooltip("Larger number requires better accuracy and shorter distances")]
        public float throw_threshold = 1f;
        public float throw_min_velocity = 0.5f;
        public Vector3 throw_power = Vector3.up * 5f;
        [UdonSynced, System.NonSerialized]
        public int _placement_id = -1001;
        public int placement_id
        {
            get => _placement_id;
            set
            {
                _placement_id = value;
                if (value < 0 || value >= card.deck.placement_spots.Length)
                {
                    spot = null;
                }
                else
                {
                    spot = card.deck.placement_spots[value];
                }
            }
        }
        [System.NonSerialized]
        public CardPlacementSpot spot;
        float real_interpolation;
        public override void Interpolate(float interpolation)
        {
            if (!spot)
            {
                start_time = Time.timeSinceLevelLoad;
                return;
            }

            real_interpolation = deal_duration <= 0 ? 1.0f : (Time.timeSinceLevelLoad - start_time) / deal_duration;
            transform.position = sync.HermiteInterpolatePosition(start_pos, start_vel, spot.placement_point.position, Vector3.zero, real_interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(start_rot, start_spin, spot.placement_point.rotation, Vector3.zero, real_interpolation);
            if (real_interpolation >= 1.0f && sync.IsOwnerLocal())
            {
                ExitState();
            }
        }

        public override void OnEnterState()
        {
            sync.rigid.detectCollisions = false;
        }

        public override void OnExitState()
        {
            sync.rigid.detectCollisions = true;
        }

        public override bool OnInterpolationEnd()
        {
            if (sync.IsOwnerLocal() && spot == null)
            {
                ExitState();
            }
            return true;
        }

        Vector3 start_pos;
        Quaternion start_rot;
        Vector3 start_vel;
        Vector3 start_spin;
        float start_time;
        public override void OnInterpolationStart()
        {
            start_time = Time.timeSinceLevelLoad;
            start_pos = transform.position;
            start_rot = transform.rotation;

            if (sync.lastState == card.stateID + SmartObjectSync.STATE_CUSTOM)
            {
                start_vel = sync.rigid.velocity + deal_power;
            }
            else
            {
                start_vel = sync.rigid.velocity + throw_power;
            }

            start_spin = sync.rigid.angularVelocity;
        }

        public override void OnSmartObjectSerialize()
        {

        }
        public void Place(CardPlacementSpot new_spot)
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            placement_id = new_spot.id;
            EnterState();
        }
        float new_score = -1001f;
        float best_score = -1001f;
        CardPlacementSpot best_spot;
        bool throw_primed = false;
        public override void OnPickup()
        {
            throw_primed = throw_primed || sync.state == card.stateID + SmartObjectSync.STATE_CUSTOM || (sync.IsHeld() && sync.lastState == card.stateID + SmartObjectSync.STATE_CUSTOM);
            Debug.LogWarning("throw: " + throw_primed);
            Debug.LogWarning("sync.state: " + sync.state);
            Debug.LogWarning("sync.lastState: " + sync.lastState);
        }
        public override void OnDrop()
        {
            if (!deal_on_throw || !throw_primed)
            {
                return;
            }
            throw_primed = false;
            SendCustomEventDelayedFrames(nameof(OnThrow), 2);
        }
        public void OnThrow()
        {
            if (sync.state != SmartObjectSync.STATE_INTERPOLATING || sync.rigid.velocity.magnitude < throw_min_velocity)
            {
                Debug.LogWarning("vel: " + sync.rigid.velocity.magnitude);
                return;
            }
            best_score = -1001f;
            best_spot = null;
            foreach (var spot in card.deck.placement_spots)
            {
                if (!spot.allow_throwing)
                {
                    continue;
                }
                new_score = Vector3.Dot(sync.rigid.velocity.normalized, (spot.placement_point.position - transform.position).normalized) / Mathf.Sqrt(Vector3.Distance(spot.placement_point.position, transform.position));

                if (new_score > best_score)
                {
                    best_score = new_score;
                    best_spot = spot;
                }
            }
            Debug.LogWarning("best_score: " + best_score);
            if (best_spot && best_score > throw_threshold)
            {
                Place(best_spot);
            }
        }
        [HideInInspector]
        public Card card;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            card = GetComponent<Card>();
            base.Reset();
        }
#endif
    }
}
