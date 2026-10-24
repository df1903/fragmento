using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Alinea una hilera de plataformas giratorias: al empezar flotan a un costado, y al recoger el fragmento
///   cada pivote gira 180° (una tras otra) hasta que las plataformas forman el camino hacia el punto más alto.
///   Al llegar, cada plataforma se enciende (cambia de material).
///
/// POR QUÉ EXISTE
///   Es el evento del Fragmento 6 ("alinea plataformas giratorias que llevan al punto más alto").
///
/// CÓMO SE USA
///   Va en un objeto padre siempre activo. Cada elemento de "Pivots" es un pivote vacío con UNA plataforma
///   hija desplazada de su centro; el pivote debe estar ya girado 180° en Y (estado "desalineado") y el
///   script lo lleva a 0°. Fragmento > Construir Escena lo arma así. "Platform Renderers" y los materiales
///   son opcionales (solo cambian el aspecto).
/// </summary>
public class RotatingPlatformsAlign : WorldEventBase
{
    [Tooltip("Pivotes que giran. Cada uno lleva una plataforma hija.")]
    [SerializeField] private Transform[] pivots;

    [Tooltip("Renderer de la plataforma de cada pivote, en el mismo orden (opcional).")]
    [SerializeField] private Renderer[] platformRenderers;

    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material activeMaterial;

    [Tooltip("Segundos que tarda cada plataforma en girar.")]
    [SerializeField] private float rotateSeconds = 3f;

    [Tooltip("Segundos entre que empieza una plataforma y la siguiente.")]
    [SerializeField] private float staggerSeconds = 0.6f;

    // Ángulo inicial (desalineado) que se recuerda de cada pivote, para girar de ahí a 0°.
    private float[] startAngles;

    public bool IsAligned { get; private set; }

    protected override void Prepare()
    {
        if (pivots == null)
        {
            return;
        }

        startAngles = new float[pivots.Length];
        for (int i = 0; i < pivots.Length; i++)
        {
            startAngles[i] = pivots[i] != null ? pivots[i].localEulerAngles.y : 0f;
        }

        SetMaterials(inactiveMaterial);
    }

    protected override IEnumerator Run()
    {
        if (pivots == null || pivots.Length == 0)
        {
            yield break;
        }

        float total = rotateSeconds + staggerSeconds * (pivots.Length - 1);
        float elapsed = 0f;
        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < pivots.Length; i++)
            {
                float t = Mathf.Clamp01((elapsed - staggerSeconds * i) / rotateSeconds);
                if (pivots[i] != null)
                {
                    float angle = Mathf.LerpAngle(startAngles[i], 0f, Mathf.SmoothStep(0f, 1f, t));
                    pivots[i].localRotation = Quaternion.Euler(0f, angle, 0f);
                }

                // Se enciende justo al llegar.
                if (t >= 1f)
                {
                    SetMaterial(i, activeMaterial);
                }
            }
            yield return null;
        }

        for (int i = 0; i < pivots.Length; i++)
        {
            if (pivots[i] != null)
            {
                pivots[i].localRotation = Quaternion.identity;
            }
        }
        SetMaterials(activeMaterial);
        IsAligned = true;
    }

    private void SetMaterials(Material material)
    {
        for (int i = 0; platformRenderers != null && i < platformRenderers.Length; i++)
        {
            SetMaterial(i, material);
        }
    }

    private void SetMaterial(int index, Material material)
    {
        if (material != null && platformRenderers != null && index < platformRenderers.Length && platformRenderers[index] != null)
        {
            platformRenderers[index].sharedMaterial = material;
        }
    }
}
