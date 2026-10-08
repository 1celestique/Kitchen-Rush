#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// ==========================================================
// KitchenBuilder v2: يبني مطبخ المرحلة ١ بضغطة وحدة
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
        "## L  T  ## CH CH ## PL ## SV",
        ".. .. .. .. .. .. .. .. .. ..",
        ".. .. .. .. CF .. .. .. .. ..",
        ".. .. .. .. .. .. .. .. .. ..",
        "## ## ## ## ## ## ## ## TR ##",
    };

    [MenuItem("Tools/Kitchen Rush/Build Level 1 Kitchen")]
    static void BuildLevel1() { Build("Kitchen_Level1", Level1); }

    // ---------- المجسمات اللي لقيناها ----------
    static GameObject mCounter, mSink, mTrash, mBoard, mKnife, mPlate, mPot, mPan, mFridge;
    static readonly Dictionary<Ingredient, GameObject> mFood = new Dictionary<Ingredient, GameObject>();
    static List<string> modelPaths;
    static readonly List<string> report = new List<string>();

    static void FindAllModels()
    {
        modelPaths = new List<string>();
        foreach (var t in new[] { "t:Model", "t:Prefab" })
            foreach (var guid in AssetDatabase.FindAssets(t, new[] { "Assets" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.StartsWith(MatFolder)) continue;
                if (!modelPaths.Contains(p)) modelPaths.Add(p);
            }
        report.Clear();

        mCounter = Find("Counter", new[] { "kitchen-counter", "counter", "kitchen-cabinet", "cabinet", "cupboard", "drawer" }, new[] { "wall", "corner", "upper", "hanging", "top", "door", "sink", "stove", "oven" });
        mSink = Find("Sink", new[] { "kitchen-sink", "sink" }, new[] { "bath", "toilet" });
        mTrash = Find("Trash", new[] { "trash-can", "trashcan", "garbage", "trash", "bin" }, new[] { "cabinet", "bag" });
        mBoard = Find("Cutting board", new[] { "cutting-board", "chopping-board", "cuttingboard", "board" }, new[] { "skate", "sign", "black", "cupboard" });
        mKnife = Find("Knife", new[] { "knife", "cleaver" }, new[] { "fork", "block" });
        mPlate = Find("Plate", new[] { "plate", "dish" }, new[] { "broken", "rack", "washer", "dishes", "full" });
        mPot = Find("Pot", new[] { "pot", "cooking-pot", "saucepan" }, new[] { "potato", "plant", "flower", "lid", "teapot" });
        mPan = Find("Pan", new[] { "frying-pan", "pan" }, new[] { "pancake", "panel", "dustpan" });
        mFridge = Find("Fridge", new[] { "fridge", "refrigerator" }, null);

        mFood.Clear();
        mFood[Ingredient.Lettuce] = Find("Lettuce", new[] { "lettuce", "cabbage", "salad-leaf", "salad" }, new[] { "bowl", "plate", "slice", "cut" });
        mFood[Ingredient.Tomato] = Find("Tomato", new[] { "tomato" }, new[] { "slice", "sauce", "soup", "ketchup", "cut", "half" });
        mFood[Ingredient.Meat] = Find("Meat", new[] { "meat-patty", "patty", "meat-raw", "meat", "steak" }, new[] { "burger", "sandwich", "ball", "cooked" });
        mFood[Ingredient.Bun] = Find("Bun", new[] { "bun", "burger-bun", "bread" }, new[] { "bunny", "slice", "bottom", "top" });
        mFood[Ingredient.Potato] = Find("Potato", new[] { "potato" }, new[] { "chips", "fries", "mashed" });
        mFood[Ingredient.Cheese] = Find("Cheese", new[] { "cheese" }, new[] { "burger", "cake", "sandwich", "pizza" });
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

        BuildRoom(root, C, R);

        Vector3 chefPos = Vector3.zero;
        for (int r = 0; r < R; r++)
        {
            for (int c = 0; c < C; c++)
            {
                string code = rows[r][c];
                Vector3 pos = new Vector3(c - (C - 1) / 2f, 0f, (R - 1) / 2f - r);
                float face = Facing(r, c, R, C);
                switch (code)
                {
                    case "##": Counter(root, "Counter", pos, face); break;
                    case "L": CrateTile(root, pos, face, Ingredient.Lettuce); break;
                    case "T": CrateTile(root, pos, face, Ingredient.Tomato); break;
                    case "M": CrateTile(root, pos, face, Ingredient.Meat); break;
                    case "B": CrateTile(root, pos, face, Ingredient.Bun); break;
                    case "P": CrateTile(root, pos, face, Ingredient.Potato); break;
                    case "C": CrateTile(root, pos, face, Ingredient.Cheese); break;
                    case "CH": ChopTile(root, pos, face); break;
                    case "PL": PlatesTile(root, pos, face); break;
                    case "SV": ServeTile(root, pos); break;
                    case "TR": TrashTile(root, pos, face); break;
                    case "CF": chefPos = pos; break;
                }
            }
        }

        BuildChef(chefPos + Vector3.up * 0.05f);
        SetupCamera();
        SetupLights(root, C, R);
        SetupPost(root);

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("Kitchen Rush: " + rootName + " built.\n" + string.Join("\n", report));
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

    static void BuildRoom(GameObject root, int C, int R)
    {
        // أرضية مربعات
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        floor.transform.localScale = new Vector3(C, 0.2f, R);
        floor.GetComponent<Renderer>().sharedMaterial = TexMat("Floor_Tiles", "FloorChecker", CheckerTex(), new Vector2(C / 2f, R / 2f));

        // إطار خشب تحت المطبخ
        var baseMat = Mat("Kitchen_Base", new Color(0.36f, 0.25f, 0.18f));
        Block(root, "Base", new Vector3(0f, -0.35f, 0f), new Vector3(C + 0.6f, 0.3f, R + 0.6f), baseMat, false);

        // جدران بلاط
        var tile = TexMat("Wall_Tiles", "WallTiles", WallTex(), Vector2.one);
        var back = Block(root, "Wall_Back", new Vector3(0f, 1.25f, R / 2f + 0.15f), new Vector3(C + 0.6f, 2.5f, 0.3f), tile, true);
        SetTiling(back, new Vector2((C + 0.6f) * 2f, 5f));
        var left = Block(root, "Wall_Left", new Vector3(-C / 2f - 0.15f, 1.25f, 0f), new Vector3(0.3f, 2.5f, R), tile, true);
        var right = Block(root, "Wall_Right", new Vector3(C / 2f + 0.15f, 1.25f, 0f), new Vector3(0.3f, 2.5f, R), tile, true);
        SetTiling(left, new Vector2(R * 2f, 5f));
        SetTiling(right, new Vector2(R * 2f, 5f));
        var trim = Mat("Wall_Trim", new Color(0.36f, 0.25f, 0.18f));
        Block(root, "Wall_Back_Top", new Vector3(0f, 2.55f, R / 2f + 0.15f), new Vector3(C + 0.7f, 0.1f, 0.36f), trim, false);
        Block(root, "Wall_Left_Top", new Vector3(-C / 2f - 0.15f, 2.55f, 0f), new Vector3(0.36f, 0.1f, R), trim, false);
        Block(root, "Wall_Right_Top", new Vector3(C / 2f + 0.15f, 2.55f, 0f), new Vector3(0.36f, 0.1f, R), trim, false);

        // جدار قدام مخفي عشان ما تطلع الطباخة
        var front = new GameObject("Wall_Front (invisible)");
        front.transform.SetParent(root.transform);
        front.transform.position = new Vector3(0f, 1f, -R / 2f - 0.15f);
        front.AddComponent<BoxCollider>().size = new Vector3(C, 2f, 0.3f);

        // رف على الجدار فيه أغراض (زينة)
        float z = R / 2f - 0.05f;
        Block(root, "Shelf", new Vector3(-C / 4f, 1.9f, z - 0.1f), new Vector3(3f, 0.08f, 0.35f), trim, false);
        Block(root, "Shelf_2", new Vector3(C / 4f, 1.9f, z - 0.1f), new Vector3(3f, 0.08f, 0.35f), trim, false);
        var shelfItems = new List<GameObject>();
        foreach (var g in new[] { mPot, mPan, mPlate, mFood[Ingredient.Tomato], mPot, mFood[Ingredient.Cheese] }) if (g != null) shelfItems.Add(g);
        for (int i = 0; i < shelfItems.Count; i++)
        {
            float x = (i < 3 ? -C / 4f : C / 4f) + ((i % 3) - 1) * 0.9f;
            Fit(shelfItems[i], root.transform, new Vector3(x, 1.94f, z - 0.1f), new Vector3(0.5f, 0.45f, 0.3f), false, 180f);
        }

        // ثلاجة جنب المطبخ لو موجودة (زينة)
        if (mFridge != null)
            Fit(mFridge, root.transform, new Vector3(C / 2f + 0.75f, 0f, R / 2f - 0.6f), new Vector3(1f, 2.1f, 1f), false, 270f + ModelFrontOffset);
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
        Part(t, PrimitiveType.Cube, new Vector3(0f, (CounterH - 0.08f) / 2f, 0f), new Vector3(0.96f, CounterH - 0.08f, 0.96f), Mat("Counter_" + ColorUtility.ToHtmlStringRGB(body), body));
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH - 0.04f, 0f), new Vector3(1f, 0.08f, 1f), Mat("Top_" + ColorUtility.ToHtmlStringRGB(top), top));
        Part(t, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.1f, 0.9f), Mat("Counter_Kick", new Color(0.3f, 0.22f, 0.16f)));
    }

    static void Counter(GameObject root, string name, Vector3 pos, float face)
    {
        var t = Tile(root, name, pos);
        CounterBody(t, face, Wood, WoodTop);
        t.AddComponent<Station>();
    }

    static void CrateTile(GameObject root, Vector3 pos, float face, Ingredient ing)
    {
        var t = Tile(root, "Crate_" + ing, pos);
        CounterBody(t, face, Wood, WoodTop);
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
        // بكرة نحط عليه سكربت التقطيع، اليوم يشتغل كطاولة عادية
        var t = Tile(root, "ChopBoard", pos);
        CounterBody(t, face, Wood, WoodTop);
        if (mBoard != null) Fit(mBoard, t.transform, new Vector3(0f, CounterH, 0f), new Vector3(0.75f, 0.12f, 0.55f), false, 0f);
        else Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH + 0.025f, 0f), new Vector3(0.7f, 0.05f, 0.5f), Mat("Board", new Color(0.94f, 0.82f, 0.6f)));
        if (mKnife != null) Fit(mKnife, t.transform, new Vector3(0.1f, CounterH + 0.06f, 0.2f), new Vector3(0.4f, 0.06f, 0.4f), false, 90f);
        else
        {
            Part(t, PrimitiveType.Cube, new Vector3(0.12f, CounterH + 0.06f, 0.15f), new Vector3(0.32f, 0.02f, 0.06f), Mat("Steel", new Color(0.8f, 0.83f, 0.86f)));
            Part(t, PrimitiveType.Cube, new Vector3(-0.1f, CounterH + 0.065f, 0.15f), new Vector3(0.12f, 0.03f, 0.06f), Mat("Handle", new Color(0.24f, 0.15f, 0.12f)));
        }
        t.AddComponent<Station>().topExtra = 0.05f;
    }

    static void PlatesTile(GameObject root, Vector3 pos, float face)
    {
        // بكرة نحط عليه سكربت الصحون
        var t = Tile(root, "Plates", pos);
        CounterBody(t, face, Wood, WoodTop);
        var white = Mat("Plate", new Color(0.97f, 0.97f, 0.97f));
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = new Vector3(0f, CounterH + i * 0.045f, 0f);
            if (mPlate != null) Fit(mPlate, t.transform, p, new Vector3(0.55f, 0.05f, 0.55f), false, 0f);
            else Part(t, PrimitiveType.Cylinder, p + Vector3.up * 0.02f, new Vector3(0.5f, 0.02f, 0.5f), white);
        }
        t.AddComponent<Station>().topExtra = 0.2f;
    }

    static void ServeTile(GameObject root, Vector3 pos)
    {
        // بكرة نحط عليه سكربت التسليم
        var t = Tile(root, "ServeWindow", pos);
        var green = Mat("Serve_Body", new Color(0.27f, 0.55f, 0.2f));
        var glow = MatGlow("Serve_Top", new Color(0.55f, 0.85f, 0.3f), 0.6f);
        Part(t, PrimitiveType.Cube, new Vector3(0f, (CounterH - 0.08f) / 2f, 0f), new Vector3(0.96f, CounterH - 0.08f, 0.96f), green);
        Part(t, PrimitiveType.Cube, new Vector3(0f, CounterH - 0.04f, 0f), new Vector3(1f, 0.08f, 1f), glow);
        Part(t, PrimitiveType.Cylinder, new Vector3(0f, CounterH + 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.14f), Mat("Bell_Base", new Color(0.2f, 0.2f, 0.2f)));
        Part(t, PrimitiveType.Sphere, new Vector3(0f, CounterH + 0.08f, 0f), new Vector3(0.16f, 0.12f, 0.16f), Mat("Bell", new Color(0.96f, 0.71f, 0f)));
        t.AddComponent<Station>();
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
        cam.fieldOfView = 40f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.12f, 0.1f);

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
        sun.color = new Color(1f, 0.94f, 0.85f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        // ضوء محيط دافي
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.74f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.52f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.24f, 0.2f);

        // لمبات معلقة
        var shade = Mat("Lamp_Shade", new Color(0.15f, 0.15f, 0.16f));
        var bulb = MatGlow("Lamp_Bulb", new Color(1f, 0.85f, 0.55f), 3f);
        foreach (float x in new[] { -C / 4f, C / 4f })
        {
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(root.transform);
            lamp.transform.position = new Vector3(x, 0f, 0.3f);
            Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 3.3f, 0f), new Vector3(0.02f, 0.5f, 0.02f), shade);
            Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 2.8f, 0f), new Vector3(0.5f, 0.08f, 0.5f), shade);
            Part(lamp, PrimitiveType.Sphere, new Vector3(0f, 2.7f, 0f), Vector3.one * 0.18f, bulb);
            var pl = new GameObject("Lamp_Light").AddComponent<Light>();
            pl.transform.SetParent(lamp.transform, false);
            pl.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.82f, 0.6f);
            pl.range = 6f;
            pl.intensity = 2.2f;
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
        bloom.intensity.Override(0.6f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.6f);

        var tone = Get<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.Neutral);

        var color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0.2f);
        color.contrast.Override(12f);
        color.saturation.Override(20f);

        var vig = Get<Vignette>(profile);
        vig.intensity.Override(0.25f);
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
