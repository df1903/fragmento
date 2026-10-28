using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba el Nivel 2 (Cavernas de Cristal): que la escena tiene todo lo necesario, que los cristales
///   crecen y se pueden pisar, que las torretas disparan solo a lo que ven en rango, que sus proyectiles matan
///   y que el portal se abre al restaurar el Núcleo.
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Nivel 2.
/// </summary>
public class Nivel02Tests : LevelTestBase
{
    protected override string LevelScene => "Nivel02_Cavernas";

    [UnityTest]
    public IEnumerator La_escena_tiene_fragmentos_peligros_vidas_y_portal()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 7; id++)
        {
            Fragment fragment = FindFragment(id);
            Assert.IsNotNull(fragment, "Falta el fragmento " + id);
            Assert.IsTrue(Physics.Raycast(fragment.transform.position, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore),
                "El fragmento " + id + " flota: no hay suelo a menos de 3 m.");
        }

        Assert.GreaterOrEqual(Object.FindObjectsByType<RedSentinel>(FindObjectsSortMode.None).Length, 8, "Faltan sombras de roca.");
        Assert.GreaterOrEqual(Object.FindObjectsByType<CrystalTurret>(FindObjectsSortMode.None).Length, 5, "Faltan torretas.");
        Assert.AreEqual(3, Object.FindObjectsByType<CrystalGrow>(FindObjectsSortMode.None).Length);
        Assert.IsNotNull(Object.FindFirstObjectByType<LivesManager>());
        Assert.IsNotNull(Object.FindFirstObjectByType<LivesDisplay>());
        Assert.IsNotNull(Object.FindFirstObjectByType<ExitPortal>());
        Assert.IsNotNull(Object.FindFirstObjectByType<OrbitRings>(), "Kael del nivel 2 debe tener anillos.");
    }

    [UnityTest]
    public IEnumerator El_fragmento_1_hace_crecer_los_cristales_y_se_pueden_pisar()
    {
        yield return LoadLevel();
        CrystalGrow grow = FindGrow("Cristales del lago");
        Assert.IsFalse(grow.HasRun);
        Assert.IsFalse(grow.transform.GetChild(0).gameObject.activeSelf, "Antes del fragmento los cristales no existen.");

        CollectFragment(1);
        yield return new WaitForSeconds(3f);

        Assert.IsTrue(grow.HasRun);
        for (int i = 0; i < grow.transform.childCount; i++)
        {
            Assert.IsTrue(grow.transform.GetChild(i).gameObject.activeSelf, "El cristal " + (i + 1) + " debe haber crecido.");
        }

        // Último pilar: cara superior en y = 0.5.
        Teleport(new Vector3(0f, 0.6f, 29.5f));
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(player.IsGrounded);
        Assert.AreEqual(0.5f, player.transform.position.y, 0.15f);
    }

    [UnityTest]
    public IEnumerator La_torreta_dispara_a_Kael_en_rango_y_el_proyectil_mata()
    {
        yield return LoadLevel(true);
        CrystalTurret turret = OnlyTurret("Torreta 2");

        int deaths = 0;
        System.Action counter = () => deaths++;
        GameEvents.PlayerDied += counter;

        Teleport(new Vector3(16f, 0.05f, 48f)); // 12 m al sur de la torreta, sin nada en medio
        float timeout = 8f;
        while (deaths == 0 && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        GameEvents.PlayerDied -= counter;

        Assert.GreaterOrEqual(turret.ShotsFired, 1, "La torreta debe haber disparado.");
        Assert.GreaterOrEqual(deaths, 1, "El proyectil debe matar a Kael si no se mueve.");
    }

    [UnityTest]
    public IEnumerator La_torreta_no_dispara_si_Kael_esta_fuera_de_rango()
    {
        yield return LoadLevel(true);
        CrystalTurret turret = OnlyTurret("Torreta 2");

        Teleport(new Vector3(-14f, 0.05f, -10f)); // a más de 70 m
        yield return new WaitForSeconds(5f);

        Assert.AreEqual(0, turret.ShotsFired);
    }

    [UnityTest]
    public IEnumerator Los_siete_fragmentos_abren_el_portal()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 7; id++)
        {
            CollectFragment(id);
            yield return WaitForControl(1.5f);
        }

        yield return new WaitForSeconds(4f);
        ExitPortal portal = Object.FindFirstObjectByType<ExitPortal>();
        Assert.IsTrue(portal.IsOpen, "Con los siete fragmentos el portal debe abrirse.");
    }

    private static CrystalGrow FindGrow(string objectName)
    {
        foreach (CrystalGrow grow in Object.FindObjectsByType<CrystalGrow>(FindObjectsSortMode.None))
        {
            if (grow.name == objectName)
            {
                return grow;
            }
        }

        Assert.Fail("No se encontró " + objectName);
        return null;
    }

    // Apaga todos los peligros menos la torreta indicada.
    private static CrystalTurret OnlyTurret(string name)
    {
        CrystalTurret chosen = null;
        foreach (Hazard hazard in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
        {
            if (hazard.name == name && hazard is CrystalTurret turret)
            {
                chosen = turret;
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
