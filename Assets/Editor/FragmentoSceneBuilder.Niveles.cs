using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   Parte común de los niveles 3, 4 y 5 (Observatorio, Jardín petrificado, Lago y cascadas): entorno, Kael por
///   etapas de materialización, gestores, HUD y decorado reutilizable.
///
/// POR QUÉ EXISTE
///   Lo que es igual en todos los niveles se escribe una vez aquí. Lo que hace distinto a cada nivel (la forma del
///   recorrido, sus dinámicas, sus peligros y sus eventos) lo define el propio nivel en su BuildLayout.
///   Antes los tres niveles copiaban la ruta del nivel 2 y se sentían iguales (decisión 2026-11-16).
///
/// CÓMO SE USA
///   Cada nivel (FragmentoSceneBuilder.Nivel03/04/05.cs) define un LevelTheme con sus zonas y su BuildLayout y
///   llama a BuildThemedLevel. Menús: Fragmento > Construir Nivel 3 / 4 / 5 (en el Editor; no por batchmode).
/// </summary>
public static partial class FragmentoSceneBuilder
{
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
        public string[] EventIds;       // evento de mundo que dispara cada fragmento (7)

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

        // Recorrido
        public Vector3 Spawn = new Vector3(0f, 0.05f, -10f);
        public ZoneInfo[] Zones;                           // suelos principales del nivel
        public Rect[] KeepOut = new Rect[0];               // franjas (x, z) del camino donde el decorado no debe estorbar
        public System.Action<LevelContext> BuildLayout;    // fragmentos, eventos, dinámicas, peligros y saltos del nivel

