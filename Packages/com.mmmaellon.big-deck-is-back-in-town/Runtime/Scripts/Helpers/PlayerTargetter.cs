
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerTargetter : CardThrowTarget
    {
        [System.NonSerialized, UdonSynced]
        public short target_id;
        [System.NonSerialized, UdonSynced]
        public short card_id;

        public override void OnDeserialization(VRC.Udon.Common.DeserializationResult result)
        {
            if (target_id == Networking.LocalPlayer.playerId && card_id >= 0 && card_id < deck.cards.Length)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                target_id = -1001;
                Networking.SetOwner(Networking.LocalPlayer, deck.cards[card_id].gameObject);
            }
        }
        public bool face_dealer = true;
        Vector3 cached_position;
        Quaternion cached_rotation;
        public override Vector3 GetTargetPosition(int index, int capacity)
        {
            return cached_position;
        }
        public override Quaternion GetTargetRotation(int index, int capacity)
        {
            return cached_rotation;
        }
        VRCPlayerApi[] players;
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
        }
        float best_score = -1001;
        float new_score = -1001;
        Vector3 relative_spot;
        Quaternion relative_rot;
        int player_id_cache = -1001;
        public override Vector3 GetAimPosition(Vector3 card_pos, Vector3 forward)
        {
            cached_position = target_center.position;
            best_score = -1001;
            for (int i = 0; i < players.Length; i++)
            {
                if (!Utilities.IsValid(players[i]) || players[i].isLocal)
                {
                    continue;
                }
                if (face_dealer)
                {
                    relative_rot = Quaternion.FromToRotation(Vector3.forward, Networking.LocalPlayer.GetPosition() - players[i].GetPosition());
                }
                else
                {
                    relative_rot = players[i].GetRotation();
                }
                relative_spot = players[i].GetPosition() + relative_rot * target_center.localPosition;
                new_score = Vector3.Dot(forward, (relative_spot - card_pos).normalized) / Mathf.Pow(Vector3.Distance(relative_spot, card_pos), 0.3f);
                if (new_score > best_score)
                {
                    best_score = new_score;
                    cached_position = relative_spot;
                    cached_rotation = relative_rot * target_center.localRotation;
                    player_id_cache = players[i].playerId;
                }
            }
            Debug.LogWarning("Returning " + cached_position);
            return cached_position;
        }

        public override void DealCard(CardThrowing card)
        {
            if (change_card_visibility)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                target_id = (short)player_id_cache;
                card_id = (short)card.card.id;
                RequestSerialization();
            }
            base.DealCard(card);
        }
    }
}
