using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   Lleva las vidas de Kael (3 por defecto). Cada muerte resta una; al llegar a 0 avisa en el HUD y
///   recarga el nivel con las vidas llenas.
///
/// POR QUÉ EXISTE
///   Da peso a cada error sin quitar la mejora por muerte: la dificultad baja con cada muerte
///   (<see cref="PlayerPowerUp"/>) pero perder todas las vidas obliga a repetir el nivel.
///   Solo escucha PlayerDied y avisa con LivesChanged; no conoce al HUD ni a Kael.
///
/// CÓMO SE USA
///   Un único LivesManager en la escena (lo crea el constructor de escenas). Las muertes se siguen
///   contando en <see cref="PlayerProgress"/>, que sobrevive a la recarga.
/// </summary>
public class LivesManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;

    [Tooltip("Segundos que se muestra el aviso 'Sin vidas' antes de recargar.")]
    [SerializeField] private float restartDelay = 1.5f;

    private int lives;
    private bool restarting;

    public int Lives => lives;
    public int MaxLives => maxLives;

    private void Awake()
    {
        lives = maxLives;
    }

    private void OnEnable()
    {
        GameEvents.PlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied -= OnPlayerDied;
    }

    // Start corre después de todos los OnEnable: el HUD ya está suscrito y muestra los corazones llenos.
    private void Start()
    {
        GameEvents.RaiseLivesChanged(lives, maxLives);
    }

    private void OnPlayerDied()
    {
        if (restarting)
        {
            return;
        }

        lives = Mathf.Max(lives - 1, 0);
        GameEvents.RaiseLivesChanged(lives, maxLives);

        if (lives == 0)
        {
            restarting = true;
            StartCoroutine(RestartLevel());
        }
    }

    private IEnumerator RestartLevel()
    {
        GameEvents.RaiseMessageRequested("Sin vidas… el nivel se reinicia");
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
