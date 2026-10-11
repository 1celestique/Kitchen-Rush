using UnityEngine;

// ==========================================================
// KitchenCamera: الكاميرا تلحق الطباخة لما تطلع برا المطبخ (يمين أو يسار)
// وترجع لمكانها لما ترجع داخل. أداة البناء تحطها على Main Camera لحالها
// ==========================================================
public class KitchenCamera : MonoBehaviour
{
    public Transform target;          // الطباخة
    public float freeZone = 3.5f;     // كم تمشي يمين ويسار قبل ما تتحرك الكاميرا
    public float maxShift = 3.5f;     // أقصى حركة للكاميرا
    public float smooth = 4f;

    Vector3 basePos;

    void Start()
    {
        basePos = transform.position;
        if (target == null)
        {
            var c = FindAnyObjectByType<ChefController>();
            if (c != null) target = c.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        float x = target.position.x;
        float shift = 0f;
        if (x > freeZone) shift = x - freeZone;
        else if (x < -freeZone) shift = x + freeZone;
        shift = Mathf.Clamp(shift, -maxShift, maxShift);
        Vector3 want = basePos + new Vector3(shift, 0f, 0f);
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}
