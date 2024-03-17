
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class YesNo : UdonSharpBehaviour
    {
        public Transform prompt_picker_teleport;
        public Transform jail_teleport;
        public Transform play_area_teleport;
        public YesNoPlayer[] players;
        public Animator animator;
        public DataList active_players = new DataList();
        [System.NonSerialized]
        public YesNoPlayer turn_player;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(turn))]
        public short _turn;
        public short turn
        {
            get => _turn;
            set
            {
                _turn = value;
                last_state_time = Time.timeSinceLevelLoad;
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                if (local_player && turn == local_player.player_id)
                {
                    animator.SetBool("your_turn", true);
                    SendCustomEventDelayedFrames(nameof(TeleportToStage), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                    local_player.choice = 0;
                    prompt_submission.allow_throwing = true;
                    card_dealer.Deal8();
                }
                else
                {
                    prompt_submission.allow_throwing = false;
                    animator.SetBool("your_turn", false);
                }
                foreach (YesNoPlayer player in players)
                {
                    if (value == player.player_id)
                    {
                        turn_player = player;
                        return;
                    }
                }
                turn_player = null;
            }
        }
        public void TeleportToStage()
        {
            Networking.LocalPlayer.TeleportTo(prompt_picker_teleport.position, prompt_picker_teleport.rotation);
        }
        public void TeleportToPlayArea()
        {
            Networking.LocalPlayer.TeleportTo(play_area_teleport.position, play_area_teleport.rotation);
        }
        public void TeleportToJail()
        {
            Networking.LocalPlayer.TeleportTo(jail_teleport.position, jail_teleport.rotation);
        }
        public PlayerSpot card_dealer;
        public GameSubmissionSpot prompt_submission;
        float last_state_time;
        public float prompt_picking_duration = 30f;
        public float answer_picking_duration = 10f;
        public float show_results_duration = 3f;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(game_state))]
        public short _game_state = STATE_STOPPED;
        public short game_state
        {
            get => _game_state;
            set
            {
                Debug.LogWarning("GAMESTATE: " + value);
                last_state_time = Time.timeSinceLevelLoad;
                _game_state = value;
                animator.SetInteger("game", value);
                switch (value)
                {
                    case STATE_SELECTING_PROMPT:
                        {
                            owner_text.text = "GAME MASTER: " + Networking.GetOwner(gameObject).displayName;
                            prompt_text.text = "";
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                Networking.SetOwner(Networking.LocalPlayer, prompt_submission.gameObject);
                                prompt_submission.submitted_text = -1001;
                            }
                            prompt_preview_text.text = "";
                            prompt_submission.submitted_text_str = "";
                            if (Networking.LocalPlayer.IsOwner(card_dealer.deck.gameObject))
                            {
                                card_dealer.deck.ResetDeck();
                            }
                            break;
                        }
                    case STATE_SELECTING_ANSWER:
                        {
                            prompt_preview_text.text = "";
                            prompt_submission.allow_throwing = false;
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            if (Networking.LocalPlayer.IsOwner(card_dealer.deck.gameObject))
                            {
                                card_dealer.deck.ResetDeck();
                            }
                            break;
                        }
                    case STATE_RESULTS:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            if (local_player && local_player.health > 0 && turn_player && local_player.choice != turn_player.choice)
                            {
                                //DIE
                                local_player.health = 0;
                            }
                            break;
                        }
                    case STATE_WINNER:
                        {
                            break;
                        }
                    case STATE_ERROR:
                        {
                            last_loop = Time.frameCount + 3;
                            break;
                        }
                    default:
                        {
                            last_loop = Time.frameCount + 3;
                            break;
                        }
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public const short STATE_STOPPED = -1001;
        public const short STATE_SELECTING_PROMPT = 0;
        public const short STATE_SELECTING_ANSWER = 1;
        public const short STATE_RESULTS = 2;
        public const short STATE_WINNER = 3;
        public const short STATE_ERROR = 4;

        public void StartGame()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            game_state = STATE_SELECTING_PROMPT;
        }

        public void StopGame()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                return;
            }
            game_state = STATE_STOPPED;
        }

        public void ToggleStart()
        {
            if (game_state < 0)
            {
                StartGame();
            }
            else
            {
                StopGame();
            }
        }

        //These are going to be controlled by animations
        int living_count = 0;
        public void OnPromptState()
        {
            Debug.LogWarning("OnPromptState");
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                return;
            }
            living_count = 0;
            foreach (YesNoPlayer player in players)
            {
                if (player.player_id >= 0 && player.health >= 0)
                {
                    living_count += 1;
                }
            }
            if (living_count < 2)
            {
                game_state = STATE_WINNER;
                return;
            }
            PickNextPicker();
        }

        public void OnAnswerState()
        {
            //when there's only like one second left on the timer
        }

        public void OnResultsState()
        {
            if (local_player && local_player.health == 0)
            {
                SendCustomEventDelayedFrames(nameof(TeleportToJail), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                local_player.health = -1;//so we don't double teleport ppl
            }
            if (local_player && local_player == turn_player)
            {
                if (local_player.health > 0)
                {
                    SendCustomEventDelayedFrames(nameof(TeleportToPlayArea), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                }
                else
                {
                    SendCustomEventDelayedFrames(nameof(TeleportToJail), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                }
            }
        }

        public void OnWinnerState()
        {

        }

        int last_loop = -1001;
        public TextMeshPro timer_text;
        public TextMeshPro prompt_text;
        public TextMeshPro prompt_preview_text;
        public TextMeshPro playerlist_text;
        public TextMeshPro owner_text;
        Vector3 prompt_dist;
        public void Loop()
        {
            if (last_loop >= Time.frameCount)
            {
                return;
            }
            last_loop = Time.frameCount;
            SendCustomEventDelayedFrames(nameof(Loop), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            prompt_dist = prompt_text.gameObject.transform.position - Networking.LocalPlayer.GetPosition();
            prompt_dist.y = 0;
            prompt_text.gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, prompt_dist);
            switch (game_state)
            {
                case STATE_SELECTING_PROMPT:
                    {
                        if (local_player && local_player.health == 0)
                        {
                            local_player.health = 1;
                        }
                        if (prompt_preview_text.text.Length < prompt_submission.submitted_text_str.Length)
                        {
                            prompt_preview_text.text = prompt_submission.submitted_text_str.Substring(0, prompt_preview_text.text.Length + 1);
                        }
                        if (Time.timeSinceLevelLoad - last_state_time > prompt_picking_duration)
                        {
                            if (turn_player && turn_player == local_player)
                            {
                                turn_player.health = -1;
                                SendCustomEventDelayedFrames(nameof(TeleportToJail), 1, VRC.Udon.Common.Enums.EventTiming.Update);
                            }
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                PickNextPicker();
                            }
                        }
                        else if (prompt_submission.submitted_text >= 0 && turn_player && turn_player.choice != 0)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_SELECTING_ANSWER;
                            }
                        }
                        timer_text.text = Mathf.CeilToInt(last_state_time + prompt_picking_duration - Time.timeSinceLevelLoad) + "s";
                        break;
                    }
                case STATE_SELECTING_ANSWER:
                    {
                        if (prompt_text.text.Length < prompt_submission.submitted_text_str.Length)
                        {
                            prompt_text.text = prompt_submission.submitted_text_str.Substring(0, prompt_text.text.Length + 1);
                        }
                        if (Time.timeSinceLevelLoad - last_state_time > answer_picking_duration)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_RESULTS;
                            }
                        }
                        timer_text.text = Mathf.CeilToInt(last_state_time + answer_picking_duration - Time.timeSinceLevelLoad) + "s";
                        break;
                    }
                case STATE_RESULTS:
                    {
                        if (Time.timeSinceLevelLoad - last_state_time > show_results_duration)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_SELECTING_PROMPT;
                            }
                        }
                        timer_text.text = Mathf.CeilToInt(last_state_time + show_results_duration - Time.timeSinceLevelLoad) + "s";
                        break;
                    }
                case STATE_WINNER:
                    {
                        prompt_text.text = "There are no winners :(";
                        foreach (YesNoPlayer player in players)
                        {
                            if (player.player_id >= 0 && player.health > 0)
                            {

                                prompt_text.text = "Winner: " + Networking.GetOwner(player.gameObject).displayName;
                                break;
                            }
                        }
                        if (local_player)
                        {
                            local_player.health = 0;
                        }
                        timer_text.text = Mathf.CeilToInt(last_state_time + show_results_duration - Time.timeSinceLevelLoad) + "s";
                        if (Time.timeSinceLevelLoad - last_state_time > show_results_duration)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_STOPPED;
                            }
                        }
                        break;
                    }
                case STATE_ERROR:
                    {
                        if (local_player)
                        {
                            local_player.health = 0;
                        }
                        break;
                    }
                default:
                    {
                        if (local_player)
                        {
                            local_player.health = 0;
                        }
                        break;
                    }
            }
        }

        public void PickNextPicker()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                return;
            }
            if (active_players.Count < 2)
            {
                game_state = STATE_WINNER;
                return;
            }
            turn = ((YesNoPlayer)active_players[Random.Range(0, active_players.Count)].Reference).player_id;
        }


        void OnEnable()
        {
            prompt_text.text = "";
            timer_text.text = "";
            RemakePlayerList();
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                ResetGame();
            }
        }
        VRCPlayerApi[] vrc_players;
        public void ResetGame()
        {
            active_players.Clear();
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                return;
            }
            for (short i = 0; i < players.Length; i++)
            {
                Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                players[i].id = i;
                players[i].player_id = -1001;
                players[i].ResetPlayer();
            }
            vrc_players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(vrc_players);
            for (short i = 0; i < vrc_players.Length; i++)
            {
                players[i].player_id = (short)-vrc_players[i].playerId;
            }
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                return;
            }

            //unresetve it
            for (short i = 0; i < players.Length; i++)
            {
                if (players[i].player_id == -player.playerId || players[i].player_id == player.playerId)
                {
                    Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                    players[i].player_id = -1001;
                    break;
                }
            }
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
            {
                return;
            }

            //reserve it
            for (short i = 0; i < players.Length; i++)
            {
                if (players[i].player_id < -100)
                {
                    Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                    players[i].player_id = (short)-player.playerId;
                    break;
                }
            }
        }

        public void Leave()
        {
            RemovePlayer(Networking.LocalPlayer);
        }
        public void Join()
        {
            if (game_state < 0 || game_state == STATE_WINNER)
            {
                AddPlayer(Networking.LocalPlayer);
            }
        }
        public void ToggleJoin()
        {
            if (local_player)
            {
                Leave();
            }
            else
            {
                Join();
            }
        }

        public void RemovePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
            {
                return;
            }
            for (short i = 0; i < players.Length; i++)
            {
                if (players[i].player_id == player.playerId)
                {
                    Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                    players[i].player_id = (short)-player.playerId;
                    break;
                }
            }
        }

        bool added_player;
        public void AddPlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player))
            {
                return;
            }
            //should not be possible to have two of these reserved
            // for(short i = 0; i < players.Length; i++){
            //     if(players[i].player_id == player.playerId){
            //         //already joined
            //         return;
            //     }
            // }
            added_player = false;
            for (short i = 0; i < players.Length; i++)
            {
                if (players[i].player_id == (short)-player.playerId)
                {
                    Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                    players[i].player_id = (short)player.playerId;
                    added_player = true;
                    break;
                }
            }
            if (!added_player)
            {
                for (short i = 0; i < players.Length; i++)
                {
                    if (players[i].player_id == -1001)
                    {
                        Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                        players[i].player_id = (short)player.playerId;
                        added_player = true;
                        break;
                    }
                }
            }
        }

        [System.NonSerialized]
        public YesNoPlayer local_player;

        public void OnJoinGame(YesNoPlayer player)
        {
            if (!active_players.Contains(player))
            {
                active_players.Add(player);
                if (player.player_id == Networking.LocalPlayer.playerId)
                {
                    local_player = player;
                    local_player.health = 1;
                }
            }
            RemakePlayerList();
        }

        public void OnLeaveGame(YesNoPlayer player)
        {
            active_players.Remove(player);
            if (local_player == player)
            {
                local_player = null;
            }
            RemakePlayerList();
        }

        string player_list;
        VRCPlayerApi temp_player;
        public void RemakePlayerList()
        {
            player_list = "";
            foreach (YesNoPlayer player in players)
            {
                if (player.player_id > 0)
                {
                    temp_player = VRCPlayerApi.GetPlayerById(player.player_id);
                    if (Utilities.IsValid(temp_player))
                    {
                        player_list += temp_player.displayName + "\n";
                    }
                }
            }
            playerlist_text.text = player_list;
        }

        public void OnMadeChoice(short choice)
        {
            animator.SetInteger("choice", choice);
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || game_state < 0)
            {
                owner_text.text = "";
            }
            owner_text.text = "GAME MASTER: " + player.displayName;
        }
    }
}
