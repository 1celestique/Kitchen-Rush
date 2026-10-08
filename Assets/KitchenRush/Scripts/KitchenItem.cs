using UnityEngine;

// ==========================================================
// KitchenItem: أي شي ينشال باليد (خس، طماطم، لحم ...)
// ItemFactory: يسوي المكوّن لما تاخذينه من الصندوق
// ==========================================================
public enum Ingredient { Lettuce, Tomato, Meat, Bun, Potato, Cheese }

public class KitchenItem : MonoBehaviour
{
    public Ingredient ingredient;
    public bool chopped;   // نستخدمها بكرة مع لوح التقطيع
    public bool cooked;
    public bool burnt;

    // نص ارتفاع الشكل، عشان ينحط فوق الطاولة بالضبط
    public float HalfHeight
    {
        get
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0.15f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.extents.y;
        }
    }
}

public static class ItemFactory
{
    // prefab: مجسم الأكل من Food Kit (لو موجود)، وإذا مو موجود يسوي شكل بسيط
    public static KitchenItem Create(Ingredient ing, Material mat, GameObject prefab = null)
    {
        GameObject go;
        if (prefab != null)
        {
            go = new GameObject("Item_" + ing);
            var model = Object.Instantiate(prefab);
            foreach (var c in model.GetComponentsInChildren<Collider>()) { c.enabled = false; Object.Destroy(c); }
            Bounds b;
            if (GetBounds(model, out b))
            {
                float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (max > 0.0001f) model.transform.localScale *= 0.38f / max;
                GetBounds(model, out b);
                model.transform.position -= b.center;
            }
            model.transform.SetParent(go.transform, true);
        }
        else
        {
            PrimitiveType shape = PrimitiveType.Cube;
            Vector3 size = new Vector3(0.35f, 0.35f, 0.35f);
            switch (ing)
            {
                case Ingredient.Lettuce: shape = PrimitiveType.Sphere;   size = new Vector3(0.40f, 0.32f, 0.40f); break;
                case Ingredient.Tomato:  shape = PrimitiveType.Sphere;   size = new Vector3(0.32f, 0.30f, 0.32f); break;
                case Ingredient.Meat:    shape = PrimitiveType.Cube;     size = new Vector3(0.40f, 0.14f, 0.40f); break;
                case Ingredient.Bun:     shape = PrimitiveType.Cylinder; size = new Vector3(0.42f, 0.07f, 0.42f); break;
                case Ingredient.Potato:  shape = PrimitiveType.Sphere;   size = new Vector3(0.30f, 0.24f, 0.40f); break;
                case Ingredient.Cheese:  shape = PrimitiveType.Cube;     size = new Vector3(0.36f, 0.08f, 0.36f); break;
            }
            go = GameObject.CreatePrimitive(shape);
            go.name = "Item_" + ing;
            var col = go.GetComponent<Collider>();
            if (col != null) { col.enabled = false; Object.Destroy(col); }
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        var item = go.AddComponent<KitchenItem>();
        item.ingredient = ing;
        return item;
    }

    static bool GetBounds(GameObject g, out Bounds b)
    {
        b = new Bounds(g.transform.position, Vector3.zero);
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return true;
    }
}
