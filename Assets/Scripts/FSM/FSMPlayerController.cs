using UnityEngine;

// CET2000 Lab 05 - the player (provided, no changes needed).
// Move: W A S D or the arrow keys (click the Game view first).  Hit the guard: Space when close.
// Keys are read through OnGUI events, so this works with the old Input Manager AND the new Input System.
[RequireComponent(typeof(Rigidbody2D))]
public class FSMPlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public GuardFSM_Template guard;
    public float hitRange = 1.5f;
    public float hitDamage = 25f;

    [Header("Autopilot (wanders by itself, handy for testing)")]
    public bool autopilot = false;
    public LayerMask obstacleLayer;
    public Vector2 areaMin = new Vector2(-4.6f, -2.6f);
    public Vector2 areaMax = new Vector2(4.6f, 2.6f);

    public int TimesHit { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColour;
    bool up, down, left, right;
    float flashUntil, nextHit, nextTarget;
    Vector2 autoTarget, startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        baseColour = sr != null ? sr.color : Color.white;
        startPosition = rb.position;
        autoTarget = rb.position;
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown && e.type != EventType.KeyUp) return;
        bool pressed = e.type == EventType.KeyDown;
        switch (e.keyCode)
        {
            case KeyCode.W: case KeyCode.UpArrow: up = pressed; break;
            case KeyCode.S: case KeyCode.DownArrow: down = pressed; break;
            case KeyCode.A: case KeyCode.LeftArrow: left = pressed; break;
            case KeyCode.D: case KeyCode.RightArrow: right = pressed; break;
            case KeyCode.Space: if (pressed) HitGuard(); break;
        }
    }

    void FixedUpdate()
    {
        Vector2 dir;
        if (autopilot)
        {
            if (Vector2.Distance(rb.position, autoTarget) < 0.3f || Time.time > nextTarget) PickTarget();
            dir = (autoTarget - rb.position).normalized;
        }
        else
        {
            dir = new Vector2((right ? 1f : 0f) - (left ? 1f : 0f), (up ? 1f : 0f) - (down ? 1f : 0f));
            if (dir.sqrMagnitude > 1f) dir.Normalize();
        }
        rb.linearVelocity = dir * moveSpeed;
        if (sr != null) sr.color = Time.time < flashUntil ? Color.red : baseColour;
    }

    public void HitGuard()
    {
        if (guard == null || Time.time < nextHit) return;
        nextHit = Time.time + 0.4f;
        if (Vector2.Distance(rb.position, guard.transform.position) <= hitRange) guard.TakeDamage(hitDamage);
        else Debug.Log("[Player] Too far away to hit the guard.");
    }

    public void TakeHit()
    {
        TimesHit++;
        flashUntil = Time.time + 0.15f;
    }

    public void ResetPlayer()
    {
        rb.position = startPosition;
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        TimesHit = 0;
        up = down = left = right = false;
    }

    void PickTarget()
    {
        nextTarget = Time.time + 4f;
        for (int i = 0; i < 30; i++)
        {
            var p = new Vector2(Random.Range(areaMin.x, areaMax.x), Random.Range(areaMin.y, areaMax.y));
            if (Physics2D.OverlapCircle(p, 0.35f, obstacleLayer.value) == null) { autoTarget = p; return; }
        }
    }
}
