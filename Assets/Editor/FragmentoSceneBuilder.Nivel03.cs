using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 3, "Observatorio en ruinas" (Nivel03_Observatorio.unity): terrazas de mármol que suben en
///   espiral alrededor de una torre, sobre un abismo estrellado. Kael gana torso y cabeza de luz.
///
/// POR QUÉ EXISTE
///   Es el tercer paso del arco de materialización: arquitectura humana (columnas, arcos, instrumentos).
///   Su identidad de juego es el ASCENSO y la INGRAVIDEZ: plataformas en órbita, un campo de baja gravedad y
///   haces de luz que barren las terrazas (se saltan). No hay torretas.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 3 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido (visto desde arriba, la torre en el centro): Atrio (sur) > Terraza sur > Terraza este >
///   Terraza norte > Terraza oeste > Cúpula (encima de la torre).
///   Fragmentos: 1 columnas del abismo, 2 puente astral, 3 plataformas en órbita, 4 campo de ingravidez,
///   5 compuerta, 6 columnas de la cúpula, 7 núcleo.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel03ScenePath = "Assets/Scenes/Nivel03_Observatorio.unity";

    [MenuItem("Fragmento/Construir Nivel 3")]
    public static void BuildNivel3()
    {
        BuildThemedLevel(CreateNivel3Theme);
    }

    private static LevelTheme CreateNivel3Theme(Materials materials)
    {
        // Mármol azulado, latón cálido y luz de luna: lo único cian sigue siendo Kael.
        materials.Stone = GetOrCreateMaterial("ObsPiedra", new Color(0.1f, 0.11f, 0.17f), null, 0f, 0.25f);
        materials.StoneTop = GetOrCreateMaterial("ObsMarmol", new Color(0.42f, 0.43f, 0.52f), null, 0f, 0.45f);
        materials.Ruin = GetOrCreateMaterial("ObsRuina", new Color(0.3f, 0.31f, 0.38f), null, 0f, 0.3f);
        Material brass = GetOrCreateMaterial("ObsLaton", new Color(0.7f, 0.5f, 0.2f), new Color(1f, 0.7f, 0.3f), 1.4f, 0.6f);
        materials.Edge = brass;
        materials.PlatformOff = GetOrCreateMaterial("ObsPlataformaApagada", new Color(0.1f, 0.1f, 0.16f), new Color(1f, 0.7f, 0.3f), 0.25f, 0.3f);
        Material growth = GetOrCreateMaterial("ObsColumnaAstral", new Color(0.72f, 0.74f, 0.88f), new Color(0.55f, 0.65f, 1f), 1.3f, 0.5f);
        materials.PlatformOn = growth;

        return new LevelTheme
        {
            ScenePath = Nivel03ScenePath,
            VolumePath = SettingsFolder + "/Nivel03_PerfilVolumen.asset",
            NextScenePath = Nivel04ScenePath,
            FilePrefix = "N3_",
            KaelPrefabName = "Kael_Nivel3",
            KaelStage = 3,
            KaelRings = true,
            FragmentFiles = new[] { "Horizonte", "Lente", "Pendulo", "Eclipse", "Orbita", "Cenit", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento del Horizonte", "Fragmento de la Lente", "Fragmento del Péndulo", "Fragmento del Eclipse",
                "Fragmento de la Órbita", "Fragmento del Cénit", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Del abismo se alzan columnas: un camino de mármol sube a la terraza.",
                "Un puente astral sube hacia la terraza del este.",
                "Dos plataformas de latón despiertan y giran en órbita.",
                "Un campo astral se enciende: dentro de él, Kael casi flota.",
                "La compuerta de sombras se hunde: el fondo de la terraza está libre.",
                "Columnas de mármol ascienden hacia la cúpula.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            EventIds = new[] { "cristal_1", "puente_2", "orbita_3", "ingravidez_4", "puerta_5", "cristal_6", "nucleo_7" },
            Growth = growth,
            Accent = brass,
            Background = new Color(0.01f, 0.012f, 0.04f),
            FogColor = new Color(0.015f, 0.02f, 0.05f),
            FogDensity = 0.004f,
            SunColor = new Color(0.6f, 0.7f, 1f),
            SunIntensity = 1.1f,
            SunEuler = new Vector3(40f, -50f, 0f),
            AmbientSky = new Color(0.22f, 0.26f, 0.5f),
            AmbientEquator = new Color(0.14f, 0.15f, 0.3f),
            AmbientGround = new Color(0.06f, 0.05f, 0.1f),
            ZoneLightColor = new Color(1f, 0.78f, 0.45f),
            VolumeFilter = new Color(0.94f, 0.96f, 1f),
            ChainBaseY = -25f, // las columnas nacen de las profundidades del abismo
            KillY = -25f,
            Stars = true,
            Zones = new[]
            {
                // La torre está en (0, 60). Las terrazas la rodean y cada una está más alta que la anterior.
                new ZoneInfo("Atrio", 0f, 0f, -2f, 26f, 24f, 2f),             // z -14..10
                new ZoneInfo("Terraza sur", 0f, 2f, 33f, 22f, 20f, 6f),       // x -11..11, z 23..43
                new ZoneInfo("Terraza este", 33f, 5f, 50f, 16f, 28f, 6f),     // x 25..41, z 36..64
                new ZoneInfo("Terraza norte", 5f, 5f, 78f, 34f, 20f, 6f),     // x -12..22, z 68..88
                new ZoneInfo("Terraza oeste", -33f, 10f, 53f, 16f, 34f, 6f),  // x -41..-25, z 36..70
                new ZoneInfo("Cupula", 0f, 14f, 60f, 24f, 20f, 6f),           // x -12..12, z 50..70 (encima de la torre)
            },
            KeepOut = new[]
            {
                Rect.MinMaxRect(-3f, 10f, 3f, 23f),      // columnas del abismo
                Rect.MinMaxRect(11f, 37f, 25f, 42f),     // puente astral
                Rect.MinMaxRect(22f, 63f, 42f, 84f),     // órbitas
                Rect.MinMaxRect(-34f, 70f, -12f, 82f),   // losas del campo de ingravidez
                Rect.MinMaxRect(-25f, 38f, 2f, 50f),     // columnas de la cúpula
            },
            BuildLayout = BuildObservatorioLayout,
            Decorate = DecorateObservatorio,
        };
    }

    private static void BuildObservatorioLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        LevelTheme theme = context.Theme;
        RouteBuilder route = context.Route;

        // --- Fragmentos ---
        context.Fragment(1, new Vector3(-6f, 1.3f, 4f));       // Atrio
        context.Fragment(2, new Vector3(7f, 3.3f, 29f));       // Terraza sur
        context.Fragment(3, new Vector3(36f, 6.3f, 58f));      // Terraza este
        context.Fragment(4, new Vector3(0f, 6.3f, 82f));       // Terraza norte
        context.Fragment(5, new Vector3(-36f, 11.3f, 60f));    // Terraza oeste, antes de la compuerta
        context.Fragment(6, new Vector3(-33f, 11.3f, 40f));    // Terraza oeste, tras la compuerta
        context.Fragment(7, new Vector3(0f, 15.3f, 55f));      // Cúpula

        // --- 1. Columnas del abismo: del Atrio (y 0) a la Terraza sur (y 2), escalones de 0.5 m ---
        GameObject chain1 = CreateCrystalChain("Columnas del abismo", "cristal_1", theme.Growth, theme.ChainBaseY, 5f, 3.2f,
            new[] { new Vector3(0f, 0.5f, 12.5f), new Vector3(0f, 1f, 16.5f), new Vector3(0f, 1.5f, 20.5f) });
        route.Jump(new Vector3(0f, 0f, 9.7f), new Vector3(0f, 0.5f, 11.2f));
        route.Jump(new Vector3(0f, 0.5f, 13.8f), new Vector3(0f, 1f, 15.2f));
        route.Jump(new Vector3(0f, 1f, 17.8f), new Vector3(0f, 1.5f, 19.2f));
        route.Jump(new Vector3(0f, 1.5f, 21.8f), new Vector3(0f, 2f, 23.3f));

        // --- 2. Puente astral: de la Terraza sur (y 2) sube al este hasta la Terraza este (y 5) ---
        GameObject bridge2 = CreateBridge("Puente astral", new Vector3(10.9f, 2f, 39.5f), new Vector3(25.1f, 5f, 39.5f), 4f, "puente_2", theme.Growth);

        // --- 3. Plataformas en órbita: giran alrededor de (33, 75), radio 7. Se suben en el punto sur (junto a la
        //        Terraza este) y se bajan en el punto oeste (junto a la Terraza norte), un cuarto de vuelta después. ---
        GameObject orbitA = CreateOrbitPlatform("Orbita A", "orbita_3", new Vector3(33f, 4.7f, 68f), new Vector3(0f, 0f, 7f), -25f, false, materials);
        CreateOrbitPlatform("Orbita B", "orbita_3", new Vector3(33f, 4.7f, 82f), new Vector3(0f, 0f, -7f), -25f, false, materials);
        // Eje de las órbitas: un astrolabio de latón (solo visual) para que se entienda alrededor de qué giran.
        NoShadow(CreateVisualPart("Eje de las orbitas", level, PrimitiveType.Cylinder, new Vector3(33f, 0f, 75f), new Vector3(0.6f, 6f, 0.6f), Vector3.zero, theme.Accent));

        // --- 4. Campo de ingravidez: entre la Terraza norte (y 5) y la Terraza oeste (y 10). Las losas están
        //        siempre, pero sin el campo los saltos (5.5 m de hueco y 2.5 m de subida) son imposibles. ---
        GameObject slab1 = CreateBlock("Losa astral 1", level, new Vector3(-19.5f, 7.2f, 78f), new Vector3(4f, 0.6f, 4f), theme.Growth);  // cara superior en 7.5
        CreateBlock("Losa astral 2", level, new Vector3(-29f, 9.2f, 76f), new Vector3(4f, 0.6f, 4f), theme.Growth);                       // cara superior en 9.5
        GameObject field = CreateLowGravityZone(level, "Campo de ingravidez", new Vector3(-21.5f, 12f, 74f), new Vector3(25f, 18f, 18f),
            materials.Star, new Color(1.2f, 1.5f, 2.4f, 0.8f));
        GameObject fieldEvent = CreateActivator("Campo astral", "ingravidez_4", field);

        // --- 5. Compuerta: muro que cruza la Terraza oeste; detrás está el fragmento 6 ---
        GameObject door5 = CreateGateWall(level, "Compuerta de sombras", "puerta_5", -41f, -25f, -33f, 10f, 45f, materials);

        // --- 6. Columnas de la cúpula: del fondo de la Terraza oeste (y 10) a la Cúpula (y 14), girando al norte ---
        GameObject chain6 = CreateCrystalChain("Columnas de la cupula", "cristal_6", theme.Growth, theme.ChainBaseY, 3.2f, 3.2f,
            new[]
            {
                new Vector3(-22.6f, 10.6f, 40f), new Vector3(-18.6f, 11.2f, 40f), new Vector3(-14.6f, 11.8f, 40f), new Vector3(-10.6f, 12.4f, 40f),
                new Vector3(-6.6f, 13f, 40f), new Vector3(-3f, 13.5f, 43.6f), new Vector3(0f, 14f, 47.2f),
            });
        route.Jump(new Vector3(-25.3f, 10f, 40f), new Vector3(-23.9f, 10.6f, 40f));
        route.Jump(new Vector3(-21.3f, 10.6f, 40f), new Vector3(-19.9f, 11.2f, 40f));
        route.Jump(new Vector3(-17.3f, 11.2f, 40f), new Vector3(-15.9f, 11.8f, 40f));
        route.Jump(new Vector3(-13.3f, 11.8f, 40f), new Vector3(-11.9f, 12.4f, 40f));
        route.Jump(new Vector3(-9.3f, 12.4f, 40f), new Vector3(-7.9f, 13f, 40f));
        route.Jump(new Vector3(-5.3f, 13f, 41.3f), new Vector3(-4.3f, 13.5f, 42.3f));
        route.Jump(new Vector3(-1.7f, 13.5f, 44.9f), new Vector3(-1.3f, 14f, 45.9f));
        route.Jump(new Vector3(0f, 14f, 48.5f), new Vector3(0f, 14f, 50.3f));

        // --- 7. Núcleo y portal en la Cúpula ---
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(0f, 14f, 60f)));

        ConfigureEvent(chain1, chain1.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(bridge2, bridge2.transform, theme.Growth);
        ConfigureEvent(orbitA, orbitA.transform, theme.Growth);
        ConfigureEvent(fieldEvent, slab1.transform, theme.Growth);
        ConfigureEvent(door5, door5.transform.Find("Losa"), materials.Door);
        ConfigureEvent(chain6, chain6.transform.GetChild(3).GetChild(0), theme.Growth);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // --- Peligros: haces que barren (se saltan) y solo dos sombras. Sin torretas. ---
        CreateRockShadow("Sombra del atrio", level, new Vector3(-9f, 0.9f, 2f), new Vector3(9f, 0.9f, 2f), IslandArea(0f, 0f, -2f, 26f, 24f), materials, context.Danger);
        CreateSweepingBeam(level, "Haz de la terraza sur", new Vector3(0f, 2f, 33f), 8f, 35f, materials, materials.Ruin);
        CreateRockShadow("Sombra del este", level, new Vector3(27f, 5.9f, 56f), new Vector3(39f, 5.9f, 56f), IslandArea(33f, 5f, 50f, 16f, 28f), materials, context.Danger);
        CreateSweepingBeam(level, "Haz de la terraza norte", new Vector3(5f, 5f, 78f), 9f, -45f, materials, materials.Ruin);
        CreateSweepingBeam(level, "Haz de la terraza oeste", new Vector3(-33f, 10f, 58f), 7.5f, 50f, materials, materials.Ruin);

        // --- Checkpoints (se registran; la reaparición sigue siendo al inicio) ---
        CreateCheckpoint("Checkpoint sur", new Vector3(0f, 5f, 33f), new Vector3(20f, 8f, 18f), new Vector3(0f, 2.05f, 26f));
        CreateCheckpoint("Checkpoint este", new Vector3(33f, 8f, 50f), new Vector3(14f, 8f, 26f), new Vector3(28f, 5.05f, 40f));
        CreateCheckpoint("Checkpoint norte", new Vector3(5f, 8f, 78f), new Vector3(32f, 8f, 18f), new Vector3(18f, 5.05f, 75f));
        CreateCheckpoint("Checkpoint oeste", new Vector3(-33f, 13f, 58f), new Vector3(14f, 8f, 22f), new Vector3(-33f, 10.05f, 66f));
    }

    private static void DecorateObservatorio(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(3);

        Material moon = GetOrCreateMaterial("ObsLuna", new Color(0.9f, 0.92f, 1f), new Color(0.8f, 0.85f, 1f), 2.4f, 0.3f);
        CreateSkyDisc(level, "Luna", new Vector3(-110f, 85f, 260f), 55f, moon);

        // Torre central: el cuerpo que sostiene la Cúpula (solo visual: no se llega a tocar).
        GameObject tower = CreateBlock("Torre del observatorio", level, new Vector3(0f, -8.5f, 60f), new Vector3(16f, 33f, 16f), materials.Ruin);
        Object.DestroyImmediate(tower.GetComponent<Collider>());
        NoShadow(tower);
        for (int i = 0; i < 4; i++)
        {
            // Ventanas encendidas en la cara sur de la torre: se ven desde el Atrio y marcan el destino.
            NoShadow(CreateVisualPart("Ventana de la torre " + (i + 1), level, PrimitiveType.Cube, new Vector3(-4.5f + i * 3f, 4.5f, 51.9f), new Vector3(1f, 2.2f, 0.2f), Vector3.zero, theme.Accent));
        }

        // Columnas (unas enteras, otras rotas) en las esquinas de cada terraza, lejos de las entradas y salidas.
        foreach (ZoneInfo zone in theme.Zones)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                float x = zone.X + (corner % 2 == 0 ? -1f : 1f) * (zone.Width / 2f - 1.2f);
                float z = zone.Z + (corner < 2 ? -1f : 1f) * (zone.Depth / 2f - 1.2f);
                if (NearPath(theme, x, z, 2f))
                {
                    continue;
                }

                bool broken = rng.NextDouble() < 0.5;
                float height = broken ? 1.5f + (float)rng.NextDouble() * 2.5f : 6f + (float)rng.NextDouble() * 3f;
                CreateColumn(level, "Columna " + zone.Name + " " + (corner + 1), new Vector3(x, zone.Y, z), height, 1.3f, materials.StoneTop, false);
                if (broken)
                {
                    // Trozo caído a los pies de la columna rota.
                    GameObject drum = CreateVisualPart("Tambor caido", level, PrimitiveType.Cylinder,
                        new Vector3(x + (x < zone.X ? 1.6f : -1.6f), zone.Y + 0.4f, z), new Vector3(1.2f, 0.5f, 1.2f), new Vector3(0f, 0f, 80f), materials.Ruin);
                    NoShadow(drum);
                }
            }
        }

        // Arcos sobre el camino: salida del Atrio y entrada de la Terraza sur (las columnas del abismo pasan por debajo).
        CreateArch(level, "Arco del atrio", new Vector3(0f, 0f, 8f), 12f, 7f, materials.StoneTop);
        CreateArch(level, "Arco de la terraza sur", new Vector3(0f, 2f, 25f), 12f, 7f, materials.StoneTop);

        // Telescopio de la Cúpula: base, tubo inclinado hacia la luna y anillo de latón.
        Vector3 scope = new Vector3(-8f, 14f, 53f);
        CreateVisualPart("Telescopio base", level, PrimitiveType.Cylinder, scope + Vector3.up * 0.9f, new Vector3(1.6f, 0.9f, 1.6f), Vector3.zero, materials.Ruin);
        GameObject tube = CreateVisualPart("Telescopio tubo", level, PrimitiveType.Cylinder, scope + new Vector3(-0.8f, 3.2f, 1.4f), new Vector3(1f, 2.4f, 1f), new Vector3(55f, 20f, 0f), materials.StoneTop);
        NoShadow(tube);
        CreateVisualPart("Telescopio anillo", level, PrimitiveType.Cylinder, scope + new Vector3(-0.5f, 2.6f, 0.8f), new Vector3(1.15f, 0.08f, 1.15f), new Vector3(55f, 20f, 0f), theme.Accent);

        // Torres en ruinas a lo lejos, con ventanas encendidas.
        for (int i = 0; i < 10; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float height = 30f + (float)rng.NextDouble() * 40f;
            Vector3 position = new Vector3(side * (70f + (float)rng.NextDouble() * 40f), height / 2f - 30f, Mathf.Lerp(-40f, 160f, (float)rng.NextDouble()));
            GameObject far = CreateBlock("Torre lejana " + (i + 1), level, position, new Vector3(9f, height, 9f), materials.Ruin);
            Object.DestroyImmediate(far.GetComponent<Collider>());
            NoShadow(far);
            for (int w = 0; w < 3; w++)
            {
                Vector3 window = position + new Vector3(-side * 4.6f, Mathf.Lerp(-height / 2f + 8f, height / 2f - 4f, (float)rng.NextDouble()), (w - 1) * 2.6f);
                NoShadow(CreateVisualPart("Ventana", level, PrimitiveType.Cube, window, new Vector3(0.2f, 1.4f, 1f), Vector3.zero, theme.Accent));
            }
        }

        // Escombros flotando en el vacío (fragmentos de la cúpula caída), fuera de las terrazas.
        for (int i = 0; i < 40; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 position = new Vector3(side * (50f + (float)rng.NextDouble() * 50f), Mathf.Lerp(-12f, 45f, (float)rng.NextDouble()), Mathf.Lerp(-40f, 150f, (float)rng.NextDouble()));
            float size = 1f + (float)rng.NextDouble() * 3f;
            GameObject debris = CreateVisualPart("Escombro " + (i + 1), level, PrimitiveType.Cube, position, new Vector3(size, size * 0.7f, size),
                new Vector3((float)rng.NextDouble() * 60f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 60f), materials.Ruin);
            NoShadow(debris);
        }

        // Polvo dorado en suspensión sobre todo el recorrido.
        CreateMotes("Polvo dorado", level, new Vector3(0f, 10f, 40f), new Vector3(100f, 24f, 140f), materials.Star, new Color(1.6f, 1.2f, 0.7f, 0.7f), 300, 18f, 0.1f, 0.02f);
    }
}
