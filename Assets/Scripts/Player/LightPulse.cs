using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QUÉ HACE
///   Habilidad de Kael desde el nivel 6: al pulsar F (o el botón oeste del mando; la E ya gira la cámara) suelta un Pulso de luz, un
///   anillo que se expande a su alrededor. Avisa con GameEvents.LightPulsed para que el mundo reaccione.
///
/// POR QUÉ EXISTE
///   Kael ya no solo huye: su luz es una herramienta. El Pulso enciende faros y faroles (Beacon), aturde a los
///   lobos de sombra (ShadowWolf) y congela a los Ecos (Echo). Tiene recarga para que haya que elegir cuándo usarlo.
///
/// CÓMO SE USA
///   Va en el prefab de Kael (lo añade el constructor en las etapas 6 y 7). "Ring" es el anillo visual (opcional).
///   Las pruebas pueden lanzarlo con Trigger().
/// </summary>
public class LightPulse : MonoBehaviour
{
    [Tooltip("Radio del Pulso en metros.")]
    [SerializeField] private float radius = 9f;

    [Tooltip("Segundos entre dos Pulsos.")]
    [SerializeField] private float cooldown = 3f;

    [Tooltip("Anillo visual que se expande (opcional).")]
    [SerializeField] private Transform ring;

    [Tooltip("Luz que destella con el Pulso (opcional).")]
    [SerializeField] private Light flash;

    private InputAction pulseAction;
    private PlayerController player;
    private float readyAt;
    private float ringTimer = 1f; // 0 = recién lanzado, 1 = terminado
    private float flashBase;

    private const float RingSeconds = 0.45f;

    public float Radius => radius;

    /// <summary>True si el Pulso ya se puede volver a lanzar.</summary>
    public bool IsReady => Time.time >= readyAt;

    /// <summary>Configura las piezas visuales desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform ringTransform, Light flashLight)
    {
        ring = ringTransform;
        flash = flashLight;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        pulseAction = new InputAction("Pulse", InputActionType.Button);
        pulseAction.AddBinding("<Keyboard>/f");
        pulseAction.AddBinding("<Gamepad>/buttonWest");

        if (ring != null)
        {
            ring.gameObject.SetActive(false);
        }

        if (flash != null)
        {
            flashBase = flash.intensity;
        }
    }

    private void OnEnable() => pulseAction.Enable();
    private void OnDisable() => pulseAction.Disable();
    private void OnDestroy() => pulseAction.Dispose();

    private void Update()
    {
        bool locked = player != null && player.IsInputLocked;
        if (!locked && pulseAction.WasPressedThisFrame())
        {
            Trigger();
        }

        AnimateRing();
    }

    /// <summary>Lanza el Pulso si está listo. Devuelve true si se lanzó.</summary>
    public bool Trigger()
    {
        if (!IsReady)
        {
            return false;
        }

        readyAt = Time.time + cooldown;
        ringTimer = 0f;
        GameEvents.RaiseLightPulsed(transform.position + Vector3.up * 0.9f, radius);
        return true;
    }

    // El anillo crece de 0 al diámetro del Pulso y desaparece; la luz de Kael destella y vuelve a su brillo.
    private void AnimateRing()
    {
        if (ringTimer >= 1f)
        {
            return;
        }

        ringTimer = Mathf.Min(1f, ringTimer + Time.deltaTime / RingSeconds);
        if (ring != null)
        {
            ring.gameObject.SetActive(ringTimer < 1f);
            float size = radius * 2f * Mathf.SmoothStep(0f, 1f, ringTimer);
            ring.localScale = new Vector3(size, ring.localScale.y, size);
        }

        if (flash != null)
        {
            flash.intensity = flashBase * (1f + 3f * (1f - ringTimer));
        }
    }
}
