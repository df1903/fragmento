using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba las vidas visibles (3 corazones, 0 vidas reinicia el nivel conservando las mejoras) y las reglas de
///   justicia de los enemigos: aviso antes de embestir, quietos durante las cámaras de evento.
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Escena (usa el nivel 1).
/// </summary>
public class LivesAndHazardTests : LevelTestBase
{
    [UnityTest]
    public IEnumerator Cada_muerte_resta_una_vida_y_el_HUD_lo_muestra()
    {
        yield return LoadLevel(false, true);
        yield return null;

        LivesManager lives = Object.FindFirstObjectByType<LivesManager>();
        LivesDisplay display = Object.FindFirstObjectByType<LivesDisplay>();
        Assert.IsNotNull(lives, "Falta LivesManager. Regenera la escena.");
        Assert.IsNotNull(display, "Falta LivesDisplay en el HUD. Regenera la escena.");
        Assert.AreEqual(3, lives.Lives);
        Assert.AreEqual(3, display.FullHearts);

        GameEvents.RaisePlayerDied();
        yield return null;

        Assert.AreEqual(2, lives.Lives);
        Assert.AreEqual(2, display.FullHearts);
    }

    [UnityTest]
    public IEnumerator Sin_vidas_el_nivel_se_reinicia_y_Kael_conserva_las_mejoras()
    {
        yield return LoadLevel(false, true);
        yield return null;

        for (int i = 0; i < 3; i++)
        {
            GameEvents.RaisePlayerDied();
            yield return null;
        }

        yield return new WaitForSeconds(2.5f); // 1.5 s de aviso + recarga
        yield return null;

        LivesManager lives = Object.FindFirstObjectByType<LivesManager>();
        player = Object.FindFirstObjectByType<PlayerController>();
        Assert.IsNotNull(lives);
        Assert.AreEqual(3, lives.Lives, "Tras reiniciar, las vidas vuelven a 3.");
        Assert.AreEqual(3, PlayerProgress.Deaths);
        Assert.Greater(player.SpeedMultiplier, 1f, "Las mejoras por muerte se conservan al reiniciar.");
    }

    [UnityTest]
    public IEnumerator El_enemigo_avisa_antes_de_embestir()
    {
        yield return LoadLevel(true);
        RedSentinel sentinel = OnlySentinel("Centinela A1");

        // Kael a 4 m: dentro del radio de embestida (5 m).
        Teleport(sentinel.transform.position + new Vector3(0f, -0.65f, 4f));

        float timeout = 3f;
        while (!sentinel.IsWindingUp && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.IsTrue(sentinel.IsWindingUp, "Debe haber un aviso (parada y brillo) antes de embestir.");

        timeout = 2f;
        while (!sentinel.IsDashing && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.IsTrue(sentinel.IsDashing, "Tras el aviso debe embestir.");
    }

    [UnityTest]
    public IEnumerator Durante_una_camara_de_evento_los_enemigos_no_matan_ni_se_mueven()
    {
        yield return LoadLevel(true);
        RedSentinel sentinel = OnlySentinel("Centinela A1");

        int deaths = 0;
        System.Action counter = () => deaths++;
        GameEvents.PlayerDied += counter;

        GameEvents.RaiseCinematicStarted();
        Vector3 before = sentinel.transform.position;
        Teleport(before + new Vector3(0f, -0.65f, 0.3f)); // encima del enemigo
        yield return new WaitForSeconds(1f);

        Assert.AreEqual(0, deaths, "Un enemigo quieto por una animación no debe matar.");
        Assert.AreEqual(0f, Vector3.Distance(before, sentinel.transform.position), 0.001f, "El enemigo debe quedarse quieto.");

        GameEvents.RaiseCinematicEnded();
        yield return new WaitForSeconds(2f); // 0.5 s de margen y luego puede actuar
        GameEvents.PlayerDied -= counter;

        Assert.GreaterOrEqual(deaths, 1, "Al terminar la animación el enemigo vuelve a ser peligroso.");
    }

    // Apaga todos los peligros menos el centinela indicado.
    private static RedSentinel OnlySentinel(string name)
    {
        RedSentinel chosen = null;
        foreach (Hazard hazard in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
        {
            if (hazard.name == name && hazard is RedSentinel sentinel)
            {
                chosen = sentinel;
            }
            else
            {
                hazard.enabled = false;
            }
        }

        Assert.IsNotNull(chosen, "No se encontró " + name);
        return chosen;
    }
}
