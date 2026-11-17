using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Pruebas de los niveles 3, 4 y 5 (Observatorio, Jardín petrificado, Lago y cascadas).
///   Comunes: la escena tiene fragmentos con suelo, vidas y portal; Kael tiene la forma de su etapa; los siete
///   fragmentos abren el portal; y los saltos normales de la ruta son posibles.
///   Propias de cada nivel: su dinámica (órbitas e ingravidez, hongos y hojas, corriente y marea) y su peligro
///   (haz que barre, zarzas, marea).
///
/// POR QUÉ EXISTE
///   Cada nivel tiene ahora su propio recorrido. Como Claude no puede jugarlos, estas pruebas son la red de
///   seguridad: comprueban que las piezas funcionan y que ningún salto anotado pide más de lo que Kael salta.
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Nivel 3 / 4 / 5 (cada nivel con su menú).
/// </summary>
public abstract class NivelesTematicosTestsBase : LevelTestBase
{
    /// <summary>Partes de Kael (hijos de "Visual") que debe tener en este nivel.</summary>
    protected abstract string[] KaelParts { get; }

    /// <summary>Altura esperada del CharacterController de Kael.</summary>
    protected abstract float KaelHeight { get; }

    /// <summary>True si Kael conserva los anillos orbitales (solo el nivel 3).</summary>
    protected abstract bool ExpectRings { get; }

    [UnityTest]
    public IEnumerator La_escena_tiene_fragmentos_vidas_y_portal()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 7; id++)
        {
            Fragment fragment = FindFragment(id);
            Assert.IsNotNull(fragment, "Falta el fragmento " + id);
            Assert.IsTrue(Physics.Raycast(fragment.transform.position, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore),
                "El fragmento " + id + " flota: no hay suelo a menos de 3 m.");
        }

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
    public IEnumerator Kael_empieza_sobre_suelo_firme()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(player.IsGrounded, "Kael debe aparecer sobre el suelo de la primera zona.");
    }

    [UnityTest]
    public IEnumerator Los_siete_fragmentos_abren_el_portal()
    {
        yield return CollectAllFragments();

        ExitPortal portal = Object.FindFirstObjectByType<ExitPortal>();
        Assert.IsTrue(portal.IsOpen, "Con los siete fragmentos el portal debe abrirse.");
    }

    // El constructor anota cada salto normal del recorrido en Nivel/Ruta (Desde, Hasta). Con todos los eventos
    // ya ejecutados, los dos extremos deben tener suelo y el salto no debe pedir más de 3 m de largo ni 1.3 m de alto
    // (Kael salta 1.8 m de alto y unos 4.9 m de largo: queda margen).
    [UnityTest]
    public IEnumerator Los_saltos_de_la_ruta_son_posibles()
    {
        yield return CollectAllFragments();

        GameObject route = GameObject.Find("Nivel/Ruta");
        Assert.IsNotNull(route, "El nivel no tiene marcadores de ruta.");
        Assert.Greater(route.transform.childCount, 0, "La ruta no tiene saltos anotados.");

        foreach (Transform jump in route.transform)
        {
            Vector3 from = jump.Find("Desde").position;
            Vector3 to = jump.Find("Hasta").position;

            Assert.IsTrue(HasGround(from), jump.name + ": no hay suelo en el punto de despegue " + from);
            Assert.IsTrue(HasGround(to), jump.name + ": no hay suelo en el punto de aterrizaje " + to);

            float rise = to.y - from.y;
            Vector3 flat = to - from;
            flat.y = 0f;
            Assert.LessOrEqual(rise, 1.3f, jump.name + ": sube demasiado.");
            Assert.LessOrEqual(flat.magnitude, 3f, jump.name + ": el hueco es demasiado largo.");
        }
    }

    // ------------------------------------------------------------------ Ayudas

    protected IEnumerator CollectAllFragments()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 7; id++)
        {
            CollectFragment(id);
            yield return WaitForControl(1.5f);
        }

        yield return new WaitForSeconds(5f); // las cadenas largas terminan de crecer
    }

    /// <summary>Recoge un fragmento y espera a que termine su evento y Kael recupere el control.</summary>
    protected IEnumerator CollectAndWait(int id)
    {
        CollectFragment(id);
        yield return WaitForControl(1.5f);
        yield return new WaitForSeconds(2f);
    }

    protected static bool HasGround(Vector3 point)
    {
        return Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Apaga todos los peligros menos el que se llama "name". Usar con LoadLevel(true).</summary>
    protected static T OnlyHazard<T>(string name) where T : Hazard
    {
        T chosen = null;
        foreach (Hazard hazard in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
        {
            if (hazard.name == name && hazard is T typed)
            {
                chosen = typed;
            }
            else
            {
                hazard.enabled = false;
            }
        }

        Assert.IsNotNull(chosen, "No se encontró " + name);
        return chosen;
    }

    protected static T FindByName<T>(string name) where T : Component
    {
        foreach (T item in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.name == name)
            {
                return item;
            }
        }

        Assert.Fail("No se encontró " + name);
        return null;
    }

    /// <summary>Cuenta las muertes de Kael durante "seconds" segundos (o hasta la primera, si stopAtFirst).</summary>
    protected IEnumerator CountDeaths(float seconds, bool stopAtFirst, System.Action<int> result)
    {
        int deaths = 0;
        System.Action counter = () => deaths++;
        GameEvents.PlayerDied += counter;

        float remaining = seconds;
        while (remaining > 0f && !(stopAtFirst && deaths > 0))
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        GameEvents.PlayerDied -= counter;
        result(deaths);
    }

    /// <summary>Altura máxima que alcanza Kael durante "seconds" segundos.</summary>
    protected IEnumerator MaxHeight(float seconds, System.Action<float> result)
    {
        float max = player.transform.position.y;
        float remaining = seconds;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            max = Mathf.Max(max, player.transform.position.y);
            yield return null;
        }

        result(max);
    }
}

