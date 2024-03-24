using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
#endif

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Animator))]
    public abstract class Game : UdonSharpBehaviour
    {
        [HideInInspector]
        public Animator animator;

        [System.NonSerialized]
        public float last_state_change = -1001f;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(state))]
        public short _state = -1001;
        public short state
        {
            get => _state;
            set
            {
                last_state_change = Time.timeSinceLevelLoad;
                if (!Utilities.IsValid(local_player))
                {
                    _state = value;
                    return;
                }
                OnChangeState(_state, value);
                _state = value;
                animator.SetInteger("state", value);
                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }


        [System.NonSerialized]
        public short turn_joined_index = -1001;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(turn))]
        public short _turn = -1001;
        public short turn
        {
            get => _turn;
            set
            {
                if (!Utilities.IsValid(local_player))
                {
                    _turn = value;
                    return;
                }
                Player new_turn_player = null;
                if (value >= 0 && value < players.Length)
                {
                    new_turn_player = players[value];
                }
                OnChangeTurn(_turn, value, turn_player, new_turn_player);
                _turn = value;
                turn_player = new_turn_player;
                if (turn_joined_index < 0 || turn_joined_index >= joined_player_ids.Count || joined_player_ids[turn_joined_index].Int != turn)
                {
                    temp_joined_players = joined_player_ids.ToArray();
                    for (int i = 0; i < temp_joined_players.Length; i++)
                    {
                        if (temp_joined_players[i].Int == turn)
                        {
                            turn_joined_index = (short)i;
                            break;
                        }
                    }
                }
                if (turn_player)
                {
                    turn_player.last_turn = Time.timeSinceLevelLoad;
                }
                animator.SetInteger("turn", value);
                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public abstract void OnChangeState(short old_state, short new_state);

        public abstract void OnChangeTurn(short old_turn, short new_turn, Player old_turn_player, Player new_turn_player);

        public abstract void OnThrowCard(CardThrowingHandler handler, int target_index, CardThrowing card);

        public abstract void OnSelectCard(Player player, int card_id);

        public abstract void OnScoreChange(Player player, int old_score, int new_score);

        public abstract void OnJoinGame(Player player);

        public abstract void OnLeftGame(Player player);

        public Player[] players;
        DataToken[] temp_joined_players;

        [System.NonSerialized]
        public Player local_player_obj;
        [System.NonSerialized]
        public Player turn_player;
        VRCPlayerApi local_player;

        public void OnEnable()
        {
            local_player = Networking.LocalPlayer;
            if (local_player.IsOwner(gameObject))
            {
                ResetPlayers();
                state = -1001;
                turn = -1001;
            }
            else
            {
                state = state;
                turn = turn;
            }
        }

        public void ResetPlayers()
        {
                Debug.LogWarning("ResetPlayers");
            foreach (Player player in players)
            {
                Networking.SetOwner(local_player, player.gameObject);
                player.player_id = -1001;
            }
            VRCPlayerApi[] vrc_players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(vrc_players);
            for (int i = 0; i < vrc_players.Length; i++)
            {
                if (Utilities.IsValid(vrc_players[i]))
                {
                    //twos complement
                    players[i].player_id = (short)(-1 - vrc_players[i].playerId);
                }
            }
        }

        public virtual void JoinGame()
        {
            Debug.LogWarning("JoinGame");
            if (local_player_obj)
            {
                return;
            }
            foreach (Player player in players)
            {
                if (player.id == -1 - local_player.playerId)
                {
                    Debug.LogWarning("found our player");
                    Networking.SetOwner(local_player, player.gameObject);
                    player.player_id = (short)local_player.playerId;
                    return;
                }
            }
            Debug.LogError("We couldn't find a reserved player object");
            foreach (Player player in players)
            {
                if (player.id < 0)
                {
                    Networking.SetOwner(local_player, player.gameObject);
                    player.player_id = (short)local_player.playerId;
                    return;
                }
            }
        }

        public virtual void LeaveGame()
        {
            if (!local_player_obj)
            {
                return;
            }
            Networking.SetOwner(Networking.LocalPlayer, local_player_obj.gameObject);
            local_player_obj.player_id = -1001;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (local_player.isInstanceOwner)
            {
                foreach (Player p in players)
                {
                    if (p.player_id == -1001)
                    {
                        Networking.SetOwner(local_player, p.gameObject);
                        p.player_id = (short)(-1 - player.playerId);
                        break;
                    }
                }
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (local_player.isInstanceOwner)
            {
                foreach (Player p in players)
                {
                    if (p.player_id == player.playerId || p.player_id == -1 - player.playerId)
                    {
                        Networking.SetOwner(local_player, p.gameObject);
                        p.player_id = -1001;
                        break;
                    }
                }
            }
        }

        [System.NonSerialized]
        public DataList joined_player_ids = new DataList();
        public bool randomize_players;
        [Tooltip("Will \"reshuffle\" this many times when picking a player. Should probably be around 3.")]
        public int reshuffles;

        public virtual Player RandomPlayer()
        {
            if (joined_player_ids.Count <= 0)
            {
                return null;
            }
            Player random = players[joined_player_ids[Random.Range(0, joined_player_ids.Count)].Int];
            Player other_random;
            if (reshuffles > 0)
            {
                for (int i = 0; i < reshuffles; i++)
                {
                    other_random = players[joined_player_ids[Random.Range(0, joined_player_ids.Count)].Int];
                    if (other_random.last_turn < random.last_turn)
                    {
                        random = other_random;
                    }
                }
            }
            return random;
        }

        public virtual short NextPlayerId(int skip)
        {
            if (joined_player_ids.Count <= 0)
            {
                return -1001;
            }
            return (short)joined_player_ids[(turn_joined_index + skip) % joined_player_ids.Count].Int;
        }

        short temp_next_id;
        public virtual Player NextPlayer(int skip)
        {
            temp_next_id = NextPlayerId(skip);
            if (temp_next_id < 0 || temp_next_id >= players.Length)
            {
                return null;
            }

            return players[temp_next_id];
        }


#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            animator = GetComponent<Animator>();
        }

        public void OnValidate()
        {
            if (players.Length > 0)
            {
                System.Collections.Generic.HashSet<Player> only_valid = new();
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i])
                    {
                        only_valid.Add(players[i]);
                    }
                }
                players = only_valid.ToArray<Player>();
            }
            else
            {
                players = GetComponentsInChildren<Player>();
            }
            for (int i = 0; i < players.Length; i++)
            {
                players[i].id = (short)i;
                players[i].game = this;
            }
        }
#endif
    }
}
