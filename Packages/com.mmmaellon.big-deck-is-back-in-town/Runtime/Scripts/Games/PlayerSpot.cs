
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerSpot : CardPlacementSpot
    {
        public int spot_id;
        public TextMeshPro nameplate;
        public GameSubmissionListener listener;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(activated))]
        public bool _activated;
        public bool activated
        {
            get => _activated;
            set
            {
                if (listener && value != _activated)
                {
                    listener.OnPlayerSpotActivation(value, this);
                }
                _activated = value;
                if (animator)
                {
                    animator.SetBool("active", value);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
                if (value)
                {
                    if (nameplate)
                    {
                        nameplate.text = Networking.GetOwner(gameObject).displayName;
                    }
                    if (listener && Networking.LocalPlayer.IsOwner(gameObject))
                    {
                        if (listener.local_spot)
                        {
                            listener.local_spot.Deactivate();
                        }
                        listener.local_spot = this;
                    }
                }
                else
                {
                    if (nameplate)
                    {
                        nameplate.text = "";
                    }
                    if (listener && listener.local_spot == this && Networking.LocalPlayer.IsOwner(gameObject))
                    {
                        listener.local_spot = null;
                    }
                }
            }
        }

        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(selection))]
        public int _selection;
        public int selection
        {
            get => _selection;
            set
            {
                _selection = value;
                if (listener)
                {
                    listener.OnPlayerSubmit(value, this);
                }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    RequestSerialization();
                }
            }
        }

        public void OnEnable()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject))
            {
                activated = false;
            }
            activated = activated;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player) && player.isLocal)
            {
                if (activated)
                {
                    activated = false;
                }
            }
        }

        public void Activate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            activated = true;
        }

        public void Deactivate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            activated = false;
        }

        public void ToggleActivation()
        {
            if (activated && Networking.LocalPlayer.IsOwner(gameObject))
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }
    }
}
