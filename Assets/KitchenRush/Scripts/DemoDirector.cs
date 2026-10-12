using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ==========================================================
// DemoDirector: عرض كامل للعبة (للفيديو)
// من القائمة الرئيسية اضغطي F9: يفتح طريقة اللعب، الإعدادات، الكريدت،
// بعدين القصة، وبعدين نينا تطبخ لحالها في المرحلة ١ و ٢ (لاعب آلي)
// F10 يوقف العرض
// ==========================================================
public class DemoDirector : MonoBehaviour
{
    public static DemoDirector I;
    public float level2Seconds = 70f;

    ChefController chef;
    bool shownWrong;

    public static void Begin()
    {
        if (I != null) return;
        var g = new GameObject("DemoDirector");
        DontDestroyOnLoad(g);
        I = g.AddComponent<DemoDirector>();
    }

    void Start() { StartCoroutine(Run()); }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.f10Key.wasPressedThisFrame) Stop();
    }

    void Stop()
    {
        ChefController.BotMode = false;
        I = null;
        Destroy(gameObject);
    }

    // ---------- أدوات ----------

    // يضغط زر كيبورد (كأن اللاعب ضغطه)
    IEnumerator Press(Key key)
    {
        var kb = Keyboard.current;
        if (kb == null) yield break;
        InputSystem.QueueStateEvent(kb, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(kb, new KeyboardState());
        yield return null;
    }

    static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

    // يضغط زر واجهة بالنص اللي عليه
    static bool ClickUI(string label)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null && t.text == label && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return true; }
        }
        return false;
    }

    // ---------- العرض ----------

    IEnumerator Run()
    {
        // ===== القائمة الرئيسية =====
        yield return Wait(1.5f);
        yield return Press(Key.Space);          // شاشة البداية
        yield return Wait(2.5f);
        var mm = FindAnyObjectByType<MainMenu>();
        if (mm != null)
        {
            // الملابس
            for (int i = 0; i < 3; i++) { yield return Press(Key.RightArrow); yield return Wait(0.9f); }
            yield return Press(Key.LeftArrow); yield return Wait(0.9f);
            yield return Press(Key.LeftArrow); yield return Wait(0.9f);
            yield return Press(Key.LeftArrow); yield return Wait(1.2f);

            mm.SendMessage("ShowHowTo"); yield return Wait(5f);
            mm.SendMessage("CloseModal"); yield return Wait(1f);

            mm.SendMessage("ShowSettings"); yield return Wait(1.5f);
            for (int i = 0; i < 3; i++) { ClickUI("-"); yield return Wait(0.4f); }
            for (int i = 0; i < 3; i++) { ClickUI("+"); yield return Wait(0.4f); }
            yield return Wait(1.5f);
            mm.SendMessage("CloseModal"); yield return Wait(1f);

            mm.SendMessage("ShowCredits"); yield return Wait(5f);
            mm.SendMessage("CloseModal"); yield return Wait(1.2f);
            mm.SendMessage("PlayStory");
        }

        // ===== القصة =====
        yield return Wait(1.5f);
        float storyT = 0f;
        while (FindAnyObjectByType<IntroCutscene>() != null && storyT < 90f)
        {
            yield return Wait(2.6f);
            storyT += 2.6f;
            yield return Press(Key.Enter);
        }

        // ===== المرحلة ١ =====
        yield return PlayLevel(false);

        // ===== المرحلة ٢ =====
        yield return PlayLevel(true);

        yield return Wait(4f);
        Stop();
    }

    IEnumerator PlayLevel(bool timed)
    {
        float t = 0f;
        while (OrderManager.Instance == null && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
        var om = OrderManager.Instance;
        if (om == null) yield break;
        chef = FindAnyObjectByType<ChefController>();

        // بطاقة الفصل
        yield return Wait(3f);
        yield return Press(Key.Enter);
        yield return Wait(2f);

        if (!timed)
        {
            // كتاب الوصفات وقائمة الإيقاف
            KitchenHUD.ToggleBook(om); yield return Wait(3.5f);
            KitchenHUD.ToggleBook(om); yield return Wait(1f);
            KitchenHUD.TogglePause(om); yield return Wait(3.5f);
            KitchenHUD.TogglePause(om); yield return Wait(1f);
        }

        ChefController.BotMode = true;
        float start = Time.time;
        while (!om.ended && !(timed && Time.time - start > level2Seconds))
        {
            if (om.orders.Count > 0) yield return Cook(om.orders[0].recipe);
            else yield return null;
        }
        chef.botInput = Vector3.zero;
        chef.botWork = false;

        if (timed)
        {
            // إيقاف وبعدين نرجع للقائمة
            KitchenHUD.TogglePause(om); yield return Wait(3f);
            KitchenHUD.TogglePause(om);
            ChefController.BotMode = false;
            om.GoToMenu();
            yield return Wait(3f);
        }
        else
        {
            yield return Wait(6f);            // شاشة النتيجة
            ChefController.BotMode = false;
            om.Next();
            yield return Wait(1f);
        }
    }

    // ---------- نينا تطبخ لحالها ----------

    T Find<T>() where T : Object { return FindAnyObjectByType<T>(); }

    Crate CrateFor(Ingredient ing)
    {
        foreach (var c in FindObjectsByType<Crate>(FindObjectsSortMode.None)) if (c.ingredient == ing) return c;
        return null;
    }

    T FreeOf<T>() where T : Station
    {
        T best = null;
        foreach (var s in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (s.GetType() != typeof(T)) continue;
            if (s.item == null) return s;
            if (best == null) best = s;
        }
        return best;
    }

    ServeWindow ServeSpot()
    {
        return FindAnyObjectByType<ServeWindow>();
    }

    IEnumerator Cook(Recipe r)
    {
        // لو بيدها شي قديم: للزبالة
        if (chef.held != null) yield return Use(Find<Trash>());
        string n = r.name;
        if (n == "COFFEE" || n == "MATCHA LATTE")
        {
            var ing = n == "COFFEE" ? Ingredient.Coffee : Ingredient.Matcha;
            yield return Use(CrateFor(ing));
            if (!shownWrong)
            {
                // نعرض إن المكان الغلط ما يقبل (لوح التقطيع ما ياخذ كوب)
                shownWrong = true;
                yield return Use(FreeOf<ChopStation>());
                yield return Wait(1.2f);
            }
            var brew = Find<BrewStation>();
            yield return Use(brew);
            yield return WorkUntil(brew, () => brew.item != null && brew.item.chopped);
            yield return Use(brew);
            if (n == "MATCHA LATTE") yield return Use(CrateFor(Ingredient.Milk));
            yield return Use(ServeSpot());
        }
        else if (n == "CORNFLAKES")
        {
            yield return Use(CrateFor(Ingredient.Cereal));
            yield return Use(CrateFor(Ingredient.Milk));
            yield return Use(ServeSpot());
        }
        else
        {
            var stack = Find<PlateStack>();
            foreach (var part in r.parts)
            {
                var crate = CrateFor(part);
                if (crate == null) continue;
                yield return Use(crate);
                var it = chef.held;
                if (it == null) continue;
                if (it.Choppable)
                {
                    var board = FreeOf<ChopStation>();
                    yield return Use(board);
                    yield return WorkUntil(board, () => board.item != null && board.item.chopped);
                    yield return Use(board);
                }
                else if (part == Ingredient.Meat)
                {
                    var g = FreeOf<Grill>();
                    yield return Use(g);
                    float w = 0f;
                    while (g.item != null && !g.item.cooked && w < 12f) { w += Time.deltaTime; yield return null; }
                    yield return Use(g);
                }
                yield return Use(stack);
            }
            yield return Use(stack);       // ناخذ الصحن
            yield return Use(ServeSpot());
        }
        yield return Wait(0.3f);
    }

    IEnumerator WorkUntil(Station s, System.Func<bool> done)
    {
        float t = 0f;
        chef.botWork = true;
        while (!done() && t < 8f) { t += Time.deltaTime; yield return null; }
        chef.botWork = false;
        yield return Wait(0.2f);
    }

    // تمشي للمحطة، تلف عليها، وتضغط E
    IEnumerator Use(Station s)
    {
        if (s == null || chef == null) yield break;
        Vector3 dir;
        Vector3 target = Approach(s, out dir);
        yield return MoveTo(target);
        chef.transform.rotation = Quaternion.LookRotation(-dir);
        yield return Wait(0.15f);
        chef.botInteract = true;
        yield return null;
        yield return null;
        yield return Wait(0.35f);
    }

    Vector3 Approach(Station s, out Vector3 dir)
    {
        var col = s.GetComponent<Collider>();
        Bounds b = col != null ? col.bounds : new Bounds(s.transform.position, Vector3.one);
        Vector3 c = b.center; c.y = 0f;
        if (Mathf.Abs(c.z) >= 1.5f && Mathf.Abs(c.x) <= 4.6f) dir = new Vector3(0f, 0f, -Mathf.Sign(c.z));
        else dir = new Vector3(-c.x, 0f, -c.z).normalized;
        float ext = Mathf.Abs(dir.x) * b.extents.x + Mathf.Abs(dir.z) * b.extents.z;
        Vector3 p = c + dir * (ext + 0.5f);
        p.y = chef.transform.position.y;
        return p;
    }

    IEnumerator MoveTo(Vector3 target)
    {
        float t = 0f, stuck = 0f;
        Vector3 last = chef.transform.position;
        while (true)
        {
            Vector3 d = target - chef.transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.07f) break;
            chef.botInput = d.normalized * Mathf.Clamp(dist * 2.2f, 0.3f, 1f);
            yield return null;
            t += Time.deltaTime;
            if ((chef.transform.position - last).sqrMagnitude < 0.0001f) stuck += Time.deltaTime; else stuck = 0f;
            last = chef.transform.position;
            if (stuck > 0.8f || t > 7f)
            {
                // علقت: ننقلها على طول
                var cc = chef.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                chef.transform.position = new Vector3(target.x, chef.transform.position.y, target.z);
                if (cc != null) cc.enabled = true;
                break;
            }
        }
        chef.botInput = Vector3.zero;
        yield return Wait(0.12f);
    }
}
