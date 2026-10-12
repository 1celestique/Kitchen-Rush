using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ==========================================================
// MainMenu: القائمة الرئيسية (تنبني لحالها)
// - اسم اللعبة كبير، زر العب (يبدأ بالقصة في الصالة)
// - بطاقات المراحل بالنجوم (الثانية تنفتح بعد نجمة في الأولى)
// - اختيار السكن بدواير الألوان (يتغير لون القبعة والمريلة على نينا)
// - Enter يلعب، ← → السكن، Esc يطلع
// ==========================================================
public class MainMenu : MonoBehaviour
{
    public string introScene = "Intro";
    public string[] levelScenes = { "level1", "level2" };
    public string[] levelNames = { "Nina's Kitchen", "Backyard Party" };
    public string[] comingSoon = { "Grand Opening" };   // مراحل جاية (مقفلة)
    public Transform preview;          // نينا اللي تدور جنب القائمة

    Font font;
    Sprite round, circle, star, fadeSprite;
    RectTransform root, title, playBtn;
    Text skinText;
    Image[] skinDots;
    ChefSkin skin;
    float baseYaw;

    static readonly Color Ink = new Color(0.2f, 0.14f, 0.12f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.92f);
    static readonly Color Pink = new Color(1f, 0.45f, 0.62f);
    static readonly Color PinkDark = new Color(0.86f, 0.3f, 0.47f);
    static readonly Color Blue = new Color(0.42f, 0.62f, 0.86f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);
    static readonly Color Grey = new Color(0.62f, 0.58f, 0.54f);

    void Start()
    {
        Time.timeScale = 1f;
        skin = FindAnyObjectByType<ChefSkin>();
        if (preview == null && skin != null) preview = skin.transform;
        if (preview != null) baseYaw = preview.eulerAngles.y;
        if (skin != null) { skin.showName = false; skin.useSavedSkin = true; }
        KitchenAudio.Music("menu");
        Build();
        RefreshSkin();
        if (!started) BuildStart();
    }

    // ---------- شاشة البداية (Press any key) ----------
    static bool started;
    RectTransform startScreen;
    Text pressText;
    RectTransform startLogo;
    float startClosing = -1f;

    readonly System.Collections.Generic.List<GameObject> menuParts = new System.Collections.Generic.List<GameObject>();

