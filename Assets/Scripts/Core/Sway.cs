using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Tres detalles de ambiente pequeños para los niveles 6 y 7, cada uno en su componente:
///   - Sway: mece un objeto (copas de árbol, hierba, faroles colgantes) como si hubiera viento.
///   - UvScroll: desliza la textura de un material (agua que corre).
///   - LightningFlash: relámpagos; sube de golpe una luz y la deja caer.
///
/// POR QUÉ EXISTE
///   Un mundo quieto se ve falso. Estos movimientos son baratos (una cuenta por fotograma) y no afectan al juego.
///
/// CÓMO SE USA
///   Añadir el componente al objeto. Todos los números están en el Inspector. Los coloca el constructor de escena.
/// </summary>
public class Sway : MonoBehaviour
{
    [Tooltip("Grados máximos de balanceo.")]
    [SerializeField] private float degrees = 2.5f;

    [Tooltip("Vaivenes por segundo (aprox.).")]
    [SerializeField] private float speed = 0.6f;

    private Quaternion rest;
    private float offset;

    private void Awake()
    {
        rest = transform.localRotation;
        // Cada objeto empieza en un punto distinto del vaivén (según su posición) para que no se muevan a la vez.
        offset = transform.position.x * 0.37f + transform.position.z * 0.21f;
    }

    private void Update()
    {
        float t = Time.time * speed * 2f * Mathf.PI + offset;
        transform.localRotation = rest * Quaternion.Euler(Mathf.Sin(t) * degrees, 0f, Mathf.Sin(t * 0.7f + 1.3f) * degrees);
    }
}
