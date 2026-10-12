using UnityEngine;

// ==========================================================
// PatternArt: خلفية زرقاء فيها توليب وأكل (برقر، طماطم، جبن، كب كيك)
// مرسومة بالكود وتتكرر بدون خطوط، تستخدمها القصة والقائمة
// ==========================================================
public static class PatternArt
{
    static Texture2D cache;
    public static float Strength = 0.38f;   // كم تبان الرسومات (0 = خلفية سادة، 1 = واضحة)
    const int N = 512;
    static Color[] px;

    public static Texture2D Get()
    {
        if (cache != null) return cache;
        px = new Color[N * N];
        var bg = new Color(0.6f, 0.73f, 0.9f);
        var bg2 = new Color(0.62f, 0.75f, 0.91f);
        // خلفية بخطوط مايلة خفيفة
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                px[y * N + x] = ((x + y) / 22) % 2 == 0 ? bg : bg2;

        var rnd = new System.Random(12);
        int cells = 4;
        float cs = N / (float)cells;
        for (int cy = 0; cy < cells; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                float x = (cx + 0.5f) * cs + (cy % 2 == 0 ? 0f : cs * 0.5f) + (float)(rnd.NextDouble() - 0.5) * 16f;
                float y = (cy + 0.5f) * cs + (float)(rnd.NextDouble() - 0.5) * 16f;
                int kind = (cx + cy * 2) % 5;
                for (int pass = 0; pass < 2; pass++)
                {
                    switch (kind)
                    {
                        case 0: Tulip(x, y, pass, new Color(1f, 0.45f, 0.62f)); break;
                        case 1: Burger(x, y, pass); break;
                        case 2: Tulip(x, y, pass, new Color(1f, 0.82f, 0.3f)); break;
                        case 3: Tomato(x, y, pass); break;
                        default: if (cy % 2 == 0) Cheese(x, y, pass); else Cupcake(x, y, pass); break;
                    }
                }
            }
        // نقاط صغيرة بين الرسومات
        for (int i = 0; i < 30; i++)
        {
            float x = (float)rnd.NextDouble() * N, y = (float)rnd.NextDouble() * N;
            Ellipse(x, y, 2.5f, 2.5f, new Color(1f, 1f, 1f, 0.5f));
        }
        // نخليها هادية: الرسومات باهتة وقريبة من لون الخلفية
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Color b0 = ((x + y) / 22) % 2 == 0 ? bg : bg2;
                Color c = Color.Lerp(b0, px[y * N + x], Strength);
                px[y * N + x] = c;
            }

