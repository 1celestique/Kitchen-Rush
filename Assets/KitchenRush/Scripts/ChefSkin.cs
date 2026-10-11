using UnityEngine;
using UnityEngine.InputSystem;

// ==========================================================
// ChefSkin: سكنات الطباخة (ألوان الملابس والقبعة والمريلة والشعر والفيونكة)
// أول سكن هو سكن نينا، ويطلع اسمها فوق راسها
// - السكن المختار ينحفظ ويرجع كل ما فتحتي اللعبة
// - للتجربة: زر K يبدّل السكن وأنتي تلعبين (نشيله لما نسوي القائمة)
// - القائمة الرئيسية تقدر تختار بـ ChefSkin.Selected = رقم
// تقدرين تعدلين الألوان أو تضيفين سكنات من الـ Inspector
// ==========================================================
public class ChefSkin : MonoBehaviour
{
    [System.Serializable]
    public class Skin
    {
        public string name;
        public Color coat;
        public Color apron;
        public Color pants;
        public Color hat;
        public Color hair;
        public Color bow;

        public Skin() { }
        public Skin(string n, Color c, Color a, Color p, Color h, Color hr, Color b) { name = n; coat = c; apron = a; pants = p; hat = h; hair = hr; bow = b; }
    }

    public Skin[] skins =
    {
        new Skin("Nina",     Hex("FFE3EC"), Hex("FF7AA8"), Hex("7A4B6B"), Hex("FFFFFF"), Hex("4A2C24"), Hex("FF4F8B")),
        new Skin("Classic",  Hex("F7F7F2"), Hex("E4572E"), Hex("2B2E3A"), Hex("FFFFFF"), Hex("4A2C24"), Hex("E4572E")),
        new Skin("Mint",     Hex("DFF5EA"), Hex("2BB673"), Hex("1E3A34"), Hex("FFFFFF"), Hex("2E1E18"), Hex("2BB673")),
        new Skin("Ocean",    Hex("F2F6FF"), Hex("2F6FE0"), Hex("1B2A4A"), Hex("E3ECFF"), Hex("1E1A1A"), Hex("2F6FE0")),
        new Skin("Lemon",    Hex("FFF7D6"), Hex("F4B400"), Hex("5A4632"), Hex("FFFFFF"), Hex("8A5A2E"), Hex("FF8A3D")),
        new Skin("Lavender", Hex("EEE6FF"), Hex("8E6CF0"), Hex("2E2450"), Hex("FFFFFF"), Hex("3A2440"), Hex("B48CFF")),
        new Skin("Mocha",    Hex("F2E6D8"), Hex("8B5A3C"), Hex("3A2A20"), Hex("F2E6D8"), Hex("2A1A12"), Hex("C9824E")),
        new Skin("Night",    Hex("2A2A30"), Hex("F4B400"), Hex("111114"), Hex("2A2A30"), Hex("14100E"), Hex("F4B400")),
    };

    [Header("الاسم فوق راسها")]
    public string chefName = "Nina";
    public bool showName = true;

    public int current;
    public bool useSavedSkin = true;      // ياخذ السكن المحفوظ
    public bool pressKToChange = true;    // للتجربة
    public bool recolor = true;           // يطفي لو الشخصية من Mixamo
    public bool accessories = true;       // قبعة طباخ ومريلة فوق الشخصية الحقيقية (لونها حسب السكن)

    float showNameUntil;

    public static int Selected
    {
        get { return PlayerPrefs.GetInt("ChefSkin_v2", 0); }
        set { PlayerPrefs.SetInt("ChefSkin_v2", value); PlayerPrefs.Save(); }
    }

    void Start()
    {
        if (useSavedSkin) current = Selected;
        if (!recolor && accessories) AddAccessories();
        Apply(current);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (recolor && pressKToChange && k != null && k.kKey.wasPressedThisFrame)
        {
            current = (current + 1) % skins.Length;
            if (useSavedSkin) Selected = current;
            Apply(current);
            showNameUntil = Time.time + 1.5f;
        }
    }

