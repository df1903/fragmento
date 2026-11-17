using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// QUÉ HACE
///   Base gráfica de los niveles 6 y 7: genera POR CÓDIGO texturas (hierba, tierra, corteza, madera, piedra,
///   tejas...) con su mapa de relieve, mallas (terreno con colinas, troncos, copas, rocas, tejados, matas de
///   hierba), el cielo y el perfil de post-proceso.
///
/// POR QUÉ EXISTE
///   Los niveles 1 a 5 usan cubos de un solo color. Para que el 6 y el 7 se vean como un mundo real sin traer
///   assets ajenos (licencias, CREDITS.md), todo se "pinta" y se "modela" con matemáticas sencillas: ruido de
///   Perlin para las manchas, senos para las vetas y fórmulas de círculo para troncos y copas.
///
/// CÓMO SE USA
///   Lo llaman los constructores de los niveles 6 y 7. Las texturas se guardan en Assets/Textures y las mallas
///   grandes en Assets/Meshes: se pueden abrir y mirar en el Editor. Regenerar un nivel las vuelve a crear.
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string TexturesFolder = "Assets/Textures";
    private const string MeshesFolder = "Assets/Meshes";
    private const int TextureSize = 256;

    // ------------------------------------------------------------------ Ruido

    // Ruido de Perlin en varias capas ("fractal"): manchas grandes con detalle pequeño encima. Devuelve 0..1.
    private static float Fbm(float x, float y, int octaves = 4)
    {
        float sum = 0f, amplitude = 0.5f, total = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x + 37.2f * i, y + 91.7f * i) * amplitude;
            total += amplitude;
            x *= 2f;
            y *= 2f;
            amplitude *= 0.5f;
        }

        return sum / total;
    }

    // Ruido que se repite sin costura: mezcla cuatro copias desplazadas para que el borde derecho case con el izquierdo.
    private static float TileNoise(float u, float v, float frequency, float seed)
    {
        float x = u * frequency, y = v * frequency;
        float a = Fbm(x + seed, y + seed);
        float b = Fbm(x - frequency + seed, y + seed);
        float c = Fbm(x + seed, y - frequency + seed);
        float d = Fbm(x - frequency + seed, y - frequency + seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    // Número "al azar" fijo para una casilla (i, j): el mismo cada vez. Sirve para dar un tono propio a cada piedra o teja.
    private static float Hash(int i, int j)
    {
        float value = Mathf.Sin(i * 127.1f + j * 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    // ------------------------------------------------------------------ Texturas

    private struct Texel
    {
        public Color Color;   // color (y transparencia en alfa)
        public float Height;  // relieve 0..1: de aquí sale el mapa de normales
    }

    private struct SurfaceTextures
    {
        public Texture2D Albedo;
        public Texture2D Normal;
    }

    // Pinta una textura píxel a píxel con "paint(u, v)" y la guarda como PNG, junto con su mapa de normales.
    private static SurfaceTextures MakeSurface(string name, System.Func<float, float, Texel> paint, bool transparent, float bumpStrength)
    {
        EnsureFolder(TexturesFolder);
        int size = TextureSize;
        Color[] colors = new Color[size * size];
        float[] heights = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Texel texel = paint((x + 0.5f) / size, (y + 0.5f) / size);
                colors[y * size + x] = texel.Color;
                heights[y * size + x] = texel.Height;
            }
        }

        // Mapa de normales: la pendiente del relieve entre píxeles vecinos (con vuelta, para que repita sin costura).
        Color[] normals = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float left = heights[y * size + (x + size - 1) % size];
                float right = heights[y * size + (x + 1) % size];
                float down = heights[((y + size - 1) % size) * size + x];
                float up = heights[((y + 1) % size) * size + x];
                Vector3 normal = new Vector3((left - right) * bumpStrength, (down - up) * bumpStrength, 1f).normalized;
                normals[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            }
        }

        return new SurfaceTextures
        {
            Albedo = SaveTexture(name, colors, size, transparent, false),
            Normal = SaveTexture(name + "_N", normals, size, false, true),
        };
    }

    private static Texture2D SaveTexture(string name, Color[] pixels, int size, bool transparent, bool isNormalMap)
    {
        string path = TexturesFolder + "/" + name + ".png";
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.alphaIsTransparency = transparent;
        importer.sRGBTexture = !isNormalMap;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texel T(Color color, float height)
    {
        return new Texel { Color = color, Height = height };
    }

    // Hierba vista desde arriba: manchas de verdes y un rayado fino que imita las briznas.
    private static Texel PaintGrass(float u, float v)
    {
        float patches = TileNoise(u, v, 4f, 3f);
        float blades = TileNoise(u * 6f % 1f, v, 9f, 11f);
        Color color = Color.Lerp(new Color(0.13f, 0.24f, 0.08f), new Color(0.32f, 0.44f, 0.16f), patches * 0.7f + blades * 0.3f);
        color = Color.Lerp(color, new Color(0.42f, 0.4f, 0.2f), Mathf.Clamp01((TileNoise(u, v, 2f, 40f) - 0.62f) * 4f)); // calvas secas
        return T(color, blades * 0.7f + patches * 0.3f);
    }

    // Tierra del sendero: marrones con piedrecitas claras.
    private static Texel PaintDirt(float u, float v)
    {
        float patches = TileNoise(u, v, 5f, 7f);
        float pebbles = Mathf.Clamp01((TileNoise(u, v, 26f, 19f) - 0.6f) * 6f);
        Color color = Color.Lerp(new Color(0.24f, 0.17f, 0.11f), new Color(0.42f, 0.33f, 0.22f), patches);
        color = Color.Lerp(color, new Color(0.55f, 0.52f, 0.47f), pebbles);
        return T(color, patches * 0.4f + pebbles * 0.6f);
    }

    // Corteza: surcos verticales oscuros que serpentean.
    private static Texel PaintBark(float u, float v)
    {
        float wobble = TileNoise(u, v, 3f, 5f);
        float ridges = Mathf.Abs(Mathf.Sin((u * 14f + wobble * 2.5f) * Mathf.PI));
        float grain = TileNoise(u, v * 0.3f, 22f, 2f);
        float height = Mathf.Pow(ridges, 0.6f) * 0.8f + grain * 0.2f;
        Color color = Color.Lerp(new Color(0.09f, 0.07f, 0.05f), new Color(0.36f, 0.28f, 0.2f), height);
        return T(color, height);
    }

    // Follaje: hojitas claras sobre huecos oscuros.
    private static Texel PaintLeaves(float u, float v)
    {
        float clumps = TileNoise(u, v, 7f, 13f);
        float leaves = TileNoise(u, v, 24f, 29f);
        float height = Mathf.Clamp01((leaves - 0.35f) * 2.5f) * (0.5f + clumps * 0.5f);
        Color color = Color.Lerp(new Color(0.03f, 0.08f, 0.03f), new Color(0.3f, 0.48f, 0.16f), height);
        return T(color, height);
    }

    // Mata de hierba de perfil (con transparencia): varias briznas que se estrechan hacia la punta.
    private static Texel PaintGrassBlades(float u, float v)
    {
        float alpha = 0f;
        for (int i = 0; i < 7; i++)
        {
            float center = (i + 0.5f) / 7f + Mathf.Sin(v * 3f + i * 1.7f) * 0.05f * v; // la brizna se curva al subir
            float tallness = 0.55f + Hash(i, 3) * 0.45f;
            float width = 0.055f * Mathf.Clamp01(1f - v / tallness);
            if (v < tallness && Mathf.Abs(u - center) < width)
            {
                alpha = 1f;
            }
        }

        Color color = Color.Lerp(new Color(0.1f, 0.2f, 0.06f), new Color(0.45f, 0.58f, 0.22f), v);
        color.a = alpha;
        return T(color, 0.5f);
    }

    // Tablones horizontales con veta, juntas oscuras y clavos.
    private static Texel PaintPlanks(float u, float v)
    {
        const float rows = 5f;
        int row = Mathf.FloorToInt(v * rows);
        float inRow = v * rows - row;
        float tint = Hash(row, 7);
        float grain = TileNoise(u * 0.25f + tint, v * rows % 1f, 18f, 17f + row);
        float gap = Mathf.Min(inRow, 1f - inRow);
        float joint = Mathf.Clamp01(gap * 14f);
        Color color = Color.Lerp(new Color(0.22f, 0.15f, 0.09f), new Color(0.46f, 0.34f, 0.2f), grain * 0.7f + tint * 0.3f) * (0.35f + 0.65f * joint);
        color.a = 1f;
        return T(color, joint * (0.7f + grain * 0.3f));
    }

    // Yeso viejo: blanco roto con manchas de humedad y grietas.
    private static Texel PaintPlaster(float u, float v)
    {
        float stains = TileNoise(u, v, 3f, 23f);
        float fine = TileNoise(u, v, 30f, 31f);
        float crack = Mathf.Clamp01(1f - Mathf.Abs(TileNoise(u, v, 3f, 47f) - 0.5f) * 160f); // grietas finas y escasas
        Color color = Color.Lerp(new Color(0.5f, 0.47f, 0.4f), new Color(0.8f, 0.76f, 0.66f), stains * 0.6f + fine * 0.4f);
        color = Color.Lerp(color, new Color(0.3f, 0.27f, 0.22f), crack * 0.5f);
        return T(color, fine * 0.4f + 0.5f - crack * 0.3f);
    }

    // Muro de piedra: hiladas de sillares desplazadas, cada piedra con su tono, y mortero oscuro entre ellas.
    private static Texel PaintStoneWall(float u, float v)
    {
        const float rows = 6f, columns = 3f;
        int row = Mathf.FloorToInt(v * rows);
        float shifted = u * columns + (row % 2) * 0.5f;
        int column = Mathf.FloorToInt(shifted) % (int)columns;
        float inX = shifted - Mathf.Floor(shifted);
        float inY = v * rows - row;
        float edge = Mathf.Min(Mathf.Min(inX, 1f - inX) * 3f, Mathf.Min(inY, 1f - inY));
        float mortar = Mathf.Clamp01(edge * 9f);
        float tone = Hash(row, column);
        float rough = TileNoise(u, v, 20f, 53f);
        Color stone = Color.Lerp(new Color(0.3f, 0.3f, 0.31f), new Color(0.56f, 0.54f, 0.5f), tone * 0.6f + rough * 0.4f);
        Color color = Color.Lerp(new Color(0.14f, 0.13f, 0.12f), stone, mortar);
        return T(color, mortar * (0.75f + rough * 0.25f));
    }

    // Adoquines: como el muro pero más redondeados y más oscuros (calle mojada).
    private static Texel PaintCobble(float u, float v)
    {
        Texel wall = PaintStoneWall(u, v);
        wall.Color = Color.Lerp(wall.Color, new Color(0.2f, 0.2f, 0.23f), 0.45f);
        return wall;
    }

    // Tejas: filas de tejas curvas que se solapan.
    private static Texel PaintRoofTiles(float u, float v)
    {
        const float rows = 8f, columns = 6f;
        int row = Mathf.FloorToInt(v * rows);
        float shifted = u * columns + (row % 2) * 0.5f;
        float inX = shifted - Mathf.Floor(shifted);
        float inY = v * rows - row;
        float curve = Mathf.Sin(inX * Mathf.PI);           // lomo de la teja
        float overlap = 1f - inY;                          // la de arriba tapa a la de abajo
        float height = curve * 0.6f + overlap * 0.4f;
        float tone = Hash(row, Mathf.FloorToInt(shifted));
        Color color = Color.Lerp(new Color(0.2f, 0.09f, 0.07f), new Color(0.55f, 0.28f, 0.18f), height * 0.6f + tone * 0.4f);
        return T(color, height);
    }

    // Roca: grises con vetas.
    private static Texel PaintRock(float u, float v)
    {
        float big = TileNoise(u, v, 3f, 61f);
        float fine = TileNoise(u, v, 18f, 67f);
        float strata = Mathf.Abs(Mathf.Sin((v * 6f + big * 2f) * Mathf.PI));
        float height = big * 0.5f + fine * 0.3f + strata * 0.2f;
        Color color = Color.Lerp(new Color(0.16f, 0.17f, 0.17f), new Color(0.5f, 0.5f, 0.47f), height);
        color = Color.Lerp(color, new Color(0.2f, 0.3f, 0.14f), Mathf.Clamp01((TileNoise(u, v, 5f, 71f) - 0.6f) * 3f)); // musgo
        return T(color, height);
    }

    // Tela: trama fina de hilos cruzados (se tiñe con el color del material).
    private static Texel PaintCloth(float u, float v)
    {
        float weave = (Mathf.Sin(u * 90f * Mathf.PI) * 0.5f + 0.5f) * (Mathf.Sin(v * 90f * Mathf.PI) * 0.5f + 0.5f);
        float worn = TileNoise(u, v, 4f, 83f);
        float value = 0.72f + weave * 0.2f + worn * 0.12f;
        return T(new Color(value, value, value, 1f), weave);
    }

    // Agua: azul verdoso con ondas (el relieve es lo que más se nota al deslizar la textura).
    private static Texel PaintWater(float u, float v)
    {
        float waves = TileNoise(u, v, 6f, 97f);
        float ripples = TileNoise(u, v, 18f, 101f);
        Color color = Color.Lerp(new Color(0.03f, 0.14f, 0.17f), new Color(0.12f, 0.32f, 0.34f), waves);
        return T(color, waves * 0.6f + ripples * 0.4f);
    }

    // ------------------------------------------------------------------ Materiales con textura

    // Material URP Lit con textura, relieve y repetición. "cutout" recorta por transparencia (hierba) y dibuja las dos caras.
    private static Material GetOrCreateTexturedMaterial(string name, SurfaceTextures surface, Vector2 tiling, Color tint, float smoothness, bool cutout = false)
    {
        Material material = GetOrCreateMaterial(name, tint, null, 0f, smoothness);
        material.SetTexture("_BaseMap", surface.Albedo);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTexture("_BumpMap", surface.Normal);
        material.SetFloat("_BumpScale", 1f);
        material.EnableKeyword("_NORMALMAP");
        material.enableInstancing = true;

        if (cutout)
        {
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.4f);
            material.SetFloat("_Cull", 0f); // 0 = dibuja las dos caras
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    // Materiales compartidos por los niveles realistas. Las texturas se pintan una vez por construcción.
    private class RealMaterials
    {
        public Material Grass, Dirt, Bark, Leaves, PineLeaves, DryLeaves, GrassBlades, Wheat, Planks, DarkPlanks, Plaster, StoneWall, Cobble, RoofTiles, Rock, Water;
        public Material Cloth, ClothDark, Skin, Hair, Leather, Glow, Flame, Shadow, ShadowEye, Glass, Vine;
    }

    private static RealMaterials CreateRealMaterials(Color grassTint, Color leavesTint)
    {
        SurfaceTextures grass = MakeSurface("Hierba", PaintGrass, false, 3f);
        SurfaceTextures dirt = MakeSurface("Tierra", PaintDirt, false, 4f);
        SurfaceTextures bark = MakeSurface("Corteza", PaintBark, false, 6f);
        SurfaceTextures leaves = MakeSurface("Follaje", PaintLeaves, false, 5f);
        SurfaceTextures blades = MakeSurface("MataHierba", PaintGrassBlades, true, 1f);
        SurfaceTextures planks = MakeSurface("Tablones", PaintPlanks, false, 5f);
        SurfaceTextures plaster = MakeSurface("Yeso", PaintPlaster, false, 3f);
        SurfaceTextures stone = MakeSurface("MuroPiedra", PaintStoneWall, false, 6f);
        SurfaceTextures cobble = MakeSurface("Adoquin", PaintCobble, false, 6f);
        SurfaceTextures tiles = MakeSurface("Tejas", PaintRoofTiles, false, 6f);
        SurfaceTextures rock = MakeSurface("Roca", PaintRock, false, 5f);
        SurfaceTextures cloth = MakeSurface("Tela", PaintCloth, false, 2f);
        SurfaceTextures water = MakeSurface("Agua", PaintWater, false, 4f);

        RealMaterials real = new RealMaterials
        {
            // Las mallas llevan sus coordenadas de textura en metros: el "tiling" dice cuántas veces repite por metro.
            Grass = GetOrCreateTexturedMaterial("RealHierba", grass, Vector2.one * 0.25f, grassTint, 0.05f),
            Dirt = GetOrCreateTexturedMaterial("RealTierra", dirt, Vector2.one * 0.3f, Color.white, 0.1f),
            Bark = GetOrCreateTexturedMaterial("RealCorteza", bark, new Vector2(1f, 0.35f), Color.white, 0.1f),
            Leaves = GetOrCreateTexturedMaterial("RealFollaje", leaves, Vector2.one * 0.5f, leavesTint, 0.1f),
            PineLeaves = GetOrCreateTexturedMaterial("RealFollajePino", leaves, Vector2.one * 0.6f, leavesTint * new Color(0.55f, 0.75f, 0.7f), 0.1f),
            DryLeaves = GetOrCreateTexturedMaterial("RealFollajeSeco", leaves, Vector2.one * 0.5f, new Color(1.3f, 0.8f, 0.45f), 0.1f),
            GrassBlades = GetOrCreateTexturedMaterial("RealMataHierba", blades, Vector2.one, grassTint * 1.2f, 0.05f, true),
            Wheat = GetOrCreateTexturedMaterial("RealTrigo", blades, Vector2.one, new Color(1.5f, 1.15f, 0.55f), 0.05f, true),
            Planks = GetOrCreateTexturedMaterial("RealTablones", planks, Vector2.one * 0.5f, Color.white, 0.15f),
            DarkPlanks = GetOrCreateTexturedMaterial("RealVigas", planks, Vector2.one * 0.5f, new Color(0.45f, 0.38f, 0.32f), 0.15f),
            Plaster = GetOrCreateTexturedMaterial("RealYeso", plaster, Vector2.one * 0.3f, Color.white, 0.1f),
            StoneWall = GetOrCreateTexturedMaterial("RealMuroPiedra", stone, Vector2.one * 0.4f, Color.white, 0.2f),
            Cobble = GetOrCreateTexturedMaterial("RealAdoquin", cobble, Vector2.one * 0.5f, Color.white, 0.55f), // mojado: brilla
            RoofTiles = GetOrCreateTexturedMaterial("RealTejas", tiles, Vector2.one * 0.35f, Color.white, 0.3f),
            Rock = GetOrCreateTexturedMaterial("RealRoca", rock, Vector2.one * 0.3f, Color.white, 0.15f),
            Cloth = GetOrCreateTexturedMaterial("RealTela", cloth, Vector2.one * 3f, new Color(0.25f, 0.45f, 0.55f), 0.05f),
            ClothDark = GetOrCreateTexturedMaterial("RealTelaOscura", cloth, Vector2.one * 3f, new Color(0.3f, 0.22f, 0.15f), 0.05f),
            Leather = GetOrCreateTexturedMaterial("RealCuero", cloth, Vector2.one * 2f, new Color(0.2f, 0.12f, 0.07f), 0.35f),
            Vine = GetOrCreateTexturedMaterial("RealLiana", bark, new Vector2(1f, 0.5f), new Color(0.5f, 0.9f, 0.4f), 0.1f),
            Skin = GetOrCreateMaterial("RealPiel", new Color(0.86f, 0.66f, 0.52f), new Color(0.5f, 0.85f, 1f), 0.25f, 0.35f),
            Hair = GetOrCreateMaterial("RealPelo", new Color(0.1f, 0.07f, 0.05f), null, 0f, 0.4f),
            Glow = GetOrCreateMaterial("RealLuzKael", new Color(0.8f, 1f, 1f), new Color(0.4f, 0.95f, 1f), 4f, 0.7f),
            Flame = GetOrCreateMaterial("RealLlama", new Color(1f, 0.75f, 0.35f), new Color(1f, 0.6f, 0.2f), 4.5f, 0.3f),
            Shadow = GetOrCreateMaterial("RealSombra", new Color(0.02f, 0.02f, 0.03f), null, 0f, 0.05f),
            ShadowEye = GetOrCreateMaterial("RealOjoSombra", new Color(1f, 0.2f, 0.1f), new Color(1f, 0.15f, 0.05f), 5f, 0.5f),
            Glass = GetOrCreateMaterial("RealVentana", new Color(0.05f, 0.06f, 0.08f), new Color(1f, 0.7f, 0.35f), 0.08f, 0.55f),
        };

        real.Water = GetOrCreateTransparentMaterial("RealAgua", "Universal Render Pipeline/Lit", new Color(0.5f, 0.8f, 0.85f, 0.72f), null, 0f, false);
        real.Water.SetTexture("_BaseMap", water.Albedo);
        real.Water.SetTextureScale("_BaseMap", Vector2.one * 0.12f);
        real.Water.SetTexture("_BumpMap", water.Normal);
        real.Water.EnableKeyword("_NORMALMAP");
        real.Water.SetFloat("_Smoothness", 0.92f);
        EditorUtility.SetDirty(real.Water);
        return real;
    }

    // ------------------------------------------------------------------ Mallas

    // Guarda una malla como asset (Assets/Meshes) para que la escena no la lleve dentro y se pueda reutilizar.
    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        EnsureFolder(MeshesFolder);
        string path = MeshesFolder + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // Pequeño ayudante para ir sumando vértices y triángulos a una malla.
    private class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        public int Add(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            Uvs.Add(uv);
            return Vertices.Count - 1;
        }

        // Cara de cuatro esquinas (en orden a, b, c, d dando la vuelta). Las coordenadas de textura van en metros.
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 normal = Vector3.Cross(d - a, b - a).normalized;
            float width = Vector3.Distance(a, b), height = Vector3.Distance(a, d);
            int i = Add(a, normal, new Vector2(0f, 0f));
            Add(b, normal, new Vector2(width, 0f));
            Add(c, normal, new Vector2(width, height));
            Add(d, normal, new Vector2(0f, height));
            Triangles.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(c - a, b - a).normalized;
            int i = Add(a, normal, new Vector2(0f, 0f));
            Add(b, normal, new Vector2(Vector3.Distance(a, b), 0f));
            Add(c, normal, new Vector2(Vector3.Distance(a, b) / 2f, Vector3.Distance(a, c)));
            Triangles.AddRange(new[] { i, i + 2, i + 1 });
        }

        public Mesh ToMesh(bool recalculateNormals)
        {
            Mesh mesh = new Mesh();
            if (Vertices.Count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            if (recalculateNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // Terreno: rejilla de puntos cada "cell" metros cuya altura sale de "height(x, z)".
    private static Mesh BuildTerrainMesh(Rect area, float cell, System.Func<float, float, float> height)
    {
        int columns = Mathf.CeilToInt(area.width / cell), rows = Mathf.CeilToInt(area.height / cell);
        MeshBuilder builder = new MeshBuilder();
        for (int j = 0; j <= rows; j++)
        {
            for (int i = 0; i <= columns; i++)
            {
                float x = area.xMin + i * cell, z = area.yMin + j * cell;
                builder.Add(new Vector3(x, height(x, z), z), Vector3.up, new Vector2(x, z));
            }
        }

        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < columns; i++)
            {
                int a = j * (columns + 1) + i, b = a + 1, c = a + columns + 1, d = c + 1;
                builder.Triangles.AddRange(new[] { a, c, b, b, c, d });
            }
        }

        return builder.ToMesh(true);
    }

    // Cinta que sigue una lista de puntos pegada al terreno (sendero de tierra, calle de adoquines).
    private static Mesh BuildRibbonMesh(Vector2[] points, float width, System.Func<float, float, float> height, float lift)
    {
        MeshBuilder builder = new MeshBuilder();
        float travelled = 0f;
        List<Vector2> samples = new List<Vector2>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(points[i], points[i + 1]) / 1.5f));
            for (int s = 0; s < steps; s++)
            {
                samples.Add(Vector2.Lerp(points[i], points[i + 1], s / (float)steps));
            }
        }
        samples.Add(points[points.Length - 1]);

        for (int i = 0; i < samples.Count; i++)
        {
            Vector2 forward = (samples[Mathf.Min(i + 1, samples.Count - 1)] - samples[Mathf.Max(i - 1, 0)]).normalized;
            Vector2 side = new Vector2(-forward.y, forward.x) * width / 2f;
            if (i > 0)
            {
                travelled += Vector2.Distance(samples[i], samples[i - 1]);
            }

            Vector2 left = samples[i] + side, right = samples[i] - side;
            builder.Add(new Vector3(left.x, height(left.x, left.y) + lift, left.y), Vector3.up, new Vector2(0f, travelled));
            builder.Add(new Vector3(right.x, height(right.x, right.y) + lift, right.y), Vector3.up, new Vector2(width, travelled));
            if (i > 0)
            {
                int n = builder.Vertices.Count;
                builder.Triangles.AddRange(new[] { n - 4, n - 2, n - 3, n - 3, n - 2, n - 1 });
            }
        }

        return builder.ToMesh(true);
    }

    // Tronco: tubo que se estrecha hacia arriba y se tuerce un poco. La base queda en el origen.
    private static void AddTrunk(MeshBuilder builder, Vector3 basePoint, Vector3 direction, float length, float radiusBase, float radiusTop, int sides, float bend, System.Random rng)
    {
        const int rings = 5;
        direction.Normalize();
        Vector3 sideA = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 sideB = Vector3.Cross(direction, sideA);
        Vector3 lean = (sideA * ((float)rng.NextDouble() - 0.5f) + sideB * ((float)rng.NextDouble() - 0.5f)) * bend;
        int first = builder.Vertices.Count;

        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            Vector3 center = basePoint + direction * length * t + lean * t * t * length;
            float radius = Mathf.Lerp(radiusBase, radiusTop, t) * (r == 0 ? 1.35f : 1f); // la base se ensancha: raíces
            for (int s = 0; s <= sides; s++)
            {
                float angle = s / (float)sides * Mathf.PI * 2f;
                Vector3 normal = sideA * Mathf.Cos(angle) + sideB * Mathf.Sin(angle);
                builder.Add(center + normal * radius, normal, new Vector2(s / (float)sides * 2f, t * length));
            }
        }

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int a = first + r * (sides + 1) + s, b = a + 1, c = a + sides + 1, d = c + 1;
                builder.Triangles.AddRange(new[] { a, b, c, b, d, c });
            }
        }
    }

    // Bola irregular: una esfera cuyos puntos se empujan hacia dentro o hacia fuera con ruido. Copas y rocas.
    private static void AddBlob(MeshBuilder builder, Vector3 center, Vector3 radii, float roughness, float seed, int detail = 9)
    {
        int first = builder.Vertices.Count;
        for (int lat = 0; lat <= detail; lat++)
        {
            float polar = lat / (float)detail * Mathf.PI;
            for (int lon = 0; lon <= detail * 2; lon++)
            {
                float around = lon / (float)(detail * 2) * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Sin(polar) * Mathf.Cos(around), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(around));
                // El ruido se toma con seno y coseno del ángulo para que la costura de la esfera no se note.
                float noise = Fbm(direction.x * 1.7f + seed, direction.y * 1.7f + direction.z * 1.7f + seed, 3);
                float push = 1f + (noise - 0.5f) * 2f * roughness;
                builder.Add(center + Vector3.Scale(direction, radii) * push, direction, new Vector2(around * radii.x, polar * radii.y));
            }
        }

        int ring = detail * 2 + 1;
        for (int lat = 0; lat < detail; lat++)
        {
            for (int lon = 0; lon < detail * 2; lon++)
            {
                int a = first + lat * ring + lon, b = a + 1, c = a + ring, d = c + 1;
                builder.Triangles.AddRange(new[] { a, b, c, b, d, c });
            }
        }
    }

    // Falda de pino: cono abierto con el borde ondulado.
    private static void AddPineSkirt(MeshBuilder builder, float baseY, float height, float radius, float seed)
    {
        const int sides = 14;
        int first = builder.Vertices.Count;
        builder.Add(new Vector3(0f, baseY + height, 0f), Vector3.up, new Vector2(0.5f, height));
        for (int s = 0; s <= sides; s++)
        {
            float angle = s / (float)sides * Mathf.PI * 2f;
            float wave = 1f + (Hash(s % sides, (int)seed) - 0.5f) * 0.35f;
            Vector3 rim = new Vector3(Mathf.Cos(angle) * radius * wave, baseY - (s % 2) * 0.25f, Mathf.Sin(angle) * radius * wave);
            builder.Add(rim, new Vector3(Mathf.Cos(angle), 0.6f, Mathf.Sin(angle)).normalized, new Vector2(s / (float)sides * radius * 3f, 0f));
        }

        for (int s = 0; s < sides; s++)
        {
            builder.Triangles.AddRange(new[] { first, first + s + 2, first + s + 1 });
            builder.Triangles.AddRange(new[] { first, first + s + 1, first + s + 2 }); // también por debajo
        }
    }

    // Caja con coordenadas de textura en metros (los cubos de Unity estiran la textura en las paredes grandes).
    private static Mesh BuildBoxMesh(Vector3 size)
    {
        Vector3 h = size / 2f;
        MeshBuilder builder = new MeshBuilder();
        Vector3 a = new Vector3(-h.x, -h.y, -h.z), b = new Vector3(h.x, -h.y, -h.z), c = new Vector3(h.x, h.y, -h.z), d = new Vector3(-h.x, h.y, -h.z);
        Vector3 e = new Vector3(-h.x, -h.y, h.z), f = new Vector3(h.x, -h.y, h.z), g = new Vector3(h.x, h.y, h.z), k = new Vector3(-h.x, h.y, h.z);
        builder.Quad(a, b, c, d); // sur
        builder.Quad(f, e, k, g); // norte
        builder.Quad(e, a, d, k); // oeste
        builder.Quad(b, f, g, c); // este
        builder.Quad(d, c, g, k); // arriba
        builder.Quad(e, f, b, a); // abajo
        return builder.ToMesh(false);
    }

    // Tejado a dos aguas: la cumbrera corre a lo largo de Z. "overhang" es el alero.
    private static Mesh BuildGableRoofMesh(float width, float depth, float rise, float overhang)
    {
        float halfWidth = width / 2f + overhang, halfDepth = depth / 2f + overhang;
        MeshBuilder builder = new MeshBuilder();
        Vector3 westSouth = new Vector3(-halfWidth, 0f, -halfDepth), westNorth = new Vector3(-halfWidth, 0f, halfDepth);
        Vector3 eastSouth = new Vector3(halfWidth, 0f, -halfDepth), eastNorth = new Vector3(halfWidth, 0f, halfDepth);
        Vector3 ridgeSouth = new Vector3(0f, rise, -halfDepth), ridgeNorth = new Vector3(0f, rise, halfDepth);
        builder.Quad(westNorth, westSouth, ridgeSouth, ridgeNorth);   // faldón oeste
        builder.Quad(eastSouth, eastNorth, ridgeNorth, ridgeSouth);   // faldón este
        builder.Quad(westSouth, westNorth, ridgeNorth, ridgeSouth);   // caras de abajo (se ven desde dentro)
        builder.Quad(eastNorth, eastSouth, ridgeSouth, ridgeNorth);
        builder.Triangle(westSouth, eastSouth, ridgeSouth);           // hastial sur
        builder.Triangle(eastNorth, westNorth, ridgeNorth);           // hastial norte
        return builder.ToMesh(false);
    }

    // Matas de hierba: en cada punto, dos láminas cruzadas con la textura de briznas. Todo el parche es una sola malla.
    private static Mesh BuildGrassMesh(Rect area, int tufts, float tuftHeight, System.Func<float, float, float> height, System.Func<float, float, bool> allowed, int seed)
    {
        System.Random rng = new System.Random(seed);
        MeshBuilder builder = new MeshBuilder();
        for (int i = 0; i < tufts; i++)
        {
            float x = Mathf.Lerp(area.xMin, area.xMax, (float)rng.NextDouble());
            float z = Mathf.Lerp(area.yMin, area.yMax, (float)rng.NextDouble());
            float tall = tuftHeight * (0.7f + (float)rng.NextDouble() * 0.6f);
            float angle = (float)rng.NextDouble() * Mathf.PI;
            if (allowed != null && !allowed(x, z))
            {
                continue;
            }

            Vector3 root = new Vector3(x, height(x, z) - 0.05f, z);
            for (int card = 0; card < 2; card++)
            {
                float turn = angle + card * Mathf.PI / 2f;
                Vector3 side = new Vector3(Mathf.Cos(turn), 0f, Mathf.Sin(turn)) * tall * 0.55f;
                int first = builder.Vertices.Count;
                // Las normales miran hacia arriba: así la hierba se ilumina como el suelo y no parece cartón.
                builder.Add(root - side, Vector3.up, new Vector2(0f, 0f));
                builder.Add(root + side, Vector3.up, new Vector2(1f, 0f));
                builder.Add(root + side + Vector3.up * tall, Vector3.up, new Vector2(1f, 1f));
                builder.Add(root - side + Vector3.up * tall, Vector3.up, new Vector2(0f, 1f));
                builder.Triangles.AddRange(new[] { first, first + 2, first + 1, first, first + 3, first + 2 });
            }
        }

        return builder.ToMesh(false);
    }

    // ------------------------------------------------------------------ Piezas con malla

    private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Vector3 position, bool solid, params Material[] materials)
    {
        GameObject meshObject = new GameObject(name);
        meshObject.transform.SetParent(parent, false);
        meshObject.transform.position = position;
        meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
        if (solid)
        {
            meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        return meshObject;
    }

    // Bloque con textura bien repetida (paredes, vigas, tablones, muros). Sólido salvo que se indique.
    private static GameObject CreateRealBlock(string name, Transform parent, Vector3 center, Vector3 size, Material material, bool solid = true)
    {
        GameObject block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = center;
        block.AddComponent<MeshFilter>().sharedMesh = BuildBoxMesh(size);
        block.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (solid)
        {
            block.AddComponent<BoxCollider>().size = size;
        }

        return block;
    }

    // Tres rocas distintas guardadas como asset; cada roca de la escena reutiliza una de ellas con otra escala y giro.
    private static Mesh[] CreateRockMeshes()
    {
        Mesh[] meshes = new Mesh[3];
        for (int i = 0; i < meshes.Length; i++)
        {
            MeshBuilder builder = new MeshBuilder();
            AddBlob(builder, Vector3.zero, new Vector3(1f, 0.7f + i * 0.12f, 0.85f), 0.38f, 200f + i * 31f, 7);
            meshes[i] = SaveMesh(builder.ToMesh(true), "Roca" + (i + 1));
        }

        return meshes;
    }

    private static GameObject CreateRock(Transform parent, Mesh[] meshes, Vector3 position, float size, Material material, System.Random rng, bool solid)
    {
        Mesh mesh = meshes[rng.Next(meshes.Length)];
        GameObject rock = CreateMeshObject("Roca", parent, mesh, position, false, material);
        rock.transform.localScale = new Vector3(size * (0.8f + (float)rng.NextDouble() * 0.5f), size * (0.6f + (float)rng.NextDouble() * 0.5f), size * (0.8f + (float)rng.NextDouble() * 0.5f));
        rock.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 20f);
        if (solid)
        {
            rock.AddComponent<SphereCollider>().radius = 0.85f;
        }

        return rock;
    }

    // Árbol = tronco (malla con corteza) + copa (malla con follaje, que se mece). kind: 0 pino, 1 roble, 2 árbol seco.
    private class TreeModel
    {
        public Mesh Trunk, Crown;
        public float TrunkRadius;
    }

    private static TreeModel[] CreateTreeModels(string name, int kind, int variants)
    {
        TreeModel[] models = new TreeModel[variants];
        for (int v = 0; v < variants; v++)
        {
            System.Random rng = new System.Random(kind * 100 + v);
            MeshBuilder trunk = new MeshBuilder(), crown = new MeshBuilder();
            float height = kind == 0 ? 11f + v * 2f : 7.5f + v * 1.5f;
            float radius = kind == 0 ? 0.32f : 0.45f;
            AddTrunk(trunk, Vector3.zero, Vector3.up, height * (kind == 0 ? 1f : 0.6f), radius, radius * 0.35f, 9, 0.02f, rng);

            if (kind == 0)
            {
                // Pino: cinco faldas, cada una más pequeña y más alta.
                for (int i = 0; i < 5; i++)
                {
                    float t = i / 4f;
                    AddPineSkirt(crown, height * (0.28f + t * 0.52f), height * 0.3f, Mathf.Lerp(3.1f, 1f, t), v * 10 + i);
                }
            }
            else
            {
                // Roble y árbol seco: ramas que salen del tronco; el roble lleva una bola de follaje en cada punta.
                float fork = height * 0.5f;
                int branches = 5 + v;
                for (int i = 0; i < branches; i++)
                {
                    float angle = i / (float)branches * Mathf.PI * 2f + (float)rng.NextDouble();
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0.75f + (float)rng.NextDouble() * 0.5f, Mathf.Sin(angle)).normalized;
                    float length = height * (0.3f + (float)rng.NextDouble() * 0.15f);
                    Vector3 start = Vector3.up * (fork - (float)rng.NextDouble() * 1.2f);
                    AddTrunk(trunk, start, direction, length, radius * 0.4f, radius * 0.12f, 6, 0.04f, rng);
                    if (kind == 1)
                    {
                        float size = 1.7f + (float)rng.NextDouble() * 0.9f;
                        AddBlob(crown, start + direction * length, new Vector3(size, size * 0.75f, size), 0.3f, v * 13f + i * 7f, 7);
                    }
                }

                if (kind == 1)
                {
                    AddBlob(crown, Vector3.up * height * 0.82f, new Vector3(2.6f, 2f, 2.6f), 0.3f, v * 17f, 8);
                }
            }

            models[v] = new TreeModel
            {
                Trunk = SaveMesh(trunk.ToMesh(false), name + "Tronco" + (v + 1)),
                Crown = kind == 2 ? null : SaveMesh(crown.ToMesh(kind == 1), name + "Copa" + (v + 1)),
                TrunkRadius = radius,
            };
        }

        return models;
    }

    private static GameObject PlaceTree(Transform parent, TreeModel[] models, Vector3 position, float scale, Material bark, Material leaves, System.Random rng, bool solid)
    {
        TreeModel model = models[rng.Next(models.Length)];
        GameObject tree = CreateMeshObject("Arbol", parent, model.Trunk, position, false, bark);
        tree.transform.localScale = Vector3.one * scale;
        tree.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        if (solid)
        {
            CapsuleCollider trunk = tree.AddComponent<CapsuleCollider>();
            trunk.radius = model.TrunkRadius;
            trunk.height = 6f;
            trunk.center = Vector3.up * 3f;
        }

        if (model.Crown != null)
        {
            GameObject crown = CreateMeshObject("Copa", tree.transform, model.Crown, position, false, leaves);
            crown.transform.localPosition = Vector3.zero;
            crown.transform.localRotation = Quaternion.identity;
            crown.transform.localScale = Vector3.one;
            crown.AddComponent<Sway>();
        }

        return tree;
    }

    // ------------------------------------------------------------------ Cielo y post-proceso

    // Cielo con atmósfera y disco solar (shader "Skybox/Procedural" que trae Unity). El sol es la luz direccional.
    private static void ConfigureRealSky(string name, Color skyTint, Color ground, float atmosphere, float exposure, float sunSize)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Fragmento: no se encontró el shader Skybox/Procedural; el nivel queda con fondo de color.");
                return;
            }

            sky = new Material(shader);
            AssetDatabase.CreateAsset(sky, path);
        }

        sky.SetColor("_SkyTint", skyTint);
        sky.SetColor("_GroundColor", ground);
        sky.SetFloat("_AtmosphereThickness", atmosphere);
        sky.SetFloat("_Exposure", exposure);
        sky.SetFloat("_SunSize", sunSize);
        sky.SetFloat("_SunSizeConvergence", 4f);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        Camera.main.clearFlags = CameraClearFlags.Skybox;
    }

    // Post-proceso "de cine" para los niveles realistas: menos brillo mágico, balance de color, desenfoque lejano y grano.
    private static void TuneRealVolume(VolumeProfile profile, float temperature, float saturation, float exposure, float farBlurStart)
    {
        if (profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.Override(0.45f);
            bloom.threshold.Override(1.15f);
            EditorUtility.SetDirty(bloom);
        }

        if (profile.TryGet(out ColorAdjustments adjustments))
        {
            adjustments.saturation.Override(saturation);
            adjustments.postExposure.Override(exposure);
            adjustments.contrast.Override(16f);
            EditorUtility.SetDirty(adjustments);
        }

        if (profile.TryGet(out Vignette vignette))
        {
            vignette.intensity.Override(0.36f);
            EditorUtility.SetDirty(vignette);
        }

        WhiteBalance balance = GetOrAddOverride<WhiteBalance>(profile);
        balance.temperature.Override(temperature);

        DepthOfField blur = GetOrAddOverride<DepthOfField>(profile);
        blur.mode.Override(DepthOfFieldMode.Gaussian);
        blur.gaussianStart.Override(farBlurStart);
        blur.gaussianEnd.Override(farBlurStart * 2.5f);
        blur.gaussianMaxRadius.Override(0.7f);

        FilmGrain grain = GetOrAddOverride<FilmGrain>(profile);
        grain.intensity.Override(0.18f);

        EditorUtility.SetDirty(balance);
        EditorUtility.SetDirty(blur);
        EditorUtility.SetDirty(grain);
        EditorUtility.SetDirty(profile);
    }

    private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        return component;
    }

    // Marca un objeto y sus hijos como estáticos: Unity los dibuja en lote y el nivel va más fluido.
    private static void MakeStatic(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }
}
