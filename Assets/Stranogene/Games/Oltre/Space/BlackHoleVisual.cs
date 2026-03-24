using UnityEngine;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// BlackHoleVisual
    /// Solo visuale. Non modifica la fisica o i collider del black hole.
    ///
    /// Effetti:
    /// - pulsazione del ring gravitazionale (LineRenderer)
    /// - rotazione/pulsazione del core visivo
    /// - rotazione/pulsazione dell'halo visivo
    /// - piccolo flicker su alpha / width
    ///
    /// IMPORTANTE:
    /// - coreVisualRoot e haloVisualRoot dovrebbero puntare a child SOLO visuali
    /// - non assegnare qui i transform che portano collider gameplay, altrimenti la scala
    ///   andrebbe a modificare anche il raggio del collider
    /// </summary>
    [DisallowMultipleComponent]
    public class BlackHoleVisual : MonoBehaviour
    {
        [Header("References")] [Tooltip("LineRenderer del campo gravitazionale del black hole.")] [SerializeField]
        private LineRenderer gravityRing;

        [Tooltip("Child SOLO visuale del core del black hole.")] [SerializeField]
        private Transform coreVisualRoot;

        [Tooltip("SpriteRenderer opzionale del core.")] [SerializeField]
        private SpriteRenderer coreSprite;

        [Tooltip("Child SOLO visuale dell'alone esterno.")] [SerializeField]
        private Transform haloVisualRoot;

        [Tooltip("SpriteRenderer opzionale dell'alone.")] [SerializeField]
        private SpriteRenderer haloSprite;

        [Header("Ring Pulse")] [SerializeField]
        private bool pulseRing = true;

        [SerializeField] [Min(0f)] private float ringWidthPulseAmplitude = 0.22f;

        [SerializeField] [Min(0f)] private float ringPulseSpeed = 1.2f;

        [SerializeField] [Range(0f, 1f)] private float ringAlphaPulseAmplitude = 0.18f;

        [Header("Core")] [SerializeField] private bool rotateCore = true;

        [SerializeField] private float coreRotationSpeed = -65f;

        [SerializeField] private bool pulseCore = true;

        [SerializeField] [Min(0f)] private float coreScalePulseAmplitude = 0.08f;

        [SerializeField] [Min(0f)] private float corePulseSpeed = 1.8f;

        [SerializeField] [Range(0f, 1f)] private float coreAlphaPulseAmplitude = 0.10f;

        [Header("Halo")] [SerializeField] private bool rotateHalo = true;

        [SerializeField] private float haloRotationSpeed = 18f;

        [SerializeField] private bool pulseHalo = true;

        [SerializeField] [Min(0f)] private float haloScalePulseAmplitude = 0.12f;

        [SerializeField] [Min(0f)] private float haloPulseSpeed = 0.9f;

        [SerializeField] [Range(0f, 1f)] private float haloAlphaPulseAmplitude = 0.16f;

        [SerializeField] private float haloPhaseOffset = 0.75f;

        [Header("Flicker")] [SerializeField] private bool enableFlicker = true;

        [SerializeField] [Min(0f)] private float flickerSpeed = 4.5f;

        [SerializeField] [Range(0f, 1f)] private float flickerAmplitude = 0.06f;

        [Header("Debug")] [SerializeField] private bool autoFindReferencesOnReset = true;

        private float noiseSeed;

        private float baseRingStartWidth;
        private float baseRingEndWidth;
        private Color baseRingStartColor;
        private Color baseRingEndColor;

        private Vector3 baseCoreScale;
        private Quaternion baseCoreRotation;
        private Color baseCoreColor;

        private Vector3 baseHaloScale;
        private Quaternion baseHaloRotation;
        private Color baseHaloColor;

        private void Reset()
        {
            if (!autoFindReferencesOnReset)
                return;

            if (gravityRing == null)
                gravityRing = GetComponentInChildren<LineRenderer>(true);

            if (coreVisualRoot == null)
            {
                var t = transform.Find("CoreVisual");
                if (t != null)
                    coreVisualRoot = t;
            }

            if (haloVisualRoot == null)
            {
                var t = transform.Find("HaloVisual");
                if (t != null)
                    haloVisualRoot = t;
            }

            if (coreSprite == null && coreVisualRoot != null)
                coreSprite = coreVisualRoot.GetComponent<SpriteRenderer>();

            if (haloSprite == null && haloVisualRoot != null)
                haloSprite = haloVisualRoot.GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            ApplyVisual(0f, true);
        }

        private void OnValidate()
        {
            ringWidthPulseAmplitude = Mathf.Max(0f, ringWidthPulseAmplitude);
            ringPulseSpeed = Mathf.Max(0f, ringPulseSpeed);

            coreScalePulseAmplitude = Mathf.Max(0f, coreScalePulseAmplitude);
            corePulseSpeed = Mathf.Max(0f, corePulseSpeed);

            haloScalePulseAmplitude = Mathf.Max(0f, haloScalePulseAmplitude);
            haloPulseSpeed = Mathf.Max(0f, haloPulseSpeed);

            flickerSpeed = Mathf.Max(0f, flickerSpeed);
        }

        [ContextMenu("Rebind Base Visual State")]
        private void RebindBaseVisualState()
        {
            CacheBaseState();
            ApplyVisual(0f, true);
        }

        private void Initialize()
        {
            if (noiseSeed <= 0f)
                noiseSeed = Random.Range(0.001f, 999.999f);

            if (coreSprite == null && coreVisualRoot != null)
                coreSprite = coreVisualRoot.GetComponent<SpriteRenderer>();

            if (haloSprite == null && haloVisualRoot != null)
                haloSprite = haloVisualRoot.GetComponent<SpriteRenderer>();

            CacheBaseState();
        }

        private void CacheBaseState()
        {
            if (gravityRing != null)
            {
                baseRingStartWidth = gravityRing.startWidth;
                baseRingEndWidth = gravityRing.endWidth;
                baseRingStartColor = gravityRing.startColor;
                baseRingEndColor = gravityRing.endColor;
            }

            if (coreVisualRoot != null)
            {
                baseCoreScale = coreVisualRoot.localScale;
                baseCoreRotation = coreVisualRoot.localRotation;
            }

            if (coreSprite != null)
                baseCoreColor = coreSprite.color;

            if (haloVisualRoot != null)
            {
                baseHaloScale = haloVisualRoot.localScale;
                baseHaloRotation = haloVisualRoot.localRotation;
            }

            if (haloSprite != null)
                baseHaloColor = haloSprite.color;
        }

        private void Update()
        {
            ApplyVisual(Time.time, false);
        }

        private void ApplyVisual(float time, bool forceBaseOnly)
        {
            var flicker = forceBaseOnly ? 0f : EvaluateFlicker(time);

            ApplyRing(time, flicker, forceBaseOnly);
            ApplyCore(time, flicker, forceBaseOnly);
            ApplyHalo(time, flicker, forceBaseOnly);
        }

        private void ApplyRing(float time, float flicker, bool forceBaseOnly)
        {
            if (gravityRing == null)
                return;

            float widthMul = 1f;
            float alphaMul = 1f;

            if (!forceBaseOnly && pulseRing)
            {
                var wave = Mathf.Sin(time * ringPulseSpeed + noiseSeed);
                widthMul += wave * ringWidthPulseAmplitude;
                alphaMul += wave * ringAlphaPulseAmplitude;
            }

            if (!forceBaseOnly && enableFlicker)
            {
                widthMul += flicker * (ringWidthPulseAmplitude * 0.5f);
                alphaMul += flicker * 0.5f;
            }

            gravityRing.startWidth = Mathf.Max(0.001f, baseRingStartWidth * widthMul);
            gravityRing.endWidth = Mathf.Max(0.001f, baseRingEndWidth * widthMul);

            var startColor = baseRingStartColor;
            var endColor = baseRingEndColor;

            startColor.a = Mathf.Clamp01(baseRingStartColor.a * alphaMul);
            endColor.a = Mathf.Clamp01(baseRingEndColor.a * alphaMul);

            gravityRing.startColor = startColor;
            gravityRing.endColor = endColor;
        }

        private void ApplyCore(float time, float flicker, bool forceBaseOnly)
        {
            if (coreVisualRoot != null)
            {
                var scale = baseCoreScale;
                var rotation = baseCoreRotation;

                if (!forceBaseOnly)
                {
                    if (pulseCore)
                    {
                        var wave = Mathf.Sin(time * corePulseSpeed + noiseSeed * 1.37f);
                        var scaleMul = 1f + (wave * coreScalePulseAmplitude);

                        if (enableFlicker)
                            scaleMul += flicker * (coreScalePulseAmplitude * 0.35f);

                        scale *= scaleMul;
                    }

                    if (rotateCore)
                    {
                        rotation *= Quaternion.Euler(0f, 0f, time * coreRotationSpeed);
                    }
                }

                coreVisualRoot.localScale = scale;
                coreVisualRoot.localRotation = rotation;
            }

            if (coreSprite != null)
            {
                var color = baseCoreColor;

                if (!forceBaseOnly)
                {
                    var alphaMul = 1f;

                    if (pulseCore)
                    {
                        var wave = Mathf.Sin(time * corePulseSpeed + noiseSeed * 1.37f);
                        alphaMul += wave * coreAlphaPulseAmplitude;
                    }

                    if (enableFlicker)
                        alphaMul += flicker * 0.35f;

                    color.a = Mathf.Clamp01(baseCoreColor.a * alphaMul);
                }

                coreSprite.color = color;
            }
        }

        private void ApplyHalo(float time, float flicker, bool forceBaseOnly)
        {
            if (haloVisualRoot != null)
            {
                var scale = baseHaloScale;
                var rotation = baseHaloRotation;

                if (!forceBaseOnly)
                {
                    if (pulseHalo)
                    {
                        var wave = Mathf.Sin(time * haloPulseSpeed + haloPhaseOffset + noiseSeed * 0.73f);
                        var scaleMul = 1f + (wave * haloScalePulseAmplitude);

                        if (enableFlicker)
                            scaleMul += flicker * (haloScalePulseAmplitude * 0.25f);

                        scale *= scaleMul;
                    }

                    if (rotateHalo)
                    {
                        rotation *= Quaternion.Euler(0f, 0f, time * haloRotationSpeed);
                    }
                }

                haloVisualRoot.localScale = scale;
                haloVisualRoot.localRotation = rotation;
            }

            if (haloSprite != null)
            {
                var color = baseHaloColor;

                if (!forceBaseOnly)
                {
                    var alphaMul = 1f;

                    if (pulseHalo)
                    {
                        var wave = Mathf.Sin(time * haloPulseSpeed + haloPhaseOffset + noiseSeed * 0.73f);
                        alphaMul += wave * haloAlphaPulseAmplitude;
                    }

                    if (enableFlicker)
                        alphaMul += flicker * 0.4f;

                    color.a = Mathf.Clamp01(baseHaloColor.a * alphaMul);
                }

                haloSprite.color = color;
            }
        }

        private float EvaluateFlicker(float time)
        {
            if (!enableFlicker || flickerAmplitude <= 0f)
                return 0f;

            var n = Mathf.PerlinNoise(noiseSeed, time * flickerSpeed);
            return (n * 2f - 1f) * flickerAmplitude;
        }
    }
}