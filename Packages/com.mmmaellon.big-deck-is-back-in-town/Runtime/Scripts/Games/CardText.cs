
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardText : Card
    {
        public TextMeshPro text;
        [System.NonSerialized]
        public CardTextBank bank = null; //becomes not null once the bank finishes parsing
        [System.NonSerialized, UdonSynced]
        public int text_id = -1001;
        [System.NonSerialized, UdonSynced]
        public int random_id = -1001;
        [System.NonSerialized, UdonSynced]
        public int owner_id = -1001;

        public override void OnEnterState()
        {
            base.OnEnterState();
            if (sync.IsOwnerLocal())
            {
                PickCardText();
            }
        }
        public void PickCardText()
        {
            if (!bank)
            {
                text_id = -69;
                return;
            }
            text_id = bank.RandomCardId();
            random_id = bank.RandomPlayerId();
            owner_id = sync.owner.playerId;
            RequestSerialization();
            SetText();
        }

        int _text_id;
        int _random_id;
        int _owner_id;
        float last_sent_time = -1001f;
        string text_str;
        VRCPlayerApi random_player;
        VRCPlayerApi owner_player;
        public override void OnDeserialization(VRC.Udon.Common.DeserializationResult result)
        {
            if (result.sendTime < last_sent_time)
            {
                text_id = _text_id;
                random_id = _random_id;
                owner_id = _owner_id;
                return;
            }
            last_sent_time = result.sendTime;
            _text_id = text_id;
            _random_id = random_id;
            _owner_id = owner_id;

            SetText();
        }

        public void SetText()
        {
            if (!bank)
            {
                return;
            }
            if (text_id < 0 || text_id >= bank.texts.Length)
            {
                text.text = bank.loading_text;
                return;
            }
            bank.last_text_selection_times.SetValue(text_id, Time.timeSinceLevelLoad);
            bank.last_player_selection_times.SetValue(random_id, Time.timeSinceLevelLoad);
            text_str = bank.texts[text_id];
            random_player = VRCPlayerApi.GetPlayerById(random_id);
            owner_player = VRCPlayerApi.GetPlayerById(owner_id);
            text.text = bank.ReplaceStringVariables(text_str, owner_player, random_player);
        }

        public void OnBankParseComplete()
        {
            if (sync.IsOwnerLocal())
            {
                PickCardText();
                RequestSerialization();
            }
            else
            {
                SetText();
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            if (!text)
            {
                text = GetComponentInChildren<TextMeshPro>();
            }
            if (text && !child)
            {
                child = text.gameObject;
            }
        }
#endif
    }
}
