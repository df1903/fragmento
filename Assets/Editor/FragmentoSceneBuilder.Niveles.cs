using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   Esqueleto común de los niveles 3, 4 y 5 (Observatorio, Jardín petrificado, Lago y cascadas) y de las etapas
///   de materialización de Kael (torso y cabeza, brazos y piernas, ropa).
///
/// POR QUÉ EXISTE
///   Los tres niveles comparten el recorrido probado del nivel 2 (siete zonas hacia +Z, mismas alturas y saltos,
///   mismos eventos y peligros) y cambian lo que el jugador ve: paleta, luz, niebla, decorado y forma de Kael.
///   Así solo se escribe una vez la geometría delicada (saltos, huecos) y cada nivel aporta su "escenario".
///
/// CÓMO SE USA
///   Cada nivel (FragmentoSceneBuilder.Nivel03/04/05.cs) define un LevelTheme y llama a BuildThemedLevel.
///   Menús: Fragmento > Construir Nivel 3 / 4 / 5 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    // Identificadores de evento comunes a los niveles temáticos (cada escena tiene los suyos: no chocan entre niveles).
    private static readonly string[] ThemedEventIds = { "cristal_1", "puente_2", "plataforma_3", "puerta_4", "cristal_5", "cristal_6", "nucleo_7" };

    // Todo lo que cambia entre niveles temáticos.
    private class LevelTheme
    {
        // Archivos y nombres
        public string ScenePath;
        public string VolumePath;
        public string NextScenePath = "";
        public string FilePrefix;       // prefijo de los FragmentData ("N3_")
        public string KaelPrefabName;
        public int KaelStage;           // 3 torso y cabeza, 4 brazos y piernas, 5 ropa
        public bool KaelRings;
        public string[] FragmentFiles;
        public string[] FragmentTitles;
        public string[] FragmentMessages;
        public string Chain1Name, Bridge2Name, Platform3Name, Door4Name, Chain5Name, Chain6Name;

        // Materiales propios (el resto se toma de Materials)
        public Material Growth;         // lo que brota con los fragmentos: columnas, troncos, piedras; puentes y plataforma activa
        public Material Accent;         // detalle decorativo emisivo
        public Material Water;          // agua (solo nivel 5)

        // Entorno
        public Color Background, FogColor, SunColor, AmbientSky, AmbientEquator, AmbientGround, ZoneLightColor, VolumeFilter;
        public float FogDensity, SunIntensity;
        public Vector3 SunEuler;
        public float ChainBaseY = -4f;  // base de las cadenas que crecen (el vacío del observatorio es más profundo)
        public float KillY = -5f;
        public float FloorY = float.NaN; // suelo decorativo bajo las zonas (NaN = vacío)
        public Material FloorMaterial;
        public bool Stars;
        public int HazardTier;          // 0 = peligros del nivel 2; 1 y 2 añaden más

        // Decorado propio del nivel (sin colliders salvo piezas sólidas pequeñas).
        public System.Action<Transform, Materials, LevelTheme> Decorate;
    }

    // Zona del recorrido: centro X, altura de la superficie Y, centro Z, ancho, fondo y grosor del bloque.
    private struct ZoneInfo
    {
        public string Name;
        public float X, Y, Z, Width, Depth, Thickness;

        public ZoneInfo(string name, float x, float y, float z, float width, float depth, float thickness)
        {
            Name = name; X = x; Y = y; Z = z; Width = width; Depth = depth; Thickness = thickness;
        }
    }

    // Recorrido del nivel 2 (superficies: Z1 y Z2 en y = 0; Z3, Z4 y pasillo en 5; Z6 en 9; Z7 en 14).
    private static readonly ZoneInfo[] ThemedZones =
    {
        new ZoneInfo("Zona 1", 0f, 0f, 0f, 30f, 30f, 2f),
        new ZoneInfo("Zona 2", 0f, 0f, 48f, 36f, 30f, 2f),
        new ZoneInfo("Zona 3", 0f, 5f, 88f, 36f, 30f, 6f),
        new ZoneInfo("Zona 4", 0f, 5f, 134f, 36f, 30f, 6f),
        new ZoneInfo("Pasillo", 0f, 5f, 159f, 8f, 20f, 6f),
        new ZoneInfo("Zona 6", 0f, 9f, 200f, 36f, 30f, 6f),
        new ZoneInfo("Zona 7", 0f, 14f, 241f, 24f, 20f, 6f),
    };

    // ------------------------------------------------------------------ Construcción

    private static void BuildThemedLevel(System.Func<Materials, LevelTheme> createTheme)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Fragmento: sal del modo Play antes de construir la escena.");
            return;
        }

        EnsureFolder(MaterialsFolder);
        EnsureFolder(PrefabsFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(SettingsFolder);
        EnsureFolder(ScenesFolder);

        Materials materials = CreateMaterials();
        LevelTheme theme = createTheme(materials);

        if (!Application.isBatchMode)
        {
            if (File.Exists(theme.ScenePath) && !EditorUtility.DisplayDialog(
                    "Construir nivel",
                    "Se va a regenerar " + Path.GetFileName(theme.ScenePath) + ". Los cambios hechos a mano en esa escena se perderán.\n\n¿Continuar?",
                    "Continuar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
        }

        // Peligros: mismos enemigos oscuros con ojos rojos en todos los niveles (rojo = peligro).
        CaveMaterials danger = CreateCaveMaterials();
        CaveMaterials cave = new CaveMaterials
        {
            CrystalGreen = theme.Growth,
            CrystalMagenta = theme.Accent,
            Lake = theme.Water,
            EnemyRock = danger.EnemyRock,
            TurretCrystal = danger.TurretCrystal,
            Projectile = danger.Projectile,
        };

        FragmentData[] fragments = new FragmentData[7];
        for (int i = 0; i < fragments.Length; i++)
        {
            fragments[i] = CreateFragmentData(i + 1, theme.FilePrefix + "Fragmento0" + (i + 1) + "_" + theme.FragmentFiles[i],
                theme.FragmentTitles[i], theme.FragmentMessages[i], ThemedEventIds[i], new Color(1f, 0.75f, 0.25f));
        }

        GameObject kaelPrefab = CreateKaelPrefab(materials, theme.KaelPrefabName, theme.KaelRings, theme.KaelStage);
        GameObject fragmentPrefab = CreateFragmentPrefab(materials.Fragment);
        TurretProjectile projectilePrefab = CreateProjectilePrefab(danger.Projectile);
        VolumeProfile volumeProfile = CreateThemeVolumeProfile(theme);

        BuildThemedScene(theme, materials, cave, fragments, kaelPrefab, fragmentPrefab, projectilePrefab, volumeProfile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fragmento: nivel construido en " + theme.ScenePath);
    }

    private static VolumeProfile CreateThemeVolumeProfile(LevelTheme theme)
    {
        VolumeProfile profile = CreateVolumeProfile(theme.VolumePath);
        if (profile.TryGet(out ColorAdjustments adjustments))
        {
            adjustments.colorFilter.Override(theme.VolumeFilter);
            EditorUtility.SetDirty(adjustments);
        }
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void BuildThemedScene(LevelTheme theme, Materials materials, CaveMaterials cave, FragmentData[] fragments,
        GameObject kaelPrefab, GameObject fragmentPrefab, TurretProjectile projectilePrefab, VolumeProfile volumeProfile)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureThemeEnvironment(theme);
        CreateMainCamera();
        Camera.main.backgroundColor = theme.Background;
        CreateGlobalVolume(volumeProfile);
        if (theme.Stars)
        {
            CreateStars(materials.Star);
        }

        GameObject level = new GameObject("Nivel");
        BuildThemedZones(level.transform, materials, theme);
        theme.Decorate(level.transform, materials, theme);

        // --- Jugador y cámara ---
        Vector3 spawn = new Vector3(0f, 0.05f, -10f);
        GameObject kael = (GameObject)PrefabUtility.InstantiatePrefab(kaelPrefab);
        kael.transform.position = spawn;
        CreatePlayerCamera(kael.transform.Find("CameraTarget"));

        // --- Fragmentos (cada uno a +1.3 m sobre el suelo de su zona) ---
        PlaceFragment(fragmentPrefab, "Fragmento 1", new Vector3(-6f, 1.3f, 8f), fragments[0]);
        PlaceFragment(fragmentPrefab, "Fragmento 2", new Vector3(-26f, 1.3f, 57f), fragments[1]);
        PlaceFragment(fragmentPrefab, "Fragmento 3", new Vector3(-10f, 6.3f, 95f), fragments[2]);
        PlaceFragment(fragmentPrefab, "Fragmento 4", new Vector3(10f, 6.3f, 130f), fragments[3]);
        PlaceFragment(fragmentPrefab, "Fragmento 5", new Vector3(0f, 6.3f, 164f), fragments[4]);
        PlaceFragment(fragmentPrefab, "Fragmento 6", new Vector3(10f, 10.3f, 200f), fragments[5]);
        PlaceFragment(fragmentPrefab, "Fragmento 7", new Vector3(0f, 15.3f, 236f), fragments[6]);

        // --- Eventos de mundo (misma geometría que el nivel 2) ---
        float b = theme.ChainBaseY;
        GameObject chain1 = CreateCrystalChain(theme.Chain1Name, "cristal_1", theme.Growth, b, 5f, 3.2f,
            new[] { new Vector3(0f, 0f, 17.5f), new Vector3(0f, 0.5f, 21.5f), new Vector3(0f, 0f, 25.5f), new Vector3(0f, 0.5f, 29.5f) });
        GameObject bridge2 = CreateBridge(theme.Bridge2Name, new Vector3(0f, 0f, 62.9f), new Vector3(0f, 5f, 73.1f), 4f, "puente_2", theme.Growth);
        GameObject platform3 = CreateMovingPlatform(theme.Platform3Name, "plataforma_3", new Vector3(0f, 4.7f, 106f), new Vector3(0f, 0f, 16f), materials);
        GameObject door4 = CreateDoor(theme.Door4Name, "puerta_4", new Vector3(0f, 6.9f, 146f), materials);
        GameObject chain5 = CreateCrystalChain(theme.Chain5Name, "cristal_5", theme.Growth, b, 8f, 4f,
            new[] { new Vector3(0f, 6f, 172f), new Vector3(0f, 7f, 177f), new Vector3(0f, 8f, 182f) });
        GameObject chain6 = CreateCrystalChain(theme.Chain6Name, "cristal_6", theme.Growth, b, 6f, 3.6f,
            new[] { new Vector3(0f, 10.4f, 217.8f), new Vector3(0f, 11.8f, 222.6f), new Vector3(0f, 13.2f, 227.4f) });
        GameObject core = CreateCoreAndPortal(materials, new Vector3(-2f, 1.5f, 64f));

        ConfigureEvent(chain1, chain1.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(bridge2, bridge2.transform, theme.Growth);
        ConfigureEvent(platform3, platform3.transform, theme.Growth);
        ConfigureEvent(door4, door4.transform.Find("Losa"), materials.Door);
        ConfigureEvent(chain5, chain5.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(chain6, chain6.transform.GetChild(1).GetChild(0), theme.Growth);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        CreateEventCamera();

        // --- Peligros ---
        BuildHazards(level.transform, materials, cave, projectilePrefab);
        BuildExtraHazards(level.transform, materials, cave, projectilePrefab, theme.HazardTier);

        // --- Checkpoints (una zona = un punto seguro) ---
        CreateCheckpoint("Checkpoint 2", new Vector3(0f, 3f, 48f), new Vector3(30f, 8f, 28f), new Vector3(0f, 0.05f, 38f));
        CreateCheckpoint("Checkpoint 3", new Vector3(0f, 8f, 88f), new Vector3(30f, 8f, 28f), new Vector3(0f, 5.05f, 78f));
        CreateCheckpoint("Checkpoint 4", new Vector3(0f, 8f, 130f), new Vector3(30f, 8f, 22f), new Vector3(0f, 5.05f, 122f));
        CreateCheckpoint("Checkpoint 6", new Vector3(0f, 12f, 200f), new Vector3(30f, 8f, 28f), new Vector3(0f, 9.05f, 190f));
        CreateCheckpoint("Checkpoint 7", new Vector3(0f, 17f, 241f), new Vector3(22f, 8f, 18f), new Vector3(0f, 14.05f, 232f));

        // --- Caída, respawn y gestores ---
        GameObject killZone = new GameObject("Zona de caida");
        killZone.transform.position = new Vector3(0f, theme.KillY, 120f);
        BoxCollider killCollider = killZone.AddComponent<BoxCollider>();
        killCollider.size = new Vector3(600f, 2f, 600f);
        killCollider.isTrigger = true;
        killZone.AddComponent<KillZone>();

        GameObject spawnPoint = new GameObject("Inicio");
        spawnPoint.transform.position = spawn;

        GameObject respawn = new GameObject("Gestor de Respawn");
        RespawnManager respawnManager = respawn.AddComponent<RespawnManager>();
        SerializedObject respawnSerialized = new SerializedObject(respawnManager);
        Property(respawnSerialized, "initialSpawn").objectReferenceValue = spawnPoint.transform;
        respawnSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject managers = new GameObject("Gestor de Fragmentos");
        managers.AddComponent<FragmentManager>();
        managers.AddComponent<LivesManager>();

        CreateHud(theme.NextScenePath, 7);

        EditorSceneManager.SaveScene(scene, theme.ScenePath);
        RegisterScenesInBuildSettings();
    }

    private static void ConfigureThemeEnvironment(LevelTheme theme)
    {
        GameObject sun = new GameObject("Luz Direccional");
        sun.transform.rotation = Quaternion.Euler(theme.SunEuler);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = theme.SunColor;
        light.intensity = theme.SunIntensity;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        RenderSettings.sun = light;

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = theme.AmbientSky;
        RenderSettings.ambientEquatorColor = theme.AmbientEquator;
        RenderSettings.ambientGroundColor = theme.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = theme.FogDensity;
        RenderSettings.fogColor = theme.FogColor;
    }

    // Zonas, piedras del lago, muros de la compuerta y coberturas: la geometría jugable del nivel 2, con los materiales del tema.
    private static void BuildThemedZones(Transform level, Materials materials, LevelTheme theme)
    {
        foreach (ZoneInfo zone in ThemedZones)
        {
            Vector3 center = new Vector3(zone.X, zone.Y - zone.Thickness / 2f, zone.Z);
            Vector3 size = new Vector3(zone.Width, zone.Thickness, zone.Depth);
            if (zone.Name == "Pasillo")
            {
                CreateBlock("Pasillo", level, center, size, materials.StoneTop);
            }
            else
            {
                CreateIsland(zone.Name, level, center, size, materials);
            }

            // Cimientos decorativos: la zona nace del suelo (valle, lecho del lago) en vez de flotar.
            if (!float.IsNaN(theme.FloorY))
            {
                float bottom = zone.Y - zone.Thickness;
                float height = bottom - theme.FloorY;
                if (height > 0.1f)
                {
                    GameObject foundation = CreateBlock(zone.Name + " cimientos", level, new Vector3(zone.X, theme.FloorY + height / 2f, zone.Z),
                        new Vector3(zone.Width - 3f, height, zone.Depth - 3f), materials.Stone);
                    Object.DestroyImmediate(foundation.GetComponent<Collider>());
                    NoShadow(foundation);
                }
            }
        }

        // Suelo decorativo lejano (valle o lecho).
        if (!float.IsNaN(theme.FloorY) && theme.FloorMaterial != null)
        {
            GameObject floor = CreateBlock("Suelo", level, new Vector3(0f, theme.FloorY - 0.5f, 120f), new Vector3(700f, 1f, 700f), theme.FloorMaterial);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            NoShadow(floor);
        }

        // Piedras hacia el Fragmento 2 (saltos de 1 a 1.5 m).
        CreateIsland("Piedra A", level, new Vector3(-21f, -1f, 46f), new Vector3(3f, 2f, 3f), materials);
        CreateIsland("Piedra B", level, new Vector3(-25f, -1f, 50f), new Vector3(3f, 2f, 3f), materials);
        CreateIsland("Islote del Fragmento 2", level, new Vector3(-26f, -1f, 57f), new Vector3(8f, 2f, 8f), materials);

        // Muros de la compuerta del Fragmento 4 (hueco de 4 m en x -2..2).
        CreateBlock("Muro compuerta izquierdo", level, new Vector3(-10f, 7f, 146f), new Vector3(16f, 4f, 1.5f), materials.Ruin);
        CreateBlock("Muro compuerta derecho", level, new Vector3(10f, 7f, 146f), new Vector3(16f, 4f, 1.5f), materials.Ruin);
        CreateBlock("Dintel compuerta", level, new Vector3(0f, 8.5f, 146f), new Vector3(4f, 1f, 1.5f), materials.Ruin);

        // Coberturas (3 m de alto): el jugador se esconde de las torretas detrás de ellas.
        CreateBlock("Cobertura 3A", level, new Vector3(-4f, 6.5f, 88f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 3B", level, new Vector3(6f, 6.5f, 97f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 4A", level, new Vector3(-5f, 6.5f, 132f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 4B", level, new Vector3(5f, 6.5f, 128f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 4C", level, new Vector3(0f, 6.5f, 138f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 6A", level, new Vector3(-6f, 10.5f, 198f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 6B", level, new Vector3(6f, 10.5f, 202f), new Vector3(3f, 3f, 3f), materials.Ruin);
        CreateBlock("Cobertura 6C", level, new Vector3(0f, 10.5f, 206f), new Vector3(3f, 3f, 3f), materials.Ruin);

        // Luz suave por zona: da color al ambiente sin tocar los enemigos (que llevan su luz roja).
        foreach (ZoneInfo zone in ThemedZones)
        {
            CreateZoneLight(level, new Vector3(zone.X, zone.Y + 7f, zone.Z), theme.ZoneLightColor, 24f, 3f);
        }
    }

    private static void CreateZoneLight(Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        GameObject lightObject = new GameObject("Luz de zona");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
    }

    // Peligros extra para los niveles más avanzados (tier 1 y 2); el tier 0 usa solo los del nivel 2.
    private static void BuildExtraHazards(Transform level, Materials materials, CaveMaterials cave, TurretProjectile projectile, int tier)
    {
        if (tier >= 1)
        {
            CreateRockShadow("Sombra 2C", level, new Vector3(14f, 0.9f, 47f), new Vector3(-14f, 0.9f, 47f), IslandArea(0f, 0f, 48f, 36f, 30f), materials, cave);
            CreateCrystalTurret("Torreta 3B", level, new Vector3(15f, 5f, 81f), 1, 2.8f, projectile, materials, cave);
        }

        if (tier >= 2)
        {
            CreateRockShadow("Sombra 4B", level, new Vector3(-12f, 5.9f, 143f), new Vector3(12f, 5.9f, 143f), IslandArea(0f, 5f, 134f, 36f, 30f), materials, cave);
            CreateRockShadow("Sombra 6B", level, new Vector3(12f, 9.9f, 190f), new Vector3(-12f, 9.9f, 190f), IslandArea(0f, 9f, 200f, 36f, 30f), materials, cave);
        }
    }

    // ------------------------------------------------------------------ Kael por etapas

    // Altura del cuerpo según la etapa: la esfera mide 1 m y la silueta completa 1.7 m.
    private static float KaelBodyHeight(int stage)
    {
        if (stage >= 5) return 1.7f;
        if (stage == 4) return 1.55f;
        if (stage == 3) return 1.25f;
        return 1f;
    }

    // Etapa 3: torso y cabeza sobre una cola de luz. Etapa 4: + brazos y piernas. Etapa 5: + túnica, cinturón, bufanda y capa.
    // Todo son primitivas sin collider (el CharacterController es el collider).
    private static void CreateKaelHumanoid(Transform visual, Materials materials, int stage)
    {
        Material light = materials.Kael;
        Material cloth = materials.Cloth;

        if (stage == 3)
        {
            CreateVisualPart("Cola", visual, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), new Vector3(0.32f, 0.5f, 0.32f), Vector3.zero, light);
            CreateVisualPart("Torso", visual, PrimitiveType.Capsule, new Vector3(0f, 0.72f, 0f), new Vector3(0.5f, 0.3f, 0.34f), Vector3.zero, light);
            CreateVisualPart("Cabeza", visual, PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.3f, Vector3.zero, light);
            return;
        }

        float s = stage >= 5 ? 1.1f : 1f; // la silueta de la etapa 5 es un 10 % más alta
        CreateVisualPart("Pierna izquierda", visual, PrimitiveType.Capsule, new Vector3(-0.12f, 0.34f, 0f) * s, new Vector3(0.16f, 0.34f, 0.16f) * s, Vector3.zero, light);
        CreateVisualPart("Pierna derecha", visual, PrimitiveType.Capsule, new Vector3(0.12f, 0.34f, 0f) * s, new Vector3(0.16f, 0.34f, 0.16f) * s, Vector3.zero, light);
        CreateVisualPart("Torso", visual, PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f) * s, new Vector3(0.4f, 0.3f, 0.26f) * s, Vector3.zero, light);
        CreateVisualPart("Brazo izquierdo", visual, PrimitiveType.Capsule, new Vector3(-0.3f, 0.95f, 0f) * s, new Vector3(0.11f, 0.28f, 0.11f) * s, new Vector3(0f, 0f, -6f), light);
        CreateVisualPart("Brazo derecho", visual, PrimitiveType.Capsule, new Vector3(0.3f, 0.95f, 0f) * s, new Vector3(0.11f, 0.28f, 0.11f) * s, new Vector3(0f, 0f, 6f), light);
        CreateVisualPart("Cabeza", visual, PrimitiveType.Sphere, new Vector3(0f, 1.4f, 0f) * s, Vector3.one * 0.3f * s, Vector3.zero, light);

        if (stage >= 5)
        {
            CreateVisualPart("Tunica", visual, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.28f, 0.5f), Vector3.zero, cloth);
            CreateVisualPart("Cinturon", visual, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.46f, 0.06f, 0.32f), Vector3.zero, light);
            CreateVisualPart("Bufanda", visual, PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(0.3f, 0.07f, 0.3f), Vector3.zero, cloth);
            CreateVisualPart("Capa", visual, PrimitiveType.Cube, new Vector3(0f, 1.0f, -0.2f), new Vector3(0.46f, 0.85f, 0.05f), new Vector3(10f, 0f, 0f), cloth);
        }
    }

    // ------------------------------------------------------------------ Decorado reutilizable (solo visual)

    // Columna: cilindro (collider solo si "solid") con base y capitel. height = altura total.
    private static GameObject CreateColumn(Transform parent, string name, Vector3 basePosition, float height, float diameter, Material material, bool solid)
    {
        GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        column.name = name;
        column.transform.SetParent(parent, false);
        column.transform.position = basePosition + Vector3.up * height / 2f;
        column.transform.localScale = new Vector3(diameter, height / 2f, diameter);
        column.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid)
        {
            Object.DestroyImmediate(column.GetComponent<Collider>());
        }

        GameObject plinth = CreateVisualPart("Base", column.transform.parent, PrimitiveType.Cube, basePosition + Vector3.up * 0.15f, new Vector3(diameter * 1.5f, 0.3f, diameter * 1.5f), Vector3.zero, material);
        plinth.name = name + " base";
        return column;
    }

    // Arco: dos columnas sólidas y un dintel. width = separación entre columnas.
    private static void CreateArch(Transform parent, string name, Vector3 centerBase, float width, float height, Material material)
    {
        CreateColumn(parent, name + " izquierda", centerBase + Vector3.left * width / 2f, height, 1.4f, material, true);
        CreateColumn(parent, name + " derecha", centerBase + Vector3.right * width / 2f, height, 1.4f, material, true);
        GameObject lintel = CreateBlock(name + " dintel", parent, centerBase + Vector3.up * (height + 0.4f), new Vector3(width + 2f, 0.8f, 1.6f), material);
        Object.DestroyImmediate(lintel.GetComponent<Collider>());
    }

    // Árbol: tronco y copa de tres esferas. "solid" deja collider en el tronco.
    private static void CreateTree(Transform parent, string name, Vector3 basePosition, float height, Material trunk, Material leaves, System.Random rng, bool solid)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = basePosition;

        float girth = 0.5f + height * 0.04f;
        GameObject trunkObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkObject.name = "Tronco";
        trunkObject.transform.SetParent(tree.transform, false);
        trunkObject.transform.localPosition = new Vector3(0f, height / 2f, 0f);
        trunkObject.transform.localScale = new Vector3(girth, height / 2f, girth);
        trunkObject.GetComponent<Renderer>().sharedMaterial = trunk;
        if (!solid)
        {
            Object.DestroyImmediate(trunkObject.GetComponent<Collider>());
        }

        for (int i = 0; i < 3; i++)
        {
            float size = height * (0.45f + (float)rng.NextDouble() * 0.2f);
            Vector3 offset = new Vector3(((float)rng.NextDouble() - 0.5f) * height * 0.3f, height * (0.85f + i * 0.12f), ((float)rng.NextDouble() - 0.5f) * height * 0.3f);
            GameObject crown = CreateVisualPart("Copa " + (i + 1), tree.transform, PrimitiveType.Sphere, offset, Vector3.one * size, Vector3.zero, leaves);
            NoShadow(crown);
        }
    }

    // Arbusto: tres esferas aplastadas.
    private static void CreateBush(Transform parent, string name, Vector3 basePosition, float size, Material material, System.Random rng)
    {
        GameObject bush = new GameObject(name);
        bush.transform.SetParent(parent, false);
        bush.transform.position = basePosition;
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(((float)rng.NextDouble() - 0.5f) * size, size * 0.3f, ((float)rng.NextDouble() - 0.5f) * size);
            GameObject part = CreateVisualPart("Hoja " + (i + 1), bush.transform, PrimitiveType.Sphere, offset, new Vector3(size * 0.8f, size * 0.6f, size * 0.8f), Vector3.zero, material);
            NoShadow(part);
        }
    }

    // Flor: tallo fino y cabeza emisiva.
    private static void CreateFlower(Transform parent, string name, Vector3 basePosition, float height, Material stem, Material head)
    {
        GameObject flower = new GameObject(name);
        flower.transform.SetParent(parent, false);
        flower.transform.position = basePosition;
        NoShadow(CreateVisualPart("Tallo", flower.transform, PrimitiveType.Cylinder, new Vector3(0f, height / 2f, 0f), new Vector3(0.06f, height / 2f, 0.06f), Vector3.zero, stem));
        NoShadow(CreateVisualPart("Corola", flower.transform, PrimitiveType.Sphere, new Vector3(0f, height, 0f), new Vector3(0.35f, 0.18f, 0.35f), Vector3.zero, head));
    }

    // Montón de rocas: tres cubos girados.
    private static void CreateRockPile(Transform parent, string name, Vector3 basePosition, float size, Material material, System.Random rng)
    {
        GameObject pile = new GameObject(name);
        pile.transform.SetParent(parent, false);
        pile.transform.position = basePosition;
        for (int i = 0; i < 3; i++)
        {
            float part = size * (0.5f + (float)rng.NextDouble() * 0.6f);
            Vector3 offset = new Vector3(((float)rng.NextDouble() - 0.5f) * size, part * 0.35f, ((float)rng.NextDouble() - 0.5f) * size);
            Vector3 euler = new Vector3((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 90f, (float)rng.NextDouble() * 40f);
            NoShadow(CreateVisualPart("Roca " + (i + 1), pile.transform, PrimitiveType.Cube, offset, new Vector3(part, part * 0.8f, part), euler, material));
        }
    }

    // Disco luminoso lejano (luna, sol): esfera emisiva sin collider ni sombra.
    private static void CreateSkyDisc(Transform parent, string name, Vector3 position, float diameter, Material material)
    {
        GameObject disc = CreateVisualPart(name, parent, PrimitiveType.Sphere, position, Vector3.one * diameter, Vector3.zero, material);
        NoShadow(disc);
    }

    // Motas de luz o polvo flotando en una caja (luciérnagas, bruma, espuma). Barato: un solo sistema de partículas.
    private static void CreateMotes(string name, Transform parent, Vector3 center, Vector3 box, Material material, Color color, int maxParticles, float rate, float size, float lift)
    {
        GameObject motesObject = new GameObject(name);
        motesObject.transform.SetParent(parent, false);
        motesObject.transform.position = center;
        ParticleSystem motes = motesObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = motes.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startColor = color;
        main.gravityModifier = -lift;

        ParticleSystem.EmissionModule emission = motes.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = motes.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = motes.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        motesObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
    }
}
