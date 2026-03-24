using UnityEngine;

namespace Stranogene.Games.Oltre.Space
{
    /// <summary>
    /// Wrapper del campo gravitazionale del black hole.
    ///
    /// Per ora usa tutta la logica del campo gravitazionale radiale generico.
    /// Serve soprattutto per:
    /// - dare un'identità chiara al prefab
    /// - poter specializzare il black hole in futuro senza toccare altri body
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BlackHoleGravityField : RadialGravityField2D
    {
    }
}