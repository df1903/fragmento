using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Torreta de cristal: gira su cabeza hacia Kael cuando está en rango y lo ve (sin paredes en medio), brilla un
///   momento como aviso y dispara uno o varios <see cref="TurretProjectile"/> en la dirección en la que apunta.
///
/// POR QUÉ EXISTE
///   Obliga a usar las coberturas del nivel 2 y a moverse en vez de solo esquivar enemigos cuerpo a cuerpo.
///   La puntería es lenta a propósito y el disparo sale recto hacia donde miraba: moverse lo evita.
///
/// CÓMO SE USA
///   Lo coloca el constructor de escenas. "Head" gira (eje vertical y horizontal), "Muzzle" es el punto de salida
///   (un hijo de Head mirando hacia delante). "Burst Count" 1 = disparo único, 3 = ráfaga.
///   Hereda las reglas de justicia de <see cref="Hazard"/> (quieta en cámaras de evento).
/// </summary>
public class CrystalTurret : Hazard
{
    private enum State { Idle, Windup, Firing }

    [Header("Piezas")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform muzzle;
    [SerializeField] private TurretProjectile projectilePrefab;

    [Header("Puntería")]
    [Tooltip("Distancia máxima a la que ve a Kael.")]
    [SerializeField] private float range = 28f;

    [Tooltip("Grados por segundo a los que gira la cabeza (lento = esquivable).")]
    [SerializeField] private float turnSpeed = 90f;

    [Header("Disparo")]
    [Tooltip("Segundos entre ataques.")]
    [SerializeField] private float cooldown = 2.5f;

    [Tooltip("Segundos de aviso (brillo) antes de disparar.")]
    [SerializeField] private float windupSeconds = 0.6f;

    [Tooltip("1 = disparo único, 3 = ráfaga.")]
    [SerializeField] private int burstCount = 1;

    [SerializeField] private float burstSpacing = 0.25f;
    [SerializeField] private float projectileSpeed = 12f;

    private State state = State.Idle;
    private float timer;
    private int shotsLeftInBurst;
    private EnemyVisual visual;

    /// <summary>Proyectiles disparados desde que existe la torreta (para pruebas y depuración).</summary>
    public int ShotsFired { get; private set; }

    /// <summary>True durante el aviso previo al disparo (para pruebas y depuración).</summary>
    public bool IsWindingUp => state == State.Windup;

    /// <summary>Configura la torreta desde código (la usa el constructor de escena).</summary>
    public void Setup(Transform headTransform, Transform muzzleTransform, TurretProjectile prefab, int burst, float cooldownSeconds)
    {
        head = headTransform;
        muzzle = muzzleTransform;
        projectilePrefab = prefab;
        burstCount = burst;
        cooldown = cooldownSeconds;
    }

    private void Awake()
    {
        visual = GetComponent<EnemyVisual>();
        timer = cooldown * 0.5f; // la primera salva no es inmediata
    }

    protected override void Tick()
    {
        Vector3 target = PlayerCenter;
        bool canSee = Vector3.Distance(head.position, target) <= range && HasLineOfSight(target);

        if (canSee)
        {
            Aim(target);
        }

        switch (state)
        {
            case State.Idle:
                timer -= Time.deltaTime;
                if (canSee && timer <= 0f)
                {
                    state = State.Windup;
                    timer = windupSeconds;
                    SetAlert(true);
                }
                break;

            case State.Windup:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    SetAlert(false);
                    shotsLeftInBurst = Mathf.Max(burstCount, 1);
                    timer = 0f;
                    state = State.Firing;
                }
                break;

            case State.Firing:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    Fire();
                    shotsLeftInBurst--;
                    timer = burstSpacing;
                    if (shotsLeftInBurst <= 0)
                    {
                        state = State.Idle;
                        timer = cooldown;
                    }
                }
                break;
        }
    }

    /// <summary>Dispara ya un proyectil hacia donde apunta la cabeza (también lo usan las pruebas).</summary>
    public void Fire()
    {
        if (projectilePrefab == null || muzzle == null)
        {
            return;
        }

        TurretProjectile projectile = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        projectile.Speed = projectileSpeed;
        ShotsFired++;
    }

    // Hay línea de vista si el primer sólido que toca el rayo es Kael (o no hay ninguno antes de llegar a él).
    private bool HasLineOfSight(Vector3 target)
    {
        Vector3 from = muzzle != null ? muzzle.position : head.position;
        if (Physics.Linecast(from, target, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PlayerController>() != null;
        }

        return true;
    }

    private void Aim(Vector3 target)
    {
        Vector3 direction = target - head.position;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
        head.rotation = Quaternion.RotateTowards(head.rotation, wanted, turnSpeed * Time.deltaTime);
    }

    private void SetAlert(bool alert)
    {
        if (visual != null)
        {
            visual.IsAlert = alert;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetAlert(false);
    }
}
