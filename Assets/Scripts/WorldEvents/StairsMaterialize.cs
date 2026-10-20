using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Hace aparecer una escalera de piedra: cada escalón (una roca) está oculto y, uno tras otro, vuela
///   desde abajo girando y encogido hasta su sitio, y al llegar se vuelve sólido.
///
/// POR QUÉ EXISTE
///   Es el evento del Fragmento 2 ("las rocas flotantes se reorganizan"). Versión barata: animación de
///   posición, giro y escala con una curva, escalonada en el tiempo. No hay física ni simulación.
///
/// CÓMO SE USA
///   El objeto con este script debe estar ACTIVO; los escalones (hijos) los oculta él en Awake.
///   Se asignan en "Steps" de abajo hacia arriba. Cada escalón necesita un collider.
///   Un escalón solo se puede pisar cuando termina de llegar (su collider se activa al final).
/// </summary>
public class StairsMaterialize : WorldEventBase
{
    [Tooltip("Escalones, en orden de abajo hacia arriba.")]
    [SerializeField] private Transform[] steps;

    [Tooltip("Segundos entre el inicio de un escalón y el siguiente.")]
    [SerializeField] private float delayBetweenSteps = 0.12f;

    [Tooltip("Segundos que tarda cada escalón en llegar a su sitio.")]
    [SerializeField] private float stepDuration = 0.5f;

    [Tooltip("De dónde sale cada escalón, relativo a su sitio final (en el espacio del objeto padre).")]
    [SerializeField] private Vector3 startOffset = new Vector3(0f, -4f, 0f);

    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3[] finalPositions;
    private Quaternion[] finalRotations;
    private Vector3[] finalScales;
    private Collider[] colliders;
    private int stepsRunning;

    public int StepCount => steps != null ? steps.Length : 0;

    protected override void Prepare()
    {
        if (steps == null)
        {
            Debug.LogError("StairsMaterialize: faltan los escalones (Steps).", this);
            return;
        }

        finalPositions = new Vector3[steps.Length];
        finalRotations = new Quaternion[steps.Length];
        finalScales = new Vector3[steps.Length];
        colliders = new Collider[steps.Length];

        for (int i = 0; i < steps.Length; i++)
        {
            finalPositions[i] = steps[i].localPosition;
            finalRotations[i] = steps[i].localRotation;
            finalScales[i] = steps[i].localScale;
            colliders[i] = steps[i].GetComponent<Collider>();

            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
            steps[i].gameObject.SetActive(false);
        }
    }

    protected override IEnumerator Run()
    {
        if (steps == null)
        {
            yield break;
        }

        stepsRunning = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            StartCoroutine(AnimateStep(i));
            yield return new WaitForSeconds(delayBetweenSteps);
        }

        while (stepsRunning > 0)
        {
            yield return null;
        }
    }

    private IEnumerator AnimateStep(int index)
    {
        Transform step = steps[index];

        // Los escalones pares y los impares giran en sentidos opuestos para que no parezca una máquina.
        float side = index % 2 == 0 ? 1f : -1f;
        Vector3 startPosition = finalPositions[index] + startOffset;
        Quaternion startRotation = finalRotations[index] * Quaternion.Euler(30f * side, 0f, 20f * side);
        Vector3 startScale = finalScales[index] * 0.2f;

        step.localPosition = startPosition;
        step.localRotation = startRotation;
        step.localScale = startScale;
        step.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / stepDuration));
            step.localPosition = Vector3.LerpUnclamped(startPosition, finalPositions[index], t);
            step.localRotation = Quaternion.SlerpUnclamped(startRotation, finalRotations[index], t);
            step.localScale = Vector3.LerpUnclamped(startScale, finalScales[index], t);
            yield return null;
        }

        step.localPosition = finalPositions[index];
        step.localRotation = finalRotations[index];
        step.localScale = finalScales[index];
        if (colliders[index] != null)
        {
            colliders[index].enabled = true;
        }

        stepsRunning--;
    }
}
