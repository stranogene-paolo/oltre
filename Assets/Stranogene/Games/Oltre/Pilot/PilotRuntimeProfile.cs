using System.Collections.Generic;
using Stranogene.Games.Oltre.ScriptableObjects;

namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// PilotRuntimeProfile
    /// Risultato della generazione di un pilota per una run.
    /// </summary>
    public struct PilotRuntimeProfile
    {
        public int startAge;
        public int maxAge;
        public float energyConsumptionMultiplier;
        public List<PilotTraitSO> traits;
    }
}