﻿using UdonSharp;
using UnityEngine;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Card))]
    public class CardPlacingState : SmartObjectSyncState
    {
        public float deal_duration = 0.25f;
        public Vector3 deal_power = Vector3.up * 10f;
        public bool deal_on_throw = true;
        [Tooltip("Larger number requires better accuracy and shorter distances")]
        public float throw_threshold = 2f;
        public float throw_min_velocity = 1f;
        public Vector3 throw_power = Vector3.up * 5f;
        [System.NonSerialized]
        float real_interpolation;
        public override void Interpolate(float interpolation)
        {
            real_interpolation = deal_duration <= 0 ? 1.0f : (Time.timeSinceLevelLoad - start_time) / deal_duration;
            transform.position = sync.HermiteInterpolatePosition(start_pos, start_vel, sync.pos, Vector3.zero, real_interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(start_rot, start_spin, sync.rot, Vector3.zero, real_interpolation);
            if (real_interpolation >= 1.0f && sync.IsOwnerLocal())
            {
                sync.rigid.detectCollisions = true;
            }
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
            return real_interpolation < 1.0f;
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
            //we already serialized in Place();
        }


        float new_score = -1001f;
        float best_score = -1001f;
        CardPlacementSpot best_spot;
        bool throw_primed = false;
        public override void OnPickup()
        {
            throw_primed = throw_primed || sync.state == card.stateID + SmartObjectSync.STATE_CUSTOM || (sync.IsHeld() && sync.lastState == card.stateID + SmartObjectSync.STATE_CUSTOM);
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
                return;
            }
            best_score = -1001f;
            best_spot = null;
            foreach (var current_spot in card.deck.placement_spots)
            {
                if (!current_spot.AllowsThrowing())
                {
                    continue;
                }
                new_score = Vector3.Dot(sync.rigid.velocity, (current_spot.GetAimPosition(this) - transform.position).normalized) / Mathf.Pow(Vector3.Distance(current_spot.GetAimPosition(this), transform.position), 0.3f);

                if (new_score > best_score)
                {
                    best_score = new_score;
                    best_spot = current_spot;
                }
            }
            if (best_spot && best_score > throw_threshold)
            {
                best_spot.Place(this);
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
