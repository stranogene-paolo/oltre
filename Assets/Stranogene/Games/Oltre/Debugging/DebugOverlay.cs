using Stranogene.Games.Oltre.Spaceship;
using UnityEngine;

namespace Stranogene.Games.Oltre.Debugging
{
    /// <summary>
    /// DebugOverlay (OLTRE)
    /// Overlay minimale per verifiche rapide in Play Mode:
    /// - FPS (smoothed)
    /// - Timescale / VSync / TargetFrameRate
    /// - Risoluzione
    /// - Spaceship Energy / Pilot Age (anni)
    /// Toggle: F3
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Toggle")] [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        [Header("Display")] [SerializeField] private bool visible = true;
        [SerializeField] private int fontSize = 14;

        [Header("FPS")] [Tooltip("Più alto = più stabile (ma meno reattivo).")] [Range(0.02f, 1f)] [SerializeField]
        private float fpsSmoothing = 0.2f;

        [Header("Gameplay (optional reference)")]
        [Tooltip("Se non assegnato, il DebugOverlay cercherà automaticamente un SpaceshipLife in scena.")]
        [SerializeField]
        private SpaceshipLife spaceshipLife;

        private float smoothedUnscaledDeltaTime = 0.016f; // start ~60fps
        private GUIStyle style;
        private Rect boxRect;

        private void Awake()
        {
            // Evita duplicati se in futuro aggiungiamo scene multiple.
            var existing = FindObjectsByType<DebugOverlay>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            // SOLO dati non-IMGUI qui.
            boxRect = new Rect(10, 10, 460, 185);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;

            // Auto-bind leggero: prova a trovare SpaceshipLife se non assegnato.
            if (spaceshipLife == null)
                spaceshipLife = FindFirstObjectByType<SpaceshipLife>(FindObjectsInactive.Exclude);

            // Smoothing su deltaTime NON scalato (così anche con slowmo l’FPS resta “vero”).
            var dt = Time.unscaledDeltaTime;
            var t = (fpsSmoothing <= 0f) ? 1f : Mathf.Clamp01(dt / fpsSmoothing);
            smoothedUnscaledDeltaTime = Mathf.Lerp(smoothedUnscaledDeltaTime, dt, t);
        }

        private void OnGUI()
        {
            if (!visible) return;

            // In Unity alcune proprietà GUI (es. GUI.skin) vanno toccate solo dentro OnGUI.
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    richText = true
                };
            }
            else if (style.fontSize != fontSize)
            {
                style.fontSize = fontSize;
            }

            var fps = (smoothedUnscaledDeltaTime > 0f) ? (1f / smoothedUnscaledDeltaTime) : 0f;

            var vsync = QualitySettings.vSyncCount;
            var target = Application.targetFrameRate;
            var timescale = Time.timeScale;

            var w = Screen.width;
            var h = Screen.height;

            // Gameplay readout (numerici come richiesto)
            string gameplayLine;
            if (spaceshipLife != null)
            {
                float energy = spaceshipLife.Energy; // float

                // Età pilota: solo interi, solo in crescita
                int age = spaceshipLife.PilotAge;
                int maxAge = spaceshipLife.PilotMaxAge;

                gameplayLine =
                    $"Energy: <b>{energy:0.0}</b>  |  Pilot Age: <b>{age}</b> / <b>{maxAge}</b>";
            }
            else
            {
                gameplayLine = "Energy: <b>—</b>  |  Pilot Age: <b>—</b> / <b>—</b> (SpaceshipLife not found)";
            }

            var text =
                $"<b>OLTRE Debug</b>  (toggle: {toggleKey})\n" +
                $"FPS: <b>{fps:0}</b>  |  unscaled dt: {smoothedUnscaledDeltaTime * 1000f:0.0} ms\n" +
                $"Time.timeScale: <b>{timescale:0.00}</b>\n" +
                $"VSync: <b>{vsync}</b>  |  targetFrameRate: <b>{target}</b>\n" +
                $"Resolution: <b>{w}x{h}</b>\n" +
                $"{gameplayLine}\n";

            GUI.Box(boxRect, GUIContent.none);
            GUI.Label(
                new Rect(boxRect.x + 10, boxRect.y + 8, boxRect.width - 20, boxRect.height - 16),
                text,
                style
            );
        }
    }
}