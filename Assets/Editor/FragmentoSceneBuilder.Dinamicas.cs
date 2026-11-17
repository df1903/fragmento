using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Ayudantes del constructor para las dinámicas y peligros propios de cada nivel: hongo de rebote, hoja que se
///   derrumba, corriente, campo de baja gravedad, plataforma en órbita, balsa, haz que barre, zarzas, estalactita
///   y marea. También los marcadores de ruta que usan las pruebas.
///
/// POR QUÉ EXISTE
///   Cada nivel debe jugarse distinto (decisión 2026-11-16). Estas piezas se arman aquí una vez, con primitivas,
///   y cada nivel (FragmentoSceneBuilder.Nivel0N.cs) las coloca donde le toca.
///
/// CÓMO SE USA
///   Se llaman desde el BuildLayout de cada nivel. Todas devuelven el objeto creado, ya con su script configurado.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    // ------------------------------------------------------------------ Ruta (para las pruebas)

    // Anota los saltos normales del recorrido (sin rebote, baja gravedad ni plataformas móviles). La prueba de
    // ruta comprueba que cada salto tiene suelo en los dos extremos y no pide más de lo que Kael puede saltar.
    private class RouteBuilder
    {
        private readonly Transform root;
        private int count;

        public RouteBuilder(Transform level)
        {
            root = new GameObject("Ruta").transform;
            root.SetParent(level, false);
        }

        /// <summary>from = punto de despegue, to = punto de aterrizaje (ambos a la altura del suelo, 0.3 m dentro del borde).</summary>
        public void Jump(Vector3 from, Vector3 to)
        {
            count++;
            Transform jump = new GameObject("Salto " + count.ToString("00")).transform;
            jump.SetParent(root, false);
            AddPoint(jump, "Desde", from);
            AddPoint(jump, "Hasta", to);
        }

        private static void AddPoint(Transform parent, string name, Vector3 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.position = position;
        }
    }

    // ------------------------------------------------------------------ Dinámicas

    // Hongo de rebote: tallo y sombrero (visuales) con una zona que lanza a Kael "bounceHeight" metros hacia arriba.
    private static GameObject CreateBouncePad(Transform parent, string name, Vector3 floorPosition, float bounceHeight, Material stem, Material cap)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;

        CreateVisualPart("Tallo", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.25f, 0.6f), Vector3.zero, stem);
        GameObject capObject = CreateVisualPart("Sombrero", root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 0.7f, 2.4f), Vector3.zero, cap);

        // La zona empieza en el suelo: tanto caer encima como caminar hacia el hongo hace rebotar.
        BoxCollider zone = root.AddComponent<BoxCollider>();
        zone.center = new Vector3(0f, 0.55f, 0f);
        zone.size = new Vector3(2.2f, 1.3f, 2.2f);
        zone.isTrigger = true;

        root.AddComponent<BouncePad>().Setup(bounceHeight, capObject.transform);
        return root;
    }

    // Hoja de piedra: bloque que tiembla y cae al pisarlo, y vuelve a los pocos segundos.
    private static GameObject CreateCrumblingPlatform(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject platform = CreateBlock(name, parent, center, size, material);
        platform.AddComponent<CrumblingPlatform>();
        return platform;
    }

    // Corriente: zona que empuja a Kael, con partículas que viajan en la dirección del empuje para que se vea.
    private static GameObject CreatePushZone(Transform parent, string name, Vector3 center, Vector3 size, Vector3 push, Material particleMaterial, Color color)
    {
        GameObject zoneObject = new GameObject(name);
        zoneObject.transform.SetParent(parent, false);
        zoneObject.transform.position = center;

        BoxCollider box = zoneObject.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        zoneObject.AddComponent<PushZone>().Setup(push);

        GameObject flowObject = new GameObject("Flujo");
        flowObject.transform.SetParent(zoneObject.transform, false);
        flowObject.transform.localPosition = new Vector3(0f, -size.y / 2f + 0.3f, 0f); // a ras del suelo de la zona
        ParticleSystem flow = flowObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = flow.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 250;
        main.startColor = color;

        ParticleSystem.EmissionModule emission = flow.emission;
        emission.rateOverTime = 60f;

        ParticleSystem.ShapeModule shape = flow.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(size.x, 0.3f, size.z);

        ParticleSystem.VelocityOverLifetimeModule velocity = flow.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(push.x);
        velocity.y = new ParticleSystem.MinMaxCurve(push.y);
        velocity.z = new ParticleSystem.MinMaxCurve(push.z);

        flowObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        return zoneObject;
    }

    // Campo de baja gravedad: zona con motas que suben despacio para que se vea dónde se "flota".
    private static GameObject CreateLowGravityZone(Transform parent, string name, Vector3 center, Vector3 size, Material particleMaterial, Color color)
    {
        GameObject zoneObject = new GameObject(name);
        zoneObject.transform.SetParent(parent, false);
        zoneObject.transform.position = center;

        BoxCollider box = zoneObject.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        zoneObject.AddComponent<LowGravityZone>();

        CreateMotes("Motas del campo", zoneObject.transform, center, size, particleMaterial, color, 260, 40f, 0.18f, 0.06f);
        return zoneObject;
    }

    // Plataforma en órbita: gira alrededor de (position + centerOffset). Con "startActive" gira desde el principio.
    private static GameObject CreateOrbitPlatform(string name, string eventId, Vector3 position, Vector3 centerOffset, float degreesPerSecond, bool startActive, Materials materials)
    {
        GameObject platform = CreateBlock(name, null, position, new Vector3(4f, 0.6f, 4f), materials.PlatformOff);

        Rigidbody body = platform.AddComponent<Rigidbody>();
        body.isKinematic = true;

        OrbitPlatformActivate orbit = platform.AddComponent<OrbitPlatformActivate>();
        orbit.Setup(centerOffset, degreesPerSecond, startActive);
        SerializedObject serialized = new SerializedObject(orbit);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "platformRenderer").objectReferenceValue = platform.GetComponent<Renderer>();
        Property(serialized, "inactiveMaterial").objectReferenceValue = materials.PlatformOff;
        Property(serialized, "activeMaterial").objectReferenceValue = materials.PlatformOn;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return platform;
    }

    // Balsa: plataforma móvil más ancha y lenta que la normal. Con "startActive" navega desde el principio.
    private static GameObject CreateRaft(string name, string eventId, Vector3 position, Vector3 travel, float size, bool startActive, Materials materials)
    {
        GameObject raft = CreateMovingPlatform(name, eventId, position, travel, materials);
        raft.transform.localScale = new Vector3(size, 0.6f, size);

        SerializedObject serialized = new SerializedObject(raft.GetComponent<MovingPlatformActivate>());
        Property(serialized, "speed").floatValue = 3f;
        Property(serialized, "pauseSeconds").floatValue = 2.5f;
        Property(serialized, "activeFromStart").boolValue = startActive;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return raft;
    }

    // Objetos que "despiertan" con un fragmento (hongos, campo de baja gravedad): se apagan hasta que llega el evento.
    private static GameObject CreateActivator(string name, string eventId, params GameObject[] targets)
    {
        GameObject root = new GameObject(name);
        Transform[] list = new Transform[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].transform.SetParent(root.transform, true);
            list[i] = targets[i].transform;
        }

        ActivateOnEvent activator = root.AddComponent<ActivateOnEvent>();
        activator.SetTargets(list);
        SerializedObject serialized = new SerializedObject(activator);
        Property(serialized, "eventId").stringValue = eventId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(activator);
        return root;
    }

    // ------------------------------------------------------------------ Peligros

    // Haz que barre: pilar central y un haz rojo de "length" metros que gira a ras de suelo.
    private static GameObject CreateSweepingBeam(Transform parent, string name, Vector3 floorPosition, float length, float degreesPerSecond, Materials materials, Material pillarMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;

        CreateVisualPart("Pilar", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), Vector3.zero, pillarMaterial);
        CreateVisualPart("Lente", root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0f), Vector3.one * 0.6f, Vector3.zero, materials.Enemy);

        GameObject pivot = new GameObject("Pivote");
        pivot.transform.SetParent(root.transform, false);
        NoShadow(CreateVisualPart("Haz", pivot.transform, PrimitiveType.Cube, new Vector3(0f, 0.4f, length / 2f), new Vector3(0.16f, 0.16f, length), Vector3.zero, materials.Enemy));

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(pivot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.8f, length / 2f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.1f, 0.05f);
        light.range = 5f;
        light.intensity = 3f;
        light.shadows = LightShadows.None;

        root.AddComponent<SweepingBeam>().Setup(pivot.transform, length, degreesPerSecond);
        return root;
    }

    // Zarzas: placa oscura en el suelo con púas que brotan y se esconden. size = (ancho X, fondo Z).
    private static GameObject CreateThornPatch(Transform parent, string name, Vector3 floorPosition, Vector2 size, float phaseOffset, Materials materials, Material thornMaterial)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;

        NoShadow(CreateVisualPart("Placa", root.transform, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(size.x, 0.06f, size.y), Vector3.zero, thornMaterial));

        // Las púas cuelgan de un pivote en el suelo: al escalar el pivote en Y crecen hacia arriba.
        GameObject spikes = new GameObject("Puas");
        spikes.transform.SetParent(root.transform, false);
        int columns = Mathf.Max(2, Mathf.RoundToInt(size.x / 0.9f));
        int rows = Mathf.Max(2, Mathf.RoundToInt(size.y / 0.9f));
        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                Vector3 position = new Vector3(Mathf.Lerp(-size.x / 2f + 0.4f, size.x / 2f - 0.4f, x / (columns - 1f)), 0.45f,
                    Mathf.Lerp(-size.y / 2f + 0.4f, size.y / 2f - 0.4f, z / (rows - 1f)));
                NoShadow(CreateVisualPart("Pua", spikes.transform, PrimitiveType.Cube, position, new Vector3(0.14f, 0.9f, 0.14f), new Vector3(0f, 45f, 0f), materials.Enemy));
            }
        }

        root.AddComponent<ThornPatch>().Setup(spikes.transform, size, phaseOffset);
        return root;
    }

    // Estalactita que cae: la raíz va en el suelo; la roca cuelga "hangHeight" metros por encima.
    private static GameObject CreateFallingStalactite(Transform parent, string name, Vector3 floorPosition, float hangHeight, Material rockMaterial, Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = floorPosition;

        GameObject rock = new GameObject("Roca");
        rock.transform.SetParent(root.transform, false);
        CreateVisualPart("Cuerpo", rock.transform, PrimitiveType.Cube, new Vector3(0f, 2f, 0f), new Vector3(1.1f, 4f, 1.1f), new Vector3(0f, 45f, 0f), rockMaterial);
        CreateVisualPart("Punta", rock.transform, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), new Vector3(45f, 45f, 0f), materials.Enemy);

        GameObject warning = CreateVisualPart("Aviso", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(2.6f, 0.02f, 2.6f), Vector3.zero, materials.Enemy);
        NoShadow(warning);

        root.AddComponent<FallingStalactite>().Setup(rock.transform, warning, hangHeight);
        return root;
    }

    // Marea: raíz que sube y baja con la lámina de agua dentro. Lo que se cuelgue de la raíz flota con el agua.
    private static Transform CreateTide(Transform parent, Vector3 lowCenter, float rise, Material water)
    {
        GameObject tide = new GameObject("Marea");
        tide.transform.SetParent(parent, false);
        tide.transform.position = lowCenter;

        GameObject sheet = CreateBlock("Agua del lago", tide.transform, lowCenter + Vector3.down * 0.05f, new Vector3(700f, 0.1f, 700f), water);
        Object.DestroyImmediate(sheet.GetComponent<Collider>());
        NoShadow(sheet);

        tide.AddComponent<TideWater>().Setup(rise);
        return tide.transform;
    }
}
