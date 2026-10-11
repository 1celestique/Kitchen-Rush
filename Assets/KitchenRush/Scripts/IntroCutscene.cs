using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ==========================================================
// IntroCutscene: بداية القصة
// نينا جالسة في الصالة تشرب قهوة وترسم المطعم اللي تحلم فيه،
// الجدة تمدح رسمتها، وبعدين أمها تناديها من المطبخ ← تبدأ المرحلة ١
// Enter: الجملة الجاية   ·   Esc: تخطي
// الكلام تقدرين تعدلينه من الـ Inspector (Lines)
// ==========================================================
public class IntroCutscene : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        public int shot;          // 0 الغرفة كاملة، 1 الرسمة، 2 نينا قريب، 3 الجدة، 4 باب المطبخ
        public string speaker;
        [TextArea] public string text;
        public Line() { }
        public Line(int s, string sp, string t) { shot = s; speaker = sp; text = t; }
    }

    public string nextScene = "level1";
    public Transform nina, granny, sketch;
    public Vector3 door;

    public Line[] lines =
    {
        new Line(0, "", "Saturday evening. Nina's living room."),
        new Line(2, "Nina", "Mmm... coffee and drawing. My favorite part of the day."),
        new Line(1, "Nina", "Someday I'll open my own restaurant. Warm lights, cozy tables..."),
        new Line(1, "Nina", "...and the best food in town. I'll call it \"Nina's Kitchen\"."),
        new Line(3, "Grandma", "That's a lovely drawing, sweetheart. You'll get there one day."),
        new Line(4, "Mom", "Ninaaa! Can you come help me cook dinner tonight?"),
        new Line(2, "Nina", "Coming, Mom! ...Okay. Maybe today is where it all starts."),
    };

    Camera cam;
    Vector3 camPos, camLook;
    Vector3 wantPos, wantLook;
    Font font;
    Text capText, hintText;
    Image fade;
    RectTransform capBox;
    bool skip, leaving;

    void Start()
    {
        Time.timeScale = 1f;
        cam = Camera.main;
        KitchenAudio.Music("menu");
        BuildUI();
        Shot(0, true);
        StartCoroutine(Run());
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame && !leaving) StartCoroutine(Leave());

        if (cam == null) return;
        // الكاميرا تتحرك بنعومة بين اللقطات، وتقرب شوي شوي
        float a = 1f - Mathf.Exp(-1.6f * Time.deltaTime);
        camPos = Vector3.Lerp(camPos, wantPos, a);
        camLook = Vector3.Lerp(camLook, wantLook, a);
        wantPos += (wantLook - wantPos).normalized * Time.deltaTime * 0.04f;
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation(camLook - camPos);
    }

    void Shot(int s, bool snap)
    {
        Vector3 n = nina != null ? nina.position : Vector3.zero;
        switch (s)
        {
            case 1:
                if (sketch != null)
                {
                    wantLook = sketch.position;
                    wantPos = sketch.position + new Vector3(0.05f, 0.75f, -0.45f);
                }
                break;
            case 2:
                wantLook = n + Vector3.up * 0.95f;
                wantPos = n + new Vector3(0.7f, 1.15f, -1.6f);
                break;
            case 3:
                if (granny != null)
                {
                    wantLook = granny.position + Vector3.up * 0.95f;
                    wantPos = granny.position + granny.forward * 1.6f + Vector3.up * 1.1f + Vector3.Cross(Vector3.up, granny.forward) * 0.4f;
                }
                break;
            case 4:
                wantLook = door;
                wantPos = n + new Vector3(0.9f, 1.45f, -1.9f);
                break;
            default:
                wantLook = n + new Vector3(0f, 0.6f, -0.3f);
                wantPos = n + new Vector3(0.1f, 1.75f, -4.4f);
                break;
        }
        if (snap) { camPos = wantPos; camLook = wantLook; }
    }

    IEnumerator Run()
    {
        // تفتح من السواد
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { fade.color = new Color(0, 0, 0, 1f - t); yield return null; }
        fade.color = new Color(0, 0, 0, 0);

        foreach (var l in lines)
        {
            if (leaving) yield break;
            Shot(l.shot, false);
            if (l.speaker == "Mom") KitchenAudio.Play("ding", 0.6f);
            yield return Say(l);
        }
        if (!leaving) yield return Leave();
    }

    IEnumerator Say(Line l)
    {
        capBox.gameObject.SetActive(true);
        float chars = 0f;
        int total = l.text.Length;
        string head = string.IsNullOrEmpty(l.speaker) ? "" : "<b><color=" + SpeakerColor(l.speaker) + ">" + l.speaker + ":</color></b>  ";
        while (chars < total)
        {
            if (leaving) yield break;
            chars += Time.deltaTime * 40f;
            if (Pressed()) chars = total;
            capText.text = string.IsNullOrEmpty(l.speaker)
                ? "<i><color=#FFD98A>" + l.text.Substring(0, Mathf.Min(total, (int)chars)) + "</color></i>"
                : head + l.text.Substring(0, Mathf.Min(total, (int)chars));
            yield return null;
        }
        yield return null;
        float wait = 2.6f + total * 0.025f;
        while (wait > 0f && !leaving)
        {
            if (Pressed()) break;
            wait -= Time.deltaTime;
            yield return null;
        }
    }

    static string SpeakerColor(string speaker)
    {
        switch (speaker)
        {
            case "Nina": return "#FF9EC4";
            case "Mom": return "#8EC5FF";
            case "Grandma": return "#B9E58E";
            default: return "#FFFFFF";
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
        fade.color = UnityEngine.Color.black;
        SceneManager.LoadScene(nextScene);
    }

    // ---------- الواجهة: أشرطة سينما، صندوق الكلام، تلميح، وسواد للانتقال ----------
    void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var g = new GameObject("Intro UI");
        var canvas = g.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = g.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        var root = g.GetComponent<RectTransform>();

        Bar(root, new Vector2(0, 1), 110);
        Bar(root, new Vector2(0, 0), 110);

        capBox = new GameObject("Caption", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        capBox.SetParent(root, false);
        capBox.anchorMin = capBox.anchorMax = new Vector2(0.5f, 0f);
        capBox.pivot = new Vector2(0.5f, 0f);
        capBox.sizeDelta = new Vector2(1300, 100);
        capBox.anchoredPosition = new Vector2(0, 125);
        capBox.GetComponent<Image>().sprite = KitchenHUD.MakeRounded(64, 20);
        capBox.GetComponent<Image>().type = Image.Type.Sliced;
        capBox.GetComponent<Image>().color = new UnityEngine.Color(0.08f, 0.06f, 0.06f, 0.82f);
        capText = Label(capBox, "", 32, TextAnchor.MiddleCenter);
        capText.rectTransform.offsetMin = new Vector2(30, 6);
        capText.rectTransform.offsetMax = new Vector2(-30, -6);
        capText.horizontalOverflow = HorizontalWrapMode.Wrap;
        capBox.gameObject.SetActive(false);

        hintText = Label(root, "Enter  Next      ·      Esc  Skip", 20, TextAnchor.LowerRight);
        hintText.color = new UnityEngine.Color(1f, 1f, 1f, 0.55f);
        hintText.rectTransform.offsetMin = new Vector2(0, 40);
        hintText.rectTransform.offsetMax = new Vector2(-40, 0);

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.rectTransform.SetParent(root, false);
        fade.rectTransform.anchorMin = Vector2.zero; fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.sizeDelta = Vector2.zero;
        fade.color = UnityEngine.Color.black;
        fade.raycastTarget = false;
    }

    void Bar(RectTransform root, Vector2 anchor, float h)
    {
        var b = new GameObject("Letterbox", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        b.SetParent(root, false);
        b.anchorMin = new Vector2(0f, anchor.y); b.anchorMax = new Vector2(1f, anchor.y);
        b.pivot = new Vector2(0.5f, anchor.y);
        b.sizeDelta = new Vector2(0f, h);
        b.anchoredPosition = Vector2.zero;
        b.GetComponent<Image>().color = UnityEngine.Color.black;
    }

    Text Label(RectTransform parent, string text, int size, TextAnchor align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = UnityEngine.Color.white;
        t.alignment = align;
        t.supportRichText = true;
        t.raycastTarget = false;
        return t;
    }
}
