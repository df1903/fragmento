using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   El cierre del juego (nivel 10): al cruzar el último portal, la pantalla se funde a blanco y aparecen, una
///   tras otra, las frases finales y los créditos. Al terminar, Enter vuelve al primer nivel.
///
/// POR QUÉ EXISTE
///   Sustituye a la pantalla "Región restaurada" solo en el último nivel: el juego necesita un final, no un
///   "siguiente nivel". Escucha GameEvents.LevelCompleted, igual que EndScreen.
///
/// CÓMO SE USA
///   Va en el Canvas del HUD. "Panel" es un panel blanco a pantalla completa (con CanvasGroup) y "Line" el texto
///   donde se escriben las frases. "Lines" son las frases y los créditos: se pueden cambiar en el Inspector
///   (por ejemplo, para poner los nombres del equipo).
/// </summary>
public class FinalCredits : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text line;
    [SerializeField] private TMP_Text hint;

    [TextArea]
    [SerializeField] private string[] lines = new string[0];

    [SerializeField] private float fadeSeconds = 2.5f;
    [SerializeField] private float lineSeconds = 3.2f;
    [SerializeField] private string firstSceneName = "Nivel01_Santuario";

    private bool canRestart;

    /// <summary>True cuando empezó el final (para pruebas).</summary>
    public bool IsShown { get; private set; }

    public void Setup(CanvasGroup whitePanel, TMP_Text lineText, TMP_Text hintText, string[] creditLines)
    {
        panel = whitePanel;
        line = lineText;
        hint = hintText;
        lines = creditLines;
    }

    private void Awake()
    {
        panel.alpha = 0f;
        panel.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
    }

    private void OnEnable() => GameEvents.LevelCompleted += Show;
    private void OnDisable() => GameEvents.LevelCompleted -= Show;

    private void Update()
    {
        if (canRestart && Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            canRestart = false;
            PlayerProgress.Reset(); // una partida nueva empieza sin las mejoras acumuladas
            SceneManager.LoadScene(firstSceneName);
        }
    }

    private void Show()
    {
        if (IsShown)
        {
            return;
        }

        IsShown = true;
        GameEvents.RaiseCinematicStarted();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(PlayCredits());
    }

    private IEnumerator PlayCredits()
    {
        panel.gameObject.SetActive(true);
        line.text = "";
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            panel.alpha = t / fadeSeconds;
            yield return null;
        }
        panel.alpha = 1f;

        foreach (string text in lines)
        {
            line.text = text;
            yield return FadeLine(0f, 1f, 0.6f);
            yield return new WaitForSeconds(lineSeconds);
            yield return FadeLine(1f, 0f, 0.6f);
        }

        line.text = "Fragmento\nEl Restaurador de Sombras";
        yield return FadeLine(0f, 1f, 1f);
        hint.gameObject.SetActive(true);
        canRestart = true;
    }

    private IEnumerator FadeLine(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            line.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }

        line.alpha = to;
    }
}
