using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hace que Kael viaje sobre una plataforma móvil sin resbalar: cada fotograma lo desplaza lo mismo
///   que se ha movido (o girado) la plataforma que pisa.
///
/// POR QUÉ EXISTE
///   El CharacterController no se mueve solo con el suelo que tiene debajo (a diferencia de un Rigidbody).
///   Sin este script, la plataforma se deslizaría bajo los pies de Kael.
///
/// CÓMO SE USA
///   Va en el prefab de Kael junto a PlayerController. Funciona con cualquier collider que tenga un Rigidbody
///   cinemático (la plataforma móvil lo tiene). Truco: se guarda la posición de Kael EN COORDENADAS DE LA
///   PLATAFORMA al final del fotograma; al siguiente se convierte de vuelta a coordenadas del mundo y la
///   diferencia es lo que se ha movido la plataforma (sirve también si la plataforma gira).
/// </summary>
[DefaultExecutionOrder(-10)] // antes que PlayerController, para que el movimiento propio se sume al de la plataforma
[RequireComponent(typeof(PlayerController), typeof(CharacterController))]
public class MovingPlatformCarrier : MonoBehaviour
{
    private PlayerController player;
    private CharacterController controller;
    private Transform platform;
    private Vector3 localPosition;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Si Kael ya no pisa nada (salto, caída o respawn), se olvida la plataforma para no arrastrarlo de vuelta.
        if (platform == null || player.GroundCollider == null)
        {
            platform = null;
            return;
        }

        Vector3 expected = platform.TransformPoint(localPosition);
        Vector3 delta = expected - transform.position;
        if (delta.sqrMagnitude > 1e-8f)
        {
            controller.Move(delta);
        }
    }

    private void LateUpdate()
    {
        Collider ground = player.GroundCollider;
        Rigidbody body = ground != null ? ground.attachedRigidbody : null;
        platform = (body != null && body.isKinematic) ? body.transform : null;

        if (platform != null)
        {
            localPosition = platform.InverseTransformPoint(transform.position);
        }
    }
}
