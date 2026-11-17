using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Pruebas de los niveles 6 y 7 (Bosque del Crepúsculo y Aldea de Brumavieja).
///   Comunes: fragmentos con suelo, Kael humano con sus articulaciones, los siete fragmentos abren el portal y el
///   camino principal se puede caminar. Propias: el Pulso de luz, los faros, la hierba alta, los lobos, las
///   lianas y el arroyo (nivel 6); el puente levadizo, los faroles, los Ecos, la escalera, el montacargas y la
///   caja con su placa (nivel 7).
///
/// POR QUÉ EXISTE
///   Estos niveles ya no son islas con saltos medidos sino terreno continuo: la red de seguridad cambia de
///   "¿el salto es posible?" a "¿hay suelo y la pendiente se puede caminar a lo largo del camino?".
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Nivel 6 y Construir Nivel 7.
/// </summary>
public abstract class NivelesRealesTestsBase : LevelTestBase
{
    /// <summary>Partes propias de la etapa (rutas bajo "Visual").</summary>
    protected abstract string[] KaelParts { get; }

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
        Assert.IsNotNull(Object.FindFirstObjectByType<ExitPortal>());
    }

    [UnityTest]
    public IEnumerator Kael_es_humano_y_tiene_sus_habilidades()
    {
        yield return LoadLevel();

        string[] common =
        {
            "Cadera/Torso/Cabeza/Cara", "Cadera/Torso/Cabeza/Pelo", "Cadera/Torso/Capa", "Cadera/Torso/Brazo izquierdo/Antebrazo izquierdo",
            "Cadera/Muslo izquierdo/Pantorrilla izquierda", "Cadera/Muslo derecho/Pantorrilla derecha",
        };
        foreach (string part in common)
        {
            Assert.IsNotNull(player.transform.Find("Visual/" + part), "A Kael le falta '" + part + "'.");
        }

        foreach (string part in KaelParts)
        {
            Assert.IsNotNull(player.transform.Find("Visual/" + part), "A Kael le falta '" + part + "'.");
        }

        Assert.AreEqual(1.75f, controller.height, 0.01f);
        Assert.IsNotNull(player.GetComponent<LightPulse>(), "Kael debe tener el Pulso de luz.");
        Assert.IsNotNull(player.GetComponent<KaelAnimator>(), "Kael debe tener su animación.");
    }

    [UnityTest]
    public IEnumerator Kael_empieza_en_el_suelo_y_corre_con_shift()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.8f);
        Assert.IsTrue(player.IsGrounded, "Kael debe aparecer sobre el suelo.");

        Press(keyboard.wKey);
        yield return new WaitForSeconds(1f);
        float walking = player.HorizontalSpeed;
        Press(keyboard.leftShiftKey);
        yield return new WaitForSeconds(1f);
        float running = player.HorizontalSpeed;
        Assert.IsTrue(player.IsSprinting);
        Release(keyboard.leftShiftKey);
        Release(keyboard.wKey);

        Assert.Greater(running, walking * 1.3f, "Con Shift Kael debe ir claramente más rápido.");
    }

    [UnityTest]
    public IEnumerator Los_siete_fragmentos_abren_el_portal()
    {
        yield return CollectAllFragments();
        Assert.IsTrue(Object.FindFirstObjectByType<ExitPortal>().IsOpen, "Con los siete fragmentos el portal debe abrirse.");
    }

    // El constructor deja en Nivel/Camino un punto cada 3 m de cada tramo andable. Con todos los eventos hechos,
    // en cada punto debe haber suelo a su altura y entre dos puntos seguidos la pendiente no pasa de 45 grados.
    [UnityTest]
    public IEnumerator El_camino_principal_se_puede_caminar()
    {
        yield return CollectAllFragments();

        GameObject path = GameObject.Find("Nivel/Camino");
        Assert.IsNotNull(path, "El nivel no tiene puntos de camino.");
        foreach (Transform stretch in path.transform)
        {
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < stretch.childCount; i++)
            {
                Vector3 point = stretch.GetChild(i).position;
                bool hit = Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit ground, 5f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(hit, stretch.name + ": no hay suelo en " + point);
                Assert.AreEqual(point.y, ground.point.y, 0.7f, stretch.name + ": algo tapa el camino en " + point);

                if (i > 0)
                {
                    Vector3 flat = point - previous;
                    float rise = Mathf.Abs(flat.y);
                    flat.y = 0f;
                    Assert.LessOrEqual(rise, flat.magnitude + 0.05f, stretch.name + ": pendiente demasiado fuerte en " + point);
                }

                previous = point;
            }
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

        yield return new WaitForSeconds(5f);
    }

    protected IEnumerator CollectAndWait(int id, float extraSeconds = 3f)
    {
        CollectFragment(id);
        yield return WaitForControl(1.5f);
        yield return new WaitForSeconds(extraSeconds);
    }

    /// <summary>Deja a Kael sobre el suelo en (x, z): lo suelta desde arriba y espera a que aterrice.</summary>
    protected IEnumerator DropAt(float x, float z)
    {
        Assert.IsTrue(Physics.Raycast(new Vector3(x, 60f, z), Vector3.down, out RaycastHit ground, 120f, ~0, QueryTriggerInteraction.Ignore), "No hay suelo en " + x + ", " + z);
        Teleport(ground.point + Vector3.up * 0.1f);
        yield return new WaitForSeconds(0.5f);
    }

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

    protected IEnumerator CountDeaths(float seconds, System.Action<int> result)
    {
        int deaths = 0;
        System.Action counter = () => deaths++;
        GameEvents.PlayerDied += counter;
        yield return new WaitForSeconds(seconds);
        GameEvents.PlayerDied -= counter;
        result(deaths);
    }

    protected static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

