using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hace girar dos anillos de luz alrededor del cuerpo de Kael, cada uno en un plano distinto.
///
/// POR QUÉ EXISTE
///   Primer paso de la "materialización" de Kael: en el nivel 2 deja de ser solo una bola y gana estructura.
///   Es puramente visual: no toca el movimiento ni la física.
///
/// CÓMO SE USA
///   Va en un objeto hijo de Kael (dentro de Visual) con dos hijos, uno por anillo. El constructor de escenas
///   lo arma en el prefab Kael_Nivel2. Velocidades ajustables en el Inspector.
/// </summary>
public class OrbitRings : MonoBehaviour
{
    [SerializeField] private Transform ringA;
    [SerializeField] private Transform ringB;

    [Tooltip("Grados por segundo del primer anillo.")]
    [SerializeField] private float speedA = 90f;

    [Tooltip("Grados por segundo del segundo anillo (sentido contrario).")]
    [SerializeField] private float speedB = -60f;

    private void Update()
    {
        if (ringA != null)
        {
            ringA.Rotate(0f, speedA * Time.deltaTime, 0f, Space.Self);
        }

        if (ringB != null)
        {
            ringB.Rotate(0f, speedB * Time.deltaTime, 0f, Space.Self);
        }
    }
}
