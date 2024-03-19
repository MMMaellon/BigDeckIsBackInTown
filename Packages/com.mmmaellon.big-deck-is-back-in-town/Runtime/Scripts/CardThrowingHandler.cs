
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif

namespace MMMaellon.BigDeckIsBackInTown
{
    public class CardThrowingHandler : UdonSharpBehaviour
    {
        public Deck deck;
        public CardThrowTarget[] targets;
        public float desktop_throw_boost = 5f;
        public bool allow_throwing = true;
        public bool first_throw_only = true;
        public bool prime_throw_on_pickup_use_down = true;
        public bool desktop_throw_assist = true;
        float new_score = -1001f;
        float best_score = -1001f;
        CardThrowTarget best_spot;
        float dist;
        public virtual CardThrowTarget LocateBestTarget(Vector3 position, Vector3 velocity)
        {
            best_score = -1001f;
            best_spot = null;
            foreach (CardThrowTarget target in targets)
            {
                if (!target.allow_throwing)
                {
                    //don't skip velocity here so we can have the thing where it forces it by setting distance to 0
                    continue;
                }
                dist = Mathf.Pow(Vector3.Distance(target.GetAimPosition(position, velocity), position), 0.3f);
                if (dist == 0)
                {
                    best_score = 1001;
                    best_spot = target;
                    break;
                }
                new_score = target.power_multiplier * Vector3.Dot(velocity, (target.GetAimPosition(position, velocity) - position).normalized) / dist;

                if (new_score < target.power_threshold || velocity.magnitude < target.velocity_threshold)
                {
                    continue;
                }
                if (new_score > best_score)
                {
                    best_score = new_score;
                    best_spot = target;
                }
            }
            return best_spot;
        }

        public virtual void OnThrowCard(int target_index, CardThrowing card)
        {

        }

        [System.NonSerialized]
        public int last_right_click = 0;//don't make it negative
        public override void InputDrop(bool value, UdonInputEventArgs args)
        {
            if (value)
            {
                last_right_click = -1001;
            }
            else
            {
                last_right_click = Time.frameCount;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public virtual void OnValidate()
        {
            if (!deck)
            {
                deck = GetComponentInChildren<Deck>();
                if (deck)
                {
                    deck.throwing_handler = this;
                }
            }
            if (targets.Length > 0)
            {
                HashSet<CardThrowTarget> only_valid = new();
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i])
                    {
                        only_valid.Add(targets[i]);
                    }
                }
                targets = only_valid.ToArray<CardThrowTarget>();
            }
            else
            {
                targets = GetComponentsInChildren<CardThrowTarget>();
            }
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].id = i;
                targets[i].deck = deck;
            }
        }
#endif
    }
}
