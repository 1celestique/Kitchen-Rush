using UnityEngine;

// ==========================================================
// ServeWindow: شباك التسليم
// تجين بالصحن وتضغطين E، ولو يطابق طلب ينحسب لك
// ==========================================================
public class ServeWindow : Station
{
    public string hint = "[E] Serve";

    public override bool CanTake(ChefController chef) { return false; }
    public override bool CanPut(KitchenItem i) { return false; }

    public override bool TryCombine(ChefController chef)
    {
        var h = chef.held;
        if (h == null) return false;
        var p = h as Plate;
        if (p == null)
        {
            // المشروبات والكورن فليكس تتسلم بدون صحن
            if (h.IsDrink)
            {
                if (!h.Servable)
                {
                    if (h.ingredient == Ingredient.Cereal || h.ingredient == Ingredient.Milk) KitchenHUD.Flash("Add milk from the fridge first");
                    else KitchenHUD.Flash(h.ingredient == Ingredient.Matcha ? "Whisk the matcha first!" : "Brew the coffee first!");
                    return true;
                }
                var om2 = OrderManager.Instance;
                if (om2 != null && om2.DeliverDrink(h)) { chef.Release(); Destroy(h.gameObject); }
                else { KitchenHUD.Flash("No order wants this"); KitchenAudio.Play("wrong"); }
                return true;
            }
            KitchenHUD.Flash("Put it on a plate first");
            return true;
        }
        if (p.contents.Count == 0) { KitchenHUD.Flash("The plate is empty"); return true; }
        var om = OrderManager.Instance;
        if (om != null && om.Deliver(p))
        {
            chef.Release();
            Destroy(p.gameObject);
        }
        else { KitchenHUD.Flash("No order wants this"); KitchenAudio.Play("wrong"); }
        return true;
    }

    public override string Hint(ChefController chef)
    {
        var h = chef.held;
        return h != null && (h is Plate || h.Servable) ? hint : null;
    }
}
