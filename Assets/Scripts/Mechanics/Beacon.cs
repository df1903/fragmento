using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Faro de piedra (nivel 6) o farol (nivel 7). Empieza apagado. Se enciende cuando lo alcanza un Pulso de luz
///   (o con un evento de mundo). Encendido hace tres cosas: da luz, es zona segura (los enemigos no entran) y
///   es el punto de reaparición de Kael.
///
/// POR QUÉ EXISTE
///   Los niveles 6 y 7 son grandes: volver al inicio en cada muerte sería frustrante. Encender faros es a la vez
///   el progreso del jugador y su refugio. Los enemigos preguntan Beacon.IsInSafeLight(posición).
///
/// CÓMO SE USA
///   "Lit Visual" es lo que se enciende (llama y luz). "Safe Radius" es el radio de la zona segura.
///   "Light On Event Id": si no está vacío, también se enciende con ese evento de mundo.
/// </summary>
public class Beacon : MonoBehaviour
{
    [Tooltip("Objeto que se activa al encenderse (llama, luz).")]
    [SerializeField] private GameObject litVisual;

    [Tooltip("Radio de la zona segura alrededor del faro encendido.")]
    [SerializeField] private float safeRadius = 7f;

    [Tooltip("Si está marcado, empieza encendido.")]
    [SerializeField] private bool startLit;

    [Tooltip("Opcional: evento de mundo que también lo enciende.")]
    [SerializeField] private string lightOnEventId = "";

    // Faros encendidos de la escena: los enemigos los recorren para saber dónde no pueden entrar.
    private static readonly List<Beacon> litBeacons = new List<Beacon>();

    public bool IsLit { get; private set; }
    public float SafeRadius => safeRadius;

    /// <summary>Configura el faro desde código (lo usa el constructor de escena).</summary>
    public void Setup(GameObject visual, float radius, bool lit, string eventId)
    {
        litVisual = visual;
        safeRadius = radius;
        startLit = lit;
        lightOnEventId = eventId;
    }

    /// <summary>True si la posición está dentro de la luz de algún faro encendido.</summary>
    public static bool IsInSafeLight(Vector3 position)
    {
        foreach (Beacon beacon in litBeacons)
        {
            Vector3 offset = position - beacon.transform.position;
            offset.y = 0f;
            if (offset.magnitude < beacon.safeRadius)
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        if (litVisual != null)
        {
            litVisual.SetActive(false);
        }
    }

    private void Start()
    {
        if (startLit)
        {
            LightUp(false);
        }
    }

    private void OnEnable()
    {
        GameEvents.LightPulsed += OnLightPulsed;
        GameEvents.WorldEventRequested += OnWorldEventRequested;
    }

    private void OnDisable()
    {
        GameEvents.LightPulsed -= OnLightPulsed;
        GameEvents.WorldEventRequested -= OnWorldEventRequested;
        litBeacons.Remove(this);
    }

    private void OnLightPulsed(Vector3 center, float radius)
    {
        if (Vector3.Distance(center, transform.position) <= radius + 1f)
        {
            LightUp(true);
        }
    }

    private void OnWorldEventRequested(string eventId)
    {
        if (!string.IsNullOrEmpty(lightOnEventId) && eventId == lightOnEventId)
        {
            LightUp(false);
        }
    }

    private void LightUp(bool byPlayer)
    {
        if (IsLit)
        {
            return;
        }

        IsLit = true;
        litBeacons.Add(this);
        if (litVisual != null)
        {
            litVisual.SetActive(true);
        }

        // Solo los faros que enciende Kael en persona pasan a ser su punto de reaparición.
        if (byPlayer)
        {
            GameEvents.RaiseCheckpointReached(transform.position + transform.forward * 1.5f + Vector3.up * 0.6f);
            GameEvents.RaiseMessageRequested("Faro encendido. Aquí reaparecerás.");
        }
    }

    // La lista es estática: se vacía al iniciar Play para no heredar faros de otra partida.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        litBeacons.Clear();
    }
}
