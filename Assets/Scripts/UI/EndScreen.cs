using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Pantalla final "Región restaurada": un panel que aparece con un fundido al cruzar el portal, con un
///   texto placeholder de la siguiente región. Libera el cursor y quita el control a Kael.
///
/// POR QUÉ EXISTE
///   Cierra el bucle del nivel (regla: cruzar el portal muestra "Región restaurada"). Solo escucha
///   GameEvents.LevelCompleted; no conoce al portal.
///
/// CÓMO SE USA
///   Va en el Canvas del HUD, con un panel hijo (con CanvasGroup) que empieza oculto. Fragmento > Construir
///   Escena lo crea. Por ahora no hay botón: el menú principal llega en el siguiente bloque de H3.
/// </summary>
public class EndScreen : MonoBehaviour
{
    [Tooltip("Grupo del panel final (controla su transparencia).")]
    [SerializeField] private CanvasGroup panel;

    [Tooltip("Segundos que tarda el panel en aparecer.")]
    [SerializeField] private float fadeSeconds = 1.5f;

    /// <summary>True cuando el panel ya se mostró (para pruebas y depuración).</summary>
    public bool IsShown { get; private set; }

    private void Awake()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEvents.LevelCompleted += Show;
    }

    private void OnDisable()
    {
        GameEvents.LevelCompleted -= Show;
    }

    private void Show()
    {
        if (IsShown || panel == null)
        {
            return;
        }

        IsShown = true;
        GameEvents.RaiseCinematicStarted(); // Kael y la cámara dejan de responder
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        panel.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            panel.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }
        panel.alpha = 1f;
    }
}
