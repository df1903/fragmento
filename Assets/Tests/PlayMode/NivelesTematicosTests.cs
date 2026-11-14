using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Pruebas comunes de los niveles 3, 4 y 5 (Observatorio, Jardín petrificado, Lago y cascadas): que la escena
///   tiene fragmentos con suelo, peligros, vidas y portal; que el primer fragmento hace crecer un camino pisable;
///   que las torretas disparan; que Kael tiene la forma de su etapa; y que los siete fragmentos abren el portal.
///
/// POR QUÉ EXISTE
///   Los tres niveles comparten recorrido y eventos (esqueleto de FragmentoSceneBuilder.Niveles.cs): las pruebas se
///   escriben una vez y cada nivel solo indica su escena, su cadena de crecimiento y las partes de Kael que debe tener.
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Nivel 3 / 4 / 5 (cada nivel con su menú).
/// </summary>
public abstract class NivelesTematicosTestsBase : LevelTestBase
{
    /// <summary>Nombre del objeto de la primera cadena que crece (evento del fragmento 1).</summary>
    protected abstract string Chain1Name { get; }

    /// <summary>Partes de Kael (hijos de "Visual") que debe tener en este nivel.</summary>
    protected abstract string[] KaelParts { get; }

    /// <summary>Altura esperada del CharacterController de Kael.</summary>
    protected abstract float KaelHeight { get; }

    /// <summary>True si Kael conserva los anillos orbitales (solo el nivel 3).</summary>
    protected abstract bool ExpectRings { get; }

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

        Assert.GreaterOrEqual(Object.FindObjectsByType<RedSentinel>(FindObjectsSortMode.None).Length, 8, "Faltan sombras.");
        Assert.GreaterOrEqual(Object.FindObjectsByType<CrystalTurret>(FindObjectsSortMode.None).Length, 5, "Faltan torretas.");
        Assert.AreEqual(3, Object.FindObjectsByType<CrystalGrow>(FindObjectsSortMode.None).Length);
        Assert.IsNotNull(Object.FindFirstObjectByType<LivesManager>());
        Assert.IsNotNull(Object.FindFirstObjectByType<LivesDisplay>());
        Assert.IsNotNull(Object.FindFirstObjectByType<ExitPortal>());
    }

    [UnityTest]
    public IEnumerator Kael_tiene_la_forma_de_su_etapa()
    {
        yield return LoadLevel();

        foreach (string part in KaelParts)
        {
            Assert.IsNotNull(player.transform.Find("Visual/" + part), "A Kael le falta la parte '" + part + "'.");
        }

        Assert.AreEqual(KaelHeight, controller.height, 0.01f, "Altura del CharacterController de Kael.");
        Assert.AreEqual(ExpectRings, Object.FindFirstObjectByType<OrbitRings>() != null, "Anillos orbitales de Kael.");
    }

    [UnityTest]
    public IEnumerator El_fragmento_1_hace_crecer_el_camino_y_se_puede_pisar()
    {
        yield return LoadLevel();
        CrystalGrow grow = FindGrow(Chain1Name);
        Assert.IsFalse(grow.HasRun);
        Assert.IsFalse(grow.transform.GetChild(0).gameObject.activeSelf, "Antes del fragmento el camino no existe.");

        CollectFragment(1);
        yield return new WaitForSeconds(3f);

        Assert.IsTrue(grow.HasRun);
        for (int i = 0; i < grow.transform.childCount; i++)
        {
            Assert.IsTrue(grow.transform.GetChild(i).gameObject.activeSelf, "El pilar " + (i + 1) + " debe haber crecido.");
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

/// <summary>Nivel 3, Observatorio en ruinas: Kael con torso y cabeza, aún con anillos.</summary>
public class Nivel03Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel03_Observatorio";
    protected override string Chain1Name => "Columnas del abismo";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza" };
    protected override float KaelHeight => 1.25f;
    protected override bool ExpectRings => true;
}

/// <summary>Nivel 4, Jardín petrificado: Kael con brazos y piernas.</summary>
public class Nivel04Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel04_Jardin";
    protected override string Chain1Name => "Troncos del barranco";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza", "Brazo izquierdo", "Brazo derecho", "Pierna izquierda", "Pierna derecha" };
    protected override float KaelHeight => 1.55f;
    protected override bool ExpectRings => false;
}

/// <summary>Nivel 5, Lago y cascadas: Kael con silueta completa y ropa de luz.</summary>
public class Nivel05Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel05_Lago";
    protected override string Chain1Name => "Piedras de cruce";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza", "Brazo izquierdo", "Pierna derecha", "Tunica", "Capa", "Bufanda" };
    protected override float KaelHeight => 1.7f;
    protected override bool ExpectRings => false;
}
