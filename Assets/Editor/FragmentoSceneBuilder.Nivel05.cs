using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 5, "Lago y cascadas" (Nivel05_Lago.unity): mesetas de roca que nacen de un lago turquesa, acantilados
///   con cascadas a ambos lados, montañas lejanas y un amanecer cálido. Kael ya es una silueta humanoide con túnica,
///   bufanda y capa de luz.
///
/// POR QUÉ EXISTE
///   Quinto paso del arco de materialización: aparece el agua y la luz de un día real (sol bajo, cielo rosado). Reutiliza
///   el recorrido y los eventos del esqueleto común (FragmentoSceneBuilder.Niveles.cs).
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 5 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Los fragmentos hacen emerger del agua piedras de cruce que sirven de pasos. Caer al lago cuenta como muerte.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel05ScenePath = "Assets/Scenes/Nivel05_Lago.unity";

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
            NextScenePath = "",
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
                "Piedras de cruce emergen del agua: un camino atraviesa el lago.",
                "Un puente de roca une la orilla con la meseta alta.",
                "Una plataforma de piedra despierta y cruza el remanso.",
                "La compuerta de la presa se hunde: el cauce está libre.",
                "Piedras emergen junto a la cascada y suben hacia la poza.",
                "Las últimas piedras ascienden hacia la cumbre.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            Chain1Name = "Piedras de cruce",
            Bridge2Name = "Puente de roca",
            Platform3Name = "Plataforma del remanso",
            Door4Name = "Compuerta de la presa",
            Chain5Name = "Piedras de la cascada",
            Chain6Name = "Piedras de la cumbre",
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
            HazardTier = 2,
            Decorate = DecorateLago,
        };
    }

    private static void DecorateLago(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(5);
        Material waterfall = GetOrCreateTransparentMaterial("LagCascada", "Universal Render Pipeline/Lit", new Color(0.85f, 0.95f, 1f, 0.5f), new Color(0.5f, 0.8f, 1f), 0.7f, false);
        Material pad = GetOrCreateMaterial("LagNenufar", new Color(0.2f, 0.5f, 0.3f), null, 0f, 0.3f);
        Material mountain = GetOrCreateMaterial("LagMontana", new Color(0.4f, 0.45f, 0.6f), null, 0f, 0.1f);

        // Superficie del lago (transparente, sin collider: caer al agua mata, ver la zona de caída).
        GameObject water = CreateBlock("Agua del lago", level, new Vector3(0f, -4.05f, 120f), new Vector3(700f, 0.1f, 700f), theme.Water);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        NoShadow(water);

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

        // Acantilados a ambos lados con cinco cascadas cada uno.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 6; i++)
            {
                float z = -10f + i * 52f;
                float height = 46f + (float)rng.NextDouble() * 14f;
                GameObject cliff = CreateBlock("Acantilado " + (side < 0 ? "O" : "E") + " " + (i + 1), level, new Vector3(side * 64f, height / 2f - 6f, z),
                    new Vector3(26f, height, 54f), materials.Ruin);
                Object.DestroyImmediate(cliff.GetComponent<Collider>());
                NoShadow(cliff);

                if (i % 2 == 0 || i == 5)
                {
                    CreateWaterfall(level, "Cascada " + (side < 0 ? "O" : "E") + " " + (i + 1), new Vector3(side * 50.6f, height - 6f, z), height - 2f, 8f, waterfall, theme.Accent, materials.Star);
                }
            }
        }

        // Rocas y montones de piedra sobre el agua, entre las mesetas y a los lados.
        for (int i = 0; i < 36; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 position = new Vector3(side * (24f + (float)rng.NextDouble() * 18f), -4.2f, Mathf.Lerp(-20f, 270f, (float)rng.NextDouble()));
            CreateRockPile(level, "Rocas del lago " + (i + 1), position, 2.5f + (float)rng.NextDouble() * 3f, materials.StoneTop, rng);
        }

        // Nenúfares: discos planos sobre el agua.
        for (int i = 0; i < 70; i++)
        {
            Vector3 position = new Vector3(Mathf.Lerp(-45f, 45f, (float)rng.NextDouble()), -4.0f, Mathf.Lerp(-20f, 270f, (float)rng.NextDouble()));
            float size = 1f + (float)rng.NextDouble() * 1.2f;
            NoShadow(CreateVisualPart("Nenufar " + (i + 1), level, PrimitiveType.Cylinder, position, new Vector3(size, 0.02f, size), Vector3.zero, pad));
        }

        // Mojones de piedra apilada en las orillas de cada meseta (guían el camino).
        foreach (ZoneInfo zone in ThemedZones)
        {
            if (zone.Name == "Pasillo")
            {
                continue;
            }

            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? -1f : 1f;
                CreateRockPile(level, "Mojon " + zone.Name + " " + (k + 1), new Vector3(zone.X + side * (zone.Width / 2f - 1.2f), zone.Y, zone.Z + (k == 0 ? 4f : -6f) * (zone.Depth / 30f)), 1.4f, materials.StoneTop, rng);
            }
        }

        // Neblina de rocío sobre el agua.
        CreateMotes("Rocio", level, new Vector3(0f, -2f, 120f), new Vector3(100f, 2f, 300f), materials.Star, new Color(1.2f, 1.3f, 1.5f, 0.18f), 120, 8f, 5f, 0f);
    }

    // Cascada: lámina translúcida desde el borde del acantilado hasta el agua, con espuma y bruma en la base. Solo visual.
    private static void CreateWaterfall(Transform parent, string name, Vector3 top, float height, float width, Material sheet, Material foam, Material particleMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);

        GameObject body = CreateVisualPart("Lamina", root.transform, PrimitiveType.Cube, top + Vector3.down * height / 2f, new Vector3(0.8f, height, width), Vector3.zero, sheet);
        NoShadow(body);
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = new Vector3(top.x, -4f, top.z + (i - 1) * width * 0.3f);
            NoShadow(CreateVisualPart("Espuma " + (i + 1), root.transform, PrimitiveType.Sphere, position, new Vector3(width * 0.5f, 1.2f, width * 0.5f), Vector3.zero, foam));
        }

        CreateMotes("Bruma de cascada", root.transform, new Vector3(top.x, -2.5f, top.z), new Vector3(width, 2f, width), particleMaterial, new Color(1.4f, 1.6f, 1.8f, 0.3f), 60, 20f, 3f, 0.05f);
    }
}
