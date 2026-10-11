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
        var p = chef.held as Plate;
        if (p == null) { KitchenHUD.Flash("Put it on a plate first"); return true; }
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
        return chef.held is Plate ? hint : null;
    }
}
