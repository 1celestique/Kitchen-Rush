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

    float showNameUntil;

    public static int Selected
    {
        get { return PlayerPrefs.GetInt("ChefSkin_v2", 0); }
        set { PlayerPrefs.SetInt("ChefSkin_v2", value); PlayerPrefs.Save(); }
    }

    void Start()
    {
        if (useSavedSkin) current = Selected;
        Apply(current);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (pressKToChange && k != null && k.kKey.wasPressedThisFrame)
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

        if (Time.time > showNameUntil) return;
        var st = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.85f, Screen.width, 50), "Skin: " + skins[current].name, st);
    }

    static Color Hex(string h)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + h, out c);
        return c;
    }
}
