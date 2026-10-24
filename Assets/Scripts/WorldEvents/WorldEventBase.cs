using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Clase base de todo lo que cambia el mundo al recoger un fragmento (puente, escalera, plataforma...).
///   Escucha GameEvents.WorldEventRequested y, si el id coincide con el suyo, ejecuta su animación UNA sola vez.
///
/// POR QUÉ EXISTE
///   Para no repetir en cada variante la parte común: suscribirse, comprobar el id, ejecutar una vez,
///   avisar al terminar. Cada variante solo escribe qué se anima (<see cref="Run"/>).
///
/// CÓMO SE USA
///   Crear una clase hija y sobrescribir:
///     - Prepare(): estado inicial (normalmente, dejar oculto lo que va a aparecer).
///     - Run():     la animación, como corrutina (yield return null para esperar un frame).
///   En la escena, el componente debe estar en un objeto ACTIVO (los objetos desactivados no escuchan eventos);
///   lo que se oculta son sus hijos. "Event Id" debe coincidir con el FragmentData que lo dispara.
///   Ejemplo: <see cref="BridgeAppear"/>.
/// </summary>
public abstract class WorldEventBase : MonoBehaviour
{
    [Tooltip("Id del evento. Debe coincidir con el Event Id del FragmentData que lo dispara.")]
    [SerializeField] private string eventId = "";

    [Tooltip("Si está marcado, el jugador no puede moverse mientras dura la animación.")]
    [SerializeField] private bool lockPlayerDuringEvent = false;

    [Tooltip("Opcional: objeto que mira la cámara breve de evento. Si está vacío, no hay cámara de evento.")]
    [SerializeField] private Transform cameraFocus;

    [Tooltip("Segundos que dura la cámara de evento.")]
    [SerializeField] private float cameraShotSeconds = 3f;

    [Tooltip("Opcional: partículas que se lanzan al empezar el evento.")]
    [SerializeField] private ParticleSystem burst;

    private bool hasRun;

    public string EventId => eventId;
    public bool HasRun => hasRun;

    protected virtual void Awake()
    {
        Prepare();
    }

    // virtual: si una variante necesita su propio OnEnable/OnDisable, debe llamar a base.OnEnable()/base.OnDisable().
    protected virtual void OnEnable()
    {
        GameEvents.WorldEventRequested += OnWorldEventRequested;
    }

    protected virtual void OnDisable()
    {
        GameEvents.WorldEventRequested -= OnWorldEventRequested;
    }

    private void OnWorldEventRequested(string requestedId)
    {
        // Idempotencia: cada evento corre una sola vez.
        if (hasRun || requestedId != eventId)
        {
            return;
        }

        hasRun = true;
        StartCoroutine(RunWithNotifications());
    }

    private IEnumerator RunWithNotifications()
    {
        if (lockPlayerDuringEvent)
        {
            GameEvents.RaiseCinematicStarted();
        }

        if (burst != null)
        {
            burst.Play();
        }

        if (cameraFocus != null)
        {
            GameEvents.RaiseEventShotRequested(cameraFocus, cameraShotSeconds);
        }

        yield return Run();

        if (lockPlayerDuringEvent)
        {
            GameEvents.RaiseCinematicEnded();
        }

        GameEvents.RaiseWorldEventFinished(eventId);
    }

    /// <summary>Estado inicial del evento (se llama en Awake). Por defecto no hace nada.</summary>
    protected virtual void Prepare() { }

    /// <summary>La animación del evento. Se ejecuta una sola vez.</summary>
    protected abstract IEnumerator Run();
}
