using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Guarda el progreso de Kael que debe sobrevivir cuando la escena se recarga (por quedarse sin vidas)
///   o cuando se pasa al siguiente nivel: por ahora, el número de muertes acumuladas.
///
/// POR QUÉ EXISTE
///   Las mejoras de velocidad y salto dependen de las muertes (<see cref="PlayerPowerUp"/>). Al recargar la escena
///   todos los componentes se recrean, así que ese número no puede vivir en ellos. Una clase estática es lo más
///   simple que funciona (sin guardar archivos).
///
/// CÓMO SE USA
///   PlayerProgress.Deaths para leer y sumar. PlayerProgress.Reset() para empezar una partida nueva (las pruebas
///   lo llaman antes de cargar un nivel). Se reinicia sola al arrancar el juego.
/// </summary>
public static class PlayerProgress
{
    /// <summary>Muertes acumuladas en esta partida (se conservan entre escenas).</summary>
    public static int Deaths { get; set; }

    public static void Reset()
    {
        Deaths = 0;
    }

    // Igual que GameEvents: con "Enter Play Mode Options" los estáticos sobreviven entre sesiones de Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Reset();
    }
}
