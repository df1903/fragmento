using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Gira un objeto sin parar alrededor de un eje (las aspas del molino).
///
/// POR QUÉ EXISTE
///   Detalle de ambiente del nivel 7: un pueblo abandonado donde algo todavía se mueve.
///
/// CÓMO SE USA
///   Añadir al objeto que gira. "Degrees Per Second" es la velocidad y "Axis" el eje local.
/// </summary>
public class Spinner : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.forward;
    [SerializeField] private float degreesPerSecond = 14f;

    private void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
