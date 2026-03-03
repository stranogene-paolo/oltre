using UnityEngine;

namespace Stranogene.Games.Oltre.Spaceship
{
    /// <summary>
    /// SpaceshipLife
    /// Gestisce:
    /// - Lifetime pilota (in "anni" astratti, convertiti da tempo reale)
    /// - Energia (consumata quando la nave si muove)
    /// Espone:
    /// - CanMove
    /// - ConsumeEnergy(...)
    /// - KillPilot(...)
    /// </summary>
    public class SpaceshipLife : MonoBehaviour
    {
        [Header("Pilot Lifetime")]
        [Tooltip("Anni massimi del pilota (unità di gameplay).")]
        [SerializeField] private float pilotMaxYears = 10f;

        [Tooltip("Quanti 'anni' passano per ogni secondo reale. (Esempio: 1 anno ogni 5 sec => 0.2)")]
        [SerializeField] private float yearsPerSecond = 0.2f;

        [Header("Energy")]
        [Tooltip("Energia massima.")]
        [SerializeField] private float maxEnergy = 100f;

        [Tooltip("Consumo energia al secondo quando stai dando input (baseline).")]
        [SerializeField] private float energyDrainPerSecond = 5f;

        [Tooltip("Consumo extra proporzionale alla velocità (opzionale).")]
        [SerializeField] private float energyDrainPerSpeedPerSecond = 0f;

        public float PilotYearsLeft { get; private set; }
        public float Energy { get; private set; }

        public bool IsPilotAlive { get; private set; } = true;
        public bool HasEnergy => Energy > 0f;

        public bool CanMove => IsPilotAlive && HasEnergy;

        private bool hasTriggeredStop;

        private void Awake()
        {
            ResetRun();
        }

        /// <summary>Resetta valori per una nuova run/pilota.</summary>
        public void ResetRun()
        {
            PilotYearsLeft = pilotMaxYears;
            Energy = maxEnergy;
            IsPilotAlive = true;
            hasTriggeredStop = false;
        }

        private void Update()
        {
            if (!IsPilotAlive) return;

            // Countdown lifetime pilota
            PilotYearsLeft -= yearsPerSecond * Time.deltaTime;

            if (PilotYearsLeft <= 0f)
            {
                PilotYearsLeft = 0f;
                KillPilot("Lifetime finished");
            }
        }

        /// <summary>
        /// Chiamata dal movement quando c'è input.
        /// dt deve essere Time.fixedDeltaTime (o un dt coerente).
        /// </summary>
        public void ConsumeEnergy(float dt, float currentSpeed)
        {
            if (!IsPilotAlive || Energy <= 0f) return;

            float drain = energyDrainPerSecond;

            if (energyDrainPerSpeedPerSecond > 0f)
                drain += currentSpeed * energyDrainPerSpeedPerSecond;

            Energy -= drain * dt;

            if (Energy <= 0f)
            {
                Energy = 0f;
                TriggerStopOnce("Energy depleted");
            }
        }

        public void KillPilot(string reason)
        {
            if (!IsPilotAlive) return;
            IsPilotAlive = false;
            TriggerStopOnce(reason);
        }

        private void TriggerStopOnce(string reason)
        {
            if (hasTriggeredStop) return;
            hasTriggeredStop = true;

            // Non facciamo altro qui: lo stop fisico lo fa il movement (così restano separati).
            Debug.Log($"[SpaceshipLife] STOP: {reason}");
        }
    }
}