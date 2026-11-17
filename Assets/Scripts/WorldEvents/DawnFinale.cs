using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   El amanecer del final (nivel 10): cuando el último Núcleo queda restaurado, el sol sale del todo. La luz pasa
///   de azul frío a dorado, el ambiente y la niebla se calientan y se encienden las luces del "sol" de la ventana.
///
/// POR QUÉ EXISTE
///   Es el momento final del juego: lo que Kael restauró se ve en el mundo entero. Solo escucha
///   GameEvents.CoreRestored; no conoce el núcleo ni el portal.
///
/// CÓMO SE USA
///   Objeto vacío con este script. "Sun" es la luz direccional. Los valores "Target" son los del final.
///   "Turn On" son objetos que se activan al amanecer (rayos, disco del sol).
/// </summary>
public class DawnFinale : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private float seconds = 6f;
    [SerializeField] private float targetSunIntensity = 3.2f;
    [SerializeField] private Color targetSunColor = new Color(1f, 0.82f, 0.55f);
    [SerializeField] private Color targetAmbientSky = new Color(0.95f, 0.8f, 0.6f);
    [SerializeField] private Color targetAmbientEquator = new Color(0.75f, 0.6f, 0.45f);
    [SerializeField] private Color targetFog = new Color(1f, 0.88f, 0.7f);
    [SerializeField] private GameObject[] turnOn = new GameObject[0];

    /// <summary>True cuando el amanecer ya empezó (para pruebas).</summary>
    public bool HasStarted { get; private set; }

    public void Setup(Light sunLight, GameObject[] objectsToTurnOn)
    {
        sun = sunLight;
        turnOn = objectsToTurnOn;
    }

    private void Awake()
    {
        foreach (GameObject item in turnOn)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
    }

    private void OnEnable() => GameEvents.CoreRestored += Begin;
    private void OnDisable() => GameEvents.CoreRestored -= Begin;

    private void Begin()
    {
        if (HasStarted)
        {
            return;
        }

        HasStarted = true;
        foreach (GameObject item in turnOn)
        {
            if (item != null)
            {
                item.SetActive(true);
            }
        }

        StartCoroutine(Sunrise());
    }

    private IEnumerator Sunrise()
    {
        float startIntensity = sun != null ? sun.intensity : 0f;
        Color startColor = sun != null ? sun.color : Color.white;
        Color startSky = RenderSettings.ambientSkyColor, startEquator = RenderSettings.ambientEquatorColor, startFog = RenderSettings.fogColor;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(startIntensity, targetSunIntensity, t);
                sun.color = Color.Lerp(startColor, targetSunColor, t);
            }

            RenderSettings.ambientSkyColor = Color.Lerp(startSky, targetAmbientSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(startEquator, targetAmbientEquator, t);
            RenderSettings.fogColor = Color.Lerp(startFog, targetFog, t);
            yield return null;
        }
    }
}
