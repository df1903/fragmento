using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Esquirla de cristal disparada por una <see cref="CrystalTurret"/>: avanza en línea recta, mata a Kael si
///   lo toca y desaparece al chocar con el escenario o pasados unos segundos.
///
/// POR QUÉ EXISTE
///   Es el peligro "a distancia" del nivel 2. Como es lento y recto, se esquiva; no usa Rigidbody: se
///   mueve por código y detecta paredes con un rayo, igual de fácil de explicar que el resto de enemigos.
///
/// CÓMO SE USA
///   Es un prefab (Assets/Prefabs/Proyectil.prefab, lo genera el constructor de escenas). La torreta lo
///   instancia y llama a Launch(). Hereda las reglas de justicia de <see cref="Hazard"/>.
/// </summary>
public class TurretProjectile : Hazard
{
    [SerializeField] private float speed = 12f;

    [Tooltip("Distancia a Kael a la que lo mata.")]
    [SerializeField] private float hitRadius = 0.6f;

    [Tooltip("Segundos de vida máxima.")]
    [SerializeField] private float lifetime = 6f;

    private float age;

    /// <summary>Velocidad en metros por segundo (la torreta puede cambiarla).</summary>
    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    protected override void Tick()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;

        // Rayo hacia delante: si hay escenario sólido en el camino, el proyectil se rompe contra él.
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step + 0.2f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerController>() == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;

        if (TryKill(transform.position, hitRadius))
        {
            Destroy(gameObject);
        }
    }
}
