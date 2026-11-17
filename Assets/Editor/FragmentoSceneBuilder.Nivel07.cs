using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 7, "Aldea de Brumavieja" (Nivel07_Aldea.unity): una aldea abandonada bajo la tormenta.
///   Campos de trigo, un arroyo con puente levadizo, muralla, calle de adoquines con casas de entramado, plaza
///   con pozo, posada, granero, molino e iglesia con campanario. Llueve y hay relámpagos. Kael es casi humano.
///
/// POR QUÉ EXISTE
///   Séptimo paso del arco de materialización: aparece lo que construyeron las personas. Su identidad de juego
///   es un PUEBLO VERTICAL CON PUZLES: se entra en las casas, se sube a los tejados por una escalera de mano, se
///   cruza por tablones, se empuja una caja hasta una placa. Enemigos: los Ecos, que solo avanzan si no los miras.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 7 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: campos > puente levadizo > calle mayor > plaza > azotea de la posada > granero > montacargas >
///   balcón del molino > (bajar) iglesia > torre > azotea de la iglesia (núcleo y campana).
///   Fragmentos: 1 baja el puente, 2 se encienden los faroles, 3 escalera a los tejados, 4 montacargas,
///   5 se abre la iglesia, 6 escalones de la torre, 7 núcleo. La reja de la torre la abre la caja sobre la placa.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel07ScenePath = "Assets/Scenes/Nivel07_Aldea.unity";

    private static RealMaterials villageReal;

    // Tramos del camino que se caminan sin ayuda: de los campos al puente, y de la puerta a la iglesia (rodeando el pozo).
    private static readonly Vector2[][] VillageStretches =
    {
        new[] { new Vector2(0f, -20f), new Vector2(0f, 10f), new Vector2(0f, 24.4f) },
        new[] { new Vector2(0f, 36f), new Vector2(0f, 112f), new Vector2(6f, 118f), new Vector2(6f, 132f), new Vector2(0f, 138f), new Vector2(0f, 164.4f) },
    };

    private static readonly Vector2[] VillagePath = JoinStretches(VillageStretches, new Vector2(0f, 165f));

    // ALTURA DEL TERRENO: campos ondulados al sur, un arroyo en z = 30 y la aldea llana (y = 0) al norte.
    private static float VillageHeight(float x, float z)
    {
        float hills = (Fbm(x / 30f + 80f, z / 30f + 80f) - 0.5f) * 2.4f + 0.3f;
        float flat = Mathf.Max(Smooth(8f, 3f, DistanceToPath(VillagePath, x, z)), Smooth(33f, 37f, z));
        float h = hills * (1f - flat);

        float creek = Mathf.Abs(z - 30f);
        if (creek < 5f)
        {
            h = Mathf.Lerp(-3.4f, h, Smooth(2f, 5f, creek));
        }

        h += (Smooth(54f, 66f, Mathf.Abs(x)) + Smooth(-24f, -34f, z) + Smooth(203f, 214f, z)) * 14f;
        return h;
    }

    private static Vector3 VillagePoint(float x, float z, float lift = 0f)
    {
        return new Vector3(x, VillageHeight(x, z) + lift, z);
    }

    [MenuItem("Fragmento/Construir Nivel 7")]
    public static void BuildNivel7()
    {
        BuildThemedLevel(CreateNivel7Theme);
    }

    private static LevelTheme CreateNivel7Theme(Materials materials)
    {
        villageReal = CreateRealMaterials(new Color(0.7f, 0.78f, 0.6f), new Color(0.7f, 0.8f, 0.6f));
        materials.Ruin = villageReal.StoneWall;
        materials.Stone = villageReal.Rock;
        materials.StoneTop = villageReal.Planks;
        materials.PlatformOff = villageReal.DarkPlanks;
        materials.PlatformOn = villageReal.Planks;

        return new LevelTheme
        {
            ScenePath = Nivel07ScenePath,
            VolumePath = SettingsFolder + "/Nivel07_PerfilVolumen.asset",
            NextScenePath = Nivel08ScenePath,
            FilePrefix = "N7_",
            KaelPrefabName = "Kael_Nivel7",
            KaelStage = 7,
            FragmentFiles = new[] { "Camino", "Farol", "Tejado", "Molino", "Umbral", "Campana", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento del Camino", "Fragmento del Farol", "Fragmento del Tejado", "Fragmento del Molino",
                "Fragmento del Umbral", "Fragmento de la Campana", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "El puente levadizo de la aldea baja.",
                "Los faroles de la calle se encienden: los Ecos no cruzan su luz.",
                "Una escalera de mano aparece en la posada. Camina contra ella para subir.",
                "El montacargas del molino arranca.",
                "La puerta de la iglesia se abre.",
                "Escalones de madera suben por dentro de la torre.",
                "El Núcleo de la Realidad vuelve a brillar y la campana suena.",
            },
            EventIds = new[] { "puente_1", "faroles_2", "escalera_3", "montacargas_4", "iglesia_5", "campanario_6", "nucleo_7" },
            Growth = villageReal.Planks,
            Accent = villageReal.Flame,
            Background = new Color(0.3f, 0.33f, 0.36f),
            FogColor = new Color(0.3f, 0.33f, 0.36f),
            FogDensity = 0.014f,
            SunColor = new Color(0.62f, 0.68f, 0.78f),
            SunIntensity = 0.95f,
            SunEuler = new Vector3(48f, -25f, 0f),
            AmbientSky = new Color(0.36f, 0.4f, 0.46f),
            AmbientEquator = new Color(0.3f, 0.32f, 0.34f),
            AmbientGround = new Color(0.1f, 0.1f, 0.1f),
            ZoneLightColor = Color.white,
            VolumeFilter = new Color(0.92f, 0.97f, 1f),
            KillY = -40f,
            Spawn = VillagePoint(0f, -20f, 0.05f),
            Zones = new ZoneInfo[0],
            RespawnAtCheckpoint = true,
            CreateKael = m => CreateHumanKaelPrefab(m, villageReal, "Kael_Nivel7", 7),
            TuneVolume = profile => TuneRealVolume(profile, -14f, -20f, -0.15f, 45f),
            OnKaelPlaced = (context, kael) => CreateRain(kael, context.Materials.Star),
            BuildLayout = BuildAldeaLayout,
        };
    }

    private static void BuildAldeaLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        RealMaterials real = villageReal;
        System.Random rng = new System.Random(7);

        // Atmósfera fina y poca exposición: cielo gris plomo (una atmósfera gruesa lo vuelve naranja, de atardecer).
        ConfigureRealSky("CieloTormenta", new Color(0.5f, 0.52f, 0.56f), new Color(0.12f, 0.12f, 0.13f), 0.55f, 0.42f, 0f);

        // Relámpagos: una segunda luz direccional que casi siempre está apagada.
        GameObject lightning = new GameObject("Relampagos");
        lightning.transform.rotation = Quaternion.Euler(60f, 140f, 0f);
        Light lightningLight = lightning.AddComponent<Light>();
        lightningLight.type = LightType.Directional;
        lightningLight.color = new Color(0.8f, 0.88f, 1f);
        lightningLight.shadows = LightShadows.None;
        lightning.AddComponent<LightningFlash>();

        new GameObject("Pistas del nivel").AddComponent<LevelHint>().Setup(
            "Los Ecos solo se mueven cuando no los miras. Gira la cámara con Q y E.",
            "Pulsa F: el Pulso de luz los detiene y enciende los faroles. Shift para correr.");

        // ================= TERRENO, CALLE Y AGUA =================
        Rect area = Rect.MinMaxRect(-68f, -36f, 68f, 216f);
        MakeStatic(CreateMeshObject("Terreno", level, SaveMesh(BuildTerrainMesh(area, 2f, VillageHeight), "TerrenoAldea"), Vector3.zero, true, real.Grass));
        NoShadow(CreateMeshObject("Camino de tierra", level, SaveMesh(BuildRibbonMesh(VillageStretches[0], 3f, VillageHeight, 0.05f), "CaminoAldea"), Vector3.zero, false, real.Dirt));
        NoShadow(CreateMeshObject("Calle mayor", level, SaveMesh(BuildRibbonMesh(new[] { new Vector2(0f, 35.5f), new Vector2(0f, 165f) }, 9f, VillageHeight, 0.05f), "CalleAldea"), Vector3.zero, false, real.Cobble));
        NoShadow(CreateRealBlock("Plaza", level, new Vector3(0f, 0.02f, 125f), new Vector3(34f, 0.12f, 32f), real.Cobble, false));
        CreatePathMarkers(level, VillageStretches, VillageHeight);

        GameObject creek = CreateWaterSheet(level, "Arroyo", new Vector3(0f, -1.2f, 30f), new Vector2(140f, 12f), new Vector2(0.08f, 0f), real);
        creek.AddComponent<TideWater>().Setup(0f); // caer al arroyo ahoga

        CreateInvisibleWall(level, "Borde oeste", new Vector3(-60f, 15f, 90f), new Vector3(2f, 50f, 260f));
        CreateInvisibleWall(level, "Borde este", new Vector3(60f, 15f, 90f), new Vector3(2f, 50f, 260f));
        CreateInvisibleWall(level, "Borde sur", new Vector3(0f, 15f, -30f), new Vector3(130f, 50f, 2f));
        CreateInvisibleWall(level, "Borde norte", new Vector3(0f, 15f, 208f), new Vector3(130f, 50f, 2f));

        // ================= FRAGMENTOS =================
        context.Fragment(1, VillagePoint(10f, 8f, 1.3f));           // campos, junto al carro
        context.Fragment(2, new Vector3(-11f, 1.3f, 50f));          // dentro de la primera casa
        context.Fragment(3, new Vector3(0f, 1.3f, 132f));           // plaza, al norte del pozo
        context.Fragment(4, new Vector3(24f, 6.6f, 146f));          // azotea del granero
        context.Fragment(5, new Vector3(18f, 12.5f, 172f));         // balcón del molino
        context.Fragment(6, new Vector3(-10.5f, 1.3f, 172f));       // base de la torre
        context.Fragment(7, new Vector3(0f, 10.6f, 175f));          // azotea de la iglesia

        // ================= MURALLA Y PUENTE LEVADIZO (evento 1) =================
        CreateRealBlock("Muralla oeste", level, new Vector3(-31f, 2.25f, 37f), new Vector3(56f, 4.5f, 1.2f), real.StoneWall);
        CreateRealBlock("Muralla este", level, new Vector3(31f, 2.25f, 37f), new Vector3(56f, 4.5f, 1.2f), real.StoneWall);
        CreateRealBlock("Arco de la puerta", level, new Vector3(0f, 5f, 37f), new Vector3(8f, 1f, 1.4f), real.StoneWall, false);
        CreateRealBlock("Torreon oeste", level, new Vector3(-4.2f, 3f, 37f), new Vector3(2.4f, 6f, 2.4f), real.StoneWall);
        CreateRealBlock("Torreon este", level, new Vector3(4.2f, 3f, 37f), new Vector3(2.4f, 6f, 2.4f), real.StoneWall);

        // El tablero cuelga de un pivote en la orilla norte. Empieza levantado (78°) y el evento lo tumba.
        GameObject bridge = new GameObject("Puente levadizo");
        bridge.transform.position = new Vector3(0f, 0.08f, 35.2f);
        Transform hinge = CreatePivot("Bisagra", bridge.transform, Vector3.zero);
        GameObject deck = CreateRealBlock("Tablero", hinge, new Vector3(0f, -0.07f, 29.9f), new Vector3(4.6f, 0.3f, 10.6f), real.Planks);
        hinge.localRotation = Quaternion.Euler(78f, 0f, 0f);
        MoveOnEvent bridgeMove = bridge.AddComponent<MoveOnEvent>();
        bridgeMove.Setup(hinge, Vector3.zero, new Vector3(-78f, 0f, 0f), 2.6f);
        SetEventId(bridgeMove, "puente_1");

        // ================= CASAS DE LA CALLE MAYOR =================
        // La primera del oeste tiene puerta a la calle (este) y dentro está el fragmento 2.
        CreateHouse(level, "Casa del farolero", new Vector3(-11f, 0f, 50f), 9f, 10f, 4.4f, 3, false, real, rng);
        float[] westZ = { 65f, 80f, 96f };
        float[] eastZ = { 50f, 66f, 82f, 97f };
        foreach (float z in westZ)
        {
            CreateHouse(level, "Casa oeste", new Vector3(-11.5f - (float)rng.NextDouble(), 0f, z), 9f, 10f + (float)rng.NextDouble() * 2f, 4f + (float)rng.NextDouble() * 1.6f, 0, false, real, rng);
        }
        foreach (float z in eastZ)
        {
            CreateHouse(level, "Casa este", new Vector3(11.5f + (float)rng.NextDouble(), 0f, z), 9f, 10f + (float)rng.NextDouble() * 2f, 4f + (float)rng.NextDouble() * 1.6f, 0, false, real, rng);
        }
        CreateHouse(level, "Casa de la plaza oeste", new Vector3(-24f, 0f, 124f), 10f, 14f, 5f, 0, false, real, rng);
        CreateHouse(level, "Casa de la plaza norte", new Vector3(-22f, 0f, 148f), 12f, 10f, 4.6f, 0, false, real, rng);

        // ================= FAROLES (evento 2 los de la calle; el resto, con el Pulso) =================
        CreateBeacon(level, "Farol del camino", VillagePoint(3.5f, -16f), 6f, true, "", true, real, materials);
        float[,] street = { { -4.2f, 44f }, { 4.2f, 61f }, { -4.2f, 78f }, { 4.2f, 94f }, { -4.2f, 109f } };
        for (int i = 0; i < street.GetLength(0); i++)
        {
            CreateBeacon(level, "Farol de la calle " + (i + 1), new Vector3(street[i, 0], 0f, street[i, 1]), 5f, false, "faroles_2", true, real, materials);
        }
        CreateBeacon(level, "Farol de la plaza", new Vector3(-9f, 0f, 117f), 6f, false, "", true, real, materials);
        CreateBeacon(level, "Farol de la posada", new Vector3(27.5f, 5.2f, 121f), 5f, false, "", true, real, materials);
        CreateBeacon(level, "Farol de la iglesia", new Vector3(-4.5f, 0f, 168f), 5f, false, "", true, real, materials);

        // ================= PLAZA: POZO, POSADA Y ESCALERA (evento 3) =================
        CreateVisualPart("Pozo brocal", level, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 125f), new Vector3(2.6f, 0.5f, 2.6f), Vector3.zero, real.StoneWall)
            .AddComponent<CapsuleCollider>();
        CreateVisualPart("Pozo agua", level, PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 125f), new Vector3(2f, 0.03f, 2f), Vector3.zero, real.Shadow);
        CreateRealBlock("Pozo poste oeste", level, new Vector3(-1.2f, 1.6f, 125f), new Vector3(0.2f, 2.4f, 0.2f), real.DarkPlanks, false);
        CreateRealBlock("Pozo poste este", level, new Vector3(1.2f, 1.6f, 125f), new Vector3(0.2f, 2.4f, 0.2f), real.DarkPlanks, false);
        CreateMeshObject("Pozo tejadillo", level, BuildGableRoofMesh(3f, 2f, 0.9f, 0.2f), new Vector3(0f, 2.8f, 125f), false, real.RoofTiles);

        // Posada: azotea a 5.2 m. La escalera de mano va en su pared oeste (la que da a la plaza).
        CreateHouse(level, "Posada", new Vector3(24f, 0f, 125f), 12f, 14f, 5f, 0, true, real, rng);
        GameObject ladder = CreateClimb(level, "Escalera de la posada", new Vector3(17.85f, 0f, 125f), 5.2f, Vector3.left, true, real);
        GameObject ladderEvent = CreateActivator("Escalera a los tejados", "escalera_3", ladder);

        // Tablón de la posada al granero (7.6 m de hueco) y granero con azotea.
        CreateRealBlock("Tablon posada-granero", level, new Vector3(24f, 5.1f, 136f), new Vector3(1.8f, 0.2f, 8.6f), real.Planks);
        CreateHouse(level, "Granero", new Vector3(24f, 0f, 146f), 12f, 12f, 5f, 0, true, real, rng);

        // ================= MOLINO Y MONTACARGAS (evento 4) =================
        CreateRealBlock("Molino torre", level, new Vector3(24f, 5.5f, 172f), new Vector3(9f, 11f, 9f), real.StoneWall);
        CreateRealBlock("Molino balcon", level, new Vector3(24f, 11f, 172f), new Vector3(13f, 0.3f, 13f), real.Planks);
        CreateRealBlock("Molino pasarela", level, new Vector3(24f, 11f, 161.7f), new Vector3(2.6f, 0.3f, 8f), real.Planks);
        CreateRealBlock("Molino cuerpo alto", level, new Vector3(24f, 13.5f, 172f), new Vector3(7f, 5f, 7f), real.Plaster);
        CreateMeshObject("Molino tejado", level, BuildGableRoofMesh(7f, 7f, 3.2f, 0.6f), new Vector3(24f, 16f, 172f), false, real.RoofTiles);
        Transform sails = CreatePivot("Aspas", level, new Vector3(24f, 13.5f, 168.3f));
        for (int i = 0; i < 4; i++)
        {
            Transform arm = CreatePivot("Aspa " + (i + 1), sails, Vector3.zero);
            arm.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            CreateRealBlock("Pala", arm, arm.position, new Vector3(1.4f, 6.5f, 0.12f), real.Planks, false).transform.localPosition = new Vector3(0f, 3.6f, 0f);
        }
        sails.gameObject.AddComponent<Spinner>();

        // Sube 6 m: del nivel de la azotea del granero (5.2) a la pasarela del molino (11.15).
        GameObject lift = CreateRaft("Montacargas del molino", "montacargas_4", new Vector3(24f, 4.9f, 155f), new Vector3(0f, 5.95f, 0f), 4f, false, materials);

        // ================= IGLESIA, CAJA Y PLACA, TORRE (eventos 5 y 6) =================
        // Nave: x -7..7, z 165..191, muros de 9 m y azotea. Puerta al sur; hueco a la torre en el muro oeste (z 171..173).
        Transform church = new GameObject("Iglesia").transform;
        church.SetParent(level, false);
        CreateRealBlock("Zocalo", church, new Vector3(0f, 0.25f, 178f), new Vector3(14.3f, 0.5f, 26.3f), real.StoneWall, false);
        CreateRealBlock("Suelo de la nave", church, new Vector3(0f, 0.04f, 178f), new Vector3(13.6f, 0.08f, 25.6f), real.Cobble, false);
        CreateHouseWall(church, "Muro sur", new Vector3(0f, 0f, 165.15f), 14f, 9f, false, true, real);
        CreateHouseWall(church, "Muro norte", new Vector3(0f, 0f, 190.85f), 14f, 9f, false, false, real);
        CreateHouseWall(church, "Muro este", new Vector3(6.85f, 0f, 178f), 26f, 9f, true, false, real);
        CreateRealBlock("Muro oeste sur", church, new Vector3(-6.85f, 4.5f, 168f), new Vector3(0.3f, 9f, 6f), real.Plaster);
        CreateRealBlock("Muro oeste norte", church, new Vector3(-6.85f, 4.5f, 182f), new Vector3(0.3f, 9f, 18f), real.Plaster);
        CreateRealBlock("Muro oeste dintel", church, new Vector3(-6.85f, 6f, 172f), new Vector3(0.3f, 6f, 2f), real.Plaster);
        CreateRealBlock("Azotea de la iglesia", church, new Vector3(0f, 9.1f, 178f), new Vector3(14.4f, 0.2f, 26.4f), real.StoneWall);
        for (int i = 0; i < 5; i++)
        {
            // Bancos de la nave: obstáculos bajos entre la caja y la placa.
            CreateRealBlock("Banco " + (i + 1), church, new Vector3(-3.4f, 0.45f, 171f + i * 3f), new Vector3(4f, 0.9f, 0.7f), real.DarkPlanks);
        }
        CreateRealBlock("Altar", church, new Vector3(0f, 0.6f, 188.5f), new Vector3(3f, 1.2f, 1.2f), real.StoneWall);

        // 5. Puerta de la iglesia: se hunde.
        GameObject churchDoor = CreateRealBlock("Puerta de la iglesia", level, new Vector3(0f, 1.2f, 165.15f), new Vector3(1.6f, 2.4f, 0.35f), real.DarkPlanks);
        GameObject churchEvent = CreateMoveEvent("Iglesia que se abre", "iglesia_5", churchDoor.transform, Vector3.down * 2.6f, Vector3.zero, 2f);

        // Puzle: empujar la caja por el pasillo este de la nave hasta la placa. Abre la reja de la torre.
        CreateCrate(level, "Caja de la nave", new Vector3(3.5f, 0.1f, 169f), real);
        CreatePressurePlate(level, "Placa de la nave", new Vector3(3.5f, 0.08f, 185f), "placa_torre", true, "Algo se mueve en la torre.", real);
        GameObject grate = CreateRealBlock("Reja de la torre", level, new Vector3(-6.85f, 1.5f, 172f), new Vector3(0.35f, 3f, 2f), real.Shadow);
        GameObject grateEvent = CreateMoveEvent("Reja que se abre", "placa_torre", grate.transform, Vector3.down * 3.2f, Vector3.zero, 2f);

        // Torre: x -14..-7, z 168.5..175.5, 13 m. Comparte el muro oeste de la nave; arriba se abre a la azotea.
        CreateRealBlock("Torre muro oeste", church, new Vector3(-13.75f, 6.5f, 172f), new Vector3(0.5f, 13f, 7f), real.StoneWall);
        CreateRealBlock("Torre muro sur", church, new Vector3(-10.5f, 6.5f, 168.75f), new Vector3(7f, 13f, 0.5f), real.StoneWall);
        CreateRealBlock("Torre muro norte", church, new Vector3(-10.5f, 6.5f, 175.25f), new Vector3(7f, 13f, 0.5f), real.StoneWall);
        CreateRealBlock("Torre muro este sur", church, new Vector3(-7.25f, 11f, 169.9f), new Vector3(0.5f, 4f, 2.2f), real.StoneWall);
        CreateRealBlock("Torre muro este norte", church, new Vector3(-7.25f, 11f, 174.1f), new Vector3(0.5f, 4f, 2.2f), real.StoneWall);
        CreateRealBlock("Torre dintel alto", church, new Vector3(-7.25f, 12.4f, 172f), new Vector3(0.5f, 1.2f, 2f), real.StoneWall);
        CreateMeshObject("Torre tejado", church, BuildGableRoofMesh(7f, 7f, 4.5f, 0.4f), new Vector3(-10.5f, 13f, 172f), false, real.RoofTiles);

        // 6. Escalones de la torre: ocho losas que dan la vuelta por dentro, 1.15 m cada una, hasta la azotea (9.2).
        Vector2[] ring =
        {
            new Vector2(-8.5f, 174f), new Vector2(-10.5f, 174f), new Vector2(-12.5f, 174f), new Vector2(-12.5f, 172f),
            new Vector2(-12.5f, 170f), new Vector2(-10.5f, 170f), new Vector2(-8.5f, 170f), new Vector2(-8.5f, 172f),
        };
        Vector3[] towerSteps = new Vector3[ring.Length];
        for (int i = 0; i < ring.Length; i++)
        {
            towerSteps[i] = new Vector3(ring[i].x, 1.15f * (i + 1), ring[i].y);
        }
        GameObject towerStairs = CreateStepChain("Escalones de la torre", "campanario_6", real.Planks, new Vector3(2f, 0.4f, 2f), towerSteps);

        // 7. Núcleo y portal en la azotea, bajo la campana (que se balancea al restaurarlo).
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(0f, 9.2f, 180f)));
        CreateRealBlock("Campana poste oeste", church, new Vector3(-2.6f, 12.2f, 180f), new Vector3(0.3f, 6f, 0.3f), real.DarkPlanks, false);
        CreateRealBlock("Campana poste este", church, new Vector3(2.6f, 12.2f, 180f), new Vector3(0.3f, 6f, 0.3f), real.DarkPlanks, false);
        CreateRealBlock("Campana viga", church, new Vector3(0f, 15.2f, 180f), new Vector3(5.8f, 0.3f, 0.3f), real.DarkPlanks, false);
        Transform bell = CreatePivot("Campana", church, new Vector3(0f, 15f, 180f));
        Material bronze = GetOrCreateMaterial("RealBronce", new Color(0.5f, 0.36f, 0.14f), null, 0f, 0.75f);
        CreateVisualPart("Cuerpo", bell, PrimitiveType.Cylinder, new Vector3(0f, -0.7f, 0f), new Vector3(1.1f, 0.5f, 1.1f), Vector3.zero, bronze);
        CreateVisualPart("Hombro", bell, PrimitiveType.Sphere, new Vector3(0f, -0.25f, 0f), Vector3.one * 1.05f, Vector3.zero, bronze);
        GameObject bellEvent = new GameObject("Campana que suena");
        bellEvent.transform.position = bell.position;
        MoveOnEvent bellMove = bellEvent.AddComponent<MoveOnEvent>();
        bellMove.Setup(bell, Vector3.zero, new Vector3(0f, 0f, 32f), 0.9f);
        SetEventId(bellMove, "nucleo_7");

        ConfigureEvent(bridge, deck.transform, real.Flame);
        ConfigureEvent(ladderEvent, ladder.transform, real.Flame);
        ConfigureEvent(lift, lift.transform, real.Flame);
        ConfigureEvent(churchEvent, churchDoor.transform, real.Flame);
        ConfigureEvent(grateEvent, grate.transform, real.Flame);
        ConfigureEvent(towerStairs, towerStairs.transform.GetChild(3), real.Flame);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // ================= ECOS =================
        float[,] echoes =
        {
            { -9f, 4f }, { 8f, 15f },                            // campos
            { 2f, 58f }, { -2f, 74f }, { 3f, 90f }, { 0f, 104f }, // calle mayor
            { -11f, 129f }, { 10f, 136f },                       // plaza
            { 3f, 181f },                                        // nave de la iglesia
        };
        for (int i = 0; i < echoes.GetLength(0); i++)
        {
            CreateEcho(level, "Eco " + (i + 1), VillagePoint(echoes[i, 0], echoes[i, 1]), 180f, real);
        }

        // ================= DECORADO =================
        // Trigo en los campos (no esconde: los Ecos no ven, solo esperan a que no los mires).
        Rect[] fields = { Rect.MinMaxRect(-46f, -18f, -6f, 22f), Rect.MinMaxRect(6f, -18f, 46f, 22f) };
        for (int i = 0; i < fields.Length; i++)
        {
            CreateGrassPatch(level, "Trigo " + (i + 1), fields[i], 3000, 1.1f, VillageHeight, (x, z) => DistanceToPath(VillagePath, x, z) > 2.2f, real.Wheat, 80 + i, false);
        }
        CreateGrassPatch(level, "Hierba de la aldea", Rect.MinMaxRect(-56f, 40f, 56f, 200f), 3500, 0.45f, VillageHeight,
            (x, z) => Mathf.Abs(x) > 18f && !(Mathf.Abs(z - 125f) < 18f && Mathf.Abs(x) < 32f), real.GrassBlades, 82, false);

        // Vallas del camino, carro, barriles y puestos de la plaza.
        for (float z = -18f; z < 22f; z += 4f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                CreateRealBlock("Poste de valla", level, VillagePoint(side * 3.2f, z, 0.5f), new Vector3(0.14f, 1.1f, 0.14f), real.DarkPlanks, false);
                CreateRealBlock("Travesano", level, VillagePoint(side * 3.2f, z + 2f, 0.75f), new Vector3(0.08f, 0.1f, 4f), real.DarkPlanks, false);
            }
        }
        Vector3 cart = VillagePoint(12f, 6f);
        CreateRealBlock("Carro caja", level, cart + Vector3.up * 0.8f, new Vector3(1.6f, 0.6f, 2.8f), real.Planks);
        for (int i = 0; i < 2; i++)
        {
            CreateVisualPart("Carro rueda " + (i + 1), level, PrimitiveType.Cylinder, cart + new Vector3(i == 0 ? -0.95f : 0.95f, 0.5f, 0f), new Vector3(1f, 0.06f, 1f), new Vector3(0f, 0f, 90f), real.DarkPlanks);
        }
        float[,] barrels = { { 5.6f, 47f }, { 5.9f, 48.2f }, { -5.6f, 70f }, { 5.7f, 88f }, { -13f, 118f }, { 14f, 119f }, { 13.2f, 120f } };
        for (int i = 0; i < barrels.GetLength(0); i++)
        {
            CreateVisualPart("Barril", level, PrimitiveType.Cylinder, new Vector3(barrels[i, 0], 0.55f, barrels[i, 1]), new Vector3(0.8f, 0.55f, 0.8f), Vector3.zero, real.DarkPlanks)
                .AddComponent<CapsuleCollider>();
        }
        for (int i = 0; i < 2; i++)
        {
            Vector3 stall = new Vector3(i == 0 ? -10f : 9f, 0f, i == 0 ? 138f : 113f);
            CreateRealBlock("Puesto mesa", level, stall + Vector3.up * 0.5f, new Vector3(3f, 1f, 1.4f), real.Planks);
            CreateRealBlock("Puesto toldo", level, stall + Vector3.up * 2.4f, new Vector3(3.6f, 0.08f, 2.2f), real.ClothDark, false).transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        // Charcos en la calle: discos oscuros muy lisos, reflejan la luz de los faroles.
        Material puddle = GetOrCreateMaterial("RealCharco", new Color(0.03f, 0.04f, 0.05f), null, 0f, 0.97f);
        for (int i = 0; i < 18; i++)
        {
            Vector3 position = new Vector3(Mathf.Lerp(-4f, 4f, (float)rng.NextDouble()), 0.075f, Mathf.Lerp(40f, 162f, (float)rng.NextDouble()));
            NoShadow(CreateVisualPart("Charco", level, PrimitiveType.Cylinder, position, new Vector3(1.2f + (float)rng.NextDouble() * 1.6f, 0.004f, 0.9f + (float)rng.NextDouble()), Vector3.zero, puddle));
        }

        // Árboles (robles y secos) y rocas alrededor de la aldea y en los campos.
        TreeModel[] oaks = CreateTreeModels("Roble", 1, 3);
        TreeModel[] dead = CreateTreeModels("ArbolSeco", 2, 2);
        Mesh[] rocks = CreateRockMeshes();
        for (int i = 0; i < 150; i++)
        {
            float x = Mathf.Lerp(-58f, 58f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-28f, 204f, (float)rng.NextDouble());
            bool inVillage = z > 34f && Mathf.Abs(x) < 34f && z < 196f;
            bool inFields = z < 24f && Mathf.Abs(x) < 48f;
            if (inVillage || inFields || Mathf.Abs(z - 30f) < 7f || Mathf.Abs(z - 37f) < 3f)
            {
                continue;
            }

            bool bare = rng.NextDouble() < 0.4;
            PlaceTree(level, bare ? dead : oaks, VillagePoint(x, z, -0.2f), 0.8f + (float)rng.NextDouble() * 0.6f, real.Bark, bare ? null : real.DryLeaves, rng, false);
            if (i % 4 == 0)
            {
                CreateRock(level, rocks, VillagePoint(x + 3f, z + 2f, 0.2f), 0.5f + (float)rng.NextDouble() * 1.2f, real.Rock, rng, false);
            }
        }

        CreateMotes("Bruma", level, new Vector3(0f, 1.2f, 90f), new Vector3(110f, 2.4f, 230f), materials.Star, new Color(1f, 1.05f, 1.1f, 0.1f), 140, 12f, 7f, 0f);
    }
}
