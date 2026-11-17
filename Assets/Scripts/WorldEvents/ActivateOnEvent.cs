using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Evento de mundo genérico: al recoger su fragmento "despierta" una lista de objetos que estaban apagados
///   (hongos de rebote, un campo de baja gravedad, una balsa), uno detrás de otro y creciendo desde pequeño.
///
/// POR QUÉ EXISTE
///   Varias dinámicas nuevas solo necesitan aparecer cuando llega su fragmento. Con este script no hace falta
///   una clase de evento por cada una.
///
/// CÓMO SE USA
///   Poner este script en un objeto SIEMPRE activo y arrastrar a "Targets" los objetos a despertar (hijos).
///   "Event Id" debe coincidir con el FragmentData que lo dispara.
/// </summary>
public class ActivateOnEvent : WorldEventBase
{
    [Tooltip("Objetos que se encienden, en orden.")]
    [SerializeField] private Transform[] targets;

    [Tooltip("Segundos que tarda cada objeto en crecer hasta su tamaño.")]
    [SerializeField] private float growSeconds = 0.4f;

    [Tooltip("Segundos entre un objeto y el siguiente.")]
    [SerializeField] private float stagger = 0.2f;

    private Vector3[] finalScales;

    /// <summary>Configura los objetos desde código (lo usa el constructor de escena).</summary>
    public void SetTargets(Transform[] list)
    {
        targets = list;
    }

    protected override void Prepare()
    {
        if (targets == null)
        {
            return;
        }

        finalScales = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            finalScales[i] = targets[i].localScale;
            targets[i].gameObject.SetActive(false);
        }
    }

    protected override IEnumerator Run()
    {
        if (targets == null)
        {
            yield break;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < growSeconds)
            {
                elapsed += Time.deltaTime;
                // Se parte de un 10 % del tamaño: un collider con escala 0 da avisos de física.
                targets[i].localScale = finalScales[i] * Mathf.Lerp(0.1f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / growSeconds));
                yield return null;
            }

            targets[i].localScale = finalScales[i];
            yield return new WaitForSeconds(stagger);
        }
    }
}
