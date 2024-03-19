
using UdonSharp;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    public class NoNoSquare : UdonSharpBehaviour
    {
        public short choice_id = 0;
        public void MakeChoice()
        {
            // if (!game.local_player)
            // {
            //     return;
            // }
            // game.local_player.choice = choice_id;
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
