
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class YesNoPlayer : UdonSharpBehaviour
    {
        public YesNo game;
        [System.NonSerialized]
        public short id = -1;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(player_id))]
        public short _player_id = -1001;
        public short player_id
        {
            get => _player_id;
            set
            {
                _player_id = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                else if (Networking.LocalPlayer.playerId == value)
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                } else if (value < 0 && Networking.LocalPlayer.IsOwner(game.gameObject)){
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                    //return to game master
                }
                if(value < 0){
                    game.OnLeaveGame(this);
                } else {
                    game.OnJoinGame(this);
                }
            }
        }
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(health))]
        public short _health = -1001;
        public short health
        {
            get => _health;
            set
            {
                _health = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                    if(Networking.LocalPlayer.playerId == _player_id){

                    }
                }

            }
        }
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(choice))]
        public short _choice = -1001;
        public short choice
        {
            get => _choice;
            set
            {
                _choice = value;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                    if(Networking.LocalPlayer.playerId == _player_id){

                    }
                }
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player){
            if(!Utilities.IsValid(player) || !player.isLocal){
                return;
            }
            if(player_id == player.playerId){
                //Correct
            } else if(player.IsOwner(game.gameObject)){
                //game master is about to do something
            } else {
                //uh oh
                player_id = -1001;
            }
        }

        public void ResetPlayer(){
            health = 5;
            choice = 0;
        }
    }
}
