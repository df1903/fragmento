using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Eco (nivel 7): la silueta de un aldeano que ya no está. SOLO se mueve cuando la cámara no lo mira. Si llega
///   hasta Kael, lo mata. El Pulso de luz lo deja inmóvil unos segundos y no cruza la luz de un farol encendido.
///
/// POR QUÉ EXISTE
///   Es un enemigo de tensión: no se le gana corriendo, sino vigilándolo. Obliga a girar la cámara (Q/E o clic
///   derecho) y a decidir a cuál mirar cuando hay varios. La regla es una sola comparación: ¿está en pantalla?
///
/// CÓMO SE USA
///   Se coloca de pie en la calle. "Body" es el hijo visual que gira hacia Kael. "Wake Range" es la distancia a la
///   que despierta. Las pruebas pueden forzar "visto / no visto" con SeenOverride.
/// </summary>
public class Echo : Hazard
{
    [SerializeField] private Transform body;

    [Tooltip("Distancia a la que empieza a acechar a Kael.")]
    [SerializeField] private float wakeRange = 26f;

    [Tooltip("Velocidad cuando nadie lo mira. Kael camina a 7.")]
    [SerializeField] private float speed = 5f;

    [SerializeField] private float killRadius = 1f;

    [Tooltip("Segundos inmóvil tras un Pulso de luz.")]
    [SerializeField] private float pulseFreezeSeconds = 4f;

    private Camera view;
    private float frozenUntil;

    /// <summary>Solo para pruebas: fuerza si se le ve (true), no se le ve (false) o se calcula con la cámara (null).</summary>
    public bool? SeenOverride { get; set; }

    /// <summary>True si la cámara lo tiene en pantalla ahora mismo.</summary>
    public bool IsSeen { get; private set; }

    /// <summary>True mientras dura el efecto del Pulso de luz.</summary>
    public bool IsDazzled => Time.time < frozenUntil;

    /// <summary>Configura el Eco desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform bodyTransform)
    {
        body = bodyTransform;
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
            frozenUntil = Time.time + pulseFreezeSeconds;
        }
    }

    protected override void Tick()
    {
        IsSeen = SeenOverride ?? IsOnScreen();

        Vector3 toPlayer = Player.transform.position - transform.position;
        float heightGap = Mathf.Abs(toPlayer.y);
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // Quieto si lo miran, si está deslumbrado, si Kael está lejos o en otra planta (tejados).
        if (IsSeen || IsDazzled || distance > wakeRange || heightGap > 2.5f || distance < 0.05f)
        {
            return;
        }

        Vector3 direction = toPlayer / distance;
        Vector3 next = transform.position + direction * speed * Time.deltaTime;
        if (!Beacon.IsInSafeLight(next))
        {
            if (Physics.Raycast(next + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                next.y = hit.point.y;
            }

            transform.position = next;
        }

        if (body != null)
        {
            body.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        TryKill(transform.position + Vector3.up * 0.9f, killRadius);
    }

    // ¿El pecho del Eco cae dentro de la imagen de la cámara? (delante de ella y dentro de los bordes de la pantalla)
    private bool IsOnScreen()
    {
        if (view == null)
        {
            view = Camera.main;
            if (view == null)
            {
                return false;
            }
        }

        Vector3 point = view.WorldToViewportPoint(transform.position + Vector3.up * 1.2f);
        return point.z > 0f && point.x > 0f && point.x < 1f && point.y > 0f && point.y < 1f;
    }
}
