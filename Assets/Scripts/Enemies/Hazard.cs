using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Base común de todo lo que puede matar a Kael al tocarlo (enemigos, proyectiles). Se encarga de lo que
///   comparten: encontrar a Kael, quedarse quieto durante las cámaras de evento, dar una breve gracia tras
///   reaparecer y avisar de la muerte (GameEvents.PlayerDied).
///
/// POR QUÉ EXISTE
///   Antes cada enemigo repetía esa lógica. Con una sola clase base las reglas de justicia (no matar durante
///   una animación ni justo al reaparecer) están en un único sitio y se explican una vez.
///
/// CÓMO SE USA
///   Crear una clase hija e implementar Tick() (se llama cada fotograma, nunca durante una cámara de evento).
///   Para matar a Kael: TryKill(posición, radio). Si se sobrescriben OnEnable/OnDisable, llamar a base.
///   Las pruebas pueden apagar todos los peligros buscando este tipo y poniendo enabled = false.
/// </summary>
public abstract class Hazard : MonoBehaviour
{
    [Tooltip("Segundos sin poder matar a Kael justo después de que reaparezca.")]
    [SerializeField] private float respawnGrace = 1.5f;

    // Compartida: tras una muerte NADIE puede matar a Kael durante la gracia (evita muertes dobles en cadena).
    private static float graceUntil;

    private PlayerController player;
    private bool frozen;

    /// <summary>True durante las cámaras de evento: el peligro no se mueve ni mata.</summary>
    protected bool IsFrozen => frozen;

    /// <summary>Kael (null si todavía no existe en la escena).</summary>
    protected PlayerController Player => player;

    /// <summary>Centro del cuerpo de Kael (la raíz está en sus pies).</summary>
    protected Vector3 PlayerCenter => player.transform.position + Vector3.up * 0.9f;

    protected virtual void OnEnable()
    {
        GameEvents.PlayerRespawned += OnPlayerRespawned;
        GameEvents.CinematicStarted += Freeze;
        GameEvents.CinematicEnded += Unfreeze;
    }

    protected virtual void OnDisable()
    {
        GameEvents.PlayerRespawned -= OnPlayerRespawned;
        GameEvents.CinematicStarted -= Freeze;
        GameEvents.CinematicEnded -= Unfreeze;
    }

    private void OnPlayerRespawned(Vector3 position)
    {
        graceUntil = Time.time + respawnGrace;
    }

    private void Freeze() => frozen = true;

    private void Unfreeze()
    {
        frozen = false;
        graceUntil = Mathf.Max(graceUntil, Time.time + 0.5f); // medio segundo de margen al recuperar el control
    }

    private void Update()
    {
        if (frozen)
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        Tick();
    }

    /// <summary>Lógica del peligro en cada fotograma. Solo se llama si Kael existe y no hay cámara de evento.</summary>
    protected abstract void Tick();

    /// <summary>Mata a Kael si su centro está a menos de "radius" metros de "position" y no está en gracia. Devuelve true si lo mató.</summary>
    protected bool TryKill(Vector3 position, float radius)
    {
        if (Time.time < graceUntil || Vector3.Distance(position, PlayerCenter) >= radius)
        {
            return false;
        }

        graceUntil = Time.time + respawnGrace; // evita avisar varias veces seguidas
        GameEvents.RaisePlayerDied();
        return true;
    }

    // Time.time vuelve a 0 al iniciar Play, pero un estático sobrevive: se limpia para no heredar una gracia vieja.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        graceUntil = 0f;
    }
}
