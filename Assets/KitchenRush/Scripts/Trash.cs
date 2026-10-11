using UnityEngine;

// ==========================================================
// Trash: سلة المهملات
// أي شي ينحط فيها يختفي، والصحن يتفضى ويبقى في يدك
// ==========================================================
public class Trash : Station
{
    public override bool CanTake(ChefController chef) { return false; }
    public override bool CanPut(KitchenItem i) { return true; }

    public override bool TryCombine(ChefController chef)
    {
        var plate = chef.held as Plate;
        if (plate == null) return false;
        plate.Clear();
        KitchenAudio.Play("drop");
        return true;
    }

    public override void Put(KitchenItem i)
    {
        if (i != null) Destroy(i.gameObject);
        KitchenAudio.Play("drop");
    }
}
