using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Plataforma que tiembla cuando Kael la pisa, se derrumba poco después y vuelve a aparecer al rato.
///
/// POR QUÉ EXISTE
///   Es la segunda dinámica del nivel 4 (hojas de piedra): obliga a no quedarse quieto. Como reaparece sola
///   y también cuando Kael reaparece, nunca deja el camino cortado.
///
/// CÓMO SE USA
///   Objeto con collider y este script. "Body" es la pieza visual que tiembla (puede ser el mismo objeto).
///   Tiempos ajustables: aviso (Shake Seconds) y regreso (Return Seconds).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CrumblingPlatform : MonoBehaviour
{
    private enum State { Solid, Shaking, Gone }

    [Tooltip("Pieza visual que tiembla. Si está vacío se usa este mismo objeto.")]
    [SerializeField] private Transform body;

    [Tooltip("Segundos que tiembla antes de caer.")]
    [SerializeField] private float shakeSeconds = 0.8f;

    [Tooltip("Segundos que tarda en reaparecer.")]
    [SerializeField] private float returnSeconds = 3f;

    private Collider solidCollider;
    private Renderer[] renderers;
    private PlayerController player;
    private State state = State.Solid;
    private float timer;
    private Vector3 restPosition;

    /// <summary>True si la plataforma existe y se puede pisar (para pruebas).</summary>
    public bool IsSolid => state != State.Gone;

    /// <summary>True mientras tiembla antes de caer (para pruebas).</summary>
    public bool IsShaking => state == State.Shaking;

    private void Awake()
    {
        solidCollider = GetComponent<Collider>();
        if (body == null)
        {
            body = transform;
        }

        renderers = GetComponentsInChildren<Renderer>();
        restPosition = body.localPosition;
    }

    private void OnEnable()
    {
        GameEvents.PlayerRespawned += OnPlayerRespawned;
    }

    private void OnDisable()
    {
        GameEvents.PlayerRespawned -= OnPlayerRespawned;
    }

    private void OnPlayerRespawned(Vector3 position)
    {
        Restore();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        switch (state)
        {
            case State.Solid:
                if (player.GroundCollider == solidCollider)
                {
                    state = State.Shaking;
                    timer = shakeSeconds;
                }
                break;

            case State.Shaking:
                timer -= Time.deltaTime;
                // Temblor: pequeño desplazamiento aleatorio de la pieza visual.
                body.localPosition = restPosition + new Vector3(Random.Range(-0.04f, 0.04f), 0f, Random.Range(-0.04f, 0.04f));
                if (timer <= 0f)
                {
                    SetVisible(false);
                    state = State.Gone;
                    timer = returnSeconds;
                }
                break;

            case State.Gone:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    Restore();
                }
                break;
        }
    }

    private void Restore()
    {
        state = State.Solid;
        body.localPosition = restPosition;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        solidCollider.enabled = visible;
        foreach (Renderer part in renderers)
        {
            part.enabled = visible;
        }
    }
}
