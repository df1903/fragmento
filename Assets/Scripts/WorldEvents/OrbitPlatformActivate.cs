using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Plataforma que, al activarse, gira en círculo alrededor de un punto (como un planeta en su órbita).
///   No rota sobre sí misma: solo cambia de posición, así Kael viaja encima sin marearse.
///
/// POR QUÉ EXISTE
///   Es la dinámica del nivel 3 (Observatorio). Kael viaja sobre ella gracias a MovingPlatformCarrier, igual
///   que en las plataformas que van y vienen.
///
/// CÓMO SE USA
///   Bloque con collider y Rigidbody cinemático + este script. Se coloca en su punto de salida; el centro de
///   la órbita se calcula con "Center Offset" (del bloque al centro). "Degrees Per Second" marca el ritmo:
///   positivo gira en sentido antihorario visto desde arriba.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OrbitPlatformActivate : WorldEventBase
{
    [Tooltip("Vector desde la posición inicial de la plataforma hasta el centro de la órbita (horizontal).")]
    [SerializeField] private Vector3 centerOffset = new Vector3(6f, 0f, 0f);

    [Tooltip("Grados por segundo. Con radio 6 m, 25 grados/s son unos 2.6 m/s.")]
    [SerializeField] private float degreesPerSecond = 25f;

    [Tooltip("Si está marcado, gira desde el principio sin esperar a un fragmento.")]
    [SerializeField] private bool activeFromStart;

    [SerializeField] private Renderer platformRenderer;
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material activeMaterial;

    private Rigidbody body;
    private Vector3 center;
    private float radius;
    private float angle; // radianes
    private bool moving;

    public bool IsMoving => moving;

    /// <summary>Configura la órbita desde código (lo usa el constructor de escena).</summary>
    public void Setup(Vector3 offsetToCenter, float speedDegrees, bool startActive)
    {
        centerOffset = offsetToCenter;
        degreesPerSecond = speedDegrees;
        activeFromStart = startActive;
    }

    protected override void Prepare()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        centerOffset.y = 0f;
        center = transform.position + centerOffset;
        radius = centerOffset.magnitude;
        angle = Mathf.Atan2(-centerOffset.z, -centerOffset.x); // ángulo del punto de salida visto desde el centro

        moving = activeFromStart;
        SetMaterial(moving ? activeMaterial : inactiveMaterial);
    }

    protected override IEnumerator Run()
    {
        SetMaterial(activeMaterial);
        yield return new WaitForSeconds(0.5f); // se ve que "despierta" antes de moverse
        moving = true;
    }

    private void FixedUpdate()
    {
        if (!moving)
        {
            return;
        }

        angle += degreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime;
        body.MovePosition(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
    }

    private void SetMaterial(Material material)
    {
        if (platformRenderer != null && material != null)
        {
            platformRenderer.sharedMaterial = material;
        }
    }
}
