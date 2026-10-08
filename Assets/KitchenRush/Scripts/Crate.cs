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
        // لو أحد حاط شي فوق الصندوق ناخذه أول
        if (item != null) return base.Take(chef);
        return ItemFactory.Create(ingredient, itemMaterial, itemPrefab);
    }
}
