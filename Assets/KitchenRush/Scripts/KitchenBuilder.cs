#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// ==========================================================
// KitchenBuilder v3: يبني مطبخ المرحلة ١ بضغطة وحدة، مع الطلبات والنقاط والتقطيع والتسليم
// Tools > Kitchen Rush > Build Level 1 Kitchen
//
// - يدور في المشروع على المجسمات اللي نزلتيها (Low Poly Kitchen و Food Kit)
//   ويستخدمها: طاولات، مغسلة، زبالة، لوح تقطيع، سكين، صحون، والأكل نفسه
// - اللي ما يلقاه يسويه بأشكال بسيطة بدالها
// - جدار بلاط، أرضية مربعات، رف فيه أغراض، لمبات معلقة دافية
// - ألوان وإضاءة وظلال وتوهج خفيف (Post Processing)
// تقدرين تضغطينه أكثر من مرة، يمسح القديم ويبني من جديد. Ctrl+Z يرجعه.
// ==========================================================
public static class KitchenBuilder
{
    const string Root = "Assets/KitchenRush";
    const string MatFolder = "Assets/KitchenRush/Materials";
    const float CounterH = 0.9f;

    // لو الطاولات طلعت وجهها للجدار غيري هذا الرقم إلى 180
    const float ModelFrontOffset = 0f;

    static readonly string[] Level1 =
    {
        "## L  T  ## CH CH ## PL ## ##",
        ".. .. .. .. .. .. .. .. .. ..",
        ".. .. .. .. CF .. .. .. .. ..",
        ".. .. .. .. .. .. .. .. .. ..",
        "## ## B  C  ## ## ## ## TR ##",
    };

    [MenuItem("Tools/Kitchen Rush/Build Level 1 Kitchen %#k")]
    static void BuildLevel1() { serveAtTable = true; outdoor = false; Build("Kitchen_Level1", Level1); }

    // المرحلة ٢: حفلة في الحوش، فيها شواية (GR) للحم
    static readonly string[] Level2 =
    {
        "## M  B  ## GR GR ## PL ## ##",
        ".. .. .. .. .. .. .. .. .. ..",
        ".. .. .. .. CF .. .. .. .. ..",
        ".. .. .. .. .. .. .. .. .. ..",
        "## L  T  C  CH CH ## ## TR ##",
    };

    // المرحلة ١ (بيت نينا): التسليم يكون للأم على طاولة الأكل
    static bool serveAtTable;
    // المرحلة ٢: برا البيت (عشب وسور وأضواء)
    static bool outdoor;

    // ---------- المشاهد: القائمة + المرحلة ٢ + Build Settings ----------
    const string SceneFolder = "Assets/Scenes";

    // يسوي مشهد القائمة ومشهد المرحلة ٢، ويرتب Build Settings (القائمة، ١، ٢)
    // مشهد المرحلة ١ حقك ما يتغير
    [MenuItem("Tools/Kitchen Rush/Build Whole Game (Menu, Intro, Levels) %#g")]
    static void BuildGame()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string back = SceneManager.GetActiveScene().path;
        string l1 = FindScene("level1");
        if (l1 != null)
        {
            EditorSceneManager.OpenScene(l1, OpenSceneMode.Single);
            BuildLevel1();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        BuildLevel2Scene();
        BuildIntroScene();
        BuildMenuScene();
        SetupBuildSettings();
        if (!string.IsNullOrEmpty(back) && File.Exists(back)) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
        Debug.Log("Kitchen Rush: MainMenu + Intro + level1 + level2 built. Build Settings: MainMenu, Intro, level1, level2. Press Play in MainMenu to test.");
    }

