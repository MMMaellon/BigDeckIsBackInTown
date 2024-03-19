
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Card))]
    public class CardThrowing : SmartObjectSyncState
    {
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(target_id))]
        public int _target_id;
        public int target_id
        {
            get => _target_id;
            set
            {
                _target_id = value;
                if (sync.IsLocalOwner())
                {
                    RequestSerialization();
                }

                if (value < 0 || !card.deck.throwing_handler || value >= card.deck.throwing_handler.targets.Length)
                {
                    target_cache = null;
                    return;
                }
                target_cache = card.deck.throwing_handler.targets[value];
                duration_cache = target_cache.throw_duration;
                power_cache = target_cache.throw_duration;
                card.deck.throwing_handler.OnThrowCard(value, this);
            }
        }
        CardThrowTarget target_cache;
        [System.NonSerialized]
        public float duration_cache = 0.25f;
        [Tooltip("Larger number requires better accuracy and shorter distances")]
        [System.NonSerialized]
        float real_interpolation;
        public override void Interpolate(float interpolation)
        {
            real_interpolation = duration_cache <= 0 ? 1.0f : Mathf.Min(1.0f, (Time.timeSinceLevelLoad - start_time) / duration_cache);
            transform.position = HermiteInterpolatePosition(start_pos, start_vel, sync.pos, sync.vel, real_interpolation);
            transform.rotation = HermiteInterpolateRotation(start_rot, sync.rot, real_interpolation);
            if (real_interpolation >= 1.0f)
            {
                sync.rigid.detectCollisions = true;
            }
        }
        Vector3 posControl1;
        Vector3 posControl2;
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            posControl1 = startPos + startVel * duration_cache * interpolation / 3f;
            posControl2 = endPos - endVel * duration_cache * (1.0f - interpolation) / 3f;
            return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
        }
        public Quaternion HermiteInterpolateRotation(Quaternion startRot, Quaternion endRot, float interpolation)
        {
            return Quaternion.Slerp(startRot, endRot, interpolation);
        }

        bool last_kinematic;
        public override void OnEnterState()
        {
            sync.rigid.detectCollisions = false;
            last_kinematic = sync.rigid.isKinematic;
            sync.rigid.isKinematic = true;
        }

        public override void OnExitState()
        {
            sync.rigid.detectCollisions = true;
            sync.rigid.isKinematic = last_kinematic;
        }

        public override bool OnInterpolationEnd()
        {
            if (real_interpolation >= 1.0f)
            {
                transform.position = sync.pos;
                transform.rotation = sync.rot;
                return false;
            }
            return true;
        }

        Vector3 start_pos;
        Quaternion start_rot;
        Vector3 start_vel;
        Vector3 start_spin;
        float start_time;
        float power_cache;
        public override void OnInterpolationStart()
        {
            start_time = Time.timeSinceLevelLoad;
            start_pos = transform.position;
            start_rot = transform.rotation;
            start_vel = sync.rigid.velocity;
            start_spin = sync.rigid.angularVelocity;
        }

        public override void OnSmartObjectSerialize()
        {
            //we already serialized in Place();
        }

        CardThrowTarget temp_target;
        bool throw_primed = false;
        public override void OnPickup()
        {
            throw_primed = throw_primed || sync.state == card.stateID + SmartObjectSync.STATE_CUSTOM || (sync.IsHeld() && sync.lastState == card.stateID + SmartObjectSync.STATE_CUSTOM);
        }
        public override void OnDrop()
        {
            if (!card.deck.throwing_handler || !card.deck.throwing_handler.allow_throwing || (!throw_primed && (card.deck.throwing_handler.prime_throw_on_pickup_use_down || card.deck.throwing_handler.first_throw_only)))
            {
                return;
            }
            throw_primed = false;
            SendCustomEventDelayedFrames(nameof(OnThrow), 2);
        }
        public override void OnPickupUseDown()
        {
            if (!card.deck.throwing_handler || card.deck.throwing_handler.prime_throw_on_pickup_use_down)
            {
                return;
            }
            throw_primed = true;
        }
        public void OnThrow()
        {
            if (!card.deck.throwing_handler && sync.state != SmartObjectSync.STATE_INTERPOLATING || !sync.IsLocalOwner())
            {
                return;
            }
            if (card.deck.throwing_handler.desktop_throw_assist && !sync.owner.IsUserInVR() && (card.deck.throwing_handler.last_right_click < 0 || card.deck.throwing_handler.last_right_click + 4 > Time.frameCount))
            {
                sync.rigid.velocity = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * card.deck.throwing_handler.desktop_throw_boost;
            }

            temp_target = card.deck.throwing_handler.LocateBestTarget(transform.position, card.sync.rigid.velocity);
            if (temp_target)
            {
                temp_target.DealCard(this);
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
