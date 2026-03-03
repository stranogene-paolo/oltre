using UnityEngine;

namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// Pilot
    /// Gestisce l'età del pilota come valore intero che cresce nel tempo.
    /// - Parte da un'età coerente (random in range)
    /// - Cresce solo a interi (no decimali esposti)
    /// - Muore quando raggiunge/supera MaxAge
    /// </summary>
    public class Pilot : MonoBehaviour
    {
        [Header("Age Setup (integers only)")] [Tooltip("Età minima coerente per un pilota.")] [SerializeField]
        private int minStartAge = 25;

        [Tooltip("Età massima coerente per un pilota (età di partenza).")] [SerializeField]
        private int maxStartAge = 45;

        [Tooltip("Età massima raggiunta la quale il pilota muore.")] [SerializeField]
        private int maxAge = 65;

        public int StartAge { get; private set; }
        public int Age { get; private set; }
        public int MaxAge => maxAge;

        // Accumula frazioni di "anni" finché non diventano 1 anno intero
        private float yearAccumulator;

        public bool IsAlive => Age < maxAge;

        private void OnValidate()
        {
            if (minStartAge < 0) minStartAge = 0;
            if (maxStartAge < minStartAge) maxStartAge = minStartAge;
            if (maxAge < maxStartAge) maxAge = maxStartAge; // maxAge deve essere >= possibile startAge
        }

        /// <summary>Genera/inizializza un nuovo pilota (nuova run).</summary>
        public void ResetPilot()
        {
            yearAccumulator = 0f;

            // Età iniziale "coerente": random intero nel range
            StartAge = Random.Range(minStartAge, maxStartAge + 1);
            Age = StartAge;

            // Safety: se per qualche motivo StartAge è già >= maxAge, rendiamo maxAge coerente
            if (maxAge <= Age) maxAge = Age + 1;
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

            // Applica solo anni interi
            int wholeYears = Mathf.FloorToInt(yearAccumulator);
            if (wholeYears <= 0) return false;

            yearAccumulator -= wholeYears;
            Age += wholeYears;

            // Clamp e check morte
            if (Age >= maxAge)
            {
                Age = maxAge;
                return true;
            }

            return false;
        }
    }
}