/// <summary>Nivel 6, Bosque del Crepúsculo: Pulso, faros, hierba alta, lobos, lianas y arroyo.</summary>
public class Nivel06Tests : NivelesRealesTestsBase
{
    protected override string LevelScene => "Nivel06_Bosque";
    protected override string[] KaelParts => new[] { "Cadera/Torso/Corazon de luz" };

    [UnityTest]
    public IEnumerator El_arbol_cae_y_hace_de_puente_sobre_el_rio()
    {
        yield return LoadLevel();
        Vector3 overRiver = new Vector3(-22f, 8f, 84f);
        Physics.Raycast(overRiver, Vector3.down, out RaycastHit before, 20f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, -1f, "Antes del fragmento 1, sobre el río solo está el cauce.");

        yield return CollectAndWait(1, 4f);

        Assert.IsTrue(Physics.Raycast(overRiver, Vector3.down, out RaycastHit after, 20f, ~0, QueryTriggerInteraction.Ignore));
        Assert.Greater(after.point.y, 0.5f, "El tronco caído debe quedar sobre el río.");
    }

    [UnityTest]
    public IEnumerator El_pulso_enciende_el_faro_y_Kael_reaparece_en_el()
    {
        yield return LoadLevel();
        Beacon beacon = FindByName<Beacon>("Faro del rio");
        Assert.IsFalse(beacon.IsLit);

        yield return DropAt(beacon.transform.position.x + 2f, beacon.transform.position.z - 3f);
        Assert.IsTrue(player.GetComponent<LightPulse>().Trigger());
        yield return null;
        Assert.IsTrue(beacon.IsLit, "El Pulso debe encender el faro cercano.");
        Assert.IsFalse(player.GetComponent<LightPulse>().Trigger(), "El Pulso tiene recarga.");

        yield return DropAt(0f, 0f);
        GameEvents.RaisePlayerDied();
        yield return new WaitForSeconds(0.5f);
        Assert.Less(FlatDistance(player.transform.position, beacon.transform.position), 4f, "Kael debe reaparecer junto al faro encendido.");
    }

