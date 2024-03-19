
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(Card))]
    public class CardText : SmartObjectSyncListener
    {
        public Card card;
        public CardTextBank bank;
        public TextMeshPro text;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(text_id))]
        public int _text_id = -1001;
        public int text_id
        {
            get => _text_id;
            set
            {
                _text_id = value;
                if (card.sync.IsLocalOwner())
                {
                    RequestSerialization();
                }
                if (value < 0)
                {
                    text.text = "";
                }
                else if (value < bank.texts.Length)
                {
                    text.text = bank.ReplaceStringVariables(bank.texts[value], card.sync.owner, random_player);
                }
                else
                {
                    text.text = "<color=red>ERROR PREFAB BROKE</color>";
                }
            }
        }
        public string RandomNameVariable = "[RANDOM_NAME]";
        public string PlayerNameVariable = "[PLAYER_NAME]";
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(player_id))]
        public int _player_id = -1001;
        public int player_id
        {
            get => _player_id;
            set
            {
                _player_id = value;
                random_player = VRCPlayerApi.GetPlayerById(value);
                text.text = bank.ReplaceStringVariables(text.text, card.sync.owner, random_player);
                if (card.sync.IsLocalOwner())
                {
                    RequestSerialization();
                }
            }
        }
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }
        public void Start()
        {
            card.sync.AddListener(this);
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (oldState == card.stateID + SmartObjectSync.STATE_CUSTOM)
            {
                if (card.sync.IsLocalOwner())
                {
                    text_id = bank.RandomCardId();
                    player_id = bank.RandomPlayerId();
                }
            }
        }

        VRCPlayerApi random_player;

#if COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset(){
            card = GetComponent<Card>();
            card.sync.AddListener(this);
        }
        public void OnValidate(){
            card.sync.AddListener(this);
        }
#endif
    }
}
