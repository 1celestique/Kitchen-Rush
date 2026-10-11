using UnityEngine;

// ==========================================================
// Grill: الشواية
// حطي اللحم النيء عليها (E) وينطبخ لحاله، شريط برتقالي يوريك كم باقي
// لما يستوي يصير بني ويرن جرس. لو تأخرتي عليه يطلع شريط أحمر، وبعدها يحترق
// اللحم المحروق ما ينفع، ارميه في الزبالة
// ==========================================================
public class Grill : Station
{
    public float cookTime = 5f;     // كم ثانية لين يستوي
    public float burnTime = 8f;     // كم ثانية بعد ما يستوي لين يحترق
    public Renderer coals;          // الفحم (يتوهج وهو يطبخ)

    float t;

    public override bool CanPut(KitchenItem i)
    {
        return item == null && i != null && !(i is Plate) && i.ingredient == Ingredient.Meat && !i.burnt;
    }

    public override void Put(KitchenItem i)
    {
        base.Put(i);
        t = i.cooked ? cookTime : 0f;
        KitchenAudio.Play("drop");
    }

    public override KitchenItem Take(ChefController chef)
    {
        KitchenAudio.Loop(this, "sizzle", false);
        return base.Take(chef);
    }

    public override bool TryCombine(ChefController chef)
    {
        bool had = item != null;
        bool r = base.TryCombine(chef);
        if (had && item == null) KitchenAudio.Loop(this, "sizzle", false);
        return r;
    }

    public override string Hint(ChefController chef)
    {
        var h = chef.held;
        if (item == null && h != null && !(h is Plate) && h.ingredient == Ingredient.Meat && !h.cooked) return "[E] Grill it";
        if (item != null && item.cooked && !item.burnt) return h is Plate ? "[E] Put it on the plate" : "Ready! [E] Take it";
        if (item != null && item.burnt) return "Burnt! Throw it away";
        return null;
    }

    void Update()
    {
        bool cooking = item != null && !item.burnt;
        KitchenAudio.Loop(this, "sizzle", cooking, 0.35f);
        if (coals != null)
        {
            var c = cooking ? new Color(1f, 0.45f, 0.1f) * (1.6f + Mathf.Sin(Time.time * 6f) * 0.3f) : new Color(0.35f, 0.12f, 0.05f);
            var b = new MaterialPropertyBlock();
            coals.GetPropertyBlock(b);
            b.SetColor("_EmissionColor", c);
            coals.SetPropertyBlock(b);
        }
        if (!cooking) return;

        t += Time.deltaTime;
        Vector3 barPos = TopPoint() + Vector3.up * 0.75f;
        if (!item.cooked)
        {
            KitchenHUD.Bar(this, barPos, t / cookTime, new Color(1f, 0.6f, 0.15f));
            if (t >= cookTime)
            {
                item.cooked = true;
                item.Tint(new Color(0.55f, 0.36f, 0.24f));
                KitchenAudio.Play("ding");
            }
        }
        else
        {
            float w = (t - cookTime) / burnTime;
            // تحذير بس في النص الثاني
            if (w > 0.4f && Mathf.Repeat(Time.time * 4f, 1f) < 0.65f)
                KitchenHUD.Bar(this, barPos, w, new Color(0.9f, 0.2f, 0.15f));
            if (w >= 1f)
            {
                item.burnt = true;
                item.Tint(new Color(0.12f, 0.1f, 0.09f));
                KitchenAudio.Play("burnt");
                KitchenHUD.Flash("The meat burnt!", new Color(1f, 0.45f, 0.4f));
            }
        }
    }

    void OnDisable() { KitchenAudio.Loop(this, "sizzle", false); }
}
