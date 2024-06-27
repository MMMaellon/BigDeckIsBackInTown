
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WouldYouRather : Game
    {
        public CardTextBank word_bank;

        public Deck blue_deck;
        public Deck red_deck;

        const short STATE_STOPPED = -1001;
        const short STATE_SELECT_NEXT_TURN = 0;
        const short STATE_SELECT_PROMPT = 1;
        const short STATE_SELECT_ANSWERS = 2;
        const short STATE_SHOW_ANSWERS = 3;

        public void StartGame()
        {
            if (!local_player_obj)
            {
                return;
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            state = STATE_SELECT_NEXT_TURN;
        }

        public void ResetGame()
        {
            if (Networking.LocalPlayer.isInstanceOwner || local_player_obj)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetGameCallback));
                blue_deck.ResetDeck();
                red_deck.ResetDeck();
            }
        }

        public void ResetGameCallback()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                state = STATE_STOPPED;
                turn = -1001;
            }
            if (local_player_obj)
            {
                local_player_obj.ResetPlayer();
                local_player_obj.Leave();
            }
        }

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(short old_state, short new_state)
        {

        }

        public override void OnChangeState(LightSync.LightSync sync, int oldState, int newState)
        {

        }

        public override void OnChangeTurn(short old_turn, short new_turn, Player old_turn_player, Player new_turn_player)
        {

        }

        public override void OnJoinGame(Player player)
        {

        }

        public override void OnLeftGame(Player player)
        {

        }

        public override void OnScoreChange(Player player, int old_score, int new_score)
        {

        }

        public override void OnSelectCard(Player player, int card_id)
        {

        }

        public override void OnThrowCard(CardThrowingHandler handler, int target_index, CardThrowing card)
        {

        }
    }
}
