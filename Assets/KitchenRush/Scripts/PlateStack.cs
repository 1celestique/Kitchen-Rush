using UnityEngine;

// ==========================================================
// PlateStack: كومة الصحون، تعطيك صحن نظيف كل ما ضغطتي E
// ولو جيتي بمكوّن جاهز يتحط في صحن جديد على طول
// ==========================================================
public class PlateStack : Station
{
    public GameObject plateModel;     // مجسم الصحن (من Food Kit)
    public Material plateMaterial;

    public override bool CanTake(ChefController chef) { return true; }

    public override KitchenItem Take(ChefController chef)
    {
        if (item != null) return base.Take(chef);
        return MakePlate();
    }

    public override bool CanPut(KitchenItem i) { return item == null && i is Plate; }

    public override bool TryCombine(ChefController chef)
    {
        if (item != null) return base.TryCombine(chef);
        var h = chef.held;
        if (h == null || h is Plate) return false;
        var p = MakePlate();
        string why;
        if (p.TryAdd(h, out why)) { chef.Release(); Put(p); return true; }
        Destroy(p.gameObject);
        if (why != null) KitchenHUD.Flash(why);
        return true;
    }

    public Plate MakePlate()
    {
        var go = new GameObject("Plate");
        GameObject m;
        if (plateModel != null) m = Instantiate(plateModel);
        else
        {
            m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            m.transform.localScale = new Vector3(0.48f, 0.02f, 0.48f);
            if (plateMaterial != null) m.GetComponent<Renderer>().sharedMaterial = plateMaterial;
        }
        m.name = "PlateModel";
        foreach (var c in m.GetComponentsInChildren<Collider>()) { c.enabled = false; Destroy(c); }
        m.transform.SetParent(go.transform, true);
        m.transform.position = Vector3.zero;

        var p = go.AddComponent<Plate>();
        Bounds b;
        if (plateModel != null && p.GetBounds(out b))
        {
            float w = Mathf.Max(b.size.x, b.size.z);
            if (w > 0.0001f) m.transform.localScale *= 0.5f / w;
        }
        if (p.GetBounds(out b)) m.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
        return p;
    }
}
