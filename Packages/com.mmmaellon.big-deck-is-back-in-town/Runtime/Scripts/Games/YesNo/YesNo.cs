
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class YesNo : UdonSharpBehaviour
    {
        public Transform prompt_picker_teleport;
        public Transform jail_teleport;
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
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                if (local_player && turn == local_player.player_id)
                {
                    Networking.LocalPlayer.TeleportTo(prompt_picker_teleport.position, prompt_picker_teleport.rotation);
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
        public PlayerSpot card_dealer;
        public GameSubmissionSpot prompt_submission;
        float last_state_time;
        public float prompt_picking_duration = 30f;
        public float answer_picking_duration = 10f;
        public float show_results_duration = 3f;
        bool freeze_pos;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(game_state))]
        public short _game_state = STATE_STOPPED;
        public short game_state
        {
            get => _game_state;
            set
            {
                last_state_time = Time.timeSinceLevelLoad;
                _game_state = value;
                animator.SetInteger("game", value);
                switch (value)
                {
                    case STATE_SELECTING_PROMPT:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            if (Networking.LocalPlayer.IsOwner(prompt_submission.gameObject))
                            {
                                prompt_submission.submitted_text = -1001;
                            }
                            prompt_submission.submitted_text_str = "";
                            break;
                        }
                    case STATE_SELECTING_ANSWER:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            freeze_pos = false;
                            break;
                        }
                    case STATE_RESULTS:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            if (local_player && turn_player && local_player.choice != turn_player.choice)
                            {
                                //DIE
                                Networking.LocalPlayer.CombatSetCurrentHitpoints(0);
                            }
                            break;
                        }
                    case STATE_WINNER:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            break;
                        }
                    case STATE_ERROR:
                        {
                            SendCustomEventDelayedFrames(nameof(Loop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                            break;
                        }
                    default:
                        {
                            last_loop = Time.frameCount + 3;
                            break;
                        }
                }
            }
        }
        public const short STATE_STOPPED = -1001;
        public const short STATE_SELECTING_PROMPT = 0;
        public const short STATE_SELECTING_ANSWER = 1;
        public const short STATE_RESULTS = 2;
        public const short STATE_WINNER = 3;
        public const short STATE_ERROR = 4;

        //These are going to be controlled by animations
        int living_count = 0;
        public void OnPromptState()
        {
            living_count = 0;
            foreach (YesNoPlayer player in players)
            {
                if (player.health > 0)
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
            card_dealer.DealMultiple(8);
        }

        public void OnAnswerState()
        {
            //when there's only like one second left on the timer
            freeze_pos = true;
        }

        public void OnResultsState()
        {
            //DIE
            Networking.LocalPlayer.CombatSetCurrentHitpoints(100);
        }

        public void OnWinnerState()
        {

        }

        int last_loop = -1001;
        public TextMeshPro timer_text;
        public void Loop()
        {
            if (last_loop <= Time.frameCount)
            {
                return;
            }
            last_loop = Time.frameCount;
            SendCustomEventDelayedFrames(nameof(Loop), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            timer_text.text = Time.timeSinceLevelLoad - last_state_time + "s";
            switch (game_state)
            {
                case STATE_SELECTING_PROMPT:
                    {
                        if (Networking.LocalPlayer.IsOwner(gameObject))
                        {
                            if (Time.timeSinceLevelLoad - last_loop > prompt_picking_duration)
                            {
                                game_state = STATE_RESULTS;
                            }
                            else if (prompt_submission.submitted_text >= 0 && local_player && local_player.choice != 0)
                            {
                                game_state = STATE_SELECTING_ANSWER;
                            }
                        }
                        break;
                    }
                case STATE_SELECTING_ANSWER:
                    {
                        if (Time.timeSinceLevelLoad - last_loop > answer_picking_duration)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_RESULTS;
                            }
                        }
                        break;
                    }
                case STATE_RESULTS:
                    {
                        if (freeze_pos)
                        {
                            Networking.LocalPlayer.SetVelocity(Vector3.zero);
                        }
                        if (Time.timeSinceLevelLoad - last_loop > prompt_picking_duration)
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject))
                            {
                                game_state = STATE_SELECTING_PROMPT;
                            }
                        }
                        break;
                    }
                case STATE_WINNER:
                    {
                        break;
                    }
                case STATE_ERROR:
                    {

                        break;
                    }
                default:
                    {

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
                game_state = STATE_STOPPED;
                return;
            }
            turn = ((YesNoPlayer)active_players[Random.Range(0, active_players.Count)].Reference).player_id;
        }


        void OnEnable()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                ResetGame();
            }
        }
        VRCPlayerApi[] vrc_players;
        public void ResetGame()
        {
            Networking.LocalPlayer.CombatSetup();
            Networking.LocalPlayer.CombatSetMaxHitpoints(100);
            Networking.LocalPlayer.CombatSetDamageGraphic(null);
            Networking.LocalPlayer.CombatSetCurrentHitpoints(100);
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
                }
            }
        }

        public void Leave()
        {
            RemovePlayer(Networking.LocalPlayer);
        }
        public void Join()
        {
            AddPlayer(Networking.LocalPlayer);
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
            for (short i = 0; i < players.Length; i++)
            {
                if (players[i].player_id == (short)-player.playerId)
                {
                    Networking.SetOwner(Networking.LocalPlayer, players[i].gameObject);
                    players[i].player_id = (short)player.playerId;
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
                }
            }
        }

        public void OnLeaveGame(YesNoPlayer player)
        {
            active_players.Remove(player);
            if (local_player == player)
            {
                local_player = null;
            }
        }
    }
}
