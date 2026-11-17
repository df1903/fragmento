using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Parche de zarzas en el suelo que brota y se esconde por turnos. Antes de brotar avisa: las puntas asoman y
///   brillan. Mata a Kael si está encima cuando las zarzas están fuera.
///
/// POR QUÉ EXISTE
///   Es el peligro propio del nivel 4 (Jardín): un obstáculo de ritmo. Se cruza esperando el momento, no
///   corriendo ni escondiéndose.
///
/// CÓMO SE USA
///   La raíz se coloca en el suelo, en el centro del parche. "Spikes" es el hijo con las púas (se escala en Y).
///   "Size" es el tamaño del parche (ancho X, fondo Z). "Phase Offset" desfasa parches vecinos.
/// </summary>
public class ThornPatch : Hazard
{
    [SerializeField] private Transform spikes;

    [Tooltip("Tamaño del parche en metros (x = ancho, y = fondo en Z).")]
    [SerializeField] private Vector2 size = new Vector2(4f, 3f);

    [Tooltip("Segundos escondidas (se puede pisar).")]
    [SerializeField] private float hiddenSeconds = 2.2f;

    [Tooltip("Segundos de aviso (asoman las puntas, todavía no matan).")]
    [SerializeField] private float warnSeconds = 0.8f;

    [Tooltip("Segundos fuera (matan).")]
    [SerializeField] private float outSeconds = 1.6f;

    [Tooltip("Segundos de desfase respecto a otros parches.")]
    [SerializeField] private float phaseOffset;

    private float clock;

    /// <summary>True cuando las zarzas están fuera y matan (para pruebas).</summary>
    public bool IsOut { get; private set; }

    /// <summary>Configura el parche desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform spikesTransform, Vector2 patchSize, float offset)
    {
        spikes = spikesTransform;
        size = patchSize;
        phaseOffset = offset;
    }

    private void Awake()
    {
        clock = phaseOffset;
        SetSpikes(0.02f);
    }

    protected override void Tick()
    {
        float cycle = hiddenSeconds + warnSeconds + outSeconds;
        clock = (clock + Time.deltaTime) % cycle;

        if (clock < hiddenSeconds)
        {
            IsOut = false;
            SetSpikes(0.02f);
        }
        else if (clock < hiddenSeconds + warnSeconds)
        {
            IsOut = false;
            SetSpikes(0.2f); // asoman las puntas: aviso
        }
        else
        {
            IsOut = true;
            SetSpikes(1f);

            Vector3 local = Player.transform.position - transform.position;
            bool onPatch = Mathf.Abs(local.x) < size.x / 2f && Mathf.Abs(local.z) < size.y / 2f && local.y > -0.5f && local.y < 1f;
            if (onPatch)
            {
                TryKill(PlayerCenter, 1f);
            }
        }
    }

    private void SetSpikes(float height)
    {
        Vector3 scale = spikes.localScale;
        scale.y = height;
        spikes.localScale = scale;
    }
}
