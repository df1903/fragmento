using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Muestra en pantalla el contador de fragmentos ("Fragmentos 2 / 7") y mensajes cortos que aparecen
///   y se desvanecen solos (por ejemplo, el texto de cada fragmento al absorberlo).
///
/// POR QUÉ EXISTE
///   Es el único script que dibuja información de juego. Solo ESCUCHA eventos (FragmentCountChanged y
///   MessageRequested); no conoce ni a Kael ni a los fragmentos, así que se puede cambiar el diseño del HUD
///   sin tocar nada más.
///
/// CÓMO SE USA
///   Va en el objeto HUD (Canvas) que crea Fragmento > Construir Escena, con sus dos textos TextMeshPro
///   y un CanvasGroup para el mensaje. Para mostrar un mensaje desde cualquier script:
///   GameEvents.RaiseMessageRequested("Texto corto");
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text messageText;

    [Tooltip("Grupo que controla la transparencia del mensaje.")]
    [SerializeField] private CanvasGroup messageGroup;

    [Tooltip("Segundos que el mensaje se queda visible.")]
    [SerializeField] private float messageDuration = 4f;

    [Tooltip("Segundos que tarda el mensaje en aparecer y en desaparecer.")]
    [SerializeField] private float fadeDuration = 0.4f;

    private Coroutine messageRoutine;

    /// <summary>Texto actual del contador (para pruebas y depuración).</summary>
    public string CounterLabel => counterText != null ? counterText.text : "";

    /// <summary>Texto actual del mensaje (para pruebas y depuración).</summary>
    public string CurrentMessage => messageText != null ? messageText.text : "";

    /// <summary>Transparencia actual del mensaje, de 0 a 1 (para pruebas y depuración).</summary>
    public float MessageAlpha => messageGroup != null ? messageGroup.alpha : 0f;

    private void Awake()
    {
        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        GameEvents.FragmentCountChanged += OnFragmentCountChanged;
        GameEvents.MessageRequested += OnMessageRequested;
    }

    private void OnDisable()
    {
        GameEvents.FragmentCountChanged -= OnFragmentCountChanged;
        GameEvents.MessageRequested -= OnMessageRequested;
    }

    private void OnFragmentCountChanged(int collected, int total)
    {
        if (counterText != null)
        {
            counterText.text = "Fragmentos  " + collected + " / " + total;
        }
    }

    private void OnMessageRequested(string message)
    {
        if (string.IsNullOrEmpty(message) || messageText == null || messageGroup == null)
        {
            return;
        }

        // Un mensaje nuevo reemplaza al anterior.
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }
        messageRoutine = StartCoroutine(ShowMessage(message));
    }

    private IEnumerator ShowMessage(string message)
    {
        messageText.text = message;

        yield return Fade(messageGroup.alpha, 1f);
        yield return new WaitForSeconds(messageDuration);
        yield return Fade(1f, 0f);

        messageRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            messageGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        messageGroup.alpha = to;
    }
}
