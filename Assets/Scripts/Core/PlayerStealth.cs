using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Guarda una sola cosa: si Kael está escondido ahora mismo. La hierba alta lo marca en cada fotograma y los
///   enemigos lo consultan.
///
/// POR QUÉ EXISTE
///   Así la hierba (TallGrass) y los lobos (ShadowWolf) no se conocen entre sí. Se usa una "marca de tiempo" en
///   vez de un verdadero/falso: si la hierba deja de marcar (Kael salió o se teletransportó), el escondite
///   caduca solo y nunca se queda encendido por error.
///
/// CÓMO SE USA
///   Para esconder: PlayerStealth.HideThisFrame() en cada fotograma. Para consultar: PlayerStealth.IsHidden.
/// </summary>
public static class PlayerStealth
{
    private static float hiddenUntil;

    /// <summary>True si algo ha escondido a Kael en la última décima de segundo.</summary>
    public static bool IsHidden => Time.time < hiddenUntil;

    public static void HideThisFrame()
    {
        hiddenUntil = Time.time + 0.1f;
    }

    // Time.time vuelve a 0 al iniciar Play, pero un estático sobrevive: se limpia.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hiddenUntil = 0f;
    }
}
