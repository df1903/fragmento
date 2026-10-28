using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hace crecer una fila de cristales uno detrás de otro (de alto 0 a su tamaño final). Sirven de pilares,
///   rampa o escalera sobre el lago del nivel 2.
///
/// POR QUÉ EXISTE
///   Es el evento "propio" del nivel 2: la energía del fragmento hace crecer cristal en vez de materializar luz.
///   Barato: solo escala un eje; los colliders de los cristales hacen transitable el camino al crecer.
///
/// CÓMO SE USA
///   Poner este script en un objeto SIEMPRE activo y arrastrar sus cristales (hijos) a "Crystals", en el orden
///   en que deben crecer. Mientras no han crecido están desactivados: no se ven ni se pueden pisar.
///   "Event Id" debe coincidir con el FragmentData que lo dispara. Fragmento > Construir Nivel 2 lo arma.
/// </summary>
public class CrystalGrow : WorldEventBase
{
    [Tooltip("Cristales en el orden en que crecen.")]
    [SerializeField] private Transform[] crystals;

    [Tooltip("Segundos que tarda en crecer cada cristal.")]
    [SerializeField] private float growSeconds = 0.6f;

    [Tooltip("Segundos entre que empieza un cristal y el siguiente.")]
    [SerializeField] private float stagger = 0.25f;

    // Un collider con escala 0 da avisos de física; se parte de una escala mínima.
    private const float MinScale = 0.01f;

    private Vector3[] finalScales;

    /// <summary>Cuántos cristales hay en la cadena (para pruebas).</summary>
    public int CrystalCount => crystals != null ? crystals.Length : 0;

    /// <summary>Configura los cristales desde código (lo usa el constructor de escena).</summary>
    public void SetCrystals(Transform[] list)
    {
        crystals = list;
    }

    protected override void Prepare()
    {
        if (crystals == null)
        {
            return;
        }

        finalScales = new Vector3[crystals.Length];
        for (int i = 0; i < crystals.Length; i++)
        {
            finalScales[i] = crystals[i].localScale;
            crystals[i].gameObject.SetActive(false);
        }
    }

    protected override IEnumerator Run()
    {
        if (crystals == null || crystals.Length == 0)
        {
            yield break;
        }

        float total = stagger * (crystals.Length - 1) + growSeconds;
        float elapsed = 0f;
        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < crystals.Length; i++)
            {
                float progress = Mathf.Clamp01((elapsed - i * stagger) / growSeconds);
                if (progress <= 0f)
                {
                    continue;
                }

                if (!crystals[i].gameObject.activeSelf)
                {
                    crystals[i].gameObject.SetActive(true);
                }

                Vector3 scale = finalScales[i];
                scale.y = Mathf.Max(finalScales[i].y * Mathf.SmoothStep(0f, 1f, progress), MinScale);
                crystals[i].localScale = scale;
            }
            yield return null;
        }

        for (int i = 0; i < crystals.Length; i++)
        {
            crystals[i].gameObject.SetActive(true);
            crystals[i].localScale = finalScales[i];
        }
    }
}