    public void Apply(int index)
    {
        if (skins == null || skins.Length == 0) return;
        current = Mathf.Clamp(index, 0, skins.Length - 1);
        var s = skins[current];
        var block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            Color c;
            string n = r.gameObject.name;
            if (n.StartsWith("Coat")) c = s.coat;
            else if (n.StartsWith("Apron")) c = s.apron;
            else if (n.StartsWith("Pants")) c = s.pants;
            else if (n.StartsWith("Hat")) c = s.hat;
            else if (n.StartsWith("Hair")) c = s.hair;
            else if (n.StartsWith("Bow")) c = s.bow;
            else continue;
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            r.SetPropertyBlock(block);
        }
    }

    void OnGUI()
    {
        // ما يطلع فوق كتاب الوصفات ولا شاشة النهاية
        if (KitchenHUD.BookOpen || KitchenHUD.CardOpen) return;
        if (OrderManager.Instance != null && OrderManager.Instance.ended) return;
        var cam = Camera.main;
        if (showName && cam != null && !string.IsNullOrEmpty(chefName))
        {
            Vector3 p = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.25f);
            if (p.z > 0f)
            {
                var ns = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                var r = new Rect(p.x - 60, Screen.height - p.y - 14, 120, 28);
                ns.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
                GUI.Label(new Rect(r.x + 1, r.y + 2, r.width, r.height), chefName, ns);
                ns.normal.textColor = skins.Length > 0 ? Color.Lerp(skins[current].bow, Color.white, 0.35f) : Color.white;
                GUI.Label(r, chefName, ns);
            }
        }

        if (showNameUntil <= 0f || Time.time > showNameUntil) return;
        var st = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.85f, Screen.width, 50), "Skin: " + skins[current].name, st);
    }

    // ---------- قبعة ومريلة للشخصية الحقيقية ----------
    // المريلة مجسم مرن ملزوق على الجسم: تنقاس على شكل البطن والصدر،
    // وتتحرك مع الحوض والرجلين وهي تمشي (مو لوح ثابت)
    void AddAccessories()
    {
        var an = GetComponentInChildren<Animator>();
        if (an == null || !an.isHuman) return;
        var head = an.GetBoneTransform(HumanBodyBones.Head);
        var neck = an.GetBoneTransform(HumanBodyBones.Neck);
        var hips = an.GetBoneTransform(HumanBodyBones.Hips);
        var spine = an.GetBoneTransform(HumanBodyBones.Spine);
        var chest = an.GetBoneTransform(HumanBodyBones.Chest);
        var legL = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var legR = an.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (head == null || hips == null || spine == null) return;
        if (chest == null) chest = spine;

        Vector3 fwd = transform.forward, right = transform.right;
        Vector3 hp = hips.position;

        // شكل الجسم الحين (نقاط الجسم في العالم)
        Vector3[] body = null;
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            var mesh = new Mesh();
            smr.BakeMesh(mesh, true);
            var M = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            var vs = mesh.vertices;
            body = new Vector3[vs.Length];
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < vs.Length; i++)
            {
                body[i] = M.MultiplyPoint3x4(vs[i]);
                minY = Mathf.Min(minY, body[i].y); maxY = Mathf.Max(maxY, body[i].y);
            }
            Destroy(mesh);
            // لو المقاس طلع غلط نصلحه على حجم الشخصية الحقيقي
            float want = smr.bounds.size.y, got = maxY - minY;
            if (got > 0.01f && want > 0.01f && Mathf.Abs(want / got - 1f) > 0.1f)
            {
                float k = want / got;
                for (int i = 0; i < body.Length; i++) body[i] = smr.transform.position + (body[i] - smr.transform.position) * k;
            }
        }

        // ===== القبعة =====
        float headTop = head.position.y + 0.2f;
        if (body != null)
        {
            float top = float.MinValue;
            foreach (var v in body)
            {
                Vector3 dh = v - head.position; dh.y = 0f;
                if (dh.magnitude < 0.12f && v.y > top) top = v.y;
            }
            if (top - head.position.y > 0.05f && top - head.position.y < 0.4f) headTop = top;
        }
        var hat = new GameObject("Hat");
        hat.transform.position = new Vector3(head.position.x, headTop - 0.035f, head.position.z) - fwd * 0.01f;
        hat.transform.rotation = transform.rotation * Quaternion.Euler(-8f, 0f, -10f);
        Acc(hat.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.03f, 0.2f), "Bow_HatBand");
        Acc(hat.transform, PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f), new Vector3(0.23f, 0.15f, 0.23f), "Hat_Top");
        Acc(hat.transform, PrimitiveType.Sphere, new Vector3(-0.06f, 0.1f, 0.03f), new Vector3(0.14f, 0.12f, 0.14f), "Hat_PuffL");
        Acc(hat.transform, PrimitiveType.Sphere, new Vector3(0.06f, 0.1f, -0.03f), new Vector3(0.14f, 0.12f, 0.14f), "Hat_PuffR");
        hat.transform.SetParent(head, true);

        if (body == null) return;

        // ===== المريلة =====
        int n = body.Length;
        var lx = new float[n]; var fz = new float[n]; var vy = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 d = body[i] - hp;
            lx[i] = Vector3.Dot(d, right); fz[i] = Vector3.Dot(d, fwd); vy[i] = body[i].y;
        }
        // أبعد نقطة لقدام قريب من (عرض، ارتفاع)
        System.Func<float, float, float, float> front = (x, y, tol) =>
        {
            float best = float.MinValue;
            for (int i = 0; i < n; i++)
                if (Mathf.Abs(vy[i] - y) < 0.03f && Mathf.Abs(lx[i] - x) < tol && fz[i] > best) best = fz[i];
            return best;
        };

        float topY = (neck != null ? neck.position.y : chest.position.y + 0.2f) - 0.13f;
        float waistY = hp.y + 0.13f;
        float botY = hp.y - 0.33f;
        const int R = 13, C = 9;
        var pts = new Vector3[R * C];
        float[] depth = new float[R * C];
        float lastRow = 0.1f;
        for (int r = 0; r < R; r++)
        {
            float y = Mathf.Lerp(topY, botY, r / (float)(R - 1));
            // الصدر ضيق، وتحت الخصر أعرض
            float hw = Mathf.Lerp(0.095f, 0.165f, Mathf.InverseLerp(waistY + 0.04f, waistY - 0.06f, y));
            bool skirt = y < hp.y - 0.04f;
            float rowMax = float.MinValue;
            if (skirt) rowMax = front(0f, y, hw);
            for (int c = 0; c < C; c++)
            {
                float x = Mathf.Lerp(-hw, hw, c / (float)(C - 1));
                float f = skirt ? rowMax : front(x, y, 0.035f);
                if (f == float.MinValue) f = lastRow;
                if (skirt) f -= Mathf.Pow(x / hw, 2f) * 0.025f;    // تلف شوي على الجوانب
                depth[r * C + c] = f;
                pts[r * C + c] = new Vector3(x, y, 0f);
            }
            float sum = 0f;
            for (int c = 0; c < C; c++) sum += depth[r * C + c];
            lastRow = sum / C;
        }
        // تنعيم عشان ما تطلع متعرجة
        for (int pass = 0; pass < 2; pass++)
        {
            var copy = (float[])depth.Clone();
            for (int r = 0; r < R; r++)
                for (int c = 0; c < C; c++)
                {
                    float s0 = 0f; int k = 0;
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int rr = r + dr, cc = c + dc;
                            if (rr < 0 || rr >= R || cc < 0 || cc >= C) continue;
                            s0 += copy[rr * C + cc]; k++;
                        }
                    depth[r * C + c] = Mathf.Max(copy[r * C + c], s0 / k);
                }
        }

        var root = new GameObject("Apron");
        root.transform.SetParent(an.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        // العظام: الحوض، الظهر، الصدر، الرجلين
        var bones = new[] { hips, spine, chest, legL != null ? legL : hips, legR != null ? legR : hips };
        float sideL = legL != null ? Mathf.Sign(Vector3.Dot(legL.position - hp, right)) : -1f;

        int V = R * C;
        var verts = new Vector3[V * 2];
        var weights = new BoneWeight[V * 2];
        for (int i = 0; i < V; i++)
        {
            float x = pts[i].x, y = pts[i].y;
            Vector3 w = hp + right * x + fwd * (depth[i] + 0.014f);
            w.y = y;
            verts[i] = root.transform.InverseTransformPoint(w);
            verts[i + V] = root.transform.InverseTransformPoint(w - fwd * 0.004f);

            var bw = new BoneWeight();
            if (y > waistY)
            {
                float t = Mathf.Clamp01((y - waistY) / Mathf.Max(0.05f, topY - waistY));
                bw.boneIndex0 = 2; bw.weight0 = t;
                bw.boneIndex1 = 1; bw.weight1 = 1f - t;
            }
            else if (y > hp.y - 0.04f)
            {
                float t = Mathf.Clamp01((y - (hp.y - 0.04f)) / Mathf.Max(0.05f, waistY - hp.y + 0.04f));
                bw.boneIndex0 = 1; bw.weight0 = t;
                bw.boneIndex1 = 0; bw.weight1 = 1f - t;
            }
            else
            {
                float t = Mathf.Clamp01((hp.y - 0.04f - y) / 0.29f);
                float sideAmt = Mathf.Clamp01(Mathf.Abs(x) / 0.12f);
                float legW = t * (0.3f + 0.45f * sideAmt);
                bool onLeft = Mathf.Sign(x) == sideL;
                bw.boneIndex0 = onLeft ? 3 : 4; bw.weight0 = legW;
                bw.boneIndex1 = 0; bw.weight1 = 1f - legW;
            }
            weights[i] = bw;
            weights[i + V] = bw;
        }

        var tris = new System.Collections.Generic.List<int>();
        for (int r = 0; r < R - 1; r++)
            for (int c = 0; c < C - 1; c++)
            {
                int i0 = r * C + c, i1 = i0 + 1, i2 = i0 + C, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        // نخلي الوجه الأمامي يطالع لقدام
        Vector3 nrm = Vector3.Cross(verts[tris[1]] - verts[tris[0]], verts[tris[2]] - verts[tris[0]]);
        bool flip = Vector3.Dot(root.transform.TransformDirection(nrm), fwd) < 0f;
        int T = tris.Count;
        var all = new int[T * 2];
        for (int i = 0; i < T; i += 3)
        {
            int a0 = tris[i], a1 = tris[i + 1], a2 = tris[i + 2];
            if (flip) { int tmp = a1; a1 = a2; a2 = tmp; }
            all[i] = a0; all[i + 1] = a1; all[i + 2] = a2;
            all[T + i] = a0 + V; all[T + i + 1] = a2 + V; all[T + i + 2] = a1 + V;   // الوجه الخلفي
        }

        var am = new Mesh { name = "ApronMesh" };
        am.vertices = verts;
        am.triangles = all;
        am.boneWeights = weights;
        var binds = new Matrix4x4[bones.Length];
        for (int i = 0; i < bones.Length; i++) binds[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
        am.bindposes = binds;
        am.RecalculateNormals();
        am.RecalculateBounds();

        var skin = new GameObject("Apron_Skin");
        skin.transform.SetParent(root.transform, false);
        var sr = skin.AddComponent<SkinnedMeshRenderer>();
        sr.sharedMesh = am;
        sr.bones = bones;
        sr.rootBone = hips;
        sr.updateWhenOffscreen = true;
        Acc(null, PrimitiveType.Cube, Vector3.zero, Vector3.zero, null);   // يجهز المادة
        if (accMat != null) sr.sharedMaterial = accMat;

        // جيب صغير وحبل الرقبة
        Vector3 pocket = hp + fwd * (depth[(R - 4) * C + C / 2] + 0.02f) + Vector3.up * (Mathf.Lerp(topY, botY, (R - 4) / (float)(R - 1)) - hp.y);
        var pk = Acc(null, PrimitiveType.Cube, Vector3.zero, new Vector3(0.13f, 0.08f, 0.006f), "Bow_Pocket");
        pk.transform.position = pocket;
        pk.transform.rotation = transform.rotation;
        pk.transform.SetParent(hips, true);
    }

    static Material accMat;
    static GameObject Acc(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, string name)
    {
        if (accMat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh != null)
            {
                accMat = new Material(sh);
                accMat.SetFloat("_Smoothness", 0.2f);
                accMat.SetFloat("_Cull", 0f);
            }
        }
        if (name == null) return null;
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        var col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>();
        if (accMat != null) r.sharedMaterial = accMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return g;
    }

    static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + h, out c);
        return c;
    }
}
