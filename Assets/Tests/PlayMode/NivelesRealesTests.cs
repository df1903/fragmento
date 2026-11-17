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

    /// <summary>Dónde probar la carrera (x, z) si al norte del inicio hay un fragmento (lo recogería y bloquearía el control).</summary>
    protected virtual Vector2? SprintTestStart => null;

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
            "Cadera/Torso/Cabeza/Cara", "Cadera/Torso/Cabeza/Pelo", "Cadera/Torso/Brazo izquierdo/Antebrazo izquierdo",
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
        if (SprintTestStart.HasValue)
        {
            yield return DropAt(SprintTestStart.Value.x, SprintTestStart.Value.y);
        }

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
    protected override string[] KaelParts => new[] { "Cadera/Torso/Corazon de luz", "Cadera/Torso/Capa" };

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
    protected override string[] KaelParts => new[] { "Cadera/Torso/Cabeza/Capucha", "Cadera/Torso/Zurron", "Cadera/Torso/Capa" };

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

/// <summary>Nivel 8, Ciudad Dormida: tirolinas, reflectores, montacargas y grúa.</summary>
public class Nivel08Tests : NivelesRealesTestsBase
{
    protected override string LevelScene => "Nivel08_Ciudad";
    protected override string[] KaelParts => new[] { "Halo", "Cadera/Torso/Capa" };

    [UnityTest]
    public IEnumerator La_tirolina_lleva_a_Kael_de_la_azotea_A_a_la_B()
    {
        yield return LoadLevel();
        yield return CollectAndWait(2, 3f);
        ZipLine zip = FindByName<ZipLine>("Tirolina A-B");

        Teleport(new Vector3(13.5f, 12.1f, 56.5f)); // junto al poste de la azotea A
        bool rode = false;
        float timeout = 8f;
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;
            rode |= zip.IsRiding;
            if (rode && !zip.IsRiding)
            {
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(1f);

        Assert.IsTrue(rode, "Kael debe colgarse de la tirolina.");
        Assert.Less(player.transform.position.x, -11f, "Kael debe acabar en la azotea B (al oeste de la avenida).");
        Assert.AreEqual(8f, player.transform.position.y, 0.6f, "Kael debe quedar de pie sobre la azotea B.");
    }

    [UnityTest]
    public IEnumerator El_reflector_mata_si_ve_a_Kael_y_no_si_esta_a_cubierto()
    {
        yield return LoadLevel(true);
        Searchlight searchlight = OnlyHazard<Searchlight>("Reflector de la avenida");
        searchlight.HoldStill = true;
        yield return null;

        Transform head = searchlight.transform.Find("Cabeza");
        Assert.IsTrue(Physics.Raycast(head.position + head.forward, head.forward, out RaycastHit spot, 60f, ~0, QueryTriggerInteraction.Ignore));
        Vector3 lit = spot.point;

        // A cubierto: un muro entre el foco y Kael.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = Vector3.Lerp(head.position, lit + Vector3.up, 0.6f);
        wall.transform.rotation = Quaternion.LookRotation(lit - head.position);
        wall.transform.localScale = new Vector3(6f, 6f, 0.5f);
        Teleport(lit + Vector3.up * 0.1f);
        int deaths = 0;
        yield return CountDeaths(2.5f, value => deaths = value);
        Assert.AreEqual(0, deaths, "A cubierto, el reflector no ve a Kael.");

        Object.Destroy(wall);
        yield return null;
        Teleport(lit + Vector3.up * 0.1f);
        yield return CountDeaths(2.5f, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Al descubierto, el reflector mata a Kael tras el aviso.");
    }

    [UnityTest]
    public IEnumerator El_pulso_ciega_al_reflector()
    {
        yield return LoadLevel(true);
        Searchlight searchlight = OnlyHazard<Searchlight>("Reflector de la avenida");
        searchlight.HoldStill = true;
        yield return null;

        Transform head = searchlight.transform.Find("Cabeza");
        Physics.Raycast(head.position + head.forward, head.forward, out RaycastHit spot, 60f, ~0, QueryTriggerInteraction.Ignore);
        Teleport(spot.point + Vector3.up * 0.1f);
        yield return null;
        Assert.IsTrue(player.GetComponent<LightPulse>().Trigger());
        yield return null;
        Assert.IsTrue(searchlight.IsBlind, "El Pulso debe cegar al reflector.");

        int deaths = 0;
        yield return CountDeaths(2.5f, value => deaths = value);
        Assert.AreEqual(0, deaths, "Cegado, el reflector no mata.");
    }

    [UnityTest]
    public IEnumerator El_montacargas_sube_y_la_grua_gira()
    {
        yield return LoadLevel();
        MovingPlatformActivate lift = FindByName<MovingPlatformActivate>("Montacargas de obra");
        float bottom = lift.transform.position.y;
        yield return CollectAndWait(3, 0f);

        float highest = bottom;
        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            highest = Mathf.Max(highest, lift.transform.position.y);
            yield return null;
        }
        Assert.Greater(highest, bottom + 6f, "El montacargas debe subir hacia la azotea C.");

        Vector3 overGap = new Vector3(16f, 20f, 131f);
        Physics.Raycast(overGap, Vector3.down, out RaycastHit before, 30f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, 10f, "Antes del fragmento 5, entre la azotea D y el balcón no hay paso.");
        yield return CollectAndWait(5, 5f);
        Assert.IsTrue(Physics.Raycast(overGap, Vector3.down, out RaycastHit after, 30f, ~0, QueryTriggerInteraction.Ignore));
        Assert.AreEqual(14.3f, after.point.y, 0.4f, "El brazo de la grúa debe quedar de puente.");
    }
}

/// <summary>Nivel 9, El Hogar: tren, tocadiscos, cajones, regla, escalera y péndulo.</summary>
public class Nivel09Tests : NivelesRealesTestsBase
{
    protected override string LevelScene => "Nivel09_Hogar";
    protected override string[] KaelParts => new[] { "Cadera/Torso/Cuello del jersey" };
    protected override Vector2? SprintTestStart => new Vector2(6f, -14f); // al norte del inicio está el fragmento 1

