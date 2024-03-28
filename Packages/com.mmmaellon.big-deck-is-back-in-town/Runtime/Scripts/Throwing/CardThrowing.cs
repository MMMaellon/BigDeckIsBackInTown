
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
                    target = null;
                    return;
                }
                target = card.deck.throwing_handler.targets[value];
                duration_cache = target.throw_duration;
                power_cache = target.throw_duration;
                card.deck.throwing_handler.OnThrowCard(value, this);
            }
        }
        [System.NonSerialized]
        public CardThrowTarget target;
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
            if (target)
            {
                if (target.change_card_visibility)
                {
                    if (target.persist_visiblity_change)
                    {
                        card.visible_only_to_owner = target.visible_only_to_owner;
                    }
                    else
                    {
                        card.SetVisibility(true, !target.visible_only_to_owner);
                    }
                }
                if (target.change_card_pickupable)
                {
                    if (target.persist_pickupable_change)
                    {
                        card.pickupable_only_by_owner = target.pickupable_only_by_owner;
                    }
                    else
                    {
                        card.SetPickupable(true, !target.pickupable_only_by_owner);
                    }
                }
            }
        }

        public override void OnExitState()
        {
            sync.rigid.detectCollisions = true;
            sync.rigid.isKinematic = last_kinematic;
            if (target)
            {
                if (target.change_card_visibility && !target.persist_visiblity_change)
                {
                    card.SetVisibility(true, !card.visible_only_to_owner);
                }
                if (target.change_card_pickupable && !target.persist_pickupable_change)
                {
                    card.SetPickupable(true, !card.pickupable_only_by_owner);
                }
            }
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
        bool _throw_primed = false;
        bool throw_primed
        {
            get => _throw_primed;
            set
            {
                _throw_primed = value;
                if (value)
                {
                    record_length = 0;
                    last_position = sync.rigid.worldCenterOfMass;
                    SendCustomEventDelayedFrames(nameof(RecordTransformLoop), 1);
                }
            }
        }

        int record_length = 0;
        int record_index = 0;
        Vector3 last_position;
        Vector3[] recorded_velocities = new Vector3[64];
        float[] recorded_deltas = new float[64];
        float[] recorded_times = new float[64];
        public void RecordTransformLoop()
        {
            if (!throw_primed)
            {
                return;
            }
            SendCustomEventDelayedFrames(nameof(RecordTransformLoop), 1);
            record_index = (record_index + 1) % recorded_velocities.Length;
            record_length++;
            recorded_velocities[record_index] = (sync.rigid.worldCenterOfMass - last_position) / Time.deltaTime;
            recorded_deltas[record_index] = Time.deltaTime;
            recorded_times[record_index] = Time.timeSinceLevelLoad;
            last_position = sync.rigid.worldCenterOfMass;
        }

        public void ReadRecord(float duration, out Vector3[] velocities, out float[] times)
        {
            var array_length = record_length;
            var index = record_index;
            for (int i = 0; i < Mathf.Min(record_length, recorded_times.Length); i++)
            {
                index = (record_index + recorded_times.Length - i) % recorded_times.Length;
                if (Time.timeSinceLevelLoad - recorded_times[index] >= duration)
                {
                    array_length = i;
                    break;
                }
            }

            if (array_length <= 1)
            {
                //duration was too short. just return the current velocity
                velocities = new Vector3[1];
                times = new float[1];
                velocities[0] = sync.rigid.velocity;
                times[0] = Time.deltaTime;
                return;
            }
            velocities = new Vector3[array_length];
            times = new float[array_length];
            index = record_index - array_length;
            for (int i = 0; i < array_length; i++)
            {
                index++;
                if (index < 0)
                {
                    index += recorded_velocities.Length;
                }
                else if (index >= recorded_velocities.Length)
                {
                    index -= recorded_velocities.Length;
                }
                velocities[i] = recorded_velocities[index];
                times[i] = recorded_deltas[index];
            }

        }

        public Vector3 CalcLinearRegressionOfVelocity()
        {
            //Taken from Mahu's AxeThrowing: https://github.com/mahuvrc/VRCAxeThrowing/blob/d90da07893a9a2006a6a21132cb25646fa78c557/Assets/mahu/axe-throwing/scripts/ThrowingAxe.cs#L600-L637

            Vector3[] y;
            float[] x;
            ReadRecord(0.2f, out y, out x);//read the record of velocities for the last 0.2 seconds
            Debug.LogWarning("Y:");
            foreach (Vector3 v in y)
            {
                Debug.LogWarning("- " + v.ToString());
            }

            Debug.LogWarning("X:");

            foreach (float f in x)
            {
                Debug.LogWarning("- " + f.ToString());
            }
            // this is linear regression via the least squares method. it's loads better
            // than averaging the velocity frame to frame especially at low frame rates
            // and will smooth out noise caused by fluctuations frame to frame and the
            // tendency for players to sharply flick their wrist when throwing

            float sumx = 0;                      /* sum of x     */
            float sumx2 = 0;                     /* sum of x**2  */
            Vector3 sumxy = Vector3.zero;                     /* sum of x * y */
            Vector3 sumy = Vector3.zero;                      /* sum of y     */
            Vector3 sumy2 = Vector3.zero;                     /* sum of y**2  */
            int n = x.Length;

            for (int i = 0; i < n; i++)
            {
                var xi = x[i];
                var yi = y[i];
                sumx += xi;
                sumx2 += xi * xi;
                sumxy += xi * yi;
                sumy += yi;
                sumy2 += Vector3.Scale(yi, yi);
            }

            float denom = n * sumx2 - sumx * sumx;
            if (denom == 0)
            {
                // singular matrix. can't solve the problem.
                return y[n - 1];
            }


            Vector3 m = (n * sumxy - sumx * sumy) / denom;
            Vector3 b = (sumy * sumx2 - sumx * sumxy) / denom;

            Debug.LogWarning("m: " + m);
            Debug.LogWarning("b: " + b);
            Debug.LogWarning("x[n-1]: " + x[n - 1]);
            return m * x[n - 1] + b;
        }

        public override void OnPickup()
        {
            throw_primed = throw_primed || sync.state == card.stateID + SmartObjectSync.STATE_CUSTOM || (sync.IsHeld() && sync.lastState == card.stateID + SmartObjectSync.STATE_CUSTOM);
        }
        public override void OnDrop()
        {
            Debug.LogWarning("vel: " + CalcLinearRegressionOfVelocity());

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
        VRCPlayerApi.TrackingData headData;
        public void OnThrow()
        {
            if (!card.deck.throwing_handler && sync.state != SmartObjectSync.STATE_INTERPOLATING || !sync.IsLocalOwner())
            {
                return;
            }
            if (card.deck.throwing_handler.desktop_throw_assist && !sync.owner.IsUserInVR() && (card.deck.throwing_handler.last_right_click < 0 || card.deck.throwing_handler.last_right_click + 4 > Time.frameCount))
            {
                headData = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                sync.rigid.velocity = headData.rotation * Vector3.forward * card.deck.throwing_handler.desktop_throw_boost;
                temp_target = card.deck.throwing_handler.LocateBestTarget(headData.position, sync.rigid.velocity);
            }
            else
            {
                sync.rigid.velocity = CalcLinearRegressionOfVelocity();
                temp_target = card.deck.throwing_handler.LocateBestTarget(transform.position, sync.rigid.velocity);
            }
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
