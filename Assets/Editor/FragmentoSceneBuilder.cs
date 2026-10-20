using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// QUÉ HACE
///   Genera con un clic todo lo que el juego necesita: materiales URP emisivos, datos de fragmentos,
///   prefabs (Kael, Fragmento), el perfil de Bloom y la escena Nivel01_Santuario.
///
/// POR QUÉ EXISTE
///   Nadie puede editar la escena "a mano" de forma reproducible, y dos personas editando la misma escena
///   chocan en Git. Con el builder la escena es un resultado del código: se regenera igual en cualquier PC.
///   Es idempotente: ejecutarlo varias veces da el mismo resultado (los assets se reutilizan por ruta).
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Escena. Por consola (sin abrir el Editor):
///     Unity.exe -batchmode -quit -projectPath . -executeMethod FragmentoSceneBuilder.BuildFromCommandLine
///   AVISO: la escena se SOBRESCRIBE. Los cambios compartidos se hacen aquí o en los prefabs, no en la escena.
///   Necesita los recursos esenciales de TextMeshPro (carpeta Assets/TextMesh Pro, ya incluida en el repositorio).
/// </summary>
public static class FragmentoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Nivel01_Santuario.unity";
    private const string MaterialsFolder = "Assets/Materials";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string DataFolder = "Assets/Data";
    private const string SettingsFolder = "Assets/Settings";
    private const string ScenesFolder = "Assets/Scenes";
    private const string VolumeProfilePath = SettingsFolder + "/Nivel01_PerfilVolumen.asset";

    // Color del vacío: fondo y niebla comparten color para que las islas lejanas se disuelvan en la oscuridad.
    private static readonly Color VoidColor = new Color(0.015f, 0.02f, 0.05f);

    // Geometría de la escalera del Fragmento 2 (de la isla B a la terraza C).
    private const int StairCount = 20;
    private const float StairRise = 0.25f;
    private const float StairDepth = 0.75f;
    private const float StairWidth = 3.5f;

    // ------------------------------------------------------------------ Entradas

    [MenuItem("Fragmento/Construir Escena")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Fragmento: sal del modo Play antes de construir la escena.");
            return;
        }

        if (!Application.isBatchMode)
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Construir escena",
                    "Se va a regenerar Nivel01_Santuario.unity. Los cambios hechos a mano en esa escena se perderán.\n\n¿Continuar?",
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
        FragmentData[] fragments =
        {
            CreateFragmentData(1, "Fragmento01_Umbral", "Fragmento del Umbral",
                "Un puente de luz cruza el abismo.", "puente_1", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(2, "Fragmento02_Piedra", "Fragmento de la Piedra",
                "Las rocas flotantes se reordenan: una escalera sube hacia la terraza.", "escalera_2", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(3, "Fragmento03_Pulso", "Fragmento del Pulso",
                "Una plataforma extinta despierta y cruza la brecha.", "plataforma_3", new Color(1f, 0.75f, 0.25f)),
        };
        GameObject kaelPrefab = CreateKaelPrefab(materials.Kael);
        GameObject fragmentPrefab = CreateFragmentPrefab(materials.Fragment);
        VolumeProfile volumeProfile = CreateVolumeProfile();

        BuildLevelScene(materials, fragments, kaelPrefab, fragmentPrefab, volumeProfile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fragmento: escena construida en " + ScenePath);
    }

    /// <summary>Punto de entrada para -executeMethod (Unity en modo batch).</summary>
    public static void BuildFromCommandLine()
    {
        BuildScene();
    }

    // ------------------------------------------------------------------ Materiales

    private class Materials
    {
        public Material Stone;
        public Material Ruin;
        public Material Rune;
        public Material Kael;
        public Material Fragment;
        public Material Bridge;
        public Material BridgeStump;
        public Material Stair;
        public Material PlatformOff;
        public Material PlatformOn;
    }

    private static Materials CreateMaterials()
    {
        return new Materials
        {
            Stone = GetOrCreateMaterial("Piedra", new Color(0.09f, 0.11f, 0.17f), null, 0f, 0.25f),
            Ruin = GetOrCreateMaterial("PiedraRuina", new Color(0.16f, 0.18f, 0.27f), null, 0f, 0.25f),
            Rune = GetOrCreateMaterial("RunaTenue", new Color(0.05f, 0.2f, 0.25f), new Color(0.1f, 0.7f, 0.8f), 1.2f, 0.4f),
            Kael = GetOrCreateMaterial("LuzKael", new Color(0.2f, 0.9f, 1f), new Color(0.2f, 0.9f, 1f), 3f, 0.6f),
            Fragment = GetOrCreateMaterial("FragmentoLuz", new Color(1f, 0.75f, 0.25f), new Color(1f, 0.75f, 0.25f), 3f, 0.6f),
            Bridge = GetOrCreateMaterial("PuenteLuz", new Color(0.3f, 0.8f, 1f), new Color(0.2f, 0.8f, 1f), 2.2f, 0.5f),
            BridgeStump = GetOrCreateMaterial("MunonPuente", new Color(0.1f, 0.18f, 0.25f), new Color(0.2f, 0.35f, 0.45f), 0.6f, 0.3f),
            Stair = GetOrCreateMaterial("EscalonPiedra", new Color(0.2f, 0.22f, 0.32f), new Color(0.15f, 0.4f, 0.5f), 0.5f, 0.3f),
            PlatformOff = GetOrCreateMaterial("PlataformaApagada", new Color(0.08f, 0.1f, 0.15f), new Color(0.1f, 0.3f, 0.35f), 0.3f, 0.3f),
            PlatformOn = GetOrCreateMaterial("PlataformaLuz", new Color(0.2f, 0.8f, 0.9f), new Color(0.2f, 0.9f, 0.9f), 2.5f, 0.5f),
        };
    }

    private static Material GetOrCreateMaterial(string name, Color baseColor, Color? emission, float emissionIntensity, float smoothness)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.Exception("No se encontró el shader 'Universal Render Pipeline/Lit'. ¿Está instalado URP?");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value * emissionIntensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    // ------------------------------------------------------------------ Datos

    private static FragmentData CreateFragmentData(int id, string fileName, string displayName, string message, string eventId, Color color)
    {
        string path = DataFolder + "/" + fileName + ".asset";
        FragmentData data = AssetDatabase.LoadAssetAtPath<FragmentData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<FragmentData>();
            AssetDatabase.CreateAsset(data, path);
        }

        SerializedObject serialized = new SerializedObject(data);
        Property(serialized, "id").intValue = id;
        Property(serialized, "displayName").stringValue = displayName;
        Property(serialized, "message").stringValue = message;
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "color").colorValue = color;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(data);
        return data;
    }

    // ------------------------------------------------------------------ Prefabs

    private static GameObject CreateKaelPrefab(Material bodyMaterial)
    {
        GameObject root = new GameObject("Kael");
        root.tag = "Player";

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = 1f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.5f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 50f;
        controller.skinWidth = 0.035f;

        // La raíz no gira (la cámara cuelga de ella); solo gira "Visual".
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Cuerpo";
        body.transform.SetParent(visual.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        body.transform.localScale = Vector3.one * 0.7f;
        body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
        Object.DestroyImmediate(body.GetComponent<Collider>()); // el CharacterController ya es su collider

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.3f, 0.9f, 1f);
        light.range = 7f;
        light.intensity = 4f;
        light.shadows = LightShadows.None;

        GameObject cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(root.transform, false);
        cameraTarget.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        cameraTarget.AddComponent<ThirdPersonCamera>();

        PlayerController player = root.AddComponent<PlayerController>();
        SerializedObject serialized = new SerializedObject(player);
        Property(serialized, "visual").objectReferenceValue = visual.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<MovingPlatformCarrier>();

        return SavePrefab(root, "Kael");
    }

    private static GameObject CreateFragmentPrefab(Material material)
    {
        GameObject root = new GameObject("Fragmento");

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

        // Rombo: un cubo girado para que una de sus diagonales quede vertical.
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(35.264f, 45f, 0f);
        visual.transform.localScale = Vector3.one * 0.6f;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = 6f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;

        Fragment fragment = root.AddComponent<Fragment>();
        SerializedObject serialized = new SerializedObject(fragment);
        Property(serialized, "visual").objectReferenceValue = visual.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, "Fragmento");
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        string path = PrefabsFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ------------------------------------------------------------------ Post-proceso

    private static VolumeProfile CreateVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);

        if (!profile.TryGet(out Tonemapping tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(true);
            AssetDatabase.AddObjectToAsset(tonemapping, profile);
        }
        tonemapping.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(tonemapping);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    // ------------------------------------------------------------------ Escena

    private static void BuildLevelScene(Materials materials, FragmentData[] fragments, GameObject kaelPrefab, GameObject fragmentPrefab, VolumeProfile volumeProfile)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureEnvironment();
        CreateMainCamera();
        CreateGlobalVolume(volumeProfile);

        GameObject level = new GameObject("Nivel");
        BuildIslands(level.transform, materials);

        // --- Jugador y cámara ---
        Vector3 spawn = new Vector3(0f, 0.05f, -10f);
        GameObject kael = (GameObject)PrefabUtility.InstantiatePrefab(kaelPrefab);
        kael.transform.position = spawn;
        CreatePlayerCamera(kael.transform.Find("CameraTarget"));

        // --- Fragmentos: F1 en A, F2 en B (tras las ruinas), F3 en C ---
        PlaceFragment(fragmentPrefab, "Fragmento 1", new Vector3(-5f, 1.3f, 9f), fragments[0]);
        PlaceFragment(fragmentPrefab, "Fragmento 2", new Vector3(-10f, 1.3f, 55f), fragments[1]);
        PlaceFragment(fragmentPrefab, "Fragmento 3", new Vector3(48f, 6.3f, 50f), fragments[2]);

        // --- Eventos de mundo ---
        CreateBridge("Puente de luz 1", new Vector3(0f, 0f, 14.9f), new Vector3(0f, 0f, 30.1f), 3f, "puente_1", materials.Bridge);
        CreateStairs("Escalera de piedra", "escalera_2", materials.Stair);
        CreateMovingPlatform("Plataforma movil", "plataforma_3", new Vector3(43f, 4.7f, 58f), new Vector3(0f, 0f, 16f), materials);

        // --- Checkpoints, caída al vacío y respawn ---
        Transform spawnPointA = CreateCheckpoint("Checkpoint A", new Vector3(0f, 3f, 0f), new Vector3(28f, 8f, 28f), spawn);
        CreateCheckpoint("Checkpoint B", new Vector3(0f, 3f, 45f), new Vector3(28f, 8f, 28f), new Vector3(0f, 0.05f, 34f));
        CreateCheckpoint("Checkpoint C", new Vector3(43f, 7f, 45f), new Vector3(24f, 6f, 20f), new Vector3(36f, 5.05f, 45f));
        CreateCheckpoint("Checkpoint D", new Vector3(43f, 7f, 88f), new Vector3(24f, 6f, 22f), new Vector3(43f, 5.05f, 80f));

        GameObject killZone = new GameObject("Zona de caida");
        killZone.transform.position = new Vector3(20f, -40f, 45f);
        BoxCollider killCollider = killZone.AddComponent<BoxCollider>();
        killCollider.size = new Vector3(400f, 2f, 400f);
        killCollider.isTrigger = true;
        killZone.AddComponent<KillZone>();

        GameObject respawn = new GameObject("Gestor de Respawn");
        RespawnManager respawnManager = respawn.AddComponent<RespawnManager>();
        SerializedObject respawnSerialized = new SerializedObject(respawnManager);
        Property(respawnSerialized, "initialSpawn").objectReferenceValue = spawnPointA;
        respawnSerialized.ApplyModifiedPropertiesWithoutUndo();

        // --- Gestor de fragmentos y HUD ---
        GameObject managers = new GameObject("Gestor de Fragmentos");
        managers.AddComponent<FragmentManager>();

        CreateHud();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    // Z+ es el norte, X+ el este. Alturas de la superficie: A y B en y = 0; C y D en y = 5.
    private static void BuildIslands(Transform level, Materials materials)
    {
        // Isla A (inicio): x -15..15, z -15..15. Isla B (plaza): z 30..60. El abismo entre ambas mide 15 m.
        CreateBlock("Isla A", level, new Vector3(0f, -1f, 0f), new Vector3(30f, 2f, 30f), materials.Stone);
        CreateBlock("Isla B", level, new Vector3(0f, -1f, 45f), new Vector3(30f, 2f, 30f), materials.Stone);
        // Terraza C (elevada, al este de B) y mirador D (al norte de C). C-D: brecha de 20 m para la plataforma móvil.
        CreateBlock("Isla C", level, new Vector3(43f, 2f, 45f), new Vector3(26f, 6f, 22f), materials.Stone);
        CreateBlock("Isla D", level, new Vector3(43f, 2f, 88f), new Vector3(26f, 6f, 24f), materials.Stone);

        // Ruinas para dar escala y puntos de referencia.
        CreateBlock("Ruina A1", level, new Vector3(9f, 1.5f, 0f), new Vector3(2f, 3f, 2f), materials.Ruin);
        CreateBlock("Ruina A2", level, new Vector3(-10f, 1f, -4f), new Vector3(2f, 2f, 6f), materials.Ruin);
        CreateBlock("Ruina A3", level, new Vector3(10f, 0.6f, -9f), new Vector3(3f, 1.2f, 1f), materials.Ruin);
        CreateBlock("Ruina B1", level, new Vector3(-9f, 1.5f, 40f), new Vector3(2f, 3f, 2f), materials.Ruin);
        CreateBlock("Ruina B2", level, new Vector3(9f, 1f, 50f), new Vector3(2f, 2f, 4f), materials.Ruin);
        // Rincón de ruinas que esconde el Fragmento 2 (se entra por el norte y el este).
        CreateBlock("Ruina B3", level, new Vector3(-10f, 1f, 51.5f), new Vector3(6f, 2f, 1f), materials.Ruin);
        CreateBlock("Ruina B4", level, new Vector3(-13.5f, 1f, 55.5f), new Vector3(1f, 2f, 6f), materials.Ruin);
        CreateBlock("Ruina C1", level, new Vector3(36.5f, 6f, 41f), new Vector3(2f, 2f, 2f), materials.Ruin);
        CreateBlock("Ruina C2", level, new Vector3(52f, 5.75f, 38f), new Vector3(1.5f, 1.5f, 1.5f), materials.Ruin);
        CreateBlock("Ruina C3", level, new Vector3(50f, 6.5f, 53f), new Vector3(3f, 3f, 1f), materials.Ruin);
        CreateBlock("Ruina D1", level, new Vector3(38f, 6f, 92f), new Vector3(2f, 2f, 2f), materials.Ruin);
        CreateBlock("Ruina D2", level, new Vector3(48f, 6.5f, 95f), new Vector3(3f, 3f, 2f), materials.Ruin);

        // Pilar luminoso en la Isla B: se ve desde el inicio y marca hacia dónde ir.
        CreateBlock("Pilar", level, new Vector3(0f, 7f, 45f), new Vector3(2f, 14f, 2f), materials.Rune);

        // Muñones del puente roto, a ambos lados del abismo.
        CreateBlock("Munon A izquierdo", level, new Vector3(-1.2f, 0.25f, 14.2f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon A derecho", level, new Vector3(1.2f, 0.25f, 14.2f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon B izquierdo", level, new Vector3(-1.2f, 0.25f, 30.8f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon B derecho", level, new Vector3(1.2f, 0.25f, 30.8f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
    }

    private static void ConfigureEnvironment()
    {
        GameObject sun = new GameObject("Luz Direccional");
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.65f, 0.75f, 1f);
        light.intensity = 0.9f;
        light.shadows = LightShadows.Soft;
        RenderSettings.sun = light;

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.30f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = VoidColor;
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 3f, -16f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = VoidColor;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 400f;

        cameraObject.AddComponent<AudioListener>();

        // El Brain es quien mueve esta cámara según la CinemachineCamera con más prioridad.
        CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

        // Sin esto el Bloom del Volume no se aplica a esta cámara.
        UniversalAdditionalCameraData urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = true;
    }

    private static void CreatePlayerCamera(Transform followTarget)
    {
        GameObject cameraObject = new GameObject("Camara Kael");

        CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Follow = followTarget;
        virtualCamera.Lens.FieldOfView = 60f;
        virtualCamera.Lens.NearClipPlane = 0.1f;
        virtualCamera.Lens.FarClipPlane = 400f;

        // Posición: detrás de Kael. Rotación: igual que el pivote CameraTarget (lo gira ThirdPersonCamera).
        CinemachineThirdPersonFollow follow = cameraObject.AddComponent<CinemachineThirdPersonFollow>();
        follow.ShoulderOffset = Vector3.zero;
        follow.VerticalArmLength = 0.2f;
        follow.CameraSide = 0.5f;
        follow.CameraDistance = 5f;
        follow.Damping = new Vector3(0.1f, 0.25f, 0.2f);
        follow.AvoidObstacles.Enabled = true;
        follow.AvoidObstacles.CollisionFilter = LayerMask.GetMask("Default");
        follow.AvoidObstacles.IgnoreTag = "Player";
        follow.AvoidObstacles.CameraRadius = 0.3f;

        cameraObject.AddComponent<CinemachineRotateWithFollowTarget>();
    }

    private static void CreateGlobalVolume(VolumeProfile profile)
    {
        GameObject volumeObject = new GameObject("Volumen Global");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    private static void PlaceFragment(GameObject prefab, string name, Vector3 position, FragmentData data)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.position = position;

        SerializedObject serialized = new SerializedObject(instance.GetComponent<Fragment>());
        Property(serialized, "data").objectReferenceValue = data;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ Eventos de mundo

    // Puente: Puente (BridgeAppear) > Pivote (crece en Z) > Cuerpo (losa con collider).
    private static GameObject CreateBridge(string name, Vector3 start, Vector3 end, float width, string eventId, Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        GameObject root = new GameObject(name);
        root.transform.position = start;
        root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        GameObject pivot = new GameObject("Pivote");
        pivot.transform.SetParent(root.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Cuerpo";
        body.transform.SetParent(pivot.transform, false);
        body.transform.localPosition = new Vector3(0f, -0.13f, length / 2f); // cara superior a +0.02 m
        body.transform.localScale = new Vector3(width, 0.3f, length);
        body.GetComponent<Renderer>().sharedMaterial = material;

        BridgeAppear bridge = root.AddComponent<BridgeAppear>();
        SerializedObject serialized = new SerializedObject(bridge);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "pivot").objectReferenceValue = pivot.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // Escalera: 20 escalones de 0.25 m de alto y 0.75 m de fondo, de B (y = 0, x = 15) a C (y = 5, x = 30).
    // Cada escalón sube 0.25 m: justo por debajo del Step Offset de Kael (0.3), así que se sube caminando.
    private static void CreateStairs(string name, string eventId, Material material)
    {
        GameObject root = new GameObject(name);
        Transform[] steps = new Transform[StairCount];

        for (int i = 0; i < StairCount; i++)
        {
            float top = StairRise * (i + 1);
            Vector3 center = new Vector3(15f + StairDepth * i + StairDepth / 2f, top - 0.25f, 45f);
            Vector3 size = new Vector3(StairDepth, 0.5f, StairWidth);
            steps[i] = CreateBlock("Escalon " + (i + 1).ToString("00"), root.transform, center, size, material).transform;
        }

        StairsMaterialize stairs = root.AddComponent<StairsMaterialize>();
        SerializedObject serialized = new SerializedObject(stairs);
        Property(serialized, "eventId").stringValue = eventId;
        SerializedProperty array = Property(serialized, "steps");
        array.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Plataforma móvil: un bloque con Rigidbody cinemático que, al activarse, va y viene "travel" metros.
    private static void CreateMovingPlatform(string name, string eventId, Vector3 position, Vector3 travel, Materials materials)
    {
        GameObject platform = CreateBlock(name, null, position, new Vector3(4f, 0.6f, 4f), materials.PlatformOff);

        Rigidbody body = platform.AddComponent<Rigidbody>();
        body.isKinematic = true;

        MovingPlatformActivate moving = platform.AddComponent<MovingPlatformActivate>();
        SerializedObject serialized = new SerializedObject(moving);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "travel").vector3Value = travel;
        Property(serialized, "platformRenderer").objectReferenceValue = platform.GetComponent<Renderer>();
        Property(serialized, "inactiveMaterial").objectReferenceValue = materials.PlatformOff;
        Property(serialized, "activeMaterial").objectReferenceValue = materials.PlatformOn;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Zona (trigger) sobre una isla + punto de reaparición seguro. Devuelve el punto.
    private static Transform CreateCheckpoint(string name, Vector3 center, Vector3 size, Vector3 respawnPosition)
    {
        GameObject zone = new GameObject(name);
        zone.transform.position = center;
        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = true;

        GameObject point = new GameObject("PuntoRespawn");
        point.transform.SetParent(zone.transform, false);
        point.transform.position = respawnPosition;

        CheckpointZone checkpoint = zone.AddComponent<CheckpointZone>();
        SerializedObject serialized = new SerializedObject(checkpoint);
        Property(serialized, "respawnPoint").objectReferenceValue = point.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return point.transform;
    }

    // ------------------------------------------------------------------ HUD

    private static void CreateHud()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            throw new System.Exception("Faltan los recursos esenciales de TextMeshPro (carpeta Assets/TextMesh Pro). " +
                                       "Impórtalos con Window > TextMeshPro > Import TMP Essential Resources.");
        }

        GameObject canvasObject = new GameObject("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Contador arriba a la izquierda.
        TextMeshProUGUI counter = CreateText("Contador", canvasObject.transform, font, 44f, TextAlignmentOptions.TopLeft);
        SetRect(counter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -36f), new Vector2(700f, 70f));
        counter.text = "Fragmentos  0 / 7";

        // Mensaje abajo en el centro, dentro de un CanvasGroup para poder desvanecerlo.
        GameObject messageObject = new GameObject("Mensaje", typeof(RectTransform), typeof(CanvasGroup));
        messageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        SetRect(messageRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1400f, 140f));
        CanvasGroup messageGroup = messageObject.GetComponent<CanvasGroup>();
        messageGroup.alpha = 0f;
        messageGroup.interactable = false;
        messageGroup.blocksRaycasts = false;

        TextMeshProUGUI message = CreateText("Texto", messageObject.transform, font, 38f, TextAlignmentOptions.Center);
        SetRect(message.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        message.text = "";

        HUDController hud = canvasObject.AddComponent<HUDController>();
        SerializedObject serialized = new SerializedObject(hud);
        Property(serialized, "counterText").objectReferenceValue = counter;
        Property(serialized, "messageText").objectReferenceValue = message;
        Property(serialized, "messageGroup").objectReferenceValue = messageGroup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.85f, 0.97f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    // ------------------------------------------------------------------ Utilidades

    private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        if (parent != null)
        {
            block.transform.SetParent(parent, false);
        }
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // Busca un campo serializado y falla con un mensaje claro si el nombre está mal escrito.
    private static SerializedProperty Property(SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            throw new System.Exception("El campo '" + name + "' no existe en " + serialized.targetObject.GetType().Name);
        }

        return property;
    }
}
