
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(VRCPickup))]
    public class CardHolder : SmartObjectSyncListener
    {
        public short id;
        CardHolderState card;
        public MeshRenderer mesh;
        public string highlight_color_variable_name = "_EmissionColor";
        [ColorUsage(hdr: true, showAlpha: true)]
        public Color highlight_color = Color.yellow;
        [System.NonSerialized]
        public Color start_color;

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {

        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {

            card = sync.GetComponent<CardHolderState>();
            if (!card)
            {
                return;
            }
            sync.RemoveListener(this);
            listeners--;
            HighlightOff();
            if (!sync.IsOwnerLocal())
            {
                return;
            }
            if (oldState != SmartObjectSync.STATE_LEFT_HAND_HELD && oldState != SmartObjectSync.STATE_RIGHT_HAND_HELD && oldState != SmartObjectSync.STATE_NO_HAND_HELD)
            {
                return;
            }
            if (newState != SmartObjectSync.STATE_FALLING && newState != SmartObjectSync.STATE_INTERPOLATING)
            {
                return;
            }
            //at this point we know the object was dropped after making contact with our collider
            card.Attach(this);
        }


        int listeners = 0;
        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            card = other.GetComponent<CardHolderState>();
            if (!card || !card.sync.IsHeld() || !card.sync.IsLocalOwner())
            {
                return;
            }
            card.sync.AddListener(this);
            listeners++;
            HighlightOn();
        }
        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            card = other.GetComponent<CardHolderState>();
            if (!card)
            {
                return;
            }
            card.sync.RemoveListener(this);
            listeners--;
            HighlightOff();
        }

        public void Start()
        {
            if (mesh)
            {
                start_color = mesh.material.GetColor(highlight_color_variable_name);
            }
        }
        public void HighlightOn()
        {
            if (mesh)
            {
                mesh.sharedMaterial.SetColor(highlight_color_variable_name, highlight_color);
            }
        }
        public void HighlightOff()
        {
            if (mesh && listeners <= 0)
            {
                mesh.sharedMaterial.SetColor(highlight_color_variable_name, start_color);
                if(listeners < 0){
                    listeners = 0;
                }
            }

        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            mesh = GetComponent<MeshRenderer>();
        }
#endif
    }
}
