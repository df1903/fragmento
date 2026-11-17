using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// QUÉ HACE
///   Texturas y piezas de los niveles 8 (Ciudad Dormida) y 9 (El Hogar): asfalto, aceras, fachadas con ventanas,
///   ladrillo, papel pintado, alfombra y colcha; edificios con azotea, coches, autobús, reflectores (torre y dron),
///   tirolinas, y los puntos del camino para las pruebas en niveles con varias alturas.
///
/// POR QUÉ EXISTE
///   Igual que Realismo.cs y MundoReal.cs para los niveles 6 y 7: todo se pinta y se modela por código, sin
///   assets ajenos. Aquí va lo que es propio de la ciudad y de la casa, para no hacer enormes los otros archivos.
///
/// CÓMO SE USA
///   Lo llaman FragmentoSceneBuilder.Nivel08.cs y Nivel09.cs.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    // ------------------------------------------------------------------ Texturas

    // Asfalto: gris oscuro con grava clara y grietas finas.
    private static Texel PaintAsphalt(float u, float v)
    {
        float patches = TileNoise(u, v, 4f, 111f);
        float gravel = Mathf.Clamp01((TileNoise(u, v, 40f, 113f) - 0.55f) * 5f);
        float crack = Mathf.Clamp01(1f - Mathf.Abs(TileNoise(u, v, 2.5f, 117f) - 0.5f) * 120f);
        Color color = Color.Lerp(new Color(0.08f, 0.08f, 0.09f), new Color(0.17f, 0.17f, 0.18f), patches * 0.6f + gravel * 0.4f);
        color = Color.Lerp(color, new Color(0.03f, 0.03f, 0.03f), crack * 0.8f);
        return T(color, patches * 0.3f + gravel * 0.5f - crack * 0.4f + 0.4f);
    }

    // Acera: losas cuadradas de hormigón con juntas.
    private static Texel PaintSlabs(float u, float v)
    {
        const float cells = 2f;
        float inX = u * cells - Mathf.Floor(u * cells), inY = v * cells - Mathf.Floor(v * cells);
        float joint = Mathf.Clamp01(Mathf.Min(Mathf.Min(inX, 1f - inX), Mathf.Min(inY, 1f - inY)) * 30f);
        float tone = Hash(Mathf.FloorToInt(u * cells), Mathf.FloorToInt(v * cells) + 9);
        float grain = TileNoise(u, v, 25f, 119f);
        Color color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), new Color(0.48f, 0.47f, 0.45f), grain * 0.6f + tone * 0.4f) * (0.55f + 0.45f * joint);
        color.a = 1f;
        return T(color, joint * (0.7f + grain * 0.3f));
    }

    // Fachada: UN módulo (3 m x 3.5 m) de hormigón con una ventana. Repetido, da la rejilla de ventanas del edificio.
    private static Texel PaintFacade(float u, float v)
    {
        float grain = TileNoise(u, v, 12f, 123f);
        float stain = Mathf.Clamp01(TileNoise(u, v, 2f, 127f) * 1.4f - 0.3f) * Mathf.Clamp01(1f - v * 1.2f); // churretes bajo la ventana
        Color wall = Color.Lerp(new Color(0.33f, 0.32f, 0.3f), new Color(0.52f, 0.5f, 0.46f), grain);
        wall = Color.Lerp(wall, new Color(0.2f, 0.19f, 0.18f), stain * 0.5f);

        bool inWindow = u > 0.2f && u < 0.8f && v > 0.3f && v < 0.85f;
        bool inFrame = u > 0.17f && u < 0.83f && v > 0.27f && v < 0.88f;
        bool sill = u > 0.15f && u < 0.85f && v > 0.24f && v < 0.28f;
        if (inWindow)
        {
            // Cristal oscuro con un reflejo diagonal del cielo y la cruz del marco.
            float reflection = Mathf.Clamp01(1f - Mathf.Abs((u - 0.2f) - (v - 0.3f) * 0.8f - 0.18f) * 6f) * 0.35f;
            bool mullion = Mathf.Abs(u - 0.5f) < 0.012f || Mathf.Abs(v - 0.6f) < 0.012f;
            Color glass = Color.Lerp(new Color(0.03f, 0.04f, 0.06f), new Color(0.22f, 0.28f, 0.36f), reflection + (v - 0.3f) * 0.3f);
            return T(mullion ? new Color(0.15f, 0.15f, 0.15f) : glass, mullion ? 0.5f : 0.1f);
        }

        if (inFrame || sill)
        {
            return T(new Color(0.2f, 0.2f, 0.21f), sill ? 0.9f : 0.65f);
        }

        return T(wall, 0.6f + grain * 0.2f);
    }

    // Ladrillo visto: hiladas rojizas desplazadas con mortero claro.
    private static Texel PaintBrick(float u, float v)
    {
        const float rows = 12f, columns = 4f;
        int row = Mathf.FloorToInt(v * rows);
        float shifted = u * columns + (row % 2) * 0.5f;
        float inX = shifted - Mathf.Floor(shifted), inY = v * rows - row;
        float mortar = Mathf.Clamp01(Mathf.Min(Mathf.Min(inX, 1f - inX) * 3.5f, Mathf.Min(inY, 1f - inY)) * 10f);
        float tone = Hash(row, Mathf.FloorToInt(shifted) + 31);
        float rough = TileNoise(u, v, 30f, 131f);
        Color brick = Color.Lerp(new Color(0.3f, 0.12f, 0.08f), new Color(0.55f, 0.27f, 0.17f), tone * 0.6f + rough * 0.4f);
        return T(Color.Lerp(new Color(0.45f, 0.43f, 0.4f), brick, mortar), mortar * (0.8f + rough * 0.2f));
    }

    // Papel pintado: rayas suaves y florecitas, en tonos cálidos.
    private static Texel PaintWallpaper(float u, float v)
    {
        float stripes = Mathf.Sin(u * 8f * Mathf.PI * 2f) * 0.5f + 0.5f;
        Vector2 cell = new Vector2(u * 8f - Mathf.Floor(u * 8f) - 0.5f, v * 6f - Mathf.Floor(v * 6f) - 0.5f);
        float flower = Mathf.Clamp01(1f - cell.magnitude * 7f);
        float age = TileNoise(u, v, 3f, 137f);
        Color color = Color.Lerp(new Color(0.78f, 0.68f, 0.52f), new Color(0.86f, 0.77f, 0.6f), stripes * 0.5f + age * 0.3f);
        color = Color.Lerp(color, new Color(0.62f, 0.36f, 0.34f), flower * 0.8f);
        return T(color, 0.5f + flower * 0.2f);
    }

    // Alfombra (una sola pieza, sin repetir): cenefa roja, campo azul y rombos concéntricos.
    private static Texel PaintRug(float u, float v)
    {
        float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        float diamond = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f);
        float rings = Mathf.Sin(diamond * 40f) * 0.5f + 0.5f;
        float fibre = TileNoise(u, v, 60f, 139f);
        Color color = edge < 0.07f ? new Color(0.45f, 0.1f, 0.08f) : edge < 0.09f ? new Color(0.8f, 0.65f, 0.35f) : Color.Lerp(new Color(0.08f, 0.13f, 0.28f), new Color(0.2f, 0.3f, 0.5f), rings);
        if (diamond < 0.18f)
        {
            color = Color.Lerp(new Color(0.75f, 0.55f, 0.25f), new Color(0.5f, 0.15f, 0.1f), rings);
        }

        return T(color * (0.85f + fibre * 0.25f), fibre);
    }

    // Colcha de retazos: cuadros de colores con costuras.
    private static Texel PaintQuilt(float u, float v)
    {
        const float cells = 6f;
        int i = Mathf.FloorToInt(u * cells), j = Mathf.FloorToInt(v * cells);
        float inX = u * cells - i, inY = v * cells - j;
        float seam = Mathf.Clamp01(Mathf.Min(Mathf.Min(inX, 1f - inX), Mathf.Min(inY, 1f - inY)) * 25f);
        Color[] palette = { new Color(0.62f, 0.2f, 0.18f), new Color(0.85f, 0.75f, 0.55f), new Color(0.25f, 0.4f, 0.55f), new Color(0.4f, 0.5f, 0.3f), new Color(0.75f, 0.5f, 0.25f) };
        Color patch = palette[Mathf.FloorToInt(Hash(i, j + 41) * palette.Length) % palette.Length];
        float weave = TileNoise(u, v, 50f, 149f);
        return T(patch * (0.75f + 0.25f * seam) * (0.9f + weave * 0.15f), seam * 0.6f + weave * 0.4f);
    }

    // ------------------------------------------------------------------ Ciudad: materiales

    private class CityMaterials
    {
        public Material Asphalt, Sidewalk, Facade, FacadeWarm, Brick, Roof, Steel, CraneYellow, CarGlass, Tire, Neon, WindowLit, Beam, Line, Rubble;
        public Material[] CarPaints;
    }

    private static CityMaterials CreateCityMaterials()
    {
        SurfaceTextures asphalt = MakeSurface("Asfalto", PaintAsphalt, false, 4f);
        SurfaceTextures slabs = MakeSurface("Losas", PaintSlabs, false, 5f);
        SurfaceTextures facade = MakeSurface("Fachada", PaintFacade, false, 6f);
        SurfaceTextures brick = MakeSurface("Ladrillo", PaintBrick, false, 6f);

        CityMaterials city = new CityMaterials
        {
            Asphalt = GetOrCreateTexturedMaterial("CiuAsfalto", asphalt, Vector2.one * 0.15f, Color.white, 0.35f),
            Sidewalk = GetOrCreateTexturedMaterial("CiuAcera", slabs, Vector2.one * 0.25f, Color.white, 0.2f),
            Facade = GetOrCreateTexturedMaterial("CiuFachada", facade, new Vector2(1f / 3f, 1f / 3.5f), Color.white, 0.25f),
            FacadeWarm = GetOrCreateTexturedMaterial("CiuFachadaOcre", facade, new Vector2(1f / 3f, 1f / 3.5f), new Color(1.05f, 0.85f, 0.65f), 0.25f),
            Brick = GetOrCreateTexturedMaterial("CiuLadrillo", brick, new Vector2(1f / 3f, 1f / 3.5f), Color.white, 0.15f),
            Roof = GetOrCreateTexturedMaterial("CiuAzotea", slabs, Vector2.one * 0.3f, new Color(0.75f, 0.73f, 0.7f), 0.15f),
            Rubble = GetOrCreateTexturedMaterial("CiuEscombro", slabs, Vector2.one * 0.6f, new Color(0.6f, 0.58f, 0.55f), 0.1f),
            Steel = GetOrCreateMaterial("CiuAcero", new Color(0.18f, 0.19f, 0.2f), null, 0f, 0.55f),
            CraneYellow = GetOrCreateMaterial("CiuGrua", new Color(0.75f, 0.55f, 0.08f), null, 0f, 0.45f),
            CarGlass = GetOrCreateMaterial("CiuCristalCoche", new Color(0.04f, 0.05f, 0.07f), null, 0f, 0.92f),
            Tire = GetOrCreateMaterial("CiuNeumatico", new Color(0.04f, 0.04f, 0.04f), null, 0f, 0.2f),
            Neon = GetOrCreateMaterial("CiuNeon", new Color(0.1f, 0.6f, 0.3f), new Color(0.2f, 1f, 0.45f), 3.5f, 0.5f),
            WindowLit = GetOrCreateMaterial("CiuVentanaEncendida", new Color(0.9f, 0.7f, 0.4f), new Color(1f, 0.7f, 0.35f), 1.6f, 0.4f),
            Line = GetOrCreateMaterial("CiuPintura", new Color(0.75f, 0.72f, 0.6f), null, 0f, 0.3f),
            CarPaints = new[]
            {
                GetOrCreateMaterial("CiuCocheRojo", new Color(0.35f, 0.07f, 0.06f), null, 0f, 0.7f),
                GetOrCreateMaterial("CiuCocheAzul", new Color(0.08f, 0.14f, 0.26f), null, 0f, 0.7f),
                GetOrCreateMaterial("CiuCocheBlanco", new Color(0.55f, 0.55f, 0.52f), null, 0f, 0.7f),
                GetOrCreateMaterial("CiuCocheVerde", new Color(0.12f, 0.22f, 0.16f), null, 0f, 0.7f),
            },
        };

        city.Beam = GetOrCreateTransparentMaterial("CiuHazReflector", "Universal Render Pipeline/Lit", new Color(1f, 0.95f, 0.8f, 0.07f), new Color(1f, 0.92f, 0.75f), 0.35f, true);
        return city;
    }

    // ------------------------------------------------------------------ Ciudad: piezas

    // Edificio: bloque macizo con fachada, losa de azotea y pretiles. footprint = (xMin, zMin, xMax, zMax).
    // parapets: lados con pretil (1 sur, 2 norte, 4 este, 8 oeste). Las ventanas encendidas son piezas aparte.
    private static GameObject CreateCityBuilding(Transform parent, string name, Rect footprint, float height, Material facade, CityMaterials city, int parapets, System.Random rng)
    {
        GameObject building = new GameObject(name);
        building.transform.SetParent(parent, false);
        Vector3 center = new Vector3(footprint.center.x, 0f, footprint.center.y);
        building.transform.position = center;
        float width = footprint.width, depth = footprint.height;

        CreateRealBlock("Cuerpo", building.transform, center + Vector3.up * height / 2f, new Vector3(width, height, depth), facade);
        CreateRealBlock("Losa de azotea", building.transform, center + Vector3.up * (height + 0.03f), new Vector3(width - 0.1f, 0.08f, depth - 0.1f), city.Roof, false);
        CreateRealBlock("Cornisa", building.transform, center + Vector3.up * (height - 0.25f), new Vector3(width + 0.3f, 0.3f, depth + 0.3f), city.Steel, false);

        if ((parapets & 1) != 0) CreateRealBlock("Pretil sur", building.transform, center + new Vector3(0f, height + 0.25f, -depth / 2f + 0.15f), new Vector3(width, 0.5f, 0.3f), facade);
        if ((parapets & 2) != 0) CreateRealBlock("Pretil norte", building.transform, center + new Vector3(0f, height + 0.25f, depth / 2f - 0.15f), new Vector3(width, 0.5f, 0.3f), facade);
        if ((parapets & 4) != 0) CreateRealBlock("Pretil este", building.transform, center + new Vector3(width / 2f - 0.15f, height + 0.25f, 0f), new Vector3(0.3f, 0.5f, depth), facade);
        if ((parapets & 8) != 0) CreateRealBlock("Pretil oeste", building.transform, center + new Vector3(-width / 2f + 0.15f, height + 0.25f, 0f), new Vector3(0.3f, 0.5f, depth), facade);

        // Ventanas encendidas: algunas, al azar, en las cuatro fachadas (cada módulo de 3 x 3.5 m tiene su ventana).
        for (int side = 0; side < 4; side++)
        {
            bool alongX = side < 2;
            float facadeLength = alongX ? width : depth;
            float sign = side % 2 == 0 ? -1f : 1f;
            for (float along = -facadeLength / 2f + 1.5f; along < facadeLength / 2f - 1f; along += 3f)
            {
                for (float floorY = 1.98f; floorY < height - 1f; floorY += 3.5f)
                {
                    if (rng.NextDouble() > 0.09)
                    {
                        continue;
                    }

                    Vector3 offset = alongX ? new Vector3(along, floorY, sign * (depth / 2f + 0.03f)) : new Vector3(sign * (width / 2f + 0.03f), floorY, along);
                    Vector3 size = alongX ? new Vector3(1.75f, 1.9f, 0.04f) : new Vector3(0.04f, 1.9f, 1.75f);
                    NoShadow(CreateVisualPart("Ventana encendida", building.transform, PrimitiveType.Cube, offset, size, Vector3.zero, city.WindowLit));
                }
            }
        }

        MakeStatic(building);
        return building;
    }

    // Coche abandonado (sólido: sirve de cobertura frente a los reflectores). yaw en grados.
    private static GameObject CreateCar(Transform parent, string name, Vector3 ground, float yaw, Material paint, CityMaterials city)
    {
        GameObject car = new GameObject(name);
        car.transform.SetParent(parent, false);
        car.transform.position = ground;
        car.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateVisualPart("Carroceria", car.transform, PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(1.85f, 0.7f, 4.3f), Vector3.zero, paint);
        CreateVisualPart("Cabina", car.transform, PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.25f), new Vector3(1.6f, 0.6f, 2.2f), Vector3.zero, city.CarGlass);
        CreateVisualPart("Techo", car.transform, PrimitiveType.Cube, new Vector3(0f, 1.57f, -0.25f), new Vector3(1.62f, 0.06f, 2f), Vector3.zero, paint);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.85f : 0.85f, z = i < 2 ? 1.35f : -1.35f;
            CreateVisualPart("Rueda", car.transform, PrimitiveType.Cylinder, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f), new Vector3(0f, 0f, 90f), city.Tire);
        }

        BoxCollider box = car.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.8f, 0f);
        box.size = new Vector3(1.85f, 1.6f, 4.3f);
        return car;
    }

    // Autobús abandonado: gran cobertura en mitad de la avenida.
    private static GameObject CreateBus(Transform parent, string name, Vector3 ground, float yaw, CityMaterials city)
    {
        GameObject bus = new GameObject(name);
        bus.transform.SetParent(parent, false);
        bus.transform.position = ground;
        bus.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Material paint = GetOrCreateMaterial("CiuAutobus", new Color(0.55f, 0.42f, 0.12f), null, 0f, 0.6f);
        CreateVisualPart("Caja", bus.transform, PrimitiveType.Cube, new Vector3(0f, 1.8f, 0f), new Vector3(2.6f, 2.8f, 11f), Vector3.zero, paint);
        CreateVisualPart("Ventanas izquierda", bus.transform, PrimitiveType.Cube, new Vector3(-1.31f, 2.3f, 0f), new Vector3(0.02f, 1f, 9.6f), Vector3.zero, city.CarGlass);
        CreateVisualPart("Ventanas derecha", bus.transform, PrimitiveType.Cube, new Vector3(1.31f, 2.3f, 0f), new Vector3(0.02f, 1f, 9.6f), Vector3.zero, city.CarGlass);
        CreateVisualPart("Parabrisas", bus.transform, PrimitiveType.Cube, new Vector3(0f, 2.2f, 5.51f), new Vector3(2.3f, 1.4f, 0.02f), Vector3.zero, city.CarGlass);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -1.2f : 1.2f, z = i < 2 ? 3.6f : -3.6f;
            CreateVisualPart("Rueda", bus.transform, PrimitiveType.Cylinder, new Vector3(x, 0.5f, z), new Vector3(1f, 0.15f, 1f), new Vector3(0f, 0f, 90f), city.Tire);
        }

        BoxCollider box = bus.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.6f, 0f);
        box.size = new Vector3(2.6f, 3.2f, 11f);
        return bus;
    }

    // Cono abierto (para el haz del reflector): vértice en el origen, abierto hacia +Z.
    private static Mesh BuildConeMesh(float length, float radius)
    {
        const int sides = 24;
        MeshBuilder builder = new MeshBuilder();
        int apex = builder.Add(Vector3.zero, Vector3.back, Vector2.zero);
        for (int s = 0; s <= sides; s++)
        {
            float angle = s / (float)sides * Mathf.PI * 2f;
            Vector3 rim = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, length);
            builder.Add(rim, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f), new Vector2(s / (float)sides, 1f));
        }

        for (int s = 0; s < sides; s++)
        {
            builder.Triangles.AddRange(new[] { apex, apex + s + 1, apex + s + 2 });
        }

        return builder.ToMesh(false);
    }

    // Cabeza de reflector: carcasa, cristal, luz de foco y haz visible. Devuelve el componente ya configurado.
    private static Searchlight AddSearchlight(GameObject root, Transform head, float sweepDegrees, float sweepSpeed, Vector3[] route, float halfAngle, float range, CityMaterials city)
    {
        CreateVisualPart("Carcasa", head, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.3f), new Vector3(0.9f, 0.4f, 0.9f), new Vector3(90f, 0f, 0f), city.Steel);
        NoShadow(CreateVisualPart("Cristal", head, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.12f), new Vector3(0.75f, 0.02f, 0.75f), new Vector3(90f, 0f, 0f), city.WindowLit));

        GameObject lightObject = new GameObject("Foco");
        lightObject.transform.SetParent(head, false);
        Light spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.93f, 0.78f);
        spot.spotAngle = halfAngle * 2f + 4f;
        spot.innerSpotAngle = halfAngle * 1.4f;
        spot.range = range * 1.15f;
        spot.intensity = 40f;
        spot.shadows = LightShadows.Hard; // la sombra del haz deja ver dónde está la cobertura

        float beamLength = range * 0.9f;
        GameObject beam = CreateMeshObject("Haz", head, BuildConeMesh(beamLength, Mathf.Tan(halfAngle * Mathf.Deg2Rad) * beamLength), head.position, false, city.Beam);
        beam.transform.localPosition = Vector3.zero;
        beam.transform.localRotation = Quaternion.identity;
        Renderer beamRenderer = beam.GetComponent<Renderer>();
        beamRenderer.shadowCastingMode = ShadowCastingMode.Off;

        Searchlight searchlight = root.AddComponent<Searchlight>();
        searchlight.Setup(head, spot, beamRenderer, sweepDegrees, sweepSpeed, route);
        SerializedObject serialized = new SerializedObject(searchlight);
        Property(serialized, "halfAngle").floatValue = halfAngle;
        Property(serialized, "range").floatValue = range;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return searchlight;
    }

    // Torre de vigilancia con reflector que barre. Apunta en reposo hacia "lookAt".
    private static Searchlight CreateSearchlightTower(Transform parent, string name, Vector3 ground, float height, Vector3 lookAt, float sweepDegrees, float sweepSpeed, CityMaterials city)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = ground;

        CreateRealBlock("Mastil", root.transform, ground + Vector3.up * height / 2f, new Vector3(0.5f, height, 0.5f), city.Steel);
        CreateRealBlock("Plataforma", root.transform, ground + Vector3.up * (height - 0.1f), new Vector3(1.8f, 0.2f, 1.8f), city.Steel, false);
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = new Vector3(i % 2 == 0 ? -0.85f : 0.85f, height + 0.45f, i < 2 ? -0.85f : 0.85f);
            CreateRealBlock("Baranda", root.transform, ground + corner, new Vector3(0.06f, 0.9f, 0.06f), city.Steel, false);
        }

        Transform head = CreatePivot("Cabeza", root.transform, Vector3.up * (height + 0.9f));
        head.rotation = Quaternion.LookRotation(lookAt - head.position, Vector3.up);
        return AddSearchlight(root, head, sweepDegrees, sweepSpeed, new Vector3[0], 12f, 36f, city);
    }

    // Dron vigía: sigue la ruta (puntos en el mundo) con el foco mirando hacia abajo.
    private static Searchlight CreateSearchDrone(Transform parent, string name, Vector3[] route, CityMaterials city)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = route[0];

        CreateVisualPart("Cuerpo", root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(1.1f, 0.45f, 1.1f), Vector3.zero, city.Steel);
        for (int i = 0; i < 4; i++)
        {
            Vector3 arm = new Vector3(i % 2 == 0 ? -0.75f : 0.75f, 0.1f, i < 2 ? -0.75f : 0.75f);
            CreateVisualPart("Brazo", root.transform, PrimitiveType.Cube, arm * 0.5f, new Vector3(0.08f, 0.05f, 1.1f), new Vector3(0f, i % 3 == 0 ? 45f : -45f, 0f), city.Steel);
            Transform rotor = CreatePivot("Rotor", root.transform, arm + Vector3.up * 0.1f);
            NoShadow(CreateVisualPart("Aspa", rotor, PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 0.02f, 0.08f), Vector3.zero, city.Steel));
            rotor.gameObject.AddComponent<Spinner>();
            SerializedObject spin = new SerializedObject(rotor.GetComponent<Spinner>());
            Property(spin, "axis").vector3Value = Vector3.up;
            Property(spin, "degreesPerSecond").floatValue = 1400f;
            spin.ApplyModifiedPropertiesWithoutUndo();
        }

        NoShadow(CreateVisualPart("Piloto", root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0.5f), Vector3.one * 0.12f, Vector3.zero, GetOrCreateMaterial("CiuPilotoRojo", new Color(1f, 0.1f, 0.05f), new Color(1f, 0.1f, 0.05f), 4f, 0.4f)));

        Transform head = CreatePivot("Cabeza", root.transform, Vector3.down * 0.3f);
        head.localRotation = Quaternion.Euler(90f, 0f, 0f); // mira hacia abajo
        return AddSearchlight(root, head, 0f, 0f, route, 19f, 26f, city);
    }

    // Tirolina: postes en los dos extremos, cable y polea. start/end son los puntos del cable.
    // La raíz queda en "start", así ActivateOnEvent hace crecer el cable desde ahí.
    private static GameObject CreateZipLine(string name, Vector3 start, Vector3 end, CityMaterials city)
    {
        GameObject root = new GameObject(name);
        root.transform.position = start;

        Transform startAnchor = CreatePivot("Anclaje inicio", root.transform, Vector3.zero);
        Transform endAnchor = CreatePivot("Anclaje final", root.transform, end - start);
        CreateRealBlock("Poste inicio", root.transform, start + Vector3.down * 1.4f + Vector3.up * 0.1f, new Vector3(0.25f, 3f, 0.25f), city.Steel, false);
        CreateRealBlock("Poste final", root.transform, end + Vector3.down * 1.4f + Vector3.up * 0.1f, new Vector3(0.25f, 3f, 0.25f), city.Steel, false);

        GameObject cable = new GameObject("Cable");
        cable.transform.SetParent(root.transform, false);
        LineRenderer line = cable.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, end - start);
        line.widthMultiplier = 0.06f;
        line.sharedMaterial = city.Steel;
        line.shadowCastingMode = ShadowCastingMode.Off;

        Transform pulley = CreatePivot("Polea", root.transform, Vector3.zero);
        CreateVisualPart("Rueda", pulley, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.35f, 0.06f, 0.35f), new Vector3(0f, 0f, 90f), city.CraneYellow);
        CreateVisualPart("Asa", pulley, PrimitiveType.Cube, Vector3.down * 0.5f, new Vector3(0.6f, 0.06f, 0.06f), Vector3.zero, city.CraneYellow);
        CreateVisualPart("Barra", pulley, PrimitiveType.Cube, Vector3.down * 0.25f, new Vector3(0.05f, 0.5f, 0.05f), Vector3.zero, city.Steel);

        root.AddComponent<ZipLine>().Setup(startAnchor, endAnchor, pulley);
        return root;
    }

    // ------------------------------------------------------------------ Camino con varias alturas

    // Como CreatePathMarkers, pero cada tramo trae su altura en los propios puntos (azoteas, muebles).
    private static void CreateFlatPathMarkers(Transform level, Vector3[][] stretches)
    {
        Transform root = new GameObject("Camino").transform;
        root.SetParent(level, false);
        for (int s = 0; s < stretches.Length; s++)
        {
            Transform stretch = new GameObject("Tramo " + (s + 1).ToString("00")).transform;
            stretch.SetParent(root, false);
            Vector3[] points = stretches[s];
            for (int i = 0; i < points.Length - 1; i++)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(points[i], points[i + 1]) / 3f));
                for (int k = 0; k < steps + (i == points.Length - 2 ? 1 : 0); k++)
                {
                    Transform marker = new GameObject("Punto").transform;
                    marker.SetParent(stretch, false);
                    marker.position = Vector3.Lerp(points[i], points[i + 1], k / (float)steps);
                }
            }
        }
    }
}
