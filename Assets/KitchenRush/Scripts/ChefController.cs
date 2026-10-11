using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ==========================================================
// ChefController: الطباخة
// WASD حركة، Shift اندفاع، E تاخذ أو تحط على الطاولة اللي قدامها
// Space تقطّع، والتلميح يطلع فوقها
// v2: حركة ناعمة (تسارع وتباطؤ)، لفة ناعمة، ورجلين ويدين تتحرك
//     وهي تمشي، ونطة خفيفة، وانضغاط مع الاندفاع، ويدينها لقدام لما تشيل شي
// isActive نستخدمه بعدين للتبديل بين الطباختين بزر Tab
// ==========================================================
[RequireComponent(typeof(CharacterController))]
public class ChefController : MonoBehaviour
{
    [Header("الحركة")]
    public float speed = 6f;
    public float acceleration = 50f;     // كم بسرعة توصل لسرعتها
    public float deceleration = 40f;     // كم بسرعة توقف
    public float turnSharpness = 18f;    // كم بسرعة تلف
    public float gravity = -20f;

    [Header("الاندفاع (Shift)")]
    public float dashSpeed = 16f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("الأخذ والوضع (E)")]
    public Transform hand;
    public float reach = 0.55f;

    [Header("الأنيميشن")]
    public bool animateBody = true;
    public float walkAnimSpeed = 1.8f;   // سرعة حركة المشي الأصلية (للشخصية الحقيقية)
    public float stepSpeed = 12f;        // سرعة الخطوات
    public float legSwing = 35f;
    public float bobHeight = 0.07f;
    public float leanAngle = 8f;

    public bool isActive = true;
    public KitchenItem held;

    CharacterController cc;
    float dashT, cooldownT, vy, walkPhase, squash, workT;
    Vector3 dashDir, moveVel;
    Transform visual, legL, legR, armL, armR;
    Animator anim;
    Transform footL, footR, modelRoot;
    public float footHeight = 0.08f;     // ارتفاع كاحل الشخصية عن الأرض
    bool hasSpeed, hasCarry, hasWork, hasFidget;
    float standT;
    public float fidgetAfter = 8f;      // كم ثانية واقفة قبل ما تسوي حركة الوقوف الكاملة

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (hand == null)
        {
            hand = new GameObject("Hand").transform;
            hand.SetParent(transform, false);
            hand.localPosition = new Vector3(0f, 0.95f, 0.55f);
        }
        // لو فيه شخصية حقيقية (Mixamo) نستخدم حركاتها بدل الحركة المصنوعة
        anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            animateBody = false;
            anim.applyRootMotion = false;
            if (anim.isHuman)
            {
                footL = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                footR = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                modelRoot = anim.transform;
            }
            foreach (var p in anim.parameters)
            {
                if (p.name == "Speed") hasSpeed = true;
                if (p.name == "Carry") hasCarry = true;
                if (p.name == "Work") hasWork = true;
                if (p.name == "Fidget") hasFidget = true;
            }
        }
        else anim = null;
        if (animateBody) SetupVisual();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var k = Keyboard.current;
        if (OrderManager.Instance != null && OrderManager.Instance.ended) return;
        Vector3 input = Vector3.zero;
        if (isActive && k != null)
        {
            if (k.wKey.isPressed) input.z += 1f;
            if (k.sKey.isPressed) input.z -= 1f;
            if (k.dKey.isPressed) input.x += 1f;
            if (k.aKey.isPressed) input.x -= 1f;
        }
        input = Vector3.ClampMagnitude(input, 1f);

        var cam = Camera.main;
        if (cam != null) input = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) * input;

        // تسارع وتباطؤ ناعم
        Vector3 target = input * speed;
        float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;
        moveVel = Vector3.MoveTowards(moveVel, target, rate * dt);

        // الاندفاع
        cooldownT -= dt;
        if (isActive && k != null && k.leftShiftKey.wasPressedThisFrame && cooldownT <= 0f)
        {
            dashT = dashTime;
            cooldownT = dashCooldown;
            dashDir = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;
            squash = 1f;
        }

        Vector3 vel = moveVel;
        if (dashT > 0f)
        {
            vel = dashDir * dashSpeed;
            moveVel = dashDir * speed;
            dashT -= dt;
        }

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * dt;
        vel.y = vy;
        cc.Move(vel * dt);

        // لفة ناعمة لجهة الأزرار
        Vector3 face = input.sqrMagnitude > 0.01f ? input : (dashT > 0f ? dashDir : Vector3.zero);
        if (face.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 1f - Mathf.Exp(-turnSharpness * dt));

        if (isActive && k != null && k.eKey.wasPressedThisFrame) Interact();

        // التقطيع: امسكي Space وأنتي قدام اللوح ويدك فاضية
        workT -= dt;
        if (isActive)
        {
            var front = FindStation();
            if (front != null && k != null && k.spaceKey.isPressed && held == null) front.Work(this, dt);
            string hint = front != null ? front.Hint(this) : null;
            if (hint != null && workT <= 0f) KitchenHUD.Hint(transform.position + Vector3.up * 2.5f, hint);
        }

        if (animateBody) Animate(dt);
        else if (anim != null)
        {
            Vector3 v = cc.velocity;
            float t = Mathf.Clamp01(new Vector3(v.x, 0f, v.z).magnitude / speed);
            if (hasSpeed) anim.SetFloat("Speed", t, 0.05f, dt);
            // سرعة الخطوات تمشي مع سرعة الحركة عشان ما تزحلق
            float hv = new Vector3(v.x, 0f, v.z).magnitude;
            anim.speed = hv > 0.3f ? Mathf.Clamp(hv / walkAnimSpeed, 1f, 2.6f) : 1f;
            if (hasCarry) anim.SetBool("Carry", held != null);
            if (hasWork) anim.SetBool("Work", workT > 0f);
            // لو وقفت فترة طويلة بدون شي في يدها: حركة الوقوف الكاملة مرة وحدة
            if (hv < 0.1f && held == null && workT <= 0f) standT += dt; else standT = 0f;
            if (hasFidget && standT > fidgetAfter) { anim.SetTrigger("Fidget"); standT = -6f; }
        }
    }

    // رجول الشخصية دايم على الأرض (عشان ما تطير)
    void LateUpdate()
    {
        if (modelRoot == null || footL == null || footR == null) return;
        float lowest = Mathf.Min(footL.position.y, footR.position.y);
        float target = transform.position.y + footHeight;
        Vector3 lp = modelRoot.localPosition;
        lp.y += (target - lowest) * 0.5f;
        lp.y = Mathf.Clamp(lp.y, -1f, 1f);
        modelRoot.localPosition = lp;
    }

    // ---------- الأنيميشن ----------

    void SetupVisual()
    {
        visual = transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            var parts = new List<Transform>();
            foreach (Transform c in transform)
                if (c != visual && c != hand) parts.Add(c);
            foreach (var c in parts) c.SetParent(visual, true);
        }
        legL = Pivot("Pants_L", 0.36f);
        legR = Pivot("Pants_R", 0.36f);
        armL = Pivot("Coat_ArmL", 1.0f);
        armR = Pivot("Coat_ArmR", 1.0f);
    }

    // نقطة دوران عند الورك أو الكتف عشان الرجل واليد تتحرك من فوق
    Transform Pivot(string partName, float y)
    {
        var part = visual.Find(partName);
        if (part == null) return null;
        var p = new GameObject(partName + "_Pivot").transform;
        p.SetParent(visual, false);
        p.localPosition = new Vector3(part.localPosition.x, y, part.localPosition.z);
        part.SetParent(p, true);
        return p;
    }

    void Animate(float dt)
    {
        if (visual == null) return;
        Vector3 v = cc.velocity;
        float t = Mathf.Clamp01(new Vector3(v.x, 0f, v.z).magnitude / speed);

        if (t > 0.05f) walkPhase += dt * stepSpeed * Mathf.Lerp(0.6f, 1f, t);
        float swing = Mathf.Sin(walkPhase) * legSwing * t;

        if (legL) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        if (legR) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);

        // لما تشيل شي يدينها لقدام
        float armHold = held != null ? -75f : 0f;
        if (workT > 0f) { armHold = -70f + Mathf.Sin(Time.time * 28f) * 30f; swing = 0f; }
        if (armL) armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(armHold - swing * 0.8f * (held != null ? 0.2f : 1f), 0f, 0f), 1f - Mathf.Exp(-20f * dt));
        if (armR) armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(armHold + swing * 0.8f * (held != null ? 0.2f : 1f), 0f, 0f), 1f - Mathf.Exp(-20f * dt));

        float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * bobHeight * t;
        squash = Mathf.MoveTowards(squash, 0f, dt * 5f);
        float breathe = t < 0.05f ? Mathf.Sin(Time.time * 2.5f) * 0.015f : 0f;

        visual.localPosition = new Vector3(0f, bob, 0f);
        visual.localRotation = Quaternion.Euler(leanAngle * t + squash * 12f, 0f, 0f);
        visual.localScale = new Vector3(1f + squash * 0.15f, 1f - squash * 0.15f + breathe, 1f + squash * 0.15f);
    }

    // ---------- الأخذ والوضع ----------

    public Station FindStation()
    {
        Vector3 center = transform.position + transform.forward * 0.75f + Vector3.up * 0.6f;
        Station best = null;
        float bestD = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(center, reach))
        {
            var s = c.GetComponentInParent<Station>();
            if (s == null) continue;
            float d = (c.ClosestPoint(center) - center).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    void Interact()
    {
        var s = FindStation();
        if (s == null) return;

        if (held == null)
        {
            if (s.CanTake(this))
            {
                var it = s.Take(this);
                if (it != null) Hold(it);
            }
        }
        else
        {
            if (s.TryCombine(this)) return;
            if (s.CanPut(held))
            {
                var it = held;
                held = null;
                s.Put(it);
            }
        }
    }

    // الشي اللي في يدها راح (انحط في صحن أو انسلم)
    public KitchenItem Release()
    {
        var i = held;
        held = null;
        return i;
    }

    // تنادى من لوح التقطيع عشان يدينها تتحرك
    public void MarkWorking() { workT = 0.1f; }

    public void Hold(KitchenItem it)
    {
        held = it;
        it.transform.SetParent(hand, false);
        it.transform.localPosition = Vector3.zero;
        it.transform.localRotation = Quaternion.identity;
    }
}