    [MenuItem("Tools/Kitchen Rush/Build Level 2 Backyard")]
    static void BuildLevel2Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildLevel2Scene();
        SetupBuildSettings();
    }

    static void BuildLevel2Scene()
    {
        OpenOrNewScene("level2");
        serveAtTable = true;
        outdoor = true;
        Build("Backyard_Level2", Level2);
        outdoor = false;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static Scene OpenOrNewScene(string name)
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
        string path = SceneFolder + "/" + name + ".unity";
        if (File.Exists(path)) return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(sc, path);
        return sc;
    }

    static string FindScene(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene " + name))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).ToLower() == name.ToLower()) return p;
        }
        return null;
    }

    static void SetupBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>();
        foreach (var n in new[] { "MainMenu", "Intro", "level1", "level2" })
        {
            string p = FindScene(n);
            if (p != null) list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ---------- البداية: نينا في الصالة تشرب قهوة وترسم مطعمها، وأمها تناديها ----------
    static void BuildIntroScene()
    {
        OpenOrNewScene("Intro");
        ConvertPinkMaterials();
        FindAllModels();
        report.Clear();
        foreach (var nm in new[] { "Intro_Set", "Chef" })
        {
            var old = GameObject.Find(nm);
            if (old != null) Object.DestroyImmediate(old);
        }
        var root = new GameObject("Intro_Set");
        var t = root.transform;

        // الغرفة
        Block(root, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(9f, 0.2f, 7f), TexMat("Floor_Wood_Intro", "FloorWood", WoodTex(), new Vector2(2.5f, 3.5f)), true);
        var paint = Mat("Living_Wall", new Color(0.72f, 0.82f, 0.78f));
        Block(root, "Wall_Back", new Vector3(0f, 1.5f, 2.3f), new Vector3(9f, 3f, 0.2f), paint, false);
        Block(root, "Wall_Left", new Vector3(-3.6f, 1.5f, 0f), new Vector3(0.2f, 3f, 7f), paint, false);
        Block(root, "Wall_Right", new Vector3(3.6f, 1.5f, 0f), new Vector3(0.2f, 3f, 7f), paint, false);
        var trim = Mat("Wall_Trim", new Color(0.9f, 0.86f, 0.78f));
        Block(root, "Skirting", new Vector3(0f, 0.06f, 2.19f), new Vector3(9f, 0.12f, 0.04f), trim, false);
        BuildWindow(root, new Vector3(-1.6f, 1.2f, 2.2f));
        // باب المطبخ (منوّر) يمين
        Block(root, "Kitchen_Door_Glow", new Vector3(2.5f, 1.05f, 2.19f), new Vector3(1.0f, 2.1f, 0.02f), MatGlow("Kitchen_Glow", new Color(1f, 0.82f, 0.55f), 1.4f), false);
        Block(root, "Door_Frame_L", new Vector3(1.97f, 1.05f, 2.17f), new Vector3(0.07f, 2.15f, 0.07f), trim, false);
        Block(root, "Door_Frame_R", new Vector3(3.03f, 1.05f, 2.17f), new Vector3(0.07f, 2.15f, 0.07f), trim, false);
        Block(root, "Door_Frame_T", new Vector3(2.5f, 2.13f, 2.17f), new Vector3(1.13f, 0.07f, 0.07f), trim, false);
        var dl = new GameObject("Kitchen_Light").AddComponent<Light>();
        dl.transform.SetParent(t); dl.transform.position = new Vector3(2.5f, 1.4f, 1.8f);
        dl.type = LightType.Point; dl.color = new Color(1f, 0.8f, 0.55f); dl.range = 3.5f; dl.intensity = 2f;

        // لوحات ومراية على الجدار
        ArtFrame(t, new Vector3(0.0f, 1.75f, 2.19f), 0f, 0);
        ArtFrame(t, new Vector3(0.75f, 1.6f, 2.19f), 0f, 2);
        WavyMirror(t, new Vector3(3.49f, 1.6f, 0.3f));

        // سجادة
        var rug = Part(root, PrimitiveType.Cylinder, new Vector3(0.2f, 0.005f, 0.6f), new Vector3(3.2f, 0.005f, 2.3f), TexMat("Rug_Living", "RugChecker", CheckerSmallTex(new Color(0.95f, 0.7f, 0.6f)), new Vector2(6f, 5f)));
        rug.name = "Rug";

        // كنبة زرقا
        var sofa = new GameObject("Sofa");
        sofa.transform.SetParent(t);
        sofa.transform.position = new Vector3(0.2f, 0f, 1.45f);
        var blue = MatS("Sofa_Blue", DustyBlue, 0.15f);
        var blueD = MatS("Sofa_BlueDark", new Color(0.45f, 0.6f, 0.76f), 0.15f);
        Part(sofa, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(2.2f, 0.36f, 0.85f), blueD);
        Part(sofa, PrimitiveType.Cube, new Vector3(-0.52f, 0.44f, -0.04f), new Vector3(1.02f, 0.1f, 0.74f), blue);
        Part(sofa, PrimitiveType.Cube, new Vector3(0.52f, 0.44f, -0.04f), new Vector3(1.02f, 0.1f, 0.74f), blue);
        Part(sofa, PrimitiveType.Cube, new Vector3(0f, 0.72f, 0.36f), new Vector3(2.2f, 0.62f, 0.18f), blueD);
        Part(sofa, PrimitiveType.Cube, new Vector3(-1.1f, 0.5f, 0f), new Vector3(0.2f, 0.5f, 0.85f), blueD);
        Part(sofa, PrimitiveType.Cube, new Vector3(1.1f, 0.5f, 0f), new Vector3(0.2f, 0.5f, 0.85f), blueD);
        var pillow = Part(sofa, PrimitiveType.Cube, new Vector3(0.82f, 0.62f, 0.22f), new Vector3(0.36f, 0.32f, 0.12f), Mat("Pillow_Pink", new Color(1f, 0.62f, 0.7f)));
        pillow.transform.localRotation = Quaternion.Euler(-12f, -15f, 8f);
        var pillow2 = Part(sofa, PrimitiveType.Cube, new Vector3(-0.85f, 0.62f, 0.22f), new Vector3(0.36f, 0.32f, 0.12f), TexMat("Pillow_Floral", "Floral_0", FloralTex(0), Vector2.one));
        pillow2.transform.localRotation = Quaternion.Euler(-12f, 12f, -6f);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                Part(sofa, PrimitiveType.Cylinder, new Vector3(a * 1.0f, 0.02f, b * 0.33f), new Vector3(0.06f, 0.02f, 0.06f), Mat("Wood_Dark", new Color(0.4f, 0.28f, 0.18f)));

        // طاولة قهوة عليها كراسة الرسم وقلم
        var table = new GameObject("CoffeeTable");
        table.transform.SetParent(t);
        table.transform.position = new Vector3(0.2f, 0f, 0.35f);
        var wood = MatS("Table_Wood", new Color(0.86f, 0.72f, 0.5f), 0.4f);
        Part(table, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.05f, 0.62f), wood);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                Part(table, PrimitiveType.Cylinder, new Vector3(a * 0.52f, 0.19f, b * 0.24f), new Vector3(0.05f, 0.19f, 0.05f), wood);
        var book = new GameObject("Sketchbook");
        book.transform.SetParent(t);
        book.transform.position = new Vector3(0.05f, 0.425f, 0.32f);
        book.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
        Part(book, PrimitiveType.Cube, new Vector3(0f, 0.008f, 0f), new Vector3(0.46f, 0.016f, 0.34f), Mat("Book_Cover", new Color(0.32f, 0.45f, 0.62f)));
        var page = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(page.GetComponent<Collider>());
        page.name = "Drawing";
        page.transform.SetParent(book.transform, false);
        page.transform.localPosition = new Vector3(0f, 0.0175f, 0f);
        page.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        page.transform.localScale = new Vector3(0.44f, 0.32f, 1f);
        var drawMat = TexMat("Drawing_Restaurant", "RestaurantSketch", RestaurantSketchTex(), Vector2.one);
        if (drawMat.HasProperty("_Smoothness")) drawMat.SetFloat("_Smoothness", 0f);
        page.GetComponent<Renderer>().sharedMaterial = drawMat;
        var pencil = Part(book, PrimitiveType.Cylinder, new Vector3(0.3f, 0.02f, 0.05f), new Vector3(0.012f, 0.09f, 0.012f), Mat("Pencil", new Color(1f, 0.8f, 0.25f)));
        pencil.transform.localRotation = Quaternion.Euler(90f, 30f, 0f);

        // كرسي الجدة يسار، لمبة وكتب ونبتة يمين
        var arm = new GameObject("Armchair");
        arm.transform.SetParent(t);
        arm.transform.position = new Vector3(-1.85f, 0f, 0.75f);
        arm.transform.rotation = Quaternion.Euler(0f, 125f, 0f);
        var mustard = MatS("Armchair_Mustard", new Color(0.93f, 0.72f, 0.33f), 0.15f);
        Part(arm, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.85f, 0.4f, 0.8f), mustard);
        Part(arm, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.34f), new Vector3(0.85f, 0.65f, 0.14f), mustard);
        Part(arm, PrimitiveType.Cube, new Vector3(-0.38f, 0.55f, 0f), new Vector3(0.12f, 0.3f, 0.8f), mustard);
        Part(arm, PrimitiveType.Cube, new Vector3(0.38f, 0.55f, 0f), new Vector3(0.12f, 0.3f, 0.8f), mustard);
        PlaceGuest(arm.transform, "granny", arm.transform.position + arm.transform.forward * -0.02f, 125f + 180f, 1.58f);

        var lamp = new GameObject("FloorLamp");
        lamp.transform.SetParent(t);
        lamp.transform.position = new Vector3(-1.2f, 0f, 1.85f);
        Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.02f, 0.3f), Mat("Appliance_Dark", new Color(0.2f, 0.2f, 0.2f)));
        Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(0.025f, 0.75f, 0.025f), MatS("Brass", new Color(0.85f, 0.68f, 0.3f), 0.8f));
        Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 1.55f, 0f), new Vector3(0.42f, 0.16f, 0.42f), MatGlow("Lamp_Shade", new Color(1f, 0.9f, 0.6f), 0.8f));
        var ll = new GameObject("Lamp_Light").AddComponent<Light>();
        ll.transform.SetParent(lamp.transform, false);
        ll.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        ll.type = LightType.Point; ll.color = new Color(1f, 0.8f, 0.55f); ll.range = 4f; ll.intensity = 2.2f;
        BigPlant(t, new Vector3(1.62f, 0f, 1.85f), 3);
        BigPlant(t, new Vector3(-2.9f, 0f, 1.7f), 1);
        CutePlant(t, new Vector3(-1.6f, 1.21f, 2.1f), 0.3f, 5);

        // نينا جالسة على الكنبة، وفي يدها كوب قهوة
        BuildChef(new Vector3(0.0f, 0f, 1.43f));
        var chef = GameObject.Find("Chef");
        if (chef != null)
        {
            ApplyCharacter(chef);
            chef.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cc = chef.GetComponent<ChefController>(); if (cc != null) Object.DestroyImmediate(cc);
            var ch = chef.GetComponent<CharacterController>(); if (ch != null) Object.DestroyImmediate(ch);
            var sk = chef.GetComponent<ChefSkin>(); if (sk != null) { sk.showName = false; sk.accessories = false; }
            var an = chef.GetComponentInChildren<Animator>();
            var sitCtrl = SitController();
            if (an != null && sitCtrl != null) an.runtimeAnimatorController = sitCtrl;
            if (an != null && an.isHuman)
            {
                var hand = an.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null)
                {
                    var mug = new GameObject("Mug");
                    mug.transform.position = hand.position + chef.transform.forward * 0.03f;
                    mug.transform.rotation = chef.transform.rotation;
                    Part(mug, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.075f, 0.05f, 0.075f), MatS("Mug_Pink", new Color(1f, 0.6f, 0.7f), 0.7f));
                    Part(mug, PrimitiveType.Cylinder, new Vector3(0f, 0.048f, 0f), new Vector3(0.062f, 0.003f, 0.062f), Mat("Coffee", new Color(0.3f, 0.18f, 0.1f)));
                    var handle = Part(mug, PrimitiveType.Cube, new Vector3(0.045f, 0f, 0f), new Vector3(0.015f, 0.05f, 0.03f), MatS("Mug_Pink", new Color(1f, 0.6f, 0.7f), 0.7f));
                    handle.name = "Handle";
                    mug.transform.SetParent(hand, true);
                }
            }
        }

        // الكاميرا والإضاءة
        var cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        var oldFollow = cam.GetComponent<KitchenCamera>();
        if (oldFollow != null) Object.DestroyImmediate(oldFollow);
        cam.transform.position = new Vector3(0.2f, 1.7f, -3.2f);
        cam.transform.LookAt(new Vector3(0.1f, 0.8f, 1.2f));
        cam.fieldOfView = 45f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.08f, 0.07f);
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null) { data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
        SetupLights(root, 6, 4);
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.intensity = 1.3f; l.transform.rotation = Quaternion.Euler(35f, 200f, 0f); }
        SetupPost(root);

        var cut = new GameObject("Intro Cutscene").AddComponent<IntroCutscene>();
        cut.transform.SetParent(t);
        cut.nina = chef != null ? chef.transform : null;
        cut.sketch = book.transform;
        cut.door = new Vector3(2.5f, 1.2f, 2.2f);
        var gran = GameObject.Find("Guest_granny");
        cut.granny = gran != null ? gran.transform : arm.transform;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Kitchen Rush: Intro built.\n" + string.Join("\n", report));
    }

    // رسمة المطعم في كراسة نينا (قلم رصاص + ألوان خفيفة)
    static Texture2D RestaurantSketchTex()
    {
        int W = 512, H = 372;
        var tex = new Texture2D(W, H);
        var paper = new Color(0.99f, 0.97f, 0.92f);
        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = paper;
        System.Action<int, int, Color> dot = (x, y, c) => { if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = c; };
        System.Action<float, float, float, float, Color, int> line = (x0, y0, x1, y1, c, th) =>
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)));
            for (int i = 0; i <= steps; i++)
            {
                float k = steps == 0 ? 0f : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, k)), y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, k));
                for (int a = -th; a <= th; a++) for (int b = -th; b <= th; b++) dot(x + a, y + b, c);
            }
        };
        System.Action<int, int, int, int, Color> fill = (x0, y0, x1, y1, c) =>
        {
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) dot(x, y, Color.Lerp(px[Mathf.Clamp(y, 0, H - 1) * W + Mathf.Clamp(x, 0, W - 1)], c, 0.55f));
        };
        var pen = new Color(0.25f, 0.25f, 0.3f);
        var pink = new Color(1f, 0.55f, 0.65f);
        var blue = new Color(0.5f, 0.7f, 0.9f);
        var warm = new Color(1f, 0.85f, 0.45f);
        // خطوط الكراسة
        for (int y = 30; y < H; y += 26) line(0, y, W, y, new Color(0.85f, 0.9f, 0.97f), 0);
        // المبنى
        fill(110, 60, 400, 250, new Color(0.98f, 0.92f, 0.82f));
        line(110, 60, 110, 250, pen, 1); line(400, 60, 400, 250, pen, 1); line(110, 250, 400, 250, pen, 1); line(105, 60, 405, 60, pen, 1);
        // مظلة مخططة
        for (int i = 0; i < 10; i++)
        {
            int x0 = 100 + i * 31;
            fill(x0, 250, x0 + 31, 285, i % 2 == 0 ? pink : Color.white);
            line(x0, 285, x0 + 15, 295, pen, 0); line(x0 + 15, 295, x0 + 31, 285, pen, 0);
        }
        line(100, 250, 410, 250, pen, 1); line(100, 285, 410, 285, pen, 0);
        // اللوحة
        fill(170, 296, 340, 340, blue);
        line(170, 296, 340, 296, pen, 1); line(170, 340, 340, 340, pen, 1); line(170, 296, 170, 340, pen, 1); line(340, 296, 340, 340, pen, 1);
        for (int i = 0; i < 6; i++) line(190 + i * 25, 310, 202 + i * 25, 328, Color.white, 1);
        // الباب والشبابيك
        fill(225, 60, 285, 175, new Color(0.85f, 0.55f, 0.4f));
        line(225, 60, 225, 175, pen, 1); line(285, 60, 285, 175, pen, 1); line(225, 175, 285, 175, pen, 1);
        foreach (int wx in new[] { 135, 310 })
        {
            fill(wx, 120, wx + 65, 200, warm);
            line(wx, 120, wx + 65, 120, pen, 1); line(wx, 200, wx + 65, 200, pen, 1); line(wx, 120, wx, 200, pen, 1); line(wx + 65, 120, wx + 65, 200, pen, 1);
            line(wx + 32, 120, wx + 32, 200, pen, 0); line(wx, 160, wx + 65, 160, pen, 0);
        }
        // نباتات وطاولة برا
        for (int i = 0; i < 2; i++)
        {
            int cx = i == 0 ? 80 : 430;
            fill(cx - 14, 60, cx + 14, 88, new Color(0.9f, 0.55f, 0.35f));
            for (int a = 0; a < 360; a += 4) dot(cx + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * 24), 112 + Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * 22), new Color(0.35f, 0.65f, 0.35f));
            line(cx, 88, cx, 100, pen, 0);
        }
        // قلوب ونجوم صغيرة
        foreach (var hpos in new[] { new Vector2(60, 300), new Vector2(450, 320), new Vector2(470, 200) })
        {
            for (int a = 0; a < 360; a += 3)
            {
                float r = a * Mathf.Deg2Rad;
                float hx = 16f * Mathf.Pow(Mathf.Sin(r), 3), hy = 13f * Mathf.Cos(r) - 5f * Mathf.Cos(2 * r) - 2f * Mathf.Cos(3 * r) - Mathf.Cos(4 * r);
                dot(Mathf.RoundToInt(hpos.x + hx * 0.7f), Mathf.RoundToInt(hpos.y + hy * 0.7f), pink);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // القائمة: خلفية دافية ونينا واقفة يمين، والواجهة تنبني لحالها (MainMenu.cs)
    static void BuildMenuScene()
    {
        OpenOrNewScene("MainMenu");
        ConvertPinkMaterials();
        FindAllModels();
        var old = GameObject.Find("Menu_Set");
        if (old != null) Object.DestroyImmediate(old);
        var oldChef = GameObject.Find("Chef");
        if (oldChef != null) Object.DestroyImmediate(oldChef);
        var root = new GameObject("Menu_Set");

        // أرضية خشب وجدار بلاط ورف عليه أغراض (نفس جو المطبخ)
        var floor = Block(root, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(14f, 0.2f, 8f), TexMat("Floor_Wood_Menu", "FloorWood", WoodTex(), new Vector2(3.5f, 4f)), false);
        Block(root, "Wall", new Vector3(0f, 2f, 2.2f), new Vector3(14f, 4.2f, 0.2f), Mat("Wall_Paint", Cream), false);
        var tiles = Block(root, "Backsplash_Menu", new Vector3(0f, 1.35f, 2.09f), new Vector3(14f, 0.9f, 0.02f), TexMat("Backsplash", "PatchworkTiles_v2", PatchworkTex(), Vector2.one), false);
        SetTiling(tiles, new Vector2(7f, 1f));
        for (int i = 0; i < 6; i++)
        {
            float x = -1.2f + i * 0.85f;
            var t = Tile(root, "Counter_Decor", new Vector3(x, 0f, 1.6f));
            CounterBody(t, 0f, i % 2 == 0 ? CabinetWhite : DustyBlue, TopGrey);
            Object.DestroyImmediate(t.GetComponent<Collider>());
            Vector3 top = new Vector3(x, CounterH, 1.6f);
            if (i == 1) Kettle(t.transform, top);
            if (i == 3) CutePlant(t.transform, top, 0.45f, i);
            if (i == 4) Toaster(t.transform, top);
        }
        Block(root, "Shelf", new Vector3(1.2f, 2.35f, 1.95f), new Vector3(3.2f, 0.05f, 0.3f), Mat("Shelf_Wood", ShelfWood), false);
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = new Vector3(-0.1f + i * 0.65f, 2.375f, 1.95f);
            if (i % 2 == 0) DutchOven(root.transform, p, i); else CutePlant(root.transform, p, 0.4f, i);
        }
        BigPlant(root.transform, new Vector3(3.6f, 0f, 1.2f), 2);

        // نينا
        BuildChef(new Vector3(1.6f, 0.02f, 0f));
        var chef = GameObject.Find("Chef");
        if (chef != null)
        {
            ApplyCharacter(chef);
            chef.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
            var cc = chef.GetComponent<ChefController>(); if (cc != null) Object.DestroyImmediate(cc);
            var ch = chef.GetComponent<CharacterController>(); if (ch != null) Object.DestroyImmediate(ch);
            var sk = chef.GetComponent<ChefSkin>(); if (sk != null) sk.showName = false;
        }

        // الكاميرا والإضاءة
        var cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        var oldFollow = cam.GetComponent<KitchenCamera>();
        if (oldFollow != null) Object.DestroyImmediate(oldFollow);
        cam.transform.position = new Vector3(0.4f, 1.55f, -3.6f);
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        cam.fieldOfView = 42f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.12f, 0.1f);
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null) { data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
        SetupLights(root, 6, 4);
        SetupPost(root);

        var menu = new GameObject("Main Menu").AddComponent<MainMenu>();
        menu.transform.SetParent(root.transform);
        if (chef != null) menu.preview = chef.transform;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    // ---------- المجسمات اللي لقيناها ----------
    static GameObject mCounter, mSink, mTrash, mBoard, mKnife, mPlate, mPot, mPan, mFridge;
    static GameObject mPlantBig, mPlantSmall, mRug, mShelf, mLamp, mCoffee, mToaster, mStool;
    static readonly Dictionary<Ingredient, GameObject> mFood = new Dictionary<Ingredient, GameObject>();
    static List<string> modelPaths;
    static readonly List<string> report = new List<string>();

    static void FindAllModels()
    {
        modelPaths = new List<string>();
        foreach (var t in new[] { "t:Prefab", "t:Model" })
            foreach (var guid in AssetDatabase.FindAssets(t, new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.StartsWith(MatFolder)) continue;
                if (!modelPaths.Contains(p)) modelPaths.Add(p);
            }
        report.Clear();

        mCounter = Find("Counter", new[] { "kitchencabinet", "kitchen-cabinet", "cabinet", "cupboard", "drawer" }, new[] { "wall", "corner", "upper", "hanging", "top", "door", "sink", "stove", "oven", "glass", "tv", "bed", "bath" });
        mSink = Find("Sink", new[] { "kitchen-sink", "sink", "kitchencounter" }, new[] { "bath", "toilet" });
        mTrash = Find("Trash", new[] { "trash-can", "trashcan", "garbage", "trash", "bin" }, new[] { "cabinet", "bag" });
        mBoard = Find("Cutting board", new[] { "cutting-board", "chopping-board", "cuttingboard", "board" }, new[] { "skate", "sign", "black", "cupboard" });
        mKnife = Find("Knife", new[] { "knife", "cleaver" }, new[] { "fork", "block" });
        mPlate = Find("Plate", new[] { "plate", "dish" }, new[] { "broken", "rack", "washer", "dishes", "full" });
        mPot = Find("Pot", new[] { "pot", "cooking-pot", "saucepan" }, new[] { "potato", "plant", "flower", "lid", "teapot" });
        mPan = Find("Pan", new[] { "frying-pan", "pan" }, new[] { "pancake", "panel", "dustpan" });
        mFridge = Find("Fridge", new[] { "kitchenfridgelarge", "kitchenfridge", "fridge", "refrigerator" }, new[] { "small", "built" });
        mPlantBig = Find("Big plant", new[] { "pottedplant", "plant-large", "plantlarge", "potted-plant", "plant" }, new[] { "small", "seed", "egg" });
        mPlantSmall = Find("Small plant", new[] { "plantsmall1", "plantsmall", "plant-small", "cactus", "succulent" }, null);
        mRug = Find("Rug", new[] { "rugrounded", "ruground", "rug" }, null);
        mShelf = Find("Shelf", new[] { "bookcaseopen", "bookcase", "shelf" }, new[] { "low", "books" });
        mLamp = Find("Floor lamp", new[] { "lamproundfloor", "lampsquarefloor", "floorlamp", "lamp" }, new[] { "wall", "table", "ceiling" });
        mCoffee = Find("Coffee machine", new[] { "kitchencoffeemachine", "coffeemachine", "coffee" }, new[] { "table", "cup", "mug" });
        mToaster = Find("Toaster", new[] { "toaster", "kitchenblender", "blender", "kitchenmicrowave", "microwave" }, null);
        mStool = Find("Stool", new[] { "stoolbar", "stool" }, null);

        mFood.Clear();
        mFood[Ingredient.Lettuce] = Find("Lettuce", new[] { "lettuce", "cabbage", "salad-leaf", "salad" }, new[] { "bowl", "plate", "slice", "cut" });
        mFood[Ingredient.Tomato] = Find("Tomato", new[] { "tomato" }, new[] { "slice", "sauce", "soup", "ketchup", "cut", "half" });
        mFood[Ingredient.Meat] = Find("Meat", new[] { "meat-patty", "patty", "meat-raw", "meat", "steak" }, new[] { "burger", "sandwich", "ball", "cooked" });
        mFood[Ingredient.Bun] = Find("Bun", new[] { "bun", "burger-bun", "bread" }, new[] { "bunny", "slice", "bottom", "top" });
        mFood[Ingredient.Potato] = Find("Potato", new[] { "potato" }, new[] { "chips", "fries", "mashed" });
        mFood[Ingredient.Cheese] = Find("Cheese", new[] { "cheese" }, new[] { "burger", "cake", "sandwich", "pizza" });
    }

    // مواد الأصول اللي مسوية للـ Built-in تطلع وردي في URP: نحولها لـ URP Lit بنفس اللون والصورة
    static void ConvertPinkMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return;
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.StartsWith(MatFolder)) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) continue;
            var sh = m.shader;
            bool bad = sh == null || !sh.isSupported || sh.name == "Standard" || sh.name == "Standard (Specular setup)"
                       || sh.name.StartsWith("Legacy Shaders") || sh.name.Contains("InternalError");
            if (!bad) continue;
            Color c = ReadColor(m, "_Color", Color.white);
            Texture t = ReadTex(m, "_MainTex");
            float gloss = ReadFloat(m, "_Glossiness", 0.3f);
            m.shader = lit;
            m.SetColor("_BaseColor", c);
            if (t != null) m.SetTexture("_BaseMap", t);
            m.SetFloat("_Smoothness", Mathf.Clamp01(gloss * 0.6f));
            EditorUtility.SetDirty(m);
            n++;
        }
        if (n > 0) { AssetDatabase.SaveAssets(); Debug.Log("Kitchen Rush: converted " + n + " pink materials to URP."); }
    }

    static SerializedProperty Saved(Material m, string list, string prop)
    {
        var so = new SerializedObject(m);
        var arr = so.FindProperty("m_SavedProperties." + list);
        if (arr == null) return null;
        for (int i = 0; i < arr.arraySize; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            var first = e.FindPropertyRelative("first");
            if (first != null && first.stringValue == prop) return e.FindPropertyRelative("second");
        }
        return null;
    }

    static Color ReadColor(Material m, string prop, Color def)
    {
        if (m.HasProperty(prop)) return m.GetColor(prop);
        var v = Saved(m, "m_Colors", prop);
        return v != null ? v.colorValue : def;
    }

    static Texture ReadTex(Material m, string prop)
    {
        if (m.HasProperty(prop)) return m.GetTexture(prop);
        var v = Saved(m, "m_TexEnvs", prop);
        if (v == null) return null;
        var t = v.FindPropertyRelative("m_Texture");
        return t != null ? t.objectReferenceValue as Texture : null;
    }

    static float ReadFloat(Material m, string prop, float def)
    {
        if (m.HasProperty(prop)) return m.GetFloat(prop);
        var v = Saved(m, "m_Floats", prop);
        return v != null ? v.floatValue : def;
    }

    static string Norm(string path)
    {
        return Path.GetFileNameWithoutExtension(path).ToLower().Replace('_', '-').Replace(' ', '-');
    }

    static GameObject Find(string what, string[] keys, string[] exclude)
    {
        string hit = null;
        foreach (var k in keys)
        {
            foreach (var p in modelPaths) if (Norm(p) == k) { hit = p; break; }
            if (hit != null) break;
        }
        if (hit == null)
            foreach (var k in keys)
            {
                foreach (var p in modelPaths)
                {
                    string n = Norm(p);
                    if (!n.Contains(k)) continue;
                    bool bad = false;
                    if (exclude != null) foreach (var e in exclude) if (n.Contains(e)) bad = true;
                    if (!bad) { hit = p; break; }
                }
                if (hit != null) break;
            }
        report.Add(what + ": " + (hit != null ? hit : "(not found, using simple shape)"));
        return hit != null ? AssetDatabase.LoadAssetAtPath<GameObject>(hit) : null;
    }

    // ---------- البناء ----------

    static void Build(string rootName, string[] map)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        ConvertPinkMaterials();
        FindAllModels();

        var old = GameObject.Find(rootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var oldChef = GameObject.Find("Chef");
        if (oldChef != null) Undo.DestroyObjectImmediate(oldChef);

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Kitchen");

        var rows = new List<string[]>();
        foreach (var r in map) rows.Add(r.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
        int R = rows.Count, C = rows[0].Length;

        if (outdoor) BuildYard(root, C, R); else BuildRoom(root, C, R);

        Vector3 chefPos = Vector3.zero;
        bool ovenDone = false;
        for (int r = 0; r < R; r++)
        {
            for (int c = 0; c < C; c++)
            {
                string code = rows[r][c];
                Vector3 pos = new Vector3(c - (C - 1) / 2f, 0f, (R - 1) / 2f - r);
                float face = Facing(r, c, R, C);
                switch (code)
                {
                    case "##":
                        bool corner = (r == 0 || r == R - 1) && (c == 0 || c == C - 1);
                        if (corner) DecorCounter(root, pos, face, r * 7 + c);
                        else Counter(root, "Counter", pos, face, !outdoor && r == 0 && !ovenDone && c > C / 2 && (ovenDone = true));
                        break;
                    case "L": CrateTile(root, pos, face, Ingredient.Lettuce); break;
                    case "T": CrateTile(root, pos, face, Ingredient.Tomato); break;
                    case "M": CrateTile(root, pos, face, Ingredient.Meat); break;
                    case "B": CrateTile(root, pos, face, Ingredient.Bun); break;
                    case "P": CrateTile(root, pos, face, Ingredient.Potato); break;
                    case "C": CrateTile(root, pos, face, Ingredient.Cheese); break;
                    case "CH": ChopTile(root, pos, face); break;
                    case "GR": GrillTile(root, pos, face); break;
                    case "PL": PlatesTile(root, pos, face); break;
                    case "SV": ServeTile(root, pos); break;
                    case "TR": TrashTile(root, pos, face); break;
                    case "CF": chefPos = pos; break;
                }
            }
        }

        BuildChef(chefPos + Vector3.up * 0.05f);
        var chefGo = GameObject.Find("Chef");
        if (chefGo != null && ApplyCharacter(chefGo)) report.Add("Character: model from " + CharFolder);
        AddLevelManager(root);
        SetupCamera();
        SetupLights(root, C, R);
        SetupPost(root);

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Kitchen Rush: " + rootName + " built.\n" + string.Join("\n", report));
    }

    // ---------- الشخصية الحقيقية (Mixamo) ----------
    // حطي ملفات Mixamo في Assets/KitchenRush/Character:
    // الشخصية (With Skin) + Idle + Walking (+ Carrying اختياري)
    const string CharFolder = "Assets/KitchenRush/Character";

    [MenuItem("Tools/Kitchen Rush/Use Character Model (Mixamo)")]
    static void UseCharacterMenu()
    {
        var chef = GameObject.Find("Chef");
        if (chef == null) { Debug.LogWarning("Kitchen Rush: build the kitchen first."); return; }
        if (ApplyCharacter(chef))
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Kitchen Rush: character model applied.");
        }
        else Debug.LogWarning("Kitchen Rush: no character model found in " + CharFolder);
    }

    static bool ApplyCharacter(GameObject chef)
    {
        if (!AssetDatabase.IsValidFolder(CharFolder)) return false;
        var all = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CharFolder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Norm(p).StartsWith("mom")) continue;   // ملفات الأم لها مكانها
            if (!all.Contains(p)) all.Add(p);
        }
        if (all.Count == 0) return false;

        // كلها Humanoid، والحركات تتكرر
        foreach (var p in all)
        {
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            bool changed = false;
            if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; changed = true; }
            string n = Norm(p);
            bool isAnim = n.Contains("idle") || n.Contains("walk") || n.Contains("carr") || n.Contains("run");
            if (changed) { imp.SaveAndReimport(); changed = false; }
            if (isAnim)
            {
                // تتكرر، وتمشي بمكانها (الحركة لقدام تنشال وما ترجع الشخصية لورا)
                var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
                bool need = imp.clipAnimations.Length == 0;
                foreach (var c in clips)
                {
                    if (!c.loopTime || c.lockRootPositionXZ || !c.lockRootRotation || !c.lockRootHeightY) need = true;
                    c.loopTime = true;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = false;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                if (need && clips.Length > 0) { imp.clipAnimations = clips; changed = true; }
            }
            if (changed) imp.SaveAndReimport();
        }

        string model = null, idle = null, walk = null, carry = null;
        foreach (var p in all)
        {
            string n = Norm(p);
            if (n.Contains("idle")) { if (idle == null) idle = p; continue; }
            if (n.Contains("walk")) { if (walk == null) walk = p; continue; }
            if (n.Contains("carr")) { if (carry == null) carry = p; continue; }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (model == null && go != null && go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) model = p;
        }
        // لو الشخصية نازلة مع حركة الوقوف (With Skin) نستخدم نفس الملف
        if (model == null)
            foreach (var p in new[] { idle, walk, carry })
            {
                if (p == null) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null && go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) { model = p; break; }
            }
        if (model == null) return false;

        // Animator: وقوف ⇄ مشي (⇄ مشي وهي شايلة)
        string ctrlPath = CharFolder + "/ChefAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Carry", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Fidget", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        // وقفة هادية: نفس حركة الوقوف بس ببطء كثير
        var sIdle = sm.AddState("Idle");
        sIdle.motion = Clip(idle);
        sIdle.speed = 0.25f;
        // الحركة الكاملة تطلع بس لو وقفت فترة طويلة
        var sFidget = sm.AddState("IdleFidget");
        sFidget.motion = Clip(idle);
        sFidget.speed = 0.8f;
        var tf = Trans(sIdle, sFidget); tf.duration = 0.4f; tf.AddCondition(AnimatorConditionMode.If, 0f, "Fidget");
        var tb = sFidget.AddTransition(sIdle); tb.hasExitTime = true; tb.exitTime = 0.95f; tb.duration = 0.4f;
        var tfw = Trans(sFidget, sIdle); tfw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); tfw.duration = 0.1f;
        var sWalk = sm.AddState("Walk");
        sWalk.motion = Clip(walk) != null ? Clip(walk) : Clip(idle);
        sm.defaultState = sIdle;
        var cClip = Clip(carry);
        if (cClip != null)
        {
            var sCarry = sm.AddState("CarryWalk");
            sCarry.motion = cClip;
            var tw = Trans(sIdle, sWalk);
            tw.AddCondition(AnimatorConditionMode.IfNot, 0f, "Carry");
            tw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var t2 = Trans(sIdle, sCarry); t2.AddCondition(AnimatorConditionMode.If, 0f, "Carry"); t2.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Trans(sWalk, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            Trans(sWalk, sCarry).AddCondition(AnimatorConditionMode.If, 0f, "Carry");
            Trans(sCarry, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            Trans(sCarry, sWalk).AddCondition(AnimatorConditionMode.IfNot, 0f, "Carry");
        }
        else
        {
            Trans(sIdle, sWalk).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Trans(sWalk, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }
        AssetDatabase.SaveAssets();

        // نشيل الشكل القديم ونحط الشخصية
        var remove = new List<GameObject>();
        foreach (Transform c in chef.transform) if (c.name != "Hand") remove.Add(c.gameObject);
        foreach (var g in remove) Undo.DestroyObjectImmediate(g);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(m, "Character");
        m.name = "Model";
        m.transform.SetParent(chef.transform, false);
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        foreach (var col in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        Bounds b;
        if (GetBounds(m, out b) && b.size.y > 0.01f)
        {
            m.transform.localScale *= 1.65f / b.size.y;
            GetBounds(m, out b);
            m.transform.position += Vector3.up * (chef.transform.position.y - b.min.y);
        }
        FixCharacterMaterials(m);
        var animator = m.GetComponent<Animator>();
        if (animator == null) animator = m.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var hand = chef.transform.Find("Hand");
        if (hand != null) hand.localPosition = new Vector3(0f, 1.0f, 0.45f);

        var skin = chef.GetComponent<ChefSkin>();
        if (skin != null) { skin.recolor = false; skin.pressKToChange = false; }
        var ctrlr = chef.GetComponent<ChefController>();
        if (ctrlr != null)
        {
            ctrlr.animateBody = false;
            ctrlr.speed = 4.5f;
            ctrlr.deceleration = 70f;
        }
        return true;
    }

    // الشخصية تطلع بيضا لو ما لقت صورها: نسوي لها مادة URP من صور Mixamo
    static void FixCharacterMaterials(GameObject m)
    {
        Texture2D diff = null, nrm = null;
        string nrmPath = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CharFolder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            string n = Norm(p);
            if (diff == null && (n.Contains("diffuse") || n.Contains("albedo") || n.Contains("basecolor") || n.Contains("color"))) diff = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            else if (nrm == null && n.Contains("normal")) { nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(p); nrmPath = p; }
        }
        if (diff == null) return;
        if (nrmPath != null)
        {
            var ti = AssetImporter.GetAtPath(nrmPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
        }
        var mat = Mat("Nina_Body", Color.white);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
        if (nrm != null && mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", nrm); mat.EnableKeyword("_NORMALMAP"); }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(mat);
        foreach (var r in m.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.sharedMaterials = arr;
        }
    }

    static AnimationClip Clip(string path)
    {
        if (path == null) return null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            var c = o as AnimationClip;
            if (c != null && !c.name.StartsWith("__preview__")) return c;
        }
        return null;
    }

    static AnimatorStateTransition Trans(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.12f;
        return t;
    }

    // الطلبات والنقاط والوقت (إعدادات المرحلة ١)
    static void AddLevelManager(GameObject root)
    {
        var g = new GameObject("Level Manager");
        g.transform.SetParent(root.transform);
        var om = g.AddComponent<OrderManager>();
        if (outdoor)
        {
            // المرحلة ٢: حفلة الحوش، برقر على الشواية
            om.levelTitle = "LEVEL 2 · BACKYARD PARTY";
            om.levelTime = 210f;
            om.maxOrders = 2;
            om.patience = 85f;
            om.starScores = new[] { 100, 180, 260 };
            om.salad = true;
            om.cheeseSalad = false;
            om.sandwich = false;
            om.burger = true;
            om.cheeseburger = true;
            om.chapterLabel = "CHAPTER 2";
            om.chapterTitle = "Backyard Party";
            om.chapterText = "Mom loved dinner so much that she told the whole neighborhood!\nNow everyone is coming to a party in the backyard,\nand they all want burgers. Time to fire up the grill!";
            om.introCaptions = new[]
            {
                "Wow... the whole neighborhood is here!",
                "Burgers need grilled meat. I can't let it burn!",
                "Tip: Put raw meat on the grill. Take it off when it turns brown, before the red bar fills!",
            };
            om.firstServeLine = "They love the burgers!";
            om.missedLine = "Oh no... a guest left hungry.";
            om.hurryLine = "The party is almost over, hurry!";
            return;
        }
        om.levelTitle = "LEVEL 1 · NINA'S KITCHEN";
        om.levelTime = 180f;
        // أول مرحلة هادية: طلب واحد بس بالمرة، وصبر أطول، والطلب الجاي يتأخر شوي
        om.maxOrders = 1;
        om.patience = 95f;
        om.firstOrderDelay = 2f;
        om.nextOrderDelay = 6f;
        om.starScores = new[] { 50, 90, 130 };
        om.salad = true;
        om.cheeseSalad = true;
        om.sandwich = true;
    }

    // الطاولة تطالع وسط المطبخ
    static float Facing(int r, int c, int R, int C)
    {
        float y = 0f;
        if (r == 0) y = 180f;
        else if (r == R - 1) y = 0f;
        else if (c == 0) y = 90f;
        else if (c == C - 1) y = 270f;
        return y + ModelFrontOffset;
    }

    // ---------- الغرفة ----------

    const float Side = 2.3f;   // مساحة الزينة يمين ويسار المطبخ

    // ألوان الستايل: مطبخ دافي ريترو بإضاءة غروب
    static readonly Color Cream = new Color(0.97f, 0.94f, 0.87f);
    static readonly Color CabinetWhite = new Color(0.95f, 0.94f, 0.9f);
    static readonly Color TopGrey = new Color(0.8f, 0.79f, 0.76f);
    static readonly Color ShelfWood = new Color(0.78f, 0.57f, 0.36f);
    static readonly Color DustyBlue = new Color(0.55f, 0.7f, 0.84f);
    static readonly Color[] Pastels =
    {
        new Color(0.42f, 0.62f, 0.86f), new Color(0.62f, 0.78f, 0.93f),
        new Color(0.95f, 0.55f, 0.33f), new Color(0.62f, 0.78f, 0.5f), new Color(0.45f, 0.68f, 0.78f),
        new Color(0.97f, 0.73f, 0.72f), new Color(0.98f, 0.82f, 0.4f), new Color(0.86f, 0.42f, 0.38f),
        new Color(0.55f, 0.75f, 0.7f), new Color(0.96f, 0.66f, 0.45f)
    };

    static void BuildRoom(GameObject root, int C, int R)
    {
        float W = C + Side * 2f;
        float z = R / 2f;

        // أرضية خشب
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        floor.transform.localScale = new Vector3(W, 0.2f, R);
        floor.GetComponent<Renderer>().sharedMaterial = TexMat("Floor_Wood", "FloorWood", WoodTex(), new Vector2(W / 4f, R / 2f));
        Block(root, "Base", new Vector3(0f, -0.35f, 0f), new Vector3(W + 0.6f, 0.3f, R + 0.6f), Mat("Kitchen_Base", new Color(0.5f, 0.36f, 0.25f)), false);

        // جدران كريمية
        var paint = Mat("Wall_Paint", Cream);
        Block(root, "Wall_Back", new Vector3(0f, 1.3f, z + 0.15f), new Vector3(W + 0.6f, 2.6f, 0.3f), paint, true);
        Block(root, "Wall_Left", new Vector3(-W / 2f - 0.15f, 1.3f, 0f), new Vector3(0.3f, 2.6f, R), paint, true);
        Block(root, "Wall_Right", new Vector3(W / 2f + 0.15f, 1.3f, 0f), new Vector3(0.3f, 2.6f, R), paint, true);
        var trim = Mat("Wall_Trim", new Color(0.9f, 0.86f, 0.78f));
        Block(root, "Skirting_Back", new Vector3(0f, 0.06f, z - 0.01f), new Vector3(W, 0.12f, 0.04f), trim, false);
        Block(root, "Skirting_Left", new Vector3(-W / 2f + 0.01f, 0.06f, 0f), new Vector3(0.04f, 0.12f, R), trim, false);
        Block(root, "Skirting_Right", new Vector3(W / 2f - 0.01f, 0.06f, 0f), new Vector3(0.04f, 0.12f, R), trim, false);

        // بلاط ملوّن ورا الطاولات (مثل الصورة)
        var tileMat = TexMat("Backsplash", "PatchworkTiles_v2", PatchworkTex(), Vector2.one);
        if (tileMat.HasProperty("_Smoothness")) tileMat.SetFloat("_Smoothness", 0.75f);
        var tiles = Block(root, "Backsplash", new Vector3(0f, 1.32f, z - 0.005f), new Vector3(C, 0.84f, 0.02f), tileMat, false);
        SetTiling(tiles, new Vector2(C / 2f, 1f));

        // جدران مخفية: الطباخة تبقى داخل منطقة اللعب
        Invisible(root, "Wall_Front (invisible)", new Vector3(0f, 1f, -R / 2f - 0.15f), new Vector3(W, 2f, 0.3f));
        if (!serveAtTable) Invisible(root, "Wall_PlayLeft (invisible)", new Vector3(-C / 2f - 0.1f, 1f, 0f), new Vector3(0.2f, 2f, R));
        Invisible(root, "Wall_PlayRight (invisible)", new Vector3(C / 2f + 0.1f, 1f, 0f), new Vector3(0.2f, 2f, R));

        // الشباك بالنص، ورفوف خشب عليها قدور ملونة يمينه ويساره
        BuildWindow(root, new Vector3(0f, 1.3f, z));
        float shelfW = C / 2f - 1.4f;
        float sxL = -C / 4f - 0.35f, sxR = C / 4f + 0.35f;
        for (int k = 0; k < 2; k++)
        {
            float sx = k == 0 ? sxL : sxR;
            for (int lvl = 0; lvl < 2; lvl++)
            {
                float y = 1.95f + lvl * 0.42f;
                Block(root, "Shelf", new Vector3(sx, y, z - 0.16f), new Vector3(shelfW, 0.05f, 0.32f), Mat("Shelf_Wood", ShelfWood), false);
                int n = Mathf.Max(2, Mathf.RoundToInt(shelfW / 0.55f));
                for (int i = 0; i < n; i++)
                {
                    float x = sx - shelfW / 2f + 0.3f + i * (shelfW - 0.6f) / Mathf.Max(1, n - 1);
                    Vector3 p = new Vector3(x, y + 0.025f, z - 0.16f);
                    int seed = k * 10 + lvl * 5 + i;
                    if ((i + lvl) % 3 == 0) DutchOven(root.transform, p, seed);
                    else if ((i + lvl) % 3 == 1) Canister(root.transform, p, seed);
                    else CutePlant(root.transform, p, 0.36f, seed);
                }
            }
            // نبتة متدلية من طرف الرف العلوي
            TrailingPlant(root.transform, new Vector3(sx + (k == 0 ? -1 : 1) * (shelfW / 2f - 0.15f), 2.395f, z - 0.16f), k);
        }

        float lx = -C / 2f - Side / 2f, rx = C / 2f + Side / 2f;

        // يسار: ثلاجة ريترو كريمية + ركن أكل
        RetroFridge(root.transform, new Vector3(lx + 0.35f, 0f, z - 0.5f));
        CutePlant(root.transform, new Vector3(lx + 0.35f, 1.98f, z - 0.5f), 0.55f, 7);
        DiningNook(root.transform, new Vector3(lx, 0f, -R / 2f + 1.1f));

        // يمين: رف وكتب ونبتة كبيرة
        Bookshelf(root.transform, new Vector3(rx + 0.2f, 0f, z - 0.4f));
        BigPlant(root.transform, new Vector3(rx + 0.4f, 0f, -R / 2f + 0.7f), 1);

        // لوحات على الجدران الجانبية
        ArtFrame(root.transform, new Vector3(-W / 2f + 0.02f, 1.7f, 0.2f), 90f, 0);
        ArtFrame(root.transform, new Vector3(-W / 2f + 0.02f, 1.55f, -0.75f), 90f, 1);
        ArtFrame(root.transform, new Vector3(W / 2f - 0.02f, 1.7f, -0.3f), -90f, 2);
        WavyMirror(root.transform, new Vector3(W / 2f - 0.02f, 1.6f, 0.7f));
    }

    static void Invisible(GameObject root, string name, Vector3 pos, Vector3 size)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root.transform);
        g.transform.position = pos;
        g.AddComponent<BoxCollider>().size = size;
    }

    // ---------- زينة ----------

    static void BuildWindow(GameObject root, Vector3 wallPoint)
    {
        var win = new GameObject("Window");
        win.transform.SetParent(root.transform);
        win.transform.position = wallPoint + new Vector3(0f, 0f, -0.02f);
        var frame = Mat("Window_Frame", new Color(0.98f, 0.97f, 0.95f));
        var sky = MatGlow("Window_Sky", new Color(1f, 0.83f, 0.6f), 1.3f);
        Part(win, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 0.04f), sky).name = "Glass";
        Part(win, PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.02f), new Vector3(1.42f, 0.07f, 0.07f), frame);
        Part(win, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.07f), new Vector3(1.5f, 0.05f, 0.18f), frame).name = "Sill";
        Part(win, PrimitiveType.Cube, new Vector3(-0.68f, 0.55f, -0.02f), new Vector3(0.07f, 1.17f, 0.07f), frame);
        Part(win, PrimitiveType.Cube, new Vector3(0.68f, 0.55f, -0.02f), new Vector3(0.07f, 1.17f, 0.07f), frame);
        Part(win, PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.02f), new Vector3(0.045f, 1.1f, 0.045f), frame);
        Part(win, PrimitiveType.Cube, new Vector3(0f, 0.75f, -0.02f), new Vector3(1.3f, 0.045f, 0.045f), frame);
        CutePlant(win.transform, wallPoint + new Vector3(-0.42f, 0.01f, -0.1f), 0.32f, 3);
        Vase(win.transform, wallPoint + new Vector3(0.4f, 0.01f, -0.1f));
    }

    // طاولة الزوايا: غلاية وتوستر وعلب
    static void DecorCounter(GameObject root, Vector3 pos, float face, int seed)
    {
        var t = Tile(root, "Counter_Decor", pos);
        CounterBody(t, face, CabinetWhite, TopGrey);
        Vector3 top = pos + Vector3.up * CounterH;
        if (seed % 2 == 0)
        {
            Kettle(t.transform, top + new Vector3(-0.18f, 0f, 0.05f));
            Canister(t.transform, top + new Vector3(0.22f, 0f, -0.1f), seed);
        }
        else
        {
            Toaster(t.transform, top + new Vector3(-0.1f, 0f, 0f));
            CutePlant(t.transform, top + new Vector3(0.25f, 0f, 0.1f), 0.38f, seed);
        }
        // بدون Station: طاولة زينة بس
    }

    static void CutePlant(Transform parent, Vector3 worldBottom, float size, int seed)
    {
        var p = new GameObject("Plant");
        p.transform.SetParent(parent, true);
        p.transform.position = worldBottom;
        var pot = Mat("Pot_" + (seed % Pastels.Length), Color.Lerp(Pastels[seed % Pastels.Length], Color.white, 0.25f));
        var leaf = Mat("Leaf", new Color(0.3f, 0.58f, 0.3f));
        var leaf2 = Mat("Leaf_Light", new Color(0.46f, 0.72f, 0.36f));
        float s = size;
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.18f * s, 0f), new Vector3(0.5f * s, 0.18f * s, 0.5f * s), pot);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.36f * s, 0f), new Vector3(0.56f * s, 0.03f * s, 0.56f * s), pot);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.6f * s, 0f), new Vector3(0.55f * s, 0.45f * s, 0.55f * s), leaf);
        Part(p, PrimitiveType.Sphere, new Vector3(-0.15f * s, 0.75f * s, 0.05f * s), new Vector3(0.35f * s, 0.4f * s, 0.35f * s), leaf2);
        Part(p, PrimitiveType.Sphere, new Vector3(0.14f * s, 0.8f * s, -0.06f * s), new Vector3(0.3f * s, 0.42f * s, 0.3f * s), leaf);
    }

    // نبتة متدلية من الرف
    static void TrailingPlant(Transform parent, Vector3 bottom, int seed)
    {
        var p = new GameObject("TrailingPlant");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        var pot = Mat("Pot_Trail", new Color(0.93f, 0.9f, 0.84f));
        var leaf = Mat("Leaf", new Color(0.3f, 0.58f, 0.3f));
        var leaf2 = Mat("Leaf_Light", new Color(0.46f, 0.72f, 0.36f));
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.2f, 0.08f, 0.2f), pot);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), new Vector3(0.28f, 0.16f, 0.28f), leaf);
        for (int vine = 0; vine < 3; vine++)
        {
            float ox = (vine - 1) * 0.08f;
            int len = 5 + (vine + seed) % 3;
            for (int i = 0; i < len; i++)
                Part(p, PrimitiveType.Sphere, new Vector3(ox + Mathf.Sin(i * 1.3f + vine) * 0.03f, 0.1f - i * 0.11f, -0.1f), Vector3.one * (0.09f - i * 0.004f), i % 2 == 0 ? leaf : leaf2);
        }
    }

    static void BigPlant(Transform parent, Vector3 bottom, int seed)
    {
        var p = new GameObject("BigPlant");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        var pot = Mat("Pot_Big", new Color(0.86f, 0.5f, 0.36f));
        var leaf = Mat("Leaf", new Color(0.3f, 0.58f, 0.3f));
        var leaf2 = Mat("Leaf_Light", new Color(0.46f, 0.72f, 0.36f));
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), pot);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(0.04f, 0.3f, 0.04f), Mat("Stem", new Color(0.4f, 0.3f, 0.2f)));
        // أوراق كبيرة مثل المونستيرا
        for (int i = 0; i < 7; i++)
        {
            float a = i * 51f + seed * 20f;
            var l = Part(p, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.3f, 1.0f + (i % 3) * 0.18f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.3f), new Vector3(0.42f, 0.06f, 0.3f), i % 2 == 0 ? leaf : leaf2);
            l.transform.localRotation = Quaternion.Euler(0f, -a, 25f);
        }
    }

    static void DutchOven(Transform parent, Vector3 bottom, int seed)
    {
        Color[] cs = { new Color(0.93f, 0.42f, 0.18f), new Color(0.3f, 0.5f, 0.82f), new Color(0.52f, 0.68f, 0.4f), new Color(0.36f, 0.62f, 0.66f), new Color(0.85f, 0.3f, 0.22f) };
        var m = MatS("Pot_Enamel_" + (seed % cs.Length), cs[seed % cs.Length], 0.7f);
        var p = new GameObject("DutchOven");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.3f, 0.09f, 0.3f), m);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), new Vector3(0.31f, 0.07f, 0.31f), m);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.23f, 0f), new Vector3(0.06f, 0.04f, 0.06f), Mat("Pot_Knob", new Color(0.25f, 0.22f, 0.2f)));
        Part(p, PrimitiveType.Cube, new Vector3(0.17f, 0.12f, 0f), new Vector3(0.05f, 0.03f, 0.08f), m);
        Part(p, PrimitiveType.Cube, new Vector3(-0.17f, 0.12f, 0f), new Vector3(0.05f, 0.03f, 0.08f), m);
    }

    // علبة بنقشة مربعات
    static void Canister(Transform parent, Vector3 bottom, int seed)
    {
        Color c = Pastels[(seed * 3) % Pastels.Length];
        var tex = CheckerSmallTex(c);
        var m = TexMat("Canister_" + ColorUtility.ToHtmlStringRGB(c), "Checker_" + ColorUtility.ToHtmlStringRGB(c), tex, new Vector2(3f, 2f));
        var p = new GameObject("Canister");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.12f, 0.18f), m);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0f), new Vector3(0.19f, 0.05f, 0.19f), Mat("Canister_Lid", new Color(0.97f, 0.95f, 0.9f)));
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), Vector3.one * 0.04f, Mat("Canister_Lid", new Color(0.97f, 0.95f, 0.9f)));
    }

    static void Kettle(Transform parent, Vector3 bottom)
    {
        var cream = Mat("Appliance_Cream", new Color(0.93f, 0.9f, 0.8f));
        var steel = Mat("Steel", new Color(0.8f, 0.83f, 0.86f));
        var p = new GameObject("Kettle");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.24f, 0.02f, 0.24f), steel);
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0f), new Vector3(0.24f, 0.24f, 0.24f), cream);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0f), new Vector3(0.12f, 0.02f, 0.12f), steel);
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.24f, -0.12f), new Vector3(0.04f, 0.12f, 0.06f), cream);
        var spout = Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.16f, 0.14f), new Vector3(0.04f, 0.06f, 0.04f), cream);
        spout.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
    }

    static void Toaster(Transform parent, Vector3 bottom)
    {
        var cream = Mat("Appliance_Cream", new Color(0.93f, 0.9f, 0.8f));
        var dark = Mat("Appliance_Dark", new Color(0.2f, 0.2f, 0.2f));
        var p = new GameObject("Toaster");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0.3f, 0.2f, 0.18f), cream);
        Part(p, PrimitiveType.Cube, new Vector3(-0.05f, 0.215f, 0f), new Vector3(0.18f, 0.01f, 0.03f), dark);
        Part(p, PrimitiveType.Cube, new Vector3(-0.05f, 0.215f, 0.05f), new Vector3(0.18f, 0.01f, 0.03f), dark);
        Part(p, PrimitiveType.Cube, new Vector3(0.155f, 0.13f, 0f), new Vector3(0.01f, 0.03f, 0.05f), dark);
    }

    static void Vase(Transform parent, Vector3 bottom)
    {
        var p = new GameObject("Tulips");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0f), new Vector3(0.1f, 0.1f, 0.1f), Mat("Vase", new Color(0.98f, 0.97f, 0.94f)));
        var stem = Mat("Stem_Green", new Color(0.35f, 0.6f, 0.3f));
        Color[] tc = { new Color(1f, 0.45f, 0.4f), new Color(1f, 0.75f, 0.3f), new Color(1f, 0.6f, 0.7f) };
        for (int i = 0; i < 3; i++)
        {
            float a = i * 2.1f;
            var st = Part(p, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.03f, 0.27f, Mathf.Sin(a) * 0.03f), new Vector3(0.012f, 0.1f, 0.012f), stem);
            st.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 10f, 0f, Mathf.Cos(a) * 10f);
            Part(p, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.05f, 0.38f, Mathf.Sin(a) * 0.05f), new Vector3(0.06f, 0.08f, 0.06f), Mat("Tulip_" + i, tc[i]));
        }
    }

    // ثلاجة ريترو كريمية (مثل SMEG)
    static void RetroFridge(Transform parent, Vector3 bottom)
    {
        var cream = Mat("Fridge_Cream", new Color(0.96f, 0.93f, 0.85f));
        var steel = Mat("Steel", new Color(0.8f, 0.83f, 0.86f));
        var p = new GameObject("RetroFridge");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        var fc = p.AddComponent<BoxCollider>();
        fc.center = new Vector3(0f, 1f, 0f);
        fc.size = new Vector3(0.85f, 2f, 0.75f);
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(0.8f, 1.7f, 0.7f), cream);
        var top = Part(p, PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.7f, 0.4f, 0.8f), cream);
        top.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        top.transform.localScale = new Vector3(0.12f, 0.4f, 0.7f);
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.72f, 0.1f, 0.62f), Mat("Appliance_Dark", new Color(0.2f, 0.2f, 0.2f)));
        Part(p, PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.355f), new Vector3(0.78f, 0.015f, 0.01f), Mat("Fridge_Line", new Color(0.8f, 0.77f, 0.7f)));
        Part(p, PrimitiveType.Cube, new Vector3(0.3f, 1.55f, -0.38f), new Vector3(0.05f, 0.25f, 0.04f), steel);
        Part(p, PrimitiveType.Cube, new Vector3(0.3f, 1.05f, -0.38f), new Vector3(0.05f, 0.3f, 0.04f), steel);
        Part(p, PrimitiveType.Sphere, new Vector3(-0.2f, 1.5f, -0.355f), new Vector3(0.09f, 0.09f, 0.02f), Mat("Magnet_Pink", new Color(1f, 0.55f, 0.6f)));
        Part(p, PrimitiveType.Sphere, new Vector3(-0.05f, 1.2f, -0.355f), new Vector3(0.08f, 0.08f, 0.02f), Mat("Magnet_Yellow", new Color(1f, 0.82f, 0.35f)));
        Part(p, PrimitiveType.Cube, new Vector3(-0.15f, 0.9f, -0.355f), new Vector3(0.16f, 0.2f, 0.01f), Mat("Note_Paper", new Color(1f, 0.98f, 0.9f)));
    }

    // ركن أكل: طاولة صغيرة وكرسيين وفازة توليب ولمبة معلقة
    static void DiningNook(Transform parent, Vector3 center)
    {
        var p = new GameObject("DiningTable");
        p.transform.SetParent(parent, true);
        p.transform.position = center;
        var wood = MatS("Table_Wood", new Color(0.86f, 0.72f, 0.5f), 0.4f);
        var seat = Mat("Chair_Blue", new Color(0.4f, 0.6f, 0.85f));
        var chrome = MatS("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.85f);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.74f, 0f), new Vector3(0.95f, 0.025f, 0.95f), wood);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), new Vector3(0.06f, 0.37f, 0.06f), chrome);
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.45f, 0.02f, 0.45f), chrome);
        // كرسي ورا (للأم) وكرسي قدام
        for (int s = -1; s <= 1; s += 2)
        {
            var ch = new GameObject(s > 0 ? "Chair_Mom" : "Chair");
            ch.transform.SetParent(p.transform, false);
            ch.transform.localPosition = new Vector3(0f, 0f, s * 0.68f);
            ch.transform.localRotation = Quaternion.Euler(0f, s > 0 ? 0f : 180f, 0f);
            Part(ch, PrimitiveType.Cube, new Vector3(0f, 0.46f, 0f), new Vector3(0.42f, 0.06f, 0.42f), seat);
            Part(ch, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.2f), new Vector3(0.42f, 0.36f, 0.05f), seat);
            for (int a = -1; a <= 1; a += 2)
                for (int b = -1; b <= 1; b += 2)
                    Part(ch, PrimitiveType.Cylinder, new Vector3(a * 0.18f, 0.22f, b * 0.18f), new Vector3(0.03f, 0.22f, 0.03f), chrome);
        }
        Vase(p.transform, center + new Vector3(0.25f, 0.765f, 0.2f));
        // لمبة معلقة فوق الطاولة
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 2.35f, 0f), new Vector3(0.01f, 0.3f, 0.01f), Mat("Appliance_Dark", new Color(0.2f, 0.2f, 0.2f)));
        Part(p, PrimitiveType.Sphere, new Vector3(0f, 1.98f, 0f), new Vector3(0.4f, 0.3f, 0.4f), MatGlow("Lamp_Shade", new Color(1f, 0.9f, 0.6f), 0.8f));
        var pl = new GameObject("Lamp_Light").AddComponent<Light>();
        pl.transform.SetParent(p.transform, false);
        pl.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        pl.type = LightType.Point;
        pl.color = new Color(1f, 0.8f, 0.55f);
        pl.range = 3.5f;
        pl.intensity = 2.5f;

        // الطاولة صلبة، وفي المرحلة ١ هي مكان التسليم
        var bc = p.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.39f, 0f);
        bc.size = new Vector3(1.0f, 0.78f, 1.0f);
        var chairs = p.AddComponent<BoxCollider>();
        chairs.center = new Vector3(0f, 0.5f, 0.7f);
        chairs.size = new Vector3(0.5f, 1f, 0.5f);
        if (serveAtTable)
        {
            var sv = p.AddComponent<ServeWindow>();
            sv.hint = "[E] Serve to Mom";
            // صحن فاضي وشوكة قدام الأم (زينة)
            Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0.77f, 0.28f), new Vector3(0.3f, 0.01f, 0.3f), Mat("Plate", new Color(0.97f, 0.97f, 0.97f)));
            PlaceMom(p.transform, center + new Vector3(0f, 0f, 0.72f));
        }
    }

    // الأم: شخصية Mixamo جالسة (لو موجودة في Character باسم Mom)
    static GameObject PlaceMom(Transform parent, Vector3 seatFloor)
    {
        // Mom.fbx = الشخصية (With Skin)، Mom@Sitting.fbx = حركة الجلوس
        var momFiles = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CharFolder }))
        {
            string pth = AssetDatabase.GUIDToAssetPath(guid);
            if (Norm(pth).StartsWith("mom") && !momFiles.Contains(pth)) momFiles.Add(pth);
        }
        if (momFiles.Count == 0) { report.Add("Mom: not found (put Mom.fbx + Mom@Sitting.fbx in " + CharFolder + ")"); return null; }

        string model = null, sit = null;
        foreach (var pth in momFiles)
        {
            var imp = AssetImporter.GetAtPath(pth) as ModelImporter;
            if (imp == null) continue;
            if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; imp.SaveAndReimport(); }
            bool isSit = Norm(pth).Contains("sit");
            if (isSit)
            {
                sit = pth;
                var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
                bool need = imp.clipAnimations.Length == 0;
                foreach (var c in clips)
                {
                    if (!c.loopTime || c.lockRootPositionXZ) need = true;
                    c.loopTime = true;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = false;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                if (need && clips.Length > 0) { imp.clipAnimations = clips; imp.SaveAndReimport(); }
            }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(pth);
            if (model == null && go != null && go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0) model = pth;
        }
        if (model == null) { report.Add("Mom: no character mesh found"); return null; }

        string ctrlPath = CharFolder + "/MomAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var st = ctrl.layers[0].stateMachine.AddState("Sit");
        st.motion = Clip(sit != null ? sit : model);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        m.name = "Mom";
        m.transform.SetParent(parent, true);
        m.transform.position = seatFloor;
        m.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        foreach (var col in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        Bounds bb;
        if (GetBounds(m, out bb) && bb.size.y > 0.01f)
        {
            m.transform.localScale *= 1.62f / bb.size.y;
            GetBounds(m, out bb);
            m.transform.position += Vector3.up * (seatFloor.y - bb.min.y);
        }
        var an = m.GetComponent<Animator>();
        if (an == null) an = m.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;
        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PersonMaterials(m, model);
        report.Add("Mom: " + model + (sit != null ? " + " + sit : ""));
        return m;
    }

    // ---------- شخصيات جانبية (الجدة والضيوف) ----------
    // حطي ملفاتهم في Assets/KitchenRush/People (Granny.fbx, Aj.fbx ...)
    // كلهم يجلسون بنفس حركة جلوس الأم
    const string PeopleFolder = "Assets/KitchenRush/People";

    static GameObject PlaceGuest(Transform parent, string key, Vector3 seatFloor, float yaw, float height = 1.62f)
    {
        if (!AssetDatabase.IsValidFolder(PeopleFolder)) { report.Add("Guest " + key + ": no People folder"); return null; }
        string model = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { PeopleFolder }))
        {
            string pth = AssetDatabase.GUIDToAssetPath(guid);
            if (Norm(pth).Contains(key.ToLower())) { model = pth; break; }
        }
        if (model == null) { report.Add("Guest " + key + ": not found"); return null; }
        var imp = AssetImporter.GetAtPath(model) as ModelImporter;
        if (imp != null && imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; imp.SaveAndReimport(); }
        var ctrl = SitController();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        if (prefab == null) return null;
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        m.name = "Guest_" + key;
        m.transform.SetParent(parent, true);
        m.transform.position = seatFloor;
        m.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        foreach (var col in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        Bounds bb;
        if (GetBounds(m, out bb) && bb.size.y > 0.01f)
        {
            m.transform.localScale *= height / bb.size.y;
            GetBounds(m, out bb);
            m.transform.position += Vector3.up * (seatFloor.y - bb.min.y);
        }
        var an = m.GetComponent<Animator>();
        if (an == null) an = m.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;
        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PersonMaterials(m, model);
        report.Add("Guest " + key + ": " + model);
        return m;
    }

    // حركة الجلوس (من Mom@Sitting) لأي شخصية
    static RuntimeAnimatorController SitController()
    {
        string path = CharFolder + "/SitAnimator.controller";
        var have = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (have != null) return have;
        string sit = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CharFolder }))
        {
            string pth = AssetDatabase.GUIDToAssetPath(guid);
            if (Norm(pth).Contains("sit")) { sit = pth; break; }
        }
        if (sit == null) return null;
        var imp = AssetImporter.GetAtPath(sit) as ModelImporter;
        if (imp != null)
        {
            bool need = imp.animationType != ModelImporterAnimationType.Human;
            imp.animationType = ModelImporterAnimationType.Human;
            var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
            if (imp.clipAnimations.Length == 0) need = true;
            foreach (var c in clips)
            {
                c.loopTime = true; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = false;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            if (need && clips.Length > 0) { imp.clipAnimations = clips; imp.SaveAndReimport(); }
        }
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var st = ctrl.layers[0].stateMachine.AddState("Sit");
        st.motion = Clip(sit);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // صور الشخصية داخل ملف الـ FBX: نطلعها ونسوي مادة URP منها (عشان ما تطلع بيضا)
    static void PersonMaterials(GameObject inst, string modelPath)
    {
        string key = Path.GetFileNameWithoutExtension(modelPath).Replace(" ", "_").Replace("@", "_");
        string baseDir = Root + "/PeopleTex";
        if (!AssetDatabase.IsValidFolder(baseDir)) AssetDatabase.CreateFolder(Root, "PeopleTex");
        string dir = baseDir + "/" + key;
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder(baseDir, key);
            var imp = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (imp != null)
            {
                try { imp.ExtractTextures(dir); } catch (System.Exception e) { Debug.LogWarning("Kitchen Rush: textures " + key + ": " + e.Message); }
                AssetDatabase.Refresh();
            }
        }
        Texture2D diff = null, nrm = null;
        string nrmPath = null;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
        {
            string pth = AssetDatabase.GUIDToAssetPath(guid);
            string n = Norm(pth);
            if (diff == null && (n.Contains("diffuse") || n.Contains("albedo") || n.Contains("basecolor"))) diff = AssetDatabase.LoadAssetAtPath<Texture2D>(pth);
            else if (nrm == null && n.Contains("normal")) { nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(pth); nrmPath = pth; }
        }
        if (diff == null) return;
        if (nrmPath != null)
        {
            var ti = AssetImporter.GetAtPath(nrmPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
        }
        var mat = Mat("Person_" + key, Color.white);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
        if (nrm != null && mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", nrm); mat.EnableKeyword("_NORMALMAP"); }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(mat);
        foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.sharedMaterials = arr;
        }
    }

    // لو الشخصية بيضا أو وردية: مادة URP بنفس اللون
    static void FixPinkInstance(GameObject m)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return;
        foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            foreach (var mat in r.sharedMaterials)
                if (mat != null && (mat.shader == null || !mat.shader.isSupported || mat.shader.name == "Standard"))
                    Debug.Log("Kitchen Rush: Mom material " + mat.name + " may need URP conversion.");
    }

    static void Bookshelf(Transform parent, Vector3 bottom)
    {
        var p = new GameObject("Bookshelf");
        p.transform.SetParent(parent, true);
        p.transform.position = bottom;
        var wood = Mat("Shelf_Wood", ShelfWood);
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0.15f), new Vector3(1.1f, 1.8f, 0.05f), wood);
        Part(p, PrimitiveType.Cube, new Vector3(-0.53f, 0.9f, 0f), new Vector3(0.05f, 1.8f, 0.35f), wood);
        Part(p, PrimitiveType.Cube, new Vector3(0.53f, 0.9f, 0f), new Vector3(0.05f, 1.8f, 0.35f), wood);
        for (int i = 0; i < 4; i++)
        {
            float y = 0.05f + i * 0.58f;
            Part(p, PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(1.1f, 0.05f, 0.35f), wood);
            if (i == 3) continue;
            if (i == 1) { DutchOven(p.transform, bottom + new Vector3(-0.2f, y + 0.025f, 0f), 2); CutePlant(p.transform, bottom + new Vector3(0.25f, y + 0.025f, 0f), 0.4f, 5); continue; }
            for (int b = 0; b < 5; b++)
                Part(p, PrimitiveType.Cube, new Vector3(-0.4f + b * 0.16f, y + 0.18f, 0f), new Vector3(0.12f, 0.3f, 0.25f), Mat("Book_" + b, Pastels[(b * 2 + i) % Pastels.Length]));
        }
    }

    // لوحة فواكه ملونة
    static void ArtFrame(Transform parent, Vector3 pos, float yRot, int seed)
    {
        var p = new GameObject("Art");
        p.transform.SetParent(parent, true);
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        float w = seed == 1 ? 0.45f : 0.55f, h = seed == 1 ? 0.55f : 0.7f;
        Part(p, PrimitiveType.Cube, Vector3.zero, new Vector3(w, h, 0.03f), Mat("Frame_" + (seed % 2), seed % 2 == 0 ? new Color(0.12f, 0.1f, 0.1f) : ShelfWood));
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0f, -0.017f), new Vector3(w - 0.07f, h - 0.07f, 0.01f), Mat("Canvas_" + seed, Color.Lerp(Pastels[(seed * 3 + 3) % Pastels.Length], Color.white, 0.75f)));
        for (int i = 0; i < 3; i++)
        {
            Color fc = Pastels[(seed + i * 2) % Pastels.Length];
            Part(p, PrimitiveType.Sphere, new Vector3((i - 1) * 0.12f, (i % 2) * 0.12f - 0.05f, -0.03f), new Vector3(0.12f, 0.12f, 0.02f), Mat("Fruit_" + ColorUtility.ToHtmlStringRGB(fc), fc));
        }
    }

    static void WavyMirror(Transform parent, Vector3 pos)
    {
        var p = new GameObject("WavyMirror");
        p.transform.SetParent(parent, true);
        p.transform.position = pos;
        p.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        var frame = Mat("Mirror_Frame", ShelfWood);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Part(p, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.38f, 0f), new Vector3(0.2f, 0.2f, 0.03f), frame);
        }
        Part(p, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.02f), new Vector3(0.6f, 0.01f, 0.75f), Mat("Mirror_Glass", new Color(0.85f, 0.9f, 0.92f))).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ---------- المحطات ----------

    static readonly Color Wood = new Color(0.72f, 0.55f, 0.38f);
    static readonly Color WoodTop = new Color(0.93f, 0.9f, 0.84f);

    static GameObject Tile(GameObject root, string name, Vector3 pos)
    {
        var t = new GameObject(name);
        t.transform.SetParent(root.transform);
        t.transform.position = pos;
        var bc = t.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, CounterH / 2f, 0f);
        bc.size = new Vector3(1f, CounterH, 1f);
        return t;
    }

    static void CounterBody(GameObject t, float face, Color body, Color top, GameObject model = null)
    {
        var m = model != null ? model : mCounter;
        if (m != null)
        {
            Fit(m, t.transform, Vector3.zero, new Vector3(1f, CounterH, 1f), true, face);
            return;
        }
        // كبت أبيض بسطح رمادي ومقابض (وجهه لداخل المطبخ)
        var cab = new GameObject("Cabinet");
        cab.transform.SetParent(t.transform, false);
        cab.transform.localRotation = Quaternion.Euler(0f, face, 0f);
        var bodyM = Mat("Cabinet_" + ColorUtility.ToHtmlStringRGB(body), body);
        var line = Mat("Cabinet_Line", new Color(0.78f, 0.77f, 0.73f));
        var handle = Mat("Cabinet_Handle", new Color(0.72f, 0.6f, 0.42f));
        Part(cab, PrimitiveType.Cube, new Vector3(0f, (CounterH - 0.05f) / 2f + 0.04f, 0f), new Vector3(0.98f, CounterH - 0.09f, 0.94f), bodyM);
        Part(cab, PrimitiveType.Cube, new Vector3(0f, CounterH - 0.025f, 0.01f), new Vector3(1.0f, 0.05f, 1.0f), MatS("Counter_TopGrey", TopGrey, 0.55f));
        Part(cab, PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.03f), new Vector3(0.94f, 0.08f, 0.86f), Mat("Counter_Kick", new Color(0.55f, 0.52f, 0.48f)));
        Part(cab, PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.472f), new Vector3(0.94f, 0.012f, 0.01f), line);
        Part(cab, PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.472f), new Vector3(0.012f, 0.56f, 0.01f), line);
        Part(cab, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.485f), new Vector3(0.22f, 0.025f, 0.025f), handle);
        Part(cab, PrimitiveType.Cube, new Vector3(-0.08f, 0.5f, 0.485f), new Vector3(0.025f, 0.12f, 0.025f), handle);
        Part(cab, PrimitiveType.Cube, new Vector3(0.08f, 0.5f, 0.485f), new Vector3(0.025f, 0.12f, 0.025f), handle);
    }

    // فرن ستيل بشباك زجاج منوّر ومقبض وأزرار، وفوقه موقد أسود بأربع عيون، وعليه فوط بورود
    static void OvenFront(GameObject t, float face, int seed)
    {
        var g = new GameObject("Oven");
        g.transform.SetParent(t.transform, false);
        g.transform.localRotation = Quaternion.Euler(0f, face, 0f);
        var steel = MatS("Oven_Steel", new Color(0.78f, 0.8f, 0.82f), 0.75f);
        var black = MatS("Oven_Black", new Color(0.08f, 0.08f, 0.09f), 0.85f);
        var glow = MatGlow("Oven_Inside", new Color(1f, 0.6f, 0.25f), 0.9f);
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.42f, 0.478f), new Vector3(0.86f, 0.66f, 0.02f), steel).name = "Oven_Door";
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.49f), new Vector3(0.64f, 0.38f, 0.01f), black).name = "Oven_Glass";
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0.486f), new Vector3(0.5f, 0.22f, 0.01f), glow).name = "Oven_Light";
        Part(g, PrimitiveType.Cylinder, new Vector3(0f, 0.65f, 0.53f), new Vector3(0.025f, 0.34f, 0.025f), steel).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Part(g, PrimitiveType.Cube, new Vector3(-0.3f, 0.65f, 0.51f), new Vector3(0.02f, 0.02f, 0.05f), steel);
        Part(g, PrimitiveType.Cube, new Vector3(0.3f, 0.65f, 0.51f), new Vector3(0.02f, 0.02f, 0.05f), steel);
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.478f), new Vector3(0.86f, 0.1f, 0.02f), black);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.32f + i * 0.2f + (i > 1 ? 0.04f : 0f);
            Part(g, PrimitiveType.Cylinder, new Vector3(x, 0.8f, 0.5f), new Vector3(0.05f, 0.012f, 0.05f), steel).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        Part(g, PrimitiveType.Cube, new Vector3(0.02f, 0.8f, 0.489f), new Vector3(0.1f, 0.04f, 0.005f), MatGlow("Oven_Display", new Color(0.3f, 0.9f, 0.6f), 1.2f));
        Part(g, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.004f, 0.02f), new Vector3(0.8f, 0.01f, 0.7f), black).name = "Cooktop";
        for (int bx = -1; bx <= 1; bx += 2)
            for (int bz = -1; bz <= 1; bz += 2)
            {
                Part(g, PrimitiveType.Cylinder, new Vector3(bx * 0.18f, CounterH + 0.01f, 0.02f + bz * 0.16f), new Vector3(0.22f, 0.002f, 0.22f), MatS("Burner_Ring", new Color(0.3f, 0.3f, 0.32f), 0.6f));
                Part(g, PrimitiveType.Cylinder, new Vector3(bx * 0.18f, CounterH + 0.012f, 0.02f + bz * 0.16f), new Vector3(0.16f, 0.002f, 0.16f), black);
            }
        for (int i = 0; i < 2; i++)
        {
            var towel = Part(g, PrimitiveType.Cube, new Vector3(-0.14f + i * 0.3f, 0.5f, 0.545f), new Vector3(0.2f, 0.32f, 0.012f), TexMat("Towel_" + ((seed + i) % 2), "Floral_" + ((seed + i) % 2), FloralTex((seed + i) % 2), new Vector2(1f, 2f)));
            towel.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 3f : -2f);
        }
    }

    static void Counter(GameObject root, string name, Vector3 pos, float face, bool oven = false)
    {
        var t = Tile(root, name, pos);
        CounterBody(t, face, pos.z < 0f ? DustyBlue : CabinetWhite, TopGrey);
        if (oven) OvenFront(t, face, Mathf.RoundToInt(pos.x));
        t.AddComponent<Station>();
    }

    static void CrateTile(GameObject root, Vector3 pos, float face, Ingredient ing)
    {
        var t = Tile(root, "Crate_" + ing, pos);
        CounterBody(t, face, CabinetWhite, TopGrey);
        var crateMat = Mat("Crate", new Color(0.62f, 0.4f, 0.22f));
        var slat = Mat("Crate_Dark", new Color(0.45f, 0.28f, 0.15f));
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.03f, 0f), new Vector3(0.82f, 0.06f, 0.82f), slat);
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.14f, 0.38f), new Vector3(0.82f, 0.22f, 0.06f), crateMat);
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.14f, -0.38f), new Vector3(0.82f, 0.22f, 0.06f), crateMat);
        Part(t, PrimitiveType.Cube, new Vector3(0.38f, CounterH + 0.14f, 0f), new Vector3(0.06f, 0.22f, 0.7f), crateMat);
        Part(t, PrimitiveType.Cube, new Vector3(-0.38f, CounterH + 0.14f, 0f), new Vector3(0.06f, 0.22f, 0.7f), crateMat);

        var im = ItemMat(ing);
        GameObject food;
        mFood.TryGetValue(ing, out food);
        Vector3[] spots = { new Vector3(-0.17f, 0, -0.16f), new Vector3(0.16f, 0, -0.15f), new Vector3(-0.15f, 0, 0.16f), new Vector3(0.17f, 0, 0.17f), new Vector3(0f, 0.08f, 0f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 p = new Vector3(spots[i].x, CounterH + 0.06f + spots[i].y, spots[i].z);
            if (food != null) Fit(food, t.transform, p, Vector3.one * 0.3f, false, i * 67f);
            else Part(t, PrimitiveType.Sphere, p + Vector3.up * 0.1f, Vector3.one * 0.22f, im);
        }

        var crate = t.AddComponent<Crate>();
        crate.ingredient = ing;
        crate.itemMaterial = im;
        crate.itemPrefab = food;
        crate.topExtra = 0.3f;
    }

    static void ChopTile(GameObject root, Vector3 pos, float face)
    {
        // لوح التقطيع: Space يقطّع
        var t = Tile(root, "ChopBoard", pos);
        CounterBody(t, face, CabinetWhite, TopGrey);
        if (mBoard != null) Fit(mBoard, t.transform, new Vector3(0f, CounterH, 0f), new Vector3(0.75f, 0.12f, 0.55f), false, 0f);
        else Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.025f, 0f), new Vector3(0.7f, 0.05f, 0.5f), Mat("Board", new Color(0.94f, 0.82f, 0.6f)));
        if (mKnife != null) Fit(mKnife, t.transform, new Vector3(0.1f, CounterH + 0.06f, 0.2f), new Vector3(0.4f, 0.06f, 0.4f), false, 90f);
        else
        {
            Part(t, PrimitiveType.Cube, new Vector3(0.12f, CounterH + 0.06f, 0.15f), new Vector3(0.32f, 0.02f, 0.06f), Mat("Steel", new Color(0.8f, 0.83f, 0.86f)));
            Part(t, PrimitiveType.Cube, new Vector3(-0.1f, CounterH + 0.065f, 0.15f), new Vector3(0.12f, 0.03f, 0.06f), Mat("Handle", new Color(0.24f, 0.15f, 0.12f)));
        }
        t.AddComponent<ChopStation>().topExtra = 0.05f;
    }

    static void PlatesTile(GameObject root, Vector3 pos, float face)
    {
        // كومة الصحون
        var t = Tile(root, "Plates", pos);
        CounterBody(t, face, CabinetWhite, TopGrey);
        var white = Mat("Plate", new Color(0.97f, 0.97f, 0.97f));
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = new Vector3(0f, CounterH + i * 0.045f, 0f);
            if (mPlate != null) Fit(mPlate, t.transform, p, new Vector3(0.55f, 0.05f, 0.55f), false, 0f);
            else Part(t, PrimitiveType.Cylinder, p + Vector3.up * 0.02f, new Vector3(0.5f, 0.02f, 0.5f), white);
        }
        var ps = t.AddComponent<PlateStack>();
        ps.topExtra = 0.2f;
        ps.plateModel = mPlate;
        ps.plateMaterial = white;
    }

    static void ServeTile(GameObject root, Vector3 pos)
    {
        // شباك التسليم
        var t = Tile(root, "ServeWindow", pos);
        var green = Mat("Serve_Body", new Color(0.27f, 0.55f, 0.2f));
        var glow = MatGlow("Serve_Top", new Color(0.55f, 0.85f, 0.3f), 0.6f);
        Part(t, PrimitiveType.Cube, new Vector3(0f, (CounterH - 0.08f) / 2f, 0f), new Vector3(0.96f, CounterH - 0.08f, 0.96f), green);
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH - 0.04f, 0f), new Vector3(1f, 0.08f, 1f), glow);
        Part(t, PrimitiveType.Cylinder, new Vector3(0f, CounterH + 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.14f), Mat("Bell_Base", new Color(0.2f, 0.2f, 0.2f)));
        Part(t, PrimitiveType.Sphere, new Vector3(0f, CounterH + 0.08f, 0f), new Vector3(0.16f, 0.12f, 0.16f), Mat("Bell", new Color(0.96f, 0.71f, 0f)));
        t.AddComponent<ServeWindow>();
    }

    static void TrashTile(GameObject root, Vector3 pos, float face)
    {
        var t = Tile(root, "Trash", pos);
        if (mTrash != null) Fit(mTrash, t.transform, Vector3.zero, new Vector3(0.75f, 0.95f, 0.75f), false, face);
        else
        {
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.65f, 0.42f, 0.65f), Mat("TrashBin", new Color(0.32f, 0.34f, 0.36f)));
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.02f, 0.7f), Mat("TrashLid", new Color(0.2f, 0.21f, 0.22f)));
        }
        t.AddComponent<Trash>();
    }

    // الشواية: صندوق ستيل أسود، فحم يتوهج، وشبك فوقه
    static void GrillTile(GameObject root, Vector3 pos, float face)
    {
        var t = Tile(root, "Grill", pos);
        var g = new GameObject("GrillBody");
        g.transform.SetParent(t.transform, false);
        g.transform.localRotation = Quaternion.Euler(0f, face, 0f);
        var body = MatS("Grill_Black", new Color(0.12f, 0.12f, 0.13f), 0.6f);
        var steel = MatS("Grill_Steel", new Color(0.7f, 0.72f, 0.75f), 0.8f);
        var red = MatS("Grill_Red", new Color(0.82f, 0.25f, 0.2f), 0.55f);
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(0.96f, 0.84f, 0.94f), body);
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.475f), new Vector3(0.8f, 0.5f, 0.02f), red).name = "Grill_Door";
        Part(g, PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0.51f), new Vector3(0.025f, 0.28f, 0.025f), steel).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (int i = 0; i < 3; i++)
            Part(g, PrimitiveType.Cylinder, new Vector3(-0.2f + i * 0.2f, 0.78f, 0.49f), new Vector3(0.06f, 0.015f, 0.06f), steel).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // الفحم
        var coals = Part(g, PrimitiveType.Cube, new Vector3(0f, CounterH - 0.03f, 0f), new Vector3(0.82f, 0.04f, 0.8f), MatGlow("Grill_Coals", new Color(1f, 0.42f, 0.1f), 1.2f));
        coals.name = "Coals";
        // الشبك
        for (int i = 0; i < 8; i++)
            Part(g, PrimitiveType.Cube, new Vector3(-0.35f + i * 0.1f, CounterH + 0.005f, 0f), new Vector3(0.025f, 0.02f, 0.82f), steel);
        Part(g, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.01f, 0.43f), new Vector3(0.96f, 0.04f, 0.06f), body);
        Part(g, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.01f, -0.43f), new Vector3(0.96f, 0.04f, 0.06f), body);
        var gr = t.AddComponent<Grill>();
        gr.topExtra = 0.02f;
        gr.coals = coals.GetComponent<Renderer>();
    }

    // ---------- الحوش (المرحلة ٢) ----------
    static readonly Color Grass = new Color(0.45f, 0.68f, 0.32f);
    static readonly Color HouseBlue = new Color(0.62f, 0.76f, 0.88f);

    static void BuildYard(GameObject root, int C, int R)
    {
        float W = C + Side * 2f;
        float z = R / 2f;

        // عشب كبير، وأرضية خشب (ديك) تحت مكان الطبخ
        var grass = Block(root, "Grass", new Vector3(0f, -0.12f, -1f), new Vector3(W + 10f, 0.2f, R + 10f), TexMat("Grass", "GrassTex", GrassTex(), new Vector2((W + 10f) / 3f, (R + 10f) / 3f)), true);
        var deck = Block(root, "Deck", new Vector3(0f, -0.1f, 0f), new Vector3(C + 0.6f, 0.2f, R + 0.3f), TexMat("Deck_Wood", "FloorWood", WoodTex(), new Vector2((C + 0.6f) / 4f, R / 2f)), true);
        Block(root, "Deck_Edge", new Vector3(0f, -0.06f, -R / 2f - 0.17f), new Vector3(C + 0.7f, 0.14f, 0.08f), Mat("Deck_Edge", new Color(0.55f, 0.38f, 0.24f)), false);

        // جدار البيت ورا (خشب أزرق) مع شبابيك وباب
        var siding = Mat("House_Blue", HouseBlue);
        Block(root, "House_Wall", new Vector3(0f, 1.5f, z + 0.15f), new Vector3(W + 0.6f, 3f, 0.3f), siding, true);
        var line = Mat("House_Line", new Color(0.52f, 0.66f, 0.8f));
        for (int i = 0; i < 9; i++) Block(root, "Siding", new Vector3(0f, 0.25f + i * 0.32f, z - 0.005f), new Vector3(W + 0.6f, 0.02f, 0.02f), line, false);
        var white = Mat("Trim_White", new Color(0.98f, 0.97f, 0.95f));
        foreach (float wx in new[] { -C / 4f - 0.3f, C / 4f + 0.3f })
        {
            BuildWindow(root, new Vector3(wx, 1.35f, z));
            Block(root, "Shutter", new Vector3(wx - 0.85f, 1.9f, z - 0.03f), new Vector3(0.2f, 1.1f, 0.04f), white, false);
            Block(root, "Shutter", new Vector3(wx + 0.85f, 1.9f, z - 0.03f), new Vector3(0.2f, 1.1f, 0.04f), white, false);
        }
        // باب بالنص
        Block(root, "Door", new Vector3(0f, 1.05f, z - 0.02f), new Vector3(0.9f, 2.1f, 0.05f), Mat("Door", new Color(0.95f, 0.55f, 0.35f)), false);
        Block(root, "Door_Frame", new Vector3(0f, 2.13f, z - 0.03f), new Vector3(1.05f, 0.08f, 0.06f), white, false);
        Part(root, PrimitiveType.Sphere, new Vector3(0.32f, 1.0f, z - 0.06f), Vector3.one * 0.07f, MatS("Brass", new Color(0.85f, 0.68f, 0.3f), 0.8f)).name = "Door_Knob";

        // أضواء معلقة بين الجدار والأعمدة قدام
        StringLights(root, new Vector3(-W / 2f + 0.2f, 2.5f, z - 0.05f), new Vector3(W / 2f - 0.2f, 2.5f, z - 0.05f), 0.35f, 0);

        // سور خشب أبيض يمين ويسار
        PicketFence(root, new Vector3(-W / 2f, 0f, -R / 2f - 0.2f), new Vector3(-W / 2f, 0f, z));
        PicketFence(root, new Vector3(W / 2f, 0f, -R / 2f - 0.2f), new Vector3(W / 2f, 0f, z));

        // جدران مخفية: الطباخة تبقى داخل منطقة اللعب
        Invisible(root, "Wall_Front (invisible)", new Vector3(0f, 1f, -R / 2f - 0.15f), new Vector3(W, 2f, 0.3f));
        if (!serveAtTable) Invisible(root, "Wall_PlayLeft (invisible)", new Vector3(-C / 2f - 0.1f, 1f, 0f), new Vector3(0.2f, 2f, R));
        Invisible(root, "Wall_PlayRight (invisible)", new Vector3(C / 2f + 0.1f, 1f, 0f), new Vector3(0.2f, 2f, R));
        Invisible(root, "Wall_Left (invisible)", new Vector3(-W / 2f + 0.1f, 1f, 0f), new Vector3(0.2f, 2f, R));

        float lx = -C / 2f - Side / 2f, rx = C / 2f + Side / 2f;

        // يسار: طاولة الحفلة (الأم والضيوف) + بالونات
        PicnicTable(root.transform, new Vector3(lx, 0f, -R / 2f + 1.4f));
        Balloons(root.transform, new Vector3(lx - 0.6f, 0f, z - 0.5f), 0);

        // يمين: ثلاجة مشروبات، شجرة، بالونات
        Cooler(root.transform, new Vector3(rx, 0f, -R / 2f + 0.9f));
        Tree(root.transform, new Vector3(rx + 0.3f, 0f, z - 0.6f), 1);
        Balloons(root.transform, new Vector3(rx - 0.5f, 0f, z - 0.3f), 1);
        CutePlant(root.transform, new Vector3(lx + 0.8f, 0f, z - 0.35f), 0.7f, 4);
    }

    static void PicketFence(GameObject root, Vector3 a, Vector3 b)
    {
        var f = new GameObject("Fence");
        f.transform.SetParent(root.transform);
        var white = Mat("Fence_White", new Color(0.97f, 0.96f, 0.93f));
        float len = Vector3.Distance(a, b);
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.22f));
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, i / (float)n);
            Part(f, PrimitiveType.Cube, p + new Vector3(0f, 0.45f, 0f), new Vector3(0.09f, 0.9f, 0.04f), white).transform.rotation = Quaternion.LookRotation(b - a);
            Part(f, PrimitiveType.Cube, p + new Vector3(0f, 0.92f, 0f), new Vector3(0.065f, 0.065f, 0.04f), white).transform.rotation = Quaternion.LookRotation(b - a) * Quaternion.Euler(0f, 0f, 45f);
        }
        foreach (float y in new[] { 0.25f, 0.7f })
        {
            var rail = Part(f, PrimitiveType.Cube, (a + b) / 2f + new Vector3(0f, y, 0f), new Vector3(0.03f, 0.07f, len), white);
            rail.transform.rotation = Quaternion.LookRotation(b - a);
        }
    }

    static void StringLights(GameObject root, Vector3 a, Vector3 b, float sag, int seed)
    {
        var g = new GameObject("String_Lights");
        g.transform.SetParent(root.transform);
        var wire = Mat("Wire", new Color(0.15f, 0.15f, 0.15f));
        Color[] cols = { new Color(1f, 0.85f, 0.5f), new Color(1f, 0.55f, 0.6f), new Color(0.6f, 0.8f, 1f), new Color(0.75f, 1f, 0.6f) };
        int n = Mathf.RoundToInt(Vector3.Distance(a, b) / 0.35f);
        Vector3 prev = a;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t) * 1f;
            p.y -= Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * sag;
            var w = Part(g, PrimitiveType.Cube, (prev + p) / 2f, new Vector3(0.01f, 0.01f, Vector3.Distance(prev, p)), wire);
            w.transform.rotation = Quaternion.LookRotation(p - prev);
            if (i < n) Part(g, PrimitiveType.Sphere, p + Vector3.down * 0.05f, new Vector3(0.07f, 0.09f, 0.07f), MatGlow("Bulb_" + ((i + seed) % cols.Length), cols[(i + seed) % cols.Length], 2.2f));
            prev = p;
        }
    }

    static void Balloons(Transform parent, Vector3 bottom, int seed)
    {
        var g = new GameObject("Balloons");
        g.transform.SetParent(parent, true);
        g.transform.position = bottom;
        Color[] cols = { new Color(1f, 0.48f, 0.66f), new Color(0.42f, 0.62f, 0.86f), new Color(1f, 0.82f, 0.35f) };
        var str = Mat("Balloon_String", new Color(0.9f, 0.9f, 0.9f));
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.1f, 0.12f), Mat("Balloon_Weight", new Color(0.5f, 0.45f, 0.5f)));
        for (int i = 0; i < 3; i++)
        {
            Vector3 top = new Vector3((i - 1) * 0.22f, 1.75f + (i % 2) * 0.22f, (i % 2) * 0.12f);
            var s = Part(g, PrimitiveType.Cylinder, top / 2f, new Vector3(0.008f, top.magnitude / 2f, 0.008f), str);
            s.transform.localRotation = Quaternion.FromToRotation(Vector3.up, top);
            Part(g, PrimitiveType.Sphere, top + Vector3.up * 0.15f, new Vector3(0.28f, 0.34f, 0.28f), MatS("Balloon_" + ((i + seed) % 3), cols[(i + seed) % 3], 0.85f));
        }
    }

    static void Tree(Transform parent, Vector3 bottom, int seed)
    {
        var g = new GameObject("Tree");
        g.transform.SetParent(parent, true);
        g.transform.position = bottom;
        Part(g, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.22f, 0.9f, 0.22f), Mat("Bark", new Color(0.45f, 0.32f, 0.22f)));
        var l1 = Mat("Leaf", new Color(0.3f, 0.58f, 0.3f));
        var l2 = Mat("Leaf_Light", new Color(0.46f, 0.72f, 0.36f));
        Part(g, PrimitiveType.Sphere, new Vector3(0f, 2.2f, 0f), new Vector3(1.4f, 1.1f, 1.4f), l1);
        Part(g, PrimitiveType.Sphere, new Vector3(-0.35f, 2.0f, -0.25f), new Vector3(0.9f, 0.8f, 0.9f), l2);
        Part(g, PrimitiveType.Sphere, new Vector3(0.4f, 2.5f, 0.1f), new Vector3(0.8f, 0.7f, 0.8f), l2);
    }

    static void Cooler(Transform parent, Vector3 bottom)
    {
        var g = new GameObject("Cooler");
        g.transform.SetParent(parent, true);
        g.transform.position = bottom;
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.7f, 0.44f, 0.45f), MatS("Cooler_Blue", new Color(0.3f, 0.55f, 0.85f), 0.5f));
        Part(g, PrimitiveType.Cube, new Vector3(0f, 0.47f, 0f), new Vector3(0.74f, 0.07f, 0.49f), Mat("Trim_White", new Color(0.98f, 0.97f, 0.95f)));
        Color[] cans = { new Color(0.9f, 0.25f, 0.25f), new Color(0.3f, 0.75f, 0.4f), new Color(1f, 0.75f, 0.2f) };
        for (int i = 0; i < 3; i++)
            Part(g, PrimitiveType.Cylinder, new Vector3(-0.2f + i * 0.2f, 0.57f, 0f), new Vector3(0.07f, 0.06f, 0.07f), MatS("Can_" + i, cans[i], 0.7f));
    }

    // طاولة الحفلة: مفرش مربعات، كراسي، لمبة. التسليم هنا للأم والضيوف
    static void PicnicTable(Transform parent, Vector3 center)
    {
        var p = new GameObject("PartyTable");
        p.transform.SetParent(parent, true);
        p.transform.position = center;
        var wood = MatS("Picnic_Wood", new Color(0.78f, 0.58f, 0.4f), 0.3f);
        var cloth = TexMat("Picnic_Cloth", "CheckerRed", CheckerSmallTex(new Color(0.9f, 0.32f, 0.32f)), new Vector2(3f, 2f));
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(1.5f, 0.05f, 0.85f), wood);
        Part(p, PrimitiveType.Cube, new Vector3(0f, 0.77f, 0f), new Vector3(1.3f, 0.01f, 0.88f), cloth);
        for (int a = -1; a <= 1; a += 2)
            Part(p, PrimitiveType.Cube, new Vector3(a * 0.6f, 0.37f, 0f), new Vector3(0.06f, 0.74f, 0.7f), wood);
        // كراسي ورا وقدام
        var seat = Mat("Chair_Blue", new Color(0.4f, 0.6f, 0.85f));
        var chrome = MatS("Chrome", new Color(0.85f, 0.86f, 0.88f), 0.85f);
        foreach (float cx in new[] { -0.35f, 0.35f })
            for (int s = -1; s <= 1; s += 2)
            {
                var ch = new GameObject(s > 0 && cx < 0f ? "Chair_Mom" : "Chair");
                ch.transform.SetParent(p.transform, false);
                ch.transform.localPosition = new Vector3(cx, 0f, s * 0.68f);
                ch.transform.localRotation = Quaternion.Euler(0f, s > 0 ? 0f : 180f, 0f);
                Part(ch, PrimitiveType.Cube, new Vector3(0f, 0.46f, 0f), new Vector3(0.42f, 0.06f, 0.42f), seat);
                Part(ch, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.2f), new Vector3(0.42f, 0.36f, 0.05f), seat);
                for (int a = -1; a <= 1; a += 2)
                    for (int b = -1; b <= 1; b += 2)
                        Part(ch, PrimitiveType.Cylinder, new Vector3(a * 0.18f, 0.22f, b * 0.18f), new Vector3(0.03f, 0.22f, 0.03f), chrome);
            }
        // صحون وعصير على الطاولة
        var plate = Mat("Plate", new Color(0.97f, 0.97f, 0.97f));
        Part(p, PrimitiveType.Cylinder, new Vector3(-0.35f, 0.785f, 0.25f), new Vector3(0.28f, 0.01f, 0.28f), plate);
        Part(p, PrimitiveType.Cylinder, new Vector3(0.35f, 0.785f, -0.25f), new Vector3(0.28f, 0.01f, 0.28f), plate);
        Part(p, PrimitiveType.Cylinder, new Vector3(0.45f, 0.88f, 0.2f), new Vector3(0.14f, 0.11f, 0.14f), MatS("Lemonade", new Color(1f, 0.9f, 0.45f), 0.9f));
        Vase(p.transform, center + new Vector3(0f, 0.78f, 0f));

        var bc = p.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.39f, 0f);
        bc.size = new Vector3(1.5f, 0.78f, 0.9f);
        var chairs = p.AddComponent<BoxCollider>();
        chairs.center = new Vector3(0f, 0.5f, 0.7f);
        chairs.size = new Vector3(1.2f, 1f, 0.5f);
        var sv = p.AddComponent<ServeWindow>();
        sv.hint = "[E] Serve the guests";
        PlaceMom(p.transform, center + new Vector3(-0.35f, 0f, 0.72f));
        // الجدة والضيوف حول الطاولة
        PlaceGuest(p.transform, "granny", center + new Vector3(0.35f, 0f, 0.72f), 180f, 1.58f);
        PlaceGuest(p.transform, "aj", center + new Vector3(-0.35f, 0f, -0.72f), 0f, 1.55f);
        PlaceGuest(p.transform, "ch03", center + new Vector3(0.35f, 0f, -0.72f), 0f, 1.7f);
        var front = p.AddComponent<BoxCollider>();
        front.center = new Vector3(0f, 0.5f, -0.7f);
        front.size = new Vector3(1.2f, 1f, 0.5f);
    }

    static Texture2D GrassTex()
    {
        int n = 128;
        var tex = new Texture2D(n, n);
        tex.wrapMode = TextureWrapMode.Repeat;
        var rnd = new System.Random(5);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f + (float)rnd.NextDouble() * 0.06f;
                tex.SetPixel(x, y, new Color(Grass.r - 0.05f + v, Grass.g - 0.05f + v, Grass.b - 0.04f + v * 0.5f));
            }
        tex.Apply();
        return tex;
    }

    // ---------- الطباخة ----------

    static void BuildChef(Vector3 pos)
    {
        var chef = new GameObject("Chef");   // نينا
        Undo.RegisterCreatedObjectUndo(chef, "Build Chef");
        chef.transform.position = pos;
        chef.tag = "Player";

        var cc = chef.AddComponent<CharacterController>();
        cc.height = 1.6f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.8f, 0f);

        // ===== نينا =====
        // الأسماء مهمة: ChefSkin يلوّن حسب أول الاسم (Coat / Apron / Pants / Hat / Hair / Bow)
        var white = Mat("Chef_White", new Color(0.97f, 0.97f, 0.95f));
        var dark = Mat("Chef_Pants", new Color(0.17f, 0.18f, 0.23f));
        var skin = Mat("Chef_Skin", new Color(0.98f, 0.82f, 0.7f));
        var red = Mat("Chef_Apron", new Color(0.89f, 0.34f, 0.18f));
        var hairM = Mat("Chef_Hair", new Color(0.29f, 0.17f, 0.14f));
        var shoe = Mat("Chef_Shoes", new Color(0.25f, 0.18f, 0.2f));
        var eye = Mat("Chef_Eyes", new Color(0.12f, 0.08f, 0.1f));
        var shine = MatGlow("Chef_EyeShine", Color.white, 0.4f);
        var blush = Mat("Chef_Blush", new Color(1f, 0.55f, 0.6f));

        // رجلين وجزمة
        Part(chef, PrimitiveType.Capsule, new Vector3(-0.12f, 0.2f, 0f), new Vector3(0.18f, 0.16f, 0.2f), dark).name = "Pants_L";
        Part(chef, PrimitiveType.Capsule, new Vector3(0.12f, 0.2f, 0f), new Vector3(0.18f, 0.16f, 0.2f), dark).name = "Pants_R";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.12f, 0.06f, 0.04f), new Vector3(0.2f, 0.12f, 0.26f), shoe).name = "Shoe_L";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.12f, 0.06f, 0.04f), new Vector3(0.2f, 0.12f, 0.26f), shoe).name = "Shoe_R";

        // جسم ومريلة وفيونكة
        Part(chef, PrimitiveType.Capsule, new Vector3(0f, 0.72f, 0f), new Vector3(0.58f, 0.4f, 0.5f), white).name = "Coat_Body";
        Part(chef, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.25f), new Vector3(0.38f, 0.36f, 0.04f), red).name = "Apron";
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0.255f), new Vector3(0.12f, 0.08f, 0.04f), red).name = "Apron_Pocket";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.08f, 1.0f, 0.24f), new Vector3(0.14f, 0.1f, 0.06f), red).name = "Bow_L";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.08f, 1.0f, 0.24f), new Vector3(0.14f, 0.1f, 0.06f), red).name = "Bow_R";
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0.26f), Vector3.one * 0.06f, red).name = "Bow_Knot";
        Part(chef, PrimitiveType.Capsule, new Vector3(-0.34f, 0.76f, 0.06f), new Vector3(0.15f, 0.2f, 0.15f), white).name = "Coat_ArmL";
        Part(chef, PrimitiveType.Capsule, new Vector3(0.34f, 0.76f, 0.06f), new Vector3(0.15f, 0.2f, 0.15f), white).name = "Coat_ArmR";

        // راس كبير كيوت
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 1.34f, 0f), new Vector3(0.56f, 0.54f, 0.54f), skin).name = "Head";
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 1.39f, -0.05f), new Vector3(0.6f, 0.58f, 0.54f), hairM).name = "Hair_Back";
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 1.54f, 0.07f), new Vector3(0.58f, 0.24f, 0.44f), hairM).name = "Hair_Bangs";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.33f, 1.2f, -0.04f), new Vector3(0.2f, 0.32f, 0.2f), hairM).name = "Hair_TailL";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.33f, 1.2f, -0.04f), new Vector3(0.2f, 0.32f, 0.2f), hairM).name = "Hair_TailR";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.3f, 1.37f, -0.04f), Vector3.one * 0.09f, red).name = "Bow_TieL";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.3f, 1.37f, -0.04f), Vector3.one * 0.09f, red).name = "Bow_TieR";

        // وجه: عيون كبيرة لامعة وخدود وفم صغير
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.1f, 1.33f, 0.245f), new Vector3(0.085f, 0.12f, 0.05f), eye).name = "Eye_L";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.1f, 1.33f, 0.245f), new Vector3(0.085f, 0.12f, 0.05f), eye).name = "Eye_R";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.085f, 1.36f, 0.27f), Vector3.one * 0.035f, shine).name = "Eye_ShineL";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.115f, 1.36f, 0.27f), Vector3.one * 0.035f, shine).name = "Eye_ShineR";
        Part(chef, PrimitiveType.Sphere, new Vector3(-0.17f, 1.26f, 0.21f), new Vector3(0.1f, 0.05f, 0.04f), blush).name = "Blush_L";
        Part(chef, PrimitiveType.Sphere, new Vector3(0.17f, 1.26f, 0.21f), new Vector3(0.1f, 0.05f, 0.04f), blush).name = "Blush_R";
        Part(chef, PrimitiveType.Sphere, new Vector3(0f, 1.24f, 0.26f), new Vector3(0.06f, 0.025f, 0.03f), eye).name = "Mouth";

        // قبعة طباخ صغيرة مايلة
        var hat = new GameObject("Hat");
        hat.transform.SetParent(chef.transform, false);
        hat.transform.localPosition = new Vector3(0.04f, 1.62f, -0.02f);
        hat.transform.localRotation = Quaternion.Euler(-6f, 0f, -12f);
        Part(hat, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.06f, 0.34f), white).name = "Hat_Band";
        Part(hat, PrimitiveType.Sphere, new Vector3(0f, 0.13f, 0f), new Vector3(0.42f, 0.26f, 0.42f), white).name = "Hat_Top";
        Part(hat, PrimitiveType.Sphere, new Vector3(-0.1f, 0.12f, 0.05f), new Vector3(0.24f, 0.2f, 0.24f), white).name = "Hat_PuffL";
        Part(hat, PrimitiveType.Sphere, new Vector3(0.1f, 0.12f, -0.05f), new Vector3(0.24f, 0.2f, 0.24f), white).name = "Hat_PuffR";

        var hand = new GameObject("Hand");
        hand.transform.SetParent(chef.transform, false);
        hand.transform.localPosition = new Vector3(0f, 0.95f, 0.55f);

        var ctrl = chef.AddComponent<ChefController>();
        ctrl.hand = hand.transform;
        chef.AddComponent<ChefSkin>();
    }

    // ---------- الكاميرا والإضاءة والألوان ----------

    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(g, "Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        Undo.RecordObject(cam.transform, "Camera");
        Undo.RecordObject(cam, "Camera");
        cam.transform.position = new Vector3(0f, 8.2f, -6.6f);
        cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        cam.fieldOfView = 44f;
        var follow = cam.GetComponent<KitchenCamera>();
        if (follow == null) follow = Undo.AddComponent<KitchenCamera>(cam.gameObject);
        var chefGo = GameObject.Find("Chef");
        if (chefGo != null) follow.target = chefGo.transform;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = outdoor ? new Color(0.98f, 0.8f, 0.62f) : new Color(0.16f, 0.12f, 0.1f);

        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            Undo.RecordObject(data, "Camera");
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
    }

    static void SetupLights(GameObject root, int C, int R)
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            var g = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(g, "Light");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        Undo.RecordObject(sun, "Light");
        Undo.RecordObject(sun.transform, "Light");
        // شمس غروب ذهبية داخلة من الشباك
        sun.color = new Color(1f, 0.78f, 0.52f);
        sun.intensity = 1.7f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sun.transform.rotation = Quaternion.Euler(32f, 155f, 0f);
        if (outdoor)
        {
            // عصرية برا: شمس أقوى
            sun.intensity = 2.1f;
            sun.color = new Color(1f, 0.86f, 0.66f);
            sun.transform.rotation = Quaternion.Euler(42f, 150f, 0f);
        }

        // ضوء محيط دافي
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.85f, 0.76f, 0.66f);
        RenderSettings.ambientEquatorColor = new Color(0.7f, 0.58f, 0.46f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.32f, 0.24f);

        // ضوء دافي من فوق
        foreach (float x in new[] { -C / 4f, C / 4f })
        {
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(root.transform);
            lamp.transform.position = new Vector3(x, 0f, 0.3f);
            // بدون شكل اللمبة عشان ما تغطي الطاولات، بس الضوء
            var pl = new GameObject("Lamp_Light").AddComponent<Light>();
            pl.transform.SetParent(lamp.transform, false);
            pl.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.8f, 0.58f);
            pl.range = 6f;
            pl.intensity = 1.4f;
        }
    }

    static void SetupPost(GameObject root)
    {
        EnsureFolders();
        string path = Root + "/KitchenPost.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        var bloom = Get<Bloom>(profile);
        bloom.intensity.Override(0.45f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.6f);

        var tone = Get<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);

        var color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0.45f);
        color.contrast.Override(10f);
        color.saturation.Override(4f);
        color.colorFilter.Override(new Color(1f, 0.95f, 0.88f));

        var wb = Get<WhiteBalance>(profile);
        wb.temperature.Override(18f);

        var vig = Get<Vignette>(profile);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.5f);

        EditorUtility.SetDirty(profile);

        var go = new GameObject("Kitchen Volume");
        go.transform.SetParent(root.transform);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = profile;
    }

    static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T comp;
        if (profile.TryGet(out comp)) return comp;
        comp = profile.Add<T>(true);
        comp.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(comp, profile);
        return comp;
    }

    // ---------- أدوات ----------

    // يحط مجسم ويكبّره أو يصغّره عشان يدخل في الحجم المطلوب، وقاعدته على النقطة
    static GameObject Fit(GameObject model, Transform parent, Vector3 bottomLocal, Vector3 size, bool stretch, float yRot)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (g == null) g = Object.Instantiate(model);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
        foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        Bounds b;
        if (!GetBounds(g, out b)) return g;
        Vector3 s = b.size;
        if (stretch)
        {
            bool side = Mathf.Abs(Mathf.DeltaAngle(yRot, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(yRot, 270f)) < 1f;
            float fx = size.x / Mathf.Max(0.001f, s.x);
            float fy = size.y / Mathf.Max(0.001f, s.y);
            float fz = size.z / Mathf.Max(0.001f, s.z);
            Vector3 ls = g.transform.localScale;
            g.transform.localScale = side ? new Vector3(ls.x * fz, ls.y * fy, ls.z * fx) : new Vector3(ls.x * fx, ls.y * fy, ls.z * fz);
        }
        else
        {
            float f = Mathf.Min(size.x / Mathf.Max(0.001f, s.x), Mathf.Min(size.y / Mathf.Max(0.001f, s.y), size.z / Mathf.Max(0.001f, s.z)));
            g.transform.localScale *= f;
        }
        GetBounds(g, out b);
        Vector3 target = parent.TransformPoint(bottomLocal);
        g.transform.position += target - new Vector3(b.center.x, b.min.y, b.center.z);
        return g;
    }

    static bool GetBounds(GameObject g, out Bounds b)
    {
        b = new Bounds(g.transform.position, Vector3.zero);
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return true;
    }

    static GameObject Part(GameObject parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = type.ToString();
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject Block(GameObject root, string name, Vector3 pos, Vector3 size, Material m, bool keepCollider)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        if (!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(root.transform);
        g.transform.position = pos;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static Material ItemMat(Ingredient ing)
    {
        switch (ing)
        {
            case Ingredient.Lettuce: return Mat("Item_Lettuce", new Color(0.44f, 0.75f, 0.23f));
            case Ingredient.Tomato: return Mat("Item_Tomato", new Color(0.9f, 0.22f, 0.21f));
            case Ingredient.Meat: return Mat("Item_Meat", new Color(0.61f, 0.29f, 0.23f));
            case Ingredient.Bun: return Mat("Item_Bun", new Color(0.88f, 0.64f, 0.35f));
            case Ingredient.Potato: return Mat("Item_Potato", new Color(0.85f, 0.7f, 0.37f));
            default: return Mat("Item_Cheese", new Color(1f, 0.84f, 0.31f));
        }
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "KitchenRush");
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder(Root, "Materials");
    }

    static Material Mat(string name, Color col)
    {
        EnsureFolders();
        string path = MatFolder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatS(string name, Color col, float smooth)
    {
        var m = Mat(name, col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatGlow(string name, Color col, float strength)
    {
        var m = Mat(name, col);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col * strength);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material TexMat(string name, string texName, Texture2D fresh, Vector2 tiling)
    {
        var m = Mat(name, Color.white);
        string texPath = MatFolder + "/" + texName + ".asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) { AssetDatabase.CreateAsset(fresh, texPath); tex = fresh; }
        else Object.DestroyImmediate(fresh);
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
        if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", tiling); }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void TintMat(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        EditorUtility.SetDirty(m);
    }

    // كل جدار ياخذ نسخة من المادة بتكرار يناسب حجمه
    static void SetTiling(GameObject g, Vector2 tiling)
    {
        var r = g.GetComponent<Renderer>();
        var baseMat = r.sharedMaterial;
        string path = MatFolder + "/" + baseMat.name + "_" + g.name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(baseMat); AssetDatabase.CreateAsset(m, path); }
        else m.CopyPropertiesFromMaterial(baseMat);
        if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
        if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
        EditorUtility.SetDirty(m);
        r.sharedMaterial = m;
    }

    // أرضية خشب: ألواح بألوان مختلفة شوي
    static Texture2D WoodTex()
    {
        int w = 256, h = 256, plank = 32;
        var tex = new Texture2D(w, h);
        tex.wrapMode = TextureWrapMode.Repeat;
        var rnd = new System.Random(7);
        Color baseC = new Color(0.66f, 0.47f, 0.31f);
        for (int row = 0; row < h / plank; row++)
        {
            float tint = (float)rnd.NextDouble() * 0.12f - 0.06f;
            int seam = rnd.Next(w);
            for (int y = row * plank; y < (row + 1) * plank; y++)
                for (int x = 0; x < w; x++)
                {
                    float grain = Mathf.Sin(x * 0.09f + y * 0.6f + row) * 0.025f + Mathf.Sin(x * 0.31f) * 0.012f;
                    Color c = new Color(baseC.r + tint + grain, baseC.g + tint * 0.8f + grain, baseC.b + tint * 0.6f + grain);
                    if (y % plank < 1 || Mathf.Abs(x - seam) < 1) c *= 0.7f;
                    tex.SetPixel(x, y, c);
                }
        }
        tex.Apply();
        return tex;
    }

    // بلاط ملوّن مثل الصورة
    static Texture2D PatchworkTex()
    {
        int cell = 32, cols = 8, rows = 4;
        var tex = new Texture2D(cell * cols, cell * rows);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        var rnd = new System.Random(3);
        Color grout = new Color(0.93f, 0.9f, 0.84f);
        for (int ty = 0; ty < rows; ty++)
            for (int tx = 0; tx < cols; tx++)
            {
                Color c = Color.Lerp(Pastels[rnd.Next(Pastels.Length)], Color.white, 0.12f + (float)rnd.NextDouble() * 0.1f);
                for (int y = 0; y < cell; y++)
                    for (int x = 0; x < cell; x++)
                    {
                        bool g = x < 2 || y < 2;
                        float shade = 1f - ((x + y) % 7) * 0.006f;
                        tex.SetPixel(tx * cell + x, ty * cell + y, g ? grout : c * shade);
                    }
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CheckerSmallTex(Color c)
    {
        var tex = new Texture2D(8, 8);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, ((x / 2 + y / 2) % 2 == 0) ? c : new Color(0.98f, 0.96f, 0.92f));
        tex.Apply();
        return tex;
    }

    // فوطة بورود
    static Texture2D FloralTex(int v)
    {
        int n = 64;
        var tex = new Texture2D(n, n);
        tex.wrapMode = TextureWrapMode.Repeat;
        Color bg = v == 0 ? new Color(0.99f, 0.96f, 0.86f) : new Color(0.98f, 0.92f, 0.9f);
        Color[] fl = { new Color(0.95f, 0.5f, 0.3f), new Color(0.55f, 0.72f, 0.4f), new Color(0.98f, 0.78f, 0.3f), new Color(0.9f, 0.45f, 0.55f) };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) tex.SetPixel(x, y, bg);
        var rnd = new System.Random(11 + v);
        for (int k = 0; k < 9; k++)
        {
            int cx = rnd.Next(n), cy = rnd.Next(n), r = 4 + rnd.Next(4);
            Color c = fl[rnd.Next(fl.Length)];
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r) tex.SetPixel((cx + x + n) % n, (cy + y + n) % n, c);
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CheckerTex()
    {
        var tex = new Texture2D(2, 2);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        Color a = new Color(0.93f, 0.89f, 0.82f), b = new Color(0.8f, 0.72f, 0.62f);
        tex.SetPixels(new[] { a, b, b, a });
        tex.Apply();
        return tex;
    }

    // بلاط أبيض بخطوط رمادية خفيفة
    static Texture2D WallTex()
    {
        int w = 64, h = 32;
        var tex = new Texture2D(w, h);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        Color tile = new Color(0.96f, 0.95f, 0.92f), grout = new Color(0.78f, 0.75f, 0.7f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int xx = (y < h / 2) ? x : (x + w / 2) % w;   // صفوف متداخلة مثل بلاط المطابخ
                bool line = y % (h / 2) < 2 || xx < 2;
                tex.SetPixel(x, y, line ? grout : tile);
            }
        tex.Apply();
        return tex;
    }
}
#endif
