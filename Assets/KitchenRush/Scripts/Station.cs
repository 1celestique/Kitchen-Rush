using UnityEngine;

// ==========================================================
// Station: أي طاولة تقدرين تحطين عليها شي وتاخذينه
// الصندوق وسلة المهملات (وبكرة لوح التقطيع) مبنية عليها
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
        i.transform.position = TopPoint() + Vector3.up * i.HalfHeight;
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
