using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Reflector (nivel 8). Un foco que ilumina un cono. Puede estar en una torre y barrer de lado a lado, o ir
///   en un dron que sigue una ruta mirando hacia abajo. Si el cono alcanza a Kael y no hay nada en medio, el foco
///   se pone rojo; si lo sigue viendo un segundo, lo mata. El Pulso de luz lo ciega (apaga) unos segundos si
///   está cerca o si su haz está sobre Kael en ese momento.
///
/// POR QUÉ EXISTE
///   Es un enemigo de cobertura y ritmo: no persigue, vigila. Se cruza esperando a que el haz pase, o a cubierto
///   detrás de coches y muros (el rayo de visión choca con sus colliders). Distinto a lobos (sigilo) y Ecos (mirar).
///
/// CÓMO SE USA
///   "Head" es la pieza que apunta (su +Z es el centro del cono). "Spot" es la luz (opcional). Para una torre:
///   "Sweep Degrees" y "Sweep Speed". Para un dron: "Route" con puntos (la raíz viaja entre ellos).
/// </summary>
public class Searchlight : Hazard
{
    [Header("Piezas")]
    [SerializeField] private Transform head;
    [SerializeField] private Light spot;
    [SerializeField] private Renderer beam;

    [Header("Cono")]
    [SerializeField] private float range = 32f;
    [SerializeField] private float halfAngle = 13f;

    [Header("Barrido (torre)")]
    [Tooltip("Grados a cada lado del centro.")]
    [SerializeField] private float sweepDegrees = 40f;
    [Tooltip("Ciclos por segundo del barrido (0 = quieto).")]
    [SerializeField] private float sweepSpeed = 0.12f;

    [Header("Ruta (dron)")]
    [SerializeField] private Vector3[] route = new Vector3[0];
    [SerializeField] private float routeSpeed = 4f;

    [Header("Ataque")]
    [Tooltip("Segundos viendo a Kael antes de matarlo.")]
    [SerializeField] private float lockSeconds = 1f;
    [SerializeField] private float blindSeconds = 4f;

    private Quaternion headRest;
    private float sweepPhase;
    private int routeIndex;
    private float seenFor;
    private float blindUntil;
    private Color normalColor;

    /// <summary>True si este fotograma el foco ve a Kael.</summary>
    public bool SeesPlayer { get; private set; }

    /// <summary>True mientras está cegado por el Pulso.</summary>
    public bool IsBlind => Time.time < blindUntil;

    /// <summary>Solo para pruebas: detiene el barrido y la ruta.</summary>
    public bool HoldStill { get; set; }

    /// <summary>Configura el foco desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform headTransform, Light spotLight, Renderer beamRenderer, float sweep, float speed, Vector3[] path)
    {
        head = headTransform;
        spot = spotLight;
        beam = beamRenderer;
        sweepDegrees = sweep;
        sweepSpeed = speed;
        route = path;
    }

    private void Awake()
    {
        headRest = head.localRotation;
        sweepPhase = transform.position.x * 0.13f; // cada foco empieza en un punto distinto del barrido
        if (spot != null)
        {
            normalColor = spot.color;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.LightPulsed += OnLightPulsed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.LightPulsed -= OnLightPulsed;
    }

    private void OnLightPulsed(Vector3 center, float radius)
    {
        // Ciega al foco si está cerca (en horizontal: puede estar alto) o si su haz está iluminando a Kael:
        // la luz de Kael le "devuelve" el destello.
        Vector3 offset = head.position - center;
        offset.y = 0f;
        bool lookingAtKael = Player != null && CanSee();
        if (offset.magnitude <= radius + 4f || lookingAtKael)
        {
            blindUntil = Time.time + blindSeconds;
            seenFor = 0f;
        }
    }

    protected override void Tick()
    {
        bool blind = IsBlind;
        if (spot != null)
        {
            spot.enabled = !blind;
        }

        if (beam != null)
        {
            beam.enabled = !blind;
        }

        if (!HoldStill)
        {
            Move();
        }

        SeesPlayer = !blind && CanSee();
        seenFor = SeesPlayer ? seenFor + Time.deltaTime : 0f;
        if (spot != null)
        {
            spot.color = SeesPlayer ? Color.Lerp(normalColor, new Color(1f, 0.1f, 0.05f), Mathf.Clamp01(seenFor / lockSeconds) * 0.5f + 0.5f) : normalColor;
        }

        if (seenFor >= lockSeconds)
        {
            TryKill(PlayerCenter, 10f); // el radio grande hace que siempre acierte: el aviso ya fue el foco en rojo
            seenFor = 0f;
        }
    }

    private void Move()
    {
        if (route != null && route.Length > 1)
        {
            Vector3 target = route[routeIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, routeSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                routeIndex = (routeIndex + 1) % route.Length;
            }
        }
        else if (sweepSpeed > 0f)
        {
            sweepPhase += sweepSpeed * Time.deltaTime;
            head.localRotation = Quaternion.Euler(0f, Mathf.Sin(sweepPhase * Mathf.PI * 2f) * sweepDegrees, 0f) * headRest;
        }
    }

    // ¿Está Kael dentro del cono y sin nada en medio?
    private bool CanSee()
    {
        Vector3 target = PlayerCenter;
        Vector3 toPlayer = target - head.position;
        float distance = toPlayer.magnitude;
        if (distance > range || Vector3.Angle(head.forward, toPlayer) > halfAngle)
        {
            return false;
        }

        // Rayo hacia Kael: si choca con algo antes de llegar (un coche, un muro), Kael está a cubierto.
        if (Physics.Raycast(head.position + toPlayer / distance * 0.6f, toPlayer, out RaycastHit hit, distance - 0.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PlayerController>() != null;
        }

        return true;
    }
}
