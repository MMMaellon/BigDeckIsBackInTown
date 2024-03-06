
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExampleCustomPlacement : CardPlacementSpot
    {
        public float spacing = 0.08f;
        public int limit = 5;
        [System.NonSerialized, UdonSynced]
        public int card_count = 0;

        public override void Place(CardPlacingState card)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            base.Place(card);
            card_count = (card_count + 1) % limit;
            RequestSerialization();
        }

        public override Vector3 GetPlacementPosition(CardPlacingState card)
        {
            return placement_transform.position - (placement_transform.rotation * Vector3.left * (card_count - ((limit - 1) / 2f)) * spacing);
        }
        public override Vector3 GetAimPosition(CardPlacingState card)
        {
            return GetPlacementPosition(card);
        }
    }
}
