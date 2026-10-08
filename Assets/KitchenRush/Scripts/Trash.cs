using UnityEngine;

// ==========================================================
// Trash: سلة المهملات، أي شي ينحط فيها يختفي
// ==========================================================
public class Trash : Station
{
    public override bool CanTake(ChefController chef) { return false; }
    public override bool CanPut(KitchenItem i) { return true; }

    public override void Put(KitchenItem i)
    {
        if (i != null) Destroy(i.gameObject);
    }
}
