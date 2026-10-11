using UnityEngine;

// ==========================================================
// ChopStation: لوح التقطيع
// حطي المكوّن عليه، وامسكي Space وأنتي قدامه لين يمتلي الشريط
// ==========================================================
public class ChopStation : Station
{
    public float chopTime = 2f;     // كم ثانية يحتاج التقطيع
    float progress;
    bool replace;

    public override bool CanPut(KitchenItem i) { return item == null && !(i is Plate); }

    public override KitchenItem Take(ChefController chef)
    {
        progress = 0f;
        return base.Take(chef);
    }

    public bool CanChop { get { return item != null && !(item is Plate) && item.Choppable && !item.chopped; } }

    public override void Work(ChefController chef, float dt)
    {
        if (!CanChop) return;
        chef.MarkWorking();
        float before = progress;
        progress += dt / Mathf.Max(0.1f, chopTime);
        // طق طق كل ربع ثانية تقريباً
        if (Mathf.FloorToInt(before * chopTime * 4f) != Mathf.FloorToInt(progress * chopTime * 4f)) KitchenAudio.Play("chop", 0.8f);
        if (progress >= 1f)
        {
            progress = 0f;
            item.SetChopped();
            replace = true;
            KitchenAudio.Play("pickup", 0.6f);
        }
    }

    public override string Hint(ChefController chef)
    {
        return chef.held == null && CanChop ? "Hold [Space] to chop" : null;
    }

    void Update()
    {
        if (replace && item != null) { item.PlaceOn(TopPoint()); replace = false; }
        if (CanChop && progress > 0f) KitchenHUD.Bar(this, TopPoint() + Vector3.up * 0.7f, progress, new Color(0.46f, 0.69f, 0.25f));
    }
}
