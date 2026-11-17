using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Permite a Kael empujar cajas: cuando camina contra un objeto con Rigidbody (no cinemático), le da velocidad
///   en la dirección en que camina.
///
/// POR QUÉ EXISTE
///   El CharacterController choca con los Rigidbody pero no los empuja solo. Con este script las cajas del
///   nivel 7 se pueden llevar hasta las placas de presión (PressurePlate).
///
/// CÓMO SE USA
///   Va en la raíz de Kael, junto al CharacterController. Las cajas necesitan un Rigidbody con la rotación
///   congelada (lo configura el constructor de escena).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CratePusher : MonoBehaviour
{
    [Tooltip("Velocidad a la que se mueve la caja empujada (m/s).")]
    [SerializeField] private float pushSpeed = 3f;

    // Unity llama a este método cada vez que el CharacterController toca un collider al moverse.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.rigidbody;
        if (body == null || body.isKinematic)
        {
            return;
        }

        // Si Kael está encima de la caja (el choque viene de abajo), no se empuja.
        if (hit.moveDirection.y < -0.3f)
        {
            return;
        }

        Vector3 direction = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 velocity = direction.normalized * pushSpeed;
        velocity.y = body.linearVelocity.y; // la caja conserva su caída
        body.linearVelocity = velocity;
    }
}
