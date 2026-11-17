using UnityEditor;
using UnityEngine;

/// <summary>
/// QUÉ HACE
///   Genera el Nivel 9, "El Hogar" (Nivel09_Hogar.unity): la habitación de la infancia, vista por un Kael
///   diminuto. Todo mide 8 veces lo normal: la alfombra es una plaza, la silla un edificio, la mesa una meseta a
///   6 m, la estantería un acantilado de 17 m y la cama, al fondo, bañada por el sol de la tarde.
///
/// POR QUÉ EXISTE
///   Noveno paso del arco: el último recuerdo antes del mundo real es la casa. Kael ya es una persona (ropa de
///   casa, sin luz). Su identidad de juego es la ESCALA: los muebles son el nivel. Dinámicas: una regla que cae
///   como puente, cajones que salen como escalones y un tocadiscos que gira. Peligros de juguete: el tren que da
///   vueltas a la alfombra, un coche de cuerda en la mesa, una peonza en la cómoda y el péndulo del reloj.
///
/// CÓMO SE USA
///   Menú Fragmento > Construir Nivel 9 (en el Editor; no por batchmode). La escena se SOBRESCRIBE.
///   Recorrido: alfombra > bloques de juguete > silla > libros > mesa > regla > estantería > escalera > techo de la
///   estantería (péndulo) > suelo > cajones > cómoda (tocadiscos) > cojines > cama (núcleo y portal).
/// </summary>
public static partial class FragmentoSceneBuilder
{
    private const string Nivel09ScenePath = "Assets/Scenes/Nivel09_Hogar.unity";

    private static RealMaterials homeReal;

    private class HomeMaterials
    {
        public Material Wallpaper, Rug, Quilt, Floor, Wood, DarkWood, Ceiling, Paper, Brass, Black, Cushion, SunBeam;
        public Material[] Toys, Covers;
    }

    private static HomeMaterials CreateHomeMaterials(RealMaterials real)
    {
        SurfaceTextures wallpaper = MakeSurface("PapelPintado", PaintWallpaper, false, 2f);
        SurfaceTextures rug = MakeSurface("Alfombra", PaintRug, false, 3f);
        SurfaceTextures quilt = MakeSurface("Colcha", PaintQuilt, false, 3f);

        HomeMaterials home = new HomeMaterials
        {
            // Todo es 8 veces más grande: las texturas se repiten 8 veces menos por metro.
            Wallpaper = GetOrCreateTexturedMaterial("CasaPapelPintado", wallpaper, Vector2.one / 12f, Color.white, 0.1f),
            Rug = GetOrCreateTexturedMaterial("CasaAlfombra", rug, new Vector2(1f / 28f, 1f / 20f), Color.white, 0.05f),
            Quilt = GetOrCreateTexturedMaterial("CasaColcha", quilt, Vector2.one / 16f, Color.white, 0.05f),
            Floor = GetOrCreateTexturedMaterialCopy("CasaParquet", real.Planks, new Color(1.1f, 0.85f, 0.6f)),
            Wood = GetOrCreateTexturedMaterialCopy("CasaMadera", real.Planks, new Color(1f, 0.78f, 0.55f)),
            DarkWood = GetOrCreateTexturedMaterialCopy("CasaMaderaOscura", real.Planks, new Color(0.55f, 0.38f, 0.26f)),
            Ceiling = GetOrCreateTexturedMaterialCopy("CasaTecho", real.Plaster, new Color(1f, 0.97f, 0.92f)),
            Cushion = GetOrCreateTexturedMaterialCopy("CasaCojin", real.Cloth, new Color(0.85f, 0.55f, 0.35f)),
            Paper = GetOrCreateMaterial("CasaPapel", new Color(0.92f, 0.88f, 0.78f), null, 0f, 0.1f),
            Brass = GetOrCreateMaterial("CasaLaton", new Color(0.75f, 0.55f, 0.25f), null, 0f, 0.8f),
            Black = GetOrCreateMaterial("CasaVinilo", new Color(0.03f, 0.03f, 0.03f), null, 0f, 0.85f),
            Toys = new[]
            {
                GetOrCreateMaterial("CasaJugueteRojo", new Color(0.75f, 0.12f, 0.1f), null, 0f, 0.6f),
                GetOrCreateMaterial("CasaJugueteAzul", new Color(0.12f, 0.3f, 0.7f), null, 0f, 0.6f),
                GetOrCreateMaterial("CasaJugueteAmarillo", new Color(0.9f, 0.7f, 0.12f), null, 0f, 0.6f),
                GetOrCreateMaterial("CasaJugueteVerde", new Color(0.2f, 0.55f, 0.25f), null, 0f, 0.6f),
            },
        };

        home.Floor.SetTextureScale("_BaseMap", Vector2.one / 16f);
        home.Wood.SetTextureScale("_BaseMap", Vector2.one / 10f);
        home.DarkWood.SetTextureScale("_BaseMap", Vector2.one / 10f);
        home.Ceiling.SetTextureScale("_BaseMap", Vector2.one / 20f);
        home.Cushion.SetTextureScale("_BaseMap", Vector2.one / 4f);
        home.Covers = new[]
        {
            GetOrCreateTexturedMaterialCopy("CasaLibroRojo", real.Cloth, new Color(0.5f, 0.1f, 0.08f)),
            GetOrCreateTexturedMaterialCopy("CasaLibroVerde", real.Cloth, new Color(0.12f, 0.3f, 0.18f)),
            GetOrCreateTexturedMaterialCopy("CasaLibroAzul", real.Cloth, new Color(0.1f, 0.16f, 0.35f)),
            GetOrCreateTexturedMaterialCopy("CasaLibroOcre", real.Cloth, new Color(0.65f, 0.45f, 0.18f)),
        };
        home.SunBeam = GetOrCreateTransparentMaterial("CasaRayoDeSol", "Universal Render Pipeline/Lit", new Color(1f, 0.85f, 0.55f, 0.05f), new Color(1f, 0.8f, 0.5f), 0.4f, true);
        return home;
    }

