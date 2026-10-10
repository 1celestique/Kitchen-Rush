using UnityEngine;

// ==========================================================
// KitchenItem: أي شي ينشال باليد (خس، طماطم، لحم ...)
// ItemFactory: يسوي المكوّن لما تاخذينه من الصندوق
// ==========================================================
public enum Ingredient { Lettuce, Tomato, Meat, Bun, Potato, Cheese }

public class KitchenItem : MonoBehaviour
{
    public Ingredient ingredient;
    public bool chopped;
    public bool cooked;
    public bool burnt;

    public bool Choppable
    {
        get { return ingredient == Ingredient.Lettuce || ingredient == Ingredient.Tomato || ingredient == Ingredient.Potato || ingredient == Ingredient.Cheese; }
    }

    // جاهز ينحط في الصحن؟
    public virtual bool Ready
    {
        get
        {
            if (burnt) return false;
            switch (ingredient)
            {
                case Ingredient.Meat: return cooked;
                case Ingredient.Bun: return true;
                case Ingredient.Potato: return chopped && cooked;
                default: return chopped;
            }
        }
    }

    public bool GetBounds(out Bounds b)
    {
        b = new Bounds(transform.position, Vector3.zero);
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return true;
    }

    // يحطه بحيث قاعدته على النقطة
    public void PlaceOn(Vector3 top)
    {
        transform.position = top;
        Bounds b;
        if (GetBounds(out b)) transform.position += Vector3.up * (top.y - b.min.y);
    }

    // يتقطع: الشكل يصير ثلاث قطع صغيرة
    public void SetChopped()
    {
        if (chopped) return;
        chopped = true;
        if (transform.childCount == 0) return;
        Transform v = transform.GetChild(0);
        Vector3[] offs = { new Vector3(-0.09f, 0f, 0.02f), new Vector3(0.09f, 0f, 0.05f), new Vector3(0.01f, 0f, -0.09f) };
        foreach (var o in offs)
        {
            var piece = Instantiate(v.gameObject, transform);
            piece.name = "Piece";
            piece.transform.localScale = v.localScale * 0.55f;
            piece.transform.localRotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            piece.transform.localPosition = v.localPosition * 0.55f + o;
        }
        v.gameObject.SetActive(false);
        Destroy(v.gameObject);
    }
}

public static class ItemFactory
{
    // prefab: مجسم الأكل من Food Kit (لو موجود)، وإذا مو موجود يسوي شكل بسيط
    public static KitchenItem Create(Ingredient ing, Material mat, GameObject prefab = null)
    {
        var go = new GameObject("Item_" + ing);
        GameObject model;
        if (prefab != null)
        {
            model = Object.Instantiate(prefab);
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
            model = GameObject.CreatePrimitive(shape);
            model.transform.localScale = size;
            if (mat != null) model.GetComponent<Renderer>().sharedMaterial = mat;
        }
        model.name = "Model";
        foreach (var c in model.GetComponentsInChildren<Collider>()) { c.enabled = false; Object.Destroy(c); }
        model.transform.SetParent(go.transform, true);
        model.transform.position = Vector3.zero;

        var item = go.AddComponent<KitchenItem>();
        item.ingredient = ing;

        Bounds b;
        if (prefab != null && item.GetBounds(out b))
        {
            float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max > 0.0001f) model.transform.localScale *= 0.38f / max;
        }
        if (item.GetBounds(out b)) model.transform.position -= b.center - go.transform.position;
        return item;
    }
}
