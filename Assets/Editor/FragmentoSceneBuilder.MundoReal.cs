using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// QUÉ HACE
///   Piezas de juego y de escenario que comparten los niveles 6 y 7: faros y faroles (Beacon), lobos de sombra,
///   Ecos, parches de hierba alta, lianas y escaleras para trepar, casas con interior, cajas, placas de presión,
///   agua, escalones que aparecen, lluvia y los puntos del camino que revisan las pruebas.
///
/// POR QUÉ EXISTE
///   Los constructores de cada nivel deben leerse como un guion ("aquí un faro, aquí una casa"). El detalle de
///   cómo se arma cada pieza con mallas y primitivas vive aquí, una sola vez.
///
/// CÓMO SE USA
///   Se llaman desde FragmentoSceneBuilder.Nivel06.cs y Nivel07.cs. Las posiciones "ground" son puntos del suelo.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    // Suavizado entre dos bordes: 0 en "from", 1 en "to" (también sirve al revés, con from mayor que to).
    private static float Smooth(float from, float to, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    }

    // Distancia de (x, z) al camino (lista de puntos unidos por rectas).
    private static float DistanceToPath(Vector2[] points, float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float best = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[i], b = points[i + 1];
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max((b - a).sqrMagnitude, 0.0001f));
            best = Mathf.Min(best, Vector2.Distance(p, a + (b - a) * t));
        }

        return best;
    }

    // Deja en Nivel/Camino un punto cada 3 m de cada tramo andable. La prueba comprueba que hay suelo y que la
    // pendiente entre puntos seguidos se puede caminar.
    private static void CreatePathMarkers(Transform level, Vector2[][] stretches, System.Func<float, float, float> height)
    {
        Transform root = new GameObject("Camino").transform;
        root.SetParent(level, false);
        for (int s = 0; s < stretches.Length; s++)
        {
            Transform stretch = new GameObject("Tramo " + (s + 1).ToString("00")).transform;
            stretch.SetParent(root, false);
            Vector2[] points = stretches[s];
            for (int i = 0; i < points.Length - 1; i++)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(points[i], points[i + 1]) / 3f));
                for (int k = 0; k < steps + (i == points.Length - 2 ? 1 : 0); k++)
                {
                    Vector2 p = Vector2.Lerp(points[i], points[i + 1], k / (float)steps);
                    Transform marker = new GameObject("Punto").transform;
                    marker.SetParent(stretch, false);
                    marker.position = new Vector3(p.x, height(p.x, p.y), p.y);
                }
            }
        }
    }

    // Pared invisible (solo collider): cierra barrancos y bordes para que no se puedan escalar a saltos.
    private static void CreateInvisibleWall(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = center;
        wall.AddComponent<BoxCollider>().size = size;
    }

    // ------------------------------------------------------------------ Faros y faroles

    // Faro de piedra (lantern = false) o farol de poste (lantern = true). Apagado hasta que lo alcanza un Pulso,
    // salvo startLit. Encendido: llama, luz cálida y un aro en el suelo que marca la zona segura.
    private static Beacon CreateBeacon(Transform parent, string name, Vector3 ground, float safeRadius, bool startLit, string eventId, bool lantern, RealMaterials real, Materials materials)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = ground;

        Vector3 flamePosition;
        if (lantern)
        {
            CreateVisualPart("Poste", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.16f, 1.6f, 0.16f), Vector3.zero, real.DarkPlanks);
            CreateVisualPart("Brazo", root.transform, PrimitiveType.Cube, new Vector3(0f, 3.1f, 0.35f), new Vector3(0.1f, 0.1f, 0.8f), Vector3.zero, real.DarkPlanks);
            CreateVisualPart("Farol", root.transform, PrimitiveType.Cube, new Vector3(0f, 2.75f, 0.65f), new Vector3(0.32f, 0.42f, 0.32f), Vector3.zero, real.Glass);
            CreateVisualPart("Tapa", root.transform, PrimitiveType.Cube, new Vector3(0f, 3f, 0.65f), new Vector3(0.42f, 0.08f, 0.42f), Vector3.zero, real.DarkPlanks);
            flamePosition = new Vector3(0f, 2.75f, 0.65f);
            CapsuleCollider post = root.AddComponent<CapsuleCollider>();
            post.radius = 0.15f;
            post.height = 3.2f;
            post.center = Vector3.up * 1.6f;
        }
        else
        {
            CreateVisualPart("Base", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.3f, 1.5f), Vector3.zero, real.StoneWall);
            CreateVisualPart("Columna", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 0.5f, 0.8f), Vector3.zero, real.StoneWall);
            CreateVisualPart("Cuenco", root.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(1.1f, 0.12f, 1.1f), Vector3.zero, real.Rock);
            flamePosition = new Vector3(0f, 1.95f, 0f);
            CapsuleCollider column = root.AddComponent<CapsuleCollider>();
            column.radius = 0.6f;
            column.height = 1.8f;
            column.center = Vector3.up * 0.9f;
        }

        GameObject lit = new GameObject("Encendido");
        lit.transform.SetParent(root.transform, false);
        NoShadow(CreateVisualPart("Llama", lit.transform, PrimitiveType.Sphere, flamePosition, new Vector3(0.28f, lantern ? 0.3f : 0.6f, 0.28f), Vector3.zero, real.Flame));

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(lit.transform, false);
        lightObject.transform.localPosition = flamePosition + Vector3.up * 0.3f;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.72f, 0.4f);
        light.range = safeRadius * 1.6f;
        light.intensity = 9f;
        light.shadows = LightShadows.None;

        // Aro en el suelo: hasta aquí llega la zona segura.
        GameObject ring = new GameObject("Aro seguro");
        ring.transform.SetParent(lit.transform, false);
        ring.transform.localPosition = Vector3.up * 0.15f;
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.08f;
        line.sharedMaterial = materials.Trail;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.startColor = new Color(1f, 0.75f, 0.4f, 0.8f);
        line.endColor = new Color(1f, 0.75f, 0.4f, 0.8f);
        const int points = 48;
        line.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float angle = i * Mathf.PI * 2f / points;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * safeRadius, 0f, Mathf.Sin(angle) * safeRadius));
        }

        Beacon beacon = root.AddComponent<Beacon>();
        beacon.Setup(lit, safeRadius, startLit, eventId);
        return beacon;
    }

    // ------------------------------------------------------------------ Enemigos

    // Lobo de sombra: cuerpo negro a cuatro patas con ojos rojos. Mira hacia +Z de "Cuerpo".
    private static GameObject CreateShadowWolf(Transform parent, string name, Vector3 ground, float prowlRadius, RealMaterials real)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = ground;

        Transform body = CreatePivot("Cuerpo", root.transform, Vector3.zero);
        CreateVisualPart("Lomo", body, PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f), new Vector3(0.5f, 0.7f, 0.55f), new Vector3(90f, 0f, 0f), real.Shadow);
        CreateVisualPart("Pecho", body, PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0.45f), new Vector3(0.6f, 0.65f, 0.6f), Vector3.zero, real.Shadow);
        CreateVisualPart("Cabeza", body, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.85f), new Vector3(0.4f, 0.38f, 0.45f), Vector3.zero, real.Shadow);
        CreateVisualPart("Hocico", body, PrimitiveType.Cube, new Vector3(0f, 0.95f, 1.12f), new Vector3(0.18f, 0.16f, 0.34f), Vector3.zero, real.Shadow);
        CreateVisualPart("Cola", body, PrimitiveType.Capsule, new Vector3(0f, 0.8f, -0.85f), new Vector3(0.14f, 0.35f, 0.14f), new Vector3(60f, 0f, 0f), real.Shadow);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.2f : 0.2f;
            float z = i < 2 ? 0.45f : -0.5f;
            CreateVisualPart("Pata " + (i + 1), body, PrimitiveType.Capsule, new Vector3(x, 0.32f, z), new Vector3(0.13f, 0.34f, 0.13f), Vector3.zero, real.Shadow);
        }

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.1f : 0.1f;
            CreateVisualPart("Oreja " + (i + 1), body, PrimitiveType.Cube, new Vector3(x * 1.3f, 1.25f, 0.8f), new Vector3(0.09f, 0.2f, 0.07f), new Vector3(-15f, 0f, x * 150f), real.Shadow);
            NoShadow(CreateVisualPart("Ojo " + (i + 1), body, PrimitiveType.Sphere, new Vector3(x, 1.07f, 1.03f), Vector3.one * 0.07f, Vector3.zero, real.ShadowEye));
        }

        GameObject lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(body, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.1f, 1.2f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.12f, 0.05f);
        light.range = 3.5f;
        light.intensity = 2.5f;
        light.shadows = LightShadows.None;

        root.AddComponent<ShadowWolf>().Setup(body, prowlRadius);
        return root;
    }

    // Eco: silueta de aldeano hecha de sombra, con sombrero de ala ancha y dos ojos pálidos.
    private static GameObject CreateEcho(Transform parent, string name, Vector3 ground, float yaw, RealMaterials real)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = ground;

        Transform body = CreatePivot("Cuerpo", root.transform, Vector3.zero);
        body.localRotation = Quaternion.Euler(0f, yaw, 0f);
        CreateVisualPart("Sayo", body, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.62f, 0.6f, 0.5f), Vector3.zero, real.Shadow);
        CreateVisualPart("Torso", body, PrimitiveType.Capsule, new Vector3(0f, 1.3f, 0f), new Vector3(0.48f, 0.4f, 0.32f), Vector3.zero, real.Shadow);
        CreateVisualPart("Cabeza", body, PrimitiveType.Sphere, new Vector3(0f, 1.82f, 0f), new Vector3(0.28f, 0.32f, 0.28f), Vector3.zero, real.Shadow);
        CreateVisualPart("Ala del sombrero", body, PrimitiveType.Cylinder, new Vector3(0f, 1.98f, 0f), new Vector3(0.75f, 0.02f, 0.75f), Vector3.zero, real.Shadow);
        CreateVisualPart("Copa del sombrero", body, PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), new Vector3(0.32f, 0.12f, 0.32f), Vector3.zero, real.Shadow);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -1f : 1f;
            // Brazos caídos, un poco separados del cuerpo: se lee la figura humana aun de lejos.
            CreateVisualPart("Brazo " + (i + 1), body, PrimitiveType.Capsule, new Vector3(x * 0.33f, 1.15f, 0f), new Vector3(0.12f, 0.42f, 0.12f), new Vector3(0f, 0f, x * 8f), real.Shadow);
            NoShadow(CreateVisualPart("Ojo " + (i + 1), body, PrimitiveType.Sphere, new Vector3(x * 0.07f, 1.85f, 0.125f), Vector3.one * 0.045f, Vector3.zero, real.Flame));
        }

        root.AddComponent<Echo>().Setup(body);
        return root;
    }

    // ------------------------------------------------------------------ Hierba, trepar, agua

    // Parche de hierba: una malla con todas las matas. Si "hides", además es hierba alta que esconde a Kael.
    private static GameObject CreateGrassPatch(Transform parent, string name, Rect area, int tufts, float tall, System.Func<float, float, float> height,
        System.Func<float, float, bool> allowed, Material material, int seed, bool hides)
    {
        Mesh mesh = SaveMesh(BuildGrassMesh(area, tufts, tall, height, allowed, seed), "Hierba_" + name.Replace(" ", ""));
        GameObject patch = CreateMeshObject(name, parent, mesh, Vector3.zero, false, material);
        patch.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        if (hides)
        {
            GameObject zone = new GameObject("Zona de hierba alta");
            zone.transform.SetParent(patch.transform, false);
            float ground = height(area.center.x, area.center.y);
            zone.transform.position = new Vector3(area.center.x, ground + 2f, area.center.y);
            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.size = new Vector3(area.width, 12f, area.height);
            box.isTrigger = true;
            zone.AddComponent<TallGrass>();
        }

        return patch;
    }

    // Liana (ladder = false) o escalera de mano (ladder = true) pegada a una pared. "outward" es hacia dónde mira
    // la pared (solo ±X o ±Z). La zona de trepar sobresale 1 m por encima para poder pasar arriba.
    private static GameObject CreateClimb(Transform parent, string name, Vector3 basePosition, float height, Vector3 outward, bool ladder, RealMaterials real)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = basePosition;
        root.transform.rotation = Quaternion.LookRotation(outward, Vector3.up); // el +Z local sale de la pared

        if (ladder)
        {
            CreateVisualPart("Larguero izquierdo", root.transform, PrimitiveType.Cube, new Vector3(-0.35f, height / 2f, 0.12f), new Vector3(0.08f, height + 0.6f, 0.08f), Vector3.zero, real.DarkPlanks);
            CreateVisualPart("Larguero derecho", root.transform, PrimitiveType.Cube, new Vector3(0.35f, height / 2f, 0.12f), new Vector3(0.08f, height + 0.6f, 0.08f), Vector3.zero, real.DarkPlanks);
            for (float y = 0.4f; y < height + 0.3f; y += 0.4f)
            {
                CreateVisualPart("Peldano", root.transform, PrimitiveType.Cube, new Vector3(0f, y, 0.12f), new Vector3(0.7f, 0.05f, 0.06f), Vector3.zero, real.Planks);
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -0.6f + i * 0.4f;
                NoShadow(CreateVisualPart("Liana " + (i + 1), root.transform, PrimitiveType.Cylinder, new Vector3(x, height / 2f, 0.1f + (i % 2) * 0.08f),
                    new Vector3(0.1f, height / 2f + 0.3f, 0.1f), new Vector3(0f, 0f, (i - 1.5f) * 1.5f), real.Vine));
                for (float y = 0.8f + i * 0.3f; y < height; y += 1.3f)
                {
                    NoShadow(CreateVisualPart("Hoja", root.transform, PrimitiveType.Sphere, new Vector3(x + 0.15f, y, 0.2f), new Vector3(0.35f, 0.08f, 0.22f), new Vector3(0f, i * 40f, 25f), real.Leaves));
                }
            }
        }

        // Zona: 1.8 m de ancho, 1.3 m de fondo delante de la pared, y 1 m más alta que el borde.
        GameObject zone = new GameObject("Zona de trepar");
        zone.transform.SetParent(root.transform, false);
        zone.transform.position = basePosition + outward.normalized * 0.65f + Vector3.up * (height + 1f) / 2f;
        bool alongX = Mathf.Abs(outward.x) > 0.5f;
        BoxCollider box = zone.AddComponent<BoxCollider>();
        zone.transform.rotation = Quaternion.identity; // la caja se mide en ejes del mundo (ver PlayerZone)
        box.size = alongX ? new Vector3(1.3f, height + 1f, 1.8f) : new Vector3(1.8f, height + 1f, 1.3f);
        box.isTrigger = true;
        zone.AddComponent<ClimbZone>().Setup(-outward);
        return root;
    }

    // Lámina de agua que corre (la textura se desliza). Solo visual.
    private static GameObject CreateWaterSheet(Transform parent, string name, Vector3 center, Vector2 size, Vector2 flow, RealMaterials real)
    {
        GameObject water = CreateRealBlock(name, parent, center, new Vector3(size.x, 0.1f, size.y), real.Water, false);
        NoShadow(water);
        water.AddComponent<UvScroll>().Setup(flow);
        return water;
    }

    // Lluvia: un sistema de partículas que cuelga de Kael y cae a su alrededor.
    private static void CreateRain(Transform kael, Material particleMaterial)
    {
        GameObject rainObject = new GameObject("Lluvia");
        rainObject.transform.SetParent(kael, false);
        rainObject.transform.localPosition = new Vector3(0f, 16f, 0f);
        ParticleSystem rain = rainObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = rain.main;
        main.loop = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2600;
        main.startColor = new Color(0.8f, 0.9f, 1f, 0.45f);
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = rain.emission;
        emission.rateOverTime = 2200f;

        ParticleSystem.ShapeModule shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(44f, 0.5f, 44f);

        ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-2.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-17f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        // Cada gota se estira en la dirección en que cae: se ve como una raya, no como un punto.
        ParticleSystemRenderer renderer = rainObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 1f;
        renderer.sharedMaterial = particleMaterial;
    }

    // ------------------------------------------------------------------ Eventos y puzles

    // Evento "esto se mueve": target se desplaza "offset" y gira "rotation" cuando llega eventId.
    private static GameObject CreateMoveEvent(string name, string eventId, Transform target, Vector3 offset, Vector3 rotation, float seconds)
    {
        GameObject root = new GameObject(name);
        root.transform.position = target.position;
        MoveOnEvent move = root.AddComponent<MoveOnEvent>();
        move.Setup(target, offset, rotation, seconds);
        SerializedObject serialized = new SerializedObject(move);
        Property(serialized, "eventId").stringValue = eventId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(move);
        return root;
    }

    // Escalones que aparecen uno tras otro (CrystalGrow con losas finas en vez de columnas). tops = cara superior de cada uno.
    private static GameObject CreateStepChain(string name, string eventId, Material material, Vector3 size, Vector3[] tops)
    {
        GameObject root = new GameObject(name);
        Transform[] pivots = new Transform[tops.Length];
        for (int i = 0; i < tops.Length; i++)
        {
            Transform pivot = new GameObject("Escalon " + (i + 1)).transform;
            pivot.SetParent(root.transform, false);
            pivot.position = tops[i] + Vector3.down * size.y;
            CreateRealBlock("Cuerpo", pivot, pivot.position + Vector3.up * size.y / 2f, size, material);
            pivots[i] = pivot;
        }

        CrystalGrow grow = root.AddComponent<CrystalGrow>();
        grow.SetCrystals(pivots);
        SerializedObject serialized = new SerializedObject(grow);
        Property(serialized, "eventId").stringValue = eventId;
        Property(serialized, "growSeconds").floatValue = 0.35f;
        Property(serialized, "stagger").floatValue = 0.15f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(grow);
        return root;
    }

    // Caja de madera que Kael puede empujar (ver CratePusher).
    private static GameObject CreateCrate(Transform parent, string name, Vector3 ground, RealMaterials real)
    {
        GameObject crate = CreateRealBlock(name, parent, ground + Vector3.up * 0.6f, Vector3.one * 1.2f, real.Planks);
        Rigidbody body = crate.AddComponent<Rigidbody>();
        body.mass = 30f;
        body.linearDamping = 4f;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        return crate;
    }

    // Placa de presión en el suelo. Solo una caja la pulsa (onlyCrate) o también Kael.
    private static PressurePlate CreatePressurePlate(Transform parent, string name, Vector3 ground, string eventId, bool onlyCrate, string message, RealMaterials real)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = ground;
        CreateVisualPart("Marco", root.transform, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(2f, 0.06f, 2f), Vector3.zero, real.StoneWall);
        GameObject plate = CreateVisualPart("Placa", root.transform, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(1.6f, 0.1f, 1.6f), Vector3.zero, real.Flame);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.6f, 0f);
        box.size = new Vector3(1.3f, 1f, 1.3f);
        box.isTrigger = true;

        PressurePlate pressure = root.AddComponent<PressurePlate>();
        pressure.Setup(eventId, onlyCrate, plate.transform, message);
        return pressure;
    }

    // ------------------------------------------------------------------ Casas

    // Casa de entramado de madera. center = centro en el suelo; el caballete del tejado corre a lo largo de Z.
    // doorSide: 0 sin puerta (bloque macizo), 1 sur, 2 norte, 3 este, 4 oeste. Con puerta se puede entrar.
    // flatRoof: azotea de tablones a la que se puede subir (en vez de tejado a dos aguas).
    private static GameObject CreateHouse(Transform parent, string name, Vector3 center, float width, float depth, float wallHeight, int doorSide, bool flatRoof, RealMaterials real, System.Random rng)
    {
        GameObject house = new GameObject(name);
        house.transform.SetParent(parent, false);
        house.transform.position = center;
        Transform t = house.transform;
        const float wall = 0.3f;
        float halfWidth = width / 2f, halfDepth = depth / 2f;

        // Zócalo de piedra y paredes.
        CreateRealBlock("Zocalo", t, center + Vector3.up * 0.25f, new Vector3(width + 0.3f, 0.5f, depth + 0.3f), real.StoneWall, false);
        if (doorSide == 0)
        {
            CreateRealBlock("Muros", t, center + Vector3.up * wallHeight / 2f, new Vector3(width, wallHeight, depth), real.Plaster);
        }
        else
        {
            CreateHouseWall(t, "Muro sur", center + new Vector3(0f, 0f, -halfDepth + wall / 2f), width, wallHeight, false, doorSide == 1, real);
            CreateHouseWall(t, "Muro norte", center + new Vector3(0f, 0f, halfDepth - wall / 2f), width, wallHeight, false, doorSide == 2, real);
            CreateHouseWall(t, "Muro este", center + new Vector3(halfWidth - wall / 2f, 0f, 0f), depth, wallHeight, true, doorSide == 3, real);
            CreateHouseWall(t, "Muro oeste", center + new Vector3(-halfWidth + wall / 2f, 0f, 0f), depth, wallHeight, true, doorSide == 4, real);
            CreateRealBlock("Suelo", t, center + Vector3.up * 0.04f, new Vector3(width - 0.4f, 0.08f, depth - 0.4f), real.Planks, false);
        }

        // Entramado: postes en las esquinas y vigas a media altura y arriba (solo visual).
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = new Vector3((i % 2 == 0 ? -1f : 1f) * (halfWidth + 0.03f), wallHeight / 2f, (i < 2 ? -1f : 1f) * (halfDepth + 0.03f));
            CreateRealBlock("Poste", t, center + corner, new Vector3(0.26f, wallHeight, 0.26f), real.DarkPlanks, false);
        }
        float[] beamHeights = { wallHeight * 0.5f, wallHeight - 0.13f };
        foreach (float y in beamHeights)
        {
            CreateRealBlock("Viga sur", t, center + new Vector3(0f, y, -halfDepth - 0.04f), new Vector3(width, 0.2f, 0.1f), real.DarkPlanks, false);
            CreateRealBlock("Viga norte", t, center + new Vector3(0f, y, halfDepth + 0.04f), new Vector3(width, 0.2f, 0.1f), real.DarkPlanks, false);
            CreateRealBlock("Viga este", t, center + new Vector3(halfWidth + 0.04f, y, 0f), new Vector3(0.1f, 0.2f, depth), real.DarkPlanks, false);
            CreateRealBlock("Viga oeste", t, center + new Vector3(-halfWidth - 0.04f, y, 0f), new Vector3(0.1f, 0.2f, depth), real.DarkPlanks, false);
        }

        // Ventanas en los lados largos (este y oeste), a la altura de los ojos.
        for (int side = -1; side <= 1; side += 2)
        {
            for (float z = -halfDepth + 2f; z <= halfDepth - 2f; z += 3.2f)
            {
                if (rng.NextDouble() < 0.25)
                {
                    continue;
                }

                Vector3 position = center + new Vector3(side * (halfWidth + 0.02f), wallHeight * 0.55f, z);
                CreateRealBlock("Ventana", t, position, new Vector3(0.08f, 1.1f, 0.9f), real.Glass, false);
                CreateRealBlock("Alfeizar", t, position + Vector3.down * 0.62f, new Vector3(0.22f, 0.1f, 1.1f), real.DarkPlanks, false);
            }
        }

        if (flatRoof)
        {
            // Azotea: losa de tablones sólida, sin alero (un alero taparía la cabeza de Kael al subir por una escalera
            // pegada a la pared), con un murete bajo al norte y al sur.
            CreateRealBlock("Azotea", t, center + Vector3.up * (wallHeight + 0.1f), new Vector3(width, 0.2f, depth), real.Planks);
            CreateRealBlock("Murete sur", t, center + new Vector3(0f, wallHeight + 0.4f, -halfDepth - 0.1f), new Vector3(width + 0.4f, 0.4f, 0.2f), real.DarkPlanks, false);
            CreateRealBlock("Murete norte", t, center + new Vector3(0f, wallHeight + 0.4f, halfDepth + 0.1f), new Vector3(width + 0.4f, 0.4f, 0.2f), real.DarkPlanks, false);
        }
        else
        {
            float rise = width * 0.42f;
            GameObject roof = CreateMeshObject("Tejado", t, BuildGableRoofMesh(width, depth, rise, 0.5f), center + Vector3.up * wallHeight, true, real.RoofTiles);
            roof.name = "Tejado";
            // Hastiales (los triángulos bajo el tejado) con tablones, y chimenea.
            CreateRealBlock("Chimenea", t, center + new Vector3(halfWidth * 0.45f, wallHeight + rise * 0.75f, halfDepth * 0.4f), new Vector3(0.7f, rise * 1.1f, 0.7f), real.StoneWall, false);
        }

        MakeStatic(house);
        return house;
    }

    // Una pared de la casa: lisa, o con un hueco de puerta de 1.6 x 2.4 m en el centro. alongZ = pared este/oeste.
    private static void CreateHouseWall(Transform parent, string name, Vector3 baseCenter, float length, float height, bool alongZ, bool door, RealMaterials real)
    {
        const float wall = 0.3f, doorWidth = 1.6f, doorHeight = 2.4f;
        System.Func<float, float, float, float, Vector3[]> piece = (offset, pieceLength, y, pieceHeight) => new[]
        {
            baseCenter + (alongZ ? new Vector3(0f, y, offset) : new Vector3(offset, y, 0f)),
            alongZ ? new Vector3(wall, pieceHeight, pieceLength) : new Vector3(pieceLength, pieceHeight, wall),
        };

        if (!door)
        {
            Vector3[] whole = piece(0f, length, height / 2f, height);
            CreateRealBlock(name, parent, whole[0], whole[1], real.Plaster);
            return;
        }

        float sideLength = (length - doorWidth) / 2f;
        Vector3[] left = piece(-(doorWidth + sideLength) / 2f, sideLength, height / 2f, height);
        Vector3[] right = piece((doorWidth + sideLength) / 2f, sideLength, height / 2f, height);
        Vector3[] lintel = piece(0f, doorWidth, doorHeight + (height - doorHeight) / 2f, height - doorHeight);
        CreateRealBlock(name + " izquierda", parent, left[0], left[1], real.Plaster);
        CreateRealBlock(name + " derecha", parent, right[0], right[1], real.Plaster);
        CreateRealBlock(name + " dintel", parent, lintel[0], lintel[1], real.Plaster);
        Vector3[] frame = piece(0f, doorWidth + 0.3f, doorHeight + 0.1f, 0.2f);
        CreateRealBlock(name + " marco", parent, frame[0], frame[1] + (alongZ ? new Vector3(0.1f, 0f, 0f) : new Vector3(0f, 0f, 0.1f)), real.DarkPlanks, false);
    }
}