/// <summary>Nivel 3, Observatorio en ruinas: órbitas, campo de ingravidez y haces que barren.</summary>
public class Nivel03Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel03_Observatorio";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza" };
    protected override float KaelHeight => 1.25f;
    protected override bool ExpectRings => true;

    [UnityTest]
    public IEnumerator Las_orbitas_giran_solo_tras_el_fragmento_3()
    {
        yield return LoadLevel();
        OrbitPlatformActivate orbit = FindByName<OrbitPlatformActivate>("Orbita A");
        Vector3 start = orbit.transform.position;
        Assert.IsFalse(orbit.IsMoving);

        yield return CollectAndWait(3);

        Assert.IsTrue(orbit.IsMoving, "Tras el fragmento 3 la plataforma debe girar.");
        Assert.Greater(Vector3.Distance(start, orbit.transform.position), 1f, "La plataforma debe haberse movido.");
        Assert.AreEqual(start.y, orbit.transform.position.y, 0.05f, "La órbita no cambia de altura.");
    }

    [UnityTest]
    public IEnumerator El_campo_de_ingravidez_alarga_el_salto()
    {
        yield return LoadLevel();
        Assert.AreEqual(0, Object.FindObjectsByType<LowGravityZone>(FindObjectsSortMode.None).Length, "El campo está apagado hasta el fragmento 4.");

        yield return CollectAndWait(4);

        // Borde oeste de la Terraza norte (suelo en y = 5), dentro del campo.
        Teleport(new Vector3(-10.5f, 5.05f, 78f));
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(player.IsGrounded);

        float max = 0f;
        Press(keyboard.spaceKey);
        yield return MaxHeight(2.5f, value => max = value);
        Release(keyboard.spaceKey);

        Assert.Greater(max - 5f, 3f, "Dentro del campo el salto debe superar los 3 m (el normal llega a 1.8 m).");
    }

    [UnityTest]
    public IEnumerator El_haz_mata_dentro_de_su_alcance()
    {
        yield return LoadLevel(true);
        OnlyHazard<SweepingBeam>("Haz de la terraza sur");

        Teleport(new Vector3(0f, 2.05f, 38f)); // a 5 m del centro: el haz (8 m) pasa por aquí una vez por vuelta
        int deaths = 0;
        yield return CountDeaths(13f, true, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "El haz debe matar a Kael si no lo salta.");
    }

    [UnityTest]
    public IEnumerator El_haz_no_mata_fuera_de_su_alcance()
    {
        yield return LoadLevel(true);
        OnlyHazard<SweepingBeam>("Haz de la terraza sur");

        Teleport(new Vector3(0f, 2.05f, 42.4f)); // a 9.4 m del centro: fuera del haz
        int deaths = 0;
        yield return CountDeaths(12f, false, value => deaths = value);
        Assert.AreEqual(0, deaths);
    }
}

