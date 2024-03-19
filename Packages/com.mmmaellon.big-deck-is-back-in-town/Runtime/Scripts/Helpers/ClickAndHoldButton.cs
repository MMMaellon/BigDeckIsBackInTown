
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.Udon.Common;

namespace MMMaellon.BigDeckIsBackInTown
{
    public class ClickAndHoldButton : UdonSharpBehaviour
    {
        public UdonBehaviour udon;
        public string udon_event;
        VRCPickup pickup;
        void Start()
        {
            DisableInteractive = !use_vrc_click;
            pickup = GetComponent<VRCPickup>();
            if (progress_slider != null)
            {
                progress_slider.gameObject.SetActive(false);
                progress_slider.value = 0;
            }
        }
        public void OnSuccessfulHold()
        {
            udon.SendCustomEvent(udon_event);
        }
        public float hold_duration = 2f;
        public Slider progress_slider;
        bool holding_left = false;
        bool holding_right = false;
        public bool use_vrc_click = false;
        public override void Interact()
        {
            if (use_vrc_click)
            {
                StartHold();
            }
        }
        public override void OnPickupUseDown()
        {
            if (!use_vrc_click)
            {
                return;
            }
            StartHold();
            if (pickup.currentHand == VRCPickup.PickupHand.Left)
            {
                holding_left = true;
                holding_right = false;
            }
            else if (pickup.currentHand == VRCPickup.PickupHand.Right)
            {

                holding_left = false;
                holding_right = true;
            }
        }
        public override void OnPickupUseUp()
        {
            AbortHold();
        }
        float hold_start = -1001f;
        public void StartHold()
        {
            if (hold_start >= 0)
            {
                return;
            }
            holding_left = true;
            holding_right = true;
            if (hold_duration == 0)
            {
                OnSuccessfulHold();
                return;
            }
            hold_start = Time.timeSinceLevelLoad;
            SendCustomEventDelayedFrames(nameof(HoldLoop), 2, VRC.Udon.Common.Enums.EventTiming.Update);//2 frames to account for vrchat weirdness
        }
        public void HoldLoop()
        {
            if (hold_start < 0)
            {
                return;
            }
            holding_left = holding_left && left_trigger;
            holding_right = holding_right && right_trigger;
            if (hold_start + hold_duration < Time.timeSinceLevelLoad)
            {
                AbortHold();
                OnSuccessfulHold();
            }
            else if (!holding_right && !holding_left)
            {
                AbortHold();
            }
            else
            {
                SendCustomEventDelayedFrames(nameof(HoldLoop), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                if (progress_slider != null)
                {
                    progress_slider.gameObject.SetActive(true);
                    progress_slider.value = (Time.timeSinceLevelLoad - hold_start) / hold_duration;
                }
            }
        }
        public void AbortHold()
        {
            hold_start = -1001f;
            if (progress_slider)
            {
                progress_slider.gameObject.SetActive(false);
                progress_slider.value = 0;
            }
        }
        bool left_trigger;
        bool right_trigger;
        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (args.handType == HandType.LEFT)
            {
                left_trigger = value;
            }

            if (args.handType == HandType.RIGHT)
            {
                right_trigger = value;
            }
        }

    }
}
