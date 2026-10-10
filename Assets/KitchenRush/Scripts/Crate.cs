using UnityEngine;

// ==========================================================
// Crate: صندوق المكونات، يعطيك مكوّن كل ما ضغطتي E وما يخلص
// ==========================================================
public class Crate : Station
{
    public Ingredient ingredient;
    public Material itemMaterial;
    public GameObject itemPrefab;   // مجسم الأكل (من Food Kit)

    public override bool CanTake(ChefController chef) { return true; }

    public override KitchenItem Take(ChefController chef)
    {
        if (item != null) return base.Take(chef);
        return ItemFactory.Create(ingredient, itemMaterial, itemPrefab);
    }

    // لو شايلة صحن: المكوّن يروح في الصحن على طول (مثل الخبز)
    public override bool TryCombine(ChefController chef)
    {
        if (item != null) return base.TryCombine(chef);
        var plate = chef.held as Plate;
        if (plate == null) return false;
        var it = ItemFactory.Create(ingredient, itemMaterial, itemPrefab);
        string why;
        if (plate.TryAdd(it, out why)) return true;
        Destroy(it.gameObject);
        if (why != null) KitchenHUD.Flash(why);
        return true;
    }
}
