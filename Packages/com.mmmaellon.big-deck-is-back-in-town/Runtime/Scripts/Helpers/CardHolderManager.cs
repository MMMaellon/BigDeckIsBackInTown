
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CardHolderManager : UdonSharpBehaviour
    {
        public CardHolder[] holders;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {
            holders = GameObject.FindObjectsOfType<CardHolder>();
            for (int i = 0; i < holders.Length; i++)
            {
                holders[i].id = (short)i;
            }
        }
#endif
    }
}