    // شاشة البداية فوق المطبخ نفسه: نينا تبان، والشعار كبير بالنص، و"اضغطي أي زر" تحت
    void BuildStart()
    {
        // نخفي القائمة لين تبدأ
        foreach (Transform c in root) { menuParts.Add(c.gameObject); c.gameObject.SetActive(false); }

        startScreen = new GameObject("StartScreen", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        startScreen.SetParent(root, false);
        startScreen.anchorMin = Vector2.zero; startScreen.anchorMax = Vector2.one; startScreen.sizeDelta = Vector2.zero;

        // تظليل خفيف فوق كل شي، وأغمق تحت
        var dim = Img(startScreen, null, new Color(0.08f, 0.05f, 0.04f, 0.25f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.sizeDelta = Vector2.zero;
        dim.raycastTarget = true;
        var grad = Img(startScreen, MakeVerticalFade(), new Color(0.1f, 0.06f, 0.05f, 0.9f));
        grad.type = Image.Type.Simple;
        grad.preserveAspect = false;
        grad.rectTransform.anchorMin = new Vector2(0f, 0f); grad.rectTransform.anchorMax = new Vector2(1f, 0.55f);
        grad.rectTransform.sizeDelta = Vector2.zero;

        // الشعار
        startLogo = new GameObject("Logo", typeof(RectTransform)).GetComponent<RectTransform>();
        startLogo.SetParent(startScreen, false);
        startLogo.anchorMin = startLogo.anchorMax = new Vector2(0.37f, 0.6f);
        startLogo.sizeDelta = new Vector2(1000, 460);
        var pill = Panel(startLogo, "Tag", new Vector2(0.5f, 1f), new Vector2(-150, 0), new Vector2(360, 48), Pink);
        pill.pivot = new Vector2(0.5f, 1f);
        var pt = Label(pill, "NINA'S COOKING STORY", 20, Color.white, TextAnchor.MiddleCenter);
        var k1 = Label(startLogo, "KITCHEN", 150, Cream, TextAnchor.UpperCenter);
        k1.rectTransform.offsetMax = new Vector2(-150, -40);
        Shadowed(k1, 10);
        var k2 = Label(startLogo, "RUSH", 210, Pink, TextAnchor.UpperCenter);
        k2.fontStyle = FontStyle.BoldAndItalic;
        k2.rectTransform.offsetMin = new Vector2(250, 0);
        k2.rectTransform.offsetMax = new Vector2(0, -165);
        var ol = k2.gameObject.AddComponent<Outline>();
        ol.effectColor = Cream; ol.effectDistance = new Vector2(5, -5);
        var ol2 = k2.gameObject.AddComponent<Shadow>();
        ol2.effectColor = new Color(0.45f, 0.12f, 0.25f, 0.6f); ol2.effectDistance = new Vector2(8, -12);

        // اضغطي أي زر (حبة شفافة بحدود)
        var press = Panel(startScreen, "Press", new Vector2(0.37f, 0f), new Vector2(0, 170), new Vector2(520, 72), new Color(1f, 1f, 1f, 0.12f));
        var pol = press.gameObject.AddComponent<Outline>();
        pol.effectColor = new Color(1f, 1f, 1f, 0.55f); pol.effectDistance = new Vector2(2, -2);
        pressText = Label(press, "Press any key to start", 28, Color.white, TextAnchor.MiddleCenter);

        var by = Label(startScreen, "A game by  <b>Nouf Alsharqi</b>   ·   © 2026", 20, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerCenter);
        by.fontStyle = FontStyle.Normal;
        by.supportRichText = true;
        by.rectTransform.offsetMin = new Vector2(0, 36);
        UIFonts.Apply(startScreen.gameObject);
    }

    static Sprite MakeVerticalFade()
    {
        var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 128; y++)
        {
            float a = 1f - Mathf.SmoothStep(0f, 1f, y / 127f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f));
    }

    void UpdateStart()
    {
        float t = Time.time;
        if (startLogo != null)
        {
            startLogo.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.6f) * 0.012f);
            startLogo.anchoredPosition = new Vector2(0, Mathf.Sin(t * 1.2f) * 6f);
        }
        if (pressText != null) pressText.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Sin(t * 4f));

