using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stranogene.Games.Oltre.Spaceship;
using Stranogene.Games.Oltre.CameraSystem;

namespace Stranogene.Games.Oltre.Run
{
    /// <summary>
    /// RunManager (OLTRE)
    /// Responsabile di:
    /// - Start nuova run (nuova spaceship + nuovo pilota)
    /// - Fine run quando il pilota muore o energia finisce (CanMove diventa false)
    /// - Rendere persistente la vecchia spaceship come derelitto incontrabile
    ///
    /// MVP assumption:
    /// - Una scena singola o più scene: RunManager è DontDestroyOnLoad.
    /// - Il "partial reset" è: nuova spaceship + vecchie ship lasciate in scena.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Boot")] [Tooltip("Se true, avvia una run automaticamente in Play.")] [SerializeField]
        private bool autoStartOnPlay = true;

        [Tooltip("Se true, quando finisce la run ne avvia un'altra automaticamente.")] [SerializeField]
        private bool autoRestartOnRunEnd = false;

        [Tooltip("Delay (secondi) prima di auto-restart (se autoRestartOnRunEnd = true).")] [SerializeField]
        private float autoRestartDelay = 1.0f;

        [Header("Spaceship Spawn")]
        [Tooltip("Prefab della spaceship 'giocabile' (deve avere SpaceshipLife + Pilot sullo stesso GameObject).")]
        [SerializeField]
        private GameObject spaceshipPrefab;

        [Tooltip("Se assegnato, la spaceship spawna qui. Altrimenti (0,0,0).")] [SerializeField]
        private Transform spawnPoint;

        [Tooltip(
            "Se true, distrugge la spaceship corrente quando parte una nuova run (NON consigliato: vogliamo persistenza).")]
        [SerializeField]
        private bool destroyCurrentShipOnNewRun = false;

        [Header("Derelicts (Persistent old ships)")]
        [Tooltip("Se true, la vecchia spaceship resta in scena come derelitto incontrabile.")]
        [SerializeField]
        private bool keepDerelictsInScene = true;

        [Tooltip("Massimo numero di derelitti mantenuti in scena (0 = infinito).")] [SerializeField]
        private int maxDerelictsInScene = 20;

        [Tooltip("Tag opzionale da applicare ai derelitti (utile per detection/loot). Lascia vuoto per non cambiare.")]
        [SerializeField]
        private string derelictTag = "DerelictShip";

        [Tooltip("Layer opzionale per derelitti (0 = non cambiare).")] [SerializeField]
        private int derelictLayer = 0;

        [Header("Derelict Freeze")]
        [Tooltip("Se true, prova a rendere Kinematic il Rigidbody per congelare il derelitto.")]
        [SerializeField]
        private bool freezeDerelictRigidbody = true;

        [Tooltip("Se true, disabilita SpaceshipLife sul derelitto (così non consuma / non invecchia).")]
        [SerializeField]
        private bool disableLifeOnDerelict = true;

        [Tooltip(
            "Lista di componenti (MonoBehaviour) da disabilitare quando una ship diventa derelitto (movement, input, camera hook, ecc).")]
        [SerializeField]
        private List<MonoBehaviour> extraBehavioursToDisableOnDerelict = new();

        [Header("Camera Follow Fix")]
        [Tooltip("Tag che la camera usa per identificare la ship player")]
        [SerializeField]
        private string playerShipTag = "Player";

        [Tooltip("Se true, dopo lo spawn prova a ricollegare automaticamente il target della MainCamera.")]
        [SerializeField]
        private bool rebindMainCameraOnNewRun = true;


        public int CurrentRunIndex { get; private set; } = 0;
        public bool IsRunActive { get; private set; } = false;

        public GameObject CurrentSpaceship { get; private set; }
        public SpaceshipLife CurrentLife { get; private set; }

        private readonly List<GameObject> derelictsInScene = new();

        private bool wasAbleToMoveLastFrame = true;

