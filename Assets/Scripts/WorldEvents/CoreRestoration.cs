using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Evento del Fragmento 7: las siete esquirlas del Núcleo giran hacia el pedestal y se ensamblan en una
///   esfera de luz; una onda luminosa recorre el escenario, el ambiente se aclara ("el abismo se ilumina") y
///   avisa con GameEvents.CoreRestored para que el portal se abra.
///
/// POR QUÉ EXISTE
///   Es el clímax del nivel. Es un evento de mundo más (misma regla: una sola vez, idempotente), así que
///   hereda de <see cref="WorldEventBase"/> en vez de tener un sistema aparte.
///
/// CÓMO SE USA
///   Va en un objeto padre siempre activo. Asignar las esquirlas (7 objetos que empiezan ocultos), la esfera
///   del Núcleo (en el pedestal, se escala de 0 a 1), la esfera de la onda (material transparente, dibujado
///   por dentro) y opcionalmente la luz del Núcleo. Fragmento > Construir Escena lo arma todo.
/// </summary>
public class CoreRestoration : WorldEventBase
{
    [Tooltip("Esquirlas que viajan al Núcleo. Empiezan ocultas.")]
    [SerializeField] private Transform[] shards;

    [Tooltip("Esfera del Núcleo, en el centro del pedestal. Empieza con escala 0.")]
    [SerializeField] private Transform core;

    [Tooltip("Esfera grande, transparente y visible desde dentro: la onda de luz.")]
    [SerializeField] private Transform wave;

    [Tooltip("Luz del Núcleo (opcional): hace un destello al ensamblarse.")]
    [SerializeField] private Light coreLight;

    [Tooltip("Segundos que tardan las esquirlas en llegar.")]
    [SerializeField] private float gatherSeconds = 3f;

    [Tooltip("Segundos que dura la onda de luz.")]
    [SerializeField] private float waveSeconds = 3.5f;

    [Tooltip("Radio final de la onda (metros).")]
    [SerializeField] private float waveRadius = 150f;

    private Vector3[] shardStarts;
    private Material waveMaterial;

    public bool IsRestored { get; private set; }

    protected override void Prepare()
    {
        shardStarts = new Vector3[shards != null ? shards.Length : 0];
        for (int i = 0; i < shardStarts.Length; i++)
        {
            shardStarts[i] = shards[i].position;
            shards[i].gameObject.SetActive(false);
        }

        if (core != null)
        {
            core.localScale = Vector3.zero;
        }

        if (wave != null)
        {
            wave.gameObject.SetActive(false);
        }

        if (coreLight != null)
        {
            coreLight.enabled = false;
        }
    }

    protected override IEnumerator Run()
    {
        if (core == null)
        {
            Debug.LogError("CoreRestoration: falta asignar el Núcleo (Core).", this);
            yield break;
        }

        // 1) Las esquirlas aparecen y giran en espiral hacia el centro del Núcleo.
        for (int i = 0; i < shardStarts.Length; i++)
        {
            shards[i].gameObject.SetActive(true);
        }

        Vector3 center = core.position;
        float elapsed = 0f;
        while (elapsed < gatherSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / gatherSeconds);
            float eased = t * t * (3f - 2f * t);
            for (int i = 0; i < shardStarts.Length; i++)
            {
                Vector3 offset = shardStarts[i] - center;
                Quaternion swirl = Quaternion.AngleAxis(360f * eased, Vector3.up);
                shards[i].position = center + swirl * offset * (1f - eased);
                shards[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, eased);
            }
            yield return null;
        }

        for (int i = 0; i < shardStarts.Length; i++)
        {
            shards[i].gameObject.SetActive(false);
        }

        // 2) El Núcleo se forma con un pequeño "rebote".
        if (coreLight != null)
        {
            coreLight.enabled = true;
            coreLight.intensity = 30f;
        }

        elapsed = 0f;
        const float formSeconds = 0.8f;
        while (elapsed < formSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / formSeconds);
            float overshoot = 1f + 0.25f * Mathf.Sin(t * Mathf.PI);
            core.localScale = Vector3.one * (t * overshoot);
            yield return null;
        }
        core.localScale = Vector3.one;

        // 3) Onda de luz: el portal se abre al empezar, y el ambiente se aclara mientras la onda se expande.
        IsRestored = true;
        GameEvents.RaiseCoreRestored();

        Color startAmbient = RenderSettings.ambientLight;
        Color startFog = RenderSettings.fogColor;
        float startDensity = RenderSettings.fogDensity;
        Color targetAmbient = new Color(0.34f, 0.36f, 0.55f);
        Color targetFog = new Color(0.07f, 0.08f, 0.17f);

        if (wave != null)
        {
            wave.gameObject.SetActive(true);
            Renderer waveRenderer = wave.GetComponent<Renderer>();
            waveMaterial = waveRenderer != null ? waveRenderer.material : null;
        }

        elapsed = 0f;
        while (elapsed < waveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / waveSeconds);

            if (wave != null)
            {
                wave.localScale = Vector3.one * Mathf.Max(waveRadius * 2f * t, 0.1f);
                SetWaveAlpha(1f - t);
            }

            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);
            RenderSettings.fogColor = Color.Lerp(startFog, targetFog, t);
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, startDensity * 0.6f, t);
            if (coreLight != null)
            {
                coreLight.intensity = Mathf.Lerp(30f, 8f, t);
            }
            yield return null;
        }

        if (wave != null)
        {
            wave.gameObject.SetActive(false);
        }
    }

    private void SetWaveAlpha(float alpha)
    {
        if (waveMaterial == null)
        {
            return;
        }

        Color color = waveMaterial.GetColor("_BaseColor");
        color.a = alpha * 0.35f;
        waveMaterial.SetColor("_BaseColor", color);
        waveMaterial.SetColor("_EmissionColor", new Color(0.7f, 0.55f, 1f) * (alpha * 2.5f));
    }
}
