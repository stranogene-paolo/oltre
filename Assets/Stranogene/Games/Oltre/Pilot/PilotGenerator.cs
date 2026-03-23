using System.Collections.Generic;
using Stranogene.Games.Oltre.ScriptableObjects;
using UnityEngine;

namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// PilotGenerator
    /// Genera un profilo runtime partendo da un PilotTraitPoolSO.
    /// </summary>
    public static class PilotGenerator
    {
        public static PilotRuntimeProfile Generate(PilotTraitPoolSO pool)
        {
            // Fallback super safe
            if (pool == null)
            {
                return new PilotRuntimeProfile
                {
                    startAge = 30,
                    maxAge = 65,
                    energyConsumptionMultiplier = 1f,
                    traits = new List<PilotTraitSO>()
                };
            }

            var profile = new PilotRuntimeProfile
            {
                startAge = Random.Range(pool.minStartAge, pool.maxStartAge + 1),
                maxAge = Random.Range(pool.minMaxAge, pool.maxMaxAge + 1),
                energyConsumptionMultiplier = 1f,
                traits = new List<PilotTraitSO>()
            };

            // Traits count
            var count = Random.Range(pool.minTraits, pool.maxTraits + 1);
            if (count <= 0 || pool.traits == null || pool.traits.Count == 0)
            {
                // garantisci coerenza: maxAge deve essere > startAge
                if (profile.maxAge <= profile.startAge) profile.maxAge = profile.startAge + 1;
                return profile;
            }

            // Copia working list se unique
            List<PilotTraitSO> working = pool.uniqueTraits ? new List<PilotTraitSO>(pool.traits) : pool.traits;

            for (var i = 0; i < count; i++)
            {
                var picked = PickWeighted(working);
                if (picked == null) break;

                profile.traits.Add(picked);

                // Apply modifiers
                profile.maxAge += picked.maxAgeDelta;
                profile.energyConsumptionMultiplier *= picked.energyConsumptionMultiplier;

                if (pool.uniqueTraits)
                    working.Remove(picked);

                if (working.Count == 0) break;
            }

            // Clamp finale ragionevole
            profile.maxAge = Mathf.Clamp(profile.maxAge, pool.finalMinMaxAge, pool.finalMaxMaxAge);

            // Garantisce coerenza con startAge
            if (profile.maxAge <= profile.startAge)
                profile.maxAge = profile.startAge + 1;

            // Clamp / sanity
            if (profile.energyConsumptionMultiplier < 0.01f) profile.energyConsumptionMultiplier = 0.01f;
            if (profile.maxAge <= profile.startAge) profile.maxAge = profile.startAge + 1;

            return profile;
        }

        private static PilotTraitSO PickWeighted(List<PilotTraitSO> list)
        {
            if (list == null || list.Count == 0) return null;

            var total = 0f;
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t == null) continue;
                if (t.weight <= 0f) continue;
                total += t.weight;
            }

            if (total <= 0f)
            {
                // fallback: prima non-null
                for (var i = 0; i < list.Count; i++)
                    if (list[i] != null)
                        return list[i];
                return null;
            }

            var roll = Random.value * total;
            var acc = 0f;

            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t == null) continue;
                if (t.weight <= 0f) continue;

                acc += t.weight;
                if (roll <= acc) return t;
            }

            // fallback
            for (var i = list.Count - 1; i >= 0; i--)
                if (list[i] != null)
                    return list[i];

            return null;
        }
    }
}