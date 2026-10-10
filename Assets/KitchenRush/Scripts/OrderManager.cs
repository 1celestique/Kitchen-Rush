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

    [Header("القصة: كلام تحت الشاشة قبل ما تبدأ الطلبات (Enter يسرّعها)")]
    public string speaker = "Nina";
    public string[] introCaptions =
    {
        "Mom asked me to cook dinner for the whole family tonight!",
        "Okay... deep breath. Let's start with fresh salads.",
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
        timeLeft = levelTime;
        spawnT = firstOrderDelay;
        KitchenHUD.Ensure(this);
        foreach (var line in introCaptions)
        {
            if (line.StartsWith("Tip:")) KitchenHUD.Caption("", line, 3f);
            else KitchenHUD.Caption(speaker, line);
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        if (!ended && k != null && k.hKey.wasPressedThisFrame) KitchenHUD.ToggleBook(this);
        if (KitchenHUD.BookOpen) return;
        if (ended)
        {
            if (k != null && k.rKey.wasPressedThisFrame) Retry();
            if (k != null && (k.enterKey.wasPressedThisFrame || k.nKey.wasPressedThisFrame)) Next();
            return;
        }

        // الوقت والطلبات تبدأ بعد كلام البداية
        if (!introDone)
        {
            if (KitchenHUD.CaptionsBusy) return;
            introDone = true;
        }

        float dt = Time.deltaTime;
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
                spawnT = nextOrderDelay;
            }
        }
    }

    public bool Deliver(Plate p)
    {
        if (ended) return false;
        foreach (var o in orders)
        {
            if (!p.Matches(o.recipe)) continue;
            int bonus = Mathf.CeilToInt(maxSpeedBonus * Mathf.Clamp01(o.timeLeft / o.patience));
            int gain = o.recipe.points + bonus;
            score += gain;
            orders.Remove(o);
            KitchenHUD.Flash(o.recipe.name + "  +" + gain, new Color(0.6f, 1f, 0.5f));
            if (!saidFirst) { saidFirst = true; KitchenHUD.Caption(speaker, firstServeLine, 1.8f); }
            spawnT = Mathf.Min(spawnT, 2f);
            return true;
        }
        return false;
    }

    public int Stars
    {
        get
        {
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
        KitchenHUD.ShowResults(this);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Next()
    {
        Time.timeScale = 1f;
        int i = SceneManager.GetActiveScene().buildIndex + 1;
        if (i > 0 && i < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(i);
        else Retry();
    }
}
