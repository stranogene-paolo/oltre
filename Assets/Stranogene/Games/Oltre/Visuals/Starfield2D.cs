using UnityEngine;

namespace Stranogene.Games.Oltre.Visuals
{
    /// <summary>
    /// Campo stellare semplice con puntini-sprite.
    /// Supporta target assegnato anche a runtime.
    /// </summary>
    public class Starfield2D : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private Transform target;
        [SerializeField] private bool autoFindTargetByTag = true;
        [SerializeField] private string targetTag = "Player";

        [Header("Stars")] [Min(1)] [SerializeField]
        private int starCount = 100;

        [SerializeField] private Sprite starSprite;
        [SerializeField] private int sortingOrder = -100;

        [Header("Field")] [SerializeField] private Vector2 fieldSize = new Vector2(80f, 80f);

        [Header("Motion")] [Range(0f, 1f)] [SerializeField]
        private float parallax = 0.15f;

        [Header("Scale")] [SerializeField] private Vector2 starScaleRange = new Vector2(0.03f, 0.10f);

        private Transform[] stars;
        private float[] starDepths;
        private Vector3 lastTargetPosition;
        private bool initialized;

        private Sprite runtimeSprite;
        private Texture2D runtimeTexture;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (target == null)
                return;

            if (!initialized)
            {
                InitializeStarfield();
            }
            else
            {
                RecenterStars();
                lastTargetPosition = target.position;
            }
        }

        private void Start()
        {
            TryResolveTarget();

            if (target != null)
                InitializeStarfield();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                TryResolveTarget();

                if (target != null && !initialized)
                    InitializeStarfield();

                return;
            }

            if (!initialized || stars == null || stars.Length == 0)
                return;

            Vector3 targetDelta = target.position - lastTargetPosition;

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;

                Vector3 pos = stars[i].position;
                pos -= targetDelta * (parallax * starDepths[i]);

                WrapAroundTarget(ref pos);
                stars[i].position = pos;
            }

            lastTargetPosition = target.position;
        }

        private void TryResolveTarget()
        {
            if (target != null) return;
            if (!autoFindTargetByTag) return;
            if (string.IsNullOrWhiteSpace(targetTag)) return;

            GameObject found = GameObject.FindGameObjectWithTag(targetTag);
            if (found != null)
                target = found.transform;
        }

        private void InitializeStarfield()
        {
            if (target == null || initialized) return;

            EnsureStarSprite();
            CreateStars();
            lastTargetPosition = target.position;
            initialized = true;
        }

        private void CreateStars()
        {
            stars = new Transform[starCount];
            starDepths = new float[starCount];

            Vector3 center = target.position;

            for (int i = 0; i < starCount; i++)
            {
                GameObject star = new GameObject("Star_" + i);
                star.transform.SetParent(transform, true);

                float x = Random.Range(-fieldSize.x * 0.5f, fieldSize.x * 0.5f);
                float y = Random.Range(-fieldSize.y * 0.5f, fieldSize.y * 0.5f);

                star.transform.position = new Vector3(center.x + x, center.y + y, 0f);

                float scale = Random.Range(starScaleRange.x, starScaleRange.y);
                star.transform.localScale = new Vector3(scale, scale, 1f);

                starDepths[i] = Random.Range(0.75f, 1.25f);

                SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = starSprite;
                sr.sortingOrder = sortingOrder;

                stars[i] = star.transform;
            }
        }

        private void RecenterStars()
        {
            if (target == null || stars == null) return;

            Vector3 center = target.position;

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;

                float x = Random.Range(-fieldSize.x * 0.5f, fieldSize.x * 0.5f);
                float y = Random.Range(-fieldSize.y * 0.5f, fieldSize.y * 0.5f);
                stars[i].position = new Vector3(center.x + x, center.y + y, 0f);
            }
        }

        private void WrapAroundTarget(ref Vector3 pos)
        {
            Vector3 center = target.position;

            float halfWidth = fieldSize.x * 0.5f;
            float halfHeight = fieldSize.y * 0.5f;

            if (pos.x < center.x - halfWidth) pos.x += fieldSize.x;
            else if (pos.x > center.x + halfWidth) pos.x -= fieldSize.x;

            if (pos.y < center.y - halfHeight) pos.y += fieldSize.y;
            else if (pos.y > center.y + halfHeight) pos.y -= fieldSize.y;

            pos.z = 0f;
        }

        private void EnsureStarSprite()
        {
            if (starSprite != null) return;

            runtimeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            runtimeTexture.SetPixel(0, 0, Color.white);
            runtimeTexture.Apply();

            runtimeSprite = Sprite.Create(
                runtimeTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );

            starSprite = runtimeSprite;
        }

        private void OnDestroy()
        {
            if (runtimeSprite != null)
                Destroy(runtimeSprite);

            if (runtimeTexture != null)
                Destroy(runtimeTexture);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(target.position, new Vector3(fieldSize.x, fieldSize.y, 0f));
        }
#endif
    }
}