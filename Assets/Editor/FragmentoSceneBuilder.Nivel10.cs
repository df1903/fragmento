using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 10, "El Jardín del Amanecer" (Nivel10_Despertar.unity): el último nivel del juego. Kael, aún
///   diminuto como en el nivel 9 (todo a escala 8), sale por fin de la casa al jardín trasero al amanecer: césped
///   que le cubre, aspersores, una cama elástica de juguete, la mesa de pícnic, una jardinera, flores gigantes, la
///   valla, un columpio colgado de un roble enorme y, arriba, la casita del árbol, frente al sol que sale.
///
/// POR QUÉ EXISTE
///   Es el final del arco de materialización: Kael "despierta" y sale a la luz. Conserva el estilo del nivel 9
///   (escala, juguetes y piezas animadas) en otro escenario. Clímax: la Sombra del Olvido sube desde el césped y hay
///   que trepar al roble; al restaurar el núcleo amanece (DawnFinale) y el portal muestra los créditos (FinalCredits).
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 10 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: porche > césped (aspersores) > cama elástica > banco y mesa (abejas) > rastrillo > jardinera
///   (mariquita) > flores > valla (pájaro) > columpio > rama baja > escalera de cuerda > ramas > casita del árbol.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel10ScenePath = "Assets/Scenes/Nivel10_Despertar.unity";

    private static RealMaterials dawnReal;

    // El columpio: punto del que cuelga (bajo la rama del roble), largo de las cuerdas y grados a cada lado.
    private static readonly Vector3 SwingPivot = new Vector3(-18.9f, 27.5f, 56f);
    private const float SwingLength = 19.1f;
    private const float SwingAmplitude = 45f;

    [MenuItem("Fragmento/Construir Nivel 10")]
    public static void BuildNivel10()
    {
        BuildThemedLevel(CreateNivel10Theme);
    }

    private static LevelTheme CreateNivel10Theme(Materials materials)
    {
        dawnReal = CreateRealMaterials(new Color(0.85f, 0.95f, 0.75f), new Color(0.8f, 1f, 0.75f));
        materials.Ruin = dawnReal.Planks;
        materials.Stone = dawnReal.Planks;
        materials.StoneTop = dawnReal.Planks;

        return new LevelTheme
        {
            ScenePath = Nivel10ScenePath,
            VolumePath = SettingsFolder + "/Nivel10_PerfilVolumen.asset",
            NextScenePath = "",
            FilePrefix = "N10_",
            KaelPrefabName = "Kael_Nivel10",
            KaelStage = 10,
            FragmentFiles = new[] { "Rocio", "Pícnic", "Jardinera", "Valla", "Columpio", "Roble", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento del Rocío", "Fragmento del Pícnic", "Fragmento de la Jardinera", "Fragmento de la Valla",
                "Fragmento del Columpio", "Fragmento del Roble", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Una cama elástica de juguete aparece en el césped. Salta sobre ella.",
                "El rastrillo se vence y cae hasta la jardinera.",
                "Las flores crecen hacia la valla.",
                "El columpio empieza a balancearse. Súbete cuando pase junto a la valla.",
                "Los farolillos del roble se encienden.",
                "Una escalera de cuerda baja del roble… y algo sube desde el césped.",
                "El Núcleo de la Realidad brilla con el primer sol.",
            },
            EventIds = new[] { "cama_1", "rastrillo_2", "flores_3", "columpio_4", "farolillos_5", "despertar_6", "nucleo_7" },
            Growth = dawnReal.Planks,
            Accent = dawnReal.Flame,
            Background = new Color(0.45f, 0.42f, 0.52f),
            FogColor = new Color(0.55f, 0.5f, 0.58f),
            FogDensity = 0.005f,
            SunColor = new Color(1f, 0.65f, 0.5f),
            SunIntensity = 1.2f,
            SunEuler = new Vector3(10f, 200f, 0f),
            AmbientSky = new Color(0.4f, 0.42f, 0.58f),
            AmbientEquator = new Color(0.36f, 0.33f, 0.38f),
            AmbientGround = new Color(0.12f, 0.14f, 0.1f),
            ZoneLightColor = Color.white,
            VolumeFilter = new Color(1f, 0.97f, 0.97f),
            KillY = -10f,
            Spawn = new Vector3(0f, 1.45f, -38f),
            Zones = new ZoneInfo[0],
            RespawnAtCheckpoint = true,
            CreateKael = m => CreateHumanKaelPrefab(m, dawnReal, "Kael_Nivel10", 10),
            TuneVolume = profile => TuneRealVolume(profile, 8f, 6f, 0.1f, 50f),
            BuildLayout = BuildJardinFinalLayout,
            AfterHud = CreateFinalCredits,
        };
    }

    private static void BuildJardinFinalLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        RealMaterials real = dawnReal;
        HomeMaterials home = CreateHomeMaterials(real);
        System.Random rng = new System.Random(10);

        ConfigureRealSky("CieloAmanecer", new Color(0.62f, 0.5f, 0.62f), new Color(0.3f, 0.3f, 0.25f), 2f, 1.15f, 0.07f);
        new GameObject("Pistas del nivel").AddComponent<LevelHint>().Setup(
            "Por fin, afuera. El sol está a punto de salir tras el roble.",
            "Salta los chorros de los aspersores. La casita del árbol te espera arriba.");

        // Materiales del jardín, a escala 8 (las texturas se repiten 8 veces menos).
        Material lawn = GetOrCreateTexturedMaterialCopy("JardinCesped", real.Grass, new Color(0.75f, 0.9f, 0.6f));
        lawn.SetTextureScale("_BaseMap", Vector2.one * 0.03f);
        Material brick = GetOrCreateTexturedMaterial("JardinLadrillo", MakeSurface("Ladrillo", PaintBrick, false, 6f), new Vector2(1f / 12f, 1f / 6f), Color.white, 0.15f);
        Material soil = GetOrCreateTexturedMaterialCopy("JardinTierra", real.Dirt, new Color(0.6f, 0.5f, 0.4f));
        soil.SetTextureScale("_BaseMap", Vector2.one * 0.05f);
        Material bark = GetOrCreateTexturedMaterialCopy("JardinCorteza", real.Bark, Color.white);
        bark.SetTextureScale("_BaseMap", new Vector2(0.4f, 0.06f));
        Material rope = GetOrCreateTexturedMaterialCopy("JardinCuerda", real.Cloth, new Color(0.75f, 0.62f, 0.42f));
        Material water = GetOrCreateTransparentMaterial("JardinChorro", "Universal Render Pipeline/Lit", new Color(0.75f, 0.9f, 1f, 0.45f), new Color(0.5f, 0.75f, 1f), 0.5f, false);
        Material petals = GetOrCreateMaterial("JardinPetalo", new Color(0.95f, 0.93f, 0.88f), null, 0f, 0.3f);
        Material pollen = GetOrCreateMaterial("JardinPolen", new Color(1f, 0.75f, 0.15f), new Color(1f, 0.6f, 0.1f), 0.4f, 0.4f);
        Material stem = GetOrCreateMaterial("JardinTallo", new Color(0.25f, 0.5f, 0.2f), null, 0f, 0.2f);

        // ================= CÉSPED, CASA Y PORCHE =================
        CreateRealBlock("Cesped", level, new Vector3(0f, -0.5f, 20f), new Vector3(170f, 1f, 170f), lawn);
        CreateRealBlock("Fachada de la casa", level, new Vector3(0f, 22f, -46f), new Vector3(140f, 44f, 8f), brick);
        CreateRealBlock("Puerta trasera", level, new Vector3(0f, 9.4f, -41.9f), new Vector3(8f, 16f, 0.3f), home.DarkWood, false);
        for (int i = 0; i < 6; i++)
        {
            // Ventanas de la casa: unas encendidas (alguien más despierta dentro) y otras no.
            float x = (i < 3 ? -1f : 1f) * (14f + (i % 3) * 14f);
            Material glass = i % 2 == 0 ? GetOrCreateMaterial("JardinVentanaEncendida", new Color(1f, 0.8f, 0.5f), new Color(1f, 0.7f, 0.4f), 1.2f, 0.5f) : home.Black;
            CreateRealBlock("Ventana de la casa", level, new Vector3(x, 14f + (i % 2) * 16f, -41.95f), new Vector3(8f, 10f, 0.1f), glass, false);
            CreateRealBlock("Marco", level, new Vector3(x, 8.6f + (i % 2) * 16f, -41.8f), new Vector3(9f, 0.6f, 0.6f), home.Wood, false);
        }
        CreateRealBlock("Porche", level, new Vector3(0f, 0.7f, -38f), new Vector3(24f, 1.4f, 8f), home.Wood);
        CreateRealBlock("Escalon del porche", level, new Vector3(0f, 0.35f, -33.3f), new Vector3(12f, 0.7f, 1.4f), home.Wood);
        CreateRealBlock("Felpudo", level, new Vector3(0f, 1.42f, -39f), new Vector3(8f, 0.04f, 4f), home.Rug, false);

        // ================= FRAGMENTOS =================
        context.Fragment(1, new Vector3(6f, 1.3f, -18f));                       // césped, junto al aspersor
        context.Fragment(2, new Vector3(-23f, 7.3f, -1f));                      // mesa de pícnic, entre abejas
        context.Fragment(3, new Vector3(-20f, 9.3f, 17f));                      // jardinera
        context.Fragment(4, new Vector3(-35.2f, 15.3f, 40f));                   // lo alto de la valla
        context.Fragment(5, SwingPivot + Vector3.down * (SwingLength - 1.3f));  // en el punto más bajo del columpio
        context.Fragment(6, new Vector3(-7f, 15.3f, 58f));                      // rama baja del roble
        context.Fragment(7, new Vector3(0f, 33.3f, 67f));                       // casita del árbol

        // ================= ASPERSORES =================
        CreateSprinkler(level, "Aspersor 1", new Vector3(2f, 0f, -16f), 8f, 50f, home, water, materials);
        CreateSprinkler(level, "Aspersor 2", new Vector3(12f, 0f, 2f), 7f, -60f, home, water, materials);

        // 1. Cama elástica de juguete (aparece con el fragmento 1).
        GameObject trampoline = CreateBouncePad(level, "Cama elastica", new Vector3(-12f, 0f, -12f), 6.5f, home.Black, home.Toys[1]);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI / 2f + Mathf.PI / 4f;
            CreateVisualPart("Pata", trampoline.transform, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(angle) * 1.1f, 0.2f, Mathf.Sin(angle) * 1.1f), new Vector3(0.12f, 0.2f, 0.12f), Vector3.zero, home.Brass);
        }
        GameObject trampolineEvent = CreateActivator("Cama elastica que aparece", "cama_1", trampoline);

        // ================= PÍCNIC =================
        CreateFurnitureTop(level, "Banco", Rect.MinMaxRect(-30f, -12f, -18f, -8f), 3.6f, 0.5f, 0.6f, home.Wood);
        CreateFurnitureTop(level, "Mesa de picnic", Rect.MinMaxRect(-30f, -7f, -18f, 3f), 6f, 0.6f, 0.8f, home.Wood);
        GameObject pot = CreateVisualPart("Maceta del banco", level, PrimitiveType.Cylinder, new Vector3(-19.2f, 4.2f, -8.8f), new Vector3(1.6f, 0.6f, 1.6f), Vector3.zero, GetOrCreateMaterial("CasaBarro", new Color(0.55f, 0.28f, 0.16f), null, 0f, 0.3f));
        pot.AddComponent<BoxCollider>();
        Material cloth = GetOrCreateTexturedMaterial("JardinMantel", MakeSurface("Colcha", PaintQuilt, false, 3f), Vector2.one / 8f, new Color(1f, 0.85f, 0.85f), 0.05f);
        NoShadow(CreateRealBlock("Mantel", level, new Vector3(-24f, 6.02f, -2f), new Vector3(11.6f, 0.04f, 9.6f), cloth, false));
        for (int i = 0; i < 2; i++)
        {
            GameObject cup = CreateVisualPart("Taza", level, PrimitiveType.Cylinder, new Vector3(-20f + i * 1.8f, 6.6f, 1f), new Vector3(1.2f, 0.6f, 1.2f), Vector3.zero, home.Paper);
            cup.AddComponent<CapsuleCollider>();
        }
        GameObject teapot = CreateVisualPart("Tetera", level, PrimitiveType.Sphere, new Vector3(-28f, 7.1f, 1.5f), new Vector3(2.2f, 2f, 2.2f), Vector3.zero, home.Toys[1]);
        teapot.AddComponent<SphereCollider>();

        // Abejas que vuelan en círculo sobre la mesa.
        Vector3[] beeLoop = new Vector3[12];
        for (int i = 0; i < beeLoop.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / beeLoop.Length;
            beeLoop[i] = new Vector3(-24f + Mathf.Cos(angle) * 3.5f, 6.1f, -2f + Mathf.Sin(angle) * 3.5f);
        }
        for (int b = 0; b < 2; b++)
        {
            GameObject bee = new GameObject("Abeja " + (b + 1));
            bee.transform.SetParent(level, false);
            CreateVisualPart("Cuerpo", bee.transform, PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.6f, 1f), Vector3.zero, home.Toys[2]);
            CreateVisualPart("Raya", bee.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.72f, 0.06f, 0.72f), new Vector3(90f, 0f, 0f), home.Black);
            NoShadow(CreateVisualPart("Ala", bee.transform, PrimitiveType.Sphere, new Vector3(0.35f, 1.3f, 0f), new Vector3(0.6f, 0.05f, 0.35f), new Vector3(0f, 0f, 20f), petals));
            NoShadow(CreateVisualPart("Ala", bee.transform, PrimitiveType.Sphere, new Vector3(-0.35f, 1.3f, 0f), new Vector3(0.6f, 0.05f, 0.35f), new Vector3(0f, 0f, -20f), petals));
            Vector3[] beeRoute = new Vector3[beeLoop.Length];
            for (int i = 0; i < beeRoute.Length; i++)
            {
                beeRoute[i] = beeLoop[(i + b * 6) % beeLoop.Length];
            }
            bee.AddComponent<PathHazard>().Setup(beeRoute, true, 4.5f, 1f);
        }

        // 2. Rastrillo: de pie en la mesa, cae hasta la jardinera (algo inclinado).
        Vector3 rakeBase = new Vector3(-24f, 6.15f, 2.6f);
        GameObject rake = new GameObject("Rastrillo");
        rake.transform.position = rakeBase;
        Transform rakePivot = CreatePivot("Pivote", rake.transform, Vector3.zero);
        GameObject rakeBody = CreateRealBlock("Mango", rakePivot, rakeBase, new Vector3(1.6f, 9.2f, 0.3f), home.Wood);
        rakeBody.transform.localPosition = new Vector3(0f, 4.6f, 0f);
        GameObject rakeHead = CreateVisualPart("Peine", rakeBody.transform, PrimitiveType.Cube, new Vector3(0f, 4.4f, 0.3f), new Vector3(4f, 0.4f, 0.5f), Vector3.zero, home.Toys[3]);
        for (int t = 0; t < 7; t++)
        {
            CreateVisualPart("Diente", rakeHead.transform, PrimitiveType.Cube, new Vector3(-0.42f + t * 0.14f, 0f, 1.6f), new Vector3(0.04f, 0.6f, 2.4f), Vector3.zero, home.Toys[3]);
        }
        FallingTreeBridge rakeFall = rake.AddComponent<FallingTreeBridge>();
        rakeFall.Setup(rakePivot, 77f);
        SetEventId(rakeFall, "rastrillo_2");

        // ================= JARDINERA =================
        CreateRealBlock("Jardinera", level, new Vector3(-20f, 4f, 14f), new Vector3(20f, 8f, 12f), brick);
        NoShadow(CreateRealBlock("Tierra de la jardinera", level, new Vector3(-20f, 8.02f, 14f), new Vector3(19.2f, 0.04f, 11.2f), soil, false));
        for (int i = 0; i < 5; i++)
        {
            // Plantitas en la parte este de la jardinera (el camino va por el oeste).
            Vector3 foot = new Vector3(-15f + (i % 2) * 2.5f, 8f, 9.5f + i * 2.2f);
            GameObject sprout = CreateVisualPart("Plantita", level, PrimitiveType.Sphere, foot + Vector3.up * 1f, new Vector3(2f, 2f, 2f), Vector3.zero, real.Leaves);
            NoShadow(sprout);
            sprout.AddComponent<Sway>();
        }
        GameObject ladybug = new GameObject("Mariquita mecanica");
        ladybug.transform.SetParent(level, false);
        CreateVisualPart("Caparazon", ladybug.transform, PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.1f, 2.2f), Vector3.zero, home.Toys[0]);
        CreateVisualPart("Cabeza", ladybug.transform, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 1.1f), Vector3.one * 0.8f, Vector3.zero, home.Black);
        for (int s = 0; s < 4; s++)
        {
            NoShadow(CreateVisualPart("Mancha", ladybug.transform, PrimitiveType.Sphere, new Vector3(s % 2 == 0 ? -0.45f : 0.45f, 1.05f, s < 2 ? 0.35f : -0.45f), new Vector3(0.4f, 0.15f, 0.4f), Vector3.zero, home.Black));
        }
        ladybug.AddComponent<PathHazard>().Setup(new[] { new Vector3(-28f, 8f, 14f), new Vector3(-13f, 8f, 14f) }, false, 4f, 1.4f);

        // 3. Flores que crecen desde el césped: sus corolas son escalones hasta lo alto de la valla.
        Vector3[] heads = { new Vector3(-26f, 9.3f, 21.8f), new Vector3(-29f, 10.6f, 25f), new Vector3(-31.6f, 11.9f, 28.4f), new Vector3(-31.8f, 13.1f, 32f) };
        GameObject flowers = new GameObject("Flores gigantes");
        Transform[] flowerPivots = new Transform[heads.Length];
        for (int i = 0; i < heads.Length; i++)
        {
            Transform pivot = CreatePivot("Flor " + (i + 1), flowers.transform, Vector3.zero);
            pivot.position = new Vector3(heads[i].x, 0f, heads[i].z);
            CreateVisualPart("Tallo", pivot, PrimitiveType.Cylinder, new Vector3(0f, heads[i].y / 2f, 0f), new Vector3(0.5f, heads[i].y / 2f, 0.5f), Vector3.zero, stem);
            // Corola redonda; el BoxCollider que Unity ajusta a su malla la hace pisable.
            GameObject corolla = CreateVisualPart("Corola", pivot, PrimitiveType.Cylinder, new Vector3(0f, heads[i].y - 0.2f, 0f), new Vector3(3.2f, 0.2f, 3.2f), Vector3.zero, pollen);
            corolla.AddComponent<BoxCollider>();
            for (int p = 0; p < 8; p++)
            {
                float angle = p * Mathf.PI / 4f;
                NoShadow(CreateVisualPart("Petalo", pivot, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 2.2f, heads[i].y - 0.3f, Mathf.Sin(angle) * 2.2f), new Vector3(1.6f, 0.15f, 0.9f), new Vector3(0f, -angle * Mathf.Rad2Deg, 0f), petals));
            }
            flowerPivots[i] = pivot;
        }
        CrystalGrow flowerGrow = flowers.AddComponent<CrystalGrow>();
        flowerGrow.SetCrystals(flowerPivots);
        SetEventId(flowerGrow, "flores_3");

        // ================= VALLA =================
        CreateRealBlock("Valla", level, new Vector3(-35.2f, 7f, 33f), new Vector3(1.6f, 14f, 50f), home.Wood);
        for (float z = 9f; z < 58f; z += 2.2f)
        {
            NoShadow(CreateRealBlock("Tabla de la valla", level, new Vector3(-34.35f, 6.6f, z), new Vector3(0.1f, 13.2f, 2f), home.DarkWood, false));
        }
        GameObject bird = new GameObject("Pajaro de la valla");
        bird.transform.SetParent(level, false);
        Material feathers = GetOrCreateMaterial("JardinPlumas", new Color(0.12f, 0.12f, 0.15f), null, 0f, 0.4f);
        CreateVisualPart("Cuerpo", bird.transform, PrimitiveType.Sphere, new Vector3(0f, 0.7f, 0f), new Vector3(1f, 1f, 1.6f), Vector3.zero, feathers);
        CreateVisualPart("Pico", bird.transform, PrimitiveType.Cube, new Vector3(0f, 0.9f, 1f), new Vector3(0.2f, 0.2f, 0.6f), new Vector3(0f, 45f, 0f), home.Toys[2]);
        for (int w = 0; w < 2; w++)
        {
            CreateVisualPart("Ala", bird.transform, PrimitiveType.Cube, new Vector3(w == 0 ? -0.7f : 0.7f, 0.85f, 0f), new Vector3(0.9f, 0.08f, 1f), new Vector3(0f, 0f, w == 0 ? 25f : -25f), feathers);
        }
        bird.AddComponent<PathHazard>().Setup(new[] { new Vector3(-35.2f, 14.3f, 20f), new Vector3(-35.2f, 14.3f, 52f) }, false, 5f, 1.2f);

        // ================= ROBLE =================
        Vector3 trunkBase = new Vector3(0f, -0.3f, 62f);
        MeshBuilder oak = new MeshBuilder(), canopy = new MeshBuilder();
        AddTrunk(oak, Vector3.zero, Vector3.up, 46f, 4.5f, 2.2f, 16, 0.02f, rng);
        AddTrunk(oak, new Vector3(-3f, 28.4f, -1f), new Vector3(-16f, 0f, -6f), 17f, 1.3f, 0.6f, 10, 0.03f, rng);   // rama del columpio
        Vector3[] steps = { new Vector3(-13f, 25.3f, 58f), new Vector3(-16f, 26.6f, 61.5f), new Vector3(-17f, 27.9f, 65.5f), new Vector3(-16.5f, 29.2f, 69.5f), new Vector3(-14f, 30.5f, 72f) };
        foreach (Vector3 step in steps)
        {
            Vector3 from = new Vector3(-2.5f, step.y - 2f, 0f);
            Vector3 to = step - trunkBase + Vector3.down * 0.6f;
            AddTrunk(oak, from, to - from, (to - from).magnitude + 1f, 1f, 0.6f, 8, 0.02f, rng);
        }
        for (int i = 0; i < 9; i++)
        {
            float angle = i * Mathf.PI * 2f / 9f;
            AddBlob(canopy, new Vector3(Mathf.Cos(angle) * 16f, 44f + (i % 3) * 4f, Mathf.Sin(angle) * 16f), new Vector3(11f, 7f, 11f), 0.3f, 700f + i * 13f, 8);
        }
        AddBlob(canopy, new Vector3(0f, 54f, 0f), new Vector3(16f, 9f, 16f), 0.3f, 790f, 9);
        GameObject oakObject = CreateMeshObject("Roble", level, SaveMesh(oak.ToMesh(false), "RobleGigante"), trunkBase, false, bark);
        CapsuleCollider trunkCollider = oakObject.AddComponent<CapsuleCollider>();
        trunkCollider.radius = 3.6f;
        trunkCollider.height = 46f;
        trunkCollider.center = Vector3.up * 23f;
        NoShadow(CreateMeshObject("Copa del roble", level, SaveMesh(canopy.ToMesh(true), "CopaRobleGigante"), trunkBase, false, real.Leaves));

        // Plataformas en las ramas: rama baja (14 m, llega el columpio), rama media (24 m) y cinco ramas en espiral.
        CreateRealBlock("Rama baja", level, new Vector3(-7f, 13.7f, 57f), new Vector3(6f, 0.6f, 6f), home.Wood);
        CreateRealBlock("Rama media", level, new Vector3(-9.1f, 23.7f, 56f), new Vector3(5.8f, 0.6f, 8f), home.Wood);
        for (int i = 0; i < steps.Length; i++)
        {
            CreateRealBlock("Rama " + (i + 1), level, steps[i] + Vector3.down * 0.3f, new Vector3(3.2f, 0.6f, 3.2f), home.Wood);
            NoShadow(CreateVisualPart("Hojas", level, PrimitiveType.Sphere, steps[i] + new Vector3(-2f, 2.5f, 0f), new Vector3(4f, 2.4f, 4f), Vector3.zero, real.Leaves));
        }
        CreateRealBlock("Tablon del tronco", level, new Vector3(-3.8f, 19.5f, 59.5f), new Vector3(0.4f, 11f, 3f), home.Wood);

        // Casita del árbol: plataforma de 24 x 20 m a 32 m, barandas, cabaña y tejado.
        CreateRealBlock("Suelo de la casita", level, new Vector3(0f, 31.7f, 74f), new Vector3(24f, 0.6f, 20f), home.Wood);
        CreateRealBlock("Baranda este", level, new Vector3(11.8f, 32.7f, 74f), new Vector3(0.3f, 1.4f, 20f), home.DarkWood);
        CreateRealBlock("Baranda norte", level, new Vector3(0f, 32.7f, 83.8f), new Vector3(24f, 1.4f, 0.3f), home.DarkWood);
        CreateRealBlock("Baranda oeste", level, new Vector3(-11.8f, 32.7f, 79f), new Vector3(0.3f, 1.4f, 10f), home.DarkWood);
        for (int i = 0; i < 4; i++)
        {
            Vector3 post = new Vector3(i % 2 == 0 ? -11.4f : 11.4f, 15.7f, i < 2 ? 64.6f : 83.4f);
            CreateRealBlock("Poste de la casita", level, post, new Vector3(0.8f, 31.4f, 0.8f), home.DarkWood);
        }
        Transform cabin = CreatePivot("Cabana", level, new Vector3(8f, 32f, 80f));
        CreateRealBlock("Pared de la cabana sur", cabin, new Vector3(8f, 34.5f, 76.2f), new Vector3(7.6f, 5f, 0.3f), home.Wood);
        CreateRealBlock("Pared de la cabana oeste", cabin, new Vector3(4.2f, 34.5f, 80f), new Vector3(0.3f, 5f, 7.6f), home.Wood);
        CreateMeshObject("Tejado de la cabana", cabin, BuildGableRoofMesh(8f, 8f, 3f, 0.6f), new Vector3(8f, 37f, 80f), false, home.Toys[0]);
        NoShadow(CreateRealBlock("Ventanuco", cabin, new Vector3(8f, 35f, 76.05f), new Vector3(2f, 1.6f, 0.1f), GetOrCreateMaterial("JardinVentanaEncendida", new Color(1f, 0.8f, 0.5f), new Color(1f, 0.7f, 0.4f), 1.2f, 0.5f), false));

        // ================= COLUMPIO (evento 4) =================
        GameObject swing = new GameObject("Columpio");
        swing.transform.position = SwingPivot;
        Transform ropes = CreatePivot("Cuerdas", swing.transform, Vector3.zero);
        for (int r = 0; r < 2; r++)
        {
            NoShadow(CreateVisualPart("Cuerda", ropes, PrimitiveType.Cylinder, new Vector3(0f, -SwingLength / 2f, r == 0 ? -1.1f : 1.1f), new Vector3(0.15f, SwingLength / 2f, 0.15f), Vector3.zero, rope));
        }
        GameObject seat = CreateRealBlock("Asiento del columpio", null, SwingPivot + Vector3.down * SwingLength, new Vector3(4f, 0.5f, 2.6f), home.Wood);
        Rigidbody seatBody = seat.AddComponent<Rigidbody>();
        seatBody.isKinematic = true;
        SwingPlatform swingPlatform = swing.AddComponent<SwingPlatform>();
        swingPlatform.Setup(seatBody, ropes, SwingLength, SwingAmplitude, 7f);
        SetEventId(swingPlatform, "columpio_4");

        // 5. Farolillos del roble (se encienden con su fragmento).
        GameObject lanterns = new GameObject("Farolillos del roble");
        lanterns.transform.SetParent(level, false);
        Vector3[] lanternSpots = { new Vector3(-9f, 19f, 55f), new Vector3(-12f, 28f, 60f), new Vector3(-15f, 31f, 67f), new Vector3(-5f, 36f, 66f), new Vector3(6f, 37f, 70f) };
        foreach (Vector3 spot in lanternSpots)
        {
            NoShadow(CreateVisualPart("Farolillo", lanterns.transform, PrimitiveType.Sphere, spot, new Vector3(0.9f, 1.1f, 0.9f), Vector3.zero, real.Flame));
            CreateZoneLight(lanterns.transform, spot, new Color(1f, 0.75f, 0.45f), 12f, 2.5f);
        }
        GameObject lanternEvent = CreateActivator("Farolillos que se encienden", "farolillos_5", lanterns);

        // 6. Escalera de cuerda en el tronco: de la rama baja (14) a la media (24). Con ella, sube la Sombra.
        GameObject ropeLadder = CreateClimb(level, "Escalera de cuerda", new Vector3(-4f, 14f, 59.5f), 10f, Vector3.left, true, real);
        GameObject ropeLadderEvent = CreateActivator("Escalera de cuerda que baja", "despertar_6", ropeLadder);

        // 7. Núcleo y portal en la casita del árbol.
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(0f, 32f, 74f)));

        ConfigureEvent(trampolineEvent, trampoline.transform, real.Flame);
        ConfigureEvent(rake, rakeBody.transform, real.Flame);
        ConfigureEvent(flowers, flowerPivots[2], real.Flame);
        ConfigureEvent(swing, seat.transform, real.Flame);
        ConfigureEvent(lanternEvent, lanterns.transform.GetChild(1), real.Flame);
        ConfigureEvent(ropeLadderEvent, ropeLadder.transform, real.Flame);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // ================= ESTANQUE =================
        CreateWaterSheet(level, "Estanque", new Vector3(30f, 0.06f, 5f), new Vector2(28f, 30f), new Vector2(0.02f, 0.01f), real);
        GameObject pondKill = new GameObject("Estanque (ahoga)");
        pondKill.transform.SetParent(level, false);
        pondKill.transform.position = new Vector3(30f, -0.3f, 5f);
        BoxCollider pondBox = pondKill.AddComponent<BoxCollider>();
        pondBox.size = new Vector3(27f, 1f, 29f);
        pondBox.isTrigger = true;
        pondKill.AddComponent<KillZone>();
        Material pad = GetOrCreateMaterial("LagNenufar", new Color(0.2f, 0.5f, 0.3f), null, 0f, 0.3f);
        for (int i = 0; i < 12; i++)
        {
            NoShadow(CreateVisualPart("Nenufar", level, PrimitiveType.Cylinder, new Vector3(Mathf.Lerp(18f, 42f, (float)rng.NextDouble()), 0.1f, Mathf.Lerp(-8f, 18f, (float)rng.NextDouble())), new Vector3(2.4f, 0.02f, 2.4f), Vector3.zero, pad));
        }
        Transform boatOrbit = CreatePivot("Barquito", level, new Vector3(30f, 0.1f, 5f));
        GameObject boat = CreateVisualPart("Casco", boatOrbit, PrimitiveType.Cube, new Vector3(7f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 3f), Vector3.zero, home.Toys[0]);
        CreateVisualPart("Vela", boat.transform, PrimitiveType.Cube, new Vector3(0f, 2f, 0f), new Vector3(0.05f, 3f, 0.6f), Vector3.zero, home.Paper);
        boatOrbit.gameObject.AddComponent<Spinner>();
        SerializedObject boatSpin = new SerializedObject(boatOrbit.GetComponent<Spinner>());
        Property(boatSpin, "axis").vector3Value = Vector3.up;
        Property(boatSpin, "degreesPerSecond").floatValue = 8f;
        boatSpin.ApplyModifiedPropertiesWithoutUndo();

        // ================= SENDERO, HIERBA, FLORES Y FONDO =================
        Vector2[] route = { new Vector2(0f, -34f), new Vector2(-6f, -22f), new Vector2(-12f, -12f), new Vector2(-24f, -10f) };
        NoShadow(CreateMeshObject("Sendero de tierra", level, SaveMesh(BuildRibbonMesh(route, 4f, (x, z) => 0f, 0.03f), "SenderoJardin"), Vector3.zero, false, soil));
        for (int i = 0; i < 9; i++)
        {
            // Losas redondas de piedra a lo largo del sendero.
            Vector2 point = Vector2.Lerp(route[0], route[2], i / 8f);
            NoShadow(CreateVisualPart("Losa del sendero", level, PrimitiveType.Cylinder, new Vector3(point.x + (i % 2 - 0.5f) * 0.8f, 0.05f, point.y), new Vector3(2.4f, 0.04f, 2.2f), Vector3.zero, real.Rock));
        }
        CreateGrassPatch(level, "Hierba alta", Rect.MinMaxRect(-70f, -32f, 70f, 100f), 9000, 2.6f, (x, z) => 0f,
            (x, z) => DistanceToPath(route, x, z) > 3f && !(x > 15f && x < 45f && z > -11f && z < 21f) && !(Mathf.Abs(x + 20f) < 11f && z > -13f && z < 21f) && Vector2.Distance(new Vector2(x, z), new Vector2(2f, -16f)) > 1.5f,
            real.GrassBlades, 100, false);
        for (int i = 0; i < 26; i++)
        {
            Vector3 foot = new Vector3(Mathf.Lerp(-60f, 60f, (float)rng.NextDouble()), 0f, Mathf.Lerp(-28f, 95f, (float)rng.NextDouble()));
            if (DistanceToPath(route, foot.x, foot.z) < 5f || (foot.x < -8f && foot.x > -38f && foot.z > -14f && foot.z < 60f) || (foot.x > 15f && foot.x < 45f && foot.z > -11f && foot.z < 21f))
            {
                continue;
            }

            float tall = 6f + (float)rng.NextDouble() * 8f;
            CreateVisualPart("Tallo de flor", level, PrimitiveType.Cylinder, foot + Vector3.up * tall / 2f, new Vector3(0.4f, tall / 2f, 0.4f), Vector3.zero, stem);
            Transform bloom = CreatePivot("Flor", level, foot + Vector3.up * tall);
            NoShadow(CreateVisualPart("Centro", bloom, PrimitiveType.Sphere, Vector3.zero, new Vector3(1.4f, 0.6f, 1.4f), Vector3.zero, pollen));
            Material color = i % 3 == 0 ? petals : home.Toys[i % 3 == 1 ? 0 : 2];
            for (int p = 0; p < 6; p++)
            {
                float angle = p * Mathf.PI / 3f;
                NoShadow(CreateVisualPart("Petalo", bloom, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 1.4f, 0f, Mathf.Sin(angle) * 1.4f), new Vector3(1.4f, 0.12f, 0.8f), new Vector3(0f, -angle * Mathf.Rad2Deg, 0f), color));
            }
            bloom.gameObject.AddComponent<Sway>();
        }

        // Setos y árboles lejanos alrededor del jardín.
        TreeModel[] farOaks = CreateTreeModels("Roble", 1, 3);
        for (int i = 0; i < 18; i++)
        {
            float angle = Mathf.Lerp(-0.2f, Mathf.PI + 0.2f, i / 17f);
            Vector3 foot = new Vector3(Mathf.Cos(angle) * 110f, -0.3f, 30f + Mathf.Sin(angle) * 110f);
            PlaceTree(level, farOaks, foot, 4f + (float)rng.NextDouble() * 2f, real.Bark, real.Leaves, rng, false);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            NoShadow(CreateRealBlock("Seto", level, new Vector3(side * 76f, 6f, 25f), new Vector3(6f, 12f, 140f), real.Leaves, false));
        }
        NoShadow(CreateRealBlock("Seto del fondo", level, new Vector3(0f, 6f, 96f), new Vector3(152f, 12f, 6f), real.Leaves, false));

        // ================= LUCES BAJAS Y SOMBRA DEL OLVIDO =================
        Light[] lowLights =
        {
            CreateDimmableLight(level, new Vector3(-4f, 1.5f, -24f), new Color(1f, 0.85f, 0.55f), 14f, 2.5f),
            CreateDimmableLight(level, new Vector3(-20f, 8f, -2f), new Color(1f, 0.85f, 0.55f), 14f, 2f),
            CreateDimmableLight(level, new Vector3(-20f, 10f, 16f), new Color(1f, 0.85f, 0.55f), 14f, 2f),
            CreateDimmableLight(level, new Vector3(-33f, 16f, 34f), new Color(1f, 0.85f, 0.55f), 14f, 2f),
            CreateDimmableLight(level, new Vector3(-8f, 16f, 56f), new Color(1f, 0.85f, 0.55f), 14f, 2.2f),
        };
        GameObject shadow = new GameObject("Sombra del Olvido");
        shadow.transform.SetParent(level, false);
        shadow.transform.position = new Vector3(0f, -0.4f, 20f);
        Material dark = GetOrCreateMaterial("CasaSombraDelOlvido", new Color(0.01f, 0.005f, 0.02f), new Color(0.25f, 0.05f, 0.45f), 0.6f, 0.95f);
        NoShadow(CreateRealBlock("Lamina", shadow.transform, new Vector3(0f, -0.45f, 20f), new Vector3(170f, 0.1f, 170f), dark, false));
        CreateMotes("Bruma de la sombra", shadow.transform, new Vector3(0f, 0.5f, 20f), new Vector3(160f, 1.5f, 160f), materials.Star, new Color(0.5f, 0.2f, 0.9f, 0.6f), 300, 60f, 0.35f, -0.1f);
        shadow.AddComponent<RisingShadow>().Setup("despertar_6", 1.2f, 29f, lowLights);

        // ================= AMANECER FINAL =================
        Material sunMaterial = GetOrCreateMaterial("CasaSolNaciente", new Color(1f, 0.75f, 0.45f), new Color(1f, 0.65f, 0.35f), 4f, 0.2f);
        CreateSkyDisc(level, "Sol naciente", new Vector3(60f, 30f, 260f), 40f, sunMaterial);
        GameObject dawnGlow = new GameObject("Luz del amanecer");
        dawnGlow.transform.SetParent(level, false);
        dawnGlow.transform.position = new Vector3(0f, 45f, 90f);
        Light glow = dawnGlow.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.8f, 0.5f);
        glow.range = 90f;
        glow.intensity = 4f;
        new GameObject("Amanecer").AddComponent<DawnFinale>().Setup(RenderSettings.sun, new[] { dawnGlow });

        // ================= LAMPARITAS (refugio y reaparición) =================
        CreateBeacon(level, "Farol del porche", new Vector3(8f, 1.4f, -38f), 5f, true, "", true, real, materials);
        CreateBeacon(level, "Farolillo de la mesa", new Vector3(-28.5f, 6f, -5.5f), 3f, false, "", true, real, materials);
        CreateBeacon(level, "Farolillo de la rama baja", new Vector3(-9.5f, 14f, 55f), 3f, false, "", true, real, materials);

        CreateMotes("Polen del amanecer", level, new Vector3(-10f, 12f, 30f), new Vector3(70f, 24f, 90f), materials.Star, new Color(1.6f, 1.4f, 0.9f, 0.5f), 300, 25f, 0.08f, 0.005f);

        // ================= CAMINO (para las pruebas) =================
        CreateFlatPathMarkers(level, new[]
        {
            new[] { new Vector3(0f, 1.4f, -40f), new Vector3(0f, 1.4f, -35f) },
            new[] { new Vector3(0f, 0f, -31f), new Vector3(-6f, 0f, -22f), new Vector3(-14f, 0f, -14f) },
            new[] { new Vector3(-22f, 3.6f, -11f), new Vector3(-22f, 3.6f, -8.6f) },
            new[] { new Vector3(-26f, 6f, -6f), new Vector3(-24f, 6f, 2.2f) },
            new[] { new Vector3(-24f, 8f, 12.5f), new Vector3(-26f, 8f, 19.2f) },
            new[] { new Vector3(-35.2f, 14f, 33f), new Vector3(-35.2f, 14f, 55f) },
            new[] { new Vector3(-9f, 14f, 55f), new Vector3(-6f, 14f, 58.5f) },
            new[] { new Vector3(-10f, 24f, 54f), new Vector3(-8f, 24f, 58f) },
            new[] { new Vector3(8f, 32f, 66f), new Vector3(8f, 32f, 74f) },
        });
    }

    // Aspersor: base, tubo y cabeza que gira con un chorro de agua a ras de suelo. El chorro mata (SweepingBeam):
    // se salta. Visualmente es agua, no un rayo rojo.
    private static void CreateSprinkler(Transform parent, string name, Vector3 floorPosition, float length, float degreesPerSecond, HomeMaterials home, Material water, Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;
        CreateVisualPart("Base", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(1.6f, 0.15f, 1.6f), Vector3.zero, home.Toys[3]);
        CreateVisualPart("Tubo", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 0.4f, 0.4f), Vector3.zero, home.Brass);

        GameObject pivot = new GameObject("Pivote");
        pivot.transform.SetParent(root.transform, false);
        NoShadow(CreateVisualPart("Chorro", pivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.45f, length / 2f), new Vector3(0.35f, 0.35f, length), Vector3.zero, water));
        NoShadow(CreateVisualPart("Cabeza", pivot.transform, PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(0.5f, 0.3f, 1f), Vector3.zero, home.Brass));
        CreateMotes("Gotas", pivot.transform, root.transform.position + new Vector3(0f, 0.6f, length * 0.6f), new Vector3(1.5f, 0.6f, length * 0.8f), materials.Star, new Color(1.4f, 1.6f, 1.9f, 0.5f), 120, 40f, 0.12f, 0.6f);

        root.AddComponent<SweepingBeam>().Setup(pivot.transform, length, degreesPerSecond);
    }

    // Luz baja: la Sombra del Olvido la apaga cuando la cubre.
    private static Light CreateDimmableLight(Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        GameObject lightObject = new GameObject("Luz baja");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return light;
    }

    // Final del juego: cambia la pantalla "Región restaurada" del HUD por el fundido a blanco y los créditos.
    private static void CreateFinalCredits(GameObject canvas)
    {
        Object.DestroyImmediate(canvas.GetComponent<EndScreen>());
        Transform oldPanel = canvas.transform.Find("Pantalla final");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        GameObject panelObject = new GameObject("Creditos finales", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        SetRect(panelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panelObject.GetComponent<Image>().color = new Color(1f, 0.97f, 0.9f, 1f);

        TextMeshProUGUI line = CreateText("Linea", panelObject.transform, font, 64f, TextAlignmentOptions.Center);
        SetRect(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1600f, 400f));
        line.color = new Color(0.22f, 0.17f, 0.12f);
        line.text = "";

        TextMeshProUGUI hint = CreateText("Volver", panelObject.transform, font, 34f, TextAlignmentOptions.Center);
        SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1600f, 80f));
        hint.color = new Color(0.4f, 0.33f, 0.25f);
        hint.text = "Pulsa Enter para volver al principio";

        canvas.AddComponent<FinalCredits>().Setup(panelObject.GetComponent<CanvasGroup>(), line, hint, new[]
        {
            "Kael abre los ojos.",
            "La luz entra por la ventana.\nEs por la mañana.",
            "Cada fragmento era un recuerdo.\nCada región, un paso de vuelta a casa.",
            "Ya no quedan sombras que restaurar.",
            "Un juego del curso Desarrollo de Videojuegos\n\n(equipo: cambia estos nombres en FinalCredits)",
            "Gracias por jugar.",
        });
    }
}