/// <summary>Nivel 4, Jardín petrificado: hongos de rebote, hojas que se derrumban y zarzas.</summary>
public class Nivel04Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel04_Jardin";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza", "Brazo izquierdo", "Brazo derecho", "Pierna izquierda", "Pierna derecha" };
    protected override float KaelHeight => 1.55f;
    protected override bool ExpectRings => false;

    [UnityTest]
    public IEnumerator Los_hongos_despiertan_con_el_fragmento_2_y_lanzan_a_Kael()
    {
        yield return LoadLevel();
        Assert.AreEqual(0, Object.FindObjectsByType<BouncePad>(FindObjectsSortMode.None).Length, "Los hongos están apagados hasta el fragmento 2.");

        yield return CollectAndWait(2);
        BouncePad pad = FindByName<BouncePad>("Hongo de rebote A");
        Assert.IsTrue(pad.gameObject.activeInHierarchy);

        Teleport(pad.transform.position + Vector3.up * 0.05f);
        float max = 0f;
        yield return MaxHeight(2f, value => max = value);

        Assert.GreaterOrEqual(pad.Bounces, 1);
        Assert.Greater(max, 5.5f, "El rebote debe superar la altura de la Terraza alta (5 m).");
    }

    [UnityTest]
    public IEnumerator La_hoja_de_piedra_se_derrumba_y_vuelve()
    {
        yield return LoadLevel();
        CrumblingPlatform leaf = FindByName<CrumblingPlatform>("Hoja de piedra 1");

        Teleport(leaf.transform.position + Vector3.up * 0.35f);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(leaf.IsShaking, "Al pisarla, la hoja debe temblar.");

        yield return new WaitForSeconds(0.8f);
        Assert.IsFalse(leaf.IsSolid, "Tras el aviso, la hoja debe caer.");

        yield return new WaitForSeconds(3.3f);
        Assert.IsTrue(leaf.IsSolid, "La hoja debe volver a aparecer.");
    }

    [UnityTest]
    public IEnumerator Las_zarzas_matan_solo_a_quien_esta_encima()
    {
        yield return LoadLevel(true);
        ThornPatch patch = OnlyHazard<ThornPatch>("Zarzas fila A 2");

        // Al lado del parche (1.5 m fuera): un ciclo entero sin morir.
        Teleport(patch.transform.position + new Vector3(-3f, 0.05f, 0f));
        int deaths = 0;
        yield return CountDeaths(6f, false, value => deaths = value);
        Assert.AreEqual(0, deaths, "Fuera del parche las zarzas no matan.");

        // Encima: muere en cuanto brotan.
        Teleport(patch.transform.position + Vector3.up * 0.05f);
        yield return CountDeaths(6f, true, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Encima del parche las zarzas deben matar al brotar.");
    }
}

/// <summary>Nivel 5, Lago y cascadas: balsas, corriente y marea.</summary>
public class Nivel05Tests : NivelesTematicosTestsBase
{
    protected override string LevelScene => "Nivel05_Lago";
    protected override string[] KaelParts => new[] { "Torso", "Cabeza", "Brazo izquierdo", "Pierna derecha", "Tunica", "Capa", "Bufanda" };
    protected override float KaelHeight => 1.7f;
    protected override bool ExpectRings => false;

    [UnityTest]
    public IEnumerator La_balsa_navega_solo_tras_el_fragmento_1()
    {
        yield return LoadLevel();
        MovingPlatformActivate raft = FindByName<MovingPlatformActivate>("Balsa del embarcadero");
        Assert.IsFalse(raft.IsMoving);

        yield return CollectAndWait(1);
        Assert.IsTrue(raft.IsMoving, "Tras el fragmento 1 la balsa debe navegar.");
    }

    [UnityTest]
    public IEnumerator La_corriente_empuja_a_Kael()
    {
        yield return LoadLevel();

        // Cauce (suelo en y = 4), dentro de la corriente que empuja al este (+X). Sin tocar ninguna tecla.
        Teleport(new Vector3(44f, 4.05f, 110f));
        yield return new WaitForSeconds(0.8f);

        Assert.Greater(player.transform.position.x, 45.5f, "La corriente debe arrastrar a Kael hacia el este.");
    }

    [UnityTest]
    public IEnumerator La_marea_alta_ahoga_sobre_las_piedras_bajas()
    {
        yield return LoadLevel(true);
        TideWater tide = OnlyHazard<TideWater>("Marea");
        yield return CollectAndWait(2); // emergen las piedras de cruce

        // Primera piedra (cara superior en y = 0.2). Con marea baja se puede estar.
        Teleport(new Vector3(12.5f, 0.25f, 50f));
        int deaths = 0;
        if (tide.IsLow)
        {
            yield return CountDeaths(1f, false, value => deaths = value);
            Assert.AreEqual(0, deaths, "Con marea baja la piedra es segura.");
            Teleport(new Vector3(12.5f, 0.25f, 50f));
        }

        // Se espera a que suba: Kael debe ahogarse antes de que termine un ciclo completo.
        yield return CountDeaths(20f, true, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Con marea alta la piedra baja queda cubierta y Kael se ahoga.");
    }

    [UnityTest]
    public IEnumerator La_marea_no_alcanza_las_mesetas()
    {
        yield return LoadLevel(true);
        OnlyHazard<TideWater>("Marea");

        Teleport(new Vector3(0f, 1.55f, 0f)); // Orilla (y = 1.5)
        int deaths = 0;
        yield return CountDeaths(16f, false, value => deaths = value); // un ciclo entero de marea
        Assert.AreEqual(0, deaths, "Las mesetas quedan por encima de la marea alta.");
    }
}
