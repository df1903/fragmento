using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Anima el cuerpo humano de Kael (niveles 6 y 7) sin clips de animación: balancea piernas y brazos al caminar,
///   dobla rodillas y codos, inclina el torso al correr, encoge las piernas al saltar, mece la capa y "respira"
///   cuando está quieto.
///
/// POR QUÉ EXISTE
///   Un cuerpo humano rígido se ve como un muñeco. No usamos Animator ni modelos externos (no están en stack.md),
///   así que el movimiento se calcula con senos: cada pieza gira alrededor de su articulación (un objeto "pivote").
///
/// CÓMO SE USA
///   Va en la raíz de Kael. Cada campo es el pivote de una articulación (lo conecta el constructor de escena).
///   "Stride" controla cuántos pasos da por metro; "Swing" cuánto abre las piernas.
/// </summary>
public class KaelAnimator : MonoBehaviour
{
    [Header("Articulaciones (pivotes)")]
    [SerializeField] private Transform hips;
    [SerializeField] private Transform torso;
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftThigh;
    [SerializeField] private Transform rightThigh;
    [SerializeField] private Transform leftShin;
    [SerializeField] private Transform rightShin;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform leftForearm;
    [SerializeField] private Transform rightForearm;
    [SerializeField] private Transform cape;

    [Header("Ajustes")]
    [Tooltip("Radianes de ciclo por metro recorrido: más alto = pasos más cortos y rápidos.")]
    [SerializeField] private float stride = 2.6f;

    [Tooltip("Grados que abre las piernas a velocidad de caminar.")]
    [SerializeField] private float swing = 32f;

    private PlayerController player;
    private float phase;
    private float blend;     // 0 = quieto, 1 = caminando
    private float airBlend;  // 0 = en el suelo, 1 = en el aire
    private Vector3 hipsRest;

    /// <summary>Conecta las articulaciones desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform hipsPivot, Transform torsoPivot, Transform headPivot, Transform[] thighs, Transform[] shins,
        Transform[] arms, Transform[] forearms, Transform capePivot)
    {
        hips = hipsPivot;
        torso = torsoPivot;
        head = headPivot;
        leftThigh = thighs[0];
        rightThigh = thighs[1];
        leftShin = shins[0];
        rightShin = shins[1];
        leftArm = arms[0];
        rightArm = arms[1];
        leftForearm = forearms[0];
        rightForearm = forearms[1];
        cape = capePivot;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (hips != null)
        {
            hipsRest = hips.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (player == null || hips == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        float speed = player.HorizontalSpeed;
        bool grounded = player.IsGrounded;

        blend = Mathf.MoveTowards(blend, grounded ? Mathf.Clamp01(speed / 5f) : 0f, dt * 6f);
        airBlend = Mathf.MoveTowards(airBlend, grounded ? 0f : 1f, dt * 8f);
        phase += speed * stride * dt;

        float run = Mathf.Clamp01((speed - 7f) / 3.5f); // 0 caminando, 1 corriendo
        float amplitude = swing * (1f + 0.35f * run) * blend;
        float step = Mathf.Sin(phase);
        float breath = Mathf.Sin(Time.time * 1.6f);

        // Piernas: una adelante y otra atrás. La rodilla se dobla cuando la pierna va hacia atrás.
        SetX(leftThigh, step * amplitude - 35f * airBlend);
        SetX(rightThigh, -step * amplitude - 15f * airBlend);
        SetX(leftShin, Mathf.Max(0f, -step) * amplitude * 1.3f + 55f * airBlend);
        SetX(rightShin, Mathf.Max(0f, step) * amplitude * 1.3f + 35f * airBlend);

        // Brazos: al revés que las piernas. En el aire se abren un poco.
        SetX(leftArm, -step * amplitude * 0.8f - 25f * airBlend, 8f + 30f * airBlend);
        SetX(rightArm, step * amplitude * 0.8f - 25f * airBlend, -8f - 30f * airBlend);
        SetX(leftForearm, -(12f + 30f * blend + 25f * run));
        SetX(rightForearm, -(12f + 30f * blend + 25f * run));

        // Torso: se inclina hacia delante al moverse y respira cuando está quieto.
        SetX(torso, 4f * blend + 10f * run + breath * 1.2f * (1f - blend));
        SetX(head, -3f * blend - 6f * run);

        // Cadera: sube y baja dos veces por ciclo (un rebote por paso).
        hips.localPosition = hipsRest + Vector3.up * (Mathf.Abs(Mathf.Cos(phase)) * 0.05f * blend + breath * 0.008f * (1f - blend));

        // Capa: se levanta con la velocidad y ondea.
        if (cape != null)
        {
            float lift = Mathf.Clamp(speed * 4f, 0f, 48f) + 20f * airBlend;
            SetX(cape, 6f + lift + Mathf.Sin(Time.time * 5f) * (2f + 4f * blend));
        }
    }

    private static void SetX(Transform joint, float degreesX, float degreesZ = 0f)
    {
        if (joint != null)
        {
            joint.localRotation = Quaternion.Euler(degreesX, 0f, degreesZ);
        }
    }
}
