
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class NoNoSquare : UdonSharpBehaviour
    {
        public short choice_id = 0;
        public YesNo game;
        public void MakeChoice()
        {
            if (!game.local_player)
            {
                return;
            }
            game.local_player.choice = choice_id;
        }

        public override void OnPlayerTriggerStay(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal)
            {
                return;
            }
            MakeChoice();
        }
    }
}
