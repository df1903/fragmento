using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 4, "Jardín petrificado" (Nivel04_Jardin.unity): un jardín de piedra con árboles, arbustos y flores
///   convertidos en roca, estatuas de jardineros, una fuente seca, bruma verdosa y luciérnagas. Es de día, con cielo
///   cubierto: la primera luz "natural" del juego. Kael gana brazos y piernas de luz.
///
/// POR QUÉ EXISTE
///   Cuarto paso del arco de materialización: aparecen las primeras plantas (aún petrificadas) y las primeras figuras
///   humanas (las estatuas). Reutiliza el recorrido y los eventos del esqueleto común (FragmentoSceneBuilder.Niveles.cs).
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 4 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Los fragmentos hacen brotar troncos y raíces de luz que sirven de pasos.
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
                "Del suelo brotan troncos: un sendero cruza el barranco.",
                "Un puente de raíces une el claro con la terraza.",
                "Una plataforma de piedra florece y cruza el vacío.",
                "La compuerta de zarzas se hunde: el sendero está libre.",
                "Troncos nuevos suben hacia la glorieta.",
                "Los últimos árboles crecen hacia la cima del jardín.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            Chain1Name = "Troncos del barranco",
            Bridge2Name = "Puente de raices",
            Platform3Name = "Plataforma floral",
            Door4Name = "Compuerta de zarzas",
            Chain5Name = "Troncos de la glorieta",
            Chain6Name = "Troncos de la cima",
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
            HazardTier = 1,
            Decorate = DecorateJardin,
        };
    }

    private static void DecorateJardin(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(4);
        Material trunk = GetOrCreateMaterial("JarTronco", new Color(0.22f, 0.22f, 0.21f), null, 0f, 0.15f);
        Material leaves = GetOrCreateMaterial("JarHojaPetrea", new Color(0.28f, 0.4f, 0.3f), null, 0f, 0.15f);
        Material stem = GetOrCreateMaterial("JarTallo", new Color(0.3f, 0.45f, 0.3f), null, 0f, 0.1f);

        for (int i = 0; i < ThemedZones.Length; i++)
        {
            ZoneInfo zone = ThemedZones[i];
            if (zone.Name == "Pasillo")
            {
                continue;
            }

            float edgeX = zone.Width / 2f - 1f;
            float depthScale = zone.Depth / 30f;

            // Árboles petrificados en los costados (sin collider: quedan al borde, fuera del camino).
            float[] treeSlots = { -11f, -2f, 9f };
            for (int k = 0; k < treeSlots.Length; k++)
            {
                float side = k % 2 == 0 ? -1f : 1f;
                CreateTree(level, "Arbol " + zone.Name + " " + (k + 1), new Vector3(zone.X + side * edgeX, zone.Y, zone.Z + treeSlots[k] * depthScale),
                    5f + (float)rng.NextDouble() * 4f, trunk, leaves, rng, false);
            }

            // Arbustos y flores en las orillas.
            for (int k = 0; k < 6; k++)
            {
                float side = k % 2 == 0 ? -1f : 1f;
                float x = zone.X + side * (edgeX - 1.5f - (float)rng.NextDouble() * 2f);
                float z = zone.Z + Mathf.Lerp(-12f, 12f, (float)rng.NextDouble()) * depthScale;
                if (k < 3)
                {
                    CreateBush(level, "Arbusto " + zone.Name + " " + (k + 1), new Vector3(x, zone.Y, z), 1.4f + (float)rng.NextDouble(), leaves, rng);
                }
                else
                {
                    CreateFlower(level, "Flor " + zone.Name + " " + (k - 2), new Vector3(x, zone.Y, z), 0.8f + (float)rng.NextDouble() * 0.6f, stem, theme.Accent);
                }
            }

            // Estatuas de jardineros en las zonas amplias: las primeras figuras humanas del juego.
            if (zone.Width >= 30f && zone.Name != "Zona 7")
            {
                CreateStatue(level, "Estatua " + zone.Name + " A", new Vector3(zone.X - (edgeX - 3f), zone.Y, zone.Z - 9f * depthScale), 20f, materials.Ruin);
                CreateStatue(level, "Estatua " + zone.Name + " B", new Vector3(zone.X + (edgeX - 3f), zone.Y, zone.Z + 5f * depthScale), -35f, materials.Ruin);
            }
        }

        // Portal vegetal en dos zonas.
        CreateArch(level, "Glorieta 3", new Vector3(0f, 5f, 75f), 12f, 6f, materials.StoneTop);
        CreateArch(level, "Glorieta 6", new Vector3(0f, 9f, 187f), 12f, 6f, materials.StoneTop);

        // Fuente seca en la zona de entrada (fuera del camino).
        CreateVisualPart("Fuente base", level, PrimitiveType.Cylinder, new Vector3(11f, 0.2f, 11f), new Vector3(3.6f, 0.2f, 3.6f), Vector3.zero, materials.Ruin);
        CreateVisualPart("Fuente pilar", level, PrimitiveType.Cylinder, new Vector3(11f, 1f, 11f), new Vector3(0.7f, 0.8f, 0.7f), Vector3.zero, materials.StoneTop);
        CreateVisualPart("Fuente copa", level, PrimitiveType.Cylinder, new Vector3(11f, 1.9f, 11f), new Vector3(1.8f, 0.12f, 1.8f), Vector3.zero, materials.Ruin);

        // Bosque petrificado en el valle (altos, para que asomen sobre el borde de las mesetas).
        for (int i = 0; i < 90; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 position = new Vector3(side * (28f + (float)rng.NextDouble() * 70f), theme.FloorY, Mathf.Lerp(-30f, 290f, (float)rng.NextDouble()));
            CreateTree(level, "Arbol del valle " + (i + 1), position, 10f + (float)rng.NextDouble() * 12f, trunk, leaves, rng, false);
        }

        // Árbol colosal tras la cima: marca el destino desde lejos.
        CreateColumn(level, "Arbol colosal tronco", new Vector3(0f, theme.FloorY, 300f), 55f, 9f, trunk, false);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 14f, 58f + (i % 2) * 6f, 300f + Mathf.Sin(angle) * 14f);
            NoShadow(CreateVisualPart("Arbol colosal copa " + (i + 1), level, PrimitiveType.Sphere, position, Vector3.one * 28f, Vector3.zero, leaves));
        }

        // Bruma baja y luciérnagas.
        CreateMotes("Bruma", level, new Vector3(0f, -2f, 120f), new Vector3(90f, 3f, 300f), materials.Star, new Color(0.9f, 1.1f, 1f, 0.22f), 120, 8f, 5f, 0f);
        CreateMotes("Luciernagas", level, new Vector3(0f, 6f, 120f), new Vector3(60f, 10f, 290f), materials.Star, new Color(1.8f, 2.2f, 0.8f, 0.9f), 220, 14f, 0.12f, 0.01f);
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
        CreateVisualPart("Brazo derecho", statue.transform, PrimitiveType.Capsule, new Vector3(0.45f, 1.8f, 0.15f), new Vector3(0.16f, 0.45f, 0.16f), new Vector3(60f, 0f, -10f), stone);
        CreateVisualPart("Herramienta", statue.transform, PrimitiveType.Cube, new Vector3(0.5f, 1.9f, 0.55f), new Vector3(0.06f, 0.06f, 1.1f), Vector3.zero, stone);
    }
}
