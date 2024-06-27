
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(VRCPickup))]
    public class CardHolder : LightSyncListener
    {
        public short id;
        CardHolderState card;
        public MeshRenderer mesh;
        public string highlight_color_variable_name = "_EmissionColor";
        [ColorUsage(hdr: true, showAlpha: true)]
        public Color highlight_color = Color.yellow;
        [System.NonSerialized]
        public Color start_color;

        public bool visible_only_to_owner = true;
        public bool pickupable_only_by_owner = false;

        public override void OnChangeState(LightSync.LightSync sync, int oldState, int newState)
        {

            card = sync.GetComponent<CardHolderState>();
            if (!card)
            {
                return;
            }
            sync.RemoveClassListener(this);
            listeners--;
            HighlightOff();
            if (!sync.IsOwner())
            {
                return;
            }
            if (oldState != LightSync.LightSync.STATE_HELD)
            {
                return;
            }
            if (newState != LightSync.LightSync.STATE_PHYSICS)
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
            if (!card || !card.sync.IsHeld || !card.sync.IsOwner())
            {
                return;
            }
            card.sync.AddClassListener(this);
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
            card.sync.RemoveClassListener(this);
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
                if (listeners < 0)
                {
                    listeners = 0;
                }
            }

        }

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi prevOwner, VRCPlayerApi currentOwner)
        {

        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            mesh = GetComponent<MeshRenderer>();
        }
#endif
    }
}
