using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Player : UdonSharpBehaviour
    {
        public Game game;
        public Animator animator;
        public TextMeshPro nameplate;
        public TextMeshPro score_display;
        public CardThrowTarget throw_target;
        VRCPlayerApi local_player;
        [System.NonSerialized]
        public VRCPlayerApi vrc_player;
        [System.NonSerialized]
        public float last_turn = -1001;

        [HideInInspector]
        public short id;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(selected_card))]
        public int _selected_card;
        public int selected_card
        {
            get => _selected_card;
            set
            {
                _selected_card = value;
                OnSubmitCard(value);
                game.OnSelectCard(this, value);
                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(score))]
        public int _score;
        public int score
        {
            get => _score;
            set
            {
                if (score_display)
                {
                    score_display.text = value.ToString();
                }
                OnScoreChange(_score, value);
                game.OnScoreChange(this, _score, value);
                _score = value;
                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(player_id))]
        public short _player_id = -1001;
        public short player_id
        {
            get => _player_id;
            set
            {
                if (_player_id >= 0)
                {
                    OnLeave();
                    game.OnLeftGame(this);
                }
                _player_id = value;
                if (value >= 0)
                {
                    OnJoin();
                    game.OnJoinGame(this);
                }
                Debug.LogWarning("setting player id " + value);
                if (!Utilities.IsValid(local_player))
                {
                    return;
                }
                if (local_player.playerId == value && !local_player.IsOwner(gameObject))
                {
                    //world owner is asking us to take ownership of this object
                    Networking.SetOwner(local_player, gameObject);
                }
                Debug.LogWarning("value is " + value);

                if (value >= 0)
                {
                    vrc_player = VRCPlayerApi.GetPlayerById(value);
                    if (nameplate && Utilities.IsValid(vrc_player))
                    {
                        nameplate.text = vrc_player.displayName;
                    }
                    if (animator)
                    {
                        animator.SetBool("joined", true);
                    }
                    if (!game.joined_player_ids.Contains(id))
                    {
                        game.joined_player_ids.Add(id);
                    }
                    if (Utilities.IsValid(vrc_player) && vrc_player.isLocal)
                    {
                        game.local_player_obj = this;
                        if (throw_target)
                        {
                            Networking.SetOwner(local_player, throw_target.gameObject);
                        }
                    }
                }
                else
                {
                    vrc_player = null;
                    if (nameplate)
                    {
                        nameplate.text = "";
                    }
                    if (animator)
                    {
                        animator.SetBool("joined", false);
                    }

                    game.joined_player_ids.RemoveAll(id);
                    if (game.local_player_obj == this)
                    {
                        Debug.LogWarning("local player obj is being set to null from " + gameObject.name);
                        game.local_player_obj = null;
                    }
                }

                if (local_player.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public void OnEnable()
        {
            local_player = Networking.LocalPlayer;
            player_id = player_id;
        }
        public virtual void OnSubmitCard(int card_id)
        {

        }
        public virtual void OnScoreChange(int old_score, int new_score)
        {

        }


        public virtual void OnJoin()
        {

        }

        public virtual void OnLeave()
        {

        }

        public void Join()
        {
            if (game.local_player_obj)
            {
                game.local_player_obj.Leave();
            }
            Networking.SetOwner(local_player, gameObject);
            player_id = (short)local_player.playerId;
        }

        public void LeaveIfOwner()
        {
            if (IsLocal())
            {
                Leave();
            }
        }

        public void Leave()
        {
            Networking.SetOwner(local_player, gameObject);
            if (Utilities.IsValid(vrc_player))
            {
                player_id = (short)(-1 - vrc_player.playerId);
            }
            else
            {
                player_id = -1001;
            }
        }

        public void RequestReset()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(ResetPlayer));
        }

        public void ResetPlayer()
        {
            Networking.SetOwner(local_player, gameObject);
            score = 0;
            selected_card = -1001;
        }

        public void TakeOwnershipOfGame()
        {
            if (Utilities.IsValid(vrc_player))
            {
                Networking.SetOwner(vrc_player, game.gameObject);
            }
        }

        public bool IsLocal()
        {
            return player_id >= 0 && Utilities.IsValid(vrc_player) && vrc_player.isLocal;
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {
            if (!nameplate)
            {
                nameplate = GetComponentInChildren<TextMeshPro>();
            }
            if (!animator)
            {
                animator = GetComponent<Animator>();
            }
        }
#endif

    }
}
