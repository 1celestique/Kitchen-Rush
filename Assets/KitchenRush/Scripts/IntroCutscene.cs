using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ==========================================================
// IntroCutscene: بداية القصة بشكل كتاب قصص
// صور ثابتة من الصالة (تنصوّر من المشهد أول ما يبدأ) داخل إطار،
// كل صورة تقرّب وتتحرك شوي شوي، والكلام ينكتب تحتها
// نينا تشرب قهوة وترسم مطعمها ← الجدة تمدحها ← الأم تناديها ← المرحلة ١
// Enter: الجاي   ·   Esc: تخطي
// الكلام تقدرين تعدلينه من الـ Inspector (Story)
// ==========================================================
public class IntroCutscene : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        public int shot;          // 0 الغرفة كاملة، 1 الرسمة، 2 نينا قريب، 3 الجدة، 4 باب المطبخ
        public string speaker;    // فاضي = كلام الراوي
        [TextArea] public string text;
        public Line() { }
        public Line(int s, string sp, string t) { shot = s; speaker = sp; text = t; }
    }

    public string nextScene = "level1";
    public Transform nina, granny, sketch;
    public Vector3 door;
    public string bookTitle = "Chapter 0  ·  The Dream";
    [Header("نينا منسدحة على الكنبة")]
    public Vector3 seat = new Vector3(0.2f, 0.49f, 1.41f);   // نص الكنبة وارتفاع المقعد
    public bool flipLying = false;                            // لو طلع وجهها للجدار حطيه صح
    public float bodyThickness = 0.09f;                       // نص سماكة الجسم تحت العظام
    public float seatForward = -0.06f;                        // تقديمها شوي لقدام على المقعد
    Vector3 ninaCenter, headPos, handsPos, faceDir = Vector3.right;
    bool isLying;

    public Line[] story =
    {
        new Line(0, "", "Every evening, Nina stretched out on the couch with a warm cup of matcha and her sketchbook."),
        new Line(2, "Nina", "Mmm... matcha and drawing. My favorite part of the day."),
        new Line(1, "", "And every evening, she drew the same dream: a little restaurant of her very own."),
        new Line(1, "Nina", "Warm lights, cozy tables, the best food in town... I'll call it \"Nina's Kitchen\"."),
        new Line(3, "Grandma", "That's a lovely drawing, sweetheart. One day it will be real."),
        new Line(4, "Mom", "Ninaaa! Can you come help me cook dinner tonight?"),
        new Line(2, "Nina", "Coming, Mom! ...Maybe today is where it all starts."),
    };

    const int Shots = 5;
    Camera cam;
    RenderTexture[] pics = new RenderTexture[Shots];

    Font font;
    RectTransform page;
    RawImage picture, bgImg;
    readonly System.Collections.Generic.List<RectTransform> bokeh = new System.Collections.Generic.List<RectTransform>();
    readonly System.Collections.Generic.List<float> bokehSeed = new System.Collections.Generic.List<float>();
    CanvasGroup pageGroup;
    Text storyText, nameText, titleText;
    Image fade;
    bool leaving;
    int shown = -1;
    float kbTime;
    Vector2 kbFrom, kbTo;

    IEnumerator Start()
    {
        Time.timeScale = 1f;
        cam = Camera.main;
        KitchenAudio.Music("menu");
        BuildUI();

        // نخلي الشخصيات تاخذ وضعيتها، وبعدين نصوّر كل لقطة
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);
        yield return LayOnSofa();
        yield return new WaitForSeconds(0.1f);
        if (cam != null)
        {
            for (int s = 0; s < Shots; s++)
            {
                pics[s] = new RenderTexture(1600, 900, 24);
                Vector3 pos, look;
                ShotPose(s, out pos, out look);
                cam.transform.position = pos;
                cam.transform.rotation = Quaternion.LookRotation(look - pos);
                cam.targetTexture = pics[s];
                cam.Render();
                cam.targetTexture = null;
            }
            // بعد التصوير الكاميرا ما تعرض المشهد، بس خلفية الكتاب
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.11f, 0.09f);
        }
        yield return Run();
    }

    // نلف نينا على طول الكنبة ونحطها على المقعد (حسب شكل الحركة)
    IEnumerator LayOnSofa()
    {
        ninaCenter = nina != null ? nina.position + Vector3.up * 0.6f : seat;
        if (nina == null) yield break;
        var an = nina.GetComponentInChildren<Animator>();
        if (an == null || !an.isHuman) yield break;
        var head = an.GetBoneTransform(HumanBodyBones.Head);
        var hips = an.GetBoneTransform(HumanBodyBones.Hips);
        var fl = an.GetBoneTransform(HumanBodyBones.LeftFoot);
        var fr = an.GetBoneTransform(HumanBodyBones.RightFoot);
        if (head == null || hips == null || fl == null || fr == null) yield break;
        // حدود الجسم الحقيقية حسب الوضعية (مو حدود الوقفة الأصلية)
        foreach (var sm in nina.GetComponentsInChildren<SkinnedMeshRenderer>()) sm.updateWhenOffscreen = true;
        yield return null;
        yield return null;
        Vector3 axis = head.position - (fl.position + fr.position) * 0.5f;
        axis.y = 0f;
        if (axis.magnitude > 0.6f)
        {
            Vector3 want = flipLying ? Vector3.left : Vector3.right;    // الراس جهة المخدة الوردية
            nina.RotateAround(hips.position, Vector3.up, Vector3.SignedAngle(axis, want, Vector3.up));
            yield return null;
        }
        var rs = nina.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) yield break;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        bool lying = axis.magnitude > 0.6f;
        Vector3 target = lying ? new Vector3(seat.x, seat.y, seat.z) : new Vector3(b.center.x, b.min.y, b.center.z);
        float bottom = b.min.y;
        if (lying)
        {
            // أسفل الجسم من العظام (الصدر والحوض والرجلين)، عشان اليدين أو الشعر ما يرفعونها
            float low = float.MaxValue;
            foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg })
            {
                var t = an.GetBoneTransform(bone);
                if (t != null) low = Mathf.Min(low, t.position.y);
            }
            if (low < float.MaxValue) bottom = low - bodyThickness;
        }
        nina.position += new Vector3(target.x - b.center.x, target.y - bottom, target.z + seatForward - b.center.z);
        yield return null;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        ninaCenter = b.center;
        headPos = head.position;
        isLying = lying;
        // الكراسة تحت يدينها على الكنبة (كأنها ترسم وهي منسدحة)
        var hl = an.GetBoneTransform(HumanBodyBones.LeftHand);
        var hr = an.GetBoneTransform(HumanBodyBones.RightHand);
        if (lying && sketch != null && hl != null && hr != null)
        {
            Vector3 h = (hl.position + hr.position) * 0.5f;
            handsPos = h;
            faceDir = h - head.position; faceDir.y = 0f;
            faceDir = faceDir.sqrMagnitude > 0.0001f ? faceDir.normalized : Vector3.right;
            sketch.position = new Vector3(h.x, seat.y + 0.01f, Mathf.Lerp(h.z, seat.z, 0.3f) - 0.05f) + faceDir * 0.06f;
            // الرسمة تنقرا من جهة وجهها
            sketch.rotation = Quaternion.Euler(0f, Mathf.Atan2(-faceDir.x, -faceDir.z) * Mathf.Rad2Deg, 0f);
        }
    }

    void ShotPose(int s, out Vector3 pos, out Vector3 look)
    {
        Vector3 c = ninaCenter;
        Vector3 n = nina != null ? nina.position : Vector3.zero;
        switch (s)
        {
            case 1:
                look = sketch != null ? sketch.position : n;
                pos = isLying ? look + faceDir * 0.12f + Vector3.up * 0.48f + Vector3.back * 0.06f : look + new Vector3(0.05f, 0.75f, -0.45f);
                break;
            case 2:
                if (isLying)
                {
                    // الكاميرا قدام وجهها، تطالعها هي والكراسة
                    look = Vector3.Lerp(c, headPos, 0.35f);
                    pos = look + new Vector3(0.15f, 0.45f, -1.75f);
                }
                else
                {
                    look = c + Vector3.up * 0.05f;
                    pos = c + new Vector3(0.55f, 0.55f, -1.7f);
                }
                break;
            case 3:
                if (granny != null)
                {
                    look = granny.position + Vector3.up * 0.95f;
                    pos = granny.position + granny.forward * 1.6f + Vector3.up * 1.1f + Vector3.Cross(Vector3.up, granny.forward) * 0.4f;
                }
                else { look = n; pos = n + new Vector3(0f, 1.5f, -3f); }
                break;
            case 4:
                look = door;
                pos = c + new Vector3(0.9f, 0.75f, -1.9f);
                break;
            default:
                look = c + new Vector3(0f, -0.15f, -0.4f);
                pos = c + new Vector3(0.0f, 1.05f, -4.4f);
                break;
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame && !leaving) StartCoroutine(Leave());

        for (int i = 0; i < bokeh.Count; i++)
        {
            float sd = bokehSeed[i], t = Time.time;
            bokeh[i].anchoredPosition = new Vector2(Mathf.Sin(t * 0.12f + sd) * 60f, Mathf.Sin(t * 0.1f + sd * 1.3f) * 50f);
        }

        // الصورة تقرّب وتتحرك بهدوء (مثل كتب القصص المتحركة)
        if (picture != null && shown >= 0)
        {
            kbTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(kbTime / 9f));
            float zoom = Mathf.Lerp(1f, 0.88f, t);
            Vector2 c = Vector2.Lerp(kbFrom, kbTo, t);
            picture.uvRect = new Rect(c.x - zoom / 2f, c.y - zoom / 2f, zoom, zoom);
        }
    }

    IEnumerator Run()
    {
        // العنوان، وبعدين تفتح الصفحة
        titleText.text = bookTitle;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { fade.color = new Color(0, 0, 0, 1f - t); yield return null; }
        fade.color = new Color(0, 0, 0, 0);

        foreach (var l in story)
        {
            if (leaving) yield break;
            if (l.shot != shown) yield return TurnPage(l.shot);
            if (l.speaker == "Mom") KitchenAudio.Play("ding", 0.6f);
            yield return Say(l);
        }
        if (!leaving) yield return Leave();
    }

    // تقليب الصفحة: الصورة القديمة تطلع والجديدة تدخل بميلان خفيف
    IEnumerator TurnPage(int shot)
    {
        if (shown >= 0)
        {
            KitchenAudio.Play("drop", 0.5f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                pageGroup.alpha = 1f - t;
                page.anchoredPosition = new Vector2(-t * 160f, 10f);
                page.localRotation = Quaternion.Euler(0f, 0f, t * 4f);
                yield return null;
            }
        }
        shown = Mathf.Clamp(shot, 0, Shots - 1);
        picture.texture = pics[shown];
        kbTime = 0f;
        float r = (shown * 37 % 10) / 10f;
        kbFrom = new Vector2(0.5f + (r - 0.5f) * 0.06f, 0.5f);
        kbTo = new Vector2(0.5f - (r - 0.5f) * 0.06f, 0.5f + 0.02f);
        storyText.text = "";
        nameText.text = "";
        float tilt = shown % 2 == 0 ? -1.2f : 1.2f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            pageGroup.alpha = e;
            page.anchoredPosition = new Vector2((1f - e) * 200f, 10f);
            page.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-5f, tilt, e));
            page.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, e);
            yield return null;
        }
        pageGroup.alpha = 1f;
        page.anchoredPosition = new Vector2(0f, 10f);
        page.localRotation = Quaternion.Euler(0f, 0f, tilt);
        page.localScale = Vector3.one;
    }

    IEnumerator Say(Line l)
    {
        bool narrator = string.IsNullOrEmpty(l.speaker);
        nameText.text = narrator ? "" : l.speaker;
        nameText.color = SpeakerColor(l.speaker);
        storyText.fontStyle = FontStyle.Normal;
        storyText.font = narrator ? UIFonts.Story : UIFonts.Body;
        string full = narrator ? l.text : "“" + l.text + "”";
        float chars = 0f;
        while (chars < full.Length)
        {
            if (leaving) yield break;
            chars += Time.deltaTime * 42f;
            if (Pressed()) chars = full.Length;
            storyText.text = full.Substring(0, Mathf.Min(full.Length, (int)chars));
            yield return null;
        }
        yield return null;
        float wait = 2.6f + full.Length * 0.03f;
        while (wait > 0f && !leaving)
        {
            if (Pressed()) break;
            wait -= Time.deltaTime;
            yield return null;
        }
    }

    static Color SpeakerColor(string speaker)
    {
        switch (speaker)
        {
            case "Nina": return new Color(0.9f, 0.35f, 0.55f);
            case "Mom": return new Color(0.3f, 0.5f, 0.85f);
            case "Grandma": return new Color(0.35f, 0.6f, 0.3f);
            default: return new Color(0.4f, 0.32f, 0.26f);
        }
    }

    bool Pressed()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        return (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
               || (m != null && m.leftButton.wasPressedThisFrame);
    }

    IEnumerator Leave()
    {
        if (leaving) yield break;
        leaving = true;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1f) { fade.color = new Color(0, 0, 0, t); yield return null; }
        fade.color = Color.black;
        SceneManager.LoadScene(nextScene);
    }

    void OnDestroy()
    {
        foreach (var p in pics) if (p != null) p.Release();
    }

    // ---------- الواجهة: صفحة الكتاب (صورة بإطار + ورقة الكلام) ----------
    void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var round = KitchenHUD.MakeRounded(64, 18);
        var g = new GameObject("Story UI");
        var canvas = g.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = g.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        var root = g.GetComponent<RectTransform>();

        // خلفية زرقا فيها توليب وأكل، تتحرك شوي شوي
        bgImg = new GameObject("Background", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        bgImg.rectTransform.SetParent(root, false);
        bgImg.rectTransform.anchorMin = Vector2.zero; bgImg.rectTransform.anchorMax = Vector2.one;
        bgImg.rectTransform.sizeDelta = Vector2.zero;
        bgImg.texture = SkyGradient();
        bgImg.raycastTarget = false;
        // دواير ضوء ناعمة تطفو (بوكيه) بألوان فاتحة
        var soft = SoftCircle();
        Color[] cols = { new Color(1f, 0.75f, 0.82f, 0.13f), new Color(1f, 0.9f, 0.6f, 0.12f), new Color(0.7f, 0.92f, 0.8f, 0.12f), new Color(1f, 1f, 1f, 0.14f) };
        var rnd = new System.Random(4);
        for (int i = 0; i < 14; i++)
        {
            var b = new GameObject("Bokeh", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            b.rectTransform.SetParent(root, false);
            b.sprite = soft;
            b.color = cols[i % cols.Length];
            b.raycastTarget = false;
            float size = 120 + (float)rnd.NextDouble() * 260;
            b.rectTransform.sizeDelta = new Vector2(size, size);
            b.rectTransform.anchorMin = b.rectTransform.anchorMax = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            bokeh.Add(b.rectTransform);
            bokehSeed.Add((float)rnd.NextDouble() * 10f);
        }

        var titleBox = Box(root, "TitleTag", new Vector2(520, 54), new Color(1f, 0.98f, 0.94f), round);
        titleBox.anchorMin = titleBox.anchorMax = new Vector2(0.5f, 1f);
        titleBox.anchoredPosition = new Vector2(0, -44);
        titleText = Label(titleBox, "", 26, new Color(0.85f, 0.35f, 0.5f), TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        // الصفحة
        page = new GameObject("Page", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        page.SetParent(root, false);
        page.sizeDelta = new Vector2(1320, 900);
        page.anchoredPosition = new Vector2(0, 10);
        pageGroup = page.GetComponent<CanvasGroup>();
        pageGroup.alpha = 0f;

        // إطار أبيض مثل صورة مطبوعة، وظل
        var frame = Box(page, "Frame", new Vector2(1260, 720), new Color(0.99f, 0.97f, 0.93f), round);
        frame.anchoredPosition = new Vector2(0, 80);
        var sh = frame.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.45f);
        sh.effectDistance = new Vector2(10, -14);
        picture = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        picture.rectTransform.SetParent(frame, false);
        picture.rectTransform.sizeDelta = new Vector2(1212, 682);
        picture.color = new Color(1f, 0.97f, 0.92f);   // دفا خفيف مثل الورق
        // شريط لاصق فوق الصورة
        var tape = Box(frame, "Tape", new Vector2(190, 46), new Color(1f, 0.92f, 0.65f, 0.85f), null);
        tape.anchoredPosition = new Vector2(0, 362);
        tape.localRotation = Quaternion.Euler(0f, 0f, -3f);

        // ورقة الكلام تحت الصورة
        var note = Box(page, "Note", new Vector2(1100, 170), new Color(1f, 0.98f, 0.94f), round);
        note.anchoredPosition = new Vector2(0, -360);
        note.localRotation = Quaternion.Euler(0f, 0f, 0.6f);
        var nsh = note.gameObject.AddComponent<Shadow>();
        nsh.effectColor = new Color(0, 0, 0, 0.35f);
        nsh.effectDistance = new Vector2(6, -8);
        nameText = Label(note, "", 26, Color.white, TextAnchor.UpperLeft);
        nameText.fontStyle = FontStyle.Bold;
        nameText.rectTransform.offsetMin = new Vector2(40, 0);
        nameText.rectTransform.offsetMax = new Vector2(-40, -16);
        storyText = Label(note, "", 30, new Color(0.25f, 0.2f, 0.17f), TextAnchor.MiddleCenter);
        storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyText.rectTransform.offsetMin = new Vector2(50, 12);
        storyText.rectTransform.offsetMax = new Vector2(-50, -30);

        var hint = Label(root, "Enter  Next      ·      Esc  Skip", 20, new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerRight);
        var ho = hint.gameObject.AddComponent<Outline>();
        ho.effectColor = new Color(0.1f, 0.2f, 0.4f, 0.6f);
        hint.rectTransform.offsetMin = new Vector2(0, 28);
        hint.rectTransform.offsetMax = new Vector2(-40, 0);

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.rectTransform.SetParent(root, false);
        fade.rectTransform.anchorMin = Vector2.zero; fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.sizeDelta = Vector2.zero;
        fade.color = Color.black;
        fade.raycastTarget = false;
        UIFonts.Apply(g);
    }

    // سما فاتحة فوق ولون خوخي دافي تحت
    static Texture2D SkyGradient()
    {
        var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.36f, 0.48f, 0.6f), mid = new Color(0.5f, 0.55f, 0.66f), bottom = new Color(0.62f, 0.52f, 0.52f);   // ألوان هادية ما تعمي
        for (int y = 0; y < 256; y++)
        {
            float k = y / 255f;
            Color c = k > 0.5f ? Color.Lerp(mid, top, (k - 0.5f) * 2f) : Color.Lerp(bottom, mid, k * 2f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }

    static Sprite SoftCircle()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    RectTransform Box(RectTransform parent, string name, Vector2 size, Color c, Sprite s)
    {
        var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        var img = rt.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        if (s != null) { img.sprite = s; img.type = Image.Type.Sliced; }
        return rt;
    }

    Text Label(RectTransform parent, string text, int size, Color c, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.supportRichText = true;
        t.raycastTarget = false;
        return t;
    }
}