    [MenuItem("Fragmento/Construir Nivel 9")]
    public static void BuildNivel9()
    {
        BuildThemedLevel(CreateNivel9Theme);
    }

    private static LevelTheme CreateNivel9Theme(Materials materials)
    {
        homeReal = CreateRealMaterials(new Color(0.85f, 0.95f, 0.75f), new Color(0.8f, 1f, 0.75f));
        materials.Ruin = homeReal.Planks;
        materials.Stone = homeReal.Planks;
        materials.StoneTop = homeReal.Planks;

        return new LevelTheme
        {
            ScenePath = Nivel09ScenePath,
            VolumePath = SettingsFolder + "/Nivel09_PerfilVolumen.asset",
            NextScenePath = "",
            FilePrefix = "N9_",
            KaelPrefabName = "Kael_Nivel9",
            KaelStage = 9,
            FragmentFiles = new[] { "Juego", "Merienda", "Cuento", "Reloj", "Cancion", "Siesta", "Nucleo" },
            FragmentTitles = new[]
            {
                "Fragmento del Juego", "Fragmento de la Merienda", "Fragmento del Cuento", "Fragmento del Reloj",
                "Fragmento de la Canción", "Fragmento de la Siesta", "Fragmento del Núcleo",
            },
            FragmentMessages = new[]
            {
                "Los bloques de juguete se apilan junto a la silla.",
                "La regla se vence y cae hasta la estantería.",
                "Una escalera de juguete se apoya en la estantería.",
                "Los cajones de la cómoda se abren como una escalera.",
                "La lámpara de la mesa se enciende.",
                "Los cojines se amontonan al pie de la cama.",
                "El Núcleo de la Realidad vuelve a brillar. Ya casi recuerdas quién eres.",
            },
            EventIds = new[] { "bloques_1", "regla_2", "escalera_3", "cajones_4", "lampara_5", "cojines_6", "nucleo_7" },
            Growth = homeReal.Planks,
            Accent = homeReal.Flame,
            Background = new Color(0.5f, 0.4f, 0.3f),
            FogColor = new Color(0.55f, 0.45f, 0.35f),
            FogDensity = 0.004f,
            SunColor = new Color(1f, 0.8f, 0.55f),
            SunIntensity = 2.2f,
            SunEuler = new Vector3(32f, -95f, 0f),
            AmbientSky = new Color(0.55f, 0.45f, 0.35f),
            AmbientEquator = new Color(0.42f, 0.33f, 0.25f),
            AmbientGround = new Color(0.18f, 0.13f, 0.09f),
            ZoneLightColor = Color.white,
            VolumeFilter = new Color(1f, 0.96f, 0.9f),
            KillY = -10f,
            Spawn = new Vector3(0f, 0.05f, -21f),
            Zones = new ZoneInfo[0],
            RespawnAtCheckpoint = true,
            CreateKael = m => CreateHumanKaelPrefab(m, homeReal, "Kael_Nivel9", 9),
            TuneVolume = profile => TuneRealVolume(profile, 15f, 6f, 0f, 38f),
            BuildLayout = BuildHogarLayout,
        };
    }

    // Bloques que crecen desde el suelo uno tras otro (CrystalGrow): cada uno con su altura y su color.
    private static GameObject CreateGrowingBlocks(string name, string eventId, Vector3[] feet, Vector3[] sizes, Material[] materials)
    {
        GameObject root = new GameObject(name);
        Transform[] pivots = new Transform[feet.Length];
        for (int i = 0; i < feet.Length; i++)
        {
            Transform pivot = CreatePivot("Bloque " + (i + 1), root.transform, Vector3.zero);
            pivot.position = feet[i];
            CreateRealBlock("Cuerpo", pivot, feet[i] + Vector3.up * sizes[i].y / 2f, sizes[i], materials[i % materials.Length]);
            pivots[i] = pivot;
        }

        CrystalGrow grow = root.AddComponent<CrystalGrow>();
        grow.SetCrystals(pivots);
        SerializedObject serialized = new SerializedObject(grow);
        Property(serialized, "eventId").stringValue = eventId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(grow);
        return root;
    }

