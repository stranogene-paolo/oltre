using UnityEngine;

namespace Stranogene.Games.Oltre.Spaceship
{
    /// <summary>
    /// SpaceshipMovement
    /// Movimento top-down 2D con Rigidbody2D:
    /// - input in Update (cache)
    /// - fisica in FixedUpdate (AddForce)
    /// - clamp velocità massima
    /// - rotazione verso la direzione di movimento
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SpaceshipMovement : MonoBehaviour
    {
        [Header("Movement")] [Tooltip("Velocità massima in unità/secondo.")] [SerializeField]
        private float maxSpeed = 1f;

        [Tooltip("Accelerazione (forza) applicata al Rigidbody2D. Valori tipici: 5-50 in base alla massa/drag.")]
        [SerializeField]
        private float acceleration = 15f;

        [Tooltip("Drag (inerzia). Più basso = più scivola.")] [SerializeField]
        private float linearDrag = 1.5f;

        [Header("Rotation")] [Tooltip("Se true, ruota lo sprite verso la direzione di movimento.")] [SerializeField]
        private bool rotateToMovement = true;

        [Tooltip("Soglia sotto cui non aggiorna la rotazione (evita jitter da micro-velocità).")] [SerializeField]
        private float rotateMinSpeed = 0.05f;

        [Tooltip("Direzione 'forward' dello sprite in locale. Se il tuo sprite guarda a destra, lascia (1,0).")]
        [SerializeField]
        private Vector2 spriteForwardLocal = Vector2.right;

        [SerializeField] private SpaceshipLife life;

        private Rigidbody2D rb;
        private Vector2 moveInput; // cache input (Update)

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (life == null) life = GetComponent<SpaceshipLife>();
            if (life == null) Debug.LogWarning("SpaceshipMovement: SpaceshipLife mancante sullo stesso GameObject.");

            // Setup base coerente per top-down “Lovers-like”
            rb.gravityScale = 0f;
            rb.linearDamping = linearDrag;
            rb.angularDamping = 0f;
            rb.freezeRotation = true; // gestiamo noi la rotazione via script
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnValidate()
        {
            if (maxSpeed < 0f) maxSpeed = 0f;
            if (acceleration < 0f) acceleration = 0f;
            if (linearDrag < 0f) linearDrag = 0f;
            if (spriteForwardLocal.sqrMagnitude < 0.0001f) spriteForwardLocal = Vector2.right;
            spriteForwardLocal = spriteForwardLocal.normalized;
        }

        private void Update()
        {
            // Frecce direzionali / WASD (Input Manager classico).
            // Se stai usando il New Input System, lo adattiamo dopo: qui teniamo MVP.
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");

            var v = new Vector2(x, y);

            // Normalizza per evitare diagonali più veloci.
            moveInput = (v.sqrMagnitude > 1f) ? v.normalized : v;
        }

        private void FixedUpdate()
        {
            if (life && !life.CanMove)
            {
                // stop fisico e blocca movimento
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Mantieni drag in sync con inspector (utile mentre tuniamo).
            rb.linearDamping = linearDrag;

            // 1) Forza di accelerazione
            if (moveInput.sqrMagnitude is > 0f and > 0f)
            {
                rb.AddForce(moveInput * acceleration, ForceMode2D.Force);

                if (life)
                    life.ConsumeEnergy(Time.fixedDeltaTime, rb.linearVelocity.magnitude);
            }

            // 2) Clamp velocità massima
            var vel = rb.linearVelocity;
            var speed = vel.magnitude;

            if (speed > maxSpeed && maxSpeed > 0f)
            {
                rb.linearVelocity = vel * (maxSpeed / speed);
                vel = rb.linearVelocity;
                speed = vel.magnitude;
            }

            // 3) Rotazione verso movimento
            if (!rotateToMovement || !(speed >= rotateMinSpeed)) return;
            var dir = vel / speed;

            // Calcola l’angolo in gradi per ruotare lo "spriteForwardLocal" verso "dir"
            var angle = Vector2.SignedAngle(spriteForwardLocal, dir);

            // Ruota attorno a Z
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}