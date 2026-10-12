using UnityEngine;
using UnityEngine.UI;

// ==========================================================
// UIFonts: خطوط اللعبة (Poppins للواجهة، Lora مائل لكلام الراوي بالقصة)
// الخطوط مجانية (OFL) في Assets/KitchenRush/Resources/Fonts
// Apply يمشي على كل النصوص ويحط الخط المناسب حسب (Bold / Italic / Normal)
// ==========================================================
public static class UIFonts
{
    static Font bold, boldItalic, medium, regular, story, fallback;
    static bool loaded;

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bold = Resources.Load<Font>("Fonts/Poppins-Bold");
        boldItalic = Resources.Load<Font>("Fonts/Poppins-BoldItalic");
        medium = Resources.Load<Font>("Fonts/Poppins-Medium");
        regular = Resources.Load<Font>("Fonts/Poppins-Regular");
        story = Resources.Load<Font>("Fonts/Lora-Italic");
    }

    public static Font Bold { get { Load(); return bold != null ? bold : fallback; } }
    public static Font Body { get { Load(); return medium != null ? medium : fallback; } }
    public static Font Story { get { Load(); return story != null ? story : (medium != null ? medium : fallback); } }

    // يحط الخط على نص واحد حسب ستايله
    public static void Apply(Text t)
    {
        if (t == null) return;
        Load();
        if (bold == null) return;   // الخطوط مو موجودة: نخلي الخط الأصلي
        switch (t.fontStyle)
        {
            case FontStyle.Bold:
                t.font = bold; t.fontStyle = FontStyle.Normal; break;
            case FontStyle.BoldAndItalic:
                t.font = boldItalic != null ? boldItalic : bold; t.fontStyle = FontStyle.Normal; break;
            case FontStyle.Italic:
                t.font = story != null ? story : medium; t.fontStyle = FontStyle.Normal; break;
            default:
                if (t.font == bold || t.font == boldItalic || t.font == story) break;   // سبق وتحدد
                t.font = medium != null ? medium : regular; break;
        }
    }

    // على كل النصوص تحت كائن
    public static void Apply(GameObject root)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Text>(true)) Apply(t);
    }
}
