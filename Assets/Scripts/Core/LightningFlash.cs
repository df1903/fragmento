using UnityEngine;

/// <summary>Relámpagos: cada cierto tiempo la luz sube de golpe y se apaga en un instante. Ver el resumen de Sway.</summary>
[RequireComponent(typeof(Light))]
public class LightningFlash : MonoBehaviour
{
    [Tooltip("Segundos mínimos y máximos entre relámpagos.")]
    [SerializeField] private Vector2 interval = new Vector2(6f, 14f);

    [Tooltip("Intensidad de la luz en el pico del relámpago.")]
    [SerializeField] private float peak = 4f;

    private Light flash;
    private float nextAt;
    private float glow; // 1 = pico, 0 = apagado

    private void Awake()
    {
        flash = GetComponent<Light>();
        flash.intensity = 0f;
        nextAt = Time.time + Random.Range(interval.x, interval.y);
    }

    private void Update()
    {
        if (Time.time >= nextAt)
        {
            glow = 1f;
            nextAt = Time.time + Random.Range(interval.x, interval.y);
        }

        // Cae rápido y parpadea un poco mientras cae, como un relámpago real.
        glow = Mathf.MoveTowards(glow, 0f, Time.deltaTime * 2.5f);
        flash.intensity = peak * glow * (0.6f + 0.4f * Mathf.Sin(Time.time * 60f));
    }
}
