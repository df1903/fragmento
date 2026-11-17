using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Un árbol muerto que, al llegar su fragmento, cruje, cae y queda tumbado sobre el río: su tronco es el puente.
///
/// POR QUÉ EXISTE
///   Es el primer evento del nivel 6. En vez de "aparece un puente de luz", el mundo real cambia: el puente es un
///   objeto que ya estaba ahí. Solo gira un pivote en la base del tronco; el collider del tronco hace el resto.
///
/// CÓMO SE USA
///   "Pivot" es un objeto en la base del árbol; el tronco es su hijo y apunta hacia arriba. "Fall Angle" son los
///   grados que gira alrededor del eje X local del pivote (90 = queda horizontal, apuntando al +Z del pivote).
/// </summary>
public class FallingTreeBridge : WorldEventBase
{
    [SerializeField] private Transform pivot;

    [Tooltip("Grados que gira el tronco al caer.")]
    [SerializeField] private float fallAngle = 90f;

    [Tooltip("Segundos que tarda en caer.")]
    [SerializeField] private float fallSeconds = 1.6f;

    /// <summary>Configura el árbol desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform pivotTransform, float angle)
    {
        pivot = pivotTransform;
        fallAngle = angle;
    }

    protected override IEnumerator Run()
    {
        Quaternion start = pivot.localRotation;

        // Cruje: tiembla un momento antes de caer.
        float shake = 0.6f;
        while (shake > 0f)
        {
            shake -= Time.deltaTime;
            pivot.localRotation = start * Quaternion.Euler(Mathf.Sin(Time.time * 40f) * 1.5f, 0f, 0f);
            yield return null;
        }

        // Cae acelerando (t al cuadrado), como algo que pesa.
        float elapsed = 0f;
        while (elapsed < fallSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallSeconds);
            pivot.localRotation = start * Quaternion.Euler(fallAngle * t * t, 0f, 0f);
            yield return null;
        }

        // Rebote corto al golpear el suelo.
        float bounce = 0.35f;
        while (bounce > 0f)
        {
            bounce -= Time.deltaTime;
            float lift = Mathf.Sin(bounce / 0.35f * Mathf.PI) * 3f;
            pivot.localRotation = start * Quaternion.Euler(fallAngle - lift, 0f, 0f);
            yield return null;
        }

        pivot.localRotation = start * Quaternion.Euler(fallAngle, 0f, 0f);
    }
}
