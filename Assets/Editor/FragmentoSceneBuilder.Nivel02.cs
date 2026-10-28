using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   Parte del constructor de escenas que genera el Nivel 2, "Cavernas de Cristal" (Nivel02_Cavernas.unity):
///   una cueva cerrada con lago de sombras, 7 fragmentos, "sombras de roca" que embisten y torretas de cristal.
///
/// POR QUÉ EXISTE
///   Igual que el nivel 1, la escena es un resultado del código: se regenera igual en cualquier PC y dos
///   personas no editan el mismo archivo de escena. Reutiliza los ayudantes del nivel 1 (bloques, islas, puentes,
///   plataformas, compuerta, núcleo y portal) y solo añade lo propio del nivel 2.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 2 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: +Z (norte), unos 250 m. Zonas: 1 Entrada, 2 Sala del lago, 3 Galería alta, 4 Cámara de estalactitas,
///   5 Pasillo de cristales, 6 Cámara del eco, 7 Cima del cristal (Núcleo y portal).
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel02ScenePath = "Assets/Scenes/Nivel02_Cavernas.unity";
    private const string Nivel02VolumeProfilePath = SettingsFolder + "/Nivel02_PerfilVolumen.asset";

    // Color de fondo y niebla de la cueva: verde-negro profundo.
    private static readonly Color CaveColor = new Color(0.01f, 0.03f, 0.035f);

    // Materiales propios del nivel 2 (el resto se reutiliza del nivel 1).
    private struct CaveMaterials
    {
        public Material CrystalGreen;   // cristal que crece y puentes
        public Material CrystalMagenta; // cristales decorativos
        public Material Lake;
        public Material EnemyRock;      // cuerpo oscuro de la sombra de roca
        public Material TurretCrystal;  // cristal de las torretas (rojo-rosado: peligro)
        public Material Projectile;
    }

    // ------------------------------------------------------------------ Entrada

    [MenuItem("Fragmento/Construir Nivel 2")]
    public static void BuildNivel2()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Fragmento: sal del modo Play antes de construir la escena.");
            return;
        }

        if (!Application.isBatchMode)
        {
            if (File.Exists(Nivel02ScenePath) && !EditorUtility.DisplayDialog(
                    "Construir Nivel 2",
                    "Se va a regenerar Nivel02_Cavernas.unity. Los cambios hechos a mano en esa escena se perderán.\n\n¿Continuar?",
                    "Continuar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
        }

        EnsureFolder(MaterialsFolder);
        EnsureFolder(PrefabsFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(SettingsFolder);
        EnsureFolder(ScenesFolder);

        Materials materials = CreateMaterials();
        CaveMaterials cave = CreateCaveMaterials();

        // Piedra y bordes de la cueva: reutilizan los campos del nivel 1 con otro color (CreateIsland lee de "materials").
        materials.Stone = GetOrCreateMaterial("CuevaPiedra", new Color(0.05f, 0.07f, 0.08f), null, 0f, 0.25f);
        materials.StoneTop = GetOrCreateMaterial("CuevaPiedraTapa", new Color(0.14f, 0.19f, 0.2f), null, 0f, 0.3f);
        materials.Ruin = GetOrCreateMaterial("CuevaRoca", new Color(0.1f, 0.14f, 0.15f), null, 0f, 0.25f);
        materials.Edge = GetOrCreateMaterial("CuevaBorde", new Color(0.05f, 0.3f, 0.2f), new Color(0.1f, 1f, 0.55f), 1.5f, 0.4f);
        materials.PlatformOff = GetOrCreateMaterial("CuevaPlataformaApagada", new Color(0.06f, 0.1f, 0.1f), new Color(0.1f, 1f, 0.55f), 0.25f, 0.3f);
        materials.PlatformOn = cave.CrystalGreen;

        FragmentData[] fragments =
        {
            CreateFragmentData(1, "N2_Fragmento01_Raiz", "Fragmento de la Raíz",
                "Del lago brotan cristales: un camino de pilares cruza las aguas.", "cristal_1", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(2, "N2_Fragmento02_Cauce", "Fragmento del Cauce",
                "Un puente de cristal sube hacia la galería.", "puente_2", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(3, "N2_Fragmento03_Eco", "Fragmento del Eco",
                "Una plataforma dormida despierta y cruza el vacío.", "plataforma_3", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(4, "N2_Fragmento04_Grieta", "Fragmento de la Grieta",
                "La compuerta de sombras se hunde: el pasillo está libre.", "puerta_4", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(5, "N2_Fragmento05_Veta", "Fragmento de la Veta",
                "Una escalera de cristal crece hacia la cámara del eco.", "cristal_5", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(6, "N2_Fragmento06_Cima", "Fragmento de la Cima",
                "Columnas de cristal ascienden hacia la cima.", "cristal_6", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(7, "N2_Fragmento07_Nucleo", "Fragmento del Núcleo",
                "El Núcleo de la Realidad vuelve a brillar.", "nucleo_7", new Color(1f, 0.75f, 0.25f)),
        };

        GameObject kaelPrefab = CreateKaelPrefab(materials, "Kael_Nivel2", true);
        GameObject fragmentPrefab = CreateFragmentPrefab(materials.Fragment);
        TurretProjectile projectilePrefab = CreateProjectilePrefab(cave.Projectile);
        VolumeProfile volumeProfile = CreateCaveVolumeProfile();

        BuildNivel2Scene(materials, cave, fragments, kaelPrefab, fragmentPrefab, projectilePrefab, volumeProfile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fragmento: Nivel 2 construido en " + Nivel02ScenePath);
    }

    private static CaveMaterials CreateCaveMaterials()
    {
        return new CaveMaterials
        {
            CrystalGreen = GetOrCreateMaterial("CristalVerde", new Color(0.1f, 0.5f, 0.35f), new Color(0.15f, 1f, 0.6f), 2.2f, 0.7f),
            CrystalMagenta = GetOrCreateMaterial("CristalMagenta", new Color(0.4f, 0.1f, 0.5f), new Color(0.9f, 0.2f, 0.9f), 1.8f, 0.7f),
            Lake = GetOrCreateMaterial("LagoSombras", new Color(0.01f, 0.03f, 0.04f), new Color(0.02f, 0.2f, 0.15f), 0.7f, 0.95f),
            EnemyRock = GetOrCreateMaterial("SombraRoca", new Color(0.05f, 0.03f, 0.04f), null, 0f, 0.3f),
            TurretCrystal = GetOrCreateMaterial("CristalTorreta", new Color(0.6f, 0.05f, 0.2f), new Color(1f, 0.1f, 0.3f), 2.5f, 0.7f),
            Projectile = GetOrCreateMaterial("Proyectil", new Color(1f, 0.3f, 0.4f), new Color(1f, 0.15f, 0.3f), 4f, 0.5f),
        };
    }

    private static VolumeProfile CreateCaveVolumeProfile()
    {
        VolumeProfile profile = CreateVolumeProfile(Nivel02VolumeProfilePath);
        if (profile.TryGet(out ColorAdjustments adjustments))
        {
            adjustments.colorFilter.Override(new Color(0.88f, 1f, 0.95f)); // leve tinte verdoso
            EditorUtility.SetDirty(adjustments);
        }
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static TurretProjectile CreateProjectilePrefab(Material material)
    {
        GameObject root = new GameObject("Proyectil");
        GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shard.name = "Esquirla";
        shard.transform.SetParent(root.transform, false);
        shard.transform.localScale = new Vector3(0.35f, 0.35f, 0.9f); // alargada hacia donde viaja
        shard.GetComponent<Renderer>().sharedMaterial = material;
        shard.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.DestroyImmediate(shard.GetComponent<Collider>());

        root.AddComponent<TurretProjectile>();
        GameObject prefab = SavePrefab(root, "Proyectil");
        return prefab.GetComponent<TurretProjectile>();
    }

    // ------------------------------------------------------------------ Escena

    private static void BuildNivel2Scene(Materials materials, CaveMaterials cave, FragmentData[] fragments, GameObject kaelPrefab,
        GameObject fragmentPrefab, TurretProjectile projectilePrefab, VolumeProfile volumeProfile)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureCaveEnvironment();
        CreateMainCamera();
        Camera.main.backgroundColor = CaveColor;
        CreateGlobalVolume(volumeProfile);

        GameObject level = new GameObject("Nivel");
        BuildCaveShell(level.transform, materials, cave);
        BuildCaveZones(level.transform, materials, cave);

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

        // --- Eventos de mundo ---
        GameObject chain1 = CreateCrystalChain("Cristales del lago", "cristal_1", cave.CrystalGreen, -4f, 5f, 3.2f,
            new[] { new Vector3(0f, 0f, 17.5f), new Vector3(0f, 0.5f, 21.5f), new Vector3(0f, 0f, 25.5f), new Vector3(0f, 0.5f, 29.5f) });
        GameObject bridge2 = CreateBridge("Puente de cristal", new Vector3(0f, 0f, 62.9f), new Vector3(0f, 5f, 73.1f), 4f, "puente_2", cave.CrystalGreen);
        GameObject platform3 = CreateMovingPlatform("Plataforma movil", "plataforma_3", new Vector3(0f, 4.7f, 106f), new Vector3(0f, 0f, 16f), materials);
        GameObject door4 = CreateDoor("Compuerta de sombras", "puerta_4", new Vector3(0f, 6.9f, 146f), materials);
        GameObject chain5 = CreateCrystalChain("Escalera de cristal", "cristal_5", cave.CrystalGreen, -4f, 8f, 4f,
            new[] { new Vector3(0f, 6f, 172f), new Vector3(0f, 7f, 177f), new Vector3(0f, 8f, 182f) });
        GameObject chain6 = CreateCrystalChain("Columnas de la cima", "cristal_6", cave.CrystalGreen, -4f, 6f, 3.6f,
            new[] { new Vector3(0f, 10.4f, 217.8f), new Vector3(0f, 11.8f, 222.6f), new Vector3(0f, 13.2f, 227.4f) });
        // Núcleo y portal: el nivel 1 los dibuja en su isla G (centro 2, 177); se desplazan a la cima del nivel 2.
        GameObject core = CreateCoreAndPortal(materials, new Vector3(-2f, 1.5f, 64f));

        ConfigureEvent(chain1, chain1.transform.GetChild(1).GetChild(0), cave.CrystalGreen);
        ConfigureEvent(bridge2, bridge2.transform, cave.CrystalGreen);
        ConfigureEvent(platform3, platform3.transform, cave.CrystalGreen);
        ConfigureEvent(door4, door4.transform.Find("Losa"), materials.Door);
        ConfigureEvent(chain5, chain5.transform.GetChild(1).GetChild(0), cave.CrystalGreen);
        ConfigureEvent(chain6, chain6.transform.GetChild(1).GetChild(0), cave.CrystalGreen);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        CreateEventCamera();

        // --- Peligros: más densos al principio (la dificultad baja con cada muerte y con la progresión) ---
        BuildHazards(level.transform, materials, cave, projectilePrefab);

        // --- Caída al lago, respawn y gestores ---
        GameObject killZone = new GameObject("Zona de caida (lago)");
        killZone.transform.position = new Vector3(0f, -5f, 120f);
        BoxCollider killCollider = killZone.AddComponent<BoxCollider>();
        killCollider.size = new Vector3(400f, 2f, 400f);
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

        CreateHud("", 7);

        EditorSceneManager.SaveScene(scene, Nivel02ScenePath);
        RegisterScenesInBuildSettings();
    }

    // ------------------------------------------------------------------ Entorno

    private static void ConfigureCaveEnvironment()
    {
        // Luz direccional tenue: da forma a las piedras. Los techos y paredes no proyectan sombra (ver NoShadow).
        GameObject sun = new GameObject("Luz Direccional");
        sun.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.65f, 1f, 0.85f);
        light.intensity = 0.7f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.7f;
        RenderSettings.sun = light;

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.16f, 0.30f, 0.27f);
        RenderSettings.ambientEquatorColor = new Color(0.1f, 0.2f, 0.2f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.08f, 0.09f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.011f;
        RenderSettings.fogColor = CaveColor;
    }

    // Lago, paredes, techo, estalactitas y cristales decorativos. Solo visual (salvo las paredes, que cierran la cueva).
    private static void BuildCaveShell(Transform level, Materials materials, CaveMaterials cave)
    {
        GameObject lake = CreateBlock("Lago de sombras", level, new Vector3(0f, -4.05f, 120f), new Vector3(400f, 0.1f, 400f), cave.Lake);
        Object.DestroyImmediate(lake.GetComponent<Collider>());
        NoShadow(lake);

        NoShadow(CreateBlock("Pared oeste", level, new Vector3(-50f, 15f, 120f), new Vector3(4f, 70f, 330f), materials.Ruin));
        NoShadow(CreateBlock("Pared este", level, new Vector3(50f, 15f, 120f), new Vector3(4f, 70f, 330f), materials.Ruin));
        NoShadow(CreateBlock("Pared sur", level, new Vector3(0f, 15f, -26f), new Vector3(104f, 70f, 4f), materials.Ruin));
        NoShadow(CreateBlock("Pared norte", level, new Vector3(0f, 15f, 266f), new Vector3(104f, 70f, 4f), materials.Ruin));
        NoShadow(CreateBlock("Techo", level, new Vector3(0f, 41f, 120f), new Vector3(104f, 2f, 330f), materials.Ruin));

        System.Random rng = new System.Random(2);

        // Estalactitas: cubos girados 45° colgando del techo (sin collider ni sombra, para no pesar).
        for (int i = 0; i < 70; i++)
        {
            float length = 3f + (float)rng.NextDouble() * 9f;
            Vector3 position = new Vector3(Mathf.Lerp(-46f, 46f, (float)rng.NextDouble()), 40f - length / 2f, Mathf.Lerp(-22f, 262f, (float)rng.NextDouble()));
            GameObject stalactite = CreateBlock("Estalactita " + (i + 1), level, position, new Vector3(1.2f, length, 1.2f), materials.Stone);
            stalactite.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Object.DestroyImmediate(stalactite.GetComponent<Collider>());
            NoShadow(stalactite);
        }

        // Cristales decorativos en las paredes laterales y en las orillas del camino.
        for (int i = 0; i < 90; i++)
        {
            bool west = i % 2 == 0;
            float z = Mathf.Lerp(-20f, 260f, (float)rng.NextDouble());
            float y = Mathf.Lerp(-3f, 28f, (float)rng.NextDouble());
            Material material = i % 5 == 0 ? cave.CrystalMagenta : cave.CrystalGreen;
            CreateCrystalCluster("Cristal pared " + (i + 1), level, new Vector3(west ? -47.5f : 47.5f, y, z), 1.2f + (float)rng.NextDouble() * 1.8f, material, rng);
        }
    }

    // Grupo de tres cristales inclinados sobre un punto. Solo visual.
    private static void CreateCrystalCluster(string name, Transform parent, Vector3 basePosition, float scale, Material material, System.Random rng)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        cluster.transform.position = basePosition;

        for (int i = 0; i < 3; i++)
        {
            float height = scale * (0.8f + (float)rng.NextDouble() * 1.4f);
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.name = "Cristal " + (i + 1);
            crystal.transform.SetParent(cluster.transform, false);
            crystal.transform.localScale = new Vector3(0.3f * scale, height, 0.3f * scale);
            crystal.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-18f, 18f, (float)rng.NextDouble()), (float)rng.NextDouble() * 90f, Mathf.Lerp(-18f, 18f, (float)rng.NextDouble()));
            crystal.transform.localPosition = new Vector3((i - 1) * 0.35f * scale, height / 2f, ((float)rng.NextDouble() - 0.5f) * 0.3f * scale);
            crystal.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(crystal.GetComponent<Collider>());
            NoShadow(crystal);
        }
    }

    private static void NoShadow(GameObject target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // ------------------------------------------------------------------ Zonas

    private static void BuildCaveZones(Transform level, Materials materials, CaveMaterials cave)
    {
        // Superficies: Z1 y Z2 en y = 0; Z3, Z4 y pasillo en y = 5; Z6 en y = 9; Z7 (cima) en y = 14. El lago está en y = -4.
        CreateIsland("Zona 1 Entrada", level, new Vector3(0f, -1f, 0f), new Vector3(30f, 2f, 30f), materials);
        CreateIsland("Zona 2 Sala del lago", level, new Vector3(0f, -1f, 48f), new Vector3(36f, 2f, 30f), materials);
        CreateIsland("Zona 3 Galeria alta", level, new Vector3(0f, 2f, 88f), new Vector3(36f, 6f, 30f), materials);
        CreateIsland("Zona 4 Camara de estalactitas", level, new Vector3(0f, 2f, 134f), new Vector3(36f, 6f, 30f), materials);
        CreateBlock("Pasillo de cristales", level, new Vector3(0f, 2f, 159f), new Vector3(8f, 6f, 20f), materials.StoneTop);
        CreateIsland("Zona 6 Camara del eco", level, new Vector3(0f, 6f, 200f), new Vector3(36f, 6f, 30f), materials);
        CreateIsland("Zona 7 Cima del cristal", level, new Vector3(0f, 11f, 241f), new Vector3(24f, 6f, 20f), materials);

        // Piedras del lago hacia el Fragmento 2 (saltos de 1 a 1.5 m).
        CreateIsland("Piedra del lago A", level, new Vector3(-21f, -1f, 46f), new Vector3(3f, 2f, 3f), materials);
        CreateIsland("Piedra del lago B", level, new Vector3(-25f, -1f, 50f), new Vector3(3f, 2f, 3f), materials);
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

        // Cristales decorativos en las orillas de cada zona (no estorban el camino central).
        System.Random rng = new System.Random(7);
        Vector4[] zones =
        {
            new Vector4(0f, 0f, 0f, 30f), new Vector4(0f, 0f, 48f, 36f), new Vector4(0f, 5f, 88f, 36f),
            new Vector4(0f, 5f, 134f, 36f), new Vector4(0f, 9f, 200f, 36f), new Vector4(0f, 14f, 241f, 24f),
        };
        foreach (Vector4 zone in zones)
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = zone.x + side * (zone.w / 2f - 1.2f);
                float z = zone.z + Mathf.Lerp(-12f, 12f, (float)rng.NextDouble());
                Material material = i % 3 == 0 ? cave.CrystalMagenta : cave.CrystalGreen;
                CreateCrystalCluster("Cristal orilla", level, new Vector3(x, zone.y, z), 1.4f + (float)rng.NextDouble(), material, rng);
            }

            CreateCaveLight(level, new Vector3(zone.x, zone.y + 7f, zone.z), cave.CrystalGreen);
        }
    }

    private static void CreateCaveLight(Transform parent, Vector3 position, Material tint)
    {
        GameObject lightObject = new GameObject("Luz de cueva");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.4f, 1f, 0.75f);
        light.range = 22f;
        light.intensity = 4f;
        light.shadows = LightShadows.None;
    }

    // ------------------------------------------------------------------ Eventos de mundo: cristales

    // Cadena de cristales (CrystalGrow). tops: (x, altura de la cara superior, z) de cada pilar. Cada pilar es un pivote en
    // "baseY" con un cubo hijo, así crece hacia arriba desde el fondo del lago.
    private static GameObject CreateCrystalChain(string name, string eventId, Material material, float baseY, float sizeX, float sizeZ, Vector3[] tops)
    {
        GameObject root = new GameObject(name);
        Transform[] pivots = new Transform[tops.Length];
        for (int i = 0; i < tops.Length; i++)
        {
            float height = tops[i].y - baseY;

            GameObject pivot = new GameObject("Cristal " + (i + 1));
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.position = new Vector3(tops[i].x, baseY, tops[i].z);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Cuerpo";
            body.transform.SetParent(pivot.transform, false);
            body.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            body.transform.localScale = new Vector3(sizeX, height, sizeZ);
            body.GetComponent<Renderer>().sharedMaterial = material;
            pivots[i] = pivot.transform;
        }

        CrystalGrow grow = root.AddComponent<CrystalGrow>();
        grow.SetCrystals(pivots);
        SerializedObject serialized = new SerializedObject(grow);
        Property(serialized, "eventId").stringValue = eventId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(grow);
        return root;
    }

    // ------------------------------------------------------------------ Peligros

    private static void BuildHazards(Transform level, Materials materials, CaveMaterials cave, TurretProjectile projectile)
    {
        // Zona 1 (entrada): dos sombras que cruzan la isla. Es el tramo más duro.
        CreateRockShadow("Sombra 1A", level, new Vector3(-11f, 0.9f, -2f), new Vector3(11f, 0.9f, -2f), IslandArea(0f, 0f, 0f, 30f, 30f), materials, cave);
        CreateRockShadow("Sombra 1B", level, new Vector3(10f, 0.9f, 6f), new Vector3(-10f, 0.9f, 6f), IslandArea(0f, 0f, 0f, 30f, 30f), materials, cave);

        // Zona 2 (sala del lago): dos sombras y una torreta que vigila el puente.
        CreateRockShadow("Sombra 2A", level, new Vector3(-14f, 0.9f, 40f), new Vector3(14f, 0.9f, 40f), IslandArea(0f, 0f, 48f, 36f, 30f), materials, cave);
        CreateRockShadow("Sombra 2B", level, new Vector3(14f, 0.9f, 55f), new Vector3(-12f, 0.9f, 55f), IslandArea(0f, 0f, 48f, 36f, 30f), materials, cave);
        CreateCrystalTurret("Torreta 2", level, new Vector3(16f, 0f, 60f), 1, 2.8f, projectile, materials, cave);

        // Zona 3 (galería alta): dos sombras, una torreta; coberturas en el centro.
        CreateRockShadow("Sombra 3A", level, new Vector3(-13f, 5.9f, 80f), new Vector3(13f, 5.9f, 80f), IslandArea(0f, 5f, 88f, 36f, 30f), materials, cave);
        CreateRockShadow("Sombra 3B", level, new Vector3(13f, 5.9f, 95f), new Vector3(-13f, 5.9f, 95f), IslandArea(0f, 5f, 88f, 36f, 30f), materials, cave);
        CreateCrystalTurret("Torreta 3", level, new Vector3(-15f, 5f, 101f), 1, 2.6f, projectile, materials, cave);

        // Zona 4 (cámara de estalactitas): torretas cruzadas, una en ráfaga.
        CreateRockShadow("Sombra 4A", level, new Vector3(-12f, 5.9f, 125f), new Vector3(12f, 5.9f, 125f), IslandArea(0f, 5f, 134f, 36f, 30f), materials, cave);
        CreateCrystalTurret("Torreta 4W", level, new Vector3(-15f, 5f, 140f), 1, 2.6f, projectile, materials, cave);
        CreateCrystalTurret("Torreta 4E", level, new Vector3(15f, 5f, 140f), 3, 3.2f, projectile, materials, cave);

        // Zona 5 (pasillo de cristales, 8 m de ancho): dos sombras, sin sitio para esconderse; hay que leer el aviso.
        CreateRockShadow("Sombra 5A", level, new Vector3(-3f, 5.9f, 155f), new Vector3(3f, 5.9f, 155f), IslandArea(0f, 5f, 159f, 8f, 20f), materials, cave);
        CreateRockShadow("Sombra 5B", level, new Vector3(3f, 5.9f, 163f), new Vector3(-3f, 5.9f, 163f), IslandArea(0f, 5f, 159f, 8f, 20f), materials, cave);

        // Zona 6 (cámara del eco): dos torretas en ráfaga y una sombra.
        CreateRockShadow("Sombra 6A", level, new Vector3(-12f, 9.9f, 200f), new Vector3(12f, 9.9f, 200f), IslandArea(0f, 9f, 200f, 36f, 30f), materials, cave);
        CreateCrystalTurret("Torreta 6W", level, new Vector3(-16f, 9f, 207f), 3, 3.4f, projectile, materials, cave);
        CreateCrystalTurret("Torreta 6E", level, new Vector3(16f, 9f, 194f), 3, 3.4f, projectile, materials, cave);
        // Zona 7 (cima): sin peligros. Es el descanso antes del final.
    }

    // Parte de enemigo/torreta: cubo o esfera sin collider (los peligros matan por distancia, no por física).
    private static GameObject CreateVisualPart(string name, Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    // Sombra de roca: cuerpo anguloso (cubos girados y púas) con núcleo y ojos rojos. Embiste tras un aviso.
    private static GameObject CreateRockShadow(string name, Transform parent, Vector3 pointA, Vector3 pointB, Bounds area, Materials materials, CaveMaterials cave)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pointA;

        GameObject body = new GameObject("Cuerpo");
        body.transform.SetParent(root.transform, false);

        CreateVisualPart("Roca central", body.transform, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.95f, new Vector3(35f, 45f, 0f), cave.EnemyRock);
        CreateVisualPart("Hombro izquierdo", body.transform, PrimitiveType.Cube, new Vector3(-0.55f, 0.25f, 0f), Vector3.one * 0.5f, new Vector3(20f, 10f, 40f), cave.EnemyRock);
        CreateVisualPart("Hombro derecho", body.transform, PrimitiveType.Cube, new Vector3(0.55f, 0.25f, 0f), Vector3.one * 0.5f, new Vector3(-15f, 60f, -35f), cave.EnemyRock);
        CreateVisualPart("Pua 1", body.transform, PrimitiveType.Cube, new Vector3(0.45f, 0.1f, -0.45f), new Vector3(0.18f, 0.18f, 0.9f), new Vector3(0f, 45f, 0f), cave.EnemyRock);
        CreateVisualPart("Pua 2", body.transform, PrimitiveType.Cube, new Vector3(-0.45f, 0.1f, -0.45f), new Vector3(0.18f, 0.18f, 0.9f), new Vector3(0f, -45f, 0f), cave.EnemyRock);
        CreateVisualPart("Pua 3", body.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.18f, 0.9f, 0.18f), new Vector3(10f, 0f, 10f), cave.EnemyRock);
        CreateVisualPart("Nucleo rojo", body.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, Vector3.zero, materials.Enemy);
        CreateVisualPart("Ojo izquierdo", body.transform, PrimitiveType.Cube, new Vector3(-0.2f, 0.15f, 0.5f), new Vector3(0.2f, 0.08f, 0.08f), Vector3.zero, materials.Enemy);
        CreateVisualPart("Ojo derecho", body.transform, PrimitiveType.Cube, new Vector3(0.2f, 0.15f, 0.5f), new Vector3(0.2f, 0.08f, 0.08f), Vector3.zero, materials.Enemy);

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.1f, 0.05f);
        light.range = 5f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;

        EnemyVisual visual = root.AddComponent<EnemyVisual>();
        SerializedObject visualSerialized = new SerializedObject(visual);
        Property(visualSerialized, "body").objectReferenceValue = body.transform;
        visualSerialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<RedSentinel>().Setup(pointA, pointB, area);
        return root;
    }

    // Torreta de cristal: pedestal sólido + cabeza que gira (cristal que flota, cañón y boca de disparo).
    private static GameObject CreateCrystalTurret(string name, Transform parent, Vector3 floorPosition, int burst, float cooldown, TurretProjectile projectile, Materials materials, CaveMaterials cave)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;

        // El pedestal conserva su collider: es sólido y sirve de obstáculo (los proyectiles se rompen contra él).
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = "Pedestal";
        pedestal.transform.SetParent(root.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        pedestal.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
        pedestal.GetComponent<Renderer>().sharedMaterial = materials.StoneTop;

        GameObject head = new GameObject("Cabeza");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        head.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // mira al sur, hacia donde llega el jugador

        GameObject crystal = CreateVisualPart("Cristal", head.transform, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.85f, new Vector3(35.264f, 45f, 0f), cave.TurretCrystal);
        CreateVisualPart("Canon", head.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.8f), new Vector3(0.28f, 0.28f, 1.1f), Vector3.zero, cave.EnemyRock);

        GameObject muzzle = new GameObject("Boca");
        muzzle.transform.SetParent(head.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 1.6f);

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(head.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.1f, 0.3f);
        light.range = 5f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;

        EnemyVisual visual = root.AddComponent<EnemyVisual>();
        SerializedObject visualSerialized = new SerializedObject(visual);
        Property(visualSerialized, "body").objectReferenceValue = crystal.transform;
        Property(visualSerialized, "bobHeight").floatValue = 0.05f;
        Property(visualSerialized, "spinSpeed").floatValue = 0f; // el cristal no gira: giraría con la cabeza al apuntar
        visualSerialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<CrystalTurret>().Setup(head.transform, muzzle.transform, projectile, burst, cooldown);
        return root;
    }
}
