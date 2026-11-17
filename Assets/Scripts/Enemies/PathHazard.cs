using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Peligro que recorre una ruta de puntos sin parar (el tren de juguete que da vueltas a la alfombra, el coche
///   de cuerda que va y viene por la mesa). Mira hacia donde va y mata a Kael si lo atropella.
///
/// POR QUÉ EXISTE
///   Es el peligro "de juguete" del nivel 9: no ve ni persigue, solo marca un ritmo. Hay que cruzar su camino en
///   el momento justo. Un solo script sirve para rutas cerradas (vuelta) y de ida y vuelta.
///
/// CÓMO SE USA
///   "Points" son los puntos de la ruta en el mundo (a la altura del suelo). "Loop" marcado: al llegar al último
///   vuelve al primero (círculo); desmarcado: deshace el camino. "Kill Radius" es el tamaño del juguete.
/// </summary>
public class PathHazard : Hazard
{
    [SerializeField] private Vector3[] points = new Vector3[0];
    [SerializeField] private bool loop = true;
    [SerializeField] private float speed = 6f;
    [SerializeField] private float killRadius = 1.4f;

    private int index = 1;
    private int direction = 1;

    /// <summary>Configura la ruta desde código (la usa el constructor de escena).</summary>
    public void Setup(Vector3[] path, bool closedLoop, float moveSpeed, float radius)
    {
        points = path;
        loop = closedLoop;
        speed = moveSpeed;
        killRadius = radius;
    }

    private void Awake()
    {
        if (points.Length > 0)
        {
            transform.position = points[0];
        }
    }

    protected override void Tick()
    {
        if (points.Length < 2)
        {
            return;
        }

        Vector3 target = points[index];
        Vector3 step = target - transform.position;
        if (step.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(step.x, 0f, step.z).sqrMagnitude > 0.0001f ? new Vector3(step.x, 0f, step.z) : transform.forward, Vector3.up);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            NextPoint();
        }

        // Mide en horizontal y a la altura del juguete: saltar por encima salva.
        Vector3 toPlayer = Player.transform.position - transform.position;
        if (toPlayer.y < 1.1f && toPlayer.y > -1f)
        {
            toPlayer.y = 0f;
            if (toPlayer.magnitude < killRadius)
            {
                TryKill(PlayerCenter, 10f);
            }
        }
    }

    private void NextPoint()
    {
        if (loop)
        {
            index = (index + 1) % points.Length;
            return;
        }

        if (index + direction >= points.Length || index + direction < 0)
        {
            direction = -direction;
        }

        index += direction;
    }
}
