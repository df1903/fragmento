using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Zona para trepar (liana, escalera de mano): dentro de ella Kael no cae. Sube al empujar HACIA la pared y
///   baja al empujar en contra. Saltar lo suelta.
///
/// POR QUÉ EXISTE
///   Los niveles 6 y 7 tienen altura de verdad (barrancos, tejados). Reutiliza lo que ya ofrece PlayerController:
///   gravedad a cero, frenar la caída y un empuje hacia arriba.
///
/// CÓMO SE USA
///   Objeto con BoxCollider (la zona, pegada a la pared y medio metro más alta que el borde de arriba) y este
///   script. "Into Wall" es la dirección (en el mundo) que va hacia la pared. "Climb Speed", la velocidad en m/s.
/// </summary>
public class ClimbZone : PlayerZone
{
    [Tooltip("Velocidad al trepar, en metros por segundo.")]
    [SerializeField] private float climbSpeed = 4f;

    [Tooltip("Dirección en el mundo que va hacia la pared (horizontal).")]
    [SerializeField] private Vector3 intoWall = Vector3.forward;

    /// <summary>Configura la zona desde código (la usa el constructor de escena).</summary>
    public void Setup(Vector3 directionIntoWall)
    {
        directionIntoWall.y = 0f;
        intoWall = directionIntoWall.normalized;
    }

    protected override void WhileInside(PlayerController kael)
    {
        // Si acaba de saltar (va hacia arriba por el salto), se le deja ir: así se puede soltar de la liana.
        if (kael.VerticalVelocity > 0.1f)
        {
            kael.GravityScale = 1f;
            return;
        }

        kael.GravityScale = 0f;
        kael.CancelFall();
        // Cuánto empuja el jugador hacia la pared (1) o en contra (-1), mire hacia donde mire la cámara.
        float push = Vector3.Dot(kael.MoveDirection, intoWall);
        kael.ExternalVelocity += Vector3.up * climbSpeed * push;
    }

    protected override void OnExit(PlayerController kael)
    {
        kael.GravityScale = 1f;
    }
}
