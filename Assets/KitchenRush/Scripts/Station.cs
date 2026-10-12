using UnityEngine;

// ==========================================================
// Station: أي طاولة تقدرين تحطين عليها شي وتاخذينه
// الصندوق ولوح التقطيع والصحون والتسليم والزبالة كلها مبنية عليها
// ==========================================================
public class Station : MonoBehaviour
{
    public KitchenItem item;        // الشي اللي على الطاولة الحين
    public float topExtra = 0f;     // ارتفاع زيادة فوق الطاولة (مثل الصندوق)

    public virtual bool CanTake(ChefController chef) { return item != null; }

    public virtual KitchenItem Take(ChefController chef)
    {
        var i = item;
        item = null;
        return i;
    }

    public virtual bool CanPut(KitchenItem i) { return item == null; }

    public virtual void Put(KitchenItem i)
    {
        item = i;
        i.transform.SetParent(null);
        i.transform.rotation = Quaternion.identity;
        i.PlaceOn(TopPoint());
    }

    // لما الطباخة شايلة شي والطاولة عليها شي: نحط المكوّن في الصحن
    public virtual bool TryCombine(ChefController chef)
    {
        var h = chef.held;
        if (h == null) return false;
        string why;

        // حليب + ماتشا / كورن فليكس
        if (item != null && !(h is Plate) && !(item is Plate))
        {
            if (h.ingredient == Ingredient.Milk && item.CanTakeMilk) { item.AddMilk(); Destroy(chef.Release().gameObject); return true; }
            if (item.ingredient == Ingredient.Milk && h.CanTakeMilk) { h.AddMilk(); Destroy(item.gameObject); item = null; return true; }
        }

        var plateHere = item as Plate;
        if (plateHere != null && !(h is Plate))
        {
            if (plateHere.TryAdd(h, out why)) { chef.Release(); return true; }
            if (why != null) { KitchenHUD.Flash(why); return true; }
            return false;
        }

        var held = h as Plate;
        if (held != null && item != null && !(item is Plate))
        {
            if (held.TryAdd(item, out why)) { item = null; return true; }
            if (why != null) KitchenHUD.Flash(why);
            return true;
        }
        return false;
    }

    // ليش ما ينحط هنا؟ (يطلع للاعب مع صوت غلط)
    public virtual string RejectReason(KitchenItem i)
    {
        return item != null ? "There's something here already" : "You can't put that here";
    }

    // لما تمسك Space قدام الطاولة (التقطيع)
    public virtual void Work(ChefController chef, float dt) { }

    public virtual string Hint(ChefController chef)
    {
        var h = chef.held;
        if (h != null && item != null && !(h is Plate) && !(item is Plate))
            if ((h.ingredient == Ingredient.Milk && item.CanTakeMilk) || (item.ingredient == Ingredient.Milk && h.CanTakeMilk)) return "[E] Add the milk";
        return null;
    }

    // سطح الطاولة من فوق
    public Vector3 TopPoint()
    {
        var c = GetComponent<Collider>();
        if (c != null)
        {
            var b = c.bounds;
            return new Vector3(b.center.x, b.max.y + topExtra, b.center.z);
        }
        return transform.position + Vector3.up * (1f + topExtra);
    }
}
