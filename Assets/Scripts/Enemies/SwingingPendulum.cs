using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Péndulo (el del reloj de pared del nivel 9): cuelga de un pivote y oscila de lado a lado. Su disco barre
///   el camino y mata a Kael si lo golpea.
///
/// POR QUÉ EXISTE
///   Peligro de ritmo clásico de plataformas: se pasa justo después de que el disco cruce. Es fácil de leer
///   porque el movimiento es un seno: siempre igual y predecible.
///
/// CÓMO SE USA
///   La raíz es el punto del que cuelga. "Arm" es el hijo que gira (el disco cuelga "Length" metros por debajo,
///   al final del brazo). Oscila alrededor del eje Z local de la raíz.
/// </summary>
public class SwingingPendulum : Hazard
{
    [SerializeField] private Transform arm;

    [Tooltip("Metros del pivote al centro del disco.")]
    [SerializeField] private float length = 8f;

    [Tooltip("Grados a cada lado.")]
    [SerializeField] private float amplitude = 50f;

    [Tooltip("Segundos de una ida y vuelta.")]
    [SerializeField] private float period = 3.2f;

    [SerializeField] private float killRadius = 1.6f;

    private float clock;

    /// <summary>Centro del disco en el mundo.</summary>
    public Vector3 BobPosition => arm.position - arm.up * length;

    public void Setup(Transform armTransform, float armLength, float swingDegrees, float swingPeriod)
    {
        arm = armTransform;
        length = armLength;
        amplitude = swingDegrees;
        period = swingPeriod;
    }

    protected override void Tick()
    {
        clock += Time.deltaTime;
        arm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(clock / period * Mathf.PI * 2f) * amplitude);
        TryKill(BobPosition, killRadius);
    }
}
