using UnityEngine;

namespace Stranogene.Games.Oltre.Spaceship
{
    /// <summary>
    /// SpaceshipMovement
    /// Patch 1 arcade:
    /// - Horizontal = rotazione
    /// - Vertical > 0 = thrust forward
    /// - Vertical < 0 = brake
    /// - inerzia semplice
    /// - rotazione con peso
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SpaceshipMovement : MonoBehaviour
    {
        [Header("Translation")] [Tooltip("Velocità massima assoluta in unità/secondo.")] [SerializeField]
        private float maxSpeed = 6f;

        [Tooltip("Spinta in avanti.")] [SerializeField]
        private float forwardThrust = 12f;

        [Tooltip("Forza di frenata applicata contro la velocità corrente.")] [SerializeField]
        private float brakeThrust = 18f;

        [Tooltip("Drag lineare base. Più basso = più inerzia.")] [SerializeField]
        private float linearDrag = 0.35f;

        [Tooltip("Smorzamento leggero della velocità laterale. Più alto = meno drift laterale.")] [SerializeField]
        private float lateralDamping = 2f;

        [Header("Rotation")] [Tooltip("Velocità angolare massima in gradi/sec.")] [SerializeField]
        private float maxTurnSpeed = 180f;

        [Tooltip("Quanto rapidamente la nave raggiunge la velocità di rotazione target.")] [SerializeField]
        private float turnAcceleration = 540f;

        [Tooltip("Smorzamento della rotazione quando non dai input.")] [SerializeField]
        private float turnDeceleration = 720f;

        [Tooltip("Direzione 'forward' dello sprite in locale. Se lo sprite guarda a destra, lascia (1,0).")]
        [SerializeField]
        private Vector2 spriteForwardLocal = Vector2.right;

        [SerializeField] private SpaceshipLife life;

        [Header("Energy Cost")] [Tooltip("Moltiplicatore del costo energetico quando ruoti la nave.")] [SerializeField]
        private float turnEnergyCostMultiplier = 0.5f;

        [Header("Thruster FX")] [SerializeField]
        private ParticleSystem mainThrustFx;

        [SerializeField] private ParticleSystem turnLeftFx;
        [SerializeField] private ParticleSystem turnRightFx;
        [SerializeField] private ParticleSystem brakeFx;

        private Rigidbody2D rb;

        // Cache input letti in Update
        private float turnInput;
        private float thrustInput;

        // Stato runtime
        private float currentTurnSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (life == null)
                life = GetComponent<SpaceshipLife>();

            if (life == null)
                Debug.LogWarning("SpaceshipMovement: SpaceshipLife mancante sullo stesso GameObject.");

            rb.gravityScale = 0f;
            rb.linearDamping = linearDrag;
            rb.angularDamping = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            PrepareThrusterFx(mainThrustFx);
            PrepareThrusterFx(turnLeftFx);
            PrepareThrusterFx(turnRightFx);
            PrepareThrusterFx(brakeFx);
        }

        private void OnValidate()
        {
            if (maxSpeed < 0f) maxSpeed = 0f;
            if (forwardThrust < 0f) forwardThrust = 0f;
            if (brakeThrust < 0f) brakeThrust = 0f;
            if (linearDrag < 0f) linearDrag = 0f;
            if (maxTurnSpeed < 0f) maxTurnSpeed = 0f;
            if (turnAcceleration < 0f) turnAcceleration = 0f;
            if (turnDeceleration < 0f) turnDeceleration = 0f;
            if (turnEnergyCostMultiplier < 0f) turnEnergyCostMultiplier = 0f;
            if (lateralDamping < 0f) lateralDamping = 0f;

            if (spriteForwardLocal.sqrMagnitude < 0.0001f)
                spriteForwardLocal = Vector2.right;

            spriteForwardLocal = spriteForwardLocal.normalized;
        }

        private void OnDisable()
        {
            StopAllThrusterFx();
        }

        private void Update()
        {
            turnInput = Input.GetAxisRaw("Horizontal");
            thrustInput = Input.GetAxisRaw("Vertical");

            UpdateThrusterFx();
        }

        private void FixedUpdate()
        {
            if (life && !life.CanMove)
            {
                rb.linearVelocity = Vector2.zero;
                currentTurnSpeed = 0f;
                return;
            }

            rb.linearDamping = linearDrag;

            HandleRotation(Time.fixedDeltaTime);
            HandleTranslation(Time.fixedDeltaTime);
            ApplyLateralDamping(Time.fixedDeltaTime);
            ClampSpeed();
        }

