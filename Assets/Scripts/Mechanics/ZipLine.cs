using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Tirolina: un cable tendido entre dos azoteas. Cuando Kael llega al punto de enganche, se cuelga de una
///   polea y baja deslizándose por el cable hasta el otro extremo, donde se suelta.
///
/// POR QUÉ EXISTE
///   Es la dinámica del nivel 8 (Ciudad Dormida): cruzar entre edificios por el aire. Usa el modo "llevado" de
///   PlayerController (IsCarried): mientras dura el viaje el jugador no controla a Kael, y la tirolina le dice
///   exactamente dónde estar en cada fotograma.
///
/// CÓMO SE USA
///   La raíz no importa; "Start" y "End" son los dos anclajes del cable (a la altura del cable). Kael cuelga
///   "Hang" metros por debajo. "Pulley" es la pieza visual que viaja por el cable (opcional).
/// </summary>
public class ZipLine : MonoBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [SerializeField] private Transform pulley;

    [Tooltip("Velocidad por el cable en m/s.")]
    [SerializeField] private float speed = 11f;

    [Tooltip("Metros entre el cable y los pies de Kael.")]
    [SerializeField] private float hang = 2.3f;

    [Tooltip("Distancia (horizontal) al punto de enganche a la que Kael se cuelga.")]
    [SerializeField] private float grabRadius = 1.4f;

    private PlayerController player;
    private float travelled;
    private float readyAt;
    private Vector3 pulleyRest;

    /// <summary>True mientras Kael va colgado.</summary>
    public bool IsRiding { get; private set; }

    /// <summary>Configura la tirolina desde código (la usa el constructor de escena).</summary>
    public void Setup(Transform startAnchor, Transform endAnchor, Transform pulleyVisual)
    {
        start = startAnchor;
        end = endAnchor;
        pulley = pulleyVisual;
    }

    private void Awake()
    {
        if (pulley != null)
        {
            pulleyRest = pulley.position;
        }
    }

    private void OnEnable() => GameEvents.PlayerRespawned += OnRespawned;
    private void OnDisable() => GameEvents.PlayerRespawned -= OnRespawned;

    private void OnRespawned(Vector3 position)
    {
        IsRiding = false;
        readyAt = Time.time + 1f;
        if (pulley != null)
        {
            pulley.position = pulleyRest;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        Vector3 line = end.position - start.position;
        float length = line.magnitude;

        if (!IsRiding)
        {
            // ¿Están los pies de Kael bajo el punto de enganche y a su altura?
            Vector3 grab = start.position + Vector3.down * hang;
            Vector3 offset = player.transform.position - grab;
            float height = offset.y;
            offset.y = 0f;
            if (Time.time >= readyAt && !player.IsInputLocked && offset.magnitude < grabRadius && Mathf.Abs(height) < 1.6f)
            {
                IsRiding = true;
                travelled = 0f;
                player.IsCarried = true;
            }

            return;
        }

        travelled = Mathf.Min(length, travelled + speed * Time.deltaTime);
        Vector3 onCable = start.position + line / length * travelled;
        if (pulley != null)
        {
            pulley.position = onCable;
        }

        // Le pide a Kael ir justo al punto colgado del cable (velocidad = distancia que falta / tiempo).
        Vector3 desired = onCable + Vector3.down * hang;
        player.ExternalVelocity = (desired - player.transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);

        if (travelled >= length)
        {
            IsRiding = false;
            player.IsCarried = false;
            readyAt = Time.time + 2f; // que no se vuelva a enganchar al soltarse
            if (pulley != null)
            {
                pulley.position = pulleyRest;
            }
        }
    }
}
