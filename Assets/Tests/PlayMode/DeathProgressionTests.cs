using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba la dificultad decreciente: cada muerte (caída o enemigo rojo) devuelve a Kael al
///   checkpoint y lo hace más rápido y con más salto, con tope; y que los enemigos persiguen sin salir de su área.
///
/// CÓMO SE USA
///   Unity Test Runner > PlayMode. Requiere haber ejecutado Fragmento > Construir Escena.
/// </summary>
public class DeathProgressionTests : LevelTestBase
{
    [UnityTest]
    public IEnumerator Death_RespawnsAtCheckpoint_AndGrantsBonuses()
    {
        yield return LoadLevel();
        var powerUp = player.GetComponent<PlayerPowerUp>();
        Assert.IsNotNull(powerUp, "Kael no tiene PlayerPowerUp. Regenera la escena.");
        Assert.AreEqual(1f, player.SpeedMultiplier, 0.001f);

        Teleport(new Vector3(0f, 0.05f, 5f));
        yield return null;
        GameEvents.RaisePlayerDied();
        yield return null;

        Assert.AreEqual(1, powerUp.Deaths);
        Assert.Greater(player.SpeedMultiplier, 1f);
        Assert.Greater(player.JumpMultiplier, 1f);
        Assert.Less(Vector3.Distance(player.transform.position, new Vector3(0f, 0.05f, -10f)), 1f, "Debe reaparecer en el inicio.");
    }

    [UnityTest]
    public IEnumerator Bonuses_AreCapped()
    {
        yield return LoadLevel();

        for (int i = 0; i < 50; i++)
        {
            GameEvents.RaisePlayerDied();
            yield return null;
        }

        Assert.AreEqual(1.6f, player.SpeedMultiplier, 0.001f);
        Assert.AreEqual(1.4f, player.JumpMultiplier, 0.001f);
    }

    [UnityTest]
    public IEnumerator Sentinel_KillsOnTouch_ChasesAndStaysInArea()
    {
        yield return LoadLevel(true);
        // Los demás centinelas se apagan: solo importa A1.
        RedSentinel sentinel = null;
        foreach (RedSentinel s in Object.FindObjectsByType<RedSentinel>(FindObjectsSortMode.None))
        {
            if (s.name == "Centinela A1") { sentinel = s; } else { s.enabled = false; }
        }
        Assert.IsNotNull(sentinel);

        int deaths = 0;
        GameEvents.PlayerDied += () => deaths++;

        // Kael a 6 m del centinela: debe empezar a perseguirlo.
        Teleport(sentinel.transform.position + new Vector3(0f, -0.65f, 6f));
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(sentinel.IsChasing, "El centinela debe perseguir a Kael cercano.");

        // Al alcanzarlo lo mata.
        yield return new WaitForSeconds(4f);
        Assert.GreaterOrEqual(deaths, 1, "El centinela debe matar a Kael al tocarlo.");

        // Nunca sale de la isla A (x e z dentro de ±15).
        Assert.LessOrEqual(Mathf.Abs(sentinel.transform.position.x), 15f);
        Assert.LessOrEqual(Mathf.Abs(sentinel.transform.position.z), 15f);
    }
}
