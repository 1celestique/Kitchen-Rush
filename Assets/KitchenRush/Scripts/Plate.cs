using System.Collections.Generic;
using UnityEngine;

// ==========================================================
// Plate: الصحن، تنحط فيه المكونات الجاهزة وبعدين يتسلم
// ==========================================================
public class Plate : KitchenItem
{
    public List<KitchenItem> contents = new List<KitchenItem>();

    public override bool Ready { get { return false; } }

    public bool TryAdd(KitchenItem i, out string why)
    {
        why = null;
        if (i == null || i is Plate) return false;
        if (i.IsDrink)
        {
            if (i.ingredient == Ingredient.Milk) why = "Pour the milk into the matcha or the cereal";
            else if (i.ingredient == Ingredient.Cereal) why = i.milk ? "Serve the bowl to Mom as it is!" : "Add milk from the fridge first";
            else why = "Drinks don't go on a plate! Serve it as it is";
            return false;
        }
        if (!i.Ready)
        {
            if (i.burnt) why = "It's burnt! Throw it away";
            else if (i.Choppable && !i.chopped) why = "Chop it first!";
            else if (i.Brewable && !i.chopped) why = i.ingredient == Ingredient.Matcha ? "Whisk the matcha first!" : "Brew the coffee first!";
            else why = "Cook it first!";
            return false;
        }
        foreach (var c in contents)
            if (c.ingredient == i.ingredient) { why = "Already on the plate"; return false; }

        contents.Add(i);
        i.transform.SetParent(transform, true);
        i.transform.localRotation = Quaternion.identity;
        i.transform.localScale = i.transform.localScale * 0.8f;
        Arrange();
        ShowDish();
        return true;
    }

    // لما الصحن يكمّل طبق، نعرض مجسم الطبق الجاهز (برقر، سلطة، قهوة ...)
    GameObject dish;
    void ShowDish()
    {
        var om = OrderManager.Instance;
        if (om == null) return;
        GameObject prefab = null;
        foreach (var r in om.AllRecipes)
            if (Matches(r)) { prefab = om.DishModel(r.name); break; }
        if (dish != null) { Destroy(dish); dish = null; }
        foreach (var c in contents) if (c != null) c.gameObject.SetActive(prefab == null);
        if (prefab == null) return;
        dish = Instantiate(prefab, transform);
        dish.name = "Dish";
        foreach (var c in dish.GetComponentsInChildren<Collider>()) Destroy(c);
        dish.transform.localPosition = Vector3.zero;
        dish.transform.localRotation = Quaternion.identity;
        var rs = dish.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float max = Mathf.Max(b.size.x, b.size.z);
        if (max > 0.0001f) dish.transform.localScale *= 0.42f / max;
        b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        dish.transform.position -= new Vector3(b.center.x - transform.position.x, b.min.y - (transform.position.y + 0.03f), b.center.z - transform.position.z);
        if (prefab.name.Contains("tea")) foreach (var r in rs) foreach (var m in r.materials) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * new Color(0.75f, 1.15f, 0.6f)); }
    }

    static int Order(Ingredient g)
    {
        switch (g)
        {
            case Ingredient.Bun: return 0;
            case Ingredient.Meat: return 1;
            case Ingredient.Cheese: return 2;
            case Ingredient.Lettuce: return 3;
            case Ingredient.Tomato: return 4;
            default: return 5;
        }
    }

    // يرتب المكونات فوق بعض (الخبز تحت، اللحم، الجبن، الخس، الطماطم)
    void Arrange()
    {
        contents.Sort((a, b) => Order(a.ingredient).CompareTo(Order(b.ingredient)));
        float y = transform.position.y + 0.04f;
        foreach (var c in contents)
        {
            c.transform.localPosition = Vector3.zero;
            Bounds b;
            if (c.GetBounds(out b))
            {
                c.transform.position += Vector3.up * (y - b.min.y);
                y += b.size.y * 0.75f;
            }
        }
    }

    public void Clear()
    {
        foreach (var c in contents) if (c != null) Destroy(c.gameObject);
        contents.Clear();
        if (dish != null) { Destroy(dish); dish = null; }
    }

    public bool Matches(Recipe r)
    {
        if (r == null || r.parts == null || r.parts.Length != contents.Count) return false;
        foreach (var p in r.parts)
        {
            bool found = false;
            foreach (var c in contents) if (c.ingredient == p) { found = true; break; }
            if (!found) return false;
        }
        return true;
    }
}