    [UnityTest]
    public IEnumerator Kael_ya_no_tiene_luz_ni_capa()
    {
        yield return LoadLevel();
        Assert.IsNull(player.transform.Find("Visual/Cadera/Torso/Corazon de luz"));
        Assert.IsNull(player.transform.Find("Visual/Cadera/Torso/Capa"));
    }

    [UnityTest]
    public IEnumerator El_tren_atropella_a_Kael_en_la_via()
    {
        yield return LoadLevel(true);
        OnlyHazard<PathHazard>("Tren de juguete");
        Teleport(new Vector3(0f, 0.2f, 1f)); // en la vía, al norte de la alfombra
        int deaths = 0;
        yield return CountDeaths(13f, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Quieto en la vía, el tren debe atropellar a Kael.");
    }

    [UnityTest]
    public IEnumerator La_regla_cae_y_hace_de_puente_a_la_estanteria()
    {
        yield return LoadLevel();
        Vector3 overRuler = new Vector3(-3.5f, 15f, 27.7f);
        Physics.Raycast(overRuler, Vector3.down, out RaycastHit before, 20f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, 1f, "Antes del fragmento 2, entre la mesa y la estantería no hay paso.");

        yield return CollectAndWait(2, 4f);
        Assert.IsTrue(Physics.Raycast(overRuler, Vector3.down, out RaycastHit after, 20f, ~0, QueryTriggerInteraction.Ignore));
        Assert.AreEqual(6.3f, after.point.y, 0.4f, "La regla caída debe quedar tendida entre la mesa y la estantería.");
    }

    [UnityTest]
    public IEnumerator La_escalera_sube_a_Kael_al_techo_de_la_estanteria()
    {
        yield return LoadLevel();
        yield return CollectAndWait(3);

        Teleport(new Vector3(12.6f, 6.6f, 30.8f)); // en la repisa, al pie de la escalera
        yield return new WaitForSeconds(0.4f);
        Press(keyboard.aKey); // la estantería queda al oeste: A empuja contra ella
        yield return new WaitForSeconds(3.8f);
        Release(keyboard.aKey);
        yield return new WaitForSeconds(0.5f);
        Assert.Greater(player.transform.position.y, 16.5f, "Kael debe llegar al techo de la estantería.");
    }

    [UnityTest]
    public IEnumerator El_pendulo_barre_el_techo_de_la_estanteria()
    {
        yield return LoadLevel(true);
        OnlyHazard<SwingingPendulum>("Pendulo del reloj");
        Teleport(new Vector3(5f, 17f, 30f));
        int deaths = 0;
        yield return CountDeaths(3f, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Bajo el péndulo, Kael debe caer derribado.");
    }

    [UnityTest]
    public IEnumerator Los_cajones_salen_como_escalones()
    {
        yield return LoadLevel();
        Physics.Raycast(new Vector3(12.7f, 10f, -15f), Vector3.down, out RaycastHit before, 20f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, 0.5f);

        yield return CollectAndWait(4, 3f);
        float[] expected = { 1.5f, 3f, 4.5f, 6f };
        float[] x = { 12.7f, 14.2f, 15.7f, 17.2f };
        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(Physics.Raycast(new Vector3(x[i], 10f, -15f), Vector3.down, out RaycastHit step, 20f, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual(expected[i], step.point.y, 0.2f, "Escalón " + (i + 1) + " de los cajones.");
        }
    }

    [UnityTest]
    public IEnumerator El_tocadiscos_lleva_a_Kael_dando_vueltas()
    {
        yield return LoadLevel();
        Teleport(new Vector3(27.5f, 8.2f, -14.5f)); // sobre el plato, a 2 m del centro
        yield return new WaitForSeconds(0.5f);
        Vector3 start = player.transform.position;
        yield return new WaitForSeconds(2f);

        Assert.Greater(FlatDistance(start, player.transform.position), 0.8f, "El plato debe llevar a Kael al girar.");
        Assert.AreEqual(8.1f, player.transform.position.y, 0.3f, "Kael sigue de pie sobre el plato.");
    }
}

/// <summary>Nivel 10, El Jardín del Amanecer: cama elástica, aspersor, rastrillo, flores, columpio, Sombra, amanecer y créditos.</summary>
public class Nivel10Tests : NivelesRealesTestsBase
{
    protected override string LevelScene => "Nivel10_Despertar";
    protected override string[] KaelParts => new[] { "Cadera/Torso/Cuello del pijama" };

    [UnityTest]
    public IEnumerator La_cama_elastica_aparece_y_lanza_a_Kael()
    {
        yield return LoadLevel();
        Assert.AreEqual(0, Object.FindObjectsByType<BouncePad>(FindObjectsSortMode.None).Length, "La cama elástica no está hasta el fragmento 1.");
        yield return CollectAndWait(1, 2f);

        BouncePad pad = FindByName<BouncePad>("Cama elastica");
        Teleport(pad.transform.position + Vector3.up * 0.05f);
        float max = 0f;
        for (float t = 0f; t < 2f; t += Time.deltaTime)
        {
            max = Mathf.Max(max, player.transform.position.y);
            yield return null;
        }
        Assert.Greater(max, 5.5f, "La cama elástica debe lanzar a Kael por encima de la mesa de pícnic.");
    }

    [UnityTest]
    public IEnumerator El_aspersor_mata_a_quien_no_salta_el_chorro()
    {
        yield return LoadLevel(true);
        OnlyHazard<SweepingBeam>("Aspersor 1");
        Teleport(new Vector3(2f, 0.05f, -11f)); // a 5 m del aspersor: el chorro (8 m) pasa por aquí
        int deaths = 0;
        yield return CountDeaths(10f, value => deaths = value);
        Assert.GreaterOrEqual(deaths, 1, "Quieto en el césped, el chorro del aspersor debe derribar a Kael.");
    }

    [UnityTest]
    public IEnumerator El_rastrillo_cae_hasta_la_jardinera()
    {
        yield return LoadLevel();
        Vector3 overRake = new Vector3(-24f, 20f, 6.5f);
        Physics.Raycast(overRake, Vector3.down, out RaycastHit before, 30f, ~0, QueryTriggerInteraction.Ignore);
        Assert.Less(before.point.y, 1f, "Antes del fragmento 2, entre la mesa y la jardinera no hay paso.");

        yield return CollectAndWait(2, 4f);
        Assert.IsTrue(Physics.Raycast(overRake, Vector3.down, out RaycastHit after, 30f, ~0, QueryTriggerInteraction.Ignore));
        Assert.AreEqual(7.2f, after.point.y, 0.6f, "El mango del rastrillo debe quedar tendido entre la mesa y la jardinera.");
    }

    [UnityTest]
    public IEnumerator Las_flores_crecen_hasta_la_valla()
    {
        yield return LoadLevel();
        CrystalGrow flowers = FindByName<CrystalGrow>("Flores gigantes");
        Assert.IsFalse(flowers.HasRun);
        yield return CollectAndWait(3, 3f);

        Assert.IsTrue(flowers.HasRun);
        Assert.IsTrue(Physics.Raycast(new Vector3(-31.8f, 20f, 32f), Vector3.down, out RaycastHit head, 30f, ~0, QueryTriggerInteraction.Ignore));
        Assert.AreEqual(13.1f, head.point.y, 0.3f, "La última flor debe quedar a un paso de lo alto de la valla.");
    }

    [UnityTest]
    public IEnumerator El_columpio_oscila_de_la_valla_al_roble()
    {
        yield return LoadLevel();
        SwingPlatform swing = FindByName<SwingPlatform>("Columpio");
        Transform seat = GameObject.Find("Asiento del columpio").transform;
        Assert.IsFalse(swing.IsSwinging);

        yield return CollectAndWait(4, 0f);
        Assert.IsTrue(swing.IsSwinging);
        float minX = seat.position.x, maxX = seat.position.x, maxY = 0f;
        for (float t = 0f; t < 16f; t += Time.deltaTime)
        {
            minX = Mathf.Min(minX, seat.position.x);
            maxX = Mathf.Max(maxX, seat.position.x);
            maxY = Mathf.Max(maxY, seat.position.y);
            yield return null;
        }

        Assert.Less(minX, -30f, "El columpio debe llegar junto a la valla.");
        Assert.Greater(maxX, -8f, "El columpio debe llegar junto a la rama baja del roble.");
        Assert.Greater(maxY, 13f, "En los extremos, el asiento sube a la altura de la valla y de la rama.");
    }

    [UnityTest]
    public IEnumerator La_sombra_sube_mata_y_vuelve_al_suelo_al_reaparecer()
    {
        yield return LoadLevel(true);
        RisingShadow shadow = OnlyHazard<RisingShadow>("Sombra del Olvido");
        float start = shadow.SurfaceY;
        Assert.IsFalse(shadow.IsAwake);

        yield return CollectAndWait(6, 4f);
        Assert.IsTrue(shadow.IsAwake, "El fragmento 6 despierta la sombra.");
        Assert.Greater(shadow.SurfaceY, start + 1.5f, "La sombra debe estar subiendo.");

        int deaths = 0;
        System.Action counter = () => deaths++;
        GameEvents.PlayerDied += counter;
        Teleport(new Vector3(0f, 0.1f, -20f)); // de vuelta al césped, ya cubierto
        for (float t = 0f; t < 1f && deaths == 0; t += Time.deltaTime)
        {
            yield return null;
        }
        GameEvents.PlayerDied -= counter;
        Assert.GreaterOrEqual(deaths, 1, "Bajo la sombra, Kael muere.");

        yield return null;
        Assert.Less(shadow.SurfaceY, start + 0.5f, "Al reaparecer Kael, la sombra vuelve al suelo.");
    }

    [UnityTest]
    public IEnumerator El_final_amanece_y_muestra_los_creditos()
    {
        yield return CollectAllFragments();
        Assert.IsTrue(Object.FindFirstObjectByType<DawnFinale>().HasStarted, "Al restaurar el núcleo debe amanecer.");
        Assert.IsNull(Object.FindFirstObjectByType<EndScreen>(), "El último nivel no tiene pantalla de 'siguiente nivel'.");

        GameEvents.RaiseLevelCompleted();
        yield return null;
        Assert.IsTrue(Object.FindFirstObjectByType<FinalCredits>().IsShown, "Al cruzar el último portal aparecen los créditos.");
    }
}
