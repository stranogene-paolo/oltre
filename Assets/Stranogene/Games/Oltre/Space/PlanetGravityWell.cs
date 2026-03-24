using UnityEngine;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// Wrapper retrocompatibile del campo gravitazionale del pianeta.
    /// 
    /// Scopo:
    /// - non rompere prefab/scenes già presenti
    /// - permettere di continuare a usare il nome PlanetGravityWell dove già esiste
    /// - spostare però la logica reale in un componente riusabile per altri corpi celesti
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlanetGravityWell : RadialGravityField2D
    {
    }
}