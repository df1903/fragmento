using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QUÉ HACE
///   Dibuja las vidas de Kael como una fila de corazones: rojos los que quedan, apagados los perdidos.
///
/// POR QUÉ EXISTE
///   Solo escucha GameEvents.LivesChanged; no conoce a <see cref="LivesManager"/> ni a Kael, igual que
///   HUDController con el contador de fragmentos.
///
/// CÓMO SE USA
///   Va en el Canvas del HUD, con una lista de Images (una por vida). Lo crea Fragmento > Construir Escena.
/// </summary>
public class LivesDisplay : MonoBehaviour
{
    [Tooltip("Una Image por vida, de izquierda a derecha.")]
    [SerializeField] private Image[] hearts;

    [SerializeField] private Color fullColor = new Color(1f, 0.15f, 0.2f);
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.2f, 0.28f, 0.6f);

    /// <summary>Cuántos corazones están llenos ahora (para pruebas y depuración).</summary>
    public int FullHearts { get; private set; }

    private void OnEnable()
    {
        GameEvents.LivesChanged += OnLivesChanged;
    }

    private void OnDisable()
    {
        GameEvents.LivesChanged -= OnLivesChanged;
    }

    private void OnLivesChanged(int current, int max)
    {
        FullHearts = current;
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < current ? fullColor : emptyColor;
        }
    }
}
