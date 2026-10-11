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
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) PlayStory();
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) ChangeSkin(1);
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) ChangeSkin(-1);
            if (k.escapeKey.wasPressedThisFrame) Quit();
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

    int SkinCount { get { return skin != null && skin.skins.Length > 0 ? skin.skins.Length : 8; } }

    void ChangeSkin(int d)
    {
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
            if (skinText != null) skinText.text = skin.skins[i].name;
        }
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
        var shade = Img(root, fadeSprite, new Color(0.16f, 0.1f, 0.08f, 0.92f));
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
        Shadowed(t2, 7);
        var pill = Panel(title, "Tag", new Vector2(0f, 1f), new Vector2(6, -265), new Vector2(330, 46), Blue);
        pill.pivot = new Vector2(0f, 1f);
        Label(pill, "Nina's Cooking Story", 24, Color.white, TextAnchor.MiddleCenter);

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
            var card = Button(root, "", new Vector2(110 + i * 300, -600), new Vector2(280, 170), open ? Cream : new Color(0.45f, 0.4f, 0.38f), open ? new Color(0.85f, 0.8f, 0.72f) : new Color(0.3f, 0.27f, 0.25f), 20, () => PlayLevel(li));
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
            if (skin != null && i < skin.skins.Length) skinDots[i].color = skin.skins[i].apron;
        }
        skinText = Label(root, "Nina", 28, Cream, TextAnchor.UpperLeft);
        Anchor(skinText.rectTransform, new Vector2(116, -910), new Vector2(400, 40));

        // ===== تحت =====
        var quit = Button(root, "QUIT", new Vector2(0, 0), new Vector2(150, 54), new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0.05f), 22, Quit);
        quit.anchorMin = quit.anchorMax = new Vector2(1f, 1f);
        quit.pivot = new Vector2(1f, 1f);
        quit.anchoredPosition = new Vector2(-40, -36);

        var help = Panel(root, "Help", new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1240, 56), new Color(0.1f, 0.07f, 0.06f, 0.75f));
        help.pivot = new Vector2(0.5f, 0f);
        var ht = Label(help, "WASD  Move      E  Pick up / Put down      SPACE  Chop      SHIFT  Dash      H  Recipes", 22, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
        ht.fontStyle = FontStyle.Normal;
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
