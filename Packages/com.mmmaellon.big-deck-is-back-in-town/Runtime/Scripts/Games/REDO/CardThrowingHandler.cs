
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace MMMaellon.BigDeckIsBackInTown
{
    public class CardThrowingHandler : UdonSharpBehaviour
    {
        public Deck deck;
        public CardThrowTarget[] targets;

        float new_score = -1001f;
        float best_score = -1001f;
        CardThrowTarget best_spot;
        float dist;
        public virtual CardThrowTarget LocateBestCard(Vector3 position, Vector3 velocity, float threshold)
        {
            best_score = -1001f;
            best_spot = null;
            foreach (CardThrowTarget target in targets)
            {
                if (!target.allow_throwing)
                {
                    continue;
                }
                dist = Mathf.Pow(Vector3.Distance(target.GetAimPosition(), position), 0.3f);
                if (dist == 0)
                {
                    best_score = threshold;
                    best_spot = target;
                    break;
                }
                new_score = target.power * Vector3.Dot(velocity, (target.GetAimPosition() - transform.position).normalized) / dist;

                if (new_score > best_score)
                {
                    best_score = new_score;
                    best_spot = target;
                }
            }
            if (best_spot && best_score >= threshold)
            {
                return best_spot;
            }
            return null;
        }

        public virtual void OnThrowCard(int target_index, CardThrowing card)
        {

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
                List<CardThrowTarget> only_valid = new();
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i])
                    {
                        only_valid.Add(targets[i]);
                    }
                }
                targets = only_valid.ToArray();
            }
            else
            {
                targets = GetComponentsInChildren<CardThrowTarget>();
            }
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].id = i;
            }
        }
#endif
    }
}
