using UnityEngine;
using Stranogene.Games.Oltre.Spaceship;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// SpaceBodyImpactHazard
    /// Corpo "solido" / letale generico per un corpo celeste.
    ///
    /// Uso consigliato:
    /// - collider NON trigger sul corpo del body
    /// - questo script sullo stesso GameObject
    ///
    /// Effetto:
    /// - se la spaceship tocca il body, il pilota muore
    /// - opzionalmente azzera subito la velocità per evitare rimbalzi strani
    /// </summary>
    public class SpaceBodyImpactHazard : MonoBehaviour
    {
        [Header("Impact")] [Tooltip("Se true, qualsiasi contatto con la spaceship è letale.")] [SerializeField]
        private bool killOnContact = true;

        [Tooltip("Se > 0, uccide solo oltre questa velocità relativa d'impatto. 0 = qualsiasi contatto.")]
        [SerializeField]
        private float minimumImpactSpeed = 0f;

        [Tooltip("Azzera subito la velocità della spaceship al momento dell'impatto.")] [SerializeField]
        private bool stopShipOnImpact = true;

        [Header("Debug")] [SerializeField] private bool logImpact = true;

        /// <summary>
        /// Permette ai wrapper specifici (es. pianeta) di mantenere reason e log invariati.
        /// </summary>
        protected virtual string KillReason => "Space body impact";

        protected virtual string LogPrefix => "SpaceBodyImpactHazard";

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryHandleImpact(collision.collider, collision.relativeVelocity.magnitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Se qualcuno preferisce usare trigger anche sul corpo del body,
            // trattiamo comunque l'ingresso come impatto.
            TryHandleImpact(other, 0f);
        }

        private void TryHandleImpact(Collider2D other, float impactSpeed)
        {
            if (!killOnContact)
                return;

            var life = FindSpaceshipLife(other);
            if (life == null)
                return;

            if (!life.IsPilotAlive)
                return;

            if (minimumImpactSpeed > 0f && impactSpeed < minimumImpactSpeed)
                return;

            var rb = other.attachedRigidbody;
            if (stopShipOnImpact && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (logImpact)
            {
                var speedText = impactSpeed > 0f ? $" | impactSpeed={impactSpeed:F2}" : string.Empty;
                Debug.Log($"[{LogPrefix}] Impact detected on {other.name}{speedText}");
            }

            life.KillPilot(KillReason);
        }

        private static SpaceshipLife FindSpaceshipLife(Collider2D other)
        {
            if (other == null)
                return null;

            if (other.attachedRigidbody != null)
            {
                var lifeOnRb = other.attachedRigidbody.GetComponent<SpaceshipLife>();
                if (lifeOnRb != null)
                    return lifeOnRb;
            }

            var lifeOnCollider = other.GetComponent<SpaceshipLife>();
            if (lifeOnCollider != null)
                return lifeOnCollider;

            return other.GetComponentInParent<SpaceshipLife>();
        }
    }
}