using System.Collections.Generic;
using Stranogene.Games.Oltre.ScriptableObjects;
using UnityEngine;

namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// Pilot
    /// - Età cresce a interi (no decimali esposti)
    /// - Muore quando raggiunge/supera MaxAge
    /// - Profilo runtime generato a ogni run (ScriptableObject-driven)
    /// </summary>
    public class Pilot : MonoBehaviour
    {
        [Header("Runtime (Debug)")] [SerializeField]
        private int startAge;

        [SerializeField] private int age;
        [SerializeField] private int maxAge = 65;
        [SerializeField] private float energyConsumptionMultiplier = 1f;

        [SerializeField] private List<PilotTraitSO> traits = new();

        [SerializeField] private string displayName;
        [SerializeField] private string callsign;
        [SerializeField] private string title;

        public string DisplayName => displayName;
        public string Callsign => callsign;
        public string Title => title;

        public int StartAge => startAge;
        public int Age => age;
        public int MaxAge => maxAge;

        public float EnergyConsumptionMultiplier => energyConsumptionMultiplier;
        public IReadOnlyList<PilotTraitSO> Traits => traits;

        private float yearAccumulator;
        public bool IsAlive => age < maxAge;

        /// <summary>
        /// Applica un profilo runtime generato per la run.
        /// </summary>
        public void ApplyProfile(PilotRuntimeProfile profile)
        {
            yearAccumulator = 0f;

            startAge = profile.startAge;
            age = startAge;

            maxAge = profile.maxAge;
            if (maxAge <= age) maxAge = age + 1;

            energyConsumptionMultiplier = Mathf.Max(0.01f, profile.energyConsumptionMultiplier);

            traits.Clear();
            if (profile.traits != null)
                traits.AddRange(profile.traits);
        }

        /// <summary>
        /// Avanza il tempo espresso in "anni" (float) ma applica solo incrementi interi.
        /// Ritorna true se il pilota muore durante l'avanzamento.
        /// </summary>
        public bool AdvanceYears(float yearsToAdd)
        {
            if (!IsAlive) return true;
            if (yearsToAdd <= 0f) return false;

            yearAccumulator += yearsToAdd;

            var wholeYears = Mathf.FloorToInt(yearAccumulator);
            if (wholeYears <= 0) return false;

            yearAccumulator -= wholeYears;
            age += wholeYears;

            if (age < maxAge) return false;
            age = maxAge;
            return true;
        }

        public void ApplyNameProfile(PilotNameProfile nameProfile, object unused = null)
        {
            callsign = nameProfile.callsign;
            title = nameProfile.title;

            displayName = PilotNameGenerator.BuildDisplayName(nameProfile, includeCallsign: true, includeTitle: true);
        }
    }
}