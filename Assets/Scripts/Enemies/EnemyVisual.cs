using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Da vida al aspecto de un enemigo o torreta sin tocar su lógica: el cuerpo flota y gira, y el brillo
///   emisivo late; cuando está en "alerta" (aviso antes de atacar) brilla mucho más y vibra.
///
/// POR QUÉ EXISTE
///   Separa lo visual de lo que mata. Así el aviso se ve claro (el jugador puede reaccionar) y la lógica
///   (<see cref="RedSentinel"/>, <see cref="CrystalTurret"/>) no se llena de código de animación.
///
/// CÓMO SE USA
///   Va en el objeto raíz del enemigo. "Body" es el hijo que se mueve; si está vacío se anima este mismo objeto.
///   El script que ataca pone IsAlert = true durante el aviso. Todo se calcula con funciones seno, sin Animator.
/// </summary>
public class EnemyVisual : MonoBehaviour
{
    [Tooltip("Hijo que flota y gira (no la raíz, que mueve la lógica).")]
    [SerializeField] private Transform body;

    [Tooltip("Cuánto sube y baja el cuerpo, en metros.")]
    [SerializeField] private float bobHeight = 0.15f;

    [Tooltip("Velocidad de giro del cuerpo, en grados por segundo (0 = no gira).")]
    [SerializeField] private float spinSpeed = 40f;

    [Tooltip("Brillo normal (multiplica el color emisivo del material).")]
    [SerializeField] private float baseGlow = 1f;

    [Tooltip("Brillo durante el aviso de ataque.")]
    [SerializeField] private float alertGlow = 4f;

    /// <summary>Lo pone la lógica de ataque: true durante el aviso.</summary>
    public bool IsAlert { get; set; }

    private Renderer[] renderers;
    private Color[] baseEmission;
    private MaterialPropertyBlock block;
    private Vector3 bodyStart;
    private float phase;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (body == null)
        {
            body = transform;
        }

        bodyStart = body.localPosition;
        phase = Random.value * 6.28f; // cada enemigo late desfasado
        block = new MaterialPropertyBlock();

        renderers = GetComponentsInChildren<Renderer>();
        baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            baseEmission[i] = material != null && material.HasProperty(EmissionColorId) ? material.GetColor(EmissionColorId) : Color.black;
        }
    }

    private void Update()
    {
        float t = Time.time + phase;

        float shake = IsAlert ? Mathf.Sin(t * 60f) * 0.04f : 0f;
        body.localPosition = bodyStart + new Vector3(shake, Mathf.Sin(t * 2f) * bobHeight, 0f);
        body.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        float glow = IsAlert ? alertGlow * (0.75f + 0.25f * Mathf.Sin(t * 30f)) : baseGlow * (0.8f + 0.2f * Mathf.Sin(t * 3f));
        for (int i = 0; i < renderers.Length; i++)
        {
            if (baseEmission[i].maxColorComponent <= 0f)
            {
                continue; // piezas sin emisión (el cuerpo oscuro) no brillan
            }

            renderers[i].GetPropertyBlock(block);
            block.SetColor(EmissionColorId, baseEmission[i] * glow);
            renderers[i].SetPropertyBlock(block);
        }
    }
}
