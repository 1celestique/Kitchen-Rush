using UnityEngine;

// ==========================================================
// BrewStation: مكينة القهوة والماتشا
// حطي كوب القهوة أو الماتشا عليها (E) وامسكي Space لين يمتلي الشريط
// ==========================================================
public class BrewStation : ChopStation
{
    public override bool CanPut(KitchenItem i) { return item == null && i != null && !(i is Plate) && i.Brewable && !i.chopped; }

    public override string RejectReason(KitchenItem i)
    {
        if (item != null) return "The machine is busy";
        if (i != null && i.Brewable && i.chopped) return "It's already ready!";
        return "Only coffee or matcha go in the machine";
    }

    public override bool CanChop { get { return item != null && !(item is Plate) && item.Brewable && !item.chopped; } }

    protected override Color BarColor { get { return new Color(0.6f, 0.4f, 0.25f); } }
    protected override string WorkSound { get { return "click"; } }

    public override string Hint(ChefController chef)
    {
        var h = chef.held;
        if (item == null && h != null && !(h is Plate) && h.Brewable && !h.chopped) return "[E] Put it in the machine";
        if (chef.held == null && CanChop) return item.ingredient == Ingredient.Matcha ? "Hold [Space] to whisk" : "Hold [Space] to brew";
        if (item != null && item.chopped && h != null && h.ingredient == Ingredient.Milk && item.CanTakeMilk) return "[E] Add the milk";
        if (item != null && item.chopped) return "Ready! [E] Take it";
        return null;
    }
}
