using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 6, "Bosque del Crepúsculo" (Nivel06_Bosque.unity): un bosque al atardecer sobre terreno
///   continuo (colinas, un río, un barranco, una meseta), con árboles, hierba, rocas y agua hechos por código.
///   Kael ya es humano (etapa 6: piel con brillo, túnica y capa).
///
/// POR QUÉ EXISTE
///   Sexto paso del arco de materialización: por primera vez el mundo es un lugar real y no islas flotantes.
///   Su identidad de juego es el SIGILO Y LA LUZ: lobos de sombra que ven y oyen, hierba alta para esconderse,
///   faros que se encienden con el Pulso (F) y son refugio y punto de reaparición, y lianas para trepar.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 6 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido (hacia el norte): Claro inicial > pradera con lobos > río (puente: árbol caído) > arboleda >
///   muralla de roca (zarzal) > hondonada > barranco (lianas) > meseta > arroyo > árbol hueco > claro alto.
///   Fragmentos: 1 cae el árbol-puente, 2 se hunde el zarzal, 3 crecen las lianas, 4 se seca el arroyo,
///   5 se abre el árbol hueco, 6 escalera de raíces, 7 núcleo.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel06ScenePath = "Assets/Scenes/Nivel06_Bosque.unity";

    private static RealMaterials forestReal;

    // Altura del suelo junto al camino: tierras bajas en 1 m; la meseta del norte, 7 m más arriba.
    private const float ForestLow = 1f;
    private const float ForestPlateauRise = 7f;
    private const float ForestMesaRise = 5f;
    private static readonly Vector2 ForestMesa = new Vector2(-38f, 214f);
    private static readonly Vector2 ForestHollowTree = new Vector2(-38f, 182f);

    // Tramos del camino que se caminan sin ayuda (entre uno y otro hay un puente, una liana...).
    private static readonly Vector2[][] ForestStretches =
    {
        new[] { new Vector2(0f, -18f), new Vector2(0f, 15f), new Vector2(-10f, 40f), new Vector2(-22f, 62f), new Vector2(-22f, 74.5f) },
        new[] { new Vector2(-22f, 92.5f), new Vector2(-18f, 108f), new Vector2(5f, 118f), new Vector2(5f, 132f), new Vector2(20f, 140f), new Vector2(28f, 146.5f) },
        new[]
        {
            new Vector2(28f, 152.5f), new Vector2(15f, 165f), new Vector2(0f, 172f), new Vector2(-9f, 174f), new Vector2(-19.5f, 174f),
            new Vector2(-28f, 176f), new Vector2(-31.5f, 182f), new Vector2(-32f, 188f), new Vector2(-38f, 188.3f),
        },
    };

    // El camino entero, de corrido: el terreno se aplana a su alrededor.
    private static readonly Vector2[] ForestPath = JoinStretches(ForestStretches, new Vector2(-38f, 203f));

    private static Vector2[] JoinStretches(Vector2[][] stretches, Vector2 last)
    {
        var all = new System.Collections.Generic.List<Vector2>();
        foreach (Vector2[] stretch in stretches)
        {
            all.AddRange(stretch);
        }

        all.Add(last);
        return all.ToArray();
    }

    // Centro del río (serpentea de oeste a este).
    private static float ForestRiverZ(float x)
    {
        return 86f + 3f * Mathf.Sin(x / 15f);
    }

    // ALTURA DEL TERRENO en (x, z). Es la "receta" del nivel: colinas de ruido y, encima, cada accidente.
    private static float ForestHeight(float x, float z)
    {
        // 1. Colinas suaves; junto al camino, el arroyo y la meseta final el suelo se aplana.
        float hills = (Fbm(x / 45f + 10f, z / 45f + 10f) - 0.5f) * 3f + (Fbm(x / 9f + 50f, z / 9f + 50f) - 0.5f) * 0.9f;
        float flat = Smooth(9f, 3f, DistanceToPath(ForestPath, x, z));
        float mesa = Smooth(13f, 11.5f, Mathf.Abs(x - ForestMesa.x)) * Smooth(11f, 9.5f, Mathf.Abs(z - ForestMesa.y));
        float streamDistance = Mathf.Abs(x + 14f);
        if (z > 151f)
        {
            flat = Mathf.Max(flat, Smooth(13f, 7f, streamDistance));
        }

        float h = ForestLow + hills * (1f - Mathf.Max(flat, mesa));

        // 2. Muralla de roca en z = 125, con un paso en x = 5 (lo cierra el zarzal).
        h += Mathf.Exp(-Mathf.Pow((z - 125f) / 2.4f, 2f)) * 8f * Smooth(2.5f, 4.5f, Mathf.Abs(x - 5f));

        // 3. Río: una zanja de 3.6 m bajo el nivel del agua.
        float riverDistance = Mathf.Abs(z - ForestRiverZ(x));
        if (riverDistance < 7f)
        {
            h = Mathf.Lerp(-3.6f, h, Smooth(3.5f, 7f, riverDistance));
        }

        // 4. Meseta del norte (barranco en z = 149) y, sobre ella, el arroyo y el claro alto.
        h += Smooth(148f, 150.5f, z) * ForestPlateauRise;
        if (z > 151f && streamDistance < 6.5f)
        {
            // Orillas tendidas (3 m de bajada en 5 m): cuando el arroyo se seca, el cauce se cruza caminando.
            h = Mathf.Lerp(ForestLow + ForestPlateauRise - 3f, h, Smooth(1.5f, 6.5f, streamDistance));
        }

        h += mesa * ForestMesaRise;

        // 5. Montes en los bordes: cierran el mapa.
        h += (Smooth(56f, 70f, Mathf.Abs(x)) + Smooth(-22f, -32f, z) + Smooth(226f, 238f, z)) * 16f;
        return h;
    }

    private static Vector3 ForestPoint(float x, float z, float lift = 0f)
    {
        return new Vector3(x, ForestHeight(x, z) + lift, z);
    }

    // True si en (x, z) se puede plantar algo: lejos del camino, del agua, de los barrancos y de las piezas del nivel.
    private static bool ForestIsFree(float x, float z, float pathMargin)
    {
        if (DistanceToPath(ForestPath, x, z) < pathMargin || Mathf.Abs(z - ForestRiverZ(x)) < 9f)
        {
            return false;
        }

        if ((z > 150f && Mathf.Abs(x + 14f) < 7.5f) || Mathf.Abs(z - 149.5f) < 3.5f || (Mathf.Abs(z - 125f) < 4f && Mathf.Abs(x - 5f) < 9f))
        {
            return false;
        }

        if (Mathf.Abs(x - ForestMesa.x) < 16f && Mathf.Abs(z - ForestMesa.y) < 14f)
        {
            return false;
        }

        return Vector2.Distance(new Vector2(x, z), ForestHollowTree) > 10f && z > -28f && z < 232f && Mathf.Abs(x) < 66f;
    }

    [MenuItem("Fragmento/Construir Nivel 6")]
    public static void BuildNivel6()
    {
        BuildThemedLevel(CreateNivel6Theme);
    }

    private static LevelTheme CreateNivel6Theme(Materials materials)
    {
        forestReal = CreateRealMaterials(new Color(0.85f, 0.95f, 0.75f), new Color(0.8f, 1f, 0.75f));
        // El pedestal del núcleo y el portal usan estos materiales: aquí son de piedra de verdad.
        materials.Ruin = forestReal.StoneWall;
        materials.Stone = forestReal.Rock;
        materials.StoneTop = forestReal.Rock;

        return new LevelTheme
        {
            ScenePath = Nivel06ScenePath,
            VolumePath = SettingsFolder + "/Nivel06_PerfilVolumen.asset",
            NextScenePath = Nivel07ScenePath,
            FilePrefix = "N6_",
            KaelPrefabName = "Kael_Nivel6",
            KaelStage = 6,
            FragmentFiles = new[] { "Claro", "Zarzal", "Liana", "Arroyo", "Tronco", "Raiz", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento del Claro", "Fragmento del Zarzal", "Fragmento de la Liana", "Fragmento del Arroyo",
                "Fragmento del Tronco", "Fragmento de la Raíz", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Un árbol muerto cruje y cae sobre el río: ya hay puente.",
                "El zarzal de la muralla se hunde y las luciérnagas marcan el sendero.",
                "Lianas trepan por el barranco. Camina contra ellas para subir.",
                "El arroyo de la meseta se seca: se puede cruzar por el cauce.",
                "El gran árbol hueco se abre.",
                "Las raíces del gran árbol forman una escalera hacia el claro alto.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            EventIds = new[] { "arbol_1", "zarzal_2", "lianas_3", "arroyo_4", "tronco_5", "raices_6", "nucleo_7" },
            Growth = forestReal.Bark,
            Accent = forestReal.Flame,
            Background = new Color(0.42f, 0.32f, 0.34f),
            FogColor = new Color(0.42f, 0.32f, 0.34f),
            FogDensity = 0.011f,
            SunColor = new Color(1f, 0.62f, 0.36f),
            SunIntensity = 1.7f,
            SunEuler = new Vector3(11f, -40f, 0f),
            AmbientSky = new Color(0.52f, 0.46f, 0.64f),
            AmbientEquator = new Color(0.5f, 0.38f, 0.34f),
            AmbientGround = new Color(0.08f, 0.1f, 0.07f),
            ZoneLightColor = Color.white,
            VolumeFilter = new Color(1f, 0.96f, 0.92f),
            KillY = -40f,
            Spawn = ForestPoint(0f, -18f, 0.05f),
            Zones = new ZoneInfo[0],
            RespawnAtCheckpoint = true,
            CreateKael = m => CreateHumanKaelPrefab(m, forestReal, "Kael_Nivel6", 6),
            TuneVolume = profile => TuneRealVolume(profile, 16f, -4f, -0.1f, 55f),
            BuildLayout = BuildBosqueLayout,
        };
    }

    private static void BuildBosqueLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        RealMaterials real = forestReal;
        System.Random rng = new System.Random(6);
        float plateau = ForestLow + ForestPlateauRise;          // 8
        float mesaTop = plateau + ForestMesaRise;               // 13

        ConfigureRealSky("CieloCrepusculo", new Color(0.66f, 0.42f, 0.55f), new Color(0.2f, 0.16f, 0.16f), 2.4f, 1.15f, 0.06f);

        new GameObject("Pistas del nivel").AddComponent<LevelHint>().Setup(
            "Mantén Shift para correr. Los lobos te oyen si corres.",
            "Pulsa F para lanzar un Pulso de luz: enciende faros y aturde a los lobos.",
            "La hierba alta te esconde si caminas. Un faro encendido es refugio y punto de regreso.");

        // ================= TERRENO =================
        Rect area = Rect.MinMaxRect(-72f, -34f, 72f, 240f);
        Mesh terrainMesh = SaveMesh(BuildTerrainMesh(area, 2f, ForestHeight), "TerrenoBosque");
        MakeStatic(CreateMeshObject("Terreno", level, terrainMesh, Vector3.zero, true, real.Grass));
        for (int i = 0; i < ForestStretches.Length; i++)
        {
            Mesh trail = SaveMesh(BuildRibbonMesh(ForestStretches[i], 2.6f, ForestHeight, 0.05f), "SenderoBosque" + (i + 1));
            NoShadow(CreateMeshObject("Sendero " + (i + 1), level, trail, Vector3.zero, false, real.Dirt));
        }
        CreatePathMarkers(level, ForestStretches, ForestHeight);

        // Paredes invisibles: los barrancos y la muralla no se pueden escalar a saltos, y el mapa tiene borde.
        CreateInvisibleWall(level, "Pared del barranco", new Vector3(0f, 2.5f, 149.6f), new Vector3(144f, 7f, 2f));
        CreateInvisibleWall(level, "Pared de la muralla oeste", new Vector3(-34f, 4f, 125f), new Vector3(72.4f, 10f, 1.6f));
        CreateInvisibleWall(level, "Pared de la muralla este", new Vector3(39f, 4f, 125f), new Vector3(62.4f, 10f, 1.6f));
        CreateInvisibleWall(level, "Borde oeste", new Vector3(-63f, 15f, 100f), new Vector3(2f, 50f, 280f));
        CreateInvisibleWall(level, "Borde este", new Vector3(63f, 15f, 100f), new Vector3(2f, 50f, 280f));
        CreateInvisibleWall(level, "Borde sur", new Vector3(0f, 15f, -28f), new Vector3(140f, 50f, 2f));
        CreateInvisibleWall(level, "Borde norte", new Vector3(0f, 15f, 232f), new Vector3(140f, 50f, 2f));
        float mesaWallY = (mesaTop - 0.25f + 5f) / 2f, mesaWallHeight = mesaTop - 0.25f - 5f;
        CreateInvisibleWall(level, "Pared del claro sur", new Vector3(ForestMesa.x, mesaWallY, ForestMesa.y - 10.3f), new Vector3(25f, mesaWallHeight, 1.5f));
        CreateInvisibleWall(level, "Pared del claro norte", new Vector3(ForestMesa.x, mesaWallY, ForestMesa.y + 10.3f), new Vector3(25f, mesaWallHeight, 1.5f));
        CreateInvisibleWall(level, "Pared del claro oeste", new Vector3(ForestMesa.x - 12.3f, mesaWallY, ForestMesa.y), new Vector3(1.5f, mesaWallHeight, 22f));
        CreateInvisibleWall(level, "Pared del claro este", new Vector3(ForestMesa.x + 12.3f, mesaWallY, ForestMesa.y), new Vector3(1.5f, mesaWallHeight, 22f));

        // ================= AGUA =================
        // Río: caer en él ahoga (TideWater sin marea: solo comprueba si Kael quedó bajo la superficie).
        GameObject river = CreateWaterSheet(level, "Rio", new Vector3(0f, -1.3f, 86f), new Vector2(150f, 24f), new Vector2(0.06f, 0f), real);
        river.AddComponent<TideWater>().Setup(0f);

        // Arroyo de la meseta: corre de norte a sur y cae por el barranco. El fragmento 4 lo seca.
        GameObject stream = CreateWaterSheet(level, "Arroyo", new Vector3(-14f, plateau - 0.6f, 192f), new Vector2(12f, 86f), new Vector2(0f, 0.1f), real);
        GameObject drown = new GameObject("Zona de ahogo");
        drown.transform.SetParent(stream.transform, false);
        drown.transform.position = new Vector3(-14f, plateau - 3f, 192f);
        BoxCollider drownBox = drown.AddComponent<BoxCollider>();
        drownBox.size = new Vector3(8.6f, 3.8f, 86f); // la cara de arriba queda 1.1 m bajo la orilla
        drownBox.isTrigger = true;
        drown.AddComponent<KillZone>();
        GameObject streamFall = CreateRealBlock("Cascada del arroyo", level, new Vector3(-14f, 4f, 148.4f), new Vector3(7f, 7.4f, 0.3f), real.Water, false);
        streamFall.AddComponent<UvScroll>().Setup(new Vector2(0f, 0.6f));
        streamFall.transform.SetParent(stream.transform, true); // se va con el arroyo al secarse

        // ================= FRAGMENTOS =================
        context.Fragment(1, ForestPoint(8f, 20f, 1.3f));        // pradera, a la vista desde el inicio
        context.Fragment(2, ForestPoint(-44f, 112f, 1.3f));     // arboleda del oeste, con lobos
        context.Fragment(3, ForestPoint(31f, 141f, 1.3f));      // hondonada, al pie del barranco
        context.Fragment(4, ForestPoint(10f, 167f, 1.3f));      // meseta
        context.Fragment(5, ForestPoint(-26f, 170f, 1.3f));     // al otro lado del arroyo
        context.Fragment(6, ForestPoint(ForestHollowTree.x, ForestHollowTree.y, 1.3f)); // dentro del árbol hueco
        context.Fragment(7, new Vector3(ForestMesa.x, mesaTop + 1.3f, ForestMesa.y - 5f)); // claro alto

        // ================= EVENTOS =================

        // 1. Árbol muerto en la orilla sur: cae hacia el norte y su tronco (16 m) queda de puente.
        Vector3 treeBase = ForestPoint(-22f, 75.5f, -0.25f);
        GameObject deadTree = new GameObject("Arbol puente");
        deadTree.transform.position = treeBase;
        Transform pivot = CreatePivot("Pivote", deadTree.transform, Vector3.zero);
        MeshBuilder log = new MeshBuilder();
        AddTrunk(log, Vector3.zero, Vector3.up, 16.5f, 0.75f, 0.6f, 12, 0f, rng);
        GameObject trunk = CreateMeshObject("Tronco", pivot, SaveMesh(log.ToMesh(false), "TroncoPuente"), treeBase, false, real.Bark);
        BoxCollider trunkBox = trunk.AddComponent<BoxCollider>();
        trunkBox.center = new Vector3(0f, 8.25f, 0f);
        trunkBox.size = new Vector3(1.3f, 16.5f, 1.3f); // tumbado, la cara de arriba es plana: se camina bien
        FallingTreeBridge fall = deadTree.AddComponent<FallingTreeBridge>();
        fall.Setup(pivot, 90f);
        SetEventId(fall, "arbol_1");

        // 2. Zarzal que cierra el paso de la muralla; se hunde. A la vez despierta un enjambre de luciérnagas.
        GameObject brambles = CreateRealBlock("Zarzal", level, ForestPoint(5f, 125f, 2.7f), new Vector3(5.8f, 5.6f, 2.2f), real.Shadow);
        for (int i = 0; i < 26; i++)
        {
            Vector3 local = new Vector3(Mathf.Lerp(-2.7f, 2.7f, (float)rng.NextDouble()), Mathf.Lerp(-2.6f, 2.8f, (float)rng.NextDouble()), (rng.Next(2) == 0 ? -1f : 1f) * 1.15f);
            NoShadow(CreateVisualPart("Espina", brambles.transform, PrimitiveType.Cube, local, new Vector3(0.1f, 0.1f, 0.9f),
                new Vector3((float)rng.NextDouble() * 60f - 30f, (float)rng.NextDouble() * 60f - 30f, 0f), materials.Enemy));
        }
        GameObject bramblesEvent = CreateMoveEvent("Zarzal de la muralla", "zarzal_2", brambles.transform, Vector3.down * 6.2f, Vector3.zero, 2.2f);

        GameObject swarm = new GameObject("Enjambre");
        swarm.transform.SetParent(level, false);
        Vector2[] glowPoints = { new Vector2(5f, 132f), new Vector2(20f, 140f), new Vector2(28f, 153f), new Vector2(15f, 165f), new Vector2(-9f, 174f), new Vector2(-28f, 176f) };
        foreach (Vector2 glow in glowPoints)
        {
            CreateZoneLight(swarm.transform, ForestPoint(glow.x, glow.y, 2.5f), new Color(0.9f, 1f, 0.5f), 9f, 2.2f);
        }
        CreateMotes("Luciernagas del sendero", swarm.transform, new Vector3(0f, plateau - 2f, 155f), new Vector3(70f, 9f, 60f), materials.Star, new Color(2.2f, 2.4f, 0.8f, 1f), 400, 70f, 0.14f, 0.01f);
        CreateActivator("Enjambre de luciernagas", "zarzal_2", swarm);

        // 3. Lianas en el barranco (pared que mira al sur): suben de la hondonada (y 1) a la meseta (y 8).
        CreateRealBlock("Roca del barranco", level, new Vector3(28f, 3.4f, 149.3f), new Vector3(8f, 7f, 2.4f), real.Rock);
        GameObject vines = CreateClimb(level, "Lianas del barranco", ForestPoint(28f, 148.05f), plateau - ForestLow + 0.4f, Vector3.back, false, real);
        GameObject vinesEvent = CreateActivator("Lianas", "lianas_3", vines);

        // 4. El arroyo se seca: el agua (y su zona de ahogo) baja 3 m, por debajo del cauce.
        GameObject streamEvent = CreateMoveEvent("Arroyo que se seca", "arroyo_4", stream.transform, Vector3.down * 3f, Vector3.zero, 3f);

        // 5. Árbol hueco gigante: anillo de 12 tablas de corteza; la del este es la puerta, que se hunde.
        Vector3 hollow = ForestPoint(ForestHollowTree.x, ForestHollowTree.y);
        GameObject giant = new GameObject("Arbol hueco");
        giant.transform.SetParent(level, false);
        Transform door = null;
        for (int k = 0; k < 12; k++)
        {
            float angle = k * Mathf.PI / 6f;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Quaternion facing = Quaternion.LookRotation(radial, Vector3.up);
            if (k == 0)
            {
                door = CreateRealBlock("Puerta del arbol", giant.transform, hollow + radial * 4.2f + Vector3.up * 1.6f, new Vector3(2.35f, 3.4f, 0.7f), real.Bark).transform;
                door.rotation = facing;
                CreateRealBlock("Dintel del arbol", giant.transform, hollow + radial * 4.2f + Vector3.up * 7.2f, new Vector3(2.35f, 7.8f, 0.8f), real.Bark).transform.rotation = facing;
            }
            else
            {
                float plankHeight = 11f + (float)rng.NextDouble() * 2.5f;
                CreateRealBlock("Corteza " + k, giant.transform, hollow + radial * 4.2f + Vector3.up * (plankHeight / 2f - 0.3f), new Vector3(2.35f, plankHeight, 0.8f), real.Bark).transform.rotation = facing;
            }
        }
        MeshBuilder canopy = new MeshBuilder(), roots = new MeshBuilder();
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.PI * 2f / 7f;
            Vector3 around = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            AddBlob(canopy, around * 6f + Vector3.up * (14f + (i % 2) * 3f), new Vector3(6f, 4.5f, 6f), 0.3f, 300f + i * 9f, 8);
            if (i != 0)
            {
                AddTrunk(roots, around * 4f + Vector3.up * 1.6f, (around + Vector3.down * 0.45f).normalized, 4.5f, 0.75f, 0.2f, 7, 0.05f, rng);
            }
        }
        AddBlob(canopy, Vector3.up * 18f, new Vector3(8f, 5f, 8f), 0.3f, 390f, 9);
        NoShadow(CreateMeshObject("Copa gigante", giant.transform, SaveMesh(canopy.ToMesh(true), "CopaGigante"), hollow, false, real.Leaves));
        CreateMeshObject("Raices gigantes", giant.transform, SaveMesh(roots.ToMesh(false), "RaicesGigantes"), hollow, false, real.Bark);
        CreateZoneLight(giant.transform, hollow + Vector3.up * 3f, new Color(1f, 0.8f, 0.5f), 7f, 2.5f);
        GameObject doorEvent = CreateMoveEvent("Arbol hueco que se abre", "tronco_5", door, Vector3.down * 3.6f, Vector3.zero, 2.5f);

        // 6. Escalera de raíces: seis escalones de 0.8 m, del pie del árbol (y 8) al claro alto (y 13).
        Vector3[] steps = new Vector3[6];
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i] = new Vector3(ForestMesa.x, plateau + 0.8f * (i + 1), 190.2f + 2.3f * i);
        }
        GameObject stairs = CreateStepChain("Escalera de raices", "raices_6", real.Bark, new Vector3(3.2f, 0.5f, 2.2f), steps);

        // 7. Núcleo y portal en el claro alto.
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(ForestMesa.x, mesaTop, ForestMesa.y)));

        ConfigureEvent(deadTree, trunk.transform, real.Flame);
        ConfigureEvent(bramblesEvent, brambles.transform, real.Flame);
        ConfigureEvent(vinesEvent, vines.transform, real.Flame);
        ConfigureEvent(streamEvent, stream.transform, real.Flame);
        ConfigureEvent(doorEvent, door, real.Flame);
        ConfigureEvent(stairs, stairs.transform.GetChild(2), real.Flame);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // ================= FAROS (se encienden con el Pulso; refugio y reaparición) =================
        CreateBeacon(level, "Faro del claro", ForestPoint(4f, -14f), 7f, true, "", false, real, materials);
        CreateBeacon(level, "Faro del rio", ForestPoint(-17f, 98f), 7f, false, "", false, real, materials);
        CreateBeacon(level, "Faro de la meseta", ForestPoint(24f, 157f), 7f, false, "", false, real, materials);
        CreateBeacon(level, "Faro del gran arbol", ForestPoint(-29f, 189f), 7f, false, "", false, real, materials);

        // ================= LOBOS DE SOMBRA =================
        float[,] dens =
        {
            { -14f, 45f, 8f }, { 14f, 42f, 8f },                       // pradera
            { -44f, 106f, 6f }, { -38f, 116f, 6f },                    // arboleda del fragmento 2
            { 18f, 133f, 6f }, { 30f, 137f, 5f }, { -8f, 138f, 7f },   // hondonada
            { 12f, 170f, 6f }, { -2f, 160f, 6f },                      // meseta
            { -30f, 171f, 5f },                                        // junto al gran árbol
        };
        for (int i = 0; i < dens.GetLength(0); i++)
        {
            CreateShadowWolf(level, "Lobo de sombra " + (i + 1), ForestPoint(dens[i, 0], dens[i, 1]), dens[i, 2], real);
        }

        // ================= HIERBA =================
        // Hierba alta (esconde): cuatro parches junto a las zonas de lobos.
        Rect[] tall =
        {
            Rect.MinMaxRect(-24f, 24f, -6f, 58f), Rect.MinMaxRect(4f, 30f, 22f, 52f),
            Rect.MinMaxRect(-52f, 100f, -32f, 120f), Rect.MinMaxRect(8f, 128f, 34f, 145f),
        };
        for (int i = 0; i < tall.Length; i++)
        {
            CreateGrassPatch(level, "Hierba alta " + (i + 1), tall[i], 1500, 1.5f, ForestHeight, (x, z) => DistanceToPath(ForestPath, x, z) > 1.2f, real.GrassBlades, 60 + i, true);
        }

        // Hierba corta por todo el bosque, en seis franjas (cada franja es una sola malla).
        for (int i = 0; i < 6; i++)
        {
            Rect band = Rect.MinMaxRect(-62f, -26f + i * 43f, 62f, -26f + (i + 1) * 43f);
            CreateGrassPatch(level, "Hierba " + (i + 1), band, 3200, 0.55f, ForestHeight,
                (x, z) => DistanceToPath(ForestPath, x, z) > 1.4f && Mathf.Abs(z - ForestRiverZ(x)) > 6.5f && !(z > 150f && Mathf.Abs(x + 14f) < 6.5f), real.GrassBlades, 70 + i, false);
        }

        // ================= ÁRBOLES Y ROCAS =================
        TreeModel[] pines = CreateTreeModels("Pino", 0, 3);
        TreeModel[] oaks = CreateTreeModels("Roble", 1, 3);
        TreeModel[] dead = CreateTreeModels("ArbolSeco", 2, 2);
        Mesh[] rocks = CreateRockMeshes();
        Transform forest = new GameObject("Bosque").transform;
        forest.SetParent(level, false);
        for (int i = 0; i < 560; i++)
        {
            float x = Mathf.Lerp(-66f, 66f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-27f, 231f, (float)rng.NextDouble());
            if (!ForestIsFree(x, z, 4.5f))
            {
                continue;
            }

            bool pine = z > 150f ? rng.NextDouble() < 0.8 : rng.NextDouble() < 0.3;
            bool nearPath = DistanceToPath(ForestPath, x, z) < 16f;
            float scale = 0.8f + (float)rng.NextDouble() * 0.6f;
            if (rng.NextDouble() < 0.06)
            {
                PlaceTree(forest, dead, ForestPoint(x, z, -0.2f), scale, real.Bark, null, rng, nearPath);
            }
            else
            {
                PlaceTree(forest, pine ? pines : oaks, ForestPoint(x, z, -0.2f), scale, real.Bark, pine ? real.PineLeaves : real.Leaves, rng, nearPath);
            }
        }

        for (int i = 0; i < 90; i++)
        {
            float x = Mathf.Lerp(-64f, 64f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-26f, 230f, (float)rng.NextDouble());
            if (ForestIsFree(x, z, 3.2f))
            {
                float size = 0.5f + (float)rng.NextDouble() * 1.6f;
                CreateRock(forest, rocks, ForestPoint(x, z, size * 0.15f), size, real.Rock, rng, size > 1.1f);
            }
        }

        // Rocas a los lados del zarzal y en las orillas del río: dan forma a los pasos estrechos.
        float[,] placed = { { 1.2f, 124f, 2.4f }, { 8.8f, 126f, 2.6f }, { -27f, 76f, 1.6f }, { -17f, 93f, 1.8f }, { 22f, 147f, 2f }, { 34f, 147f, 2.2f } };
        for (int i = 0; i < placed.GetLength(0); i++)
        {
            CreateRock(forest, rocks, ForestPoint(placed[i, 0], placed[i, 1], placed[i, 2] * 0.2f), placed[i, 2], real.Rock, rng, true);
        }

        // ================= AMBIENTE =================
        CreateMotes("Luciernagas", level, new Vector3(0f, 4f, 60f), new Vector3(120f, 7f, 180f), materials.Star, new Color(2f, 2.2f, 0.8f, 0.9f), 350, 40f, 0.11f, 0.01f);
        CreateMotes("Niebla baja", level, new Vector3(0f, 0f, 86f), new Vector3(140f, 2f, 30f), materials.Star, new Color(1.1f, 1f, 1.1f, 0.12f), 90, 8f, 6f, 0f);
    }

    // Pone el "Event Id" de un evento de mundo creado a mano.
    private static void SetEventId(WorldEventBase worldEvent, string eventId)
    {
        SerializedObject serialized = new SerializedObject(worldEvent);
        Property(serialized, "eventId").stringValue = eventId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(worldEvent);
    }
}
