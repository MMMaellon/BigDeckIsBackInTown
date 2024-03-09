
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
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
                if (text)
                {
                    if (value < 0)
                    {
                        text.text = "";
                    }
                    else if (value < bank.texts.Length)
                    {
                        text.text = bank.texts[value];
                    }
                    else
                    {
                        text.text = "";
                    }
                }
                if (card.sync.IsLocalOwner())
                {
                    RequestSerialization();
                }
            }
        }
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {

        }


        void Start()
        {

        }
#if COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset(){
            card = GetComponent<Card>();
        }
#endif
    }
}
