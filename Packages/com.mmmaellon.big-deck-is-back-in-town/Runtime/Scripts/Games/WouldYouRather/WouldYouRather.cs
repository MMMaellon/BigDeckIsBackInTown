
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WouldYouRather : GameSubmissionListener
    {
        public GameSubmissionSpot left_prompt;
        public GameSubmissionSpot right_prompt;
        public GameSubmissionSpot left_answer;
        public GameSubmissionSpot right_answer;
        VRCPlayerApi.TrackingData head_data;

        public TextMeshPro left_prompt_text;
        public TextMeshPro right_prompt_text;

        Vector3 forward_dir;
        public void LateUpdate()
        {
            forward_dir = transform.position - Networking.LocalPlayer.GetPosition();
            forward_dir.y = 0;
            transform.rotation = Quaternion.LookRotation(forward_dir);
        }

        public override void OnSubmit(CardPlacingState card, GameSubmissionSpot spot)
        {
            SendCustomEventDelayedSeconds(nameof(UpdateTextLoop), 0.25f);
        }

        public int type_delay = 3;
        public void UpdateTextLoop()
        {
            if (left_prompt_text.text.Length == 0 || left_prompt.submitted_text_str.StartsWith(left_prompt_text.text))
            {
                if (left_prompt_text.text.Length < left_prompt.submitted_text_str.Length)
                {
                    left_prompt_text.text = left_prompt.submitted_text_str.Substring(0, left_prompt_text.text.Length + 1);
                    SendCustomEventDelayedFrames(nameof(UpdateTextLoop), type_delay);
                }
            }
            else
            {
                left_prompt_text.text = left_prompt_text.text.Substring(0, left_prompt_text.text.Length - 1);
                SendCustomEventDelayedFrames(nameof(UpdateTextLoop), 1);
            }
            if (right_prompt_text.text.Length == 0 || right_prompt.submitted_text_str.StartsWith(right_prompt_text.text))
            {
                if (right_prompt_text.text.Length < right_prompt.submitted_text_str.Length)
                {
                    right_prompt_text.text = right_prompt.submitted_text_str.Substring(0, right_prompt_text.text.Length + 1);
                    SendCustomEventDelayedFrames(nameof(UpdateTextLoop), type_delay);
                }
            }
            else
            {
                right_prompt_text.text = right_prompt_text.text.Substring(0, right_prompt_text.text.Length - 1);
                SendCustomEventDelayedFrames(nameof(UpdateTextLoop), 1);
            }
        }
    }
}
