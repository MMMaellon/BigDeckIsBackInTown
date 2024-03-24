
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardsAgainstHumanity : Game
    {
        public string blanks_variable = "____";
        public int black_card_count = 1;
        public int white_card_count = 10;

        public CardThrowTarget black_card_submission;
        public CardThrowTarget magnifying_submission1;
        public CardThrowTarget magnifying_submission2;
        public CardThrowTarget white_card_submission1;
        public CardThrowTarget white_card_submission2;

        public CardThrowTarget black_card_dealer;

        public Transform czar_teleport_point;

        public CardTextBank black_card_bank;
        public CardTextBank white_card_bank;

        CardText magnified_1;
        CardText magnified_2;

        public Transform display_parent;
        public TextMeshPro selected_black_card_text;
        public TextMeshPro magnified_white_card_text1;
        public TextMeshPro magnified_white_card_text2;
        public TextMeshPro winner_name;
        public TextMeshPro czar_name;
        public TextMeshPro timer;

        public float choose_black_card_timer = 30;
        public float choose_white_card_timer = 30;
        public float choose_winner_timer = 120;

        public const short STATE_STOPPED = -1001;
        public const short STATE_CZAR_TURN = 0;
        public const short STATE_PLAYER_TURN = 1;
        public const short STATE_PICK_WINNER = 2;
        public const short STATE_WINNER = 3;

        [System.NonSerialized, FieldChangeCallback(nameof(two_blanks))]
        bool _two_blanks = false;
        bool two_blanks
        {
            get => _two_blanks;
            set
            {
                _two_blanks = value;
                animator.SetBool("two_blanks", value);
            }
        }

        public void StartGame()
        {
            if (!local_player_obj)
            {

                //only someone in the game can start the game
                return;
            }

            Networking.SetOwner(local_player_obj.vrc_player, gameObject);
            state = STATE_CZAR_TURN;
        }

        public void ResetGame()
        {
            if (Networking.LocalPlayer.isInstanceOwner || local_player_obj)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetGameCallback));
                black_card_submission.deck.ResetDeck();
                white_card_submission1.deck.ResetDeck();
                white_card_submission2.deck.ResetDeck();
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
            if (new_state >= 0)
            {
                SendCustomEventDelayedFrames(nameof(Loop), 1);
            }
            if (Networking.LocalPlayer.IsOwner(gameObject) && !local_player_obj && new_state >= 0)
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
                    Debug.LogWarning("could not find new owner stopping game");
                    state = STATE_STOPPED;
                }
            }

            switch (new_state)
            {
                case STATE_CZAR_TURN:
                    {
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            foreach (Card card in white_card_submission1.deck.cards)
                            {
                                if (card.IsActiveState())
                                {
                                    continue;
                                }
                                throwing_temp = card.GetComponent<CardThrowing>();
                                if (throwing_temp && (throwing_temp.target_id == white_card_submission1.id || throwing_temp.target_id == white_card_submission2.id || throwing_temp.target_id == magnifying_submission1.id || throwing_temp.target_id == magnifying_submission2.id))
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
                            two_blanks = false;
                        }
                        white_card_submission1.allow_throwing = false;
                        white_card_submission2.allow_throwing = false;
                        magnifying_submission1.allow_throwing = false;
                        magnifying_submission2.allow_throwing = false;
                        magnified_white_card_text1.text = "";
                        magnified_white_card_text2.text = "";
                        selected_black_card_text.text = "";
                        magnified_1 = null;
                        magnified_2 = null;
                        //wait for callback where we choose the czar
                        break;
                    }
                case STATE_PLAYER_TURN:
                    {
                        black_card_submission.allow_throwing = false;
                        magnifying_submission1.allow_throwing = false;
                        magnifying_submission2.allow_throwing = false;
                        if (local_player_obj)
                        {
                            if (!cards_dealt && local_player_obj.throw_target && turn_player && !turn_player.IsLocal())
                            {
                                cards_dealt = true;
                                local_player_obj.throw_target.DealMultiple(white_card_count);
                            }
                            if (local_player_obj != turn_player)
                            {
                                local_player_obj.selected_card = -1001;
                                white_card_submission1.allow_throwing = true;
                                white_card_submission2.allow_throwing = two_blanks;
                            }
                            else
                            {
                                white_card_submission1.allow_throwing = false;
                                white_card_submission2.allow_throwing = false;
                            }
                        }
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            foreach (Card card in black_card_dealer.deck.cards)
                            {
                                if (card.IsActiveState())
                                {
                                    continue;
                                }
                                throwing_temp = card.GetComponent<CardThrowing>();
                                if (throwing_temp && throwing_temp.target_id != black_card_submission.id)
                                {
                                    card.sync.Respawn();
                                }
                            }
                        }
                        break;
                    }
                case STATE_PICK_WINNER:
                    {
                        black_card_submission.allow_throwing = false;
                        white_card_submission1.allow_throwing = false;
                        white_card_submission2.allow_throwing = false;
                        if (turn_player && turn_player.IsLocal())
                        {
                            EnableCardPickups();
                            magnifying_submission1.allow_throwing = true;
                            magnifying_submission2.allow_throwing = true;
                        }
                        else
                        {
                            magnifying_submission1.allow_throwing = false;
                            magnifying_submission2.allow_throwing = false;
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
            foreach (Card card in white_card_submission1.deck.cards)
            {
                card.sync.EnablePickupable();
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
            if (czar_teleport_point && old_turn_player && old_turn_player.IsLocal())
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

                czar_name.text = "It's your turn <b><color=red>" + new_turn_player.vrc_player.displayName;

                new_turn_player.selected_card = -1001;
                VRCPlayerApi.TrackingData head = new_turn_player.vrc_player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                Networking.SetOwner(new_turn_player.vrc_player, black_card_dealer.gameObject);
                black_card_dealer.transform.position = head.position;
                Vector3 dealer_vector = black_card_dealer.deck.transform.position - head.position;
                dealer_vector.y = 0;
                black_card_dealer.transform.rotation = Quaternion.LookRotation(dealer_vector);
                black_card_dealer.DealMultiple(black_card_count);
                black_card_submission.allow_throwing = true;
            }
            else
            {
                if (Utilities.IsValid(new_turn_player.vrc_player))
                {
                    czar_name.text = "Czar: <b>" + new_turn_player.vrc_player.displayName;
                }
                else
                {
                    czar_name.text = "ERROR: Could not find the czar???";
                }

                black_card_submission.allow_throwing = false;
            }
        }

        bool cards_dealt = false;
        public override void OnJoinGame(Player player)
        {
            if (player && player.IsLocal() && state >= 0)
            {
                cards_dealt = true;
                Networking.SetOwner(player.vrc_player, player.throw_target.gameObject);
                player.throw_target.DealMultiple(white_card_count);
            }
        }

        public override void OnLeftGame(Player player)
        {
            if (player && player.IsLocal())
            {
                cards_dealt = false;
                foreach (Card card in white_card_submission1.deck.cards)
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
                card.sync.pickupable = false;
                if (card_text.text_id >= 0 && card_text.text_id < black_card_bank.texts.Length)
                {
                    selected_black_card_text.text = black_card_bank.texts[card_text.text_id];
                    Debug.LogWarning("Two Blanks : " + selected_black_card_text.text.IndexOf(blanks_variable) + " == " + selected_black_card_text.text.LastIndexOf(blanks_variable));
                    two_blanks = selected_black_card_text.text.IndexOf(blanks_variable) != selected_black_card_text.text.LastIndexOf(blanks_variable);
                }
                else
                {
                    selected_black_card_text.text = "Error could not find text";
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    state = STATE_PLAYER_TURN;
                }
            }
            else if (handler.targets[target_index] == white_card_submission1)
            {
                card.sync.pickupable = false;
                if (Networking.LocalPlayer.IsOwner(card.gameObject))
                {
                    submitted_white_cards.Add(card);
                    if (local_player_obj)
                    {
                        local_player_obj.throw_target.Deal1();
                    }
                    white_card_submission1.allow_throwing = false;
                    if (two_blanks)
                    {
                        white_card_submission2.allow_throwing = true;
                    }
                }
            }
            else if (handler.targets[target_index] == white_card_submission2)
            {
                card.sync.pickupable = false;
                if (Networking.LocalPlayer.IsOwner(card.gameObject))
                {
                    submitted_white_cards.Add(card);
                    if (local_player_obj)
                    {
                        local_player_obj.throw_target.Deal1();
                    }
                    white_card_submission2.allow_throwing = false;
                }
            }
            else if (handler.targets[target_index] == magnifying_submission1)
            {
                card.sync.pickupable = true;
                if (card_text.text_id >= 0 && card_text.text_id < white_card_bank.texts.Length)
                {
                    magnified_1 = card_text;
                    magnified_white_card_text1.text = white_card_bank.texts[card_text.text_id];
                }
            }
            else if (handler.targets[target_index] == magnifying_submission2)
            {
                card.sync.pickupable = true;
                if (card_text.text_id >= 0 && card_text.text_id < white_card_bank.texts.Length)
                {
                    magnified_2 = card_text;
                    magnified_white_card_text2.text = white_card_bank.texts[card_text.text_id];
                }
            }
            else
            {
                card.sync.pickupable = true;
            }
        }

        public override void OnSelectCard(Player player, int card_id)
        {

        }

        public override void OnScoreChange(Player player, int old_score, int new_score)
        {
            if (player && Utilities.IsValid(player.vrc_player))
            {
                winner_name.text = player.vrc_player.displayName;
            }
            else
            {
                Debug.LogError("Could not get winner name :(");
                winner_name.text = "Could not get winner name :(";
            }
        }

        int timer_number;
        int last_loop_frame = -1001;
        Vector3 rotational_diff = Vector3.zero;
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
                            if (timer_number <= 0 && turn_player && turn_player.IsLocal())
                            {
                                //timer ran out before prompt was submitted
                                turn_player.Leave();
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(NextTurn));
                            }
                            else if (timer_number <= -2 && Networking.LocalPlayer.IsOwner(gameObject))//give them like 2 seconds of leeway
                            {
                                NextTurn();
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

            if (!Utilities.IsValid(Networking.LocalPlayer))
            {
                return;
            }
            rotational_diff = Networking.LocalPlayer.GetPosition() - display_parent.position;
            rotational_diff.y = 0;
            display_parent.rotation = Quaternion.LookRotation(rotational_diff);
        }

        public void SubmitWinner()
        {
            if (turn_player && turn_player.IsLocal() && turn_player.selected_card >= 0 && magnified_1)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CheckIfIWon));
            }
        }

        public void CheckIfIWon()
        {
            if (!local_player_obj)
            {
                return;
            }

            if (magnified_1 && magnified_2)
            {
                if (submitted_white_cards.Contains(magnified_1) && submitted_white_cards.Contains(magnified_2))
                {
                    local_player_obj.score++;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(WinCallback));
                }
            }
        }

        public void WinCallback()
        {
            state = STATE_WINNER;
        }

        public void ResetTimerCallback()
        {
            last_state_change = Time.timeSinceLevelLoad;
        }

        //Animation Callbacks
        public void SelectCzar()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
            {
                return;
            }
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                var next_turn = NextPlayerId(1);
                if (next_turn == turn)
                {
                    state = STATE_STOPPED;
                    return;
                }
                turn = next_turn;
            }
        }

        public void NextTurn()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                state = STATE_CZAR_TURN;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetTimerCallback));
            }
        }
    }
}
