
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerPlacer : CardPlacementSpot
    {
        public bool face_dealer = true;
        Vector3 cached_position;
        Quaternion cached_rotation;
        public override Vector3 GetPlacementPosition(CardPlacingState card)
        {
            return cached_position;
        }
        public override Quaternion GetPlacementRotation(CardPlacingState card){
            return cached_rotation;
        }
        VRCPlayerApi[] players;
        public override void OnPlayerLeft(VRCPlayerApi player){
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
        }
        public override void OnPlayerJoined(VRCPlayerApi player){
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
        }
        float score = -1001;
        float new_score = -1001;
        Vector3 card_pos;
        Vector3 forward;
        Vector3 relative_spot;
        Quaternion relative_rot;
        public override Vector3 GetAimPosition(CardPlacingState card)
        {
            cached_position = placement_transform.position;
            score = -1001;
            card_pos = card.transform.position;
            forward = card.sync.rigid.velocity;
            for(int i = 0; i < players.Length; i++){
                if(!Utilities.IsValid(players[i]) || players[i].isLocal){
                    continue;
                }
                if(face_dealer){
                    relative_rot = Quaternion.FromToRotation(Vector3.forward, Networking.LocalPlayer.GetPosition() - players[i].GetPosition());
                } else {
                    relative_rot = players[i].GetRotation();
                }
                relative_spot = players[i].GetPosition() + relative_rot * placement_transform.localPosition;
                new_score = Vector3.Dot(forward, (relative_spot - card_pos).normalized) / Mathf.Pow(Vector3.Distance(relative_spot, card_pos), 0.3f);
                if(new_score  > score){
                    score = new_score;
                    cached_position = relative_spot;
                    cached_rotation = relative_rot * placement_transform.localRotation;
                }
            }
            return cached_position;
        }
    }
}
