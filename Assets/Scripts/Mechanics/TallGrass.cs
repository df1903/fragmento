using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hierba alta: mientras Kael está dentro y NO corre, queda escondido (PlayerStealth). Los lobos de sombra no
///   lo ven salvo que estén casi encima.
///
/// POR QUÉ EXISTE
///   Es la dinámica de sigilo del nivel 6: cruzar el bosque por la hierba, despacio, en vez de a campo abierto.
///
/// CÓMO SE USA
///   Objeto con BoxCollider (la zona que ocupa la hierba) y este script. Las matas de hierba son solo visuales.
/// </summary>
public class TallGrass : PlayerZone
{
    protected override void WhileInside(PlayerController kael)
    {
        if (!kael.IsSprinting)
        {
            PlayerStealth.HideThisFrame();
        }
    }
}
