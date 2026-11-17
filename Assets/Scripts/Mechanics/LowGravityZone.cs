using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Baja la gravedad de Kael mientras está dentro: salta más alto, cae más lento y llega más lejos.
///
/// POR QUÉ EXISTE
///   Es la dinámica del nivel 3 (Observatorio): el campo astral deja "flotar". Con gravedad 0.45 el salto pasa
///   de 1.8 m a unos 4 m de alto y de 5 m a unos 10 m de largo, así que dentro de la zona hay huecos más grandes.
///
/// CÓMO SE USA
///   Objeto con BoxCollider (la zona) y este script. Al salir de la zona la gravedad vuelve a 1.
/// </summary>
public class LowGravityZone : PlayerZone
{
    [Tooltip("Multiplicador de gravedad dentro de la zona (1 = normal, 0.45 = casi la mitad).")]
    [SerializeField] private float gravityScale = 0.45f;

    protected override void WhileInside(PlayerController kael)
    {
        kael.GravityScale = gravityScale;
    }

    protected override void OnExit(PlayerController kael)
    {
        kael.GravityScale = 1f;
    }
}