        public event Action<int> OnRunStarted;
        public event Action<int, string> OnRunEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            if (autoStartOnPlay)
                StartNewRun();
        }

        private void Update()
        {
            if (!IsRunActive) return;
            if (CurrentLife == null) return;

            // La run finisce quando non puoi più muovere: o pilota morto o energia finita
            var canMove = CurrentLife.CanMove;

            // Trigger SOLO quando passa da true -> false
            if (wasAbleToMoveLastFrame && !canMove)
            {
                var reason = ResolveRunEndReason(CurrentLife);
                EndCurrentRun(reason);

                if (autoRestartOnRunEnd)
                    Invoke(nameof(StartNewRun), Mathf.Max(0f, autoRestartDelay));
            }

            wasAbleToMoveLastFrame = canMove;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // MVP: non respawniamo derelitti (restano se la scena non viene ricaricata).
            // In futuro: qui puoi ricreare derelitti da snapshot.
        }

        /// <summary>
        /// Avvia una nuova run:
        /// - Se c'è una ship corrente: o la trasformi in derelitto (persistente) oppure la distruggi (opzione)
        /// - Spawni una nuova ship e resetti la run (Pilot random + energia)
        /// </summary>
        public void StartNewRun()
        {
            if (spaceshipPrefab == null)
            {
                Debug.LogError("[RunManager] spaceshipPrefab non assegnato.");
                return;
            }

            if (IsRunActive)
            {
                EndCurrentRun("Forced new run");
            }

            if (CurrentSpaceship != null)
            {
                if (destroyCurrentShipOnNewRun)
                {
                    Destroy(CurrentSpaceship);
                }
                else if (keepDerelictsInScene)
                {
                    MakeCurrentShipDerelict();
                }
            }

            var pos = spawnPoint ? spawnPoint.position : Vector3.zero;
            var rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

            CurrentSpaceship = Instantiate(spaceshipPrefab, pos, rot);
            CurrentSpaceship.name = $"Spaceship_Run_{CurrentRunIndex + 1}";

            RebindCameraFollow(CurrentSpaceship.transform);

            CurrentLife = CurrentSpaceship.GetComponent<SpaceshipLife>();
            if (CurrentLife == null)
            {
                Debug.LogError(
                    "[RunManager] La spaceshipPrefab non ha SpaceshipLife. Aggiungilo al root della prefab.");
            }
            else
            {
                // ResetRun già genera un nuovo Pilot random e resetta energia
                CurrentLife.ResetRun();
            }

            CurrentRunIndex++;
            IsRunActive = true;

            wasAbleToMoveLastFrame = (CurrentLife != null) && CurrentLife.CanMove;

            Debug.Log($"[RunManager] RUN START #{CurrentRunIndex}");
            OnRunStarted?.Invoke(CurrentRunIndex);
        }

        /// <summary>
        /// Termina la run corrente.
        /// Nota: NON distrugge la spaceship (persistenza). La conversione in derelitto avviene su StartNewRun.
        /// </summary>
        public void EndCurrentRun(string reason)
        {
            if (!IsRunActive) return;

            IsRunActive = false;

            Debug.Log($"[RunManager] RUN END #{CurrentRunIndex} - {reason}");
            OnRunEnded?.Invoke(CurrentRunIndex, reason);
        }

        private void MakeCurrentShipDerelict()
        {
            if (CurrentSpaceship == null) return;

            var ship = CurrentSpaceship;

            // Mark component (utile per debug / future logic)
            var marker = ship.GetComponent<DerelictMarker>();
            if (marker == null) marker = ship.AddComponent<DerelictMarker>();
            marker.RunIndex = CurrentRunIndex;
            marker.Timestamp = DateTime.UtcNow.ToString("o");

            // Tag / Layer (opzionali)
            if (!string.IsNullOrEmpty(derelictTag))
                ship.tag = derelictTag;

            if (derelictLayer != 0)
                SetLayerRecursively(ship, derelictLayer);

            // Disabilita life (così non consuma / non invecchia più)
            if (disableLifeOnDerelict && CurrentLife != null)
                CurrentLife.enabled = false;

            // Disabilita movement sulla ship che sta diventando derelitto
            var movements = ship.GetComponentsInChildren<SpaceshipMovement>(true);
            foreach (var movement in movements)
            {
                if (movement != null)
                    movement.enabled = false;
            }

            // Congela fisica 2D della ship
            if (freezeDerelictRigidbody)
            {
                var rb2D = ship.GetComponent<Rigidbody2D>();
                if (rb2D != null)
                {
                    rb2D.linearVelocity = Vector2.zero;
                    rb2D.angularVelocity = 0f;
                    rb2D.bodyType = RigidbodyType2D.Kinematic;
                }
                else
                {
                    // fallback difensivo nel caso in futuro si usi una fisica 3D
                    var rb = ship.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                    }
                }
            }

            // Disabilita eventuali extra behaviour assegnati manualmente
            foreach (var b in extraBehavioursToDisableOnDerelict)
            {
                if (b != null) b.enabled = false;
            }

            // Track
            derelictsInScene.Add(ship);

            // Enforce cap
            if (maxDerelictsInScene > 0 && derelictsInScene.Count > maxDerelictsInScene)
            {
                var toRemove = derelictsInScene.Count - maxDerelictsInScene;
                for (var i = 0; i < toRemove; i++)
                {
                    var go = derelictsInScene[0];
                    derelictsInScene.RemoveAt(0);
                    if (go != null) Destroy(go);
                }
            }

            // Stacca riferimenti run current
            CurrentSpaceship = null;
            CurrentLife = null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform t in root.transform)
                SetLayerRecursively(t.gameObject, layer);
        }

        private static void RebindCameraFollow(Transform ship)
        {
            if (ship == null) return;

            // Se in futuro vuoi un child dedicato tipo "CameraTarget", lo supportiamo gratis.
            var target = ship.Find("CameraTarget");
            if (target == null) target = ship;

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[RunManager] Camera.main non trovata (manca tag MainCamera?).");
                return;
            }

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
            {
                Debug.LogWarning("[RunManager] CameraFollow non trovato sulla MainCamera.");
                return;
            }

            follow.SetTarget(target);
        }

        private static string ResolveRunEndReason(SpaceshipLife life)
        {
            if (life == null)
                return "Unknown";

            return life.CurrentStopReason switch
            {
                SpaceshipLife.StopReason.EnergyDepleted => "Energy depleted",
                SpaceshipLife.StopReason.PilotDead => "Pilot dead",
                _ => life.IsPilotAlive ? "Movement unavailable" : "Pilot dead"
            };
        }
    }
}