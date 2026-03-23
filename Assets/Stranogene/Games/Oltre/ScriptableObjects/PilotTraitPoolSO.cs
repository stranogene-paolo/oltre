using System.Collections.Generic;
using UnityEngine;

namespace Stranogene.Games.Oltre.ScriptableObjects
{
    /// <summary>
    /// PilotTraitPoolSO
    /// Pool di tratti pescabili + regole di generazione.
    /// </summary>
    [CreateAssetMenu(fileName = "PilotTraitPool", menuName = "Stranogene/Oltre/Pilot/Trait Pool", order = 1)]
    public class PilotTraitPoolSO : ScriptableObject
    {
        [Header("Pilot Base Age")] public int minStartAge = 25;
        public int maxStartAge = 45;

        [Header("Pilot Max Age (base)")] public int minMaxAge = 55;
        public int maxMaxAge = 75;

        [Header("Traits Generation")] [Tooltip("Numero minimo di tratti da assegnare al pilota.")]
        public int minTraits = 1;

        [Tooltip("Numero massimo di tratti da assegnare al pilota.")]
        public int maxTraits = 3;

        [Tooltip("Se true, non pesca due volte lo stesso trait.")]
        public bool uniqueTraits = true;

        [Tooltip("Lista di tratti pescabili.")]
        public List<PilotTraitSO> traits = new();

        [Header("MaxAge Final Clamp")] public int finalMinMaxAge = 40;
        public int finalMaxMaxAge = 90;

        private void OnValidate()
        {
            if (minStartAge < 0) minStartAge = 0;
            if (maxStartAge < minStartAge) maxStartAge = minStartAge;

            if (minMaxAge < 0) minMaxAge = 0;
            if (maxMaxAge < minMaxAge) maxMaxAge = minMaxAge;

            if (minTraits < 0) minTraits = 0;
            if (maxTraits < minTraits) maxTraits = minTraits;
        }
    }
}