using System.Collections.Generic;
using UnityEngine;

// ==========================================================
// KitchenAudio: كل أصوات اللعبة والموسيقى
// الأصوات مصنوعة بالكود (ما تحتاج ملفات)، ولو حطيتي ملفات صوت
// في Assets/KitchenRush/Resources/Sounds بنفس الأسماء تنستخدم بدالها:
// chop, pickup, drop, serve, wrong, ding, burnt, sizzle, music, menu
// ==========================================================
public class KitchenAudio : MonoBehaviour
{
    static KitchenAudio I;
    const int Rate = 44100;

    AudioSource sfx, music;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<Object, AudioSource> loops = new Dictionary<Object, AudioSource>();

    public static float SfxVolume = 0.8f;
    public static float MusicVolume = 0.32f;

    static KitchenAudio Get()
    {
        if (I == null)
        {
            var g = new GameObject("Kitchen Audio");
            I = g.AddComponent<KitchenAudio>();
            I.sfx = g.AddComponent<AudioSource>();
            I.sfx.playOnAwake = false;
            I.music = g.AddComponent<AudioSource>();
            I.music.playOnAwake = false;
            I.music.loop = true;
        }
        return I;
    }

    // ---------- اللي تناديه السكربتات ----------

    public static void Play(string name, float volume = 1f)
    {
        var a = Get();
        var c = a.Clip(name);
        if (c != null) a.sfx.PlayOneShot(c, volume * SfxVolume);
    }

    public static void Music(string name)
    {
        var a = Get();
        var c = a.Clip(name);
        if (c == null || a.music.clip == c) return;
        a.music.clip = c;
        a.music.volume = MusicVolume;
        a.music.Play();
    }

    // صوت يتكرر وهو شغال (مثل أزيز الشواية)
    public static void Loop(Object key, string name, bool on, float volume = 0.5f)
    {
        if (!on && I == null) return;
        var a = Get();
        AudioSource s;
        a.loops.TryGetValue(key, out s);
        if (!on) { if (s != null && s.isPlaying) s.Stop(); return; }
        if (s == null)
        {
            s = a.gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.clip = a.Clip(name);
            a.loops[key] = s;
        }
        s.volume = volume * SfxVolume;
        if (!s.isPlaying) s.Play();
    }

    void Update()
    {
        // الموسيقى تهدى لما اللعبة واقفة (كتاب الوصفات)
        if (music != null) music.pitch = Time.timeScale < 0.01f ? 0.85f : 1f;
    }

    // ---------- صناعة الأصوات ----------

    AudioClip Clip(string name)
    {
        AudioClip c;
        if (clips.TryGetValue(name, out c)) return c;
        c = Resources.Load<AudioClip>("Sounds/" + name);
        if (c == null) c = Make(name);
        clips[name] = c;
        return c;
    }

    static AudioClip Make(string name)
    {
        switch (name)
        {
            case "chop": return Synth(name, 0.09f, (t, i) => Noise(i) * Env(t, 0.002f, 0.07f) * 0.7f + Sine(180f, t) * Env(t, 0.001f, 0.05f) * 0.5f);
            case "pickup": return Synth(name, 0.12f, (t, i) => Sine(Mathf.Lerp(500f, 900f, t / 0.12f), t) * Env(t, 0.005f, 0.11f) * 0.45f);
            case "drop": return Synth(name, 0.12f, (t, i) => Sine(Mathf.Lerp(420f, 260f, t / 0.12f), t) * Env(t, 0.003f, 0.11f) * 0.45f);
            case "serve": return Synth(name, 0.7f, (t, i) => (Bell(1046.5f, t, 0f) + Bell(1318.5f, t, 0.12f) + Bell(1568f, t, 0.24f)) * 0.3f);
            case "ding": return Synth(name, 0.5f, (t, i) => Bell(1760f, t, 0f) * 0.35f);
            case "wrong": return Synth(name, 0.35f, (t, i) => Square(Mathf.Lerp(220f, 150f, t / 0.35f), t) * Env(t, 0.01f, 0.33f) * 0.18f);
            case "burnt": return Synth(name, 0.6f, (t, i) => Square(t % 0.2f < 0.1f ? 880f : 660f, t) * Env(t, 0.01f, 0.58f) * 0.12f);
            case "sizzle": return Synth(name, 1.0f, (t, i) => Hiss(i) * (0.55f + 0.45f * Mathf.PerlinNoise(t * 30f, 0.5f)) * 0.35f, true);
            case "click": return Synth(name, 0.05f, (t, i) => Sine(1200f, t) * Env(t, 0.001f, 0.045f) * 0.4f);
            case "music": return Song(name, 112f, new[] { 0, 5, 9, 7 }, false);
            case "menu": return Song(name, 92f, new[] { 0, 9, 5, 7 }, true);
        }
        return null;
    }

    delegate float Wave(float t, int i);