        // Decorado propio del nivel (sin colliders salvo piezas sólidas pequeñas).
        public System.Action<Transform, Materials, LevelTheme> Decorate;
    }

    // Lo que necesita un nivel para armar su recorrido.
    private class LevelContext
    {
        public Transform Level;
        public Materials Materials;
        public CaveMaterials Danger;        // materiales de sombras y torretas (iguales en todos los niveles: rojo = peligro)
        public LevelTheme Theme;
        public GameObject FragmentPrefab;
        public FragmentData[] Fragments;
        public TurretProjectile Projectile;
        public RouteBuilder Route;

        /// <summary>Coloca el fragmento "id" (1 a 7). La posición es su centro: 1.3 m sobre el suelo.</summary>
        public void Fragment(int id, Vector3 position)
        {
            PlaceFragment(FragmentPrefab, "Fragmento " + id, position, Fragments[id - 1]);
        }
    }

    // Zona del recorrido: centro X, altura de la superficie Y, centro Z, ancho, fondo y grosor del bloque.
    // "Plain" = bloque liso sin bordes luminosos (pasillos, cauces).
    private struct ZoneInfo
    {
        public string Name;
        public float X, Y, Z, Width, Depth, Thickness;
        public bool Plain;

        public ZoneInfo(string name, float x, float y, float z, float width, float depth, float thickness, bool plain = false)
        {
            Name = name; X = x; Y = y; Z = z; Width = width; Depth = depth; Thickness = thickness; Plain = plain;
        }
    }

    // Posición del núcleo y el portal: CreateCoreAndPortal los dibuja para la cima del nivel 1 (centro 2, 177; suelo
    // en 12.5) y recibe un desplazamiento. Esto lo calcula a partir del centro de la cima del nivel (superficie).
    // La cima debe medir al menos 24 x 20 m y se llega a ella por el sur: el fragmento 7 va 5 m al sur del centro.
    private static Vector3 CoreOffset(Vector3 summitSurfaceCenter)
    {
        return summitSurfaceCenter - new Vector3(2f, 12.5f, 177f);
    }

    // True si el punto (x, z) queda lejos de las zonas y del camino: ahí se puede poner decorado alto.
    private static bool IsClear(LevelTheme theme, float x, float z, float margin)
    {
        foreach (ZoneInfo zone in theme.Zones)
        {
            if (Mathf.Abs(x - zone.X) < zone.Width / 2f + margin && Mathf.Abs(z - zone.Z) < zone.Depth / 2f + margin)
            {
                return false;
            }
        }

        return !NearPath(theme, x, z, margin);
    }

    // True si el punto (x, z) cae sobre una franja del camino (theme.KeepOut) o a menos de "margin" metros de ella.
    private static bool NearPath(LevelTheme theme, float x, float z, float margin)
    {
        foreach (Rect strip in theme.KeepOut)
        {
            if (x > strip.xMin - margin && x < strip.xMax + margin && z > strip.yMin - margin && z < strip.yMax + margin)
            {
                return true;
            }
        }

        return false;
    }

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

        FragmentData[] fragments = new FragmentData[7];
        for (int i = 0; i < fragments.Length; i++)
        {
            fragments[i] = CreateFragmentData(i + 1, theme.FilePrefix + "Fragmento0" + (i + 1) + "_" + theme.FragmentFiles[i],
                theme.FragmentTitles[i], theme.FragmentMessages[i], theme.EventIds[i], new Color(1f, 0.75f, 0.25f));
        }

        // Peligros: mismos enemigos oscuros con ojos rojos en todos los niveles (rojo = peligro).
        CaveMaterials danger = CreateCaveMaterials();

        LevelContext context = new LevelContext
        {
            Materials = materials,
            Danger = danger,
            Theme = theme,
            Fragments = fragments,
            FragmentPrefab = CreateFragmentPrefab(materials.Fragment),
            Projectile = CreateProjectilePrefab(danger.Projectile),
        };

        GameObject kaelPrefab = CreateKaelPrefab(materials, theme.KaelPrefabName, theme.KaelRings, theme.KaelStage);
        VolumeProfile volumeProfile = CreateThemeVolumeProfile(theme);

        BuildThemedScene(context, kaelPrefab, volumeProfile);

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

    private static void BuildThemedScene(LevelContext context, GameObject kaelPrefab, VolumeProfile volumeProfile)
    {
        LevelTheme theme = context.Theme;
        Materials materials = context.Materials;
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
        context.Level = level.transform;
        context.Route = new RouteBuilder(level.transform);

        // --- Suelos, y después lo propio del nivel: fragmentos, eventos, dinámicas y peligros ---
        BuildThemedZones(level.transform, materials, theme);
        theme.BuildLayout(context);
        theme.Decorate(level.transform, materials, theme);
        CreateEventCamera();

        // --- Jugador y cámara ---
        GameObject kael = (GameObject)PrefabUtility.InstantiatePrefab(kaelPrefab);
        kael.transform.position = theme.Spawn;
        CreatePlayerCamera(kael.transform.Find("CameraTarget"));

        // --- Caída, respawn y gestores ---
        GameObject killZone = new GameObject("Zona de caida");
        killZone.transform.position = new Vector3(0f, theme.KillY, 120f);
        BoxCollider killCollider = killZone.AddComponent<BoxCollider>();
        killCollider.size = new Vector3(800f, 2f, 800f);
        killCollider.isTrigger = true;
        killZone.AddComponent<KillZone>();

        GameObject spawnPoint = new GameObject("Inicio");
        spawnPoint.transform.position = theme.Spawn;

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

    // Suelos del nivel (theme.Zones) con sus cimientos, el suelo decorativo lejano y una luz suave por zona.
    private static void BuildThemedZones(Transform level, Materials materials, LevelTheme theme)
    {
        foreach (ZoneInfo zone in theme.Zones)
        {
            Vector3 center = new Vector3(zone.X, zone.Y - zone.Thickness / 2f, zone.Z);
            Vector3 size = new Vector3(zone.Width, zone.Thickness, zone.Depth);
            if (zone.Plain)
            {
                CreateBlock(zone.Name, level, center, size, materials.StoneTop);
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
                        new Vector3(Mathf.Max(zone.Width - 3f, 1f), height, Mathf.Max(zone.Depth - 3f, 1f)), materials.Stone);
                    Object.DestroyImmediate(foundation.GetComponent<Collider>());
                    NoShadow(foundation);
                }
            }

            // Luz suave: da color al ambiente sin tocar los enemigos (que llevan su luz roja).
            CreateZoneLight(level, new Vector3(zone.X, zone.Y + 7f, zone.Z), theme.ZoneLightColor, 24f, 3f);
        }

        // Suelo decorativo lejano (valle o lecho).
        if (!float.IsNaN(theme.FloorY) && theme.FloorMaterial != null)
        {
            GameObject floor = CreateBlock("Suelo", level, new Vector3(0f, theme.FloorY - 0.5f, 120f), new Vector3(800f, 1f, 800f), theme.FloorMaterial);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            NoShadow(floor);
        }
    }

    // Muro con hueco de 4 m y compuerta que se hunde con el evento. El muro cruza en X de xMin a xMax, a la altura
    // "floorY" y en z; el hueco queda centrado en doorX. Devuelve la compuerta (para ConfigureEvent).
    private static GameObject CreateGateWall(Transform level, string name, string eventId, float xMin, float xMax, float doorX, float floorY, float z, Materials materials)
    {
        float leftWidth = doorX - 2f - xMin;
        float rightWidth = xMax - (doorX + 2f);
        CreateBlock(name + " muro izquierdo", level, new Vector3(xMin + leftWidth / 2f, floorY + 2f, z), new Vector3(leftWidth, 4f, 1.5f), materials.Ruin);
        CreateBlock(name + " muro derecho", level, new Vector3(xMax - rightWidth / 2f, floorY + 2f, z), new Vector3(rightWidth, 4f, 1.5f), materials.Ruin);
        CreateBlock(name + " dintel", level, new Vector3(doorX, floorY + 3.5f, z), new Vector3(4f, 1f, 1.5f), materials.Ruin);
        return CreateDoor(name, eventId, new Vector3(doorX, floorY + 1.9f, z), materials);
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
