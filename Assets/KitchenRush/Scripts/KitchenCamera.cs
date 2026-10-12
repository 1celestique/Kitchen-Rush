using UnityEngine;

// ==========================================================
// KitchenCamera: الكاميرا تلحق الطباخة لما تطلع برا المطبخ (يمين أو يسار)
// وتقرّب عليها وهي تقطع أو تحضّر (zoomOnWork)، وترجع لمكانها بعدين
// أداة البناء تحطها على Main Camera لحالها
// ==========================================================
public class KitchenCamera : MonoBehaviour
{
    public Transform target;          // الطباخة
    public float freeZone = 3.5f;     // كم تمشي يمين ويسار قبل ما تتحرك الكاميرا
    public float maxShift = 3.5f;     // أقصى حركة للكاميرا
    public float smooth = 4f;

    [Header("تقريب وقت التقطيع")]
    public bool zoomOnWork = true;
    public float zoomAmount = 0.55f;  // كم تقرب (0 ما تقرب، 1 لين الطباخة)
    public float zoomFov = 34f;

    Vector3 basePos;
    float baseFov;
    float zoom;
    Camera cam;
    ChefController chef;

    void Start()
    {
        basePos = transform.position;
        cam = GetComponent<Camera>();
        if (cam != null) baseFov = cam.fieldOfView;
        if (target == null)
        {
            var c = FindAnyObjectByType<ChefController>();
            if (c != null) target = c.transform;
        }
        if (target != null) chef = target.GetComponent<ChefController>();
    }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.deltaTime;
        float x = target.position.x;
        float shift = 0f;
        if (x > freeZone) shift = x - freeZone;
        else if (x < -freeZone) shift = x + freeZone;
        shift = Mathf.Clamp(shift, -maxShift, maxShift);
        Vector3 want = basePos + new Vector3(shift, 0f, 0f);

        // تقريب ناعم على الطباخة وهي تشتغل
        bool working = zoomOnWork && chef != null && chef.IsWorking;
        zoom = Mathf.MoveTowards(zoom, working ? 1f : 0f, dt * (working ? 2.2f : 1.6f));
        float z = Mathf.SmoothStep(0f, 1f, zoom);
        if (z > 0.001f)
        {
            Vector3 focus = target.position + Vector3.up * 1.0f;
            Vector3 toFocus = focus - transform.forward * 4.2f;   // نفس زاوية الكاميرا بس أقرب
            want = Vector3.Lerp(want, Vector3.Lerp(basePos, toFocus, zoomAmount * 1.8f), z);
        }
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * 1.5f * dt));
        if (cam != null) cam.fieldOfView = Mathf.Lerp(baseFov, zoomFov, z);
    }
}
