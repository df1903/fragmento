using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 8, "Ciudad Dormida" (Nivel08_Ciudad.unity): una avenida de una ciudad en ruinas, de noche,
///   con edificios de hormigón y ladrillo, coches y un autobús abandonados, farolas, reflectores y drones vigía.
///   Kael es humano con ropa de ciudad y un halo tenue: lo último de luz que le queda.
///
/// POR QUÉ EXISTE
///   Octavo paso del arco: el recuerdo ya es una ciudad moderna. Su identidad de juego es VERTICAL Y A CUBIERTO:
///   se sube por una escalera de incendios, se cruza entre azoteas por TIROLINAS, se pisa un forjado que se hunde,
///   se sube en un montacargas de obra y se cruza por el brazo de una grúa. Los REFLECTORES matan si ven a Kael
///   un segundo: hay que esconderse tras coches y muros, o cegarlos con el Pulso de luz (F).
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 8 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: avenida > escalera de incendios > azotea A > tirolina > azotea B (forjado roto) > montacargas >
///   azotea C > tirolina > azotea D > grúa > balcón de la torre > escalera > helipuerto (núcleo y portal).
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel08ScenePath = "Assets/Scenes/Nivel08_Ciudad.unity";

    private static RealMaterials cityReal;
    private static CityMaterials cityMaterials;

    [MenuItem("Fragmento/Construir Nivel 8")]
    public static void BuildNivel8()
    {
        BuildThemedLevel(CreateNivel8Theme);
    }

    private static LevelTheme CreateNivel8Theme(Materials materials)
    {
        cityReal = CreateRealMaterials(new Color(0.6f, 0.7f, 0.55f), new Color(0.6f, 0.75f, 0.55f));
        cityMaterials = CreateCityMaterials();
        materials.Ruin = cityMaterials.Roof;
        materials.Stone = cityMaterials.Roof;
        materials.StoneTop = cityMaterials.Roof;
        materials.PlatformOff = cityMaterials.Steel;
        materials.PlatformOn = cityMaterials.CraneYellow;

        return new LevelTheme
        {
            ScenePath = Nivel08ScenePath,
            VolumePath = SettingsFolder + "/Nivel08_PerfilVolumen.asset",
            NextScenePath = Nivel09ScenePath,
            FilePrefix = "N8_",
            KaelPrefabName = "Kael_Nivel8",
            KaelStage = 8,
            FragmentFiles = new[] { "Avenida", "Cable", "Forjado", "Antena", "Grua", "Balcon", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento de la Avenida", "Fragmento del Cable", "Fragmento del Forjado", "Fragmento de la Antena",
                "Fragmento de la Grúa", "Fragmento del Balcón", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Una escalera de incendios baja por la fachada. Camina contra ella para subir.",
                "Se tensa un cable entre las azoteas. Acércate al poste para colgarte.",
                "El montacargas de la obra se pone en marcha.",
                "Otro cable cruza la avenida hacia la azotea del este.",
                "La grúa gira su brazo hacia la torre.",
                "Una escalera aparece en la fachada de la torre.",
                "El Núcleo de la Realidad vuelve a brillar sobre la ciudad.",
            },
            EventIds = new[] { "escalera_1", "tirolina_2", "montacargas_3", "tirolina_4", "grua_5", "escalera_6", "nucleo_7" },
            Growth = cityMaterials.Steel,
            Accent = cityMaterials.Neon,
            Background = new Color(0.05f, 0.07f, 0.12f),
            FogColor = new Color(0.06f, 0.08f, 0.13f),
            FogDensity = 0.011f,
            SunColor = new Color(0.55f, 0.65f, 0.9f),
            SunIntensity = 0.4f,
            SunEuler = new Vector3(32f, 155f, 0f),
            AmbientSky = new Color(0.14f, 0.17f, 0.28f),
            AmbientEquator = new Color(0.1f, 0.11f, 0.16f),
            AmbientGround = new Color(0.05f, 0.05f, 0.06f),
            ZoneLightColor = Color.white,
            VolumeFilter = new Color(0.95f, 0.97f, 1f),
            KillY = -30f,
            Stars = true,
            Spawn = new Vector3(0f, 0.05f, -24f),
            Zones = new ZoneInfo[0],
            RespawnAtCheckpoint = true,
            CreateKael = m => CreateHumanKaelPrefab(m, cityReal, "Kael_Nivel8", 8),
            TuneVolume = profile => TuneRealVolume(profile, -18f, -12f, 0.15f, 65f),
            BuildLayout = BuildCiudadLayout,
        };
    }

    private static void BuildCiudadLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        RealMaterials real = cityReal;
        CityMaterials city = cityMaterials;
        System.Random rng = new System.Random(8);

        ConfigureRealSky("CieloNoche", new Color(0.18f, 0.22f, 0.38f), new Color(0.04f, 0.04f, 0.05f), 0.35f, 0.16f, 0.025f);
        new GameObject("Pistas del nivel").AddComponent<LevelHint>().Setup(
            "Los reflectores te matan si te ven un segundo: escóndete tras coches y muros.",
            "Pulsa F cerca de un reflector o un dron para cegarlo. Enciende farolas para reaparecer en ellas.",
            "Para tomar una tirolina, acércate a su poste.");

        // ================= CALLE =================
        CreateRealBlock("Asfalto", level, new Vector3(0f, -0.5f, 70f), new Vector3(260f, 1f, 340f), city.Asphalt);
        CreateRealBlock("Acera este", level, new Vector3(9f, 0.1f, 45f), new Vector3(4f, 0.2f, 175f), city.Sidewalk);
        CreateRealBlock("Acera oeste", level, new Vector3(-9f, 0.1f, 45f), new Vector3(4f, 0.2f, 175f), city.Sidewalk);
        for (float z = -36f; z < 128f; z += 6f)
        {
            NoShadow(CreateRealBlock("Linea central", level, new Vector3(0f, 0.01f, z), new Vector3(0.18f, 0.02f, 3f), city.Line, false));
        }
        for (int i = 0; i < 7; i++)
        {
            NoShadow(CreateRealBlock("Paso de cebra", level, new Vector3(-6f + i * 2f, 0.01f, -4f), new Vector3(1f, 0.02f, 4f), city.Line, false));
        }

        // Coches, autobús y barreras: la cobertura frente a los reflectores de la avenida.
        CreateBus(level, "Autobus", new Vector3(2.5f, 0f, 31f), 2f, city);
        float[,] cars = { { -4f, 5f, 10f }, { 4f, 14f, -8f }, { -4.5f, 19f, 170f }, { -3f, 52f, 170f }, { 3.5f, 66f, 3f }, { -4f, 80f, 25f }, { 4.5f, 95f, 180f }, { -3.5f, 112f, -15f } };
        for (int i = 0; i < cars.GetLength(0); i++)
        {
            CreateCar(level, "Coche " + (i + 1), new Vector3(cars[i, 0], 0f, cars[i, 1]), cars[i, 2], city.CarPaints[i % city.CarPaints.Length], city);
        }
        CreateRealBlock("Barrera 1", level, new Vector3(-4.5f, 0.5f, 26f), new Vector3(2.4f, 1f, 0.6f), city.Roof);
        CreateRealBlock("Barrera 2", level, new Vector3(-1f, 0.5f, -10f), new Vector3(2.4f, 1f, 0.6f), city.Roof);

        // Farolas de sodio rotas (solo ambiente; las que sirven de refugio son las "Farola" con Beacon).
        float[,] sodium = { { 7.6f, -12f }, { -7.6f, 18f }, { 7.6f, 72f }, { -7.6f, 102f } };
        for (int i = 0; i < sodium.GetLength(0); i++)
        {
            Vector3 foot = new Vector3(sodium[i, 0], 0.2f, sodium[i, 1]);
            CreateRealBlock("Poste de sodio", level, foot + Vector3.up * 3f, new Vector3(0.18f, 6f, 0.18f), city.Steel, false);
            CreateZoneLight(level, foot + new Vector3(-Mathf.Sign(foot.x) * 1.2f, 5.8f, 0f), new Color(1f, 0.6f, 0.25f), 16f, 5f);
            NoShadow(CreateVisualPart("Lampara de sodio", level, PrimitiveType.Cube, foot + new Vector3(-Mathf.Sign(foot.x) * 1.2f, 5.9f, 0f), new Vector3(0.6f, 0.15f, 0.3f), Vector3.zero, real.Flame));
        }

        // ================= EDIFICIOS DEL RECORRIDO =================
        // A (este): 12 m. B (oeste): 8 m, con el forjado central hundido. C (oeste): 18 m. D (este): 14 m. E (torre): 26 m.
        CreateCityBuilding(level, "Edificio A", Rect.MinMaxRect(11f, 40f, 27f, 60f), 12f, city.FacadeWarm, city, 1 | 2 | 4, rng);
        CreateCityBuilding(level, "Edificio B sur", Rect.MinMaxRect(-27f, 64f, -11f, 74f), 8f, city.Brick, city, 1 | 4 | 8, rng);
        CreateCityBuilding(level, "Edificio B hundido", Rect.MinMaxRect(-27f, 74f, -11f, 80f), 2f, city.Brick, city, 0, rng);
        CreateCityBuilding(level, "Edificio B norte", Rect.MinMaxRect(-27f, 80f, -11f, 84f), 8f, city.Brick, city, 4 | 8, rng);
        CreateCityBuilding(level, "Edificio C", Rect.MinMaxRect(-24f, 92f, -6f, 112f), 18f, city.Facade, city, 2 | 8, rng);
        CreateCityBuilding(level, "Edificio D", Rect.MinMaxRect(12f, 100f, 30f, 122f), 14f, city.FacadeWarm, city, 1 | 4, rng);
        CreateCityBuilding(level, "Torre E", Rect.MinMaxRect(-10f, 134f, 10f, 156f), 26f, city.Facade, city, 1 | 2 | 8, rng);

        // Forjado hundido de B: escombros dentro y dos tablones que se hunden al pisarlos. Caer dentro mata.
        for (int i = 0; i < 6; i++)
        {
            CreateRealBlock("Escombro", level, new Vector3(-25f + i * 3f, 2.4f, 75f + (i % 3) * 2f), new Vector3(2f, 0.8f, 1.6f), city.Rubble, false).transform.rotation = Quaternion.Euler(0f, i * 37f, 12f);
        }
        CreateCrumblingPlatform(level, "Tablon del forjado 1", new Vector3(-19f, 7.85f, 75.7f), new Vector3(3f, 0.3f, 2.4f), real.Planks);
        CreateCrumblingPlatform(level, "Tablon del forjado 2", new Vector3(-19f, 7.85f, 78.6f), new Vector3(3f, 0.3f, 2.4f), real.Planks);
        GameObject pit = new GameObject("Hueco del forjado");
        pit.transform.SetParent(level, false);
        pit.transform.position = new Vector3(-19f, 3.5f, 77f);
        BoxCollider pitBox = pit.AddComponent<BoxCollider>();
        pitBox.size = new Vector3(16f, 3f, 6f);
        pitBox.isTrigger = true;
        pit.AddComponent<KillZone>();

        // Balcón de la torre E (cara este, 14 m) con baranda al este y al norte.
        CreateRealBlock("Balcon", level, new Vector3(14f, 13.75f, 138f), new Vector3(8f, 0.5f, 8f), city.Roof);
        CreateRealBlock("Baranda este", level, new Vector3(17.9f, 14.5f, 138f), new Vector3(0.12f, 1f, 8f), city.Steel);
        CreateRealBlock("Baranda norte", level, new Vector3(14f, 14.5f, 141.9f), new Vector3(8f, 1f, 0.12f), city.Steel);
        for (int i = 0; i < 2; i++)
        {
            CreateRealBlock("Mensula", level, new Vector3(14f, 12.5f, 135f + i * 6f), new Vector3(7.6f, 2f, 0.4f), city.Steel, false);
        }

        // Helipuerto: círculo y "H" pintados, y luces en el borde.
        NoShadow(CreateVisualPart("Circulo del helipuerto", level, PrimitiveType.Cylinder, new Vector3(0f, 26.07f, 145f), new Vector3(15f, 0.005f, 15f), Vector3.zero, city.Line));
        NoShadow(CreateVisualPart("Fondo del helipuerto", level, PrimitiveType.Cylinder, new Vector3(0f, 26.075f, 145f), new Vector3(13.6f, 0.005f, 13.6f), Vector3.zero, city.Asphalt));
        Material padLight = GetOrCreateMaterial("CiuLuzHelipuerto", new Color(0.3f, 0.5f, 1f), new Color(0.3f, 0.5f, 1f), 3f, 0.5f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            NoShadow(CreateVisualPart("Luz del helipuerto", level, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 8f, 26.15f, 145f + Mathf.Sin(angle) * 8f), Vector3.one * 0.25f, Vector3.zero, padLight));
        }

        // ================= FRAGMENTOS =================
        context.Fragment(1, new Vector3(-1.5f, 1.3f, 31f));     // a la sombra del autobús
        context.Fragment(2, new Vector3(21f, 13.3f, 55f));      // azotea A
        context.Fragment(3, new Vector3(-23f, 9.3f, 82.5f));    // azotea B, tras el forjado
        context.Fragment(4, new Vector3(-12f, 19.3f, 108f));    // azotea C
        context.Fragment(5, new Vector3(26f, 15.3f, 116f));     // azotea D
        context.Fragment(6, new Vector3(12.5f, 15.3f, 136f));   // balcón de la torre
        context.Fragment(7, new Vector3(0f, 27.3f, 140f));      // helipuerto

        // ================= EVENTOS =================
        // 1. Escalera de incendios en la fachada oeste del edificio A.
        GameObject fireLadder = CreateClimb(level, "Escalera de incendios", new Vector3(11f, 0.2f, 44f), 11.8f, Vector3.left, true, real);
        GameObject fireLadderEvent = CreateActivator("Escalera de incendios que baja", "escalera_1", fireLadder);

        // 2. Tirolina de A (12 m) a B (8 m), sobre la avenida.
        GameObject zipAB = CreateZipLine("Tirolina A-B", new Vector3(13f, 15.2f, 57f), new Vector3(-17f, 11.6f, 70f), city);
        GameObject zipABEvent = CreateActivator("Cable A-B", "tirolina_2", zipAB);

        // 3. Montacargas de obra: de la azotea B (8 m) a la C (18 m), con andamio alrededor.
        GameObject lift = CreateRaft("Montacargas de obra", "montacargas_3", new Vector3(-15f, 7.7f, 87f), new Vector3(0f, 10f, 0f), 4f, false, materials);
        for (int i = 0; i < 4; i++)
        {
            Vector3 pole = new Vector3(i % 2 == 0 ? -17.4f : -12.6f, 9.5f, i < 2 ? 84.6f : 89.4f);
            CreateRealBlock("Andamio", level, pole, new Vector3(0.15f, 19f, 0.15f), city.CraneYellow, false);
        }

        // 4. Tirolina de C (18 m) a D (14 m).
        GameObject zipCD = CreateZipLine("Tirolina C-D", new Vector3(-8f, 21.2f, 104f), new Vector3(20f, 17.2f, 110f), city);
        GameObject zipCDEvent = CreateActivator("Cable C-D", "tirolina_4", zipCD);

        // 5. Grúa: el brazo gira 90° y queda de puente entre la azotea D y el balcón de la torre.
        CreateRealBlock("Mastil de la grua", level, new Vector3(16f, 7f, 129f), new Vector3(1.2f, 14f, 1.2f), city.CraneYellow);
        GameObject jibRoot = new GameObject("Grua");
        jibRoot.transform.position = new Vector3(16f, 14.1f, 129f);
        jibRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        CreateRealBlock("Brazo", jibRoot.transform, jibRoot.transform.position, new Vector3(1.8f, 0.4f, 18f), city.CraneYellow).transform.localRotation = Quaternion.identity;
        GameObject cab = CreateRealBlock("Cabina de la grua", jibRoot.transform, jibRoot.transform.position, new Vector3(1.6f, 1.6f, 1.6f), city.Steel, false);
        cab.transform.localPosition = new Vector3(1.8f, 0.3f, 0f);
        GameObject counterweight = CreateRealBlock("Contrapeso", jibRoot.transform, jibRoot.transform.position, new Vector3(1.6f, 1.2f, 2f), city.Roof, false);
        counterweight.transform.localPosition = new Vector3(0f, -0.8f, -8f);
        GameObject jibEvent = CreateMoveEvent("Grua que gira", "grua_5", jibRoot.transform, Vector3.zero, new Vector3(0f, -90f, 0f), 4f);

        // 6. Escalera de la torre: del balcón (14 m) al helipuerto (26 m).
        GameObject towerLadder = CreateClimb(level, "Escalera de la torre", new Vector3(10f, 14f, 140f), 12f, Vector3.right, true, real);
        GameObject towerLadderEvent = CreateActivator("Escalera de la torre que aparece", "escalera_6", towerLadder);

        // 7. Núcleo y portal en el helipuerto.
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(0f, 26f, 145f)));

        ConfigureEvent(fireLadderEvent, fireLadder.transform, real.Flame);
        ConfigureEvent(zipABEvent, zipAB.transform, real.Flame);
        ConfigureEvent(lift, lift.transform, real.Flame);
        ConfigureEvent(zipCDEvent, zipCD.transform, real.Flame);
        ConfigureEvent(jibEvent, jibRoot.transform, real.Flame);
        ConfigureEvent(towerLadderEvent, towerLadder.transform, real.Flame);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // ================= REFLECTORES =================
        CreateSearchlightTower(level, "Reflector de la avenida", new Vector3(12.5f, 0.2f, 12f), 9f, new Vector3(-2f, 0f, 22f), 35f, 0.1f, city);
        CreateSearchlightTower(level, "Reflector de la escalera", new Vector3(-12.5f, 0.2f, 50f), 10f, new Vector3(8f, 0f, 45f), 25f, 0.08f, city);
        CreateSearchlightTower(level, "Reflector de la azotea D", new Vector3(29f, 14f, 101.5f), 6f, new Vector3(18f, 14f, 116f), 35f, 0.12f, city);
        CreateSearchDrone(level, "Dron vigia A", new[] { new Vector3(14f, 21f, 43f), new Vector3(24f, 21f, 43f), new Vector3(24f, 21f, 57f), new Vector3(14f, 21f, 57f) }, city);
        CreateSearchDrone(level, "Dron vigia C", new[] { new Vector3(-21f, 27f, 95f), new Vector3(-9f, 27f, 95f), new Vector3(-9f, 27f, 109f), new Vector3(-21f, 27f, 109f) }, city);

        // Cobertura en las azoteas: aparatos de aire acondicionado y depósitos de agua.
        float[,] roofBoxes = { { 22f, 12f, 47f }, { 24f, 12f, 52f }, { -20f, 18f, 104f }, { -16f, 18f, 110f }, { 24f, 14f, 106f }, { 18f, 14f, 104f }, { -24f, 8f, 68f } };
        for (int i = 0; i < roofBoxes.GetLength(0); i++)
        {
            Vector3 foot = new Vector3(roofBoxes[i, 0], roofBoxes[i, 1], roofBoxes[i, 2]);
            CreateRealBlock("Aire acondicionado", level, foot + Vector3.up * 0.75f, new Vector3(2.2f, 1.5f, 2.2f), city.Steel);
            NoShadow(CreateVisualPart("Rejilla", level, PrimitiveType.Cylinder, foot + Vector3.up * 1.52f, new Vector3(1.4f, 0.02f, 1.4f), Vector3.zero, city.Tire));
        }
        float[,] tanks = { { 24f, 12f, 58f }, { -9f, 18f, 94f }, { 28f, 14f, 120f } };
        for (int i = 0; i < tanks.GetLength(0); i++)
        {
            Vector3 foot = new Vector3(tanks[i, 0], tanks[i, 1], tanks[i, 2]);
            for (int leg = 0; leg < 4; leg++)
            {
                CreateRealBlock("Pata del deposito", level, foot + new Vector3(leg % 2 == 0 ? -1f : 1f, 1f, leg < 2 ? -1f : 1f), new Vector3(0.15f, 2f, 0.15f), city.Steel, false);
            }
            GameObject tank = CreateVisualPart("Deposito de agua", level, PrimitiveType.Cylinder, foot + Vector3.up * 3.3f, new Vector3(2.8f, 1.3f, 2.8f), Vector3.zero, real.DarkPlanks);
            tank.AddComponent<CapsuleCollider>();
        }

        // ================= FAROLAS (refugio y reaparición) =================
        CreateBeacon(level, "Farola de la salida", new Vector3(5f, 0f, -20f), 6f, true, "", true, real, materials);
        CreateBeacon(level, "Farola de la avenida", new Vector3(-6f, 0f, 34f), 6f, false, "", true, real, materials);
        CreateBeacon(level, "Farola de la azotea A", new Vector3(24f, 12f, 43f), 5f, false, "", true, real, materials);
        CreateBeacon(level, "Farola de la azotea B", new Vector3(-25f, 8f, 81.5f), 4f, false, "", true, real, materials);
        CreateBeacon(level, "Farola de la azotea C", new Vector3(-22f, 18f, 95f), 5f, false, "", true, real, materials);
        CreateBeacon(level, "Farola de la azotea D", new Vector3(28f, 14f, 103f), 5f, false, "", true, real, materials);
        CreateBeacon(level, "Farola del balcon", new Vector3(17f, 14f, 135f), 4f, false, "", true, real, materials);

        // ================= CAMINO (para las pruebas) =================
        CreateFlatPathMarkers(level, new[]
        {
            new[] { new Vector3(0f, 0f, -24f), new Vector3(-2f, 0f, 20f), new Vector3(-2f, 0f, 40f), new Vector3(6f, 0f, 44f), new Vector3(10.1f, 0.2f, 44f) },
            new[] { new Vector3(12.5f, 12f, 44f), new Vector3(19f, 12f, 50f), new Vector3(13f, 12f, 57f) },
            new[] { new Vector3(-17f, 8f, 70f), new Vector3(-19f, 8f, 73.6f) },
            new[] { new Vector3(-19f, 8f, 80.3f), new Vector3(-15f, 8f, 83.6f) },
            new[] { new Vector3(-15f, 18f, 92.5f), new Vector3(-12f, 18f, 100f), new Vector3(-8f, 18f, 104f) },
            new[] { new Vector3(20f, 14f, 110f), new Vector3(16f, 14f, 120f) },
            new[] { new Vector3(16f, 14f, 134.5f), new Vector3(12f, 14f, 140f), new Vector3(10.8f, 14f, 140f) },
            new[] { new Vector3(8.5f, 26f, 140f), new Vector3(5f, 26f, 147f) },
        });

        // ================= RESTO DE LA CIUDAD =================
        Material[] facades = { city.Facade, city.FacadeWarm, city.Brick };
        Rect[] lots =
        {
            Rect.MinMaxRect(12f, -40f, 32f, -22f), Rect.MinMaxRect(15f, -20f, 32f, 6f), Rect.MinMaxRect(12f, 16f, 32f, 37f), Rect.MinMaxRect(12f, 62f, 32f, 97f),
            Rect.MinMaxRect(-32f, -40f, -12f, -22f), Rect.MinMaxRect(-32f, -20f, -12f, 4f), Rect.MinMaxRect(-32f, 6f, -12f, 28f), Rect.MinMaxRect(-32f, 30f, -12f, 44f),
            Rect.MinMaxRect(-34f, 46f, -15f, 62f), Rect.MinMaxRect(-34f, 114f, -12f, 132f), Rect.MinMaxRect(27f, 124f, 38f, 158f), Rect.MinMaxRect(-34f, 136f, -13f, 160f),
        };
        for (int i = 0; i < lots.Length; i++)
        {
            float height = 10f + (float)rng.NextDouble() * 20f;
            GameObject building = CreateCityBuilding(level, "Edificio " + (i + 1), lots[i], height, facades[i % facades.Length], city, 15, rng);
            if (i % 3 == 1)
            {
                // Ruina: un trozo de la última planta caído y escombros en la azotea.
                Vector3 top = new Vector3(lots[i].center.x, height, lots[i].center.y);
                CreateRealBlock("Planta rota", building.transform, top + new Vector3(lots[i].width * 0.2f, 1.6f, 0f), new Vector3(lots[i].width * 0.4f, 3.2f, lots[i].height * 0.5f), facades[i % facades.Length], false)
                    .transform.rotation = Quaternion.Euler(0f, 0f, 9f);
            }

            if (i % 2 == 0)
            {
                // Hiedra que baja por la fachada que da a la avenida.
                float x = lots[i].center.x > 0f ? lots[i].xMin - 0.05f : lots[i].xMax + 0.05f;
                for (int k = 0; k < 3; k++)
                {
                    float z = Mathf.Lerp(lots[i].yMin + 2f, lots[i].yMax - 2f, (k + 0.5f) / 3f);
                    float length = height * (0.4f + (float)rng.NextDouble() * 0.5f);
                    NoShadow(CreateRealBlock("Hiedra", building.transform, new Vector3(x, height - length / 2f, z), new Vector3(0.06f, length, 1.8f + (float)rng.NextDouble()), real.Leaves, false));
                }
            }
        }

        // Letrero de neón verde en la fachada del edificio A (se ve desde la salida).
        NoShadow(CreateRealBlock("Letrero", level, new Vector3(18f, 8f, 39.9f), new Vector3(6f, 1.4f, 0.1f), city.Neon, false));
        CreateZoneLight(level, new Vector3(18f, 8f, 38f), new Color(0.2f, 1f, 0.45f), 10f, 3f);

        // Rascacielos lejanos (sin colisión), recortados contra la niebla.
        for (int i = 0; i < 22; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float height = 35f + (float)rng.NextDouble() * 60f;
            Vector3 foot = i < 16 ? new Vector3(side * (48f + (float)rng.NextDouble() * 40f), 0f, Mathf.Lerp(-30f, 190f, (float)rng.NextDouble()))
                                  : new Vector3(Mathf.Lerp(-60f, 60f, (float)rng.NextDouble()), 0f, 195f + (float)rng.NextDouble() * 40f);
            GameObject skyscraper = CreateRealBlock("Rascacielos", level, foot + Vector3.up * height / 2f, new Vector3(14f, height, 14f), facades[i % 3], false);
            NoShadow(skyscraper);
            if (rng.NextDouble() < 0.5)
            {
                NoShadow(CreateVisualPart("Baliza", level, PrimitiveType.Sphere, foot + Vector3.up * (height + 1f), Vector3.one * 0.6f, Vector3.zero, GetOrCreateMaterial("CiuPilotoRojo", new Color(1f, 0.1f, 0.05f), new Color(1f, 0.1f, 0.05f), 4f, 0.4f)));
            }
        }

        CreateMotes("Bruma de la calle", level, new Vector3(0f, 1.5f, 60f), new Vector3(30f, 3f, 200f), materials.Star, new Color(0.8f, 0.9f, 1.1f, 0.1f), 120, 10f, 6f, 0f);
        CreateMotes("Ceniza", level, new Vector3(0f, 14f, 80f), new Vector3(70f, 26f, 180f), materials.Star, new Color(1.2f, 1.1f, 1f, 0.35f), 260, 25f, 0.08f, -0.02f);
    }
}
