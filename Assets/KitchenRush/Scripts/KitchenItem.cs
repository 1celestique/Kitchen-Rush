using UnityEngine;

// ==========================================================
// KitchenItem: أي شي ينشال باليد (خس، طماطم، لحم ...)
// ItemFactory: يسوي المكوّن لما تاخذينه من الصندوق
// ==========================================================
public enum Ingredient { Lettuce, Tomato, Meat, Bun, Potato, Cheese, Coffee, Matcha, Milk, Cereal }

public class KitchenItem : MonoBehaviour
{
    public Ingredient ingredient;
    public bool chopped;
    public bool cooked;
    public bool burnt;
    public GameObject processedPrefab;   // شكله بعد التقطيع/التحضير/الطبخ (من Food Kit)
    public bool milk;                    // انحط فيه حليب (ماتشا لاتيه / كورن فليكس)

    // المشروبات والكورن فليكس ما تنحط في صحن، تتسلم على طول
    public bool IsDrink { get { return ingredient == Ingredient.Coffee || ingredient == Ingredient.Matcha || ingredient == Ingredient.Milk || ingredient == Ingredient.Cereal; } }

    // يقبل حليب؟ (ماتشا بعد ما تنخفق، أو الكورن فليكس)
    public bool CanTakeMilk { get { return !milk && ((ingredient == Ingredient.Matcha && chopped) || ingredient == Ingredient.Cereal); } }

    // جاهز يتسلم بدون صحن
    public bool Servable
    {
        get
        {
            if (ingredient == Ingredient.Coffee) return chopped;
            if (ingredient == Ingredient.Matcha) return chopped;
            if (ingredient == Ingredient.Cereal) return milk;
            return false;
        }
    }

    public void AddMilk()
    {
        if (!CanTakeMilk) return;
        milk = true;
        if (ingredient == Ingredient.Cereal) SwapModel(processedPrefab, 0.36f);
        else if (ingredient == Ingredient.Matcha) Tint(new Color(0.85f, 1f, 0.72f));   // أفتح: لاتيه
    }

    // يطابق طلب؟ (المشروب نفسه + الحليب لو فيه)
    public bool MatchesDrink(Recipe r)
    {
        if (r == null || r.parts == null || !Servable) return false;
        int n = milk ? 2 : 1;
        if (r.parts.Length != n) return false;
        foreach (var p in r.parts)
            if (p != ingredient && !(milk && p == Ingredient.Milk)) return false;
        return true;
    }

    // تنحضر بالمكينة (Space): قهوة وماتشا
    public bool Brewable { get { return ingredient == Ingredient.Coffee || ingredient == Ingredient.Matcha; } }

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
                case Ingredient.Milk: return true;
                case Ingredient.Cereal: return true;
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

    // يغير لونه (اللحم لما يستوي أو يحترق). الألوان الأصلية تنحفظ أول مرة
    System.Collections.Generic.Dictionary<Material, Color> baseCols;
    public void Tint(Color mul)
    {
        if (baseCols == null) baseCols = new System.Collections.Generic.Dictionary<Material, Color>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            foreach (var m in r.materials)
            {
                string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : (m.HasProperty("_Color") ? "_Color" : null);
                if (prop == null) continue;
                Color c;
                if (!baseCols.TryGetValue(m, out c)) { c = m.GetColor(prop); baseCols[m] = c; }
                m.SetColor(prop, new Color(c.r * mul.r * 1.2f, c.g * mul.g * 1.2f, c.b * mul.b * 1.2f, c.a));
            }
        }
    }

    // يتقطع: الشكل يصير ثلاث قطع صغيرة
    // يبدل الشكل بمجسم ثاني بنفس الحجم تقريباً
    public bool SwapModel(GameObject prefab, float size = 0.36f)
    {
        if (prefab == null || transform.childCount == 0) return false;
        var old = transform.GetChild(0);
        var m = Instantiate(prefab);
        m.name = "Model";
        foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
        m.transform.SetParent(transform, false);
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        old.gameObject.SetActive(false);
        Destroy(old.gameObject);
        var rs = m.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max > 0.0001f) m.transform.localScale *= size / max;
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            m.transform.position -= new Vector3(b.center.x - transform.position.x, b.min.y - transform.position.y, b.center.z - transform.position.z);
        }
        baseCols = null;
        return true;
    }

    public void SetChopped()
    {
        if (chopped) return;
        chopped = true;
        // لو عندنا شكل جاهز (طماطم مقطعة، جبن مقطع، قهوة، ماتشا) نستخدمه
        if (processedPrefab != null && SwapModel(processedPrefab))
        {
            if (ingredient == Ingredient.Matcha) Tint(new Color(0.6f, 1f, 0.45f));
            return;
        }
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
    public static KitchenItem Create(Ingredient ing, Material mat, GameObject prefab = null, GameObject processed = null)
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
                case Ingredient.Coffee:  shape = PrimitiveType.Cylinder; size = new Vector3(0.22f, 0.12f, 0.22f); break;
                case Ingredient.Matcha:  shape = PrimitiveType.Cylinder; size = new Vector3(0.22f, 0.12f, 0.22f); break;
                case Ingredient.Milk:    shape = PrimitiveType.Cube;     size = new Vector3(0.18f, 0.32f, 0.18f); break;
                case Ingredient.Cereal:  shape = PrimitiveType.Cube;     size = new Vector3(0.26f, 0.34f, 0.1f); break;
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
        item.processedPrefab = processed;

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
