using UnityEngine;
using Stranogene.Games.Oltre.Pilot;
using Stranogene.Games.Oltre.ScriptableObjects;

namespace Stranogene.Games.Oltre.Spaceship
{
    /// <summary>
    /// SpaceshipLife
    /// Gestisce:
    /// - Età pilota (in anni interi, cresce nel tempo)
    /// - Energia (consumata quando la nave si muove)
    /// Espone:
    /// - CanMove
    /// - ConsumeEnergy(...)
    /// - KillPilot(...)
    /// </summary>
    public class SpaceshipLife : MonoBehaviour
    {
        public enum StopReason
        {
            None = 0,
            EnergyDepleted = 1,
            PilotDead = 2
        }

        [Header("Pilot Age")]
        [Tooltip("Quanti 'anni' passano per ogni secondo reale. (Esempio: 1 anno ogni 5 sec => 0.2)")]
        [SerializeField]
        private float yearsPerSecond = 0.2f;

        [Tooltip("Riferimento al Pilot. Se nullo prova a prenderlo dallo stesso GameObject.")] [SerializeField]
        private Pilot.Pilot pilot;

        [Header("Pilot Generation (ScriptableObject)")]
        [Tooltip("Pool di tratti e range età per generare il pilota a ogni run.")]
        [SerializeField]
        private PilotTraitPoolSO pilotTraitPool;

        [Header("Energy")] [Tooltip("Energia massima.")] [SerializeField]
        private float maxEnergy = 100f;

        [Tooltip("Consumo energia al secondo quando stai dando input (baseline).")] [SerializeField]
        private float energyDrainPerSecond = 5f;

        [Tooltip("Consumo extra proporzionale alla velocità (opzionale).")] [SerializeField]
        private float energyDrainPerSpeedPerSecond = 0f;


        private float energyDrainMultiplier = 1f;
        private bool hasTriggeredStop;

        public int PilotAge => pilot ? pilot.Age : 0;
        public int PilotMaxAge => pilot ? pilot.MaxAge : 0;

        public float Energy { get; private set; }

        public bool IsPilotAlive { get; private set; } = true;
        public bool HasEnergy => Energy > 0f;
        public bool CanMove => IsPilotAlive && HasEnergy;

        public StopReason CurrentStopReason { get; private set; } = StopReason.None;

        private void Awake()
        {
            if (pilot == null)
                pilot = GetComponent<Pilot.Pilot>();
        }

        /// <summary>
        /// Resetta valori per una nuova run/pilota.
        /// </summary>
        public void ResetRun()
        {
            CurrentStopReason = StopReason.None;
            hasTriggeredStop = false;
            Energy = maxEnergy;
            IsPilotAlive = true;
            energyDrainMultiplier = 1f;

            if (pilot == null)
            {
                Debug.LogWarning("SpaceshipLife: Pilot mancante sullo stesso GameObject (aggiungi Pilot.cs).");
                return;
            }

            var profile = PilotGenerator.Generate(pilotTraitPool);
            pilot.ApplyProfile(profile);

            var seed = (int)(Time.realtimeSinceStartup * 1000f) ^ GetInstanceID();
            var nameProfile = PilotNameGenerator.Generate(seed);
            pilot.ApplyNameProfile(nameProfile, null);

            energyDrainMultiplier = Mathf.Max(0.01f, pilot.EnergyConsumptionMultiplier);

            Debug.Log(
                $"[SpaceshipLife] New Pilot: name={pilot.DisplayName} startAge={pilot.StartAge} maxAge={pilot.MaxAge} energyMul={energyDrainMultiplier} traits={pilot.Traits.Count}");
        }

        private void Update()
        {
            if (!IsPilotAlive) return;
            if (!pilot) return;

            var yearsToAdd = yearsPerSecond * Time.deltaTime;
            var died = pilot.AdvanceYears(yearsToAdd);

            if (died)
                KillPilot("Pilot reached max age");
        }

        /// <summary>
        /// Chiamata dal movement quando c'è input.
        /// dt deve essere Time.fixedDeltaTime (o un dt coerente).
        /// </summary>
        public void ConsumeEnergy(float dt, float currentSpeed)
        {
            if (!IsPilotAlive || Energy <= 0f) return;

            var drain = energyDrainPerSecond;

            if (energyDrainPerSpeedPerSecond > 0f)
                drain += currentSpeed * energyDrainPerSpeedPerSecond;

            drain *= energyDrainMultiplier;

            Energy -= drain * dt;

            if (Energy > 0f) return;

            Energy = 0f;
            TriggerStopOnce(StopReason.EnergyDepleted, "Energy depleted");
        }

        public void KillPilot(string reason)
        {
            if (!IsPilotAlive) return;

            IsPilotAlive = false;
            TriggerStopOnce(StopReason.PilotDead, reason);
        }

        private void TriggerStopOnce(StopReason stopReason, string reason)
        {
            if (hasTriggeredStop) return;

            hasTriggeredStop = true;
            CurrentStopReason = stopReason;

            Debug.Log($"[SpaceshipLife] STOP: {reason}");
        }
    }
}