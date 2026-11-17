using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Empuja a Kael en una dirección mientras está dentro de la zona (corriente del río, viento).
///
/// POR QUÉ EXISTE
///   Es la dinámica del nivel 5: el agua se mueve y hay que caminar contra ella o dejarse llevar.
///   No usa física: solo suma una velocidad al movimiento de Kael (PlayerController.ExternalVelocity).
///
/// CÓMO SE USA
///   Objeto con BoxCollider (la zona) y este script. "Push" es la velocidad del empuje en m/s, en el mundo.
///   Con Kael a 7 m/s, un empuje de 3 a 4 m/s se nota pero se puede vencer.
/// </summary>
public class PushZone : PlayerZone
{
    [Tooltip("Velocidad del empuje en metros por segundo (dirección en el mundo).")]
    [SerializeField] private Vector3 push = new Vector3(3.5f, 0f, 0f);

    public Vector3 Push => push;

    /// <summary>Configura el empuje desde código (lo usa el constructor de escena).</summary>
    public void Setup(Vector3 pushVelocity)
    {
        push = pushVelocity;
    }

    protected override void WhileInside(PlayerController kael)
    {
        kael.ExternalVelocity += push;
    }
}
