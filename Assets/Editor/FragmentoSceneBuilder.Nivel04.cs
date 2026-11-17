using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 4, "Jardín petrificado" (Nivel04_Jardin.unity): un jardín de piedra con árboles, arbustos y flores
///   convertidos en roca, estatuas de jardineros, un invernadero, bruma verdosa y luciérnagas. Es de día, con cielo
///   cubierto: la primera luz "natural" del juego. Kael gana brazos y piernas de luz.
///
/// POR QUÉ EXISTE
///   Cuarto paso del arco de materialización: aparecen las primeras plantas (aún petrificadas) y las primeras figuras
///   humanas (las estatuas). Su identidad de juego es que es ABIERTO: una plaza central con tres ramas que se pueden
///   explorar en el orden que se quiera. Dinámicas propias: hongos de rebote, hojas de piedra que se derrumban y
///   zarzas que brotan por turnos.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 4 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Ramas desde la Plaza: OESTE (hojas que se derrumban > Claro oeste > hongos > Terraza alta > puente > Mirador),
///   ESTE (piedras con zarzas > Claro este) y NORTE (troncos > Invernadero > plataforma > Islote).
///   Fragmentos: 1 troncos al invernadero, 2 hongos de rebote, 3 puente de raíces, 4 compuerta del invernadero,
///   5 plataforma floral, 6 troncos a la colina, 7 núcleo (en la Colina).
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel04ScenePath = "Assets/Scenes/Nivel04_Jardin.unity";

    [MenuItem("Fragmento/Construir Nivel 4")]
    public static void BuildNivel4()
    {
        BuildThemedLevel(CreateNivel4Theme);
    }

    private static LevelTheme CreateNivel4Theme(Materials materials)
    {
        materials.Stone = GetOrCreateMaterial("JarPiedra", new Color(0.14f, 0.16f, 0.15f), null, 0f, 0.2f);
        materials.StoneTop = GetOrCreateMaterial("JarMusgo", new Color(0.25f, 0.36f, 0.24f), null, 0f, 0.15f);
        materials.Ruin = GetOrCreateMaterial("JarRoca", new Color(0.32f, 0.34f, 0.32f), null, 0f, 0.25f);
        materials.Edge = GetOrCreateMaterial("JarBorde", new Color(0.3f, 0.7f, 0.35f), new Color(0.4f, 1f, 0.5f), 1f, 0.3f);
        materials.PlatformOff = GetOrCreateMaterial("JarPlataformaApagada", new Color(0.18f, 0.2f, 0.18f), new Color(0.4f, 1f, 0.5f), 0.25f, 0.2f);
        Material growth = GetOrCreateMaterial("JarRaiz", new Color(0.4f, 0.55f, 0.28f), new Color(0.5f, 1f, 0.4f), 1.2f, 0.35f);
        materials.PlatformOn = growth;
        Material flower = GetOrCreateMaterial("JarFlor", new Color(0.9f, 0.5f, 0.7f), new Color(1f, 0.5f, 0.8f), 2f, 0.4f);

        return new LevelTheme
        {
            ScenePath = Nivel04ScenePath,
            VolumePath = SettingsFolder + "/Nivel04_PerfilVolumen.asset",
            NextScenePath = Nivel05ScenePath,
            FilePrefix = "N4_",
            KaelPrefabName = "Kael_Nivel4",
            KaelStage = 4,
            KaelRings = false,
            FragmentFiles = new[] { "Semilla", "Raiz", "Polen", "Espina", "Brote", "Copa", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento de la Semilla", "Fragmento de la Raíz", "Fragmento del Polen", "Fragmento de la Espina",
                "Fragmento del Brote", "Fragmento de la Copa", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Del valle brotan troncos: un sendero sube hacia el invernadero.",
                "Los hongos del claro despiertan: saltar sobre ellos lanza muy alto.",
                "Un puente de raíces une la terraza alta con el mirador.",
                "La compuerta del invernadero se hunde.",
                "Una plataforma de piedra florece y cruza hacia el islote.",
                "Troncos nuevos suben del mirador a la colina.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            EventIds = new[] { "cristal_1", "hongos_2", "puente_3", "puerta_4", "plataforma_5", "cristal_6", "nucleo_7" },
            Growth = growth,
            Accent = flower,
            Background = new Color(0.34f, 0.43f, 0.4f),
            FogColor = new Color(0.34f, 0.43f, 0.4f),
            FogDensity = 0.008f,
            SunColor = new Color(1f, 0.95f, 0.8f),
            SunIntensity = 1.4f,
            SunEuler = new Vector3(50f, -30f, 0f),
            AmbientSky = new Color(0.55f, 0.65f, 0.6f),
            AmbientEquator = new Color(0.4f, 0.48f, 0.42f),
            AmbientGround = new Color(0.18f, 0.2f, 0.16f),
            ZoneLightColor = new Color(0.7f, 1f, 0.7f),
            VolumeFilter = new Color(1f, 1f, 0.96f),
            ChainBaseY = -4.6f,
            KillY = -5f,
            FloorY = -4.6f,
            FloorMaterial = GetOrCreateMaterial("JarValle", new Color(0.1f, 0.16f, 0.12f), null, 0f, 0.1f),
            Stars = false,
            Spawn = new Vector3(0f, 0.05f, -4f),
            Zones = new[]
            {
                new ZoneInfo("Plaza", 0f, 0f, 10f, 34f, 34f, 3f),             // x -17..17, z -7..27
                new ZoneInfo("Claro oeste", -46f, 0f, 10f, 22f, 24f, 3f),     // x -57..-35, z -2..22
                new ZoneInfo("Terraza alta", -46f, 5f, 32f, 22f, 16f, 4f),    // x -57..-35, z 24..40
                new ZoneInfo("Mirador", -14f, 6.5f, 36f, 20f, 14f, 4f),       // x -24..-4, z 29..43
                new ZoneInfo("Claro este", 45f, 0f, 10f, 22f, 24f, 3f),       // x 34..56, z -2..22
                new ZoneInfo("Invernadero", 10f, 2f, 54f, 28f, 26f, 4f),      // x -4..24, z 41..67
                new ZoneInfo("Islote", 10f, 2f, 93f, 12f, 12f, 4f),           // x 4..16, z 87..99
                new ZoneInfo("Colina", -16f, 9.5f, 62f, 24f, 20f, 5f),        // x -28..-4, z 52..72 (núcleo y portal)
            },
            KeepOut = new[]
            {
                Rect.MinMaxRect(-35f, 7f, -17f, 13f),        // hojas que se derrumban
                Rect.MinMaxRect(17f, 7f, 34f, 14.5f),        // piedras con zarzas
                Rect.MinMaxRect(3f, 27f, 9f, 41f),           // troncos al invernadero
                Rect.MinMaxRect(-52f, 17f, -40f, 24f),       // hongos de rebote
                Rect.MinMaxRect(-35f, 32.5f, -24f, 37.5f),   // puente de raíces
                Rect.MinMaxRect(7f, 67f, 13f, 87f),          // plataforma floral
                Rect.MinMaxRect(-19f, 43f, -13f, 52f),       // troncos a la colina
            },
            BuildLayout = BuildJardinLayout,
            Decorate = DecorateJardin,
        };
    }

    private static void BuildJardinLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        LevelTheme theme = context.Theme;
        RouteBuilder route = context.Route;
        Material leaf = GetOrCreateMaterial("JarHojaPetrea", new Color(0.28f, 0.4f, 0.3f), null, 0f, 0.15f);
        Material stem = GetOrCreateMaterial("JarTallo", new Color(0.3f, 0.45f, 0.3f), null, 0f, 0.1f);

        // --- Fragmentos ---
        context.Fragment(1, new Vector3(0f, 1.3f, 20f));        // Plaza
        context.Fragment(2, new Vector3(-50f, 1.3f, 4f));       // Claro oeste
        context.Fragment(3, new Vector3(-51f, 6.3f, 36f));      // Terraza alta
        context.Fragment(4, new Vector3(50f, 1.3f, 10f));       // Claro este, tras las zarzas
        context.Fragment(5, new Vector3(16f, 3.3f, 62f));       // Invernadero, tras la compuerta
        context.Fragment(6, new Vector3(10f, 3.3f, 94f));       // Islote
        context.Fragment(7, new Vector3(-16f, 10.8f, 57f));     // Colina

        // ================= RAMA OESTE: hojas que se derrumban, hongos y puente =================

        // Cuatro hojas de piedra entre la Plaza y el Claro oeste (huecos de 1 m). Tiemblan y caen al pisarlas.
        float[] leafX = { -19.6f, -23.8f, -28f, -32.2f };
        float[] leafTop = { 0f, 0.3f, 0f, 0.3f };
        for (int i = 0; i < leafX.Length; i++)
        {
            CreateCrumblingPlatform(level, "Hoja de piedra " + (i + 1), new Vector3(leafX[i], leafTop[i] - 0.25f, 10f), new Vector3(3.2f, 0.5f, 3.2f), leaf);
        }
        route.Jump(new Vector3(-16.7f, 0f, 10f), new Vector3(-18.3f, 0f, 10f));
        route.Jump(new Vector3(-20.9f, 0f, 10f), new Vector3(-22.5f, 0.3f, 10f));
        route.Jump(new Vector3(-25.1f, 0.3f, 10f), new Vector3(-26.7f, 0f, 10f));
        route.Jump(new Vector3(-29.3f, 0f, 10f), new Vector3(-30.9f, 0.3f, 10f));
        route.Jump(new Vector3(-33.5f, 0.3f, 10f), new Vector3(-35.3f, 0f, 10f));

        // 2. Hongos de rebote: en el borde norte del Claro oeste. Lanzan 6.5 m: se llega a la Terraza alta (5 m más arriba).
        GameObject mushroomA = CreateBouncePad(level, "Hongo de rebote A", new Vector3(-49f, 0f, 20f), 6.5f, stem, theme.Accent);
        GameObject mushroomB = CreateBouncePad(level, "Hongo de rebote B", new Vector3(-43f, 0f, 20f), 6.5f, stem, theme.Accent);
        GameObject mushrooms = CreateActivator("Hongos del claro", "hongos_2", mushroomA, mushroomB);

        // 3. Puente de raíces: de la Terraza alta (y 5) al Mirador (y 6.5).
        GameObject bridge3 = CreateBridge("Puente de raices", new Vector3(-35.1f, 5f, 35f), new Vector3(-23.9f, 6.5f, 35f), 4f, "puente_3", theme.Growth);

        // 6. Troncos a la colina: del Mirador (y 6.5) a la Colina (y 9.5), escalones de 1 m.
        GameObject chain6 = CreateCrystalChain("Troncos de la colina", "cristal_6", theme.Growth, theme.ChainBaseY, 4f, 3.2f,
            new[] { new Vector3(-16f, 7.5f, 45.5f), new Vector3(-16f, 8.5f, 49.5f) });
        route.Jump(new Vector3(-16f, 6.5f, 42.7f), new Vector3(-16f, 7.5f, 44.2f));
        route.Jump(new Vector3(-16f, 7.5f, 46.8f), new Vector3(-16f, 8.5f, 48.2f));
        route.Jump(new Vector3(-16f, 8.5f, 50.8f), new Vector3(-16f, 9.5f, 52.3f));

        // ================= RAMA ESTE: piedras con zarzas =================

        // Tres piedras entre la Plaza y el Claro este (huecos de 1.5 a 2 m). La segunda y la tercera tienen zarzas.
        CreateBlock("Piedra del sendero 1", level, new Vector3(20.2f, -1f, 10f), new Vector3(3.5f, 2f, 3.5f), materials.StoneTop);
        CreateBlock("Piedra del sendero 2", level, new Vector3(25.2f, -1f, 12f), new Vector3(3.5f, 2f, 3.5f), materials.StoneTop);
        CreateBlock("Piedra del sendero 3", level, new Vector3(30.2f, -1f, 10f), new Vector3(3.5f, 2f, 3.5f), materials.StoneTop);
        route.Jump(new Vector3(16.7f, 0f, 10f), new Vector3(18.75f, 0f, 10f));
        route.Jump(new Vector3(21.65f, 0f, 10.5f), new Vector3(23.75f, 0f, 11.5f));
        route.Jump(new Vector3(26.65f, 0f, 11.5f), new Vector3(28.75f, 0f, 10.5f));
        route.Jump(new Vector3(31.65f, 0f, 10f), new Vector3(34.3f, 0f, 10f));

        Material thornPlate = context.Danger.EnemyRock;
        CreateThornPatch(level, "Zarzas de la piedra 2", new Vector3(25.2f, 0f, 12f), new Vector2(3f, 3f), 0f, materials, thornPlate);
        CreateThornPatch(level, "Zarzas de la piedra 3", new Vector3(30.2f, 0f, 10f), new Vector2(3f, 3f), 2.3f, materials, thornPlate);

        // Dos filas de zarzas cruzan el Claro este de lado a lado, desfasadas: hay que leer el ritmo para llegar al fragmento 4.
        float[] rowZ = { 2f, 10f, 18f };
        float[] rowAOffset = { 0f, 1.5f, 3f };
        float[] rowBOffset = { 2.3f, 3.8f, 0.7f };
        for (int i = 0; i < rowZ.Length; i++)
        {
            CreateThornPatch(level, "Zarzas fila A " + (i + 1), new Vector3(39f, 0f, rowZ[i]), new Vector2(3f, 7.6f), rowAOffset[i], materials, thornPlate);
            CreateThornPatch(level, "Zarzas fila B " + (i + 1), new Vector3(45f, 0f, rowZ[i]), new Vector2(3f, 7.6f), rowBOffset[i], materials, thornPlate);
        }

        // ================= RAMA NORTE: troncos, invernadero, plataforma e islote =================

        // 1. Troncos al invernadero: de la Plaza (y 0) al Invernadero (y 2), escalones de 0.5 m.
        GameObject chain1 = CreateCrystalChain("Troncos del invernadero", "cristal_1", theme.Growth, theme.ChainBaseY, 4f, 3.2f,
            new[] { new Vector3(6f, 0.5f, 29.5f), new Vector3(6f, 1f, 33.5f), new Vector3(6f, 1.5f, 37.5f) });
        route.Jump(new Vector3(6f, 0f, 26.7f), new Vector3(6f, 0.5f, 28.2f));
        route.Jump(new Vector3(6f, 0.5f, 30.8f), new Vector3(6f, 1f, 32.2f));
        route.Jump(new Vector3(6f, 1f, 34.8f), new Vector3(6f, 1.5f, 36.2f));
        route.Jump(new Vector3(6f, 1.5f, 38.8f), new Vector3(6f, 2f, 41.3f));

        // 4. Compuerta del invernadero: muro que cruza la zona; el fragmento 5 está detrás.
        GameObject door4 = CreateGateWall(level, "Compuerta del invernadero", "puerta_4", -4f, 24f, 10f, 2f, 54f, materials);

        // Zarzas delante de la compuerta.
        float[] gateX = { 1f, 10f, 19f };
        for (int i = 0; i < gateX.Length; i++)
        {
            CreateThornPatch(level, "Zarzas del invernadero " + (i + 1), new Vector3(gateX[i], 2f, 49f), new Vector2(8.6f, 3f), i * 1.5f, materials, thornPlate);
        }

        // 5. Plataforma floral: del fondo del Invernadero al Islote (14 m de viaje).
        GameObject platform5 = CreateMovingPlatform("Plataforma floral", "plataforma_5", new Vector3(10f, 1.7f, 70f), new Vector3(0f, 0f, 14f), materials);

        // 7. Núcleo y portal en la Colina.
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(-16f, 9.5f, 62f)));

        ConfigureEvent(chain1, chain1.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(mushrooms, mushroomA.transform, theme.Accent);
        ConfigureEvent(bridge3, bridge3.transform, theme.Growth);
        ConfigureEvent(door4, door4.transform.Find("Losa"), materials.Door);
        ConfigureEvent(platform5, platform5.transform, theme.Growth);
        ConfigureEvent(chain6, chain6.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // --- Peligros: tres sombras y una sola torreta (vigila la plataforma desde el Islote) ---
        CreateRockShadow("Sombra de la plaza", level, new Vector3(-10f, 0.9f, 14f), new Vector3(10f, 0.9f, 14f), IslandArea(0f, 0f, 10f, 34f, 34f), materials, context.Danger);
        CreateRockShadow("Sombra del claro", level, new Vector3(-54f, 0.9f, 12f), new Vector3(-38f, 0.9f, 12f), IslandArea(-46f, 0f, 10f, 22f, 24f), materials, context.Danger);
        CreateRockShadow("Sombra del invernadero", level, new Vector3(0f, 2.9f, 61f), new Vector3(20f, 2.9f, 61f), IslandArea(10f, 2f, 61f, 28f, 12f), materials, context.Danger);
        CreateCrystalTurret("Torreta del islote", level, new Vector3(14f, 2f, 97f), 1, 3f, context.Projectile, materials, context.Danger);

        // --- Checkpoints (se registran; la reaparición sigue siendo al inicio) ---
        CreateCheckpoint("Checkpoint claro oeste", new Vector3(-46f, 3f, 10f), new Vector3(20f, 8f, 22f), new Vector3(-37f, 0.05f, 10f));
        CreateCheckpoint("Checkpoint claro este", new Vector3(45f, 3f, 10f), new Vector3(20f, 8f, 22f), new Vector3(36f, 0.05f, 10f));
        CreateCheckpoint("Checkpoint invernadero", new Vector3(10f, 5f, 47f), new Vector3(26f, 8f, 10f), new Vector3(6f, 2.05f, 43f));
    }

    private static void DecorateJardin(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(4);
        Material trunk = GetOrCreateMaterial("JarTronco", new Color(0.22f, 0.22f, 0.21f), null, 0f, 0.15f);
        Material leaves = GetOrCreateMaterial("JarHojaPetrea", new Color(0.28f, 0.4f, 0.3f), null, 0f, 0.15f);
        Material stem = GetOrCreateMaterial("JarTallo", new Color(0.3f, 0.45f, 0.3f), null, 0f, 0.1f);

        foreach (ZoneInfo zone in theme.Zones)
        {
            // Árboles petrificados en las esquinas (sin collider), salvo donde pasa el camino.
            for (int corner = 0; corner < 4; corner++)
            {
                float x = zone.X + (corner % 2 == 0 ? -1f : 1f) * (zone.Width / 2f - 1.3f);
                float z = zone.Z + (corner < 2 ? -1f : 1f) * (zone.Depth / 2f - 1.3f);
                if (!NearPath(theme, x, z, 2.5f))
                {
                    CreateTree(level, "Arbol " + zone.Name + " " + (corner + 1), new Vector3(x, zone.Y, z), 5f + (float)rng.NextDouble() * 4f, trunk, leaves, rng, false);
                }
            }

            // Arbustos y flores pegados a los bordes este y oeste.
            for (int k = 0; k < 6; k++)
            {
                float side = k % 2 == 0 ? -1f : 1f;
                float x = zone.X + side * (zone.Width / 2f - 1.5f - (float)rng.NextDouble() * 1.5f);
                float z = zone.Z + Mathf.Lerp(-0.4f, 0.4f, (float)rng.NextDouble()) * zone.Depth;
                if (NearPath(theme, x, z, 2f))
                {
                    continue;
                }

                if (k < 3)
                {
                    CreateBush(level, "Arbusto " + zone.Name + " " + (k + 1), new Vector3(x, zone.Y, z), 1.2f + (float)rng.NextDouble() * 0.8f, leaves, rng);
                }
                else
                {
                    CreateFlower(level, "Flor " + zone.Name + " " + (k - 2), new Vector3(x, zone.Y, z), 0.8f + (float)rng.NextDouble() * 0.6f, stem, theme.Accent);
                }
            }
        }

        // Estatuas de jardineros en la Plaza: las primeras figuras humanas del juego.
        CreateStatue(level, "Estatua de la plaza A", new Vector3(-13f, 0f, -3f), 20f, materials.Ruin);
        CreateStatue(level, "Estatua de la plaza B", new Vector3(13f, 0f, 23f), -35f, materials.Ruin);
        CreateStatue(level, "Estatua del mirador", new Vector3(-7f, 6.5f, 32f), 160f, materials.Ruin);

        // Glorieta sobre la salida norte de la Plaza (los troncos del invernadero pasan por debajo).
        CreateArch(level, "Glorieta norte", new Vector3(6f, 0f, 25f), 8f, 6f, materials.StoneTop);

        // Fuente seca en la Plaza (fuera del camino).
        CreateVisualPart("Fuente base", level, PrimitiveType.Cylinder, new Vector3(-10f, 0.2f, 22f), new Vector3(3.6f, 0.2f, 3.6f), Vector3.zero, materials.Ruin);
        CreateVisualPart("Fuente pilar", level, PrimitiveType.Cylinder, new Vector3(-10f, 1f, 22f), new Vector3(0.7f, 0.8f, 0.7f), Vector3.zero, materials.StoneTop);
        CreateVisualPart("Fuente copa", level, PrimitiveType.Cylinder, new Vector3(-10f, 1.9f, 22f), new Vector3(1.8f, 0.12f, 1.8f), Vector3.zero, materials.Ruin);

        // Techo de cristal del invernadero (sobre la sala del fondo) con cuatro pilares.
        Material glass = GetOrCreateTransparentMaterial("JarCristal", "Universal Render Pipeline/Lit", new Color(0.7f, 0.95f, 0.85f, 0.25f), new Color(0.4f, 0.9f, 0.7f), 0.4f, false);
        GameObject roof = CreateBlock("Techo del invernadero", level, new Vector3(10f, 7.2f, 61f), new Vector3(28f, 0.2f, 12f), glass);
        Object.DestroyImmediate(roof.GetComponent<Collider>());
        NoShadow(roof);
        float[] roofX = { -3f, 23f };
        float[] roofZ = { 56f, 66f };
        foreach (float x in roofX)
        {
            foreach (float z in roofZ)
            {
                CreateColumn(level, "Pilar del invernadero", new Vector3(x, 2f, z), 5.1f, 0.6f, materials.Ruin, false);
            }
        }

        // Bosque petrificado en el valle (altos, para que asomen sobre el borde de las mesetas), lejos del recorrido.
        for (int i = 0; i < 110; i++)
        {
            float x = Mathf.Lerp(-130f, 130f, (float)rng.NextDouble());
            float z = Mathf.Lerp(-60f, 170f, (float)rng.NextDouble());
            if (IsClear(theme, x, z, 6f))
            {
                CreateTree(level, "Arbol del valle " + (i + 1), new Vector3(x, theme.FloorY, z), 10f + (float)rng.NextDouble() * 12f, trunk, leaves, rng, false);
            }
        }

        // Árbol colosal tras la Colina: marca el destino desde lejos.
        CreateColumn(level, "Arbol colosal tronco", new Vector3(-16f, theme.FloorY, 125f), 55f, 9f, trunk, false);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            Vector3 position = new Vector3(-16f + Mathf.Cos(angle) * 14f, 58f + (i % 2) * 6f, 125f + Mathf.Sin(angle) * 14f);
            NoShadow(CreateVisualPart("Arbol colosal copa " + (i + 1), level, PrimitiveType.Sphere, position, Vector3.one * 28f, Vector3.zero, leaves));
        }

        // Bruma baja y luciérnagas.
        CreateMotes("Bruma", level, new Vector3(0f, -2f, 45f), new Vector3(160f, 3f, 170f), materials.Star, new Color(0.9f, 1.1f, 1f, 0.22f), 120, 8f, 5f, 0f);
        CreateMotes("Luciernagas", level, new Vector3(0f, 6f, 45f), new Vector3(120f, 10f, 130f), materials.Star, new Color(1.8f, 2.2f, 0.8f, 0.9f), 220, 14f, 0.12f, 0.01f);
    }

    // Estatua de jardinero: cuerpo, cabeza, brazos y una herramienta (todo piedra). Solo visual.
    private static void CreateStatue(Transform parent, string name, Vector3 basePosition, float yaw, Material stone)
    {
        GameObject statue = new GameObject(name);
        statue.transform.SetParent(parent, false);
        statue.transform.position = basePosition;
        statue.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateVisualPart("Pedestal", statue.transform, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 1.2f), Vector3.zero, stone);
        CreateVisualPart("Cuerpo", statue.transform, PrimitiveType.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 0.8f, 0.4f), Vector3.zero, stone);
        CreateVisualPart("Cabeza", statue.transform, PrimitiveType.Sphere, new Vector3(0f, 2.55f, 0f), Vector3.one * 0.45f, Vector3.zero, stone);
        CreateVisualPart("Brazo izquierdo", statue.transform, PrimitiveType.Capsule, new Vector3(-0.45f, 1.7f, 0.15f), new Vector3(0.16f, 0.45f, 0.16f), new Vector3(35f, 0f, 10f), stone);
        CreateVisualPart("Brazo derecho", statue.transform, PrimitiveType.Capsule, new Vector3(0.45f, 1.7f, 0.15f), new Vector3(0.16f, 0.45f, 0.16f), new Vector3(60f, 0f, -10f), stone);
        CreateVisualPart("Herramienta", statue.transform, PrimitiveType.Cube, new Vector3(0.5f, 1.9f, 0.55f), new Vector3(0.06f, 0.06f, 1.1f), Vector3.zero, stone);
    }
}
