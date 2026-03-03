using UnityEngine;

namespace Stranogene.Games.Oltre.Run
{
    /// <summary>
    /// Marker minimale per identificare una spaceship persistente (derelitto).
    /// </summary>
    public class DerelictMarker : MonoBehaviour
    {
        public int RunIndex { get; set; }
        public string Timestamp { get; set; }
    }
}