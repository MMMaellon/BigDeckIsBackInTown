
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GameSubmissionSpot : CardPlacementSpot
    {
        public CardTextBank bank;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(submitted_text))]
        public int _submitted_text = -1001;
        public int submitted_text
        {
            get => _submitted_text;
            set
            {
                _submitted_text = value;
                if (value < 0)
                {
                    submitted_text_str = "";
                }
                else if (value < bank.texts.Length)
                {
                    submitted_text_str = bank.ReplaceStringVariables(bank.texts[value], Networking.GetOwner(gameObject), submitted_player);
                }
                else
                {
                    submitted_text_str = "<color=red>ERROR PREFAB BROKE</color>";
                }
                if (animator)
                {
                    animator.SetTrigger(place_parameter);
                }
                foreach (GameSubmissionListener listener in placement_listeners)
                {
                    listener.OnTextSubmitted(submitted_text_str, this);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        VRCPlayerApi submitted_player;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(submitted_player_id))]
        public int _submitted_player_id;
        public int submitted_player_id
        {
            get => _submitted_player_id;
            set
            {
                _submitted_player_id = value;
                submitted_player = VRCPlayerApi.GetPlayerById(value);
                if (value < 0)
                {
                    submitted_text_str = "";
                }
                else if (value < bank.texts.Length)
                {
                    submitted_text_str = bank.ReplaceStringVariables(bank.texts[value], Networking.GetOwner(gameObject), submitted_player);
                }
                else
                {
                    submitted_text_str = "<color=red>ERROR PREFAB BROKE</color>";
                }
            }
        }

        public string submitted_text_str;
        public GameSubmissionListener[] placement_listeners;
        public CardText card_text;
        DataList cards_to_respawn = new DataList();
        public override void Place(CardPlacingState card)
        {
            base.Place(card);
            cards_to_respawn.Add(card.sync);
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            card_text = card.GetComponent<CardText>();
            if (!card_text)
            {
                return;
            }
            submitted_player_id = card_text.player_id;
            submitted_text = card_text.text_id;
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(RespawnPlaced), 1f);
            foreach (GameSubmissionListener listener in placement_listeners)
            {
                listener.OnSubmit(card, this);
            }
        }

        public void RespawnPlaced()
        {
            foreach (DataToken sync_token in cards_to_respawn.ToArray())
            {
                ((SmartObjectSync)sync_token.Reference).Respawn();
            }
        }

        public float angle_limit = 10f;
        public override Vector3 GetAimPosition(CardPlacingState card)
        {
            if (aimed)
            {
                return card.transform.position;
            }
            return base.GetAimPosition(card);
        }

        public string look_parameter = "look";
        public string place_parameter = "place";
        VRCPlayerApi.TrackingData head_data;
        public float update_delay = 0.25f;
        int last_update_frame;

        [System.NonSerialized]
        public bool aimed = false;
        public void LookUpdate()
        {
            if (!allow_throwing)
            {
                return;
            }
            head_data = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.rotation = Quaternion.LookRotation(transform.position - head_data.position);
            aimed = Vector3.Angle(head_data.rotation * Vector3.forward, placement_transform.position - head_data.position) < angle_limit;
            if (animator)
            {
                animator.SetBool(look_parameter, aimed);
                animator.SetBool(active_parameter, allow_throwing);
            }
            SendCustomEventDelayedFrames(nameof(LookUpdate), 1);
        }
        public override bool allow_throwing
        {
            get => _allow_throwing;
            set
            {
                _allow_throwing = value;
                if (animator)
                {
                    animator.SetBool(active_parameter, value);
                }
                if (value)
                {
                    SendCustomEventDelayedFrames(nameof(LookUpdate), 1);
                }
            }
        }
    }
}
