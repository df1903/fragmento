using UnityEngine;

/// <summary>
/// QUÉ HACE
///   La Sombra del Olvido (nivel 10): una marea de oscuridad que, al llegar su evento, empieza a subir desde el
///   suelo y llena la sala. Mata a Kael si lo cubre. Las luces que quedan por debajo se apagan a su paso.
///   Si Kael reaparece, la sombra vuelve al suelo y empieza a subir otra vez tras una pausa.
///
/// POR QUÉ EXISTE
///   Es el clímax del juego: la última huida, escaleras arriba, hacia la luz del amanecer. Usa las reglas de
///   siempre (Hazard): quieta durante las cámaras de evento y con gracia al reaparecer.
///
/// CÓMO SE USA
///   Este script va en la raíz de la sombra (la lámina oscura es su hijo). Su altura inicial es el punto de
///   partida; sube "Rise Speed" m/s hasta "Max Height". "Event Id" es el evento que la despierta.
///   "Lights" son las luces que se apagan cuando la sombra las cubre.
/// </summary>
public class RisingShadow : Hazard
{
    [SerializeField] private string eventId = "";

    [Tooltip("Metros por segundo que sube.")]
    [SerializeField] private float riseSpeed = 1f;

    [Tooltip("Altura (en el mundo) a la que se detiene.")]
    [SerializeField] private float maxHeight = 22f;

    [Tooltip("Segundos de espera antes de subir (al despertar y tras cada reaparición).")]
    [SerializeField] private float startDelay = 2f;

    [Tooltip("Luces que se apagan cuando la sombra pasa por encima.")]
    [SerializeField] private Light[] lights = new Light[0];

    private float startY;
    private float resumeAt;

    /// <summary>True desde que llega su evento.</summary>
    public bool IsAwake { get; private set; }

    /// <summary>Altura actual de la superficie de la sombra.</summary>
    public float SurfaceY => transform.position.y;

    public void Setup(string id, float speed, float top, Light[] dimmedLights)
    {
        eventId = id;
        riseSpeed = speed;
        maxHeight = top;
        lights = dimmedLights;
    }

    private void Awake()
    {
        startY = transform.position.y;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.WorldEventRequested += OnWorldEventRequested;
        GameEvents.PlayerRespawned += OnRespawned;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.WorldEventRequested -= OnWorldEventRequested;
        GameEvents.PlayerRespawned -= OnRespawned;
    }

    private void OnWorldEventRequested(string id)
    {
        if (id == eventId && !IsAwake)
        {
            IsAwake = true;
            resumeAt = Time.time + startDelay;
            GameEvents.RaiseMessageRequested("¡La Sombra del Olvido sube! Corre escaleras arriba, hacia la luz.");
        }
    }

    // Al reaparecer, la sombra vuelve al suelo: nunca deja a Kael atrapado bajo ella.
    private void OnRespawned(Vector3 position)
    {
        SetHeight(startY);
        resumeAt = Time.time + startDelay;
    }

    protected override void Tick()
    {
        if (!IsAwake || Time.time < resumeAt)
        {
            return;
        }

        SetHeight(Mathf.Min(maxHeight, SurfaceY + riseSpeed * Time.deltaTime));
        if (Player.transform.position.y < SurfaceY - 0.15f)
        {
            TryKill(PlayerCenter, 10f);
        }
    }

    private void SetHeight(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;

        foreach (Light dimmed in lights)
        {
            if (dimmed != null)
            {
                dimmed.enabled = dimmed.transform.position.y > y + 0.5f;
            }
        }
    }
}
