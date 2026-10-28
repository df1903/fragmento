using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba el checklist del hito H2 (vertical slice): HUD con contador y mensajes, los fragmentos 2 y 3
///   con sus eventos (escalera y plataforma móvil), que Kael viaje sobre la plataforma sin resbalar,
///   la caída al vacío con respawn en el último checkpoint y que ningún fragmento quede inalcanzable.
///
/// POR QUÉ EXISTE
///   Es la evidencia automática de que el slice (A → B → C → D) se puede completar y de que los sistemas
///   nuevos (respawn, HUD, plataformas) no rompen lo del prototipo.
///
/// CÓMO SE USA
///   Igual que PrototipoH1Tests: Test Runner > PlayMode, después de Fragmento > Construir Escena.
/// </summary>
public class VerticalSliceH2Tests : LevelTestBase
{
    // ------------------------------------------------------------------ HUD

    [UnityTest]
    public IEnumerator El_HUD_muestra_el_contador_y_el_mensaje_del_fragmento()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.3f);

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        Assert.IsNotNull(hud, "Falta el HUD en la escena.");
        StringAssert.Contains("0 / 7", hud.CounterLabel);
        Assert.AreEqual(0f, hud.MessageAlpha, 0.01f, "Al empezar no debe haber mensaje visible.");

        CollectFragment(1);
        yield return new WaitForSeconds(0.7f);

        StringAssert.Contains("1 / 7", hud.CounterLabel);
        StringAssert.Contains("puente", hud.CurrentMessage);
        Assert.Greater(hud.MessageAlpha, 0.9f, "El mensaje del fragmento debe verse.");
    }

    // ------------------------------------------------------------------ Fragmento 2: escalera

    [UnityTest]
    public IEnumerator El_fragmento_2_ensambla_la_escalera_y_Kael_sube_a_la_terraza_C()
    {
        yield return LoadLevel();

        StairsMaterialize stairs = Object.FindFirstObjectByType<StairsMaterialize>();
        Assert.IsNotNull(stairs);
        Assert.AreEqual(20, stairs.StepCount);
        foreach (Transform step in stairs.transform)
        {
            if (!step.name.StartsWith("Escalon")) continue; // también cuelgan las partículas del evento
            Assert.IsFalse(step.gameObject.activeSelf, "Antes del fragmento 2 no debe haber escalones.");
        }

        CollectFragment(2);
        yield return WaitForControl(4f);

        foreach (Transform step in stairs.transform)
        {
            if (!step.name.StartsWith("Escalon")) continue;
            Assert.IsTrue(step.gameObject.activeSelf, "Todos los escalones deben haber aparecido.");
            Assert.IsTrue(step.GetComponent<Collider>().enabled, "Cada escalón debe ser sólido al terminar.");
        }

        // Kael sube caminando hacia el este (cámara al norte: la tecla D va hacia +X).
        Teleport(new Vector3(13f, 0.05f, 45f));
        yield return new WaitForSeconds(0.5f);
        Press(keyboard.dKey);
        yield return new WaitForSeconds(4.5f);
        Release(keyboard.dKey);

        Vector3 position = player.transform.position;
        Assert.Greater(position.x, 30f, "Kael debería haber llegado a la terraza C (x > 30).");
        Assert.That(position.y, Is.InRange(4.9f, 5.3f), "Kael debería estar sobre la terraza (y ≈ 5).");
    }

    // ------------------------------------------------------------------ Fragmento 3: plataforma móvil

    [UnityTest]
    public IEnumerator El_fragmento_3_activa_la_plataforma_y_Kael_viaja_sobre_ella_sin_resbalar()
    {
        yield return LoadLevel();

        MovingPlatformActivate platform = Object.FindFirstObjectByType<MovingPlatformActivate>();
        Assert.IsNotNull(platform);
        Assert.IsFalse(platform.IsMoving, "La plataforma no debe moverse antes del fragmento 3.");

        CollectFragment(3);
        yield return new WaitForSeconds(1.2f);
        Assert.IsTrue(platform.IsMoving, "La plataforma debe moverse tras el fragmento 3.");

        // Kael aterriza sobre la plataforma en movimiento.
        Teleport(platform.transform.position + Vector3.up * 0.4f);
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(player.IsGrounded, "Kael debe estar apoyado en la plataforma.");

        Vector3 startOffset = player.transform.position - platform.transform.position;
        float startZ = player.transform.position.z;

        yield return new WaitForSeconds(2.5f);

        Vector3 offset = player.transform.position - platform.transform.position;
        Assert.Greater(player.transform.position.z - startZ, 5f, "La plataforma debe haber llevado a Kael varios metros al norte.");
        Assert.Less(Vector3.Distance(offset, startOffset), 0.25f, "Kael no debe resbalar sobre la plataforma.");
        Assert.IsTrue(player.IsGrounded);

        // Al llegar al extremo norte la plataforma queda a ras del mirador D: Kael baja caminando.
        float timeout = 10f;
        while (platform.transform.position.z < 73.5f && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.Greater(timeout, 0f, "La plataforma no llegó al mirador D a tiempo.");

        Press(keyboard.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(keyboard.wKey);

        Vector3 position = player.transform.position;
        Assert.Greater(position.z, 77f, "Kael debería haber pasado al mirador D (z > 77).");
        Assert.That(position.y, Is.InRange(4.9f, 5.3f), "Kael debería estar sobre el mirador (y ≈ 5).");
    }

    // ------------------------------------------------------------------ Respawn

    [UnityTest]
    public IEnumerator Al_caer_Kael_reaparece_en_el_inicio_aunque_haya_pisado_otra_isla()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.5f);

        // Pisa la isla B: su checkpoint queda registrado.
        Teleport(new Vector3(0f, 0.05f, 40f));
        yield return new WaitForSeconds(0.5f);
        RespawnManager respawn = Object.FindFirstObjectByType<RespawnManager>();
        Assert.Less(Vector3.Distance(respawn.LastCheckpoint, new Vector3(0f, 0.05f, 34f)), 0.1f, "El último checkpoint debe ser el de la isla B.");

        int respawns = 0;
        System.Action<Vector3> counter = _ => respawns++;
        GameEvents.PlayerRespawned += counter;

        // Cae por debajo de la isla B.
        Teleport(new Vector3(5f, -15f, 40f));
        yield return new WaitForSeconds(3f);
        GameEvents.PlayerRespawned -= counter;

        Assert.AreEqual(1, respawns, "Debe haber exactamente un respawn.");
        Assert.Less(Vector3.Distance(player.transform.position, new Vector3(0f, 0.05f, -10f)), 1.5f, "Kael debe reaparecer en el inicio del nivel, aunque haya pisado la isla B.");
        Assert.IsTrue(player.IsGrounded);
        Assert.Less(Vector3.Distance(Camera.main.transform.position, player.transform.position), 8f, "La cámara debe haber saltado con Kael.");
    }

    [UnityTest]
    public IEnumerator Si_Kael_cae_antes_de_pisar_otra_isla_reaparece_en_la_isla_de_inicio()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.5f);

        Teleport(new Vector3(3f, -15f, 0f));
        yield return new WaitForSeconds(3f);

        Assert.Less(Vector3.Distance(player.transform.position, new Vector3(0f, 0.05f, -10f)), 1.5f, "Kael debe reaparecer en el inicio.");
        Assert.IsTrue(player.IsGrounded);
    }

    // ------------------------------------------------------------------ Nivel completo (slice)

    [UnityTest]
    public IEnumerator Ruta_completa_del_slice_A_B_C_D()
    {
        yield return LoadLevel();

        var finished = new List<string>();
        System.Action<string> onFinished = id => finished.Add(id);
        GameEvents.WorldEventFinished += onFinished;

        // Fragmento 1 y puente de A a B.
        CollectFragment(1);
        yield return WaitForControl(3f);
        Teleport(new Vector3(0f, 0.05f, 11f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(3.5f);
        Release(keyboard.wKey);
        Assert.Greater(player.transform.position.z, 31f, "No se pudo cruzar el puente a la isla B.");

        // Fragmento 2 y escalera de B a C.
        CollectFragment(2);
        yield return WaitForControl(4f);
        Teleport(new Vector3(13f, 0.05f, 45f));
        yield return new WaitForSeconds(0.5f);
        Press(keyboard.dKey);
        yield return new WaitForSeconds(4.5f);
        Release(keyboard.dKey);
        Assert.Greater(player.transform.position.x, 30f, "No se pudo subir a la terraza C.");

        // Fragmento 3 y plataforma móvil de C a D.
        CollectFragment(3);
        yield return WaitForControl(1.2f);
        MovingPlatformActivate platform = Object.FindFirstObjectByType<MovingPlatformActivate>();
        Teleport(platform.transform.position + Vector3.up * 0.4f);

        float timeout = 15f;
        while (platform.transform.position.z < 73.5f && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Press(keyboard.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(keyboard.wKey);
        Assert.Greater(player.transform.position.z, 77f, "No se pudo llegar al mirador D.");

        GameEvents.WorldEventFinished -= onFinished;

        CollectionAssert.AreEquivalent(new List<string> { "puente_1", "escalera_2", "plataforma_3" }, finished,
            "Deben haber terminado exactamente los tres eventos, cada uno una vez.");
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        StringAssert.Contains("3 / 7", hud.CounterLabel);
    }

    // ------------------------------------------------------------------ Diseño del nivel

    [UnityTest]
    public IEnumerator Ningun_fragmento_flota_fuera_de_una_isla()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 3; id++)
        {
            Fragment fragment = FindFragment(id);
            Assert.IsNotNull(fragment, "Falta el fragmento " + id);

            bool groundBelow = Physics.Raycast(fragment.transform.position, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(groundBelow, "El fragmento " + id + " no tiene suelo a menos de 3 m: sería inalcanzable.");
        }
    }
}
