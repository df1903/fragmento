using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Cada vez que Kael muere y reaparece, lo hace un poco más rápido y con un salto más alto,
///   hasta un tope. Muestra un mensaje corto en el HUD.
///
/// POR QUÉ EXISTE
///   Dificultad decreciente: el inicio del nivel es duro y cada muerte da una ventaja, así que
///   fallar enseña y recompensa en vez de solo castigar. No toca el movimiento: solo cambia los
///   multiplicadores públicos de <see cref="PlayerController"/>.
///
/// CÓMO SE USA
///   Va en el prefab de Kael junto a PlayerController. Los porcentajes se ajustan en el Inspector.
///   Las muertes se guardan en <see cref="PlayerProgress"/>, así que las mejoras se conservan al
///   recargar el nivel o pasar al siguiente. Escucha GameEvents.PlayerRespawned (una vez por muerte).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerPowerUp : MonoBehaviour
{
    [Tooltip("Aumento de velocidad por muerte (0.08 = +8 %).")]
    [SerializeField] private float speedBonusPerDeath = 0.08f;

    [Tooltip("Aumento máximo de velocidad (0.6 = +60 %).")]
    [SerializeField] private float maxSpeedBonus = 0.6f;

    [Tooltip("Aumento de altura de salto por muerte (0.06 = +6 %).")]
    [SerializeField] private float jumpBonusPerDeath = 0.06f;

    [Tooltip("Aumento máximo de altura de salto (0.4 = +40 %).")]
    [SerializeField] private float maxJumpBonus = 0.4f;

    private PlayerController player;

    /// <summary>Muertes acumuladas en esta partida.</summary>
    public int Deaths => PlayerProgress.Deaths;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Si la escena se recargó (o es el nivel siguiente), Kael conserva lo ganado.
        ApplyBonuses();
    }

    private void OnEnable()
    {
        GameEvents.PlayerRespawned += OnPlayerRespawned;
    }

    private void OnDisable()
    {
        GameEvents.PlayerRespawned -= OnPlayerRespawned;
    }

    private void OnPlayerRespawned(Vector3 position)
    {
        PlayerProgress.Deaths++;
        ApplyBonuses();
        GameEvents.RaiseMessageRequested("Kael se vuelve más fuerte  (muertes: " + Deaths + ")");
    }

    private void ApplyBonuses()
    {
        player.SpeedMultiplier = 1f + Mathf.Min(Deaths * speedBonusPerDeath, maxSpeedBonus);
        player.JumpMultiplier = 1f + Mathf.Min(Deaths * jumpBonusPerDeath, maxJumpBonus);
    }
}