        private void HandleRotation(float dt)
        {
            var hasTurnInput = Mathf.Abs(turnInput) > 0.01f;
            var targetTurnSpeed = -turnInput * maxTurnSpeed;

            if (hasTurnInput)
            {
                currentTurnSpeed = Mathf.MoveTowards(
                    currentTurnSpeed,
                    targetTurnSpeed,
                    turnAcceleration * dt);

                // Ruotare costa energia
                if (life)
                    life.ConsumeEnergy(dt * turnEnergyCostMultiplier, rb.linearVelocity.magnitude);
            }
            else
            {
                currentTurnSpeed = Mathf.MoveTowards(
                    currentTurnSpeed,
                    0f,
                    turnDeceleration * dt);
            }

            if (Mathf.Abs(currentTurnSpeed) <= 0.001f) return;

            var deltaAngle = currentTurnSpeed * dt;
            transform.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z + deltaAngle);
        }


        private void HandleTranslation(float dt)
        {
            var worldForward = GetWorldForward();

            // Thrust in avanti
            if (thrustInput > 0.01f)
            {
                rb.AddForce(worldForward * (forwardThrust * thrustInput), ForceMode2D.Force);

                if (life)
                    life.ConsumeEnergy(dt, rb.linearVelocity.magnitude);

                return;
            }

            // Brake contro la velocità corrente
            if (!(thrustInput < -0.01f)) return;

            var velocity = rb.linearVelocity;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                rb.AddForce(-velocity.normalized * (brakeThrust * Mathf.Abs(thrustInput)), ForceMode2D.Force);
            }

            if (life)
                life.ConsumeEnergy(dt, rb.linearVelocity.magnitude);
        }

        private void ClampSpeed()
        {
            if (maxSpeed <= 0f) return;

            var speed = rb.linearVelocity.magnitude;
            if (speed <= maxSpeed) return;

            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        private Vector2 GetWorldForward()
        {
            return ((Vector2)(transform.rotation * spriteForwardLocal)).normalized;
        }

        private void ApplyLateralDamping(float dt)
        {
            if (lateralDamping <= 0f) return;

            var velocity = rb.linearVelocity;
            if (velocity.sqrMagnitude <= 0.0001f) return;

            var forward = GetWorldForward();
            var right = GetWorldRight();

            var forwardSpeed = Vector2.Dot(velocity, forward);
            var lateralSpeed = Vector2.Dot(velocity, right);

            // Smorzamento frame-rate independent: leggero ma continuo
            var t = 1f - Mathf.Exp(-lateralDamping * dt);
            var dampedLateralSpeed = Mathf.Lerp(lateralSpeed, 0f, t);

            rb.linearVelocity = (forward * forwardSpeed) + (right * dampedLateralSpeed);
        }

        private Vector2 GetWorldRight()
        {
            var localRight = new Vector2(-spriteForwardLocal.y, spriteForwardLocal.x).normalized;
            return ((Vector2)(transform.rotation * localRight)).normalized;
        }

        private void PrepareThrusterFx(ParticleSystem fx)
        {
            if (fx == null) return;

            var emission = fx.emission;
            emission.enabled = false;

            if (!fx.isPlaying)
                fx.Play();
        }

        private void UpdateThrusterFx()
        {
            if (!isActiveAndEnabled)
            {
                StopAllThrusterFx();
                return;
            }

            if (life != null && !life.CanMove)
            {
                StopAllThrusterFx();
                return;
            }

            var mainActive = thrustInput > 0.01f;
            var brakeActive = thrustInput < -0.01f;
            var turnLeftActive = turnInput < -0.01f;
            var turnRightActive = turnInput > 0.01f;

            SetThrusterEmission(mainThrustFx, mainActive);
            SetThrusterEmission(brakeFx, brakeActive);
            SetThrusterEmission(turnLeftFx, turnLeftActive);
            SetThrusterEmission(turnRightFx, turnRightActive);
        }

        private void StopAllThrusterFx()
        {
            SetThrusterEmission(mainThrustFx, false);
            SetThrusterEmission(brakeFx, false);
            SetThrusterEmission(turnLeftFx, false);
            SetThrusterEmission(turnRightFx, false);
        }

        private void SetThrusterEmission(ParticleSystem fx, bool enabled)
        {
            if (fx == null) return;

            var emission = fx.emission;
            emission.enabled = enabled;
        }
    }
}