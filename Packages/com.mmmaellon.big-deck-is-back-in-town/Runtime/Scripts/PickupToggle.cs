
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PickupToggle : UdonSharpBehaviour
    {
        public SmartObjectSync target;
        public Toggle toggle;
        [UdonSynced, FieldChangeCallback(nameof(pickupable))]
        public bool _pickupable = false;
        public bool pickupable {
            get => _pickupable;
            set {
                Debug.LogWarning("Setting toggle to " + value);
                _pickupable = value;
                if(toggle){
                    toggle.isOn = value;
                }
                target.pickupable = value;
                if(disable_interaction){
                    target.DisableInteractive = !value;
                }
                if(!value && target.IsLocalOwner()){
                    target.Respawn();
                }
                if(Networking.LocalPlayer.IsOwner(gameObject)){
                    RequestSerialization();
                }
            }
        }
        public bool disable_interaction = true;
        public void Start()
        {
            pickupable = pickupable;
        }
        
        public void TogglePickup(){
            if(!Networking.LocalPlayer.IsOwner(gameObject)){
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            pickupable = toggle.isOn;
        }
    }
}
