using UnityEngine;

namespace Stranogene.Games.Oltre.CameraSystem
{
    /// <summary>
    /// CameraFollow
    /// Camera ortografica con smoothing (inerzia leggera).
    /// - Segue solo X/Y
    /// - Mantiene Z originale
    /// - Check presenza target
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Smoothing")]
        [Tooltip("Tempo di smoothing. Più basso = più reattiva.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 velocity;
        private bool hasWarnedMissingTarget = false;

        private void LateUpdate()
        {
            if (!target)
            {
                if (hasWarnedMissingTarget) return;
                Debug.LogWarning("CameraFollow2D: Target non assegnato.");
                hasWarnedMissingTarget = true;
                return;
            }

            var currentPos = transform.position;
            var targetPos = target.position;

            // Manteniamo la Z originale della camera
            targetPos.z = currentPos.z;

            transform.position = Vector3.SmoothDamp(
                currentPos,
                targetPos,
                ref velocity,
                smoothTime
            );
        }
    }
}