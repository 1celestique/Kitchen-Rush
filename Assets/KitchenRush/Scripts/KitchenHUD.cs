using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// ==========================================================
// KitchenHUD: واجهة اللعب، تنبني لحالها أول ما تبدأ المرحلة
// - بطاقات الطلبات فوق يسار مع شريط الوقت
// - مؤقت المرحلة فوق بالنص، والنقاط والنجوم فوق يمين
// - شريط التقطيع فوق اللوح، وتلميح الزر فوق الطباخة، ورسايل قصيرة
// - شاشة النهاية بالنجوم وزر الإعادة والتالي
// ما يحتاج تسوين شي بيدك
// ==========================================================
public class KitchenHUD : MonoBehaviour
{
    static KitchenHUD I;

    OrderManager om;
    Font font;
    Sprite round, star;
    RectTransform root, ticketRow;
    Text timerText, scoreText, flashText, hintText;
    Image[] starImgs = new Image[3];
    float flashUntil;
    int hintFrame = -1;
    Vector3 hintWorld;

    // الكلام تحت الشاشة (Captions)
    struct Line { public string speaker, text; public float hold; }
    readonly Queue<Line> captions = new Queue<Line>();
    RectTransform capPanel;
    Text capText;
    bool capActive;
    Line capLine;
    float capChars, capHold;

    readonly Dictionary<Order, RectTransform> tickets = new Dictionary<Order, RectTransform>();
    readonly Dictionary<Object, RectTransform> bars = new Dictionary<Object, RectTransform>();
    readonly Dictionary<Object, int> barFrame = new Dictionary<Object, int>();

    static readonly Color Ink = new Color(0.16f, 0.13f, 0.11f);
    static readonly Color Paper = new Color(1f, 0.97f, 0.92f);
    static readonly Color Dark = new Color(0.12f, 0.09f, 0.08f, 0.82f);
    static readonly Color Pink = new Color(1f, 0.48f, 0.66f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);

    public static void Ensure(OrderManager m)
    {
        if (I != null) return;
        var g = new GameObject("Kitchen HUD");
        I = g.AddComponent<KitchenHUD>();
        I.om = m;
        I.Build();
    }

    // ---------- اللي تناديه السكربتات الثانية ----------

    public static void Flash(string msg) { Flash(msg, Color.white); }

    public static void Flash(string msg, Color c)
    {
        if (I == null || I.flashText == null) return;
        I.flashText.text = msg;
        I.flashText.color = c;
        I.flashUntil = Time.unscaledTime + 1.6f;
    }