    static AudioClip Synth(string name, float len, Wave w, bool loop = false)
    {
        int n = Mathf.CeilToInt(len * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = w(i / (float)Rate, i);
        if (loop)
        {
            // يتكرر بدون طقة
            int fade = Rate / 50;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[n - 1 - i] *= k;
            }
        }
        var c = AudioClip.Create(name, n, 1, Rate, false);
        c.SetData(data, 0);
        return c;
    }

    static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
    static float Square(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t) > 0f ? 1f : -1f; }
    static float Tri(float f, float t) { float p = (t * f) % 1f; return 4f * Mathf.Abs(p - 0.5f) - 1f; }
    static float Env(float t, float a, float d) { return t < a ? t / a : Mathf.Exp(-(t - a) / (d * 0.35f)); }

    static uint seed = 22222;
    static float Noise(int i)
    {
        seed = seed * 1664525u + 1013904223u;
        return ((seed >> 9) / 8388608f) * 2f - 1f;
    }

    static float lastHiss;
    static float Hiss(int i)
    {
        float n = Noise(i);
        float h = n - lastHiss * 0.6f;
        lastHiss = n;
        return h;
    }

    static float Bell(float f, float t, float start)
    {
        float x = t - start;
        if (x < 0f) return 0f;
        return (Sine(f, x) + 0.4f * Sine(f * 2.01f, x)) * Env(x, 0.003f, 0.4f);
    }

    // موسيقى خفيفة: أكوردات + لحن بسيط، ٨ بارات وتتكرر
    static AudioClip Song(string name, float bpm, int[] prog, bool soft)
    {
        float beat = 60f / bpm;
        int bars = 8;
        float len = bars * 4 * beat;
        int n = Mathf.CeilToInt(len * Rate);
        var data = new float[n];
        float root = 261.63f; // C
        int[] major = { 0, 4, 7 };
        int[] minor = { 0, 3, 7 };
        int[] mel = { 7, 9, 12, 9, 7, 4, 5, 7, 12, 11, 9, 7, 4, 5, 4, 2 };

        for (int bar = 0; bar < bars; bar++)
        {
            int deg = prog[bar % prog.Length];
            int[] tri = deg == 9 ? minor : major;
            float barStart = bar * 4 * beat;
            for (int b8 = 0; b8 < 8; b8++)
            {
                // أربيجيو ناعم
                float st = barStart + b8 * beat / 2f;
                int note = deg + tri[b8 % 3] + (b8 % 4 == 3 ? 12 : 0);
                AddNote(data, st, beat * 0.5f, root * Mathf.Pow(2f, note / 12f), soft ? 0.07f : 0.08f, 1);
            }
            // باص
            AddNote(data, barStart, beat * 2f, root / 2f * Mathf.Pow(2f, deg / 12f), 0.12f, 2);
            AddNote(data, barStart + beat * 2f, beat * 2f, root / 2f * Mathf.Pow(2f, (deg + 7) / 12f), 0.1f, 2);
            // لحن
            if (!soft || bar % 2 == 1)
                for (int q = 0; q < 4; q++)
                {
                    if ((bar * 4 + q) % 3 == 2) continue;
                    int m = mel[(bar * 4 + q) % mel.Length];
                    AddNote(data, barStart + q * beat, beat * 0.9f, root * 2f * Mathf.Pow(2f, (m - 12 + (deg > 6 ? 0 : 0)) / 12f), 0.05f, 0);
                }
            // طقطقة خفيفة على الإيقاع
            if (!soft)
                for (int q = 0; q < 4; q++)
                {
                    int s0 = Mathf.FloorToInt((barStart + q * beat + beat / 2f) * Rate);
                    for (int i = 0; i < Rate / 40 && s0 + i < n; i++) data[s0 + i] += Noise(i) * Env(i / (float)Rate, 0.001f, 0.02f) * 0.04f;
                }
        }
        float peak = 0.001f;
        foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
        for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.8f;
        var c = AudioClip.Create(name, n, 1, Rate, false);
        c.SetData(data, 0);
        return c;
    }

    static void AddNote(float[] data, float start, float len, float f, float vol, int kind)
    {
        int s0 = Mathf.FloorToInt(start * Rate);
        int count = Mathf.FloorToInt((len + 0.15f) * Rate);
        for (int i = 0; i < count; i++)
        {
            int k = s0 + i;
            if (k >= data.Length) break;
            float t = i / (float)Rate;
            float env = t < 0.01f ? t / 0.01f : Mathf.Exp(-(t - 0.01f) / (len * 0.6f));
            float w;
            if (kind == 0) w = Sine(f, t) * 0.7f + Tri(f * 2f, t) * 0.15f;       // لحن
            else if (kind == 1) w = Tri(f, t) * 0.6f + Sine(f * 2f, t) * 0.2f;   // أربيجيو
            else w = Sine(f, t) + Sine(f * 2f, t) * 0.25f;                       // باص
            data[k] += w * env * vol;
        }
    }
}
