using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Plataforma que gira sin parar sobre su eje vertical (el tocadiscos del nivel 9). Kael viaja encima.
///
/// POR QUÉ EXISTE
///   Girar con un Rigidbody cinemático (MoveRotation) hace que MovingPlatformCarrier lleve a Kael con el giro,
///   igual que lleva a Kael en las plataformas que van y vienen. Si se girara solo el Transform, Kael resbalaría.
///
/// CÓMO SE USA
///   Objeto con collider, Rigidbody cinemático y este script. "Degrees Per Second" es la velocidad del giro.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SpinningPlatform : MonoBehaviour
{
    [Tooltip("Grados por segundo (positivo = sentido horario visto desde arriba).")]
    [SerializeField] private float degreesPerSecond = 20f;

    private Rigidbody body;

    public void Setup(float speed)
    {
        degreesPerSecond = speed;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, degreesPerSecond * Time.fixedDeltaTime, 0f));
    }
}
