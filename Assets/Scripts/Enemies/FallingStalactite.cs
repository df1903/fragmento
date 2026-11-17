using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Estalactita que cuelga del techo. Cuando Kael pasa por debajo aparece un círculo rojo de aviso en el suelo,
///   la roca tiembla y cae. Mata si le da. Después vuelve a subir y se rearma.
///
/// POR QUÉ EXISTE
///   Es el peligro propio del nivel 2 (Cavernas): obliga a mirar el suelo y el techo, no solo a los enemigos.
///   El aviso da tiempo de sobra para apartarse: es un peligro "justo".
///
/// CÓMO SE USA
///   La raíz se coloca en el SUELO, bajo la roca. "Rock" es la pieza que cae (empieza a "Hang Height" metros
///   sobre la raíz) y "Warning" el disco de aviso. Lo arma Fragmento > Construir Nivel 2.
/// </summary>
public class FallingStalactite : Hazard
{
    private enum State { Waiting, Warning, Falling, Resting, Rising }

    [SerializeField] private Transform rock;
    [SerializeField] private GameObject warning;

    [Tooltip("Altura a la que cuelga la roca sobre el suelo.")]
    [SerializeField] private float hangHeight = 12f;

    [Tooltip("Distancia horizontal a la que Kael la hace caer.")]
    [SerializeField] private float triggerRadius = 3f;

    [Tooltip("Segundos de aviso antes de caer.")]
    [SerializeField] private float warnSeconds = 0.9f;

    [Tooltip("Segundos que tarda en llegar al suelo.")]
    [SerializeField] private float fallSeconds = 0.35f;

    [Tooltip("Segundos clavada en el suelo antes de volver a subir.")]
    [SerializeField] private float restSeconds = 1.5f;

    [Tooltip("Radio en el que mata al caer.")]
    [SerializeField] private float killRadius = 1.3f;

    private State state = State.Waiting;
    private float timer;

    /// <summary>Veces que ha caído (para pruebas y depuración).</summary>
    public int Falls { get; private set; }

    /// <summary>True mientras avisa antes de caer (para pruebas).</summary>
    public bool IsWarning => state == State.Warning;

    /// <summary>Configura la estalactita desde código (la usa el constructor de escena).</summary>
    public void Setup(Transform rockTransform, GameObject warningObject, float height)
    {
        rock = rockTransform;
        warning = warningObject;
        hangHeight = height;
    }

    private void Awake()
    {
        warning.SetActive(false);
        rock.localPosition = Vector3.up * hangHeight;
    }

    protected override void Tick()
    {
        switch (state)
        {
            case State.Waiting:
                Vector3 toPlayer = Player.transform.position - transform.position;
                bool sameFloor = Mathf.Abs(toPlayer.y) < 3f;
                toPlayer.y = 0f;
                if (sameFloor && toPlayer.magnitude < triggerRadius)
                {
                    state = State.Warning;
                    timer = warnSeconds;
                    warning.SetActive(true);
                }
                break;

            case State.Warning:
                timer -= Time.deltaTime;
                // La roca tiembla: se ve que está a punto de soltarse.
                rock.localPosition = Vector3.up * hangHeight + new Vector3(Random.Range(-0.06f, 0.06f), 0f, Random.Range(-0.06f, 0.06f));
                if (timer <= 0f)
                {
                    state = State.Falling;
                    timer = fallSeconds;
                }
                break;

            case State.Falling:
                timer -= Time.deltaTime;
                float fallen = 1f - Mathf.Clamp01(timer / fallSeconds);
                rock.localPosition = Vector3.up * Mathf.Lerp(hangHeight, 0f, fallen * fallen); // acelera al caer
                TryKill(rock.position + Vector3.up * 0.9f, killRadius);
                if (timer <= 0f)
                {
                    Falls++;
                    state = State.Resting;
                    timer = restSeconds;
                    warning.SetActive(false);
                }
                break;

            case State.Resting:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Rising;
                    timer = 1f;
                }
                break;

            case State.Rising:
                timer -= Time.deltaTime;
                rock.localPosition = Vector3.up * Mathf.Lerp(hangHeight, 0f, Mathf.Clamp01(timer));
                if (timer <= 0f)
                {
                    state = State.Waiting;
                }
                break;
        }
    }
}