    public static void Bar(Object key, Vector3 world, float value, Color c)
    {
        if (I == null) return;
        RectTransform b;
        if (!I.bars.TryGetValue(key, out b) || b == null)
        {
            b = I.Panel(I.root, "Bar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 18), Dark);
            var fill = I.Panel(b, "Fill", new Vector2(0f, 0.5f), new Vector2(3, 0), new Vector2(104, 12), c);
            fill.pivot = new Vector2(0f, 0.5f);
            I.bars[key] = b;
        }
        b.gameObject.SetActive(true);
        I.barFrame[key] = Time.frameCount;
        b.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(104f * Mathf.Clamp01(value), 12f);
        I.PlaceAt(b, world);
    }

    public static void Hint(Vector3 world, string text)
    {
        if (I == null || I.hintText == null) return;
        I.hintText.text = text;
        I.hintWorld = world;
        I.hintFrame = Time.frameCount;
    }

    // تضيف سطر كلام. speaker فاضي = تلميح
    public static void Caption(string speaker, string text, float hold = 2.2f)
    {
        if (I == null) return;
        I.captions.Enqueue(new Line { speaker = speaker, text = text, hold = hold });
    }

    public static bool CaptionsBusy { get { return I != null && (I.capActive || I.captions.Count > 0); } }

    public static void ShowResults(OrderManager m)
    {
        if (I != null) I.BuildResults(m);
    }

    // ---------- البناء ----------

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        round = MakeRounded(64, 20);
        star = MakeStar(128);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        root = GetComponent<RectTransform>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // الطلبات فوق يسار
        ticketRow = new GameObject("Orders", typeof(RectTransform)).GetComponent<RectTransform>();
        ticketRow.SetParent(root, false);
        ticketRow.anchorMin = ticketRow.anchorMax = new Vector2(0f, 1f);
        ticketRow.pivot = new Vector2(0f, 1f);
        ticketRow.anchoredPosition = new Vector2(28, -24);
        ticketRow.sizeDelta = new Vector2(900, 150);
        var h = ticketRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14;
        h.childControlWidth = h.childControlHeight = false;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        // المؤقت
        var tp = Panel(root, "Timer", new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(200, 76), Dark);
        tp.pivot = new Vector2(0.5f, 1f); tp.anchoredPosition = new Vector2(0, -24);
        timerText = Label(tp, "03:00", 46, Color.white, TextAnchor.MiddleCenter);

        // النقاط والنجوم
        var sp = Panel(root, "Score", new Vector2(1f, 1f), Vector2.zero, new Vector2(270, 76), Dark);
        sp.pivot = new Vector2(1f, 1f); sp.anchoredPosition = new Vector2(-28, -24);
        scoreText = Label(sp, "0", 40, Gold, TextAnchor.MiddleCenter);
        var stp = Panel(root, "Stars", new Vector2(1f, 1f), Vector2.zero, new Vector2(270, 64), Dark);
        stp.pivot = new Vector2(1f, 1f); stp.anchoredPosition = new Vector2(-28, -110);
        for (int i = 0; i < 3; i++)
        {
            var s = Img(stp, star, new Color(1f, 1f, 1f, 0.2f));
            s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            s.rectTransform.sizeDelta = new Vector2(50, 50);
            s.rectTransform.anchoredPosition = new Vector2((i - 1) * 64, 0);
            starImgs[i] = s;
        }

        // اسم المرحلة تحت يسار
        var lp = Panel(root, "Level", new Vector2(0f, 0f), Vector2.zero, new Vector2(560, 56), Dark);
        lp.pivot = new Vector2(0f, 0f); lp.anchoredPosition = new Vector2(28, 24);
        Label(lp, om.levelTitle + "    [H] Recipes", 22, Color.white, TextAnchor.MiddleCenter);

        // رسالة بالنص تحت
        flashText = Label(root, "", 38, Color.white, TextAnchor.MiddleCenter);
        var fr = flashText.rectTransform;
        fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 0.2f);
        fr.sizeDelta = new Vector2(1200, 70);
        var outl = flashText.gameObject.AddComponent<Outline>();
        outl.effectColor = new Color(0, 0, 0, 0.75f); outl.effectDistance = new Vector2(2, -2);

        // تلميح فوق الطباخة
        hintText = Label(root, "", 24, Color.white, TextAnchor.MiddleCenter);
        hintText.rectTransform.anchorMin = hintText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        hintText.rectTransform.sizeDelta = new Vector2(400, 40);
        var ho = hintText.gameObject.AddComponent<Outline>();
        ho.effectColor = new Color(0, 0, 0, 0.8f); ho.effectDistance = new Vector2(2, -2);

