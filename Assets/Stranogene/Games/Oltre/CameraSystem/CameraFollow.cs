using UnityEngine;

namespace Stranogene.Games.Oltre.CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private Transform target;

        [Header("Smoothing")] [Range(0.01f, 1f)] [SerializeField]
        private float smoothTime = 0.15f;

        private Vector3 velocity;
        private bool hasWarnedMissingTarget = false;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasWarnedMissingTarget = false;
            velocity = Vector3.zero;
        }

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