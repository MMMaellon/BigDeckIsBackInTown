
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardsAgainstHumanity : Game
    {
        public int black_card_count = 1;
        public int white_card_count = 10;
        public CardThrowTarget black_card_submission;
        public CardThrowTarget white_card_submission;
        public CardThrowTarget winner_submission;

        public CardThrowTarget black_card_dealer;

        public CardTextBank black_card_bank;
        public CardTextBank white_card_bank;

        public Transform czar_teleport_point;
        public TextMeshPro timer;

        public float choose_black_card_timer = 15;
        public float choose_white_card_timer = 30;
        public float choose_winner_timer = 15;

        public const short STATE_STOPPED = -1001;
        public const short STATE_CZAR_TURN = 0;
        public const short STATE_PLAYER_TURN = 1;
        public const short STATE_PICK_WINNER = 2;

        [System.NonSerialized, UdonSynced]
        bool two_blanks = false;

        public void StartGame()
        {
            if (!local_player_obj)
            {
                //only someone in the game can start the game
                return;
            }
            Networking.SetOwner(local_player_obj.vrc_player, gameObject);
            turn = local_player_obj.id;
        }

        public void ResetGame()
        {
            if (Networking.LocalPlayer.isInstanceOwner || Networking.LocalPlayer.IsOwner(gameObject))
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetGameCallback));
                black_card_submission.deck.ResetDeck();
                white_card_submission.deck.ResetDeck();
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

        DataList submitted_white_cards = new DataList();
        CardThrowing throwing_temp;
        public override void OnChangeState(short old_state, short new_state)
        {
            if (Networking.LocalPlayer.IsOwner(gameObject) && !local_player_obj && state >= 0)
            {
                //owner left and the person who took ownership isn't even playing
                bool found_new_owner = false;
                foreach (Player player in players)
                {
                    if (player && player.player_id >= 0)
                    {
                        player.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "TakeOwnershipOfGame");
                        found_new_owner = true;
                        break;
                    }
                }
                if (!found_new_owner)
                {
                    state = STATE_STOPPED;
                }
            }

            switch (new_state)
            {
                case STATE_CZAR_TURN:
                    {
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            foreach (Card card in white_card_submission.deck.cards)
                            {
                                if (card.IsActiveState())
                                {
                                    continue;
                                }
                                throwing_temp = card.GetComponent<CardThrowing>();
                                if (throwing_temp && throwing_temp.target_id == white_card_submission.id || throwing_temp.target_id == winner_submission.id)
                                {
                                    card.sync.Respawn();
                                }
                            }
                            foreach (Card card in black_card_dealer.deck.cards)
                            {
                                if (card.IsActiveState())
                                {
                                    continue;
                                }
                                throwing_temp = card.GetComponent<CardThrowing>();
                                if (throwing_temp && throwing_temp.target_id == black_card_submission.id)
                                {
                                    card.sync.Respawn();
                                }
                            }

                        }
                        white_card_submission.allow_throwing = false;
                        winner_submission.allow_throwing = false;
                        //wait for callback where we choose the czar
                        break;
                    }
                case STATE_PLAYER_TURN:
                    {
                        CheckValidTurn();
                        black_card_submission.allow_throwing = false;
                        winner_submission.allow_throwing = false;
                        if (local_player_obj)
                        {
                            if (local_player_obj.throw_target)
                            {
                                local_player_obj.throw_target.DealMultiple(white_card_count);
                            }
                            if (local_player_obj != turn_player)
                            {
                                local_player_obj.selected_card = -1001;
                                white_card_submission.allow_throwing = true;
                            }
                            else
                            {
                                white_card_submission.allow_throwing = false;
                            }
                        }
                        break;
                    }
                case STATE_PICK_WINNER:
                    {
                        black_card_submission.allow_throwing = false;
                        white_card_submission.allow_throwing = false;
                        if (turn_player && turn_player.IsLocal())
                        {
                            EnableCardPickups();
                            winner_submission.allow_throwing = true;
                        }
                        else
                        {
                            winner_submission.allow_throwing = false;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        public void EnableCardPickups()
        {
            foreach (Card card in white_card_submission.deck.cards)
            {
                card.sync.EnablePickupable();
            }
        }

        public void CheckValidTurn()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                if (!turn_player)
                {
                    if (turn >= 0)
                    {
                        //something broke, just stop the game
                        ResetGame();
                        return;
                    }
                    else
                    {
                        RandomTurn();
                    }
                }
                else if (turn_player.player_id < 0 || turn_player.selected_card < 0)
                {
                    //player whose turn it was left or didn't submit a card
                    //skip them and pick a new player
                    turn_player.Leave();
                    state = STATE_CZAR_TURN;
                }
            }
        }

        public void RandomTurn()
        {
            Player random_player = RandomPlayer();
            if (random_player)
            {
                turn = random_player.id;
                state = STATE_CZAR_TURN;
            }
            else
            {
                ResetGame();
            }

        }

        public override void OnChangeTurn(short old_turn, short new_turn, Player old_turn_player, Player new_turn_player)
        {
            //if the previous czar did not return to their station, let's teleport them
            if (old_turn_player && old_turn_player.IsLocal())
            {
                if (Vector3.Distance(old_turn_player.vrc_player.GetPosition(), czar_teleport_point.position) < Vector3.Distance(old_turn_player.vrc_player.GetPosition(), old_turn_player.transform.position))
                {
                    old_turn_player.vrc_player.TeleportTo(old_turn_player.transform.position, old_turn_player.transform.rotation);
                }
            }

            if (!new_turn_player)
            {
                //error we couldn't get a valid turn
                if (state >= 0 && Networking.LocalPlayer.IsOwner(gameObject))
                {
                    state = STATE_STOPPED;
                }
                return;
            }

            if (new_turn_player.IsLocal())
            {
                if (czar_teleport_point)
                {
                    new_turn_player.vrc_player.TeleportTo(czar_teleport_point.position, czar_teleport_point.rotation);
                }
                new_turn_player.selected_card = -1001;
                black_card_dealer.DealMultiple(black_card_count);
                black_card_submission.allow_throwing = true;
            }
            else
            {
                black_card_submission.allow_throwing = false;
            }
        }

        public override void OnJoinGame(Player player)
        {
            if (player && player.IsLocal())
            {
                player.throw_target.DealMultiple(white_card_count);
            }
        }

        public override void OnLeftGame(Player player)
        {
            if (player && player.IsLocal())
            {
                foreach (Card card in white_card_submission.deck.cards)
                {
                    card.sync.EnablePickupable();
                    throwing_temp = card.GetComponent<CardThrowing>();
                    if (!card.IsActiveState() && throwing_temp.target_id == player.throw_target.id)
                    {
                        card.sync.Respawn();
                    }
                }
            }
            if (turn_player && turn_player == player)
            {
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    state = STATE_CZAR_TURN;
                }
            }
        }

        CardText card_text;
        public override void OnThrowCard(CardThrowingHandler handler, int target_index, CardThrowing card)
        {
            if (!card)
            {
                return;
            }
            card_text = card.GetComponent<CardText>();
            if (!card_text)
            {
                return;
            }
            if (target_index < 0 || target_index >= handler.targets.Length)
            {
                return;
            }
            if (handler.targets[target_index] == black_card_submission)
            {
                if (turn_player && turn_player.IsLocal())
                {
                    turn_player.selected_card = card_text.text_id;
                }
            }
            else if (handler.targets[target_index] == white_card_submission)
            {
                if (turn_player && turn_player.IsLocal())
                {
                    card.sync.DisablePickupable();
                }
                else if (Networking.LocalPlayer.IsOwner(card.gameObject))
                {
                    submitted_white_cards.Add(card);
                    if (local_player_obj)
                    {
                        local_player_obj.throw_target.Deal1();
                    }
                    if (!two_blanks || submitted_white_cards.Count > 1)
                    {
                        white_card_submission.allow_throwing = false;
                    }
                }
            }
            else if (handler.targets[target_index] == winner_submission)
            {
                if (submitted_white_cards.Contains(card))
                {
                    //One of my cards was submitted as the winner
                    if (local_player_obj)
                    {
                        local_player_obj.score++;
                    }
                }
            }
        }

        public override void OnSelectCard(Player player, int card_id)
        {

        }

        public override void OnScoreChange(Player player, int old_score, int new_score)
        {
            if (Networking.LocalPlayer.IsOwner(gameObject) && state == STATE_PICK_WINNER && old_score < new_score)
            {
                state = STATE_CZAR_TURN;
            }
        }

        int timer_number;
        int last_loop_frame = -1001;
        public void Loop()
        {
            if (last_loop_frame >= Time.frameCount)
            {
                return;
            }

            if (state >= 0)
            {
                SendCustomEventDelayedFrames(nameof(Loop), 1);
            }
            else
            {
                return;
            }

            switch (state)
            {
                case STATE_CZAR_TURN:
                    {
                        if (timer)
                        {
                            timer_number = Mathf.CeilToInt(last_state_change + choose_white_card_timer - Time.timeSinceLevelLoad);
                            timer.text = timer_number + "s";
                            if (timer_number <= 0 && Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                state = STATE_PLAYER_TURN;
                            }
                        }
                        break;
                    }
                case STATE_PLAYER_TURN:
                    {
                        if (timer)
                        {
                            timer_number = Mathf.CeilToInt(last_state_change + choose_black_card_timer - Time.timeSinceLevelLoad);
                            timer.text = timer_number + "s";
                            if (timer_number <= 0 && Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                state = STATE_PICK_WINNER;
                            }
                        }
                        break;
                    }
                case STATE_PICK_WINNER:
                    {
                        if (timer)
                        {
                            timer_number = Mathf.CeilToInt(last_state_change + choose_winner_timer - Time.timeSinceLevelLoad);
                            timer.text = timer_number + "s";
                            if (timer_number <= 0 && Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                state = STATE_CZAR_TURN;
                            }
                        }
                        break;
                    }
                default:
                    {
                        if (timer)
                        {
                            timer_number = 69;
                            timer.text = "";
                        }
                        break;
                    }
            }
        }

        //Animation Callbacks
        public void SelectCzar()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                turn = NextPlayerId(1);
            }
        }
    }
}
