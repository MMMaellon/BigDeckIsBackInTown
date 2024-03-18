using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Player : UdonSharpBehaviour
    {
        public Game game;

        [HideInInspector]
        public int id;

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(player_id))]
        public int _player_id = -1001;
        public int player_id
        {
            get => _player_id;
            set
            {
                _player_id = value;
                if (!Utilities.IsValid(local_player))
                {
                    return;
                }
                if (local_player.playerId == -1 - value && !local_player.IsOwner(gameObject))
                {
                    //world owner is asking us to take ownership of this object
                    Networking.SetOwner(local_player, gameObject);
                    game.local_player_obj = this;
                }

                if (value >= 0)
                {
                    vrc_player = VRCPlayerApi.GetPlayerById(value);
                    if (!game.joined_player_ids.Contains(id))
                    {
                        game.joined_player_ids.Add(id);
                    }
                }
                else
                {
                    vrc_player = VRCPlayerApi.GetPlayerById(-1 - value);
                    if (game.local_player_obj == this)
                    {
                        game.local_player_obj = null;
                    }
                    game.joined_player_ids.RemoveAll(id);
                }
            }
        }

        VRCPlayerApi local_player;
        [System.NonSerialized]
        public VRCPlayerApi vrc_player;
        [System.NonSerialized]
        public float last_turn = -1001;
        public void OnEnable()
        {
            local_player = Networking.LocalPlayer;
            player_id = player_id;
        }

        public virtual void ResetPlayer()
        {

        }

    }
}