    // Mueble de cuatro patas con tablero (mesa, silla). top = altura de la cara de arriba.
    private static void CreateFurnitureTop(Transform parent, string name, Rect footprint, float top, float thickness, float legSize, Material material)
    {
        Vector3 center = new Vector3(footprint.center.x, 0f, footprint.center.y);
        CreateRealBlock(name + " tablero", parent, center + Vector3.up * (top - thickness / 2f), new Vector3(footprint.width, thickness, footprint.height), material);
        float legHeight = top - thickness;
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = new Vector3((i % 2 == 0 ? -1f : 1f) * (footprint.width / 2f - legSize / 2f - 0.2f), legHeight / 2f, (i < 2 ? -1f : 1f) * (footprint.height / 2f - legSize / 2f - 0.2f));
            CreateRealBlock(name + " pata", parent, center + corner, new Vector3(legSize, legHeight, legSize), material);
        }
    }

    // Libros de pie en una balda: lomos de colores y alturas distintas, de xMin a xMax.
    private static void CreateBookRow(Transform parent, float xMin, float xMax, float shelfTop, float zFront, float zBack, float maxHeight, HomeMaterials home, System.Random rng)
    {
        float x = xMin;
        while (x < xMax - 0.6f)
        {
            float width = 0.6f + (float)rng.NextDouble() * 0.7f;
            width = Mathf.Min(width, xMax - x);
            float height = maxHeight * (0.65f + (float)rng.NextDouble() * 0.35f);
            CreateRealBlock("Libro", parent, new Vector3(x + width / 2f, shelfTop + height / 2f, (zFront + zBack) / 2f), new Vector3(width - 0.05f, height, zBack - zFront), home.Covers[rng.Next(home.Covers.Length)]);
            x += width;
        }
    }

    private static void BuildHogarLayout(LevelContext context)
    {
        Transform level = context.Level;
        Materials materials = context.Materials;
        RealMaterials real = homeReal;
        HomeMaterials home = CreateHomeMaterials(real);
        System.Random rng = new System.Random(9);

        ConfigureRealSky("CieloTarde", new Color(0.5f, 0.55f, 0.65f), new Color(0.35f, 0.3f, 0.25f), 1.3f, 1.2f, 0.05f);
        new GameObject("Pistas del nivel").AddComponent<LevelHint>().Setup(
            "Eres diminuto: la habitación entera es el nivel.",
            "Cruza la vía cuando el tren haya pasado. Los juguetes y el péndulo te derriban.",
            "Las lámparas encendidas (F) son tu punto de regreso.");

        // ================= HABITACIÓN (64 x 56 m, 24 m de alto) =================
        CreateRealBlock("Suelo", level, new Vector3(0f, -0.5f, 4f), new Vector3(66f, 1f, 58f), home.Floor);
        CreateRealBlock("Pared sur", level, new Vector3(0f, 12f, -24.5f), new Vector3(66f, 24f, 1f), home.Wallpaper);
        CreateRealBlock("Pared norte", level, new Vector3(0f, 12f, 32.5f), new Vector3(66f, 24f, 1f), home.Wallpaper);
        CreateRealBlock("Pared oeste", level, new Vector3(-32.5f, 12f, 4f), new Vector3(1f, 24f, 58f), home.Wallpaper);
        // Pared este con la ventana (z 4..18, y 9..21).
        CreateRealBlock("Pared este bajo la ventana", level, new Vector3(32.5f, 4.5f, 4f), new Vector3(1f, 9f, 58f), home.Wallpaper);
        CreateRealBlock("Pared este sobre la ventana", level, new Vector3(32.5f, 22.5f, 4f), new Vector3(1f, 3f, 58f), home.Wallpaper);
        CreateRealBlock("Pared este sur", level, new Vector3(32.5f, 15f, -10f), new Vector3(1f, 12f, 28f), home.Wallpaper);
        CreateRealBlock("Pared este norte", level, new Vector3(32.5f, 15f, 25f), new Vector3(1f, 12f, 14f), home.Wallpaper);
        CreateRealBlock("Techo", level, new Vector3(0f, 24.5f, 4f), new Vector3(66f, 1f, 58f), home.Ceiling);
        for (int i = 0; i < 4; i++)
        {
            // Rodapié de madera.
            bool alongX = i < 2;
            Vector3 position = alongX ? new Vector3(0f, 0.6f, i == 0 ? -23.9f : 31.9f) : new Vector3(i == 2 ? -31.9f : 31.9f, 0.6f, 4f);
            NoShadow(CreateRealBlock("Rodapie", level, position, alongX ? new Vector3(64f, 1.2f, 0.25f) : new Vector3(0.25f, 1.2f, 56f), home.DarkWood, false));
        }

        // Marco de la ventana, alféizar y cortinas.
        CreateRealBlock("Alfeizar", level, new Vector3(31.4f, 8.8f, 11f), new Vector3(1.4f, 0.4f, 15f), home.Wood, false);
        CreateRealBlock("Marco superior", level, new Vector3(31.8f, 21.2f, 11f), new Vector3(0.6f, 0.4f, 14.4f), home.Wood, false);
        CreateRealBlock("Parteluz", level, new Vector3(31.9f, 15f, 11f), new Vector3(0.3f, 12f, 0.3f), home.Wood, false);
        CreateRealBlock("Travesano", level, new Vector3(31.9f, 15f, 11f), new Vector3(0.3f, 0.3f, 14f), home.Wood, false);
        Material curtain = GetOrCreateTexturedMaterialCopy("CasaCortina", real.Cloth, new Color(0.85f, 0.75f, 0.55f));
        curtain.SetTextureScale("_BaseMap", Vector2.one / 4f);
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? 2.2f : 19.8f;
            for (int fold = 0; fold < 3; fold++)
            {
                CreateRealBlock("Cortina", level, new Vector3(30.8f - fold * 0.25f, 14.5f, z + (side == 0 ? -1f : 1f) * fold * 0.9f), new Vector3(0.5f, 15f, 1.1f), curtain, false).AddComponent<Sway>();
            }
        }

        // Polvo flotando en la luz de la ventana.
        CreateMotes("Polvo en el sol", level, new Vector3(22f, 10f, 11f), new Vector3(16f, 12f, 14f), materials.Star, new Color(1.6f, 1.3f, 0.9f, 0.55f), 300, 30f, 0.07f, 0.005f);

        // ================= FRAGMENTOS =================
        context.Fragment(1, new Vector3(0f, 1.38f, -8f));        // centro de la alfombra, dentro de la vía
        context.Fragment(2, new Vector3(-9f, 7.3f, 21f));        // mesa
        context.Fragment(3, new Vector3(14f, 7.7f, 30f));        // repisa de la estantería
        context.Fragment(4, new Vector3(-1.5f, 18.2f, 30f));     // techo de la estantería, tras el péndulo
        context.Fragment(5, new Vector3(20f, 8.5f, -18f));       // cómoda
        context.Fragment(6, new Vector3(25.5f, 9.4f, -14.5f));   // sobre el tocadiscos
        context.Fragment(7, new Vector3(20f, 5.9f, 5f));         // cama

        // ================= ALFOMBRA Y TREN =================
        CreateRealBlock("Alfombra", level, new Vector3(0f, 0.04f, -8f), new Vector3(28f, 0.08f, 20f), home.Rug);
        const int trackPoints = 32;
        Vector3[] track = new Vector3[trackPoints];
        for (int i = 0; i < trackPoints; i++)
        {
            float angle = i * Mathf.PI * 2f / trackPoints;
            track[i] = new Vector3(Mathf.Cos(angle) * 9f, 0.08f, -8f + Mathf.Sin(angle) * 9f);
            NoShadow(CreateVisualPart("Traviesa", level, PrimitiveType.Cube, track[i] + Vector3.up * 0.04f, new Vector3(0.35f, 0.08f, 2.2f), new Vector3(0f, -angle * Mathf.Rad2Deg, 0f), home.DarkWood));
        }
        for (int rail = 0; rail < 2; rail++)
        {
            GameObject railObject = new GameObject("Rail");
            railObject.transform.SetParent(level, false);
            LineRenderer line = railObject.AddComponent<LineRenderer>();
            line.loop = true;
            line.widthMultiplier = 0.15f;
            line.sharedMaterial = home.Brass;
            line.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                float radius = rail == 0 ? 8.4f : 9.6f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0.2f, -8f + Mathf.Sin(angle) * radius));
            }
        }

        // Locomotora y dos vagones: cada uno es un peligro que sigue la vía (empiezan unos puntos por detrás).
        for (int car = 0; car < 3; car++)
        {
            Vector3[] route = new Vector3[trackPoints];
            for (int i = 0; i < trackPoints; i++)
            {
                route[i] = track[(i - car * 2 + trackPoints) % trackPoints];
            }

            GameObject wagon = new GameObject(car == 0 ? "Tren de juguete" : "Vagon " + car);
            wagon.transform.SetParent(level, false);
            Material paint = home.Toys[car % home.Toys.Length];
            CreateVisualPart("Caja", wagon.transform, PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(1.8f, 1.5f, 3f), Vector3.zero, paint);
            for (int w = 0; w < 4; w++)
            {
                CreateVisualPart("Rueda", wagon.transform, PrimitiveType.Cylinder, new Vector3(w % 2 == 0 ? -0.9f : 0.9f, 0.4f, w < 2 ? 0.9f : -0.9f), new Vector3(0.7f, 0.08f, 0.7f), new Vector3(0f, 0f, 90f), home.Black);
            }
            if (car == 0)
            {
                CreateVisualPart("Cabina", wagon.transform, PrimitiveType.Cube, new Vector3(0f, 2.2f, -0.8f), new Vector3(1.8f, 1f, 1.3f), Vector3.zero, home.Toys[2]);
                CreateVisualPart("Chimenea", wagon.transform, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 1f), new Vector3(0.5f, 0.5f, 0.5f), Vector3.zero, home.Black);
            }

            wagon.AddComponent<PathHazard>().Setup(route, true, 5f, 1.7f);
        }

        // ================= SILLA, LIBROS Y MESA =================
        CreateFurnitureTop(level, "Silla", Rect.MinMaxRect(-20f, 2f, -12f, 10f), 3.6f, 0.5f, 0.6f, home.Wood);
        CreateRealBlock("Respaldo", level, new Vector3(-19.7f, 6.3f, 6f), new Vector3(0.6f, 5.4f, 8f), home.Wood);
        CreateRealBlock("Libro sobre la silla", level, new Vector3(-16f, 4.05f, 8.9f), new Vector3(4f, 0.9f, 1.8f), home.Covers[0]);
        for (int i = 0; i < 6; i++)
        {
            // Pila de libros en el suelo entre la silla y la mesa: cada uno un poco girado.
            GameObject book = CreateRealBlock("Libro de la pila", level, new Vector3(-16f + (i % 2) * 0.2f, 0.45f + i * 0.9f, 12f), new Vector3(4.2f - (i % 3) * 0.2f, 0.88f, 2.8f), home.Covers[i % home.Covers.Length]);
            book.transform.rotation = Quaternion.Euler(0f, (i % 3 - 1) * 3f, 0f);
        }
        CreateFurnitureTop(level, "Mesa", Rect.MinMaxRect(-22f, 14f, -6f, 26f), 6f, 0.6f, 0.8f, home.Wood);
        GameObject cup = CreateVisualPart("Taza", level, PrimitiveType.Cylinder, new Vector3(-12f, 7f, 24f), new Vector3(1.6f, 1f, 1.6f), Vector3.zero, home.Toys[1]);
        cup.AddComponent<CapsuleCollider>();
        NoShadow(CreateVisualPart("Papel", level, PrimitiveType.Cube, new Vector3(-15f, 6.02f, 17f), new Vector3(4f, 0.02f, 5.6f), new Vector3(0f, 14f, 0f), home.Paper));
        GameObject crayon = CreateVisualPart("Lapiz de color", level, PrimitiveType.Cylinder, new Vector3(-17f, 6.2f, 21f), new Vector3(0.4f, 2.4f, 0.4f), new Vector3(90f, 30f, 0f), home.Toys[0]);
        NoShadow(crayon);

        // Coche de cuerda que va y viene por la mesa.
        GameObject windUp = new GameObject("Coche de cuerda");
        windUp.transform.SetParent(level, false);
        CreateVisualPart("Carroceria", windUp.transform, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.6f, 2f), Vector3.zero, home.Toys[0]);
        CreateVisualPart("Cabina", windUp.transform, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.2f), new Vector3(1f, 0.45f, 1f), Vector3.zero, home.Toys[2]);
        CreateVisualPart("Llave", windUp.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, -1.2f), new Vector3(0.6f, 0.4f, 0.08f), Vector3.zero, home.Brass);
        windUp.AddComponent<PathHazard>().Setup(new[] { new Vector3(-20.5f, 6f, 18.5f), new Vector3(-7.5f, 6f, 18.5f) }, false, 4.5f, 1.3f);

        // ================= ESTANTERÍA =================
        // x -4..12, z 28..32. Baldas: S1 a 6.4 (con repisa que sale hasta x 16), S2 a 11.4, techo a 16.9.
        // El lateral derecho solo ocupa el fondo (z 29.6..32): por delante se pasa de S1 a la repisa.
        Transform shelf = new GameObject("Estanteria").transform;
        shelf.SetParent(level, false);
        CreateRealBlock("Lateral izquierdo", shelf, new Vector3(-3.8f, 8.65f, 30f), new Vector3(0.4f, 17.3f, 4f), home.DarkWood);
        CreateRealBlock("Lateral derecho", shelf, new Vector3(11.8f, 8.65f, 30.8f), new Vector3(0.4f, 17.3f, 2.4f), home.DarkWood);
        CreateRealBlock("Fondo", shelf, new Vector3(4f, 8.65f, 31.8f), new Vector3(16f, 17.3f, 0.4f), home.DarkWood);
        CreateRealBlock("Balda 1", shelf, new Vector3(6f, 6.2f, 30f), new Vector3(19.6f, 0.4f, 4f), home.Wood);
        CreateRealBlock("Balda 2", shelf, new Vector3(4f, 11.2f, 30f), new Vector3(15.2f, 0.4f, 4f), home.Wood);
        CreateRealBlock("Techo de la estanteria", shelf, new Vector3(4f, 16.7f, 30f), new Vector3(16f, 0.4f, 4f), home.Wood);
        CreateRealBlock("Escuadra de la repisa", shelf, new Vector3(15.6f, 5f, 30f), new Vector3(0.4f, 2.4f, 3f), home.DarkWood, false);
        CreateBookRow(shelf, -3.6f, 11.6f, 0f, 29.4f, 31.6f, 5.6f, home, rng);
        CreateBookRow(shelf, -3.6f, 11.4f, 6.4f, 29.6f, 31.6f, 4.4f, home, rng);
        CreateBookRow(shelf, -3.6f, 11.6f, 11.4f, 29.4f, 31.6f, 5f, home, rng);

        // Reloj de pared sobre la estantería y su péndulo, que barre el techo de la estantería.
        CreateRealBlock("Reloj", level, new Vector3(5f, 21.7f, 31.5f), new Vector3(5f, 4.4f, 1f), home.DarkWood, false);
        NoShadow(CreateVisualPart("Esfera", level, PrimitiveType.Cylinder, new Vector3(5f, 22f, 30.95f), new Vector3(3.4f, 0.05f, 3.4f), new Vector3(90f, 0f, 0f), home.Paper));
        GameObject pendulum = new GameObject("Pendulo del reloj");
        pendulum.transform.SetParent(level, false);
        pendulum.transform.position = new Vector3(5f, 23.5f, 30.3f);
        Transform arm = CreatePivot("Brazo", pendulum.transform, Vector3.zero);
        CreateVisualPart("Varilla", arm, PrimitiveType.Cube, new Vector3(0f, -2.8f, 0f), new Vector3(0.2f, 5.6f, 0.2f), Vector3.zero, home.Brass);
        CreateVisualPart("Disco", arm, PrimitiveType.Cylinder, new Vector3(0f, -5.6f, 0f), new Vector3(2.4f, 0.15f, 2.4f), new Vector3(90f, 0f, 0f), home.Brass);
        pendulum.AddComponent<SwingingPendulum>().Setup(arm, 5.6f, 55f, 2.6f);

        // 2. La regla: de pie en la mesa, cae hacia la balda 1.
        Vector3 rulerBase = new Vector3(-7.2f, 6.15f, 24.5f);
        Vector3 rulerDirection = new Vector3(6.2f, 0f, 5.5f).normalized;
        GameObject ruler = new GameObject("Regla");
        ruler.transform.position = rulerBase;
        ruler.transform.rotation = Quaternion.LookRotation(rulerDirection, Vector3.up);
        Transform rulerPivot = CreatePivot("Pivote", ruler.transform, Vector3.zero);
        GameObject rulerBody = CreateRealBlock("Cuerpo", rulerPivot, rulerBase, new Vector3(1.8f, 8.5f, 0.3f), home.Wood);
        rulerBody.transform.localPosition = new Vector3(0f, 4.25f, 0f);
        rulerBody.transform.localRotation = Quaternion.identity;
        for (int mark = 1; mark < 17; mark++)
        {
            NoShadow(CreateVisualPart("Marca", rulerBody.transform, PrimitiveType.Cube, new Vector3(-0.6f, -4.25f + mark * 0.5f, -0.16f), new Vector3(mark % 2 == 0 ? 0.5f : 0.25f, 0.05f, 0.02f), Vector3.zero, home.Black));
        }
        FallingTreeBridge rulerFall = ruler.AddComponent<FallingTreeBridge>();
        rulerFall.Setup(rulerPivot, 90f);
        SetEventId(rulerFall, "regla_2");

        // 1. Bloques de juguete hasta el asiento de la silla.
        GameObject blocks = CreateGrowingBlocks("Bloques de juguete", "bloques_1",
            new[] { new Vector3(-16f, 0f, -3.6f), new Vector3(-16f, 0f, -2f), new Vector3(-16f, 0f, -0.4f) },
            new[] { new Vector3(1.6f, 0.9f, 1.6f), new Vector3(1.6f, 1.8f, 1.6f), new Vector3(1.6f, 2.7f, 1.6f) },
            home.Toys);

        // 3. Escalera de juguete en el lateral derecho de la estantería: de la repisa (6.4) al techo (16.9).
        GameObject toyLadder = CreateClimb(level, "Escalera de juguete", new Vector3(12f, 6.4f, 30.8f), 10.5f, Vector3.right, true, real);
        GameObject toyLadderEvent = CreateActivator("Escalera de la estanteria", "escalera_3", toyLadder);

        // ================= CÓMODA, CAJONES Y TOCADISCOS =================
        CreateRealBlock("Comoda", level, new Vector3(24f, 3.6f, -15f), new Vector3(12f, 7.2f, 10f), home.Wood);
        float[] drawerOut = { 6f, 4.5f, 3f, 1.5f };
        GameObject[] drawerEvents = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            float bottom = 0.2f + i * 1.5f;
            GameObject drawer = CreateRealBlock("Cajon " + (i + 1), level, new Vector3(21.25f, bottom + 0.65f, -15f), new Vector3(6.5f, 1.3f, 9f), home.DarkWood);
            NoShadow(CreateVisualPart("Tirador", drawer.transform, PrimitiveType.Sphere, new Vector3(-0.51f, 0f, 0f), new Vector3(0.05f, 0.25f, 0.06f), Vector3.zero, home.Brass));
            drawerEvents[i] = CreateMoveEvent("Cajon que se abre " + (i + 1), "cajones_4", drawer.transform, Vector3.left * drawerOut[i], Vector3.zero, 1.2f + i * 0.3f);
        }

        CreateRealBlock("Caja del tocadiscos", level, new Vector3(25.5f, 7.5f, -14.5f), new Vector3(7f, 0.6f, 7f), home.DarkWood);
        GameObject platter = CreateVisualPart("Plato", level, PrimitiveType.Cylinder, new Vector3(25.5f, 7.95f, -14.5f), new Vector3(6f, 0.15f, 6f), Vector3.zero, home.Brass);
        MeshCollider platterCollider = platter.AddComponent<MeshCollider>();
        platterCollider.convex = true;
        Rigidbody platterBody = platter.AddComponent<Rigidbody>();
        platterBody.isKinematic = true;
        platter.AddComponent<SpinningPlatform>().Setup(30f);
        NoShadow(CreateVisualPart("Disco de vinilo", platter.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.95f, 0.1f, 0.95f), Vector3.zero, home.Black));
        NoShadow(CreateVisualPart("Etiqueta", platter.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 0.1f, 0.3f), Vector3.zero, home.Toys[0]));
        CreateVisualPart("Brazo del tocadiscos", level, PrimitiveType.Cube, new Vector3(28.4f, 8.6f, -15.5f), new Vector3(0.2f, 0.2f, 4.5f), new Vector3(0f, 18f, 0f), home.Brass);

        // Peonza que da vueltas alrededor del tocadiscos.
        GameObject top = new GameObject("Peonza");
        top.transform.SetParent(level, false);
        Transform spin = CreatePivot("Giro", top.transform, Vector3.zero);
        CreateVisualPart("Cuerpo", spin, PrimitiveType.Sphere, new Vector3(0f, 0.8f, 0f), new Vector3(1.4f, 0.9f, 1.4f), Vector3.zero, home.Toys[3]);
        CreateVisualPart("Punta", spin, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), new Vector3(45f, 0f, 45f), home.Brass);
        CreateVisualPart("Mango", spin, PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.18f, 0.3f, 0.18f), Vector3.zero, home.DarkWood);
        spin.gameObject.AddComponent<Spinner>();
        SerializedObject topSpin = new SerializedObject(spin.GetComponent<Spinner>());
        Property(topSpin, "axis").vector3Value = Vector3.up;
        Property(topSpin, "degreesPerSecond").floatValue = 720f;
        topSpin.ApplyModifiedPropertiesWithoutUndo();
        top.AddComponent<PathHazard>().Setup(new[] { new Vector3(19.4f, 7.2f, -19.4f), new Vector3(29.6f, 7.2f, -19.4f), new Vector3(29.6f, 7.2f, -10.6f), new Vector3(19.4f, 7.2f, -10.6f) }, true, 3.5f, 1f);

        // ================= CAMA =================
        CreateRealBlock("Somier", level, new Vector3(20f, 1.2f, 12f), new Vector3(16f, 2.4f, 24f), home.Wood);
        CreateRealBlock("Colchon", level, new Vector3(20f, 3.5f, 12f), new Vector3(15.6f, 2.2f, 23.6f), home.Quilt);
        CreateRealBlock("Cabecero", level, new Vector3(20f, 5f, 24.5f), new Vector3(16f, 10f, 1f), home.DarkWood);
        for (int i = 0; i < 2; i++)
        {
            GameObject pillow = CreateRealBlock("Almohada", level, new Vector3(16f + i * 8f, 5.2f, 22f), new Vector3(6.5f, 1.2f, 3f), home.Paper);
            pillow.transform.rotation = Quaternion.Euler(0f, (i == 0 ? -1f : 1f) * 4f, 0f);
        }
        GameObject bear = new GameObject("Osito");
        bear.transform.SetParent(level, false);
        bear.transform.position = new Vector3(26f, 4.6f, 19f);
        bear.transform.rotation = Quaternion.Euler(0f, -150f, 0f);
        Material fur = GetOrCreateTexturedMaterialCopy("CasaPeluche", real.Cloth, new Color(0.55f, 0.36f, 0.2f));
        CreateVisualPart("Cuerpo", bear.transform, PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0f), new Vector3(2.2f, 2.6f, 2f), Vector3.zero, fur);
        CreateVisualPart("Cabeza", bear.transform, PrimitiveType.Sphere, new Vector3(0f, 3.2f, 0.1f), Vector3.one * 1.8f, Vector3.zero, fur);
        CreateVisualPart("Hocico", bear.transform, PrimitiveType.Sphere, new Vector3(0f, 3f, 0.9f), new Vector3(0.7f, 0.55f, 0.5f), Vector3.zero, home.Paper);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.65f : 0.65f;
            CreateVisualPart("Oreja", bear.transform, PrimitiveType.Sphere, new Vector3(x, 4f, 0f), new Vector3(0.6f, 0.6f, 0.3f), Vector3.zero, fur);
            CreateVisualPart("Ojo", bear.transform, PrimitiveType.Sphere, new Vector3(x * 0.5f, 3.4f, 0.85f), Vector3.one * 0.2f, Vector3.zero, home.Black);
            CreateVisualPart("Pata", bear.transform, PrimitiveType.Sphere, new Vector3(x * 1.2f, 0.4f, 0.6f), new Vector3(0.8f, 0.7f, 1f), Vector3.zero, fur);
        }

        // 6. Cojines al pie de la cama: tres alturas (1.2, 2.4, 3.6) hasta el colchón (4.6).
        GameObject cushions = CreateGrowingBlocks("Cojines", "cojines_6",
            new[] { new Vector3(20f, 0f, -6.5f), new Vector3(20f, 0f, -4.3f), new Vector3(20f, 0f, -2.1f) },
            new[] { new Vector3(4f, 1.2f, 2.2f), new Vector3(4f, 2.4f, 2.2f), new Vector3(4f, 3.6f, 2.2f) },
            new[] { home.Cushion, home.Toys[1], home.Cushion });

        // 5. Lámpara de la mesa: se enciende con su fragmento (o con un Pulso). Es zona segura.
        GameObject lamp = new GameObject("Lampara de la mesa");
        lamp.transform.SetParent(level, false);
        lamp.transform.position = new Vector3(-19f, 6f, 23.5f);
        CreateVisualPart("Pie", lamp.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(2f, 0.15f, 2f), Vector3.zero, home.Brass);
        CreateVisualPart("Columna", lamp.transform, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.3f, 2f, 0.3f), Vector3.zero, home.Brass);
        CreateVisualPart("Pantalla", lamp.transform, PrimitiveType.Cylinder, new Vector3(0f, 4.6f, 0f), new Vector3(3.2f, 1f, 3.2f), Vector3.zero, curtain);
        lamp.AddComponent<CapsuleCollider>().center = Vector3.up * 2f;
        GameObject lampLit = new GameObject("Encendido");
        lampLit.transform.SetParent(lamp.transform, false);
        NoShadow(CreateVisualPart("Bombilla", lampLit.transform, PrimitiveType.Sphere, new Vector3(0f, 4.1f, 0f), Vector3.one * 0.9f, Vector3.zero, real.Flame));
        GameObject lampLightObject = new GameObject("Luz");
        lampLightObject.transform.SetParent(lampLit.transform, false);
        lampLightObject.transform.localPosition = new Vector3(0f, 3.8f, 0f);
        Light lampLight = lampLightObject.AddComponent<Light>();
        lampLight.type = LightType.Point;
        lampLight.color = new Color(1f, 0.75f, 0.45f);
        lampLight.range = 26f;
        lampLight.intensity = 6f;
        lampLight.shadows = LightShadows.Soft;
        Beacon lampBeacon = lamp.AddComponent<Beacon>();
        lampBeacon.Setup(lampLit, 6f, false, "lampara_5");
        GameObject lampEvent = CreateActivator("Lampara que se enciende", "lampara_5", new GameObject("Destello de la lampara"));
        lampEvent.transform.position = lamp.transform.position + Vector3.up * 4f;

        // 7. Núcleo y portal sobre la cama, a la luz de la ventana.
        GameObject core = CreateCoreAndPortal(materials, CoreOffset(new Vector3(20f, 4.6f, 10f)));

        ConfigureEvent(blocks, blocks.transform.GetChild(2), real.Flame);
        ConfigureEvent(ruler, rulerBody.transform, real.Flame);
        ConfigureEvent(toyLadderEvent, toyLadder.transform, real.Flame);
        ConfigureEvent(drawerEvents[0], drawerEvents[0].transform, real.Flame);
        ConfigureEvent(lampEvent, lamp.transform, real.Flame);
        ConfigureEvent(cushions, cushions.transform.GetChild(1), real.Flame);
        ConfigureEvent(core, core.transform.Find("Nucleo"), materials.Core);

        // ================= LAMPARITAS (refugio y reaparición) =================
        CreateBeacon(level, "Lamparita de la puerta", new Vector3(4f, 0f, -21.5f), 5f, true, "", true, real, materials);
        CreateBeacon(level, "Lamparita de la repisa", new Vector3(15.2f, 6.4f, 28.6f), 3f, false, "", true, real, materials);
        CreateBeacon(level, "Lamparita del suelo", new Vector3(8f, 0f, -16f), 5f, false, "", true, real, materials);
        CreateBeacon(level, "Lamparita de la comoda", new Vector3(19f, 7.2f, -19.3f), 3f, false, "", true, real, materials);

        // ================= OTROS MUEBLES Y JUGUETES =================
        CreateRealBlock("Armario", level, new Vector3(-28.5f, 10f, -10f), new Vector3(7f, 20f, 14f), home.DarkWood);
        CreateRealBlock("Puerta del armario", level, new Vector3(-24.95f, 10f, -10f), new Vector3(0.1f, 18f, 0.3f), home.Brass, false);
        CreateRealBlock("Baul de juguetes", level, new Vector3(-26f, 2.2f, 14f), new Vector3(8f, 4.4f, 6f), home.Toys[1]);
        GameObject ball = CreateVisualPart("Pelota", level, PrimitiveType.Sphere, new Vector3(-8f, 2.2f, -18f), Vector3.one * 4.4f, Vector3.zero, home.Toys[0]);
        ball.AddComponent<SphereCollider>();
        GameObject airplane = CreateVisualPart("Avion de papel", level, PrimitiveType.Cube, new Vector3(6f, 0.3f, 10f), new Vector3(4f, 0.05f, 6f), new Vector3(0f, 35f, 8f), home.Paper);
        NoShadow(airplane);
        CreateRealBlock("Cuadro", level, new Vector3(-31.9f, 15f, 12f), new Vector3(0.3f, 7f, 9f), home.DarkWood, false);
        NoShadow(CreateRealBlock("Lienzo", level, new Vector3(-31.7f, 15f, 12f), new Vector3(0.1f, 6f, 8f), GetOrCreateMaterial("CasaLienzo", new Color(0.45f, 0.6f, 0.75f), null, 0f, 0.2f), false));
        CreateVisualPart("Lampara del techo", level, PrimitiveType.Sphere, new Vector3(0f, 22.5f, 4f), new Vector3(4f, 2.4f, 4f), Vector3.zero, curtain);
        CreateZoneLight(level, new Vector3(0f, 20f, 4f), new Color(1f, 0.85f, 0.65f), 40f, 2.5f);

        // ================= CAMINO (para las pruebas) =================
        CreateFlatPathMarkers(level, new[]
        {
            new[] { new Vector3(0f, 0f, -21f), new Vector3(-8f, 0f, -14f), new Vector3(-16f, 0f, -5.2f) },
            new[] { new Vector3(-16f, 3.6f, 3f), new Vector3(-16f, 3.6f, 7.2f) },
            new[] { new Vector3(-16f, 6f, 14.6f), new Vector3(-10f, 6f, 20f), new Vector3(-7.6f, 6f, 23.5f) },
            new[] { new Vector3(-2f, 6.4f, 28.7f), new Vector3(11f, 6.4f, 28.7f), new Vector3(14f, 6.4f, 29.6f) },
            new[] { new Vector3(11f, 16.9f, 29.4f), new Vector3(-2f, 16.9f, 29.4f) },
            new[] { new Vector3(6f, 0f, 20f), new Vector3(6f, 0f, -14f), new Vector3(10.8f, 0f, -14f) },
            new[] { new Vector3(18.6f, 7.2f, -12f), new Vector3(21f, 7.2f, -11.2f) },
            new[] { new Vector3(14f, 0f, -8.5f), new Vector3(19f, 0f, -8.5f) },
            new[] { new Vector3(14f, 4.6f, 1f), new Vector3(14f, 4.6f, 8f) },
        });
    }
}
