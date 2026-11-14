using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 3, "Observatorio en ruinas" (Nivel03_Observatorio.unity): terrazas de mármol sobre un abismo
///   estrellado, columnas rotas, arcos, un telescopio y una luna enorme. Kael gana torso y cabeza de luz.
///
/// POR QUÉ EXISTE
///   Es el tercer paso del arco de materialización: la geometría pura del nivel 1 y la roca del nivel 2 dan paso a
///   arquitectura humana (columnas, arcos, instrumentos). Reutiliza el recorrido y los eventos del esqueleto común
///   (FragmentoSceneBuilder.Niveles.cs); aquí solo está lo que cambia: paleta, luz y decorado.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 3 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Los fragmentos hacen alzarse columnas desde el abismo (CrystalGrow con mármol).
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
                "Del abismo se alzan columnas: un camino de mármol cruza el vacío.",
                "Un puente astral une la plaza con la galería alta.",
                "Una plataforma de latón despierta y cruza el vacío.",
                "La compuerta de sombras se hunde: la galería está libre.",
                "Columnas de mármol ascienden hacia la sala de la cúpula.",
                "Las últimas columnas se alzan hacia la cima del observatorio.",
                "El Núcleo de la Realidad vuelve a brillar.",
            },
            Chain1Name = "Columnas del abismo",
            Bridge2Name = "Puente astral",
            Platform3Name = "Plataforma de laton",
            Door4Name = "Compuerta de sombras",
            Chain5Name = "Columnas de la cupula",
            Chain6Name = "Columnas de la cima",
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
            HazardTier = 0,
            Decorate = DecorateObservatorio,
        };
    }

    private static void DecorateObservatorio(Transform level, Materials materials, LevelTheme theme)
    {
        System.Random rng = new System.Random(3);

        Material moon = GetOrCreateMaterial("ObsLuna", new Color(0.9f, 0.92f, 1f), new Color(0.8f, 0.85f, 1f), 2.4f, 0.3f);
        CreateSkyDisc(level, "Luna", new Vector3(-110f, 85f, 360f), 55f, moon);

        // Columnas rotas en los costados de cada zona (fuera del camino central) y arcos en la entrada de las zonas grandes.
        for (int i = 0; i < ThemedZones.Length; i++)
        {
            ZoneInfo zone = ThemedZones[i];
            if (zone.Name == "Pasillo")
            {
                continue;
            }

            float edgeX = zone.Width / 2f - 0.9f;
            float[] slots = { -10f, -3f, 4f, 10f };
            for (int k = 0; k < slots.Length; k++)
            {
                float side = k % 2 == 0 ? -1f : 1f;
                bool broken = rng.NextDouble() < 0.5;
                float height = broken ? 1.5f + (float)rng.NextDouble() * 2.5f : 6f + (float)rng.NextDouble() * 3f;
                float z = zone.Z + slots[k] * (zone.Depth / 30f);
                CreateColumn(level, "Columna " + zone.Name + " " + (k + 1), new Vector3(zone.X + side * edgeX, zone.Y, z), height, 1.3f, materials.StoneTop, false);

                if (broken)
                {
                    // Trozo caído a los pies de la columna rota.
                    GameObject drum = CreateVisualPart("Tambor caido", level, PrimitiveType.Cylinder,
                        new Vector3(zone.X + side * (edgeX - 1.8f), zone.Y + 0.4f, z + 1f), new Vector3(1.2f, 0.5f, 1.2f), new Vector3(0f, 0f, 80f), materials.Ruin);
                    NoShadow(drum);
                }
            }

            if (zone.Name != "Zona 7")
            {
                CreateArch(level, "Arco " + zone.Name, new Vector3(zone.X, zone.Y, zone.Z - zone.Depth / 2f + 2f), 12f, 7f, materials.StoneTop);
            }
        }

        // Telescopio de la cima: base, tubo inclinado hacia la luna y anillos de latón.
        Vector3 scope = new Vector3(-8f, 14f, 232f);
        CreateVisualPart("Telescopio base", level, PrimitiveType.Cylinder, scope + Vector3.up * 0.9f, new Vector3(1.6f, 0.9f, 1.6f), Vector3.zero, materials.Ruin);
        GameObject tube = CreateVisualPart("Telescopio tubo", level, PrimitiveType.Cylinder, scope + new Vector3(-0.8f, 3.2f, 1.4f), new Vector3(1f, 2.4f, 1f), new Vector3(55f, 20f, 0f), materials.StoneTop);
        NoShadow(tube);
        CreateVisualPart("Telescopio anillo", level, PrimitiveType.Cylinder, scope + new Vector3(-0.5f, 2.6f, 0.8f), new Vector3(1.15f, 0.08f, 1.15f), new Vector3(55f, 20f, 0f), theme.Accent);

        // Torres en ruinas a lo lejos, con ventanas encendidas.
        for (int i = 0; i < 10; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float height = 30f + (float)rng.NextDouble() * 40f;
            Vector3 position = new Vector3(side * (60f + (float)rng.NextDouble() * 40f), height / 2f - 30f, Mathf.Lerp(-20f, 280f, (float)rng.NextDouble()));
            GameObject tower = CreateBlock("Torre lejana " + (i + 1), level, position, new Vector3(9f, height, 9f), materials.Ruin);
            Object.DestroyImmediate(tower.GetComponent<Collider>());
            NoShadow(tower);
            for (int w = 0; w < 3; w++)
            {
                Vector3 window = position + new Vector3(-side * 4.6f, Mathf.Lerp(-height / 2f + 8f, height / 2f - 4f, (float)rng.NextDouble()), (w - 1) * 2.6f);
                NoShadow(CreateVisualPart("Ventana", level, PrimitiveType.Cube, window, new Vector3(0.2f, 1.4f, 1f), Vector3.zero, theme.Accent));
            }
        }

        // Escombros flotando en el vacío (fragmentos de la cúpula caída).
        for (int i = 0; i < 40; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 position = new Vector3(side * (28f + (float)rng.NextDouble() * 50f), Mathf.Lerp(-12f, 45f, (float)rng.NextDouble()), Mathf.Lerp(-20f, 270f, (float)rng.NextDouble()));
            float size = 1f + (float)rng.NextDouble() * 3f;
            GameObject debris = CreateVisualPart("Escombro " + (i + 1), level, PrimitiveType.Cube, position, new Vector3(size, size * 0.7f, size),
                new Vector3((float)rng.NextDouble() * 60f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 60f), materials.Ruin);
            NoShadow(debris);
        }

        // Polvo dorado en suspensión sobre todo el recorrido.
        CreateMotes("Polvo dorado", level, new Vector3(0f, 8f, 120f), new Vector3(70f, 18f, 290f), materials.Star, new Color(1.6f, 1.2f, 0.7f, 0.7f), 300, 18f, 0.1f, 0.02f);
    }
}
