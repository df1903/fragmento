using System.Collections;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Muestra uno o varios mensajes cortos al empezar el nivel (los controles nuevos), uno detrás de otro.
///
/// POR QUÉ EXISTE
///   Los niveles 6 y 7 añaden correr, el Pulso de luz y trepar. Sin un aviso, el jugador no sabría que existen.
///   Reutiliza el texto del HUD: solo lanza GameEvents.MessageRequested.
///
/// CÓMO SE USA
///   Objeto vacío con este script. "Hints" son los textos; "Seconds Between" la pausa entre uno y otro.
/// </summary>
public class LevelHint : MonoBehaviour
{
    [SerializeField] private string[] hints = new string[0];
    [SerializeField] private float secondsBetween = 5f;

    /// <summary>Configura los mensajes desde código (lo usa el constructor de escena).</summary>
    public void Setup(params string[] texts)
    {
        hints = texts;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        foreach (string hint in hints)
        {
            GameEvents.RaiseMessageRequested(hint);
            yield return new WaitForSeconds(secondsBetween);
        }
    }
}