        // صندوق الكلام تحت
        capPanel = Panel(root, "Caption", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1180, 96), new Color(0.1f, 0.08f, 0.08f, 0.85f));
        capPanel.pivot = new Vector2(0.5f, 0f);
        capPanel.anchoredPosition = new Vector2(0, 96);
        capText = Label(capPanel, "", 30, Color.white, TextAnchor.MiddleCenter);
        capText.supportRichText = true;
        capText.horizontalOverflow = HorizontalWrapMode.Wrap;
        capText.rectTransform.sizeDelta = new Vector2(-60, -16);
        capPanel.gameObject.SetActive(false);
    }

    void UpdateCaptions()
    {
        var k = UnityEngine.InputSystem.Keyboard.current;
        bool skip = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
        if (!capActive && captions.Count > 0)
        {
            capLine = captions.Dequeue();
            capActive = true;
            capChars = 0f;
            capHold = capLine.hold;
            capPanel.gameObject.SetActive(true);
            skip = false;
        }
        if (!capActive) { capPanel.gameObject.SetActive(false); return; }

        float dt = Time.unscaledDeltaTime;
        int total = capLine.text.Length;
        if (capChars < total)
        {
            capChars = skip ? total : capChars + dt * 45f;
        }
        else
        {
            capHold -= dt;
            if (skip || capHold <= 0f) { capActive = false; return; }
        }
        string shown = capLine.text.Substring(0, Mathf.Min(total, Mathf.FloorToInt(capChars)));
        capText.text = string.IsNullOrEmpty(capLine.speaker)
            ? "<color=#FFD24D>" + shown + "</color>"
            : "<b><color=#FF9EC4>" + capLine.speaker + ":</color></b>  " + shown;
    }

    void LateUpdate()
    {
        UpdateCaptions();
        if (om == null) return;

        int t = Mathf.CeilToInt(om.timeLeft);
        timerText.text = (t / 60).ToString("00") + ":" + (t % 60).ToString("00");
        timerText.color = om.timeLeft < 30f && !om.ended ? new Color(1f, 0.45f, 0.4f) : Color.white;
        scoreText.text = om.score.ToString();
        int stars = om.Stars;
        for (int i = 0; i < 3; i++) starImgs[i].color = i < stars ? Gold : new Color(1f, 1f, 1f, 0.2f);

        // بطاقات الطلبات
        var gone = new List<Order>();
        foreach (var kv in tickets) if (!om.orders.Contains(kv.Key)) gone.Add(kv.Key);
        foreach (var o in gone) { if (tickets[o] != null) Destroy(tickets[o].gameObject); tickets.Remove(o); }
        foreach (var o in om.orders) if (!tickets.ContainsKey(o)) tickets[o] = MakeTicket(o);
        foreach (var kv in tickets)
        {
            float v = Mathf.Clamp01(kv.Key.timeLeft / kv.Key.patience);
            var fill = kv.Value.Find("BarBg/Fill") as RectTransform;
            if (fill != null)
            {
                float bw;
                if (!barWidth.TryGetValue(kv.Value, out bw)) bw = 216f;
                fill.sizeDelta = new Vector2(bw * v, 10f);
                fill.GetComponent<Image>().color = Color.Lerp(new Color(0.9f, 0.3f, 0.25f), new Color(0.46f, 0.69f, 0.25f), v * 1.4f);
            }
        }

        // الرسالة
        float a = Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.4f);
        var fc = flashText.color; fc.a = a; flashText.color = fc;

        // التلميح
        bool showHint = hintFrame >= Time.frameCount - 1 && !om.ended;
        hintText.gameObject.SetActive(showHint);
        if (showHint) PlaceAt(hintText.rectTransform, hintWorld);

        // الأشرطة اللي ما انطلبت هالفريم تختفي
        foreach (var kv in bars)
        {
            int f;
            if (kv.Value != null && (!barFrame.TryGetValue(kv.Key, out f) || f < Time.frameCount - 1)) kv.Value.gameObject.SetActive(false);
        }
    }

    // خطوات الطبق: وش تقطعين، وش تطبخين، وبعدين الصحن والتسليم
    public static string[] Steps(Recipe r)
    {
        var chop = new List<string>();
        var raw = new List<string>();
        bool meat = false, fry = false;
        foreach (var p in r.parts)
        {
            switch (p)
            {
                case Ingredient.Meat: meat = true; break;
                case Ingredient.Bun: raw.Add("bun"); break;
                case Ingredient.Potato: chop.Add("potato"); fry = true; break;
                default: chop.Add(p.ToString().ToLower()); break;
            }
        }
        var lines = new List<string>();
        if (chop.Count > 0) lines.Add("Chop the " + string.Join(" + ", chop.ToArray()));
        if (meat) lines.Add("Grill the meat");
        if (fry) lines.Add("Fry the potato");
        string all = string.Join(" + ", System.Array.ConvertAll(r.parts, x => x.ToString().ToLower()));
        lines.Add("Put " + all + " on a plate");
        lines.Add("Serve it");
        for (int i = 0; i < lines.Count; i++) lines[i] = (i + 1) + ". " + lines[i];
        return lines.ToArray();
    }

    RectTransform MakeTicket(Order o)
    {
        var steps = Steps(o.recipe);
        float h = 104 + steps.Length * 22;
        var t = Panel(ticketRow, "Ticket", new Vector2(0f, 1f), Vector2.zero, new Vector2(290, h), Paper);
        var title = Label(t, o.recipe.name, 24, Ink, TextAnchor.UpperLeft);
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(0, 1);
        title.rectTransform.anchoredPosition = new Vector2(14, -10);
        title.rectTransform.sizeDelta = new Vector2(-28, 30);

        float x = 12;
        foreach (var p in o.recipe.parts)
        {
            string label = p.ToString().ToUpper();
            float w = label.Length * 10 + 14;
            var chip = Panel(t, "Chip", new Vector2(0, 1), Vector2.zero, new Vector2(w, 24), IngColor(p));
            chip.pivot = new Vector2(0, 1);
            chip.anchoredPosition = new Vector2(x, -44);
            Label(chip, label, 13, Color.white, TextAnchor.MiddleCenter);
            x += w + 5;
        }
        float tw = Mathf.Max(290, x + 8);

        var stepText = Label(t, string.Join("\n", steps), 15, new Color(0.35f, 0.3f, 0.26f), TextAnchor.UpperLeft);
        stepText.fontStyle = FontStyle.Normal;
        stepText.rectTransform.anchorMin = new Vector2(0, 1); stepText.rectTransform.anchorMax = new Vector2(1, 1);
        stepText.rectTransform.pivot = new Vector2(0, 1);
        stepText.rectTransform.anchoredPosition = new Vector2(14, -76);
        stepText.rectTransform.sizeDelta = new Vector2(-24, steps.Length * 22);
        t.sizeDelta = new Vector2(tw, h);

        var bg = Panel(t, "BarBg", new Vector2(0, 0), Vector2.zero, new Vector2(tw - 20, 16), new Color(0.85f, 0.8f, 0.72f));
        bg.pivot = new Vector2(0, 0);
        bg.anchoredPosition = new Vector2(10, 10);
        var fill = Panel(bg, "Fill", new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(tw - 26, 10), new Color(0.46f, 0.69f, 0.25f));
        fill.pivot = new Vector2(0, 0.5f);
        barWidth[t] = tw - 26;
        return t;
    }

    readonly Dictionary<RectTransform, float> barWidth = new Dictionary<RectTransform, float>();

    // ---------- كتاب الوصفات (H) ----------
    RectTransform book;
    public static bool BookOpen { get { return I != null && I.book != null && I.book.gameObject.activeSelf; } }

    public static void ToggleBook(OrderManager m)
    {
        if (I == null) return;
        if (I.book == null) I.BuildBook(m);
        bool open = !I.book.gameObject.activeSelf;
        I.book.gameObject.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
    }

    void BuildBook(OrderManager m)
    {
        book = Panel(root, "RecipeBook", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.55f));
        book.GetComponent<Image>().sprite = null;
        book.anchorMin = Vector2.zero; book.anchorMax = Vector2.one; book.sizeDelta = Vector2.zero;
        int n = m.Menu.Count;
        var card = Panel(book, "Page", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 150 + n * 150), Paper);
        var title = Label(card, "RECIPE BOOK", 40, Ink, TextAnchor.MiddleCenter);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(700, 60);
        title.rectTransform.anchoredPosition = new Vector2(0, -50);
        for (int i = 0; i < n; i++)
        {
            var r = m.Menu[i];
            float y = -110 - i * 150;
            var name = Label(card, r.name + "   (" + r.points + " pts)", 26, new Color(0.85f, 0.35f, 0.45f), TextAnchor.UpperLeft);
            name.rectTransform.anchorMin = name.rectTransform.anchorMax = new Vector2(0f, 1f);
            name.rectTransform.pivot = new Vector2(0f, 1f);
            name.rectTransform.sizeDelta = new Vector2(680, 34);
            name.rectTransform.anchoredPosition = new Vector2(40, y);
            var st = Label(card, string.Join("\n", Steps(r)), 20, new Color(0.3f, 0.26f, 0.22f), TextAnchor.UpperLeft);
            st.fontStyle = FontStyle.Normal;
            st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0f, 1f);
            st.rectTransform.pivot = new Vector2(0f, 1f);
            st.rectTransform.sizeDelta = new Vector2(680, 110);
            st.rectTransform.anchoredPosition = new Vector2(56, y - 36);
        }
        var close = Label(card, "Press [H] to close", 20, new Color(0.5f, 0.45f, 0.4f), TextAnchor.MiddleCenter);
        close.rectTransform.anchorMin = close.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        close.rectTransform.sizeDelta = new Vector2(600, 40);
        close.rectTransform.anchoredPosition = new Vector2(0, 30);
        book.gameObject.SetActive(false);
    }

    void BuildResults(OrderManager m)
    {
        var dim = Panel(root, "Results", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.6f));
        dim.GetComponent<Image>().sprite = null;
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;

        var card = Panel(dim, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 470), Paper);
        var title = Label(card, m.Stars > 0 ? "LEVEL COMPLETE!" : "TIME'S UP!", 48, Ink, TextAnchor.MiddleCenter);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(600, 70);
        title.rectTransform.anchoredPosition = new Vector2(0, -60);

        int stars = m.Stars;
        for (int i = 0; i < 3; i++)
        {
            var s = Img(card, star, i < stars ? Gold : new Color(0.8f, 0.76f, 0.7f));
            s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            s.rectTransform.sizeDelta = i == 1 ? new Vector2(120, 120) : new Vector2(96, 96);
            s.rectTransform.anchoredPosition = new Vector2((i - 1) * 130, i == 1 ? -170 : -180);
        }

        var sc = Label(card, "SCORE  " + m.score, 40, Ink, TextAnchor.MiddleCenter);
        sc.rectTransform.anchorMin = sc.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        sc.rectTransform.sizeDelta = new Vector2(600, 60);
        sc.rectTransform.anchoredPosition = new Vector2(0, -275);

        var need = Label(card, "Stars at:  " + string.Join("  /  ", System.Array.ConvertAll(m.starScores, v => v.ToString())), 22, new Color(0.45f, 0.4f, 0.35f), TextAnchor.MiddleCenter);
        need.rectTransform.anchorMin = need.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        need.rectTransform.sizeDelta = new Vector2(600, 40);
        need.rectTransform.anchoredPosition = new Vector2(0, -320);

        MakeButton(card, "RETRY  (R)", new Vector2(-140, 60), new Color(0.35f, 0.32f, 0.3f), m.Retry);
        MakeButton(card, "NEXT  (Enter)", new Vector2(140, 60), m.Stars > 0 ? Pink : new Color(0.7f, 0.66f, 0.62f), m.Next);
    }

    // ---------- أدوات ----------

    void MakeButton(RectTransform parent, string text, Vector2 pos, Color c, UnityEngine.Events.UnityAction act)
    {
        var b = Panel(parent, "Button", new Vector2(0.5f, 0f), pos, new Vector2(250, 70), c);
        var btn = b.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(act);
        Label(b, text, 26, Color.white, TextAnchor.MiddleCenter);
    }

    void PlaceAt(RectTransform rt, Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(world);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out local);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = local;
    }

    RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = g.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = g.GetComponent<Image>();
        img.sprite = round;
        img.type = Image.Type.Sliced;
        img.color = c;
        return rt;
    }

    Image Img(RectTransform parent, Sprite s, Color c)
    {
        var g = new GameObject("Image", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        var img = g.GetComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.preserveAspect = true;
        return img;
    }

    Text Label(RectTransform parent, string text, int size, Color c, TextAnchor align)
    {
        var g = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = g.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var t = g.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = c;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Color IngColor(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.Lettuce: return new Color(0.42f, 0.7f, 0.26f);
            case Ingredient.Tomato: return new Color(0.89f, 0.3f, 0.24f);
            case Ingredient.Meat: return new Color(0.55f, 0.32f, 0.24f);
            case Ingredient.Bun: return new Color(0.8f, 0.56f, 0.3f);
            case Ingredient.Potato: return new Color(0.82f, 0.62f, 0.2f);
            default: return new Color(0.95f, 0.72f, 0.1f);
        }
    }

    static Sprite MakeRounded(int size, int r)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    static Sprite MakeStar(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float rad = (i % 2 == 0 ? 0.48f : 0.21f) * size;
            pts[i] = new Vector2(size / 2f + Mathf.Cos(ang) * rad, size / 2f + Mathf.Sin(ang) * rad - size * 0.03f);
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sx = 0; sx < 2; sx++)
                    for (int sy = 0; sy < 2; sy++)
                        if (Inside(pts, new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f))) hits++;
                tex.SetPixel(x, y, new Color(1, 1, 1, hits / 4f));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static bool Inside(Vector2[] poly, Vector2 p)
    {
        bool c = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) && (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) c = !c;
        return c;
    }
}
