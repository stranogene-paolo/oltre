using UnityEngine;

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
        [Header("Pilot Age")]
        [Tooltip("Quanti 'anni' passano per ogni secondo reale. (Esempio: 1 anno ogni 5 sec => 0.2)")]
        [SerializeField]
        private float yearsPerSecond = 0.2f;

        [Tooltip("Riferimento al Pilot. Se nullo prova a prenderlo dallo stesso GameObject.")] [SerializeField]
        private Pilot.Pilot pilot;

        [Header("Energy")] [Tooltip("Energia massima.")] [SerializeField]
        private float maxEnergy = 100f;

        [Tooltip("Consumo energia al secondo quando stai dando input (baseline).")] [SerializeField]
        private float energyDrainPerSecond = 5f;

        [Tooltip("Consumo extra proporzionale alla velocità (opzionale).")] [SerializeField]
        private float energyDrainPerSpeedPerSecond = 0f;

        // UI-friendly: età intera (solo crescita)
        public int PilotAge => pilot ? pilot.Age : 0;
        public int PilotMaxAge => pilot ? pilot.MaxAge : 0;

        public float Energy { get; private set; }

        public bool IsPilotAlive { get; private set; } = true;
        public bool HasEnergy => Energy > 0f;

        public bool CanMove => IsPilotAlive && HasEnergy;

        private bool hasTriggeredStop;

        private void Awake()
        {
            if (pilot == null) pilot = GetComponent<Pilot.Pilot>();
            ResetRun();
        }

        /// <summary>Resetta valori per una nuova run/pilota.</summary>
        public void ResetRun()
        {
            // Pilot
            if (pilot != null)
            {
                pilot.ResetPilot();
                IsPilotAlive = true;
            }
            else
            {
                // Se manca Pilot, consideriamo comunque vivo per non bloccare il gioco,
                // ma logghiamo il warning una volta.
                IsPilotAlive = true;
                Debug.LogWarning("SpaceshipLife: Pilot mancante sullo stesso GameObject (aggiungi Pilot.cs).");
            }

            // Energy
            Energy = maxEnergy;

            hasTriggeredStop = false;
        }

        private void Update()
        {
            if (!IsPilotAlive) return;
            if (pilot == null) return;

            // Età cresce (solo interi) in base al tempo reale
            float yearsToAdd = yearsPerSecond * Time.deltaTime;

            bool died = pilot.AdvanceYears(yearsToAdd);
            if (died)
            {
                KillPilot("Pilot reached max age");
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