        cache = new Texture2D(N, N, TextureFormat.RGBA32, true);
        cache.wrapMode = TextureWrapMode.Repeat;
        cache.filterMode = FilterMode.Trilinear;
        cache.SetPixels(px);
        cache.Apply();
        px = null;
        return cache;
    }

    // ---------- الرسومات (pass 0 = حدود بيضا، pass 1 = الألوان) ----------
    static readonly Color Edge = new Color(1f, 1f, 1f, 0.95f);

    static void Tulip(float x, float y, int pass, Color petal)
    {
        float o = pass == 0 ? 4f : 0f;
        Color green = pass == 0 ? Edge : new Color(0.35f, 0.7f, 0.4f);
        Rect(x - 3 - o, y - 42 - o, 6 + o * 2, 44 + o * 2, green);                 // الساق
        Ellipse(x - 12, y - 26, 10 + o, 5 + o, green);                               // ورقة
        Ellipse(x + 12, y - 32, 10 + o, 5 + o, green);
        Color p = pass == 0 ? Edge : petal;
        Ellipse(x, y + 8, 17 + o, 16 + o, p);                                        // الوردة
        Ellipse(x - 11, y + 20, 7 + o, 11 + o, p);
        Ellipse(x + 11, y + 20, 7 + o, 11 + o, p);
        Ellipse(x, y + 22, 7 + o, 12 + o, p);
        if (pass == 1) Ellipse(x - 5, y + 10, 4, 6, new Color(1f, 1f, 1f, 0.35f));  // لمعة
    }

    static void Burger(float x, float y, int pass)
    {
        float o = pass == 0 ? 4f : 0f;
        Color E = Edge;
        Ellipse(x, y + 14, 30 + o, 15 + o, pass == 0 ? E : new Color(0.95f, 0.65f, 0.3f));   // الخبزة فوق
        Rect(x - 30 - o, y - 1 - o, 60 + o * 2, 8 + o * 2, pass == 0 ? E : new Color(0.45f, 0.8f, 0.35f)); // خس
        Rect(x - 28 - o, y - 6 - o, 56 + o * 2, 6 + o, pass == 0 ? E : new Color(1f, 0.85f, 0.25f));     // جبن
        Rect(x - 28 - o, y - 16 - o, 56 + o * 2, 11 + o, pass == 0 ? E : new Color(0.5f, 0.28f, 0.2f));  // لحم
        Rect(x - 28 - o, y - 28 - o, 56 + o * 2, 12 + o * 2, pass == 0 ? E : new Color(0.95f, 0.65f, 0.3f)); // الخبزة تحت
        if (pass == 1)
            for (int i = 0; i < 5; i++) Ellipse(x - 16 + i * 8, y + 20 + (i % 2) * 4, 1.6f, 2.4f, new Color(1f, 0.95f, 0.8f)); // سمسم
    }

    static void Tomato(float x, float y, int pass)
    {
        float o = pass == 0 ? 4f : 0f;
        Ellipse(x, y, 24 + o, 21 + o, pass == 0 ? Edge : new Color(0.92f, 0.3f, 0.28f));
        Color g = pass == 0 ? Edge : new Color(0.3f, 0.65f, 0.3f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f;
            Ellipse(x + Mathf.Cos(a) * 7f, y + 20 + Mathf.Sin(a) * 4f, 5 + o * 0.5f, 3 + o * 0.5f, g);
        }
        if (pass == 1) Ellipse(x - 9, y + 6, 5, 7, new Color(1f, 1f, 1f, 0.35f));
    }

    static void Cheese(float x, float y, int pass)
    {
        float o = pass == 0 ? 4f : 0f;
        Tri(new Vector2(x - 30 - o, y - 18 - o), new Vector2(x + 30 + o, y - 18 - o), new Vector2(x + 30 + o, y + 22 + o), pass == 0 ? Edge : new Color(1f, 0.82f, 0.28f));
        if (pass == 1)
        {
            var hole = new Color(0.92f, 0.68f, 0.18f);
            Ellipse(x + 14, y - 4, 5, 5, hole);
            Ellipse(x + 22, y + 10, 3, 3, hole);
            Ellipse(x - 4, y - 10, 4, 4, hole);
        }
    }

    static void Cupcake(float x, float y, int pass)
    {
        float o = pass == 0 ? 4f : 0f;
        Tri(new Vector2(x - 20 - o, y + 2 + o), new Vector2(x + 20 + o, y + 2 + o), new Vector2(x + 14 + o, y - 26 - o), pass == 0 ? Edge : new Color(0.98f, 0.6f, 0.45f));
        Tri(new Vector2(x - 20 - o, y + 2 + o), new Vector2(x - 14 - o, y - 26 - o), new Vector2(x + 14 + o, y - 26 - o), pass == 0 ? Edge : new Color(0.98f, 0.6f, 0.45f));
        Color cream = pass == 0 ? Edge : new Color(1f, 0.88f, 0.92f);
        Ellipse(x, y + 10, 24 + o, 12 + o, cream);
        Ellipse(x, y + 20, 16 + o, 10 + o, cream);
        if (pass == 1) Ellipse(x, y + 31, 5, 5, new Color(0.9f, 0.25f, 0.3f));
        else Ellipse(x, y + 31, 5 + o, 5 + o, Edge);
    }

    // ---------- أدوات رسم (تلف على الأطراف عشان تتكرر بدون خط) ----------
    static void Put(int x, int y, Color c, float cover)
    {
        x = ((x % N) + N) % N; y = ((y % N) + N) % N;
        int i = y * N + x;
        float a = c.a * cover;
        px[i] = Color.Lerp(px[i], new Color(c.r, c.g, c.b, 1f), a);
    }

    static void Ellipse(float cx, float cy, float rx, float ry, Color c)
    {
        for (int y = Mathf.FloorToInt(cy - ry - 1); y <= Mathf.CeilToInt(cy + ry + 1); y++)
            for (int x = Mathf.FloorToInt(cx - rx - 1); x <= Mathf.CeilToInt(cx + rx + 1); x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = (1f - d) * Mathf.Min(rx, ry);       // بالبكسل تقريباً
                float cover = Mathf.Clamp01(edge + 0.5f);
                if (cover > 0f) Put(x, y, c, cover);
            }
    }

    static void Rect(float x0, float y0, float w, float h, Color c)
    {
        for (int y = Mathf.FloorToInt(y0); y < Mathf.CeilToInt(y0 + h); y++)
            for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x0 + w); x++)
                Put(x, y, c, 1f);
    }

    static void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
        int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
        float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
        if (Mathf.Abs(area) < 0.001f) return;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = ((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y)) / area;
                float w1 = ((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y)) / area;
                float w2 = 1f - w0 - w1;
                if (w0 >= 0f && w1 >= 0f && w2 >= 0f) Put(x, y, col, 1f);
            }
    }
}
