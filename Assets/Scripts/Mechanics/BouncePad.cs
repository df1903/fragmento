using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Lanza a Kael hacia arriba cuando lo pisa (hongo de rebote). El sombrero se aplasta un instante al rebotar.
///
/// POR QUÉ EXISTE
///   Es la dinámica del nivel 4 (Jardín): permite subir a terrazas más altas que un salto normal (1.8 m).
///
/// CÓMO SE USA
///   Objeto con BoxCollider (la zona justo encima del sombrero) y este script. "Bounce Height" es la altura
///   del rebote en metros. "Cap" es la pieza visual que se aplasta (opcional).
/// </summary>
public class BouncePad : PlayerZone
{
    [Tooltip("Altura del rebote en metros.")]
    [SerializeField] private float bounceHeight = 5f;

    [Tooltip("Pieza visual que se aplasta al rebotar (opcional).")]
    [SerializeField] private Transform cap;

    [Tooltip("Segundos mínimos entre dos rebotes.")]
    [SerializeField] private float cooldown = 0.25f;

    private float readyAt;
    private float squash; // 1 = recién pisado, 0 = en reposo
    private Vector3 capScale;

    /// <summary>Rebotes dados desde que existe (para pruebas y depuración).</summary>
    public int Bounces { get; private set; }

    /// <summary>Configura el hongo desde código (lo usa el constructor de escena).</summary>
    public void Setup(float height, Transform capTransform)
    {
        bounceHeight = height;
        cap = capTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        if (cap != null)
        {
            capScale = cap.localScale;
        }
    }

    protected override void WhileInside(PlayerController kael)
    {
        if (Time.time < readyAt)
        {
            return;
        }

        readyAt = Time.time + cooldown;
        kael.Launch(bounceHeight);
        Bounces++;
        squash = 1f;
    }

    private void LateUpdate()
    {
        if (cap == null || squash <= 0f)
        {
            return;
        }

        // El sombrero se aplasta y vuelve a su forma en un cuarto de segundo.
        squash = Mathf.Max(0f, squash - Time.deltaTime * 4f);
        cap.localScale = new Vector3(capScale.x * (1f + 0.2f * squash), capScale.y * (1f - 0.5f * squash), capScale.z * (1f + 0.2f * squash));
    }
}
