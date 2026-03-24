using System.Collections.Generic;
using UnityEngine;
using Stranogene.Games.Oltre.Spaceship;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// RadialGravityField2D
    /// - usa un CircleCollider2D trigger come area di influenza
    /// - applica una forza verso il centro del campo
    /// - più ti avvicini, più la gravità aumenta
    ///
    /// Visual:
    /// - disegna in game un ring procedurale via LineRenderer
    /// - nessuno sprite richiesto per il campo gravitazionale
    ///
    /// Gameplay:
    /// - può applicare una spinta di slingshot
    /// - può applicare un trajectory assist per rendere il flyby più leggibile
    /// - può ridurre temporaneamente il lateral damping della nave
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CircleCollider2D))]
    public class RadialGravityField2D : MonoBehaviour
    {
        [Header("Gravity")]
        [Tooltip("Accelerazione gravitazionale base. Più alto = traiettorie più curve.")]
        [SerializeField]
        private float gravityStrength = 25f;

        [Tooltip("Esponente del falloff. 2 = circa inverse-square, 1 = più morbido.")] [SerializeField]
        private float gravityExponent = 2f;

        [Tooltip("Distanza minima usata per evitare forze infinite vicino al centro.")] [SerializeField]
        private float minDistance = 0.75f;

        [Tooltip("Clamp massimo dell'accelerazione applicata.")] [SerializeField]
        private float maxAcceleration = 40f;

        [Tooltip(
            "Moltiplicatore della gravità al bordo del campo. 0 = nessuna gravità sul bordo, 1 = piena gravità già sul bordo.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float boundaryGravityMultiplier = 0.2f;

        [Tooltip("Quanto rapidamente la gravità cresce andando verso il centro del campo.")] [SerializeField]
        private float radialFalloffPower = 1.75f;

        [Header("Slingshot")] [Tooltip("Abilita la spinta extra da fionda gravitazionale.")] [SerializeField]
        private bool enableSlingshot = true;

        [Tooltip("Accelerazione extra applicata lungo la direzione della velocità in uscita dal pozzo.")]
        [SerializeField]
        private float slingshotAcceleration = 14f;

        [Tooltip("Velocità minima richiesta per poter attivare la fionda.")] [SerializeField]
        private float minSpeedForSlingshot = 1.5f;

        [Tooltip("Quanto la traiettoria deve essere tangenziale. 0 = irrilevante, 1 = perfettamente tangenziale.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float minTangentialFactor = 0.55f;

        [Tooltip(
            "Quanto la nave deve essere già in uscita dal campo. 0 = basta non entrare, 1 = uscita perfettamente radiale.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float minOutwardFactor = 0.1f;

        [Tooltip("Controlla quanto la fionda privilegia il passaggio vicino al centro del campo.")] [SerializeField]
        private float slingshotRadialPower = 1.35f;

        [Header("Target Filter")]
        [Tooltip("Layer colpiti dalla gravità. Se lasci Everything, influenzerà tutti i Rigidbody2D nel trigger.")]
        [SerializeField]
        private LayerMask affectedLayers = ~0;

        [Tooltip("Se true, applica gravità solo a oggetti che hanno SpaceshipLife.")] [SerializeField]
        private bool requireSpaceshipLife = true;

        [Header("Visual")] [Tooltip("Mostra in game il ring dell'area di influenza gravitazionale.")] [SerializeField]
        private bool showGravityField = true;

        [Tooltip("Colore base del ring gravitazionale.")] [SerializeField]
        private Color gravityFieldColor = new Color(0.25f, 0.85f, 1f, 1f);

        [Tooltip("Alpha del ring gravitazionale.")] [SerializeField] [Range(0f, 1f)]
        private float gravityFieldAlpha = 0.22f;

        [Tooltip("Spessore del ring.")] [SerializeField]
        private float gravityFieldLineWidth = 0.08f;

        [Tooltip("Numero di segmenti usati per disegnare il ring.")] [SerializeField] [Range(24, 256)]
        private int gravityFieldSegments = 96;

        [Tooltip("Sorting order del ring.")] [SerializeField]
        private int gravityFieldSortingOrder = -5;

        [Header("Trajectory Assist")]
        [Tooltip("Riduce il lateral damping della nave mentre attraversa il pozzo gravitazionale.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float gravityDriftLateralDampingMultiplier = 0.12f;

        [Tooltip("Piccola coda temporale per mantenere la curvatura anche subito dopo il passaggio.")] [SerializeField]
        private float gravityDriftGraceTime = 0.2f;

        [Tooltip("Abilita un aiuto di traiettoria per ottenere una fionda gravitazionale leggibile in gameplay.")]
        [SerializeField]
        private bool enableTrajectoryAssist = true;

        [Tooltip("Velocità minima per applicare il trajectory assist.")] [SerializeField]
        private float minSpeedForTrajectoryAssist = 1f;

        [Tooltip("Quanto il moto deve essere tangenziale per attivare l'assist.")] [SerializeField] [Range(0f, 1f)]
        private float minTangentialFactorForAssist = 0.45f;

        [Tooltip("Quanto assist applichiamo già al bordo del campo gravitazionale.")] [SerializeField] [Range(0f, 1f)]
        private float boundaryAssistMultiplier = 0.40f;

        [Tooltip("Accelerazione che contrasta la caduta verso il centro del campo.")] [SerializeField]
        private float inwardCancelAcceleration = 10f;

        [Tooltip("Accelerazione che accompagna la nave lungo la tangente corretta.")] [SerializeField]
        private float tangentialAssistAcceleration = 11f;

        [Header("Debug")] [SerializeField] private bool drawDebugLines = true;

        private CircleCollider2D gravityTrigger;
        private readonly List<Rigidbody2D> trackedBodies = new List<Rigidbody2D>();

        private LineRenderer gravityFieldRenderer;
        private Vector3[] gravityFieldPoints;

        private static Material sharedLineMaterial;

        private void Awake()
        {
            gravityTrigger = GetComponent<CircleCollider2D>();
            gravityTrigger.isTrigger = true;

            EnsureGravityFieldRenderer();
            SyncGravityFieldRenderer();
        }

        private void OnEnable()
        {
            EnsureGravityFieldRenderer();
            SyncGravityFieldRenderer();
        }

        private void OnDisable()
        {
            trackedBodies.Clear();

            if (gravityFieldRenderer != null)
                gravityFieldRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            SyncGravityFieldRenderer();
        }

        private void OnValidate()
        {
            if (gravityStrength < 0f) gravityStrength = 0f;
            if (gravityExponent < 0.01f) gravityExponent = 0.01f;
            if (minDistance < 0.01f) minDistance = 0.01f;
            if (maxAcceleration < 0f) maxAcceleration = 0f;

            boundaryGravityMultiplier = Mathf.Clamp01(boundaryGravityMultiplier);
            if (radialFalloffPower < 0.01f) radialFalloffPower = 0.01f;

            if (slingshotAcceleration < 0f) slingshotAcceleration = 0f;
            if (minSpeedForSlingshot < 0f) minSpeedForSlingshot = 0f;
            minTangentialFactor = Mathf.Clamp01(minTangentialFactor);
            minOutwardFactor = Mathf.Clamp01(minOutwardFactor);
            if (slingshotRadialPower < 0.01f) slingshotRadialPower = 0.01f;

            if (gravityFieldLineWidth < 0.001f) gravityFieldLineWidth = 0.001f;
            if (gravityFieldSegments < 24) gravityFieldSegments = 24;

            gravityDriftLateralDampingMultiplier = Mathf.Clamp01(gravityDriftLateralDampingMultiplier);
            if (gravityDriftGraceTime < 0f) gravityDriftGraceTime = 0f;

            if (minSpeedForTrajectoryAssist < 0f) minSpeedForTrajectoryAssist = 0f;
            minTangentialFactorForAssist = Mathf.Clamp01(minTangentialFactorForAssist);
            boundaryAssistMultiplier = Mathf.Clamp01(boundaryAssistMultiplier);
            if (inwardCancelAcceleration < 0f) inwardCancelAcceleration = 0f;
            if (tangentialAssistAcceleration < 0f) tangentialAssistAcceleration = 0f;

            var trigger = GetComponent<CircleCollider2D>();
            if (trigger != null)
                trigger.isTrigger = true;

            gravityTrigger = trigger;

            EnsureGravityFieldRenderer();
            SyncGravityFieldRenderer();
        }

        private void FixedUpdate()
        {
            if (trackedBodies.Count == 0)
                return;

            var center = GetGravityCenterWorld();
            var triggerWorldRadius = GetWorldTriggerRadius();

            for (var i = trackedBodies.Count - 1; i >= 0; i--)
            {
                var rb = trackedBodies[i];

                if (rb == null || !rb.gameObject.activeInHierarchy)
                {
                    trackedBodies.RemoveAt(i);
                    continue;
                }

                if (rb.bodyType != RigidbodyType2D.Dynamic)
                    continue;

                if (!IsValidTarget(rb))
                    continue;

                var toCenter = center - rb.position;
                var distance = Mathf.Max(minDistance, toCenter.magnitude);

                if (distance <= 0.0001f)
                    continue;

                var direction = toCenter / distance;

                var acceleration = gravityStrength / Mathf.Pow(distance, gravityExponent);

                var inward01 = 0f;
                if (triggerWorldRadius > 0.0001f)
                {
                    var normalizedDistance = Mathf.Clamp01(distance / triggerWorldRadius);
                    inward01 = 1f - normalizedDistance;

                    var radialBlend = Mathf.Pow(inward01, radialFalloffPower);
                    var boundaryFactor = Mathf.Lerp(boundaryGravityMultiplier, 1f, radialBlend);

                    acceleration *= boundaryFactor;
                }

                acceleration = Mathf.Min(acceleration, maxAcceleration);

                rb.AddForce(direction * acceleration * rb.mass, ForceMode2D.Force);

                if (enableTrajectoryAssist)
                    ApplyTrajectoryAssist(rb, direction, distance, triggerWorldRadius);

                if (rb.TryGetComponent<SpaceshipMovement>(out var movement))
                {
                    movement.SetExternalLateralDampingMultiplier(
                        gravityDriftLateralDampingMultiplier,
                        gravityDriftGraceTime);
                }

                if (enableSlingshot)
                    ApplySlingshot(rb, direction, distance, triggerWorldRadius, inward01);

                if (drawDebugLines)
                    Debug.DrawLine(rb.position, center, Color.cyan);
            }
        }

        private void ApplySlingshot(
            Rigidbody2D rb,
            Vector2 directionToCenter,
            float distance,
            float triggerWorldRadius,
            float inward01)
        {
            var velocity = rb.linearVelocity;
            var speed = velocity.magnitude;

            if (speed < minSpeedForSlingshot)
                return;

            var velocityDir = velocity / speed;
            var outwardDir = -directionToCenter;
            var tangentDir = new Vector2(-outwardDir.y, outwardDir.x);

            var tangentialFactor = Mathf.Abs(Vector2.Dot(velocityDir, tangentDir));
            var outwardFactor = Vector2.Dot(velocityDir, outwardDir);

            if (tangentialFactor < minTangentialFactor)
                return;

            if (outwardFactor < minOutwardFactor)
                return;

            var tangential01 = Mathf.InverseLerp(minTangentialFactor, 1f, tangentialFactor);
            var outward01 = Mathf.InverseLerp(minOutwardFactor, 1f, outwardFactor);

            var radial01 = inward01;
            if (triggerWorldRadius > 0.0001f)
                radial01 = Mathf.Clamp01(1f - (distance / triggerWorldRadius));

            var radialBoost = Mathf.Pow(radial01, slingshotRadialPower);
            var slingshotFactor = tangential01 * outward01 * radialBoost;

            if (slingshotFactor <= 0.0001f)
                return;

            rb.AddForce(
                velocityDir * (slingshotAcceleration * slingshotFactor * rb.mass),
                ForceMode2D.Force);

            if (drawDebugLines)
                Debug.DrawRay(rb.position, velocityDir * (1.5f + slingshotFactor * 2f), Color.yellow);
        }

        private void ApplyTrajectoryAssist(
            Rigidbody2D rb,
            Vector2 directionToCenter,
            float distance,
            float triggerWorldRadius)
        {
            var velocity = rb.linearVelocity;
            var speed = velocity.magnitude;

            if (speed < minSpeedForTrajectoryAssist)
                return;

            var velocityDir = velocity / speed;
            var baseTangent = new Vector2(-directionToCenter.y, directionToCenter.x);

            var tangentialDot = Vector2.Dot(velocityDir, baseTangent);
            var tangentialFactor = Mathf.Abs(tangentialDot);

            if (tangentialFactor < minTangentialFactorForAssist)
                return;

            var tangentDir = tangentialDot >= 0f ? baseTangent : -baseTangent;

            var radial01 = 0f;
            if (triggerWorldRadius > 0.0001f)
                radial01 = Mathf.Clamp01(1f - (distance / triggerWorldRadius));

            var tangential01 = Mathf.InverseLerp(minTangentialFactorForAssist, 1f, tangentialFactor);
            var assist01 = tangential01 * Mathf.Lerp(boundaryAssistMultiplier, 1f, radial01);

            if (assist01 <= 0.0001f)
                return;

            var inwardSpeed = Vector2.Dot(velocity, directionToCenter);

            if (inwardSpeed > 0f)
            {
                rb.AddForce(
                    (-directionToCenter) * (inwardCancelAcceleration * assist01 * rb.mass),
                    ForceMode2D.Force);
            }

            rb.AddForce(
                tangentDir * (tangentialAssistAcceleration * assist01 * rb.mass),
                ForceMode2D.Force);

            if (drawDebugLines)
            {
                Debug.DrawRay(rb.position, tangentDir * (1.2f + assist01 * 2f), Color.green);

                if (inwardSpeed > 0f)
                {
                    Debug.DrawRay(
                        rb.position,
                        (-directionToCenter) * (1f + assist01 * 1.5f),
                        new Color(1f, 0.5f, 0f));
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            if (trackedBodies.Contains(rb)) return;
            if (!IsLayerAllowed(rb.gameObject.layer)) return;

            trackedBodies.Add(rb);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            trackedBodies.Remove(rb);
        }

        private bool IsValidTarget(Rigidbody2D rb)
        {
            if (!IsLayerAllowed(rb.gameObject.layer))
                return false;

            if (!requireSpaceshipLife)
                return true;

            var life = rb.GetComponent<SpaceshipLife>();
            return life != null && life.CanMove;
        }

        private bool IsLayerAllowed(int layer)
        {
            return (affectedLayers.value & (1 << layer)) != 0;
        }

        private Vector2 GetGravityCenterWorld()
        {
            if (gravityTrigger == null)
                return transform.position;

            return transform.TransformPoint(gravityTrigger.offset);
        }

        private float GetWorldTriggerRadius()
        {
            if (gravityTrigger == null)
                return 0f;

            var scale = transform.lossyScale;
            var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            return gravityTrigger.radius * maxScale;
        }

        private void EnsureGravityFieldRenderer()
        {
            if (gravityFieldRenderer == null)
                gravityFieldRenderer = GetComponent<LineRenderer>();

            if (gravityFieldRenderer == null)
                gravityFieldRenderer = gameObject.AddComponent<LineRenderer>();

            gravityFieldRenderer.useWorldSpace = true;
            gravityFieldRenderer.loop = true;
            gravityFieldRenderer.textureMode = LineTextureMode.Stretch;
            gravityFieldRenderer.alignment = LineAlignment.View;
            gravityFieldRenderer.numCornerVertices = 0;
            gravityFieldRenderer.numCapVertices = 0;
            gravityFieldRenderer.sortingOrder = gravityFieldSortingOrder;
            gravityFieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gravityFieldRenderer.receiveShadows = false;

            var material = GetSharedLineMaterial();
            if (material != null)
                gravityFieldRenderer.sharedMaterial = material;
        }

        private void SyncGravityFieldRenderer()
        {
            if (gravityFieldRenderer == null)
                EnsureGravityFieldRenderer();

            if (gravityFieldRenderer == null)
                return;

            if (!showGravityField)
            {
                gravityFieldRenderer.enabled = false;
                return;
            }

            if (gravityTrigger == null)
                gravityTrigger = GetComponent<CircleCollider2D>();

            if (gravityTrigger == null)
            {
                gravityFieldRenderer.enabled = false;
                return;
            }

            var worldRadius = GetWorldTriggerRadius();
            if (worldRadius <= 0.0001f)
            {
                gravityFieldRenderer.enabled = false;
                return;
            }

            gravityFieldRenderer.enabled = true;
            gravityFieldRenderer.sortingOrder = gravityFieldSortingOrder;
            gravityFieldRenderer.startWidth = gravityFieldLineWidth;
            gravityFieldRenderer.endWidth = gravityFieldLineWidth;

            var color = gravityFieldColor;
            color.a = gravityFieldAlpha;
            gravityFieldRenderer.startColor = color;
            gravityFieldRenderer.endColor = color;

            EnsurePointBuffer(gravityFieldSegments);

            var center = GetGravityCenterWorld();
            var center3 = new Vector3(center.x, center.y, transform.position.z);

            for (var i = 0; i < gravityFieldSegments; i++)
            {
                var t = (float)i / gravityFieldSegments;
                var angle = t * Mathf.PI * 2f;

                var x = Mathf.Cos(angle) * worldRadius;
                var y = Mathf.Sin(angle) * worldRadius;

                gravityFieldPoints[i] = center3 + new Vector3(x, y, 0f);
            }

            gravityFieldRenderer.positionCount = gravityFieldSegments;
            gravityFieldRenderer.SetPositions(gravityFieldPoints);
        }

        private void EnsurePointBuffer(int count)
        {
            if (gravityFieldPoints != null && gravityFieldPoints.Length == count)
                return;

            gravityFieldPoints = new Vector3[count];
        }

        private static Material GetSharedLineMaterial()
        {
            if (sharedLineMaterial != null)
                return sharedLineMaterial;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            sharedLineMaterial = new Material(shader)
            {
                name = "RadialGravityField2D_LineMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };

            return sharedLineMaterial;
        }

        private void OnDrawGizmosSelected()
        {
            var trigger = GetComponent<CircleCollider2D>();
            if (trigger == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);

            var worldCenter = transform.TransformPoint(trigger.offset);
            var scale = transform.lossyScale;
            var maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            var worldRadius = trigger.radius * maxScale;

            Gizmos.DrawWireSphere(worldCenter, worldRadius);
        }
    }
}