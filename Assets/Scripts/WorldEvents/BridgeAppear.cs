using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hace aparecer un puente de luz: el puente "crece" desde su inicio hasta el otro lado
///   escalando un objeto pivote de 0 a 1 en su eje Z.
///
/// POR QUÉ EXISTE
///   Es el evento del Fragmento 1 (y se reutiliza en los fragmentos 5 y 7). Versión barata del efecto:
///   animación de escala + material emisivo, sin simulaciones complejas.
///
/// CÓMO SE USA
///   Estructura esperada (la crea Fragmento > Construir Escena):
///     Puente (este script, siempre activo)
///       └ Pivote (en el punto de inicio; este script lo escala en Z)
///           └ Cuerpo (la losa; su collider hace transitable el puente)
///   Mientras el puente no ha aparecido, el Pivote está desactivado: no se ve y no se puede pisar.
/// </summary>
public class BridgeAppear : WorldEventBase
{
    [Tooltip("Objeto que crece de 0 a 1 en Z. Debe estar en el extremo donde empieza el puente.")]
    [SerializeField] private Transform pivot;

    [Tooltip("Segundos que tarda en aparecer.")]
    [SerializeField] private float duration = 2f;

    [Tooltip("Cómo avanza el crecimiento (0 = nada, 1 = completo).")]
    [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Un collider con escala 0 da avisos de física; se parte de una escala mínima.
    private const float MinScale = 0.01f;

    protected override void Prepare()
    {
        if (pivot == null)
        {
            Debug.LogError("BridgeAppear: falta asignar el Pivot.", this);
            return;
        }

        pivot.gameObject.SetActive(false);
    }

    protected override IEnumerator Run()
    {
        if (pivot == null)
        {
            yield break;
        }

        pivot.localScale = new Vector3(1f, 1f, MinScale);
        pivot.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = growthCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            pivot.localScale = new Vector3(1f, 1f, Mathf.Max(progress, MinScale));
            yield return null;
        }

        pivot.localScale = Vector3.one;
    }
}
