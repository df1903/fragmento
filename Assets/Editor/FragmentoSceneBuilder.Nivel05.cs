using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 5, "Lago y cascadas" (Nivel05_Lago.unity): mesetas de roca que nacen de un lago turquesa, acantilados
///   con cascadas, montañas lejanas y un amanecer cálido. Kael ya es una silueta humanoide con túnica, bufanda y capa.
///
/// POR QUÉ EXISTE
///   Quinto paso del arco de materialización: aparece el agua y la luz de un día real. Su identidad de juego es el
///   AGUA EN MOVIMIENTO: balsas que cruzan el lago, una marea que sube y baja (las piedras bajas solo se cruzan con
///   marea baja) y un cauce con corriente que empuja hacia los bordes.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 5 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: Orilla > (balsa) > Islote > (piedras de cruce, al este) > Remanso > (puente) > Presa > Cauce con
///   corriente > (balsa, al oeste) > Poza > (piedras de la cascada) > Cumbre.
///   Fragmentos: 1 balsa del embarcadero, 2 piedras de cruce, 3 puente de roca, 4 compuerta de la presa,
///   5 balsa del río, 6 piedras de la cascada, 7 núcleo. Caer al agua cuenta como muerte.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel05ScenePath = "Assets/Scenes/Nivel05_Lago.unity";

    // Altura del agua con marea baja y cuánto sube. Las mesetas están en y = 1.5 o más: nunca se inundan.
    private const float LagoLowTideY = -1.2f;
    private const float LagoTideRise = 2f;

    [MenuItem("Fragmento/Construir Nivel 5")]
    public static void BuildNivel5()
    {
        BuildThemedLevel(CreateNivel5Theme);
    }

    private static LevelTheme CreateNivel5Theme(Materials materials)
    {
        materials.Stone = GetOrCreateMaterial("LagPiedra", new Color(0.2f, 0.22f, 0.24f), null, 0f, 0.35f);
        materials.StoneTop = GetOrCreateMaterial("LagRoca", new Color(0.46f, 0.48f, 0.47f), null, 0f, 0.3f);
        materials.Ruin = GetOrCreateMaterial("LagRocaOscura", new Color(0.3f, 0.31f, 0.33f), null, 0f, 0.3f);
        materials.Edge = GetOrCreateMaterial("LagBorde", new Color(0.4f, 0.8f, 0.85f), new Color(0.5f, 1f, 1f), 0.8f, 0.4f);
        materials.PlatformOff = GetOrCreateMaterial("LagPlataformaApagada", new Color(0.2f, 0.22f, 0.24f), new Color(0.3f, 0.9f, 1f), 0.25f, 0.3f);
        Material growth = GetOrCreateMaterial("LagPiedraCruce", new Color(0.5f, 0.58f, 0.58f), new Color(0.3f, 0.9f, 1f), 1.1f, 0.45f);
        materials.PlatformOn = growth;
        Material foam = GetOrCreateMaterial("LagEspuma", new Color(0.9f, 0.97f, 1f), new Color(0.6f, 0.8f, 1f), 1f, 0.5f);

        return new LevelTheme
        {
            ScenePath = Nivel05ScenePath,
            VolumePath = SettingsFolder + "/Nivel05_PerfilVolumen.asset",
            NextScenePath = Nivel06ScenePath,
            FilePrefix = "N5_",
            KaelPrefabName = "Kael_Nivel5",
            KaelStage = 5,
            KaelRings = false,
            FragmentFiles = new[] { "Orilla", "Corriente", "Remanso", "Presa", "Cascada", "Cumbre", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento de la Orilla", "Fragmento de la Corriente", "Fragmento del Remanso", "Fragmento de la Presa",
                "Fragmento de la Cascada", "Fragmento de la Cumbre", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "La balsa del embarcadero despierta y cruza hacia el islote.",
                "Piedras de cruce emergen del agua. Cuidado: la marea las cubre.",
                "Un puente de roca sube del remanso a la presa.",
                "La compuerta de la presa se hunde: el cauce está libre.",
                "Una balsa despierta al final del cauce y navega hacia la poza.",
                "Piedras emergen junto a la cascada y suben hacia la cumbre.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            EventIds = new[] { "balsa_1", "cristal_2", "puente_3", "puerta_4", "balsa_5", "cristal_6", "nucleo_7" },
            Growth = growth,
            Accent = foam,
            Water = GetOrCreateTransparentMaterial("LagAgua", "Universal Render Pipeline/Lit", new Color(0.08f, 0.4f, 0.5f, 0.75f), new Color(0.05f, 0.35f, 0.45f), 0.6f, false),
            Background = new Color(0.8f, 0.62f, 0.6f),
            FogColor = new Color(0.8f, 0.62f, 0.6f),
            FogDensity = 0.006f,
            SunColor = new Color(1f, 0.72f, 0.5f),
            SunIntensity = 1.5f,
            SunEuler = new Vector3(18f, -40f, 0f),
            AmbientSky = new Color(0.55f, 0.62f, 0.8f),
            AmbientEquator = new Color(0.5f, 0.45f, 0.5f),
            AmbientGround = new Color(0.2f, 0.25f, 0.3f),
            ZoneLightColor = new Color(0.7f, 0.95f, 1f),
            VolumeFilter = new Color(1f, 0.97f, 0.95f),
            ChainBaseY = -9f,
            KillY = -5f,
            FloorY = -9f,
            FloorMaterial = GetOrCreateMaterial("LagLecho", new Color(0.12f, 0.14f, 0.13f), null, 0f, 0.2f),
            Stars = false,
            Spawn = new Vector3(0f, 1.55f, -8f),
            Zones = new[]
            {
                new ZoneInfo("Orilla", 0f, 1.5f, 0f, 30f, 26f, 3f),           // z -13..13
                new ZoneInfo("Islote", 0f, 1.5f, 50f, 20f, 18f, 3f),          // x -10..10, z 41..59
                new ZoneInfo("Remanso", 46f, 1.5f, 50f, 20f, 22f, 3f),        // x 36..56, z 39..61
                new ZoneInfo("Presa", 46f, 4f, 88f, 24f, 24f, 6f),            // x 34..58, z 76..100
                new ZoneInfo("Cauce", 46f, 4f, 120f, 8f, 40f, 6f, true),      // x 42..50, z 100..140 (corriente)
                new ZoneInfo("Poza", 2f, 4f, 136f, 24f, 22f, 6f),             // x -10..14, z 125..147
                new ZoneInfo("Cumbre", 2f, 9f, 173f, 24f, 20f, 6f),           // z 163..183 (núcleo y portal)
            },
            KeepOut = new[]
            {
                Rect.MinMaxRect(-4f, 13f, 4f, 41f),          // balsa del embarcadero
                Rect.MinMaxRect(10f, 46f, 36f, 54f),         // piedras de cruce
                Rect.MinMaxRect(43f, 61f, 49f, 76f),         // puente de roca
                Rect.MinMaxRect(14f, 132f, 42f, 140f),       // balsa del río
                Rect.MinMaxRect(-1f, 147f, 5f, 163f),        // piedras de la cascada
                Rect.MinMaxRect(12f, 25f, 18f, 31f),         // roca de la torreta A
                Rect.MinMaxRect(57f, 123f, 63f, 129f),       // roca de la torreta C
            },
            BuildLayout = BuildLagoLayout,
            Decorate = DecorateLago,
        };
    }

    private static void BuildLagoLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        LevelTheme theme = context.Theme;
        RouteBuilder route = context.Route;

        // --- Marea: el agua sube y baja. Con marea alta cubre las piedras bajas (cara superior en y = 0.2). ---
        CreateTide(level, new Vector3(0f, LagoLowTideY, 120f), LagoTideRise, theme.Water);

        // --- Fragmentos ---
        context.Fragment(1, new Vector3(-8f, 2.8f, 4f));        // Orilla
        context.Fragment(2, new Vector3(5f, 2.8f, 52f));        // Islote
        context.Fragment(3, new Vector3(51f, 2.8f, 56f));       // Remanso
        context.Fragment(4, new Vector3(40f, 5.3f, 82f));       // Presa, delante de la compuerta
        context.Fragment(5, new Vector3(46f, 5.3f, 121f));      // Cauce, entre las dos corrientes
        context.Fragment(6, new Vector3(-4f, 5.3f, 140f));      // Poza
        context.Fragment(7, new Vector3(2f, 10.3f, 168f));      // Cumbre

        // --- 1. Balsa del embarcadero: de la Orilla al Islote (22 m). Una torreta la vigila desde una roca. ---
        GameObject raft1 = CreateRaft("Balsa del embarcadero", "balsa_1", new Vector3(0f, 1.2f, 16f), new Vector3(0f, 0f, 22f), 5f, false, materials);
        CreateBlock("Roca de la torreta A", level, new Vector3(15f, -0.5f, 28f), new Vector3(4f, 4f, 4f), materials.StoneTop);
        CreateCrystalTurret("Torreta del embarcadero", level, new Vector3(15f, 1.5f, 28f), 1, 3.2f, context.Projectile, materials, context.Danger);

        // --- 2. Piedras de cruce: del Islote hacia el este, hasta el Remanso. Las bajas (y 0.2) se inundan con la
        //        marea alta; la "Roca segura" del medio (y 1) y la última piedra (y 0.8) no. ---
        GameObject chain2 = CreateCrystalChain("Piedras de cruce", "cristal_2", theme.Growth, theme.ChainBaseY, 3.2f, 3.2f,
            new[]
            {
                new Vector3(12.5f, 0.2f, 50f), new Vector3(16.5f, 0.2f, 50f), new Vector3(20.5f, 0.2f, 50f),
                new Vector3(29.5f, 0.2f, 50f), new Vector3(33.5f, 0.8f, 50f),
            });
        CreateBlock("Roca segura", level, new Vector3(25f, -0.5f, 50f), new Vector3(4f, 3f, 4f), materials.StoneTop);
        route.Jump(new Vector3(9.7f, 1.5f, 50f), new Vector3(11.2f, 0.2f, 50f));
        route.Jump(new Vector3(13.8f, 0.2f, 50f), new Vector3(15.2f, 0.2f, 50f));
        route.Jump(new Vector3(17.8f, 0.2f, 50f), new Vector3(19.2f, 0.2f, 50f));
        route.Jump(new Vector3(21.8f, 0.2f, 50f), new Vector3(23.3f, 1f, 50f));
        route.Jump(new Vector3(26.7f, 1f, 50f), new Vector3(28.2f, 0.2f, 50f));
        route.Jump(new Vector3(30.8f, 0.2f, 50f), new Vector3(32.2f, 0.8f, 50f));
        route.Jump(new Vector3(34.8f, 0.8f, 50f), new Vector3(36.3f, 1.5f, 50f));

        // --- 3. Puente de roca: del Remanso (y 1.5) sube al norte hasta la Presa (y 4) ---
        GameObject bridge3 = CreateBridge("Puente de roca", new Vector3(46f, 1.5f, 60.9f), new Vector3(46f, 4f, 76.1f), 4f, "puente_3", theme.Growth);

        // --- 4. Compuerta de la presa: muro que cruza la Presa. Detrás empieza el Cauce. ---
        GameObject door4 = CreateGateWall(level, "Compuerta de la presa", "puerta_4", 34f, 58f, 46f, 4f, 90f, materials);
        CreateBlock("Cobertura de la presa", level, new Vector3(46f, 5.5f, 81f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateCrystalTurret("Torreta de la presa", level, new Vector3(55f, 4f, 84f), 1, 2.8f, context.Projectile, materials, context.Danger);

        // --- Cauce: pasarela de 8 m de ancho con dos corrientes. La primera empuja al este; la segunda, al oeste.
        //     Las rocas de los bordes sirven para frenar y descansar. ---
        Color flowColor = new Color(1.4f, 1.8f, 2f, 0.7f);
        CreatePushZone(level, "Corriente este", new Vector3(46f, 6.5f, 110.5f), new Vector3(8f, 5f, 17f), new Vector3(3.5f, 0f, 0f), materials.Star, flowColor);
        CreatePushZone(level, "Corriente oeste", new Vector3(46f, 6.5f, 127.5f), new Vector3(8f, 5f, 9f), new Vector3(-3.5f, 0f, 0f), materials.Star, flowColor);
        CreateBlock("Rompeolas 1", level, new Vector3(49.2f, 4.8f, 106f), Vector3.one * 1.6f, materials.Ruin);
        CreateBlock("Rompeolas 2", level, new Vector3(49.2f, 4.8f, 114f), Vector3.one * 1.6f, materials.Ruin);
        CreateBlock("Rompeolas 3", level, new Vector3(42.8f, 4.8f, 126f), Vector3.one * 1.6f, materials.Ruin);
        CreateBlock("Rompeolas 4", level, new Vector3(42.8f, 4.8f, 130f), Vector3.one * 1.6f, materials.Ruin);
        CreateBlock("Roca de la torreta C", level, new Vector3(60f, 1f, 126f), new Vector3(4f, 6f, 4f), materials.StoneTop);
        CreateCrystalTurret("Torreta del cauce", level, new Vector3(60f, 4f, 126f), 1, 3.4f, context.Projectile, materials, context.Danger);

        // --- 5. Balsa del río: del final del Cauce hacia el oeste, hasta la Poza (22 m) ---
        GameObject raft5 = CreateRaft("Balsa del rio", "balsa_5", new Vector3(39f, 3.7f, 136f), new Vector3(-22f, 0f, 0f), 5f, false, materials);

        // --- 6. Piedras de la cascada: de la Poza (y 4) a la Cumbre (y 9), escalones de 0.8 m ---
        GameObject chain6 = CreateCrystalChain("Piedras de la cascada", "cristal_6", theme.Growth, theme.ChainBaseY, 4f, 2.6f,
            new[]
            {
                new Vector3(2f, 4.8f, 148.9f), new Vector3(2f, 5.6f, 152.1f), new Vector3(2f, 6.4f, 155.3f),
                new Vector3(2f, 7.2f, 158.5f), new Vector3(2f, 8f, 161.7f),
            });
        route.Jump(new Vector3(2f, 4f, 146.7f), new Vector3(2f, 4.8f, 147.9f));
        route.Jump(new Vector3(2f, 4.8f, 149.9f), new Vector3(2f, 5.6f, 151.1f));
        route.Jump(new Vector3(2f, 5.6f, 153.1f), new Vector3(2f, 6.4f, 154.3f));
        route.Jump(new Vector3(2f, 6.4f, 156.3f), new Vector3(2f, 7.2f, 157.5f));
        route.Jump(new Vector3(2f, 7.2f, 159.5f), new Vector3(2f, 8f, 160.7f));
        route.Jump(new Vector3(2f, 8f, 162.7f), new Vector3(2f, 9f, 163.3f));

        // --- 7. Núcleo y portal en la Cumbre ---
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(2f, 9f, 173f)));

        ConfigureEvent(raft1, raft1.transform, theme.Growth);
        ConfigureEvent(chain2, chain2.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(bridge3, bridge3.transform, theme.Growth);
        ConfigureEvent(door4, door4.transform.Find("Losa"), materials.Door);
        ConfigureEvent(raft5, raft5.transform, theme.Growth);
        ConfigureEvent(chain6, chain6.transform.GetChild(2).GetChild(0), theme.Growth);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // --- Peligros: además de la marea y las tres torretas, dos sombras en tierra firme ---
        CreateRockShadow("Sombra del remanso", level, new Vector3(38f, 2.4f, 44f), new Vector3(54f, 2.4f, 44f), IslandArea(46f, 1.5f, 50f, 20f, 22f), materials, context.Danger);
        CreateRockShadow("Sombra de la poza", level, new Vector3(-6f, 4.9f, 130f), new Vector3(10f, 4.9f, 130f), IslandArea(2f, 4f, 136f, 24f, 22f), materials, context.Danger);

        // --- Checkpoints (se registran; la reaparición sigue siendo al inicio) ---
        CreateCheckpoint("Checkpoint islote", new Vector3(0f, 4.5f, 50f), new Vector3(18f, 8f, 16f), new Vector3(0f, 1.55f, 44f));
        CreateCheckpoint("Checkpoint remanso", new Vector3(46f, 4.5f, 50f), new Vector3(18f, 8f, 20f), new Vector3(38f, 1.55f, 50f));
        CreateCheckpoint("Checkpoint presa", new Vector3(46f, 7f, 83f), new Vector3(22f, 8f, 12f), new Vector3(46f, 4.05f, 78f));
        CreateCheckpoint("Checkpoint poza", new Vector3(2f, 7f, 136f), new Vector3(22f, 8f, 20f), new Vector3(11f, 4.05f, 136f));
    }

    private static void DecorateLago(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(5);
        Material waterfall = GetOrCreateTransparentMaterial("LagCascada", "Universal Render Pipeline/Lit", new Color(0.85f, 0.95f, 1f, 0.5f), new Color(0.5f, 0.8f, 1f), 0.7f, false);
        Material pad = GetOrCreateMaterial("LagNenufar", new Color(0.2f, 0.5f, 0.3f), null, 0f, 0.3f);
        Material mountain = GetOrCreateMaterial("LagMontana", new Color(0.4f, 0.45f, 0.6f), null, 0f, 0.1f);

        // Sol bajo y montañas lejanas (pirámides: cubos girados).
        Material sun = GetOrCreateMaterial("LagSol", new Color(1f, 0.85f, 0.6f), new Color(1f, 0.8f, 0.5f), 3f, 0.2f);
        CreateSkyDisc(level, "Sol", new Vector3(110f, 30f, 400f), 50f, sun);
        for (int i = 0; i < 7; i++)
        {
            float size = 60f + (float)rng.NextDouble() * 50f;
            Vector3 position = new Vector3(Mathf.Lerp(-220f, 220f, i / 6f), -15f, 420f + (float)rng.NextDouble() * 60f);
            GameObject peak = CreateVisualPart("Montana " + (i + 1), level, PrimitiveType.Cube, position, new Vector3(size, size, size), new Vector3(45f, 45f, 0f), mountain);
            NoShadow(peak);
        }

        // Acantilados a ambos lados (más allá del Remanso y la Presa) con cascadas.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -10f + i * 52f;
                float height = 46f + (float)rng.NextDouble() * 14f;
                GameObject cliff = CreateBlock("Acantilado " + (side < 0 ? "O" : "E") + " " + (i + 1), level, new Vector3(side * 84f, height / 2f - 9f, z),
                    new Vector3(26f, height, 54f), materials.Ruin);
                Object.DestroyImmediate(cliff.GetComponent<Collider>());
                NoShadow(cliff);

                if (i % 2 == 0)
                {
                    CreateWaterfall(level, "Cascada " + (side < 0 ? "O" : "E") + " " + (i + 1), new Vector3(side * 70.6f, height - 9f, z), height - 8f, 8f, waterfall, theme.Accent, materials.Star);
                }
            }
        }

        // Gran cascada tras la Cumbre: una pared de roca con una lámina de agua. Marca el destino desde lejos.
        GameObject wall = CreateBlock("Pared de la cascada", level, new Vector3(2f, 13f, 197f), new Vector3(70f, 46f, 20f), materials.Ruin);
        Object.DestroyImmediate(wall.GetComponent<Collider>());
        NoShadow(wall);
        NoShadow(CreateVisualPart("Gran cascada", level, PrimitiveType.Cube, new Vector3(2f, 17f, 186.6f), new Vector3(10f, 38f, 0.8f), Vector3.zero, waterfall));
        CreateMotes("Bruma de la gran cascada", level, new Vector3(2f, 0f, 185f), new Vector3(14f, 3f, 6f), materials.Star, new Color(1.4f, 1.6f, 1.8f, 0.3f), 80, 24f, 3f, 0.05f);

        // Rocas que asoman del agua, lejos de las balsas y de las piedras de cruce.
        for (int i = 0; i < 60; i++)
        {
            float x = Mathf.Lerp(-65f, 68f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-30f, 200f, (float)rng.NextDouble());
            if (IsClear(theme, x, z, 4f))
            {
                CreateRockPile(level, "Rocas del lago " + (i + 1), new Vector3(x, LagoLowTideY - 0.6f, z), 2.5f + (float)rng.NextDouble() * 3f, materials.StoneTop, rng);
            }
        }

        // Nenúfares: discos planos que flotan con la marea (son hijos del agua).
        Transform tide = level.Find("Marea");
        for (int i = 0; i < 90; i++)
        {
            float x = Mathf.Lerp(-60f, 65f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-25f, 190f, (float)rng.NextDouble());
            if (IsClear(theme, x, z, 1.5f))
            {
                float size = 1f + (float)rng.NextDouble() * 1.2f;
                NoShadow(CreateVisualPart("Nenufar " + (i + 1), tide, PrimitiveType.Cylinder, new Vector3(x, 0.03f, z - 120f), new Vector3(size, 0.02f, size), Vector3.zero, pad));
            }
        }

        // Mojones de piedra apilada en las esquinas de cada meseta.
        foreach (ZoneInfo zone in theme.Zones)
        {
            if (zone.Plain)
            {
                continue;
            }

            for (int corner = 0; corner < 4; corner += 3) // dos esquinas opuestas
            {
                float x = zone.X + (corner % 2 == 0 ? -1f : 1f) * (zone.Width / 2f - 1.2f);
                float z = zone.Z + (corner < 2 ? -1f : 1f) * (zone.Depth / 2f - 1.2f);
                if (!NearPath(theme, x, z, 2f))
                {
                    CreateRockPile(level, "Mojon " + zone.Name + " " + (corner + 1), new Vector3(x, zone.Y, z), 1.4f, materials.StoneTop, rng);
                }
            }
        }

        // Neblina de rocío sobre el agua.
        CreateMotes("Rocio", level, new Vector3(0f, 1f, 90f), new Vector3(140f, 2f, 240f), materials.Star, new Color(1.2f, 1.3f, 1.5f, 0.18f), 120, 8f, 5f, 0f);
    }

    // Cascada lateral: lámina translúcida desde el borde del acantilado hasta el agua, con espuma y bruma en la base. Solo visual.
    private static void CreateWaterfall(Transform parent, string name, Vector3 top, float height, float width, Material sheet, Material foam, Material particleMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        GameObject body = CreateVisualPart("Lamina", root.transform, PrimitiveType.Cube, top + Vector3.down * height / 2f, new Vector3(0.8f, height, width), Vector3.zero, sheet);
        NoShadow(body);
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = new Vector3(top.x, LagoLowTideY + 0.2f, top.z + (i - 1) * width * 0.3f);
            NoShadow(CreateVisualPart("Espuma " + (i + 1), root.transform, PrimitiveType.Sphere, position, new Vector3(width * 0.5f, 1.6f, width * 0.5f), Vector3.zero, foam));
        }

        CreateMotes("Bruma de cascada", root.transform, new Vector3(top.x, LagoLowTideY + 1.5f, top.z), new Vector3(width, 2f, width), particleMaterial, new Color(1.4f, 1.6f, 1.8f, 0.3f), 60, 20f, 3f, 0.05f);
    }
}
