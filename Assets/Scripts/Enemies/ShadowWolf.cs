using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Lobo de sombra (nivel 6). Ronda alrededor de su guarida. Si ve a Kael (o lo oye correr) lo persigue más
///   rápido de lo que Kael camina. Lo mata si lo alcanza. Le teme a la luz: no entra en la zona de un faro
///   encendido y el Pulso de luz lo aturde unos segundos.
///
/// POR QUÉ EXISTE
///   Es un enemigo de sigilo, distinto a las sombras que embisten: se evita escondiéndose en la hierba alta,
///   se deja atrás corriendo y se frena con la luz. Sigue el terreno con un rayo hacia abajo (sin NavMesh).
///
/// CÓMO SE USA
///   Se coloca en su guarida (posición inicial). "Body" es el hijo visual que gira hacia donde va.
///   Números ajustables: vista, oído, velocidades y segundos de aturdimiento.
/// </summary>
public class ShadowWolf : Hazard
{
    private enum State { Prowl, Chase, Stunned }

    [SerializeField] private Transform body;

    [Header("Sentidos")]
    [Tooltip("Distancia a la que ve a Kael si no está escondido.")]
    [SerializeField] private float sightRange = 14f;

    [Tooltip("Distancia a la que oye a Kael cuando corre.")]
    [SerializeField] private float hearRange = 22f;

    [Tooltip("Distancia a la que deja de perseguir.")]
    [SerializeField] private float loseRange = 28f;

    [Header("Movimiento")]
    [SerializeField] private float prowlSpeed = 2.2f;

    [Tooltip("Velocidad de persecución. Kael camina a 7 y corre a 10.5.")]
    [SerializeField] private float chaseSpeed = 8.2f;

    [Tooltip("Radio en el que ronda alrededor de su guarida.")]
    [SerializeField] private float prowlRadius = 7f;

    [Header("Ataque y luz")]
    [SerializeField] private float killRadius = 1.1f;
    [SerializeField] private float stunSeconds = 3.5f;

    private State state = State.Prowl;
    private Vector3 den;
    private Vector3 prowlTarget;
    private float timer;

    public bool IsChasing => state == State.Chase;
    public bool IsStunned => state == State.Stunned;

    /// <summary>Configura el lobo desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform bodyTransform, float radius)
    {
        body = bodyTransform;
        prowlRadius = radius;
    }

    private void Awake()
    {
        den = transform.position;
        prowlTarget = den;
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
        if (Vector3.Distance(center, transform.position) <= radius)
        {
            state = State.Stunned;
            timer = stunSeconds;
        }
    }

    protected override void Tick()
    {
        Vector3 toPlayer = Player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        switch (state)
        {
            case State.Prowl:
                if (CanSense(distance))
                {
                    state = State.Chase;
                    break;
                }

                // Ronda: camina hacia un punto al azar cerca de la guarida y elige otro al llegar.
                timer -= Time.deltaTime;
                if (timer <= 0f || FlatDistance(transform.position, prowlTarget) < 0.5f)
                {
                    Vector2 random = Random.insideUnitCircle * prowlRadius;
                    prowlTarget = den + new Vector3(random.x, 0f, random.y);
                    timer = 4f;
                }
                MoveTowards(prowlTarget, prowlSpeed);
                break;

            case State.Chase:
                // Pierde el rastro si Kael se aleja, se esconde en la hierba o se refugia en un faro.
                bool hidden = PlayerStealth.IsHidden && distance > 3f;
                if (distance > loseRange || hidden || Beacon.IsInSafeLight(Player.transform.position))
                {
                    state = State.Prowl;
                    timer = 0f;
                    break;
                }

                MoveTowards(Player.transform.position, chaseSpeed);
                TryKill(transform.position + Vector3.up * 0.7f, killRadius);
                break;

            case State.Stunned:
                timer -= Time.deltaTime;
                if (body != null)
                {
                    body.localRotation = Quaternion.Euler(0f, body.localEulerAngles.y, Mathf.Sin(Time.time * 30f) * 6f); // tiembla
                }
                if (timer <= 0f)
                {
                    state = State.Prowl;
                    timer = 0f;
                }
                break;
        }
    }

    private bool CanSense(float distance)
    {
        if (Beacon.IsInSafeLight(Player.transform.position))
        {
            return false;
        }

        if (Player.IsSprinting && distance < hearRange)
        {
            return true; // lo oye aunque esté en la hierba
        }

        return distance < sightRange && !PlayerStealth.IsHidden;
    }

    // Camina en horizontal hacia el destino, sin entrar en la luz de un faro, y se pega al suelo con un rayo.
    private void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();
        Vector3 next = transform.position + direction * speed * Time.deltaTime;
        if (Beacon.IsInSafeLight(next))
        {
            return; // la luz lo frena en el borde
        }

        if (Physics.Raycast(next + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            next.y = hit.point.y;
        }

        transform.position = next;
        if (body != null)
        {
            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
            body.rotation = Quaternion.RotateTowards(body.rotation, look, 540f * Time.deltaTime);
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
