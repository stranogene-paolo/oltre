using UnityEngine;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// Wrapper dell'event horizon / core letale del black hole.
    ///
    /// Di base eredita il comportamento di impatto/ingresso letale dal body hazard generico.
    /// Pensato per essere usato con un collider 2D centrale, preferibilmente trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BlackHoleEventHorizon : SpaceBodyImpactHazard
    {
        protected override string KillReason => "Black hole event horizon";
        protected override string LogPrefix => "BlackHoleEventHorizon";
    }
}