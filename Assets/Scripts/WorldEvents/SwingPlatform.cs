using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Columpio (nivel 10): al activarse, el asiento oscila de lado a lado en un arco, colgado de sus cuerdas.
///   Kael puede subirse: el asiento lo lleva de un extremo al otro, bajando por el centro y subiendo a los lados.
///
/// POR QUÉ EXISTE
///   Es la dinámica estrella del jardín: cruzar del muro al roble montado en un columpio gigante. El asiento es un
///   Rigidbody cinemático (MovePosition), así MovingPlatformCarrier lleva a Kael igual que en las plataformas.
///   El asiento no gira: se queda horizontal para que Kael no resbale.
///
/// CÓMO SE USA
///   La raíz está en el punto del que cuelga (la rama). "Seat" es el asiento (con collider y Rigidbody cinemático),
///   "Ropes" el objeto visual de las cuerdas, que gira con el ángulo. Oscila en el plano X-Y del mundo.
/// </summary>
public class SwingPlatform : WorldEventBase
{
    [SerializeField] private Rigidbody seat;
    [SerializeField] private Transform ropes;

    [Tooltip("Metros desde la rama hasta el asiento.")]
    [SerializeField] private float length = 19f;

    [Tooltip("Grados a cada lado.")]
    [SerializeField] private float amplitude = 45f;

    [Tooltip("Segundos de una ida y vuelta completa.")]
    [SerializeField] private float period = 7f;

    private float clock;

    /// <summary>True desde que el columpio se pone en marcha.</summary>
    public bool IsSwinging { get; private set; }

    /// <summary>Ángulo actual en grados (0 = abajo del todo).</summary>
    public float Angle { get; private set; }

    public void Setup(Rigidbody seatBody, Transform ropesVisual, float ropeLength, float swingDegrees, float swingPeriod)
    {
        seat = seatBody;
        ropes = ropesVisual;
        length = ropeLength;
        amplitude = swingDegrees;
        period = swingPeriod;
    }

    protected override void Prepare()
    {
        seat.isKinematic = true;
        seat.interpolation = RigidbodyInterpolation.Interpolate;
        Place(0f);
    }

    protected override IEnumerator Run()
    {
        // Arranca suave: la amplitud crece durante los primeros segundos, como un columpio al empujarlo.
        IsSwinging = true;
        yield break;
    }

    private void FixedUpdate()
    {
        if (!IsSwinging)
        {
            return;
        }

        clock += Time.fixedDeltaTime;
        float ramp = Mathf.Clamp01(clock / period);
        Place(Mathf.Sin(clock / period * Mathf.PI * 2f) * amplitude * ramp);
    }

    private void Place(float degrees)
    {
        Angle = degrees;
        float radians = degrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians), -Mathf.Cos(radians), 0f) * length;
        if (IsSwinging)
        {
            seat.MovePosition(transform.position + offset);
        }
        else
        {
            seat.position = transform.position + offset; // en reposo: se coloca directamente
        }
        if (ropes != null)
        {
            ropes.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
