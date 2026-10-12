using UnityEngine;

// ==========================================================
// Crate: كبت المكونات (أو الثلاجة للحليب)
// يعطيك مكوّن كل ما ضغطتي E وما يخلص، وبابه ينفتح شوي
// ==========================================================
public class Crate : Station
{
    public Ingredient ingredient;
    public Material itemMaterial;
    public GameObject itemPrefab;       // مجسم الأكل (من Food Kit)
    public GameObject processedPrefab;  // شكله بعد التقطيع/التحضير
    public Transform door;              // الباب (ينفتح لما تاخذين)
    public float doorAngle = -100f;
    public bool isFridge;               // الثلاجة: ما ينحط فوقها شي

    float openUntil = -1f;
    float doorT;

    public override bool CanTake(ChefController chef) { return true; }

    // ما ينحط شي فوق الكبت/الصندوق/الثلاجة
    public override bool CanPut(KitchenItem i) { return false; }

    public override string RejectReason(KitchenItem i)
    {
        if (isFridge) return i != null && i.ingredient == Ingredient.Matcha && !i.chopped ? "Whisk the matcha first!" : "That doesn't go in the fridge";
        return "Put it on an empty counter";
    }

    public override KitchenItem Take(ChefController chef)
    {
        if (item != null) return base.Take(chef);
        Open();
        return ItemFactory.Create(ingredient, itemMaterial, itemPrefab, processedPrefab);
    }

    void Open() { openUntil = Time.time + 0.6f; }

    public override bool TryCombine(ChefController chef)
    {
        if (item != null) return base.TryCombine(chef);
        var h = chef.held;
        if (h == null) return false;

        // الثلاجة: الماتشا أو الكورن فليكس ياخذ حليب على طول
        if (ingredient == Ingredient.Milk && !(h is Plate))
        {
            if (h.CanTakeMilk) { Open(); h.AddMilk(); return true; }
            if (h.ingredient == Ingredient.Matcha && !h.chopped) { KitchenHUD.Flash("Whisk the matcha first!"); return true; }
            return false;
        }

        // لو شايلة صحن: المكوّن يروح في الصحن على طول (مثل الخبز)
        var plate = h as Plate;
        if (plate == null) return false;
        var it = ItemFactory.Create(ingredient, itemMaterial, itemPrefab, processedPrefab);
        string why;
        if (plate.TryAdd(it, out why)) { Open(); return true; }
        Destroy(it.gameObject);
        if (why != null) KitchenHUD.Flash(why);
        return true;
    }

    public override string Hint(ChefController chef)
    {
        var h = chef.held;
        if (item != null) return base.Hint(chef);
        if (h == null) return "[E] Take " + Name(ingredient);
        if (ingredient == Ingredient.Milk && !(h is Plate) && h.CanTakeMilk) return "[E] Add milk";
        return null;
    }

    static string Name(Ingredient i)
    {
        switch (i)
        {
            case Ingredient.Coffee: return "a coffee cup";
            case Ingredient.Matcha: return "matcha";
            case Ingredient.Milk: return "milk";
            case Ingredient.Cereal: return "cereal";
            case Ingredient.Bun: return "bread";
            default: return i.ToString().ToLower();
        }
    }

    void Update()
    {
        if (door == null) return;
        float target = Time.time < openUntil ? 1f : 0f;
        doorT = Mathf.MoveTowards(doorT, target, Time.deltaTime * 5f);
        float e = doorT * doorT * (3f - 2f * doorT);
        door.localRotation = Quaternion.Euler(0f, doorAngle * e, 0f);
    }
}
