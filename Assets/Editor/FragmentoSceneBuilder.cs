using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BlendMode = UnityEngine.Rendering.BlendMode;
using RenderQueue = UnityEngine.Rendering.RenderQueue;
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
            CreateFragmentData(4, "Fragmento04_Sombra", "Fragmento de la Sombra",
                "La compuerta sellada por las sombras se hunde: el camino al norte está libre.", "puerta_4", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(5, "Fragmento05_Cauce", "Fragmento del Cauce",
                "Otro puente de luz cruza el abismo hacia el oeste.", "puente_5", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(6, "Fragmento06_Giro", "Fragmento del Giro",
                "Las plataformas giratorias se alinean y suben hacia la cima.", "plataformas_6", new Color(1f, 0.75f, 0.25f)),
            CreateFragmentData(7, "Fragmento07_Nucleo", "Fragmento del Núcleo",
                "El Núcleo de la Realidad vuelve a brillar.", "nucleo_7", new Color(1f, 0.75f, 0.25f)),
        };
        GameObject kaelPrefab = CreateKaelPrefab(materials);
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
        public Material StoneTop;
        public Material Edge;
        public Material Door;
        public Material Core;
        public Material Wave;
        public Material PortalSealed;
        public Material PortalOpen;
        public Material Halo;
        public Material Marker;
        public Material Trail;
        public Material Star;
    }

    // Paleta: cian/blanco brillante es SOLO de Kael. La luz del mundo es violeta; los fragmentos, ámbar.
    private static readonly Color WorldLight = new Color(0.55f, 0.35f, 1f);

    private static Materials CreateMaterials()
    {
        return new Materials
        {
            Stone = GetOrCreateMaterial("Piedra", new Color(0.07f, 0.08f, 0.13f), null, 0f, 0.2f),
            StoneTop = GetOrCreateMaterial("PiedraTapa", new Color(0.2f, 0.23f, 0.34f), null, 0f, 0.3f),
            Ruin = GetOrCreateMaterial("PiedraRuina", new Color(0.17f, 0.18f, 0.27f), null, 0f, 0.25f),
            Rune = GetOrCreateMaterial("RunaTenue", new Color(0.12f, 0.07f, 0.25f), WorldLight, 1.4f, 0.4f),
            Edge = GetOrCreateMaterial("BordeLuz", new Color(0.2f, 0.1f, 0.4f), WorldLight, 1.8f, 0.4f),
            Kael = GetOrCreateMaterial("LuzKael", new Color(0.8f, 1f, 1f), new Color(0.4f, 0.95f, 1f), 5f, 0.7f),
            Fragment = GetOrCreateMaterial("FragmentoLuz", new Color(1f, 0.75f, 0.25f), new Color(1f, 0.75f, 0.25f), 3f, 0.6f),
            Bridge = GetOrCreateMaterial("PuenteLuz", new Color(0.55f, 0.4f, 0.95f), WorldLight, 2.2f, 0.5f),
            BridgeStump = GetOrCreateMaterial("MunonPuente", new Color(0.14f, 0.1f, 0.28f), WorldLight, 0.6f, 0.3f),
            Stair = GetOrCreateMaterial("EscalonPiedra", new Color(0.22f, 0.2f, 0.36f), WorldLight, 0.6f, 0.3f),
            PlatformOff = GetOrCreateMaterial("PlataformaApagada", new Color(0.09f, 0.08f, 0.17f), WorldLight, 0.25f, 0.3f),
            PlatformOn = GetOrCreateMaterial("PlataformaLuz", new Color(0.55f, 0.4f, 0.95f), WorldLight, 2.5f, 0.5f),
            Door = GetOrCreateMaterial("CompuertaSombra", new Color(0.04f, 0.02f, 0.08f), new Color(0.7f, 0.1f, 0.5f), 0.9f, 0.3f),
            Core = GetOrCreateMaterial("NucleoLuz", new Color(0.9f, 0.85f, 1f), new Color(0.75f, 0.6f, 1f), 4f, 0.8f),
            PortalSealed = GetOrCreateMaterial("PortalSellado", new Color(0.03f, 0.02f, 0.06f), new Color(0.35f, 0.05f, 0.3f), 0.8f, 0.2f),
            PortalOpen = GetOrCreateMaterial("PortalLuz", new Color(0.9f, 0.85f, 1f), new Color(0.7f, 0.55f, 1f), 3.5f, 0.5f),
            Wave = GetOrCreateTransparentMaterial("OndaLuz", "Universal Render Pipeline/Lit", new Color(0.7f, 0.55f, 1f, 0.3f), new Color(0.7f, 0.55f, 1f), 2.5f, true),
            Halo = GetOrCreateTransparentMaterial("HaloKael", "Universal Render Pipeline/Lit", new Color(0.4f, 0.95f, 1f, 0.12f), new Color(0.3f, 0.9f, 1f), 1.2f, false),
            Marker = GetOrCreateTransparentMaterial("MarcadorKael", "Universal Render Pipeline/Lit", new Color(0.5f, 0.95f, 1f, 0.35f), new Color(0.3f, 0.9f, 1f), 1.5f, false),
            Trail = GetOrCreateTransparentMaterial("EstelaKael", "Universal Render Pipeline/Particles/Unlit", new Color(0.5f, 0.95f, 1f, 0.6f), null, 0f, false),
            Star = GetOrCreateTransparentMaterial("Estrella", "Universal Render Pipeline/Particles/Unlit", new Color(2.2f, 2.2f, 2.6f, 1f), null, 0f, false),
        };
    }

    // Material transparente URP (sin código en el juego): la mezcla y la cola de render se fijan a mano
    // porque, al crear el material por script, el Editor no aplica la "plantilla" Surface Type = Transparent.
    private static Material GetOrCreateTransparentMaterial(string name, string shaderName, Color baseColor, Color? emission, float emissionIntensity, bool drawInside)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.Exception("No se encontró el shader '" + shaderName + "'. ¿Está instalado URP?");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetFloat("_Surface", 1f);   // 1 = Transparent
        material.SetFloat("_Blend", 0f);     // 0 = mezcla alfa normal
        // El shader Lit de URP multiplica el color por el alfa dentro del shader (premultiplicado), así que
        // su mezcla es One / OneMinusSrcAlpha; con SrcAlpha el alfa se aplicaría dos veces y se vería casi invisible.
        // Los shaders de partículas sí usan SrcAlpha.
        bool isLit = shaderName.EndsWith("/Lit");
        material.SetFloat("_SrcBlend", (float)(isLit ? BlendMode.One : BlendMode.SrcAlpha));
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        if (isLit)
        {
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", drawInside ? 1f : 2f); // 1 = dibuja las caras interiores (se ve desde dentro)
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value * emissionIntensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        EditorUtility.SetDirty(material);
        return material;
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

    private static GameObject CreateKaelPrefab(Materials materials)
    {
        Material bodyMaterial = materials.Kael;
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

        // Halo: esfera exterior translúcida. Con el Bloom hace que Kael se lea como "la" luz del nivel.
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "Halo";
        halo.transform.SetParent(visual.transform, false);
        halo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        halo.transform.localScale = Vector3.one * 1.25f;
        halo.GetComponent<Renderer>().sharedMaterial = materials.Halo;
        Object.DestroyImmediate(halo.GetComponent<Collider>());

        // Estela: deja un rastro cian al moverse; ayuda a seguir a Kael con la vista.
        GameObject trailObject = new GameObject("Estela");
        trailObject.transform.SetParent(root.transform, false);
        trailObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = materials.Trail;
        trail.time = 0.45f;
        trail.widthMultiplier = 0.4f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        Gradient trailGradient = new Gradient();
        trailGradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.6f, 1f, 1f), 0f), new GradientColorKey(new Color(0.2f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = trailGradient;

        // Marcador de aterrizaje en el suelo (ver GroundMarker).
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "MarcadorSuelo";
        marker.transform.SetParent(root.transform, false);
        marker.GetComponent<Renderer>().sharedMaterial = materials.Marker;
        marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.AddComponent<GroundMarker>();

        // Polvo de luz alrededor de Kael: da sensación de volumen y de movimiento.
        CreateDust(root.transform, materials.Star);

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.4f, 0.95f, 1f);
        light.range = 9f;
        light.intensity = 5f;
        light.shadows = LightShadows.None;

        GameObject cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(root.transform, false);
        cameraTarget.transform.localPosition = new Vector3(0f, 1.3f, 0f);
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
        bloom.intensity.Override(0.9f);
        bloom.threshold.Override(1.1f); // solo brillan los emisivos fuertes, no la piedra
        bloom.scatter.Override(0.7f);
        bloom.highQualityFiltering.Override(true);

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.45f);

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
            AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
        }
        colorAdjustments.contrast.Override(12f);
        colorAdjustments.saturation.Override(8f);
        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(colorAdjustments);

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

        // --- Fragmentos: F1 en A, F2 en B (tras las ruinas), F3 en C, F4 en D, F5 en E, F6 en F, F7 en la cima G ---
        PlaceFragment(fragmentPrefab, "Fragmento 1", new Vector3(-5f, 1.3f, 9f), fragments[0]);
        PlaceFragment(fragmentPrefab, "Fragmento 2", new Vector3(-10f, 1.3f, 55f), fragments[1]);
        PlaceFragment(fragmentPrefab, "Fragmento 3", new Vector3(48f, 6.3f, 50f), fragments[2]);
        PlaceFragment(fragmentPrefab, "Fragmento 4", new Vector3(43f, 6.3f, 93f), fragments[3]);
        PlaceFragment(fragmentPrefab, "Fragmento 5", new Vector3(48f, 6.3f, 126f), fragments[4]);
        PlaceFragment(fragmentPrefab, "Fragmento 6", new Vector3(2f, 6.3f, 122f), fragments[5]);
        PlaceFragment(fragmentPrefab, "Fragmento 7", new Vector3(2f, 13.8f, 172f), fragments[6]);

        // --- Eventos de mundo (cada uno con cámara breve y partículas) ---
        GameObject bridge1 = CreateBridge("Puente de luz 1", new Vector3(0f, 0f, 14.9f), new Vector3(0f, 0f, 30.1f), 3f, "puente_1", materials.Bridge);
        GameObject stairs = CreateStairs("Escalera de piedra", "escalera_2", materials.Stair);
        GameObject movingPlatform = CreateMovingPlatform("Plataforma movil", "plataforma_3", new Vector3(43f, 4.7f, 58f), new Vector3(0f, 0f, 16f), materials);
        GameObject door = CreateDoor("Compuerta de sombras", "puerta_4", new Vector3(43f, 6.9f, 99.25f), materials);
        GameObject bridge5 = CreateBridge("Puente de luz 5", new Vector3(30.1f, 5f, 125f), new Vector3(14.9f, 5f, 125f), 3f, "puente_5", materials.Bridge);
        GameObject platforms = CreateRotatingPlatforms("Plataformas giratorias", "plataformas_6", materials);
        GameObject core = CreateCoreAndPortal(materials);

        ConfigureEvent(bridge1, bridge1.transform, materials.Bridge);
        ConfigureEvent(stairs, stairs.transform.Find("Escalon 10"), materials.Stair);
        ConfigureEvent(movingPlatform, movingPlatform.transform, materials.PlatformOn);
        ConfigureEvent(door, door.transform.Find("Losa"), materials.Door);
        ConfigureEvent(bridge5, bridge5.transform, materials.Bridge);
        ConfigureEvent(platforms, platforms.transform.Find("Foco"), materials.PlatformOn);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        CreateEventCamera();
        CreateStars(materials.Star);

        // --- Checkpoints, caída al vacío y respawn ---
        Transform spawnPointA = CreateCheckpoint("Checkpoint A", new Vector3(0f, 3f, 0f), new Vector3(28f, 8f, 28f), spawn);
        CreateCheckpoint("Checkpoint B", new Vector3(0f, 3f, 45f), new Vector3(28f, 8f, 28f), new Vector3(0f, 0.05f, 34f));
        CreateCheckpoint("Checkpoint C", new Vector3(43f, 7f, 45f), new Vector3(24f, 6f, 20f), new Vector3(36f, 5.05f, 45f));
        CreateCheckpoint("Checkpoint D", new Vector3(43f, 7f, 88f), new Vector3(24f, 6f, 22f), new Vector3(43f, 5.05f, 80f));
        CreateCheckpoint("Checkpoint E", new Vector3(43f, 8f, 125f), new Vector3(24f, 6f, 20f), new Vector3(43f, 5.05f, 118f));
        CreateCheckpoint("Checkpoint F", new Vector3(2f, 8f, 125f), new Vector3(22f, 6f, 20f), new Vector3(2f, 5.05f, 117f));
        CreateCheckpoint("Checkpoint G", new Vector3(2f, 16f, 177f), new Vector3(22f, 8f, 18f), new Vector3(2f, 12.55f, 169f));

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
        CreateIsland("Isla A", level, new Vector3(0f, -1f, 0f), new Vector3(30f, 2f, 30f), materials);
        CreateIsland("Isla B", level, new Vector3(0f, -1f, 45f), new Vector3(30f, 2f, 30f), materials);
        // Terraza C (elevada, al este de B) y mirador D (al norte de C). C-D: brecha de 20 m para la plataforma móvil.
        CreateIsland("Isla C", level, new Vector3(43f, 2f, 45f), new Vector3(26f, 6f, 22f), materials);
        CreateIsland("Isla D", level, new Vector3(43f, 2f, 88f), new Vector3(26f, 6f, 24f), materials);

        // --- Mitad norte (fragmentos 4-7) ---
        // Muro de ruinas al norte de D con un hueco de 4 m (x 41..45) que cierra la compuerta (fragmento 4).
        CreateBlock("Muro D izquierdo", level, new Vector3(35.5f, 7f, 99.25f), new Vector3(11f, 4f, 1.5f), materials.Ruin);
        CreateBlock("Muro D derecho", level, new Vector3(50.5f, 7f, 99.25f), new Vector3(11f, 4f, 1.5f), materials.Ruin);
        CreateBlock("Dintel compuerta", level, new Vector3(43f, 8.5f, 99.25f), new Vector3(4f, 1f, 1.5f), materials.Ruin);
        CreateBlock("Pasillo", level, new Vector3(43f, 2f, 107f), new Vector3(4f, 6f, 14f), materials.StoneTop);
        // Isla E (al norte del pasillo) e isla F (al oeste de E, brecha de 15 m: puente del fragmento 5).
        CreateIsland("Isla E", level, new Vector3(43f, 2f, 125f), new Vector3(26f, 6f, 22f), materials);
        CreateIsland("Isla F", level, new Vector3(2f, 2f, 125f), new Vector3(26f, 6f, 22f), materials);
        // Cima G: pedestal del Núcleo y portal. Se llega por las plataformas giratorias (fragmento 6).
        CreateIsland("Isla G (cima)", level, new Vector3(2f, 9.5f, 177f), new Vector3(24f, 6f, 20f), materials);

        CreateBlock("Ruina E1", level, new Vector3(36f, 6f, 120f), new Vector3(2f, 2f, 2f), materials.Ruin);
        CreateBlock("Ruina E2", level, new Vector3(50f, 6.5f, 131f), new Vector3(3f, 3f, 1.5f), materials.Ruin);
        CreateBlock("Ruina F1", level, new Vector3(-6f, 6f, 130f), new Vector3(2f, 2f, 2f), materials.Ruin);
        CreateBlock("Ruina F2", level, new Vector3(9f, 5.75f, 118f), new Vector3(1.5f, 1.5f, 1.5f), materials.Ruin);
        CreateBlock("Ruina G1", level, new Vector3(-6f, 13.5f, 175f), new Vector3(2f, 2f, 2f), materials.Ruin);
        CreateBlock("Ruina G2", level, new Vector3(10f, 14f, 172f), new Vector3(2f, 3f, 2f), materials.Ruin);
        // Obelisco en la cima: se ve desde casi todo el nivel y marca el destino final.
        CreateBlock("Obelisco", level, new Vector3(-7f, 28f, 181f), new Vector3(2f, 30f, 2f), materials.Rune);

        // Muñones del puente 5 (E-F).
        CreateBlock("Munon E norte", level, new Vector3(30.8f, 5.25f, 126.2f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon E sur", level, new Vector3(30.8f, 5.25f, 123.8f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon F norte", level, new Vector3(14.2f, 5.25f, 126.2f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);
        CreateBlock("Munon F sur", level, new Vector3(14.2f, 5.25f, 123.8f), new Vector3(1.2f, 0.5f, 1.2f), materials.BridgeStump);

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
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.85f;
        RenderSettings.sun = light;

        RenderSettings.skybox = null;
        // Ambiente en degradado: cielo azulado arriba, suelo violeta oscuro abajo. Da volumen a las caras
        // que no reciben la luz direccional (con ambiente plano todo se veía "aplastado").
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.30f, 0.38f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.20f, 0.20f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.10f, 0.07f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0045f;
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
        camera.farClipPlane = 600f;

        cameraObject.AddComponent<AudioListener>();

        // El Brain es quien mueve esta cámara según la CinemachineCamera con más prioridad.
        CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

        // Sin esto el Bloom del Volume no se aplica a esta cámara.
        UniversalAdditionalCameraData urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = true;

        // Antialiasing de post-proceso (SMAA alta calidad) además del MSAA del asset URP: bordes sin dientes.
        urpData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        urpData.antialiasingQuality = AntialiasingQuality.High;
        urpData.renderShadows = true;
    }

    private static void CreatePlayerCamera(Transform followTarget)
    {
        GameObject cameraObject = new GameObject("Camara Kael");

        CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Follow = followTarget;
        // Prioridad 10: la cámara de evento (0, sube a 20 solo durante un evento) nunca gana por "empate".
        virtualCamera.Priority = 10;
        virtualCamera.Lens.FieldOfView = 65f;
        virtualCamera.Lens.NearClipPlane = 0.1f;
        virtualCamera.Lens.FarClipPlane = 600f;

        // Posición: detrás de Kael. Rotación: igual que el pivote CameraTarget (lo gira ThirdPersonCamera).
        CinemachineThirdPersonFollow follow = cameraObject.AddComponent<CinemachineThirdPersonFollow>();
        follow.ShoulderOffset = Vector3.zero;
        follow.VerticalArmLength = 0.2f;
        follow.CameraSide = 0.5f;
        follow.CameraDistance = 6.5f;
        follow.Damping = new Vector3(0.15f, 0.3f, 0.25f);
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
    private static GameObject CreateStairs(string name, string eventId, Material material)
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
        return root;
    }

    // Plataforma móvil: un bloque con Rigidbody cinemático que, al activarse, va y viene "travel" metros.
    private static GameObject CreateMovingPlatform(string name, string eventId, Vector3 position, Vector3 travel, Materials materials)
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
        return platform;
    }

    // Compuerta: Compuerta (DoorOpen) > Losa (cubo con collider que se hunde al abrirse).
    private static GameObject CreateDoor(string name, string eventId, Vector3 slabCenter, Materials materials)
    {
        GameObject root = new GameObject(name);
        GameObject slab = CreateBlock("Losa", root.transform, slabCenter, new Vector3(4f, 3.9f, 1.2f), materials.Door);

        DoorOpen door = root.AddComponent<DoorOpen>();
        SerializedObject serialized = new SerializedObject(door);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "door").objectReferenceValue = slab.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // Cuatro plataformas que suben de la isla F a la cima G, cada una colgada de un pivote desplazado 8 m.
    // Los pivotes empiezan girados 180° (las plataformas flotan a un costado) y el evento los gira a 0°.
    // Plataformas de 4x4 con centros cada 7 m en Z (brecha 3 m) y cada 1.5 m más altas (el salto llega a 1.8 m).
    private static GameObject CreateRotatingPlatforms(string name, string eventId, Materials materials)
    {
        const int count = 4;
        const float x = 2f;
        const float firstZ = 141f;
        const float spacingZ = 7f;
        const float firstTop = 6.5f;
        const float rise = 1.5f;
        const float swing = 8f;

        GameObject root = new GameObject(name);
        GameObject focus = new GameObject("Foco");
        focus.transform.SetParent(root.transform, false);
        focus.transform.position = new Vector3(x, firstTop + rise * 1.5f, firstZ + spacingZ * 1.5f);

        Transform[] pivots = new Transform[count];
        Renderer[] renderers = new Renderer[count];
        for (int i = 0; i < count; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f; // las plataformas pares flotan al este, las impares al oeste
            Vector3 finalCenter = new Vector3(x, firstTop + rise * i - 0.3f, firstZ + spacingZ * i);

            GameObject pivot = new GameObject("Pivote " + (i + 1));
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.position = finalCenter + new Vector3(swing * side, 0f, 0f);

            // Primero se coloca la plataforma en su posición FINAL (pivote sin girar) y después se gira el
            // pivote 180°: así la plataforma empieza al otro lado, flotando fuera del camino.
            GameObject platform = CreateBlock("Plataforma " + (i + 1), pivot.transform, finalCenter, new Vector3(4f, 0.6f, 4f), materials.PlatformOff);
            pivot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            pivots[i] = pivot.transform;
            renderers[i] = platform.GetComponent<Renderer>();
        }

        RotatingPlatformsAlign align = root.AddComponent<RotatingPlatformsAlign>();
        SerializedObject serialized = new SerializedObject(align);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "inactiveMaterial").objectReferenceValue = materials.PlatformOff;
        Property(serialized, "activeMaterial").objectReferenceValue = materials.PlatformOn;
        SerializedProperty pivotsProperty = Property(serialized, "pivots");
        SerializedProperty renderersProperty = Property(serialized, "platformRenderers");
        pivotsProperty.arraySize = count;
        renderersProperty.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            pivotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = pivots[i];
            renderersProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // Pedestal + esferas del Núcleo (evento del fragmento 7) y Portal de Salida. Devuelve el objeto del evento.
    private static GameObject CreateCoreAndPortal(Materials materials)
    {
        Vector3 pedestalCenter = new Vector3(2f, 13.1f, 177f);
        Vector3 coreCenter = new Vector3(2f, 15.2f, 177f);

        CreateBlock("Pedestal", null, pedestalCenter, new Vector3(2.5f, 1.2f, 2.5f), materials.Ruin);
        CreateBlock("Pedestal base", null, new Vector3(2f, 12.7f, 177f), new Vector3(4f, 0.4f, 4f), materials.Rune);

        GameObject root = new GameObject("Restauracion del Nucleo");

        GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreSphere.name = "Nucleo";
        coreSphere.transform.SetParent(root.transform, false);
        coreSphere.transform.position = coreCenter;
        coreSphere.transform.localScale = Vector3.one * 1.6f;
        coreSphere.GetComponent<Renderer>().sharedMaterial = materials.Core;
        Object.DestroyImmediate(coreSphere.GetComponent<Collider>());

        GameObject lightObject = new GameObject("Luz del Nucleo");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.position = coreCenter;
        Light coreLight = lightObject.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = new Color(0.75f, 0.6f, 1f);
        coreLight.range = 25f;
        coreLight.intensity = 8f;
        coreLight.shadows = LightShadows.None;

        GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wave.name = "Onda de luz";
        wave.transform.SetParent(root.transform, false);
        wave.transform.position = coreCenter;
        wave.GetComponent<Renderer>().sharedMaterial = materials.Wave;
        wave.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(wave.GetComponent<Collider>());

        // Siete esquirlas en círculo alrededor del pedestal (aparecen al recoger el fragmento 7).
        Transform[] shards = new Transform[7];
        for (int i = 0; i < shards.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / shards.Length;
            Vector3 position = coreCenter + new Vector3(Mathf.Cos(angle) * 7f, Mathf.Sin(i * 1.7f) * 1.5f, Mathf.Sin(angle) * 7f);
            GameObject shard = CreateBlock("Esquirla " + (i + 1), root.transform, position, Vector3.one * 0.7f, materials.Fragment);
            shard.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            Object.DestroyImmediate(shard.GetComponent<Collider>());
            shards[i] = shard.transform;
        }

        CoreRestoration restoration = root.AddComponent<CoreRestoration>();
        SerializedObject serialized = new SerializedObject(restoration);
        Property(serialized, "eventId").stringValue = "nucleo_7";
        Property(serialized, "core").objectReferenceValue = coreSphere.transform;
        Property(serialized, "wave").objectReferenceValue = wave.transform;
        Property(serialized, "coreLight").objectReferenceValue = coreLight;
        SerializedProperty shardsProperty = Property(serialized, "shards");
        shardsProperty.arraySize = shards.Length;
        for (int i = 0; i < shards.Length; i++)
        {
            shardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = shards[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CreatePortal(materials);
        return root;
    }

    // Arco al norte de la cima: pilares + dintel, un disco sellado (con collider) y otro de luz (se activa al abrir).
    private static void CreatePortal(Materials materials)
    {
        GameObject portal = new GameObject("Portal de Salida");
        portal.transform.position = new Vector3(2f, 12.5f, 185.5f);

        CreateBlock("Pilar izquierdo", portal.transform, new Vector3(-0.6f, 15.5f, 185.5f), new Vector3(0.8f, 6f, 0.8f), materials.Ruin);
        CreateBlock("Pilar derecho", portal.transform, new Vector3(4.6f, 15.5f, 185.5f), new Vector3(0.8f, 6f, 0.8f), materials.Ruin);
        CreateBlock("Dintel", portal.transform, new Vector3(2f, 18.9f, 185.5f), new Vector3(6f, 0.8f, 0.8f), materials.Rune);

        GameObject sealedDisc = CreateBlock("Disco sellado", portal.transform, new Vector3(2f, 15.7f, 185.5f), new Vector3(4.4f, 5.4f, 0.2f), materials.PortalSealed);
        GameObject openDisc = CreateBlock("Disco de luz", portal.transform, new Vector3(2f, 15.7f, 185.5f), new Vector3(4.4f, 5.4f, 0.1f), materials.PortalOpen);
        Object.DestroyImmediate(openDisc.GetComponent<Collider>());

        GameObject lightObject = new GameObject("Luz del portal");
        lightObject.transform.SetParent(portal.transform, false);
        lightObject.transform.position = new Vector3(2f, 15.7f, 183.5f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.75f, 0.6f, 1f);
        light.range = 14f;
        light.intensity = 6f;
        light.shadows = LightShadows.None;

        // El trigger está delante del disco, para que Kael lo "cruce" al atravesar el arco.
        BoxCollider trigger = portal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 3.2f, 0f);
        trigger.size = new Vector3(4.4f, 5.4f, 2.5f);

        ExitPortal exitPortal = portal.AddComponent<ExitPortal>();
        SerializedObject serialized = new SerializedObject(exitPortal);
        Property(serialized, "sealedVisual").objectReferenceValue = sealedDisc;
        Property(serialized, "openVisual").objectReferenceValue = openDisc.transform;
        Property(serialized, "portalLight").objectReferenceValue = light;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Asigna a un evento su cámara breve (mira a "focus") y una ráfaga de partículas del color del material.
    private static void ConfigureEvent(GameObject eventObject, Transform focus, Material tint)
    {
        WorldEventBase worldEvent = eventObject.GetComponent<WorldEventBase>();
        if (worldEvent == null || focus == null)
        {
            throw new System.Exception("ConfigureEvent: '" + eventObject.name + "' no tiene evento o falta el objeto de foco.");
        }

        GameObject burstObject = new GameObject("Particulas del evento");
        burstObject.transform.SetParent(eventObject.transform, false);
        burstObject.transform.position = focus.position;
        ParticleSystem burst = burstObject.AddComponent<ParticleSystem>();
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.gravityModifier = -0.05f;
        Color color = tint.HasProperty("_EmissionColor") ? tint.GetColor("_EmissionColor") : Color.white;
        float peak = Mathf.Max(color.r, color.g, color.b, 0.01f);
        main.startColor = new Color(color.r / peak * 2f, color.g / peak * 2f, color.b / peak * 2f, 1f);

        ParticleSystem.EmissionModule emission = burst.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

        ParticleSystem.ShapeModule shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = LoadStarMaterial();

        SerializedObject serialized = new SerializedObject(worldEvent);
        Property(serialized, "cameraFocus").objectReferenceValue = focus;
        Property(serialized, "burst").objectReferenceValue = burst;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material LoadStarMaterial()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Estrella.mat");
    }

    // Cámara breve de evento (ver CameraEventShot). Prioridad 0: solo sube cuando un evento la pide.
    private static void CreateEventCamera()
    {
        GameObject cameraObject = new GameObject("Camara Evento");
        CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Lens.FieldOfView = 55f;
        virtualCamera.Lens.NearClipPlane = 0.1f;
        virtualCamera.Lens.FarClipPlane = 600f;
        virtualCamera.Priority = 0;
        cameraObject.AddComponent<CameraEventShot>();
    }

    // Estrellas fijas muy lejos: un solo estallido de partículas inmóviles con vida casi infinita.
    private static void CreateStars(Material material)
    {
        GameObject starsObject = new GameObject("Estrellas");
        starsObject.transform.position = new Vector3(20f, 20f, 90f);
        ParticleSystem stars = starsObject.AddComponent<ParticleSystem>();
        stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = stars.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.1f;
        main.startLifetime = 1000000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
        main.maxParticles = 900;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.6f, 1.6f, 2.2f), new Color(2.4f, 2f, 1.6f));

        ParticleSystem.EmissionModule emission = stars.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 900) });

        ParticleSystem.ShapeModule shape = stars.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 230f;
        shape.radiusThickness = 0f; // todas en la superficie de la esfera

        ParticleSystemRenderer particleRenderer = starsObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void CreateDust(Transform parent, Material material)
    {
        GameObject dustObject = new GameObject("Polvo de luz");
        dustObject.transform.SetParent(parent, false);
        dustObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        ParticleSystem dust = dustObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = dust.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        main.startColor = new Color(1.2f, 1.1f, 1.8f, 0.8f);

        ParticleSystem.EmissionModule emission = dust.emission;
        emission.rateOverTime = 14f;

        ParticleSystem.ShapeModule shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 9f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = dust.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
    }

    // Isla = cuerpo de piedra oscura (con collider) + tapa más clara y borde luminoso (solo visuales)
    // + rocas flotantes debajo (solo visuales). La tapa y el borde hacen que se vea dónde termina el suelo.
    private static void CreateIsland(string name, Transform parent, Vector3 center, Vector3 size, Materials materials)
    {
        GameObject island = CreateBlock(name, parent, center, size, materials.Stone);

        float top = center.y + size.y / 2f;
        float bottom = center.y - size.y / 2f;
        const float edgeWidth = 0.25f;
        const float edgeMargin = 0.3f;

        GameObject cap = CreateBlock(name + " tapa", island.transform.parent, new Vector3(center.x, top - 0.04f, center.z),
            new Vector3(size.x - edgeMargin * 2f, 0.1f, size.z - edgeMargin * 2f), materials.StoneTop);
        Object.DestroyImmediate(cap.GetComponent<Collider>());

        // Cuatro tiras emisivas en el borde superior.
        float halfX = size.x / 2f - edgeWidth / 2f;
        float halfZ = size.z / 2f - edgeWidth / 2f;
        float stripY = top - 0.04f;
        MakeEdge(name + " borde N", island.transform.parent, new Vector3(center.x, stripY, center.z + halfZ), new Vector3(size.x, 0.1f, edgeWidth), materials.Edge);
        MakeEdge(name + " borde S", island.transform.parent, new Vector3(center.x, stripY, center.z - halfZ), new Vector3(size.x, 0.1f, edgeWidth), materials.Edge);
        MakeEdge(name + " borde E", island.transform.parent, new Vector3(center.x + halfX, stripY, center.z), new Vector3(edgeWidth, 0.1f, size.z), materials.Edge);
        MakeEdge(name + " borde O", island.transform.parent, new Vector3(center.x - halfX, stripY, center.z), new Vector3(edgeWidth, 0.1f, size.z), materials.Edge);

        // Rocas flotantes bajo la isla: cada una más pequeña y girada, para que parezca un trozo de tierra volando.
        MakeRock(name + " roca 1", island.transform.parent, new Vector3(center.x, bottom - 1.5f, center.z), new Vector3(size.x * 0.7f, 3f, size.z * 0.7f), 15f, materials.Stone);
        MakeRock(name + " roca 2", island.transform.parent, new Vector3(center.x, bottom - 4.2f, center.z), new Vector3(size.x * 0.4f, 3.5f, size.z * 0.4f), -30f, materials.Stone);
        MakeRock(name + " roca 3", island.transform.parent, new Vector3(center.x, bottom - 7f, center.z), new Vector3(size.x * 0.15f, 3f, size.z * 0.15f), 45f, materials.Stone);
    }

    private static void MakeEdge(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject strip = CreateBlock(name, parent, position, scale, material);
        Object.DestroyImmediate(strip.GetComponent<Collider>());
        strip.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void MakeRock(string name, Transform parent, Vector3 position, Vector3 scale, float yaw, Material material)
    {
        GameObject rock = CreateBlock(name, parent, position, scale, material);
        rock.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Object.DestroyImmediate(rock.GetComponent<Collider>());
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

        // Pantalla final: panel a pantalla completa, oculto hasta cruzar el portal.
        GameObject panelObject = new GameObject("Pantalla final", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        SetRect(panelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        panelObject.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.07f, 0.88f);
        CanvasGroup panelGroup = panelObject.GetComponent<CanvasGroup>();

        TextMeshProUGUI title = CreateText("Titulo", panelObject.transform, font, 96f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1600f, 160f));
        title.text = "Región restaurada";

        TextMeshProUGUI subtitle = CreateText("Subtitulo", panelObject.transform, font, 40f, TextAlignmentOptions.Center);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1600f, 120f));
        subtitle.text = "A lo lejos, otras regiones esperan la luz de Kael…\n(próxima región: placeholder)";

        EndScreen endScreen = canvasObject.AddComponent<EndScreen>();
        SerializedObject endSerialized = new SerializedObject(endScreen);
        Property(endSerialized, "panel").objectReferenceValue = panelGroup;
        endSerialized.ApplyModifiedPropertiesWithoutUndo();
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