        if (startClosing < 0f)
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            if ((k != null && k.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame))
            {
                startClosing = 0f;
                KitchenAudio.Play("serve");
            }
            return;
        }
        // تختفي وتطلع القائمة
        startClosing += Time.deltaTime / 0.45f;
        var g = startScreen.GetComponent<CanvasGroup>();
        g.alpha = 1f - startClosing;
        startScreen.localScale = Vector3.one * (1f + startClosing * 0.08f);
        if (startClosing >= 1f)
        {
            started = true;
            foreach (var g0 in menuParts) if (g0 != null) g0.SetActive(true);
            Destroy(startScreen.gameObject);
            startScreen = null;
        }
    }

    Vector3 camBase;
    bool camBaseSet;

    void LateUpdate()
    {
        // حركة كاميرا خفيفة جداً فوق الطاولة
        var cam = Camera.main;
        if (cam == null) return;
        if (!camBaseSet) { camBase = cam.transform.position; camBaseSet = true; }
        float t = Time.time;
        cam.transform.position = camBase + new Vector3(Mathf.Sin(t * 0.25f) * 0.05f, Mathf.Sin(t * 0.33f) * 0.025f, Mathf.Sin(t * 0.2f) * 0.03f);
    }

    void Update()
    {
        // F9: عرض كامل للعبة (للفيديو)
        var kd = Keyboard.current;
        if (kd != null && kd.f9Key.wasPressedThisFrame) DemoDirector.Begin();
        if (startScreen != null) { UpdateStart(); return; }
        var k = Keyboard.current;
        if (k != null)
        {
            if (modal == null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) PlayStory();
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) ChangeSkin(1);
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) ChangeSkin(-1);
            if (k.escapeKey.wasPressedThisFrame && modal != null) CloseModal();
        }
        if (preview != null)
            preview.rotation = Quaternion.Euler(0f, baseYaw + Mathf.Sin(Time.time * 0.6f) * 22f, 0f);
        // حركة خفيفة للعنوان
        if (title != null) title.anchoredPosition = new Vector2(110, -70 + Mathf.Sin(Time.time * 1.6f) * 5f);
    }

    // ---------- الأزرار ----------

    void PlayStory()
    {
        KitchenAudio.Play("serve");
        if (Application.CanStreamedLevelBeLoaded(introScene)) SceneManager.LoadScene(introScene);
        else SceneManager.LoadScene(levelScenes[0]);
    }

    void PlayLevel(int level)
    {
        if (!Unlocked(level)) { KitchenAudio.Play("wrong"); return; }
        KitchenAudio.Play("serve");
        SceneManager.LoadScene(levelScenes[level]);
    }

    bool Unlocked(int level)
    {
        if (level <= 0) return true;
        return PlayerPrefs.GetInt("Stars_" + levelScenes[level - 1], 0) > 0;
    }

    [System.NonSerialized] ChefSkin.Skin[] defaults;
    ChefSkin.Skin[] Skins { get { if (skin != null && skin.skins != null && skin.skins.Length > 0) return skin.skins; if (defaults == null || defaults.Length == 0) defaults = ChefSkin.DefaultSkins(); return defaults; } }
    int SkinCount { get { return Skins.Length; } }

    void ChangeSkin(int d)
    {
        if (SkinCount == 0) return;
        SetSkin((ChefSkin.Selected + d + SkinCount) % SkinCount);
    }

    void SetSkin(int i)
    {
        ChefSkin.Selected = i;
        KitchenAudio.Play("click");
        RefreshSkin();
    }

    void RefreshSkin()
    {
        int i = Mathf.Clamp(ChefSkin.Selected, 0, SkinCount - 1);
        if (skin != null && skin.skins.Length > 0)
        {
            skin.Apply(i);
        }
        var all = Skins;
        if (skinText != null && all != null && i >= 0 && i < all.Length) skinText.text = all[i].name;
        if (skinDots != null)
            for (int d = 0; d < skinDots.Length; d++)
            {
                var ring = skinDots[d].transform.parent.GetComponent<Image>();
                ring.color = d == i ? Ink : new Color(1f, 1f, 1f, 0f);
                skinDots[d].transform.parent.localScale = Vector3.one * (d == i ? 1.15f : 1f);
            }
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- بناء الواجهة ----------

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        round = KitchenHUD.MakeRounded(64, 24);
        circle = KitchenHUD.MakeRounded(64, 32);
        star = KitchenHUD.MakeStar(128);
        fadeSprite = MakeFade();

        var g = new GameObject("Menu UI");
        var canvas = g.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = g.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        g.AddComponent<GraphicRaycaster>();
        root = g.GetComponent<RectTransform>();
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // تظليل دافي يسار عشان الكلام يبان والمطبخ يبقى ورا
        var shade = Img(root, fadeSprite, new Color(0.2f, 0.14f, 0.12f, 0.62f));
        shade.type = Image.Type.Simple;
        shade.rectTransform.anchorMin = new Vector2(0f, 0f); shade.rectTransform.anchorMax = new Vector2(0f, 1f);
        shade.rectTransform.pivot = new Vector2(0f, 0.5f);
        shade.rectTransform.sizeDelta = new Vector2(1150, 0);

        // ===== العنوان =====
        title = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
        title.SetParent(root, false);
        title.anchorMin = title.anchorMax = new Vector2(0f, 1f);
        title.pivot = new Vector2(0f, 1f);
        title.sizeDelta = new Vector2(800, 300);
        var t1 = Label(title, "KITCHEN", 110, Cream, TextAnchor.UpperLeft);
        Shadowed(t1, 6);
        var t2 = Label(title, "RUSH", 150, Pink, TextAnchor.UpperLeft);
        t2.fontStyle = FontStyle.BoldAndItalic;
        t2.rectTransform.offsetMin = new Vector2(150, 0);
        t2.rectTransform.offsetMax = new Vector2(150, -100);
        var t2o = t2.gameObject.AddComponent<Outline>();
        t2o.effectColor = Cream; t2o.effectDistance = new Vector2(4, -4);
        Shadowed(t2, 7);
        var pill = Panel(title, "Tag", new Vector2(0f, 1f), new Vector2(6, -265), new Vector2(330, 46), Blue);
        pill.pivot = new Vector2(0f, 1f);
        Label(pill, "Nina's Cooking Story", 24, Color.white, TextAnchor.MiddleCenter);
        // مجموع النجوم
        int total = 0;
        foreach (var sc0 in levelScenes) total += PlayerPrefs.GetInt("Stars_" + sc0, 0);
        var sp = Panel(title, "StarsTotal", new Vector2(0f, 1f), new Vector2(350, -265), new Vector2(150, 46), new Color(1f, 1f, 1f, 0.15f));
        sp.pivot = new Vector2(0f, 1f);
        var starIcon = Img(sp, star, Gold);
        starIcon.rectTransform.anchorMin = starIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        starIcon.rectTransform.sizeDelta = new Vector2(30, 30);
        starIcon.rectTransform.anchoredPosition = new Vector2(30, 0);
        var stt = Label(sp, total + " / " + (levelScenes.Length * 3), 24, Cream, TextAnchor.MiddleCenter);
        stt.rectTransform.offsetMin = new Vector2(40, 0);

        // ===== العب =====
        playBtn = Button(root, "PLAY", new Vector2(110, -430), new Vector2(420, 104), Pink, PinkDark, 50, PlayStory);
        var sub = Label(playBtn, "Start the story", 18, new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerCenter);
        sub.fontStyle = FontStyle.Normal;
        sub.rectTransform.offsetMin = new Vector2(0, 8);
        playBtn.GetChild(0).GetComponent<RectTransform>().offsetMin = new Vector2(0, 14);



        // ===== المراحل =====
        var lvTitle = Label(root, "LEVELS", 26, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperLeft);
        Anchor(lvTitle.rectTransform, new Vector2(116, -560), new Vector2(300, 34));
        for (int i = 0; i < levelScenes.Length; i++)
        {
            int li = i;
            bool open = Unlocked(i);
            var card = Button(root, "", new Vector2(110 + i * 270, -600), new Vector2(250, 170), open ? Cream : new Color(0.45f, 0.4f, 0.38f), open ? new Color(0.85f, 0.8f, 0.72f) : new Color(0.3f, 0.27f, 0.25f), 20, () => PlayLevel(li));
            var num = Panel(card, "Num", new Vector2(0f, 1f), new Vector2(16, -16), new Vector2(56, 56), open ? (i == 0 ? Pink : Blue) : Grey);
            num.GetComponent<Image>().sprite = circle;
            num.pivot = new Vector2(0f, 1f);
            Label(num, (i + 1).ToString(), 30, Color.white, TextAnchor.MiddleCenter);
            var nm = Label(card, levelNames[i], 24, open ? Ink : new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft);
            nm.rectTransform.offsetMin = new Vector2(84, 0);
            nm.rectTransform.offsetMax = new Vector2(-10, -30);
            nm.horizontalOverflow = HorizontalWrapMode.Wrap;
            int stars = PlayerPrefs.GetInt("Stars_" + levelScenes[i], 0);
            for (int s = 0; s < 3 && open; s++)
            {
                var im = Img(card, star, s < stars ? Gold : (open ? new Color(0.82f, 0.77f, 0.7f) : new Color(1f, 1f, 1f, 0.25f)));
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0f, 0f);
                im.rectTransform.pivot = new Vector2(0f, 0f);
                im.rectTransform.sizeDelta = new Vector2(40, 40);
                im.rectTransform.anchoredPosition = new Vector2(18 + s * 44, 18);
            }
            if (!open)
            {
                var lk = Label(card, "LOCKED\nGet 1 star in Level " + i, 18, new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerLeft);
                lk.fontStyle = FontStyle.Normal;
                lk.rectTransform.offsetMin = new Vector2(18, 18);
            }
        }
        // مراحل جاية قريب
        for (int j = 0; j < comingSoon.Length; j++)
        {
            int i = levelScenes.Length + j;
            var card = Button(root, "", new Vector2(110 + i * 270, -600), new Vector2(250, 170), new Color(1f, 1f, 1f, 0.14f), new Color(0f, 0f, 0f, 0.15f), 20, () => { KitchenAudio.Play("wrong"); });
            var num = Panel(card, "Num", new Vector2(0f, 1f), new Vector2(16, -16), new Vector2(56, 56), new Color(1f, 1f, 1f, 0.25f));
            num.GetComponent<Image>().sprite = circle;
            num.pivot = new Vector2(0f, 1f);
            Label(num, (i + 1).ToString(), 30, Color.white, TextAnchor.MiddleCenter);
            var tag = Panel(card, "Soon", new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(190, 42), Gold);
            var tl = Label(tag, "COMING SOON", 20, Ink, TextAnchor.MiddleCenter);
        }

        // ===== السكن =====
        var skTitle = Label(root, "OUTFIT", 26, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperLeft);
        Anchor(skTitle.rectTransform, new Vector2(116, -800), new Vector2(300, 34));
        int count = SkinCount;
        skinDots = new Image[count];
        for (int i = 0; i < count; i++)
        {
            int si = i;
            var ring = Panel(root, "SkinRing", new Vector2(0f, 1f), new Vector2(110 + i * 64, -842), new Vector2(54, 54), Color.clear);
            ring.pivot = new Vector2(0f, 1f);
            ring.GetComponent<Image>().sprite = circle;
            var b = ring.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.onClick.AddListener(() => SetSkin(si));
            ring.gameObject.AddComponent<Hover>();
            var dot = Panel(ring, "Dot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44), Color.white);
            dot.GetComponent<Image>().sprite = circle;
            dot.GetComponent<Image>().raycastTarget = false;
            skinDots[i] = dot.GetComponent<Image>();
            if (i < Skins.Length) skinDots[i].color = Skins[i].apron;
        }
        skinText = Label(root, "Nina", 28, Cream, TextAnchor.UpperLeft);
        Anchor(skinText.rectTransform, new Vector2(116, -910), new Vector2(400, 40));

        // ===== تحت =====
        // أزرار فوق يمين: طريقة اللعب، الوصفات، الإعدادات، عن اللعبة، خروج
        string[] names = { "HOW TO PLAY", "SETTINGS", "CREDITS", "QUIT" };
        UnityEngine.Events.UnityAction[] acts = { ShowHowTo, ShowSettings, ShowCredits, Quit };
        float[] widths = { 200, 160, 150, 120 };
        float rx = -40;
        for (int i = names.Length - 1; i >= 0; i--)
        {
            bool q = i == names.Length - 1;
            var b = Button(root, names[i], Vector2.zero, new Vector2(widths[i], 54), q ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 0.97f, 0.92f, 0.95f), new Color(0f, 0f, 0f, 0.15f), 20, acts[i]);
            b.anchorMin = b.anchorMax = new Vector2(1f, 1f);
            b.pivot = new Vector2(1f, 1f);
            b.anchoredPosition = new Vector2(rx, -36);
            if (!q) b.GetChild(0).GetComponent<Text>().color = Ink;
            rx -= widths[i] + 12;
        }

        // تحت: حقوق اللعبة
        var credit = Panel(root, "Credit", new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(560, 50), new Color(0.1f, 0.07f, 0.06f, 0.6f));
        credit.pivot = new Vector2(0.5f, 0f);
        var ct = Label(credit, "A game by  <b>Nouf Alsharqi</b>   ·   © 2026", 22, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter);
        ct.fontStyle = FontStyle.Normal;
        ct.supportRichText = true;
        UIFonts.Apply(root.gameObject);
    }

    // ---------- النوافذ (طريقة اللعب، الوصفات، الإعدادات، عن اللعبة) ----------
    RectTransform modal;

    void CloseModal()
    {
        if (modal != null) Destroy(modal.gameObject);
        modal = null;
        KitchenAudio.Play("click");
    }

    RectTransform OpenModal(string head, float h)
    {
        if (modal != null) Destroy(modal.gameObject);
        KitchenAudio.Play("click");
        modal = Panel(root, "Modal", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.04f, 0.6f));
        modal.anchorMin = Vector2.zero; modal.anchorMax = Vector2.one; modal.sizeDelta = Vector2.zero;
        modal.GetComponent<Image>().sprite = null;
        modal.GetComponent<Image>().raycastTarget = true;
        var card = Panel(modal, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, h), Cream);
        var sh = card.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.4f); sh.effectDistance = new Vector2(8, -12);
        // شريط التوليب والأكل فوق
        var band = new GameObject("Band", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        band.rectTransform.SetParent(card, false);
        band.rectTransform.anchorMin = new Vector2(0f, 1f); band.rectTransform.anchorMax = new Vector2(1f, 1f);
        band.rectTransform.pivot = new Vector2(0.5f, 1f);
        band.rectTransform.sizeDelta = new Vector2(-24, 110);
        band.rectTransform.anchoredPosition = new Vector2(0, -12);
        band.texture = PatternArt.Get();
        band.uvRect = new Rect(0f, 0f, 876f / 300f, 110f / 300f);
        band.raycastTarget = false;
        var tag = Panel(card, "Head", new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(380, 60), Cream);
        var ht = Label(tag, head, 34, Ink, TextAnchor.MiddleCenter);
        var x = Button(card, "X", new Vector2(0, 0), new Vector2(54, 54), Pink, PinkDark, 26, CloseModal);
        x.anchorMin = x.anchorMax = new Vector2(1f, 1f);
        x.pivot = new Vector2(1f, 1f);
        x.anchoredPosition = new Vector2(-20, -20);
        var close = Label(card, "Esc  Close", 18, Grey, TextAnchor.LowerCenter);
        close.fontStyle = FontStyle.Normal;
        close.rectTransform.offsetMin = new Vector2(0, 14);
        return card;
    }

    Text Body(RectTransform card, string text, float top, int size = 24)
    {
        var t = Label(card, text, size, new Color(0.3f, 0.25f, 0.21f), TextAnchor.UpperLeft);
        t.fontStyle = FontStyle.Normal;
        t.supportRichText = true;
        t.lineSpacing = 1.25f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.rectTransform.offsetMin = new Vector2(60, 40);
        t.rectTransform.offsetMax = new Vector2(-60, -top);
        return t;
    }

    void ShowHowTo()
    {
        var c = OpenModal("HOW TO PLAY", 700);
        Body(c, "<b>1.</b>  Check the order tickets at the top left.\n" +
                "<b>2.</b>  Grab ingredients from the crates with <b>[E]</b>.\n" +
                "<b>3.</b>  Chop them on the cutting board: hold <b>[Space]</b>.\n" +
                "<b>4.</b>  Put raw meat on the grill. Take it off when it's brown, before it burns!\n" +
                "<b>5.</b>  Take a plate and put everything on it with <b>[E]</b>.\n" +
                "<b>6.</b>  Serve it before the ticket runs out. Faster = more points!\n\n" +
                "<color=#E0607E><b>Move</b></color> WASD    <color=#E0607E><b>Dash</b></color> Shift    <color=#E0607E><b>Recipes</b></color> H    <color=#E0607E><b>Trash</b></color> drop it in the bin", 150);
        UIFonts.Apply(modal.gameObject);
    }

    void ShowRecipes()
    {
        Recipe[] all = { OrderManager.Salad, OrderManager.CheeseSalad, OrderManager.Sandwich, OrderManager.Burger, OrderManager.Cheeseburger };
        var c = OpenModal("RECIPES", 860);
        var sb = new System.Text.StringBuilder();
        foreach (var r in all)
        {
            sb.Append("<color=#E0607E><b>" + r.name + "</b></color>   <size=18>(" + r.points + " pts)</size>\n");
            sb.Append("<size=20>" + string.Join("    ", KitchenHUD.Steps(r)) + "</size>\n\n");
        }
        Body(c, sb.ToString(), 150, 24);
        UIFonts.Apply(modal.gameObject);
    }

    void ShowSettings()
    {
        var c = OpenModal("SETTINGS", 560);
        VolumeRow(c, "Music", -180, () => KitchenAudio.MusicVolume, v => KitchenAudio.SetVolumes(v, KitchenAudio.SfxVolume));
        VolumeRow(c, "Sounds", -270, () => KitchenAudio.SfxVolume, v => KitchenAudio.SetVolumes(KitchenAudio.MusicVolume, v));
        RectTransform fs = null;
        bool fsOn = Screen.fullScreen;
        fs = Button(c, fsOn ? "Fullscreen: ON" : "Fullscreen: OFF", new Vector2(60, -350), new Vector2(330, 64), Blue, new Color(0.3f, 0.45f, 0.65f), 24, () =>
        {
            fsOn = !fsOn;
            Screen.fullScreen = fsOn;
            fs.GetChild(0).GetComponent<Text>().text = fsOn ? "Fullscreen: ON" : "Fullscreen: OFF";
        });
        Button(c, "Reset progress", new Vector2(510, -350), new Vector2(330, 64), new Color(0.55f, 0.5f, 0.47f), new Color(0.35f, 0.32f, 0.3f), 24, () =>
        {
            foreach (var sc0 in levelScenes) PlayerPrefs.DeleteKey("Stars_" + sc0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        UIFonts.Apply(modal.gameObject);
    }

    void VolumeRow(RectTransform c, string name, float y, System.Func<float> get, System.Action<float> set)
    {
        var l = Label(c, name, 28, Ink, TextAnchor.MiddleLeft);
        Anchor(l.rectTransform, new Vector2(60, y), new Vector2(200, 64));
        var barBg = Panel(c, "Bar", new Vector2(0f, 1f), new Vector2(330, y - 20), new Vector2(360, 24), new Color(0.85f, 0.8f, 0.72f));
        barBg.pivot = new Vector2(0f, 1f);
        var fill = Panel(barBg, "Fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(360 * get(), 24), Pink);
        fill.pivot = new Vector2(0f, 0.5f);
        System.Action<float> change = d =>
        {
            float v = Mathf.Clamp01(Mathf.Round((get() + d) * 10f) / 10f);
            set(v);
            fill.sizeDelta = new Vector2(360 * v, 24);
            KitchenAudio.Play("click");
        };
        Button(c, "-", new Vector2(250, y), new Vector2(64, 64), Ink, new Color(0, 0, 0, 0.3f), 34, () => change(-0.1f));
        Button(c, "+", new Vector2(710, y), new Vector2(64, 64), Ink, new Color(0, 0, 0, 0.3f), 34, () => change(0.1f));
    }

    void ShowCredits()
    {
        var c = OpenModal("CREDITS", 760);
        var title = Label(c, "Kitchen Rush", 40, Ink, TextAnchor.MiddleCenter);
        Anchor(title.rectTransform, new Vector2(0, -140), new Vector2(900, 50));
        var sub = Label(c, "Nina's Cooking Story", 22, Pink, TextAnchor.MiddleCenter);
        sub.fontStyle = FontStyle.Italic;
        Anchor(sub.rectTransform, new Vector2(0, -190), new Vector2(900, 34));

        // صندوق صغير: صاحبة اللعبة
        var by = Panel(c, "By", new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(520, 70), new Color(0.97f, 0.89f, 0.86f));
        var byT = Label(by, "Game design & development\n<b>Nouf Alsharqi</b>", 22, Ink, TextAnchor.MiddleCenter);
        byT.fontStyle = FontStyle.Normal;
        byT.supportRichText = true;

        string[,] rows =
        {
            { "Characters & animations", "Mixamo" },
            { "Kitchen models", "Low Poly Kitchen Starter Pack" },
            { "Food models", "Ultimate Food Pack, Kenney Food Kit" },
            { "Fonts", "Poppins, Lora" },
            { "Music & sounds", "Made in code" },
            { "Engine", "Unity 6" },
        };
        float y = -335;
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            if (i % 2 == 0)
            {
                var stripe = Panel(c, "Row", new Vector2(0.5f, 1f), new Vector2(0, y - 2), new Vector2(780, 44), new Color(0.95f, 0.9f, 0.82f));
                stripe.pivot = new Vector2(0.5f, 1f);
            }
            var k = Label(c, rows[i, 0], 21, new Color(0.45f, 0.38f, 0.33f), TextAnchor.MiddleRight);
            k.fontStyle = FontStyle.Normal;
            Anchor(k.rectTransform, new Vector2(70, y), new Vector2(350, 40));
            var v = Label(c, rows[i, 1], 21, Ink, TextAnchor.MiddleLeft);
            Anchor(v.rectTransform, new Vector2(450, y), new Vector2(400, 40));
            y -= 48;
        }
        var thanks = Label(c, "Thanks for playing!", 22, Pink, TextAnchor.MiddleCenter);
        Anchor(thanks.rectTransform, new Vector2(0, y - 14), new Vector2(900, 36));
        UIFonts.Apply(modal.gameObject);
    }

    // ---------- أدوات ----------

    RectTransform Button(RectTransform parent, string text, Vector2 pos, Vector2 size, Color c, Color shadow, int fontSize, UnityEngine.Events.UnityAction act)
    {
        var b = Panel(parent, "Button", new Vector2(0f, 1f), pos, size, c);
        b.pivot = new Vector2(0f, 1f);
        var sh = b.gameObject.AddComponent<Shadow>();
        sh.effectColor = shadow;
        sh.effectDistance = new Vector2(0, -7);
        var btn = b.gameObject.AddComponent<UnityEngine.UI.Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(act);
        b.gameObject.AddComponent<Hover>();
        var t = Label(b, text, fontSize, Color.white, TextAnchor.MiddleCenter);
        Shadowed(t, 2);
        return b;
    }

    void Anchor(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static void Shadowed(Text t, float d)
    {
        var s = t.gameObject.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.35f);
        s.effectDistance = new Vector2(d * 0.5f, -d);
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
        img.raycastTarget = false;
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

    // تدرج من غامق يسار لشفاف يمين
    static Sprite MakeFade()
    {
        int w = 256;
        var tex = new Texture2D(w, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < w; x++)
        {
            float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(w * 0.45f, w, x));
            for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, 4), new Vector2(0.5f, 0.5f));
    }

    // يكبر شوي لما الماوس فوقه
    public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        static Transform over;
        float want = 1f;
        public static bool IsOver(Transform t) { return over == t; }
        public void OnPointerEnter(PointerEventData e) { want = 1.06f; over = transform; KitchenAudio.Play("click", 0.4f); }
        public void OnPointerExit(PointerEventData e) { want = 1f; if (over == transform) over = null; }
        void Update()
        {
            if (want == 1f && over != transform && transform.localScale.x > 1.1f) return;   // دواير السكن المختارة
            float s = Mathf.Lerp(transform.localScale.x, want, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            if (over == transform || Mathf.Abs(s - want) > 0.001f) transform.localScale = Vector3.one * s;
        }
    }
}
