using UnityEngine;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// Wrapper retrocompatibile del body hazard del pianeta.
    ///
    /// Scopo:
    /// - non rompere prefab/scenes già presenti
    /// - permettere di continuare a usare il nome PlanetBodyHazard dove già esiste
    /// - spostare però la logica reale in un componente riusabile per altri corpi celesti
    /// </summary>
    public class PlanetBodyHazard : SpaceBodyImpactHazard
    {
        protected override string KillReason => "Planet impact";
        protected override string LogPrefix => "PlanetBodyHazard";
    }
}