using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Animator))]
    public class Game : CardThrowingHandler
    {
        [HideInInspector]
        public Animator animator;

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(state))]
        public short _state = -1001;
        public short state
        {
            get => _state;
            set
            {
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
                turn_player.last_turn = Time.timeSinceLevelLoad;
                animator.SetInteger("turn", value);
                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public virtual void OnChangeState(short old_state, short new_state)
        {

        }
        public virtual void OnChangeTurn(short old_turn, short new_turn, Player old_turn_player, Player new_turn_player)
        {

        }

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
            foreach (Player player in players)
            {
                Networking.SetOwner(local_player, player.gameObject);
                player.player_id = -1001;
                player.ResetPlayer();
            }
            VRCPlayerApi[] vrc_players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(vrc_players);
            for (int i = 0; i < vrc_players.Length; i++)
            {
                if (Utilities.IsValid(vrc_players[i]))
                {
                    //twos complement
                    players[i].player_id = -1 - vrc_players[i].playerId;
                }
            }
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
                        p.player_id = -1 - player.playerId;
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

        public DataList joined_player_ids = new DataList();

        public virtual Player RandomPlayer(int reshuffles)
        {
            if (joined_player_ids.Count <= 0)
            {
                return null;
            }
            Player random = players[joined_player_ids[Random.Range(0, joined_player_ids.Count)].Int];
            Player other_random;
            while (reshuffles > 0)
            {
                other_random = players[joined_player_ids[Random.Range(0, joined_player_ids.Count)].Int];
                if (other_random.last_turn < random.last_turn)
                {
                    random = other_random;
                }
                reshuffles--;
            }
            return random;
        }

        public virtual Player NextPlayer(int skip)
        {
            if (joined_player_ids.Count <= 0)
            {
                return null;
            }
            return players[joined_player_ids[(turn_joined_index + skip) % joined_player_ids.Count].Int];
        }

        public override void OnThrowCard(int target_index, CardThrowing card)
        {

        }


#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            animator = GetComponent<Animator>();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (players.Length > 0)
            {
                System.Collections.Generic.List<Player> only_valid = new();
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i])
                    {
                        only_valid.Add(players[i]);
                    }
                }
                players = only_valid.ToArray();
            }
            else
            {
                players = GetComponentsInChildren<Player>();
            }
            for (int i = 0; i < players.Length; i++)
            {
                players[i].id = i;
                players[i].game = this;
            }
        }
#endif
    }
}
