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
        if (!i.Ready)
        {
            if (i.burnt) why = "It's burnt! Throw it away";
            else if (i.Choppable && !i.chopped) why = "Chop it first!";
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
        return true;
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
