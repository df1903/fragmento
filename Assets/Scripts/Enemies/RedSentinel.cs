using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Enemigo rojo: patrulla entre dos puntos y, si Kael se acerca, lo persigue. Cuando está cerca avisa
///   (se detiene y brilla), embiste en línea recta y se queda un momento agotado. Si toca a Kael, Kael muere
///   (GameEvents.PlayerDied). Nunca sale del área permitida, así que no cae al vacío.
///
/// POR QUÉ EXISTE
///   Da dureza al inicio del nivel. Es una "máquina de estados" mínima (Patrulla, Persecución, Aviso,
///   Embestida, Descanso) sin NavMesh ni paquetes extra. El aviso y el descanso hacen que sea justo:
///   siempre se puede esquivar si se reacciona.
///
/// CÓMO SE USA
///   Lo coloca el constructor de escenas. Ajustes en el Inspector: radios, velocidades y tiempos.
///   "Can Lunge" apaga la embestida (queda solo patrulla y persecución). Hereda las reglas de justicia de
///   <see cref="Hazard"/> (quieto en cámaras de evento, gracia al reaparecer).
/// </summary>
public class RedSentinel : Hazard
{
    private enum State { Patrol, Chase, Windup, Dash, Recover }

    [Header("Patrulla")]
    [SerializeField] private Vector3 pointA;
    [SerializeField] private Vector3 pointB;
    [SerializeField] private float patrolSpeed = 2.5f;

    [Header("Persecución")]
    [Tooltip("Distancia a la que ve a Kael y empieza a perseguirlo.")]
    [SerializeField] private float detectRadius = 9f;

    [Tooltip("Si Kael se aleja más que esto, deja de perseguirlo y vuelve a patrullar.")]
    [SerializeField] private float loseRadius = 13f;

    [SerializeField] private float chaseSpeed = 4.5f;

    [Tooltip("Zona (en coordenadas del mundo) de la que el enemigo no puede salir.")]
    [SerializeField] private Bounds allowedArea = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f));

    [Header("Embestida")]
    [SerializeField] private bool canLunge = true;

    [Tooltip("A esta distancia de Kael, el enemigo se detiene y avisa.")]
    [SerializeField] private float lungeRadius = 5f;

    [Tooltip("Segundos de aviso antes de embestir (el jugador debe reaccionar).")]
    [SerializeField] private float windupSeconds = 0.5f;

    [SerializeField] private float dashSpeed = 11f;
    [SerializeField] private float dashSeconds = 0.5f;

    [Tooltip("Segundos quieto y vulnerable tras embestir.")]
    [SerializeField] private float recoverSeconds = 0.8f;

    [Header("Contacto")]
    [Tooltip("Distancia a Kael a la que lo mata.")]
    [SerializeField] private float touchRadius = 1.1f;

    private State state = State.Patrol;
    private Vector3 patrolTarget;
    private Vector3 dashDirection;
    private float stateTimer;
    private EnemyVisual visual;

    /// <summary>True si está persiguiendo o atacando a Kael (para pruebas y depuración).</summary>
    public bool IsChasing => state != State.Patrol;

    /// <summary>True durante el aviso previo a la embestida (para pruebas y depuración).</summary>
    public bool IsWindingUp => state == State.Windup;

    /// <summary>True mientras embiste (para pruebas y depuración).</summary>
    public bool IsDashing => state == State.Dash;

    /// <summary>Configura la patrulla desde código (la usa el constructor de escena).</summary>
    public void Setup(Vector3 a, Vector3 b, Bounds area)
    {
        pointA = a;
        pointB = b;
        allowedArea = area;
        patrolTarget = b;
    }

    private void Awake()
    {
        patrolTarget = pointB;
        visual = GetComponent<EnemyVisual>();
    }

    protected override void Tick()
    {
        Vector3 kael = PlayerCenter;
        float distance = Vector3.Distance(transform.position, kael);
        bool kaelInArea = allowedArea.Contains(kael);

        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (distance < detectRadius && kaelInArea)
                {
                    state = State.Chase;
                }
                break;

            case State.Chase:
                MoveTowards(new Vector3(kael.x, transform.position.y, kael.z), chaseSpeed);
                if (distance > loseRadius || !kaelInArea)
                {
                    state = State.Patrol;
                    patrolTarget = pointA;
                }
                else if (canLunge && distance < lungeRadius)
                {
                    StartWindup();
                }
                break;

            case State.Windup:
                // Quieto y brillando. La dirección se fija al final del aviso: moverse a un lado la esquiva.
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    dashDirection = Vector3.ProjectOnPlane(kael - transform.position, Vector3.up).normalized;
                    stateTimer = dashSeconds;
                    state = State.Dash;
                    SetAlert(false);
                }
                break;

            case State.Dash:
                Move(dashDirection * dashSpeed * Time.deltaTime);
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    stateTimer = recoverSeconds;
                    state = State.Recover;
                }
                break;

            case State.Recover:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    state = State.Chase;
                }
                break;
        }

        TryKill(transform.position, touchRadius);
    }

    private void Patrol()
    {
        MoveTowards(patrolTarget, patrolSpeed);
        if ((transform.position - patrolTarget).sqrMagnitude < 0.04f)
        {
            patrolTarget = patrolTarget == pointA ? pointB : pointA;
        }
    }

    private void StartWindup()
    {
        state = State.Windup;
        stateTimer = windupSeconds;
        SetAlert(true);
    }

    private void SetAlert(bool alert)
    {
        if (visual != null)
        {
            visual.IsAlert = alert;
        }
    }

    // Avanza hacia el objetivo sin salir del área permitida.
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        Move(next - transform.position);
    }

    private void Move(Vector3 delta)
    {
        Vector3 next = transform.position + delta;
        next.x = Mathf.Clamp(next.x, allowedArea.min.x, allowedArea.max.x);
        next.z = Mathf.Clamp(next.z, allowedArea.min.z, allowedArea.max.z);
        transform.position = next;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetAlert(false);
    }
}
