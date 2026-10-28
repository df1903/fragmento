using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Recuerda el último checkpoint que pisó Kael y, cuando Kael muere (caída o enemigo), ordena devolverlo allí
///   (GameEvents.PlayerRespawned).
///
/// POR QUÉ EXISTE
///   Separa tres responsabilidades: detectar la caída (KillZone), saber a dónde volver (este script)
///   y moverse (PlayerController). Ninguno conoce a los otros.
///
/// CÓMO SE USA
///   Un único RespawnManager en la escena. "Initial Spawn" es el punto de la primera isla, por si Kael
///   cae antes de que se registre ningún checkpoint. El checkpoint se guarda solo en memoria.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    [Tooltip("Punto de reaparición inicial (isla de inicio).")]
    [SerializeField] private Transform initialSpawn;

    private Vector3 lastCheckpoint;

    /// <summary>Posición a la que volvería Kael si cayera ahora.</summary>
    public Vector3 LastCheckpoint => lastCheckpoint;

    private void Awake()
    {
        if (initialSpawn != null)
        {
            lastCheckpoint = initialSpawn.position;
        }
    }

    private void OnEnable()
    {
        GameEvents.CheckpointReached += OnCheckpointReached;
        GameEvents.PlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEvents.CheckpointReached -= OnCheckpointReached;
        GameEvents.PlayerDied -= OnPlayerDied;
    }

    private void OnCheckpointReached(Vector3 position)
    {
        lastCheckpoint = position;
    }

    private void OnPlayerDied()
    {
        // Dificultad por reintentos: toda muerte devuelve al inicio del nivel, no al último checkpoint.
        GameEvents.RaisePlayerRespawned(initialSpawn != null ? initialSpawn.position : lastCheckpoint);
    }
}
