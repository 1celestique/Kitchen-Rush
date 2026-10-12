using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ==========================================================
// OrderManager: الطلبات والنقاط والوقت والنجوم في المرحلة
// حطيه على كائن واحد في المشهد (أداة البناء تحطه لحالها)
// من الـ Inspector تحددين: وقت المرحلة، صبر الزبون، الأطباق، وحدود النجوم
// ==========================================================
[System.Serializable]
public class Recipe
{
    public string name;
    public int points;
    public Ingredient[] parts;

    public Recipe() { }
    public Recipe(string n, int p, params Ingredient[] i) { name = n; points = p; parts = i; }
}

public class Order
{
    public Recipe recipe;
    public float timeLeft;
    public float patience;
}

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("المرحلة")]
    public string levelTitle = "LEVEL 1 · NINA'S KITCHEN";
    public float levelTime = 180f;       // وقت المرحلة بالثواني
    public float patience = 60f;         // صبر الزبون لكل طلب
    public int maxOrders = 1;            // كم طلب يظهر بنفس الوقت
    public float firstOrderDelay = 2f;
    public float nextOrderDelay = 4f;
    public int[] starScores = { 60, 100, 140 };

    [Header("بطاقة القصة أول المرحلة (Enter يبدأ)")]
    public bool showChapter = true;
    public string chapterLabel = "CHAPTER 1";
    public string chapterTitle = "Dinner for Mom";
    [TextArea(3, 6)]
    public string chapterText = "Nina has always dreamed of opening her own restaurant one day.\nTonight, Mom asked her to cook dinner for the family.\nIt's her first real test in the kitchen!";

    [Header("القصة: كلام تحت الشاشة قبل ما تبدأ الطلبات (Enter يسرّعها)")]
    public string speaker = "Nina";
    public string[] introCaptions =
    {
        "Mom asked me to make her something tonight!",
        "Okay... deep breath. One dish at a time.",
        "Mom is waiting at the dining table.",
        "Tip: Each order shows its steps. Press [H] any time to open the recipe book.",
    };
    public string firstServeLine = "Mom loves it!";
    public string missedLine = "Oops... I was too slow.";
    public string hurryLine = "Hurry, dinner time is almost over!";
    public bool introDone;
    bool saidFirst, saidHurry;

    [Header("الأطباق في هالمرحلة")]
    public bool salad = true;
    public bool cheeseSalad = false;
    public bool sandwich = false;
    public bool burger = false;
    public bool cheeseburger = false;
    public bool fries = false;

    [Header("وجبة الأم (المرحلة ١): طلب ورا طلب، وتخلص المرحلة لما تخلص الوجبة")]
    public bool mealMode = false;
    public float[] starTimes = { 100f, 160f };   // تحت ١٠٠ ثانية ٣ نجوم، تحت ١٦٠ نجمتين، غير كذا نجمة
    public string mealName;
    readonly List<Recipe> mealQueue = new List<Recipe>();
    public float elapsed;
    float endT;

    [Header("مجسمات الأطباق الجاهزة (Food Kit)")]
    public string[] dishNames;
    public GameObject[] dishModels;

    [Header("النقاط")]
    public int missedPenalty = 10;
    public int maxSpeedBonus = 10;

    [Header("للتجربة")]
    public int score;
    public float timeLeft;
    public bool ended;

    public readonly List<Order> orders = new List<Order>();
    readonly List<Recipe> menu = new List<Recipe>();
    float spawnT;

    public static readonly Recipe Salad = new Recipe("SALAD", 20, Ingredient.Lettuce, Ingredient.Tomato);
    public static readonly Recipe Burger = new Recipe("BURGER", 35, Ingredient.Bun, Ingredient.Meat, Ingredient.Lettuce);
    public static readonly Recipe Cheeseburger = new Recipe("CHEESEBURGER", 50, Ingredient.Bun, Ingredient.Meat, Ingredient.Cheese, Ingredient.Lettuce);
    public static readonly Recipe Fries = new Recipe("FRIES", 25, Ingredient.Potato);
    public static readonly Recipe CheeseSalad = new Recipe("CHEESE SALAD", 30, Ingredient.Lettuce, Ingredient.Tomato, Ingredient.Cheese);
    public static readonly Recipe Sandwich = new Recipe("SANDWICH", 30, Ingredient.Bun, Ingredient.Cheese, Ingredient.Tomato);
    public static readonly Recipe Coffee = new Recipe("COFFEE", 20, Ingredient.Coffee);
    public static readonly Recipe MatchaLatte = new Recipe("MATCHA LATTE", 30, Ingredient.Matcha, Ingredient.Milk);
    public static readonly Recipe Cornflakes = new Recipe("CORNFLAKES", 20, Ingredient.Cereal, Ingredient.Milk);

    public static readonly Recipe[] All = { Salad, CheeseSalad, Sandwich, Burger, Cheeseburger, Fries, Coffee, MatchaLatte, Cornflakes };
    public Recipe[] AllRecipes { get { return All; } }

    // وجبات الأم: كل مرة تدخلين المرحلة تطلب وحدة غير
    static readonly string[] MealNames = { "Coffee & Cereal", "Matcha Night", "Fresh Greens", "Cozy Dinner" };
    static readonly Recipe[][] Meals =
    {
        new[] { Coffee, Cornflakes },
        new[] { MatchaLatte, Sandwich },
        new[] { Salad, CheeseSalad },
        new[] { Sandwich, Salad, Coffee },
    };

    public GameObject DishModel(string recipeName)
    {
        if (dishNames == null || dishModels == null) return null;
        for (int i = 0; i < dishNames.Length && i < dishModels.Length; i++)
            if (dishNames[i] == recipeName) return dishModels[i];
        return null;
    }

    public List<Recipe> Menu { get { return menu; } }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (salad) menu.Add(Salad);
        if (cheeseSalad) menu.Add(CheeseSalad);
        if (sandwich) menu.Add(Sandwich);
        if (burger) menu.Add(Burger);
        if (cheeseburger) menu.Add(Cheeseburger);
        if (fries) menu.Add(Fries);
        if (menu.Count == 0) menu.Add(Salad);
        if (mealMode)
        {
            // نختار وجبة غير الأخيرة
            int last = PlayerPrefs.GetInt("LastMeal", -1);
            int pick = Random.Range(0, Meals.Length);
            if (Meals.Length > 1 && pick == last) pick = (pick + 1) % Meals.Length;
            PlayerPrefs.SetInt("LastMeal", pick);
            mealName = MealNames[pick];
            mealQueue.AddRange(Meals[pick]);
            menu.Clear();
            foreach (var r in mealQueue) if (!menu.Contains(r)) menu.Add(r);
            var names = new List<string>();
            foreach (var r in mealQueue) names.Add(Nice(r.name));
            string list = names.Count > 1 ? string.Join(", ", names.GetRange(0, names.Count - 1).ToArray()) + " and " + names[names.Count - 1] : names[0];
            var lines = new List<string>(introCaptions);
            lines.Insert(Mathf.Min(2, lines.Count), "Tonight Mom wants: " + list + ".");
            introCaptions = lines.ToArray();
            levelTitle = levelTitle + "  ·  " + mealName.ToUpper();
        }
        timeLeft = levelTime;
        spawnT = firstOrderDelay;
        KitchenHUD.Ensure(this);
        KitchenAudio.Music("music");
        if (showChapter) KitchenHUD.ShowCard(chapterLabel, chapterTitle, chapterText);
        foreach (var line in introCaptions)
        {
            if (line.StartsWith("Tip:")) KitchenHUD.Caption("", line, 3f);
            else KitchenHUD.Caption(speaker, line);
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        // Esc: قائمة الإيقاف (أو يسكر كتاب الوصفات لو مفتوح)
        if (!ended && !KitchenHUD.CardOpen && k != null && k.escapeKey.wasPressedThisFrame)
        {
            if (KitchenHUD.BookOpen) KitchenHUD.ToggleBook(this);
            else KitchenHUD.TogglePause(this);
            return;
        }
        if (KitchenHUD.Paused) return;
        if (!ended && !KitchenHUD.CardOpen && k != null && k.hKey.wasPressedThisFrame) KitchenHUD.ToggleBook(this);
        if (KitchenHUD.BookOpen || KitchenHUD.CardOpen) return;
        if (ended)
        {
            if (k != null && k.rKey.wasPressedThisFrame) Retry();
            if (k != null && (k.enterKey.wasPressedThisFrame || k.nKey.wasPressedThisFrame)) Next();
            if (k != null && k.escapeKey.wasPressedThisFrame) GoToMenu();
            return;
        }

        // الوقت والطلبات تبدأ بعد كلام البداية
        if (!introDone)
        {
            if (KitchenHUD.CaptionsBusy) return;
            introDone = true;
        }

        float dt = Time.deltaTime;
        if (mealMode) { UpdateMeal(dt); return; }
        timeLeft -= dt;
        if (!saidHurry && timeLeft < 30f) { saidHurry = true; KitchenHUD.Caption(speaker, hurryLine, 1.8f); }
        if (timeLeft <= 0f) { timeLeft = 0f; End(); return; }

        for (int i = orders.Count - 1; i >= 0; i--)
        {
            orders[i].timeLeft -= dt;
            if (orders[i].timeLeft <= 0f)
            {
                orders.RemoveAt(i);
                score = Mathf.Max(0, score - missedPenalty);
                KitchenHUD.Flash("Order missed! -" + missedPenalty, new Color(1f, 0.45f, 0.4f));
                KitchenAudio.Play("wrong");
                if (!KitchenHUD.CaptionsBusy) KitchenHUD.Caption(speaker, missedLine, 1.6f);
                spawnT = Mathf.Max(spawnT, 1.5f);
            }
        }

        if (orders.Count < maxOrders)
        {
            spawnT -= dt;
            if (spawnT <= 0f)
            {
                var r = menu[Random.Range(0, menu.Count)];
                orders.Add(new Order { recipe = r, timeLeft = patience, patience = patience });
                KitchenAudio.Play("ding", 0.5f);
                spawnT = nextOrderDelay;
            }
        }
    }

    // وجبة الأم: طلب واحد بالمرة، بدون وقت ينتهي. تخلص المرحلة لما تخلص الوجبة
    void UpdateMeal(float dt)
    {
        elapsed += dt;
        timeLeft = elapsed;   // الساعة فوق تعد لقدام
        if (orders.Count == 0)
        {
            if (mealQueue.Count == 0) { endT += dt; if (endT > 2.2f) End(); return; }
            spawnT -= dt;
            if (spawnT <= 0f)
            {
                var r = mealQueue[0];
                mealQueue.RemoveAt(0);
                orders.Add(new Order { recipe = r, timeLeft = 1f, patience = 1f });
                KitchenAudio.Play("ding", 0.5f);
                spawnT = nextOrderDelay;
            }
        }
    }

    static string Nice(string n)
    {
        n = n.ToLower();
        return char.ToUpper(n[0]) + n.Substring(1);
    }

    public int MealLeft { get { return mealQueue.Count + orders.Count; } }

    public bool Deliver(Plate p) { return Deliver(r => p.Matches(r)); }
    public bool DeliverDrink(KitchenItem d) { return Deliver(r => d.MatchesDrink(r)); }

    bool Deliver(System.Func<Recipe, bool> match)
    {
        if (ended) return false;
        foreach (var o in orders)
        {
            if (!match(o.recipe)) continue;
            int bonus = Mathf.CeilToInt(maxSpeedBonus * Mathf.Clamp01(o.timeLeft / o.patience));
            int gain = o.recipe.points + bonus;
            score += gain;
            orders.Remove(o);
            KitchenHUD.Flash(o.recipe.name + "  +" + gain, new Color(0.6f, 1f, 0.5f));
            KitchenAudio.Play("serve");
            if (!saidFirst) { saidFirst = true; KitchenHUD.Caption(speaker, firstServeLine, 1.8f); }
            spawnT = Mathf.Min(spawnT, 2f);
            if (mealMode && MealLeft > 0) KitchenHUD.Caption("Mom", Thanks[Random.Range(0, Thanks.Length)], 1.6f);
            if (mealMode && MealLeft == 0) KitchenHUD.Caption("Mom", "Everything was perfect, Nina! I'm so proud of you!", 2.2f);
            return true;
        }
        return false;
    }

    static readonly string[] Thanks = { "Mmm, that's delicious!", "Just how I like it!", "Thank you, sweetie!" };

    public int Stars
    {
        get
        {
            if (mealMode)
            {
                if (!ended && MealLeft > 0) return 0;
                int st = 1;
                foreach (var t in starTimes) if (elapsed <= t) st++;
                return Mathf.Min(3, st);
            }
            int s = 0;
            foreach (var t in starScores) if (score >= t) s++;
            return s;
        }
    }

    void End()
    {
        ended = true;
        orders.Clear();
        string key = "Stars_" + SceneManager.GetActiveScene().name;
        if (Stars > PlayerPrefs.GetInt(key, 0)) { PlayerPrefs.SetInt(key, Stars); PlayerPrefs.Save(); }
        Time.timeScale = 0f;
        KitchenAudio.Play(Stars > 0 ? "serve" : "wrong");
        KitchenHUD.ShowResults(this);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Next()
    {
        // المرحلة الجاية تنفتح بس لو جبتي نجمة وحدة على الأقل
        if (Stars == 0) { KitchenHUD.Flash("Get at least 1 star to unlock the next level", new Color(1f, 0.6f, 0.5f)); KitchenAudio.Play("wrong"); return; }
        Time.timeScale = 1f;
        int i = SceneManager.GetActiveScene().buildIndex + 1;
        if (i > 0 && i < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(i);
        else GoToMenu();
    }

    // يرجع للقائمة الرئيسية (أول مشهد في Build Settings)
    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (SceneManager.sceneCountInBuildSettings > 0 && SceneManager.GetActiveScene().buildIndex != 0) SceneManager.LoadScene(0);
        else Retry();
    }
}
