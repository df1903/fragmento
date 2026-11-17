using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Haz de luz roja que gira a ras de suelo alrededor de un pilar, como la aguja de un reloj. Mata si toca a
///   Kael; se esquiva saltándolo o caminando delante de él.
///
/// POR QUÉ EXISTE
///   Es el peligro propio del nivel 3 (Observatorio): en vez de perseguir o disparar, marca un ritmo que el
///   jugador debe leer. Mata por distancia al haz (sin física), igual que los demás peligros.
///
/// CÓMO SE USA
///   La raíz se coloca en el suelo, en el centro del giro. "Pivot" es el hijo que gira (contiene el haz visual,
///   que apunta a +Z). "Length" es el largo del haz. Lo arma Fragmento > Construir Nivel 3.
/// </summary>
public class SweepingBeam : Hazard
{
    [SerializeField] private Transform pivot;

    [Tooltip("Largo del haz en metros, desde el centro.")]
    [SerializeField] private float length = 9f;

    [Tooltip("Grados por segundo. Negativo gira al revés.")]
    [SerializeField] private float degreesPerSecond = 40f;

    [Tooltip("Altura del haz sobre el suelo. Un salto (1.8 m) lo supera con margen.")]
    [SerializeField] private float beamHeight = 0.4f;

    [Tooltip("Grosor de la zona que mata, en metros a cada lado del haz.")]
    [SerializeField] private float killWidth = 0.45f;

    /// <summary>Configura el haz desde código (lo usa el constructor de escena).</summary>
    public void Setup(Transform pivotTransform, float beamLength, float speedDegrees)
    {
        pivot = pivotTransform;
        length = beamLength;
        degreesPerSecond = speedDegrees;
    }

    /// <summary>Coloca el haz en un ángulo concreto (0 = hacia +Z). Para pruebas.</summary>
    public void SetAngle(float degrees)
    {
        pivot.localRotation = Quaternion.Euler(0f, degrees, 0f);
    }

    protected override void Tick()
    {
        pivot.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);

        // ¿El cuerpo de Kael cruza la altura del haz? Si saltó por encima (pies más altos que el haz), se salva.
        float beamY = transform.position.y + beamHeight;
        float feet = Player.transform.position.y;
        if (feet > beamY + 0.15f || feet + 1.2f < beamY)
        {
            return;
        }

        // Punto del haz más cercano a Kael, visto desde arriba.
        Vector3 origin = transform.position;
        Vector3 direction = pivot.forward;
        direction.y = 0f;
        direction.Normalize();
        Vector3 toPlayer = Player.transform.position - origin;
        toPlayer.y = 0f;
        float along = Mathf.Clamp(Vector3.Dot(toPlayer, direction), 0.8f, length); // 0.8: el pilar central no mata
        Vector3 closest = origin + direction * along;

        TryKill(new Vector3(closest.x, PlayerCenter.y, closest.z), killWidth);
    }
}
