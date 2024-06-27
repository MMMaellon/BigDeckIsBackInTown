
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PickupToggle : UdonSharpBehaviour
    {
        public LightSync.LightSync target;
        public Toggle toggle;
        [UdonSynced, FieldChangeCallback(nameof(pickupable))]
        public bool _pickupable = false;
        public bool pickupable
        {
            get => _pickupable;
            set
            {
                _pickupable = value;
                if (toggle)
                {
                    toggle.isOn = value;
                }
                target.pickupableFlag = value;
                if (disable_interaction)
                {
                    target.DisableInteractive = !value;
                }
                if (!value && target.IsOwner())
                {
                    target.Respawn();
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }
        public bool disable_interaction = true;
        public void Start()
        {
            pickupable = pickupable;
        }

        public void TogglePickup()
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            pickupable = toggle.isOn;
        }
    }
}
