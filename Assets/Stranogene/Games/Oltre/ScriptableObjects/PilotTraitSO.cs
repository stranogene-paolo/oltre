using UnityEngine;

namespace Stranogene.Games.Oltre.ScriptableObjects
{
    /// <summary>
    /// PilotTraitSO
    /// Un singolo tratto pescabile.
    /// Modifica:
    /// - MaxAge (delta)
    /// - consumo energia (moltiplicatore)
    /// </summary>
    [CreateAssetMenu(fileName = "PilotTrait", menuName = "Stranogene/Oltre/Pilot/Trait", order = 0)]
    public class PilotTraitSO : ScriptableObject
    {
        [Header("Identity")] public string traitId = "trait_id";
        public string displayName = "Trait Name";
        [TextArea] public string description;

        [Header("Random Weight")] [Min(0f)] public float weight = 1f;

        [Header("Modifiers")] [Tooltip("Delta applicato al MaxAge finale (può essere negativo).")]
        public int maxAgeDelta = 0;

        [Tooltip("Moltiplicatore consumo energia. 1 = normale, 1.2 = consuma di più, 0.8 = consuma di meno.")]
        [Min(0.01f)]
        public float energyConsumptionMultiplier = 1f;
    }
}