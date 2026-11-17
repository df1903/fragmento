using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// QUÉ HACE
///   Construye el Kael humano de los niveles 6 y 7: un cuerpo con articulaciones (cadera, rodillas, hombros,
///   codos, cuello), rostro, pelo, ropa y capa. Le añade la animación por script (KaelAnimator), el Pulso de
///   luz (LightPulse), correr y empujar cajas.
///
/// POR QUÉ EXISTE
///   Es el final del arco de materialización: Kael ya no es una figura de luz sino una persona. En el nivel 6
///   su piel aún brilla; en el 7 la luz solo le queda en el pecho. Cada articulación es un objeto vacío
///   ("pivote") y la pieza visible cuelga de él: girar el pivote dobla la articulación.
///
/// CÓMO SE USA
///   Lo llaman los constructores de los niveles 6 y 7 (campo CreateKael del LevelTheme). Mide 1.75 m.
///   Jerarquía: Visual > Cadera > (Muslo > Pantorrilla) x2 y Torso > (Cabeza, Brazo > Antebrazo x2, Capa).
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const float HumanKaelHeight = 1.75f;

    private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        return pivot;
    }

    // stage 6: piel con brillo y túnica azul. stage 7: piel normal, ropa de tierra, capucha y zurrón.
    // stage 8: ropa de ciudad (chaqueta) y un halo tenue. stage 9: ropa de casa (jersey), sin capa ni brillo.
    private static GameObject CreateHumanKaelPrefab(Materials materials, RealMaterials real, string prefabName, int stage)
    {
        bool human = stage >= 7;
        bool modern = stage >= 8;
        Material skin = human ? GetOrCreateMaterial("RealPielHumana", new Color(0.82f, 0.62f, 0.5f), null, 0f, 0.35f) : real.Skin;
        Material tunic = human ? real.ClothDark : real.Cloth;
        Material cloak = human ? GetOrCreateTexturedMaterialCopy("RealCapaVerde", real.Cloth, new Color(0.16f, 0.24f, 0.16f)) : real.ClothDark;
        Material trousers = GetOrCreateTexturedMaterialCopy("RealPantalon", real.Cloth, new Color(0.17f, 0.16f, 0.2f));
        if (stage == 8)
        {
            tunic = GetOrCreateTexturedMaterialCopy("RealChaqueta", real.Cloth, new Color(0.12f, 0.17f, 0.26f));
            cloak = tunic;
            trousers = GetOrCreateTexturedMaterialCopy("RealVaquero", real.Cloth, new Color(0.2f, 0.25f, 0.34f));
        }
        else if (stage >= 10)
        {
            // Pijama: Kael acaba de despertar.
            tunic = GetOrCreateTexturedMaterialCopy("RealPijama", real.Cloth, new Color(0.5f, 0.64f, 0.85f));
            trousers = tunic;
        }
        else if (stage >= 9)
        {
            tunic = GetOrCreateTexturedMaterialCopy("RealJersey", real.Cloth, new Color(0.62f, 0.2f, 0.16f));
            trousers = GetOrCreateTexturedMaterialCopy("RealPantalonCasa", real.Cloth, new Color(0.3f, 0.27f, 0.22f));
        }

        GameObject root = new GameObject("Kael");
        root.tag = "Player";

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = HumanKaelHeight;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, HumanKaelHeight / 2f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 50f;
        controller.skinWidth = 0.035f;

        // La raíz no gira (la cámara cuelga de ella); solo gira "Visual".
        Transform visual = CreatePivot("Visual", root.transform, Vector3.zero);

        // --- Cadera y piernas ---
        Transform hips = CreatePivot("Cadera", visual, new Vector3(0f, 0.93f, 0f));
        CreateVisualPart("Pelvis", hips, PrimitiveType.Cube, Vector3.zero, new Vector3(0.3f, 0.16f, 0.18f), Vector3.zero, trousers);
        CreateVisualPart("Faldon", hips, PrimitiveType.Cylinder, new Vector3(0f, -0.1f, 0f), new Vector3(0.37f, 0.13f, 0.27f), Vector3.zero, tunic);

        Transform[] thighs = new Transform[2], shins = new Transform[2];
        string[] sides = { "izquierdo", "derecho" };
        string[] sidesFeminine = { "izquierda", "derecha" };
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.085f : 0.085f;
            thighs[i] = CreatePivot("Muslo " + sides[i], hips, new Vector3(x, -0.02f, 0f));
            CreateVisualPart("Pierna", thighs[i], PrimitiveType.Capsule, new Vector3(0f, -0.21f, 0f), new Vector3(0.15f, 0.22f, 0.15f), Vector3.zero, trousers);
            shins[i] = CreatePivot("Pantorrilla " + sidesFeminine[i], thighs[i], new Vector3(0f, -0.43f, 0f));
            CreateVisualPart("Bota", shins[i], PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.21f, 0.13f), Vector3.zero, real.Leather);
            CreateVisualPart("Pie", shins[i], PrimitiveType.Cube, new Vector3(0f, -0.43f, 0.05f), new Vector3(0.11f, 0.08f, 0.24f), Vector3.zero, real.Leather);
        }

        // --- Torso ---
        Transform torso = CreatePivot("Torso", hips, new Vector3(0f, 0.06f, 0f));
        CreateVisualPart("Pecho", torso, PrimitiveType.Capsule, new Vector3(0f, 0.24f, 0f), new Vector3(0.33f, 0.27f, 0.2f), Vector3.zero, tunic);
        CreateVisualPart("Cinturon", torso, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.35f, 0.06f, 0.23f), Vector3.zero, real.Leather);
        CreateVisualPart("Cuello", torso, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.09f, 0.05f, 0.09f), Vector3.zero, skin);
        // El corazón de luz: lo que queda del Kael de los primeros niveles. En la etapa 9 ya no queda.
        if (stage < 9)
        {
            NoShadow(CreateVisualPart("Corazon de luz", torso, PrimitiveType.Sphere, new Vector3(0f, 0.33f, 0.09f), Vector3.one * (human ? 0.07f : 0.1f), Vector3.zero, real.Glow));
        }

        // --- Cabeza ---
        Transform head = CreatePivot("Cabeza", torso, new Vector3(0f, 0.56f, 0f));
        CreateVisualPart("Cara", head, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.24f, 0.21f), Vector3.zero, skin);
        CreateVisualPart("Pelo", head, PrimitiveType.Sphere, new Vector3(0f, 0.15f, -0.025f), new Vector3(0.225f, 0.2f, 0.23f), Vector3.zero, real.Hair);
        CreateVisualPart("Flequillo", head, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0.07f), new Vector3(0.17f, 0.05f, 0.07f), new Vector3(20f, 0f, 0f), real.Hair);
        CreateVisualPart("Nariz", head, PrimitiveType.Cube, new Vector3(0f, 0.085f, 0.105f), new Vector3(0.025f, 0.05f, 0.03f), Vector3.zero, skin);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.045f : 0.045f;
            CreateVisualPart("Ojo " + sides[i], head, PrimitiveType.Sphere, new Vector3(x, 0.115f, 0.092f), Vector3.one * 0.036f, Vector3.zero, human ? real.Hair : real.Glow);
            CreateVisualPart("Ceja " + sidesFeminine[i], head, PrimitiveType.Cube, new Vector3(x, 0.15f, 0.097f), new Vector3(0.05f, 0.012f, 0.02f), Vector3.zero, real.Hair);
        }

        if (stage == 7)
        {
            CreateVisualPart("Capucha", head, PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.055f), new Vector3(0.27f, 0.3f, 0.27f), Vector3.zero, cloak);
        }

        // --- Brazos ---
        Transform[] arms = new Transform[2], forearms = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.2f : 0.2f;
            arms[i] = CreatePivot("Brazo " + sides[i], torso, new Vector3(x, 0.42f, 0f));
            CreateVisualPart("Manga", arms[i], PrimitiveType.Capsule, new Vector3(0f, -0.14f, 0f), new Vector3(0.1f, 0.15f, 0.1f), Vector3.zero, tunic);
            forearms[i] = CreatePivot("Antebrazo " + sides[i], arms[i], new Vector3(0f, -0.28f, 0f));
            CreateVisualPart("Antebrazo", forearms[i], PrimitiveType.Capsule, new Vector3(0f, -0.12f, 0f), new Vector3(0.085f, 0.13f, 0.085f), Vector3.zero, skin);
            CreateVisualPart("Mano", forearms[i], PrimitiveType.Sphere, new Vector3(0f, -0.27f, 0f), Vector3.one * 0.09f, Vector3.zero, skin);
        }

        // --- Capa (cuelga de los hombros y se levanta al correr) y zurrón ---
        // Etapa 8: en vez de capa, el faldón de la chaqueta (más corto). Etapa 9: nada.
        Transform cape = null;
        if (stage < 9)
        {
            cape = CreatePivot("Capa", torso, new Vector3(0f, modern ? 0.1f : 0.46f, -0.11f));
            CreateVisualPart("Tela", cape, PrimitiveType.Cube, new Vector3(0f, modern ? -0.14f : -0.4f, -0.02f), new Vector3(0.36f, modern ? 0.3f : 0.8f, 0.03f), Vector3.zero, cloak);
        }

        if (stage == 8)
        {
            // Halo tenue: un aro de luz fino sobre la cabeza, lo último de luz que le queda a Kael.
            GameObject halo = new GameObject("Halo");
            halo.transform.SetParent(visual, false);
            halo.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            LineRenderer haloLine = halo.AddComponent<LineRenderer>();
            haloLine.useWorldSpace = false;
            haloLine.loop = true;
            haloLine.widthMultiplier = 0.025f;
            haloLine.sharedMaterial = materials.Trail;
            haloLine.shadowCastingMode = ShadowCastingMode.Off;
            haloLine.startColor = new Color(0.7f, 1f, 1f);
            haloLine.endColor = new Color(0.7f, 1f, 1f);
            haloLine.positionCount = 32;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                haloLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.17f, 0f, Mathf.Sin(angle) * 0.17f));
            }
            CreateVisualPart("Cuello de la chaqueta", torso, PrimitiveType.Cube, new Vector3(0f, 0.46f, -0.02f), new Vector3(0.3f, 0.08f, 0.22f), Vector3.zero, tunic);
        }

        if (stage >= 10)
        {
            CreateVisualPart("Cuello del pijama", torso, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.07f), new Vector3(0.22f, 0.06f, 0.08f), new Vector3(-20f, 0f, 0f), real.Cloth);
            for (int i = 0; i < 3; i++)
            {
                CreateVisualPart("Boton", torso, PrimitiveType.Sphere, new Vector3(0f, 0.36f - i * 0.11f, 0.1f), Vector3.one * 0.025f, Vector3.zero, real.Cloth);
            }
        }

        if (stage == 9)
        {
            CreateVisualPart("Cuello del jersey", torso, PrimitiveType.Cylinder, new Vector3(0f, 0.47f, 0f), new Vector3(0.16f, 0.04f, 0.16f), Vector3.zero, tunic);
        }

        if (stage == 7)
        {
            CreateVisualPart("Zurron", torso, PrimitiveType.Cube, new Vector3(0.2f, 0f, -0.03f), new Vector3(0.1f, 0.2f, 0.18f), Vector3.zero, real.Leather);
            CreateVisualPart("Correa", torso, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0.105f), new Vector3(0.46f, 0.035f, 0.01f), new Vector3(0f, 0f, 35f), real.Leather);
        }

        // --- Anillo del Pulso de luz (LineRenderer circular de radio 0.5; LightPulse lo agranda) ---
        GameObject ring = new GameObject("Anillo del Pulso");
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.18f;
        line.sharedMaterial = materials.Trail;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startColor = new Color(0.7f, 1f, 1f);
        line.endColor = new Color(0.7f, 1f, 1f);
        const int points = 48;
        line.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float angle = i * Mathf.PI * 2f / points;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f));
        }

        // Marcador de aterrizaje en el suelo (ver GroundMarker).
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "MarcadorSuelo";
        marker.transform.SetParent(root.transform, false);
        marker.GetComponent<Renderer>().sharedMaterial = materials.Marker;
        marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.AddComponent<GroundMarker>();

        // Luz propia: más tenue cuanto más humano es.
        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.3f, 0.15f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = human ? new Color(0.75f, 0.95f, 1f) : new Color(0.45f, 0.95f, 1f);
        light.range = human ? 6f : 8f;
        light.intensity = stage >= 9 ? 0.8f : human ? 1.8f : 3.2f;
        light.shadows = LightShadows.None;

        GameObject cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(root.transform, false);
        cameraTarget.transform.localPosition = new Vector3(0f, 1.9f, 0f);
        cameraTarget.AddComponent<ThirdPersonCamera>();

        PlayerController player = root.AddComponent<PlayerController>();
        SerializedObject serialized = new SerializedObject(player);
        Property(serialized, "visual").objectReferenceValue = visual;
        Property(serialized, "canSprint").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<MovingPlatformCarrier>();
        root.AddComponent<PlayerPowerUp>();
        root.AddComponent<CratePusher>();
        root.AddComponent<KaelAnimator>().Setup(hips, torso, head, thighs, shins, arms, forearms, cape);
        root.AddComponent<LightPulse>().Setup(ring.transform, light);

        return SavePrefab(root, prefabName);
    }

    // Copia de un material con textura pero con otro tinte (misma tela en otro color).
    private static Material GetOrCreateTexturedMaterialCopy(string name, Material source, Color tint)
    {
        Material material = GetOrCreateMaterial(name, tint, null, 0f, source.GetFloat("_Smoothness"));
        material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
        material.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
        material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }
}
