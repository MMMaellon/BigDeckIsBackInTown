
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WouldYouRather : GameSubmissionListener
    {
        public float prompt_select_duration = 30f;
        public float answer_select_duration = 30f;
        public float show_results_duration = 10f;
        public GameSubmissionSpot left_prompt;
        public GameSubmissionSpot right_prompt;
        public GameSubmissionSpot answer_prompt;
        public PlayerSpot[] player_spots;
        VRCPlayerApi.TrackingData head_data;

        public TextMeshPro left_prompt_text;
        public TextMeshPro right_prompt_text;

        public TextMeshPro left_names;
        public TextMeshPro right_names;
        public TextMeshPro turn_name;
        public TextMeshPro timer;

        DataList active_players = new DataList();

        Vector3 forward_dir;
        VRCPlayerApi local_player;
        public void Start()
        {
            local_player = Networking.LocalPlayer;
        }

        int last_loop = -1001;
        public float deal_cards_delay = 1f;
        public void GameLoop()
        {
            if (last_loop == Time.frameCount || game_state < 0)
            {
                return;
            }
            last_loop = Time.frameCount;
            SendCustomEventDelayedFrames(nameof(GameLoop), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            forward_dir = transform.position - Networking.LocalPlayer.GetPosition();
            forward_dir.y = 0;
            transform.rotation = Quaternion.LookRotation(forward_dir);
            switch (game_state)
            {
                case STATE_SELECTING_PROMPTS:
                    {
                        timer.text = Mathf.CeilToInt(last_state_change + prompt_select_duration - Time.timeSinceLevelLoad) + "s";
                        if (local_player.IsOwner(gameObject))
                        {
                            if (last_state_change + prompt_select_duration < Time.timeSinceLevelLoad)
                            {
                                game_state = STATE_GETTING_ANSWERS;
                            }
                        }
                        if (!cards_dealt && last_state_change + deal_cards_delay < Time.timeSinceLevelLoad)
                        {
                            cards_dealt = true;
                            if (local_spot && local_spot.spot_id == turn)
                            {
                                if (!Networking.LocalPlayer.IsOwner(gameObject))
                                {
                                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                                }
                                //put all cards back in the deck and select a new card
                                foreach (var card in answer_prompt.deck.cards)
                                {
                                    if (card.IsActiveState())
                                    {
                                        continue;
                                    }
                                    card.EnterState();
                                    card.selected = false;
                                }
                                local_spot.DealMultiple(8);
                            }
                        }
                        break;
                    }
                case STATE_GETTING_ANSWERS:
                    {
                        timer.text = Mathf.CeilToInt(last_state_change + answer_select_duration - Time.timeSinceLevelLoad) + "s";
                        if (local_player.IsOwner(gameObject))
                        {
                            if (last_state_change + answer_select_duration < Time.timeSinceLevelLoad)
                            {
                                game_state = STATE_SHOW_RESULTS;
                            }
                        }
                        if (!cards_dealt && last_state_change + deal_cards_delay < Time.timeSinceLevelLoad)
                        {
                            cards_dealt = true;
                            if (local_player.IsOwner(answer_prompt.deck.gameObject))
                            {
                                answer_prompt.deck.ResetDeck();
                                foreach (var spot in player_spots)
                                {
                                    if (!spot.activated)
                                    {
                                        continue;
                                    }
                                    spot.capacity = 2;
                                    spot.cards_to_deal = 2;
                                    spot.DealSpecificCard(right_prompt.submitted_text);
                                    spot.DealSpecificCard(left_prompt.submitted_text);
                                }
                            }
                        }
                        break;
                    }
                case STATE_SHOW_RESULTS:
                    {
                        timer.text = Mathf.CeilToInt(last_state_change + show_results_duration - Time.timeSinceLevelLoad) + "s";
                        if (local_player.IsOwner(gameObject))
                        {
                            if (last_state_change + show_results_duration < Time.timeSinceLevelLoad)
                            {
                                NextTurn();
                            }
                        }
                        if (!cards_dealt && last_state_change + deal_cards_delay < Time.timeSinceLevelLoad)
                        {
                            cards_dealt = true;
                            if (local_player.IsOwner(answer_prompt.deck.gameObject))
                            {
                                answer_prompt.deck.ResetDeck();
                            }
                        }
                        break;
                    }
            }
        }

        public Animator animator;
        public string game_parameter = "game";
        float last_state_change = -1001f;
        bool cards_dealt;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(game_state))]
        public int _game_state = STATE_STOPPED;
        public int game_state
        {
            get => _game_state;
            set
            {
                last_state_change = Time.timeSinceLevelLoad;
                _game_state = value;
                animator.SetInteger(game_parameter, value);
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }

                if (value >= 0 && last_loop < Time.frameCount)
                {
                    SendCustomEventDelayedFrames(nameof(GameLoop), 2, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                }

                cards_dealt = false;
                switch (value)
                {
                    case STATE_SELECTING_PROMPTS:
                        {
                            left_prompt_text.text = "";
                            right_prompt_text.text = "";
                            answer_prompt.allow_throwing = false;
                            break;
                        }
                    case STATE_GETTING_ANSWERS:
                        {
                            if (Networking.LocalPlayer.IsOwner(gameObject) && left_prompt.submitted_text < 0 || right_prompt.submitted_text < 0)
                            {
                                if (left_prompt.submitted_text < 0 && right_prompt.submitted_text < 0)
                                {
                                    player_spots[turn].SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Deactivate");
                                }
                                NextTurn();
                            }
                            player_answered = new bool[player_spots.Length];
                            if (local_spot)
                            {
                                local_spot.selection = -1001;
                            }
                            left_names.text = "";
                            right_names.text = "";

                            answer_prompt.allow_throwing = true;
                            left_prompt.allow_throwing = false;
                            right_prompt.allow_throwing = false;
                            StartTextLoop();

                            break;
                        }
                    case STATE_SHOW_RESULTS:
                        {
                            answer_prompt.allow_throwing = false;
                            left_prompt.allow_throwing = false;
                            right_prompt.allow_throwing = false;
                            break;
                        }

                    default:
                        {
                            if (local_spot)
                            {
                                local_spot.selection = -1001;
                            }
                            //Stopped
                            break;
                        }
                }
            }
        }

        bool found_turn;
        VRCPlayerApi turn_player;
        VRCPlayerApi temp_player;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(turn))]
        public int _turn = -1001;//id of the player who is choosing the prompts
        public int turn
        {
            get => _turn;
            set
            {
                _turn = value;
                found_turn = false;
                for (int i = 0; i < player_spots.Length; i++)
                {
                    if (value == i)
                    {
                        turn_player = Networking.GetOwner(player_spots[i].gameObject);
                        animator.SetBool("your_turn", turn_player.isLocal);
                        left_prompt.allow_throwing = turn_player.isLocal;
                        right_prompt.allow_throwing = turn_player.isLocal;
                        turn_name.text = turn_player.displayName + "'s Turn";
                        if (turn_player.isLocal)
                        {
                            Networking.SetOwner(Networking.LocalPlayer, left_prompt.gameObject);
                            Networking.SetOwner(Networking.LocalPlayer, right_prompt.gameObject);
                            left_prompt.submitted_text = -1001;
                            right_prompt.submitted_text = -1001;
                        }
                        found_turn = true;
                    }
                }
                if (!found_turn)
                {
                    turn_name.text = "";
                    animator.SetBool("your_turn", false);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public float cleanup_cards_delay = 0.1f;

        public const int STATE_STOPPED = -1001;
        public const int STATE_SELECTING_PROMPTS = 0;
        public const int STATE_GETTING_ANSWERS = 1;
        public const int STATE_SHOW_RESULTS = 2;

        public override void OnSubmit(CardPlacingState card, GameSubmissionSpot spot)
        {
            spot.allow_throwing = false;
            if (spot == left_prompt || spot == right_prompt)
            {
                StartTextLoop();
            }
            else if (local_spot)
            {
                local_spot.selection = card.GetComponent<CardText>().text_id;
            }
        }

        bool all_answered = false;
        public override void OnTextSubmitted(string text, GameSubmissionSpot spot)
        {
            if (spot == left_prompt || spot == right_prompt)
            {
                if (Networking.LocalPlayer.IsOwner(gameObject) && game_state == STATE_SELECTING_PROMPTS && left_prompt.submitted_text > 0 && right_prompt.submitted_text > 0)
                {
                    game_state = STATE_GETTING_ANSWERS;
                }
            }

        }

        bool[] player_answered;

        bool loop_requested = false;
        public void StartTextLoop()
        {
            if (loop_requested)
            {
                return;
            }
            left_prompt_text.text = "";
            right_prompt_text.text = "";
            SendCustomEventDelayedSeconds(nameof(UpdateTextLoop), 0.25f);
        }
        public void OnEnable()
        {
            game_state = game_state;
            loop_requested = false;
            StartTextLoop();
            active_players.Clear();
            foreach (var spot in player_spots)
            {
                if (spot.activated)
                {
                    active_players.Add(spot);
                }
            }
        }

        public int type_delay = 3;
        public void UpdateTextLoop()
        {
            loop_requested = false;
            if (left_prompt_text.text.Length == 0 || left_prompt.submitted_text_str.StartsWith(left_prompt_text.text))
            {
                if (left_prompt_text.text.Length < left_prompt.submitted_text_str.Length)
                {
                    left_prompt_text.text = left_prompt.submitted_text_str.Substring(0, left_prompt_text.text.Length + 1);
                    SendCustomEventDelayedFrames(nameof(UpdateTextLoop), type_delay);
                    loop_requested = true;
                }
            }
            else
            {
                left_prompt_text.text = left_prompt_text.text.Substring(0, left_prompt_text.text.Length - 1);
                SendCustomEventDelayedFrames(nameof(UpdateTextLoop), 1);
                loop_requested = true;
            }
            if (right_prompt_text.text.Length == 0 || right_prompt.submitted_text_str.StartsWith(right_prompt_text.text))
            {
                if (right_prompt_text.text.Length < right_prompt.submitted_text_str.Length)
                {
                    right_prompt_text.text = right_prompt.submitted_text_str.Substring(0, right_prompt_text.text.Length + 1);
                    SendCustomEventDelayedFrames(nameof(UpdateTextLoop), type_delay);
                    loop_requested = true;
                }
            }
            else
            {
                right_prompt_text.text = right_prompt_text.text.Substring(0, right_prompt_text.text.Length - 1);
                SendCustomEventDelayedFrames(nameof(UpdateTextLoop), 1);
                loop_requested = true;
            }
        }

        public override void OnPlayerSpotActivation(bool activated, PlayerSpot spot)
        {
            if (activated)
            {
                active_players.Add(spot);
            }
            else
            {
                active_players.RemoveAll(spot);
            }
            if (Networking.IsOwner(Networking.LocalPlayer, gameObject) && game_state == STATE_SELECTING_PROMPTS && !activated && spot.spot_id == turn)
            {
                //person left the game while it was their turn
                spot.Deactivate();
                NextTurn();
            }
        }

        PlayerSpot temp_spot;
        bool found_next = false;
        public void NextTurn()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (turn < 0)
            {
                _turn = 0;
            }
            found_next = false;
            for (int i = 1; i <= player_spots.Length; i++)
            {
                temp_spot = player_spots[(i + turn) % player_spots.Length];
                if (temp_spot.activated)
                {
                    turn = temp_spot.spot_id;
                    found_next = true;
                    break;
                }
            }
            if (found_next)
            {
                game_state = STATE_SELECTING_PROMPTS;
            }
            else
            {
                game_state = STATE_STOPPED;
            }
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                if (game_state == STATE_SELECTING_PROMPTS)
                {
                    //check if we're left in a weird state because the owner just left
                    if (turn >= 0)
                    {
                        NextTurn();
                    }
                }
            }
        }
        public void StartGame()
        {
            if (game_state < 0)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                NextTurn();
            }
        }
        public void StopGame()
        {
            if (game_state >= 0)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                game_state = STATE_STOPPED;
            }
        }
        public void ToggleGame()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (game_state >= 0)
            {
                game_state = STATE_STOPPED;
            }
            else
            {
                NextTurn();
            }
        }
        public override void OnPlayerSubmit(int selection, PlayerSpot spot)
        {
            temp_player = Networking.GetOwner(spot.gameObject);
            if (selection >= 0)
            {
                if (selection == left_prompt.submitted_text)
                {
                    left_names.text += temp_player.displayName + "\n";
                }
                else if (selection == right_prompt.submitted_text)
                {
                    right_names.text += temp_player.displayName + "\n";
                }
            }
            if (Networking.LocalPlayer.IsOwner(gameObject) && game_state == STATE_GETTING_ANSWERS)
            {
                all_answered = true;
                for (int i = 0; i < player_spots.Length; i++)
                {

                    if (player_spots[i].activated && player_spots[i].selection < 0)
                    {
                        all_answered = false;
                        break;
                    }
                }

                if (all_answered)
                {
                    game_state = STATE_SHOW_RESULTS;
                }
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {
            for (int i = 0; i < player_spots.Length; i++)
            {
                player_spots[i].spot_id = i;
            }
        }


#endif
    }
}