    [UnityTest]
    public IEnumerator La_hierba_alta_esconde_a_Kael_si_no_corre()
    {
        yield return LoadLevel();

        yield return DropAt(13f, 40f); // dentro del segundo parche de hierba alta
        Assert.IsTrue(PlayerStealth.IsHidden, "Quieto en la hierba alta, Kael está escondido.");

        Press(keyboard.wKey);
        yield return null; // cada tecla en su fotograma: dos pulsaciones simuladas a la vez se pisan
        Press(keyboard.leftShiftKey);
        yield return new WaitForSeconds(0.4f);
        bool hiddenWhileSprinting = PlayerStealth.IsHidden;
        Release(keyboard.leftShiftKey);
        Release(keyboard.wKey);
        Assert.IsFalse(hiddenWhileSprinting, "Corriendo, la hierba no esconde.");

        yield return DropAt(0f, -10f); // claro inicial, sin hierba alta
        Assert.IsFalse(PlayerStealth.IsHidden);
    }

    [UnityTest]
    public IEnumerator El_lobo_persigue_a_Kael_y_el_pulso_lo_aturde()
    {
        yield return LoadLevel(true);
        ShadowWolf wolf = OnlyHazard<ShadowWolf>("Lobo de sombra 2"); // guarida en (14, 42)

        yield return DropAt(1f, 42f); // a campo abierto, a la vista
        float timeout = 10f;
        while (!wolf.IsChasing && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.IsTrue(wolf.IsChasing, "El lobo debe perseguir a Kael si lo ve.");

        timeout = 6f;
        while (FlatDistance(wolf.transform.position, player.transform.position) > 7f && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        player.GetComponent<LightPulse>().Trigger();
        yield return null;
        Assert.IsTrue(wolf.IsStunned, "El Pulso de luz debe aturdir al lobo.");
    }

    [UnityTest]
    public IEnumerator Las_lianas_suben_a_Kael_a_la_meseta()
    {
        yield return LoadLevel();
        Assert.AreEqual(0, Object.FindObjectsByType<ClimbZone>(FindObjectsSortMode.None).Length, "Las lianas no existen hasta el fragmento 3.");
        yield return CollectAndWait(3);

        Teleport(new Vector3(28f, 1.4f, 147.4f)); // al pie del barranco, pegado a las lianas
        yield return new WaitForSeconds(0.4f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(3.5f);
        Release(keyboard.wKey);
        yield return new WaitForSeconds(0.6f);

        Assert.Greater(player.transform.position.y, 6.5f, "Caminando contra las lianas, Kael debe llegar arriba (la meseta está a 8 m).");
    }

    [UnityTest]
    public IEnumerator El_arroyo_ahoga_hasta_que_el_fragmento_4_lo_seca()
    {
        yield return LoadLevel();
        int deaths = 0;

        Teleport(new Vector3(-14f, 7.2f, 174f)); // en mitad del arroyo
        yield return CountDeaths(2f, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Con agua, caer al arroyo mata.");

        yield return CollectAndWait(4, 4f);
        Teleport(new Vector3(-14f, 7.2f, 174f));
        yield return CountDeaths(2f, value => deaths = value);
        Assert.AreEqual(0, deaths, "Con el arroyo seco se puede bajar al cauce.");
    }
}

/// <summary>Nivel 7, Aldea de Brumavieja: puente levadizo, faroles, Ecos, escalera, montacargas y caja con placa.</summary>
public class Nivel07Tests : NivelesRealesTestsBase
{
    protected override string LevelScene => "Nivel07_Aldea";
    protected override string[] KaelParts => new[] { "Cadera/Torso/Cabeza/Capucha", "Cadera/Torso/Zurron" };

    [UnityTest]
    public IEnumerator El_puente_levadizo_baja_con_el_fragmento_1()
    {
        yield return LoadLevel();
        Vector3 overCreek = new Vector3(0f, 2.5f, 30f);
        Physics.Raycast(overCreek, Vector3.down, out RaycastHit before, 10f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, -1f, "Antes del fragmento 1 el puente está levantado.");

        yield return CollectAndWait(1, 4f);

        Assert.IsTrue(Physics.Raycast(overCreek, Vector3.down, out RaycastHit after, 10f, ~0, QueryTriggerInteraction.Ignore));
        Assert.Greater(after.point.y, -0.3f, "El tablero debe quedar tendido sobre el arroyo.");
    }

    [UnityTest]
    public IEnumerator Los_faroles_de_la_calle_se_encienden_con_el_fragmento_2()
    {
        yield return LoadLevel();
        Beacon lantern = FindByName<Beacon>("Farol de la calle 3");
        Assert.IsFalse(lantern.IsLit);

        yield return CollectAndWait(2, 1f);
        Assert.IsTrue(lantern.IsLit);
        Assert.IsTrue(Beacon.IsInSafeLight(lantern.transform.position + Vector3.right * 2f), "Junto al farol encendido hay zona segura.");
    }

    [UnityTest]
    public IEnumerator El_eco_solo_avanza_cuando_no_se_le_mira_y_el_pulso_lo_detiene()
    {
        yield return LoadLevel(true);
        Echo echo = OnlyHazard<Echo>("Eco 3"); // en la calle mayor, (2, 58)
        yield return DropAt(2f, 70f);

        echo.SeenOverride = true;
        Vector3 start = echo.transform.position;
        yield return new WaitForSeconds(1.5f);
        Assert.Less(Vector3.Distance(start, echo.transform.position), 0.05f, "Mientras se le mira, el Eco no se mueve.");

        echo.SeenOverride = false;
        yield return new WaitForSeconds(1.2f);
        Assert.Greater(Vector3.Distance(start, echo.transform.position), 3f, "Sin mirarlo, el Eco avanza hacia Kael.");

        player.GetComponent<LightPulse>().Trigger();
        yield return null;
        Assert.IsTrue(echo.IsDazzled, "El Pulso de luz debe deslumbrar al Eco.");
        Vector3 dazzledAt = echo.transform.position;
        yield return new WaitForSeconds(1f);
        Assert.Less(Vector3.Distance(dazzledAt, echo.transform.position), 0.05f, "Deslumbrado, el Eco no se mueve.");
    }

    [UnityTest]
    public IEnumerator La_escalera_de_la_posada_sube_a_Kael_a_la_azotea()
    {
        yield return LoadLevel();
        yield return CollectAndWait(3);

        Teleport(new Vector3(17.2f, 0.2f, 125f)); // al pie de la escalera
        yield return new WaitForSeconds(0.4f);
        Press(keyboard.dKey); // la pared de la posada queda al este: D empuja contra ella
        yield return new WaitForSeconds(2.5f);
        Release(keyboard.dKey);

        Assert.Greater(player.transform.position.y, 4.5f, "Caminando contra la escalera, Kael debe subir (la azotea está a 5.2 m).");
    }

    [UnityTest]
    public IEnumerator El_montacargas_arranca_con_el_fragmento_4()
    {
        yield return LoadLevel();
        MovingPlatformActivate lift = FindByName<MovingPlatformActivate>("Montacargas del molino");
        float bottom = lift.transform.position.y;
        Assert.IsFalse(lift.IsMoving);

        yield return CollectAndWait(4, 0f);
        Assert.IsTrue(lift.IsMoving);

        // Sube y vuelve a bajar: se guarda la altura máxima durante unos segundos.
        float highest = bottom;
        float watch = 6f;
        while (watch > 0f)
        {
            watch -= Time.deltaTime;
            highest = Mathf.Max(highest, lift.transform.position.y);
            yield return null;
        }
        Assert.Greater(highest, bottom + 3f, "El montacargas debe subir hacia el molino.");
    }

    [UnityTest]
    public IEnumerator La_caja_sobre_la_placa_abre_la_reja_de_la_torre()
    {
        yield return LoadLevel();
        PressurePlate plate = FindByName<PressurePlate>("Placa de la nave");
        Transform grate = GameObject.Find("Reja de la torre").transform;
        float closedY = grate.position.y;

        // Kael solo no pesa lo suficiente.
        Teleport(plate.transform.position + Vector3.up * 0.2f);
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(plate.IsPressed, "La placa necesita la caja, no a Kael.");

        Teleport(plate.transform.position + new Vector3(-3f, 0.2f, 0f));
        Rigidbody crate = GameObject.Find("Caja de la nave").GetComponent<Rigidbody>();
        crate.position = plate.transform.position + Vector3.up * 0.8f;
        yield return new WaitForSeconds(4f);

        Assert.IsTrue(plate.IsPressed, "La caja debe pulsar la placa.");
        Assert.Less(grate.position.y, closedY - 2.5f, "La reja de la torre debe haberse hundido.");
    }
}
