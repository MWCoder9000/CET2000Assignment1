using UnityEngine;

// CET2000 Lab 05 - Finite State Machines.  *** SOLUTION ***
// A guard NPC controlled by a finite state machine (enum + switch).
// States: Idle, Patrol, Chase, Attack, Flee (+ Search, expert).
// Exactly one state is active. Each physics step: run the current state's Update action,
// then check its transitions in priority order. ChangeState() runs the Exit and Enter actions.

public enum GuardState { Idle, Patrol, Chase, Attack, Flee, Search }

[RequireComponent(typeof(Rigidbody2D))]
public class GuardFSM_Template : MonoBehaviour
{
    [Header("Current state (watch this in Play mode)")]
    [SerializeField] GuardState state = GuardState.Idle;

    [Header("Links (set by the CET2000 menu)")]
    public Transform player;
    public Transform[] waypoints;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Senses")]
    public float viewDistance = 5f;
    public float fieldOfView = 110f;              // degrees

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 2.6f;
    public float fleeSpeed = 3f;
    public float turnSpeed = 360f;                // degrees per second

    [Header("Rules")]
    public float idleTime = 2f;                   // Idle -> Patrol after this many seconds
    public float attackRange = 1.2f;              // Chase -> Attack when this close
    public float attackExitRange = 1.6f;          // Attack -> Chase when further than this (Expert E2)
    public float attackCooldown = 0.8f;           // seconds between hits
    public float fleeHealth = 30f;                // Flee when health drops below this
    public float safeDistance = 4f;               // Flee -> Idle when this far away ...
    public float safeTime = 3f;                   // ... for this many seconds

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float regenPerSecond = 8f;

    [Header("Expert E1: Search state")]
    public bool useSearchState = true;
    public float searchTime = 4f;

    [Header("Expert E3: one class per state")]
    public bool useStateClasses = false;
    public readonly GuardStateSet S = new GuardStateSet();

    // ---- read by the HUD, the label and the state classes ----
    public GuardState State => state;
    public float TimeInState => stateTimer;
    public bool CanSee => canSeePlayer;
    public Vector2 LastSeenPosition => lastSeenPosition;
    public int HitsOnPlayer { get; private set; }
    public event System.Action<GuardState, GuardState> StateChanged;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float stateTimer;          // seconds in the current state (reset by ChangeState)
    float attackTimer;         // counts down to the next hit
    float safeTimer;           // seconds spent far enough away while fleeing
    int waypointIndex;
    bool canSeePlayer;
    Vector2 lastSeenPosition;
    Vector2 startPosition;
    float startRotation;
    IGuardState current;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        startPosition = rb.position;
        startRotation = rb.rotation;
    }

    void Start()
    {
        OnEnter(state);
        if (useStateClasses) SetState(S.Idle);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        stateTimer += dt;
        canSeePlayer = CanSeePlayer();
        if (canSeePlayer) lastSeenPosition = player.position;

        if (useStateClasses)                       // Expert E3: the same FSM, one class per state
        {
            if (current == null) SetState(S.Idle);
            current.Tick(this, dt);
            return;
        }

        switch (state)
        {
            case GuardState.Idle:                  // TODO 1
                StopMoving();
                Regenerate(dt);
                if (canSeePlayer) ChangeState(GuardState.Chase);
                else if (stateTimer >= idleTime) ChangeState(GuardState.Patrol);
                break;

            case GuardState.Patrol:                // TODO 2
                PatrolWaypoints(patrolSpeed);
                if (health < fleeHealth) ChangeState(GuardState.Flee); 
                if (canSeePlayer) ChangeState(GuardState.Chase);
                break;

            case GuardState.Chase:                 // TODO 3  (most important rule first)
                MoveTowards(player.position, chaseSpeed);
                if (health < fleeHealth) ChangeState(GuardState.Flee);
                else if (DistanceToPlayer() <= attackRange) ChangeState(GuardState.Attack);
                else if (!canSeePlayer) ChangeState(useSearchState ? GuardState.Search : GuardState.Patrol);
                break;

            case GuardState.Attack:                // TODO 4
                StopMoving();
                FacePlayer();
                attackTimer -= dt;
                if (attackTimer <= 0f) { HitPlayer(); attackTimer = attackCooldown; }
                if (health < fleeHealth) ChangeState(GuardState.Flee);
                else if (DistanceToPlayer() > attackExitRange) ChangeState(GuardState.Chase);
                break;

            case GuardState.Flee:                  // TODO 5
                MoveAwayFrom(player.position, fleeSpeed);
                Regenerate(dt);
                safeTimer = DistanceToPlayer() >= safeDistance ? safeTimer + dt : 0f;
                if (safeTimer >= safeTime) ChangeState(GuardState.Idle);
                break;

            case GuardState.Search:                // Expert E1
                if (ReachedPoint(lastSeenPosition)) LookAround(dt);
                else MoveTowards(lastSeenPosition, patrolSpeed);
                if (canSeePlayer) ChangeState(GuardState.Chase);
                else if (stateTimer >= searchTime) ChangeState(GuardState.Patrol);
                break;
        }
    }

    // ------------------------------------------------------------ CHANGE STATE (provided)
    public void ChangeState(GuardState next)
    {
        if (next == state) return;
        GuardState previous = state;
        Debug.Log($"[Guard] {previous} -> {next}  (after {stateTimer:0.0} s)");
        OnExit(previous);
        state = next;
        stateTimer = 0f;
        OnEnter(next);
        StateChanged?.Invoke(previous, next);
    }

    // ------------------------------------------------------------ ENTER / EXIT ACTIONS (TODO 6)
    void OnEnter(GuardState s)
    {
        switch (s)
        {
            case GuardState.Idle: SetColour(new Color(0.65f, 0.65f, 0.65f)); StopMoving(); break;
            case GuardState.Patrol: SetColour(new Color(0.25f, 0.55f, 0.95f)); waypointIndex = NearestWaypoint(); break;
            case GuardState.Chase: SetColour(new Color(1f, 0.6f, 0.1f)); Shout("Hey!"); break;
            case GuardState.Attack: SetColour(new Color(0.9f, 0.2f, 0.2f)); attackTimer = 0.2f; break;
            case GuardState.Flee: SetColour(new Color(0.6f, 0.35f, 0.85f)); safeTimer = 0f; Shout("Retreat!"); break;
            case GuardState.Search: SetColour(new Color(0.95f, 0.85f, 0.2f)); Shout("Where did you go?"); break;
        }
    }

    void OnExit(GuardState s)
    {
        switch (s)
        {
            case GuardState.Attack: StopMoving(); break;
            case GuardState.Flee: Debug.Log("[Guard] Recovered, health " + health.ToString("0")); break;
        }
    }

    // ------------------------------------------------------------ EXPERT E3 hook (provided)
    public void SetState(IGuardState next)
    {
        if (next == null || next == current) return;
        GuardState previous = state;
        if (current != null) current.Exit(this);
        current = next;
        if (state != next.Id) Debug.Log($"[Guard/classes] {previous} -> {next.Id}  (after {stateTimer:0.0} s)");
        state = next.Id;
        stateTimer = 0f;
        current.Enter(this);
        if (previous != state) StateChanged?.Invoke(previous, state);
    }

    // ============================================================ HELPERS (provided, do not change)
    // ---- sensing ----
    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.magnitude > viewDistance) return false;                        // too far
        if (Vector2.Angle(Forward, toPlayer) > fieldOfView * 0.5f) return false;    // outside the view cone
        RaycastHit2D hit = Physics2D.Raycast(rb.position, toPlayer.normalized, viewDistance, playerLayer.value | obstacleLayer.value);
        return hit.collider != null && (hit.collider.transform == player || hit.collider.transform.IsChildOf(player));
    }

    public float DistanceToPlayer() => player == null ? Mathf.Infinity : Vector2.Distance(rb.position, player.position);
    public Vector2 PlayerPosition => player == null ? rb.position : (Vector2)player.position;
    public bool ReachedPoint(Vector2 p) => Vector2.Distance(rb.position, p) < 0.3f;

    // ---- movement ----
    public void MoveTowards(Vector2 target, float speed)
    {
        Vector2 d = target - rb.position;
        if (d.sqrMagnitude < 0.0025f) { StopMoving(); return; }
        Drive(Steer(d.normalized), speed);
    }

    public void MoveAwayFrom(Vector2 point, float speed)
    {
        Vector2 d = rb.position - point;
        if (d.sqrMagnitude < 1e-6f) d = Forward;
        Drive(Steer(d.normalized), speed);
    }

    public void PatrolWaypoints(float speed)
    {
        if (waypoints == null || waypoints.Length == 0) { StopMoving(); return; }
        Vector2 target = waypoints[waypointIndex].position;
        if (ReachedPoint(target)) waypointIndex = (waypointIndex + 1) % waypoints.Length;
        MoveTowards(waypoints[waypointIndex].position, speed);
    }

    public void StopMoving()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void FacePlayer()
    {
        Vector2 d = PlayerPosition - rb.position;
        if (d.sqrMagnitude > 1e-6f) Turn(d.normalized);
    }

    public void LookAround(float dt)
    {
        StopMoving();
        rb.MoveRotation(rb.rotation + 120f * dt);
    }

    // ---- actions ----
    public void HitPlayer()
    {
        HitsOnPlayer++;
        FSMPlayerController pc = player != null ? player.GetComponent<FSMPlayerController>() : null;
        if (pc != null) pc.TakeHit();
        Debug.Log("[Guard] Hits the player (" + HitsOnPlayer + ")");
    }

    public void TakeDamage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
        Debug.Log("[Guard] Took " + amount + " damage, health " + health.ToString("0"));
    }

    public void Regenerate(float dt) => health = Mathf.Min(maxHealth, health + regenPerSecond * dt);

    public void SetColour(Color c) { if (sr != null) sr.color = c; }

    public void Shout(string words) { shout = words; shoutUntil = Time.time + 1.2f; }

    public void ResetGuard()
    {
        rb.position = startPosition;
        rb.rotation = startRotation;
        transform.SetPositionAndRotation(startPosition, Quaternion.Euler(0f, 0f, startRotation));
        StopMoving();
        health = maxHealth;
        HitsOnPlayer = 0;
        waypointIndex = 0;
        current = null;
        GuardState previous = state;
        state = GuardState.Idle;
        stateTimer = 0f;
        OnEnter(GuardState.Idle);
        if (previous != state) StateChanged?.Invoke(previous, state);
    }

    // ---- internals ----
    Vector2 Forward => DirFromAngle(rb != null ? rb.rotation : transform.eulerAngles.z);

    static Vector2 DirFromAngle(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(r), Mathf.Cos(r));          // rotation 0 = facing up
    }

    void Turn(Vector2 dir)
    {
        float target = Vector2.SignedAngle(Vector2.up, dir);
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        rb.angularVelocity = 0f;
    }

    void Drive(Vector2 dir, float speed)
    {
        Turn(dir);
        rb.linearVelocity = dir * speed;
    }

    // Simple obstacle avoidance: if the way ahead is blocked, try turning 45 then 90 degrees.
    Vector2 Steer(Vector2 dir)
    {
        float[] tries = { 0f, 45f, -45f, 90f, -90f };
        foreach (float a in tries)
        {
            Vector2 d = Quaternion.Euler(0f, 0f, a) * (Vector3)dir;
            if (!Physics2D.CircleCast(rb.position, 0.2f, d, 0.6f, obstacleLayer.value)) return d;
        }
        return dir;
    }

    int NearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return 0;
        int best = 0;
        float bestDist = Mathf.Infinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector2.Distance(rb.position, waypoints[i].position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // ---- state label above the guard's head (provided) ----
    string shout = "";
    float shoutUntil;

    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.55f);
        if (sp.z < 0f) return;
        float x = sp.x, y = Screen.height - sp.y;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
        GUI.color = Color.black;
        GUI.Label(new Rect(x - 60, y - 34, 120, 20), state.ToString().ToUpper(), style);
        GUI.color = Color.white;
        GUI.Box(new Rect(x - 25, y - 14, 50, 6), GUIContent.none);
        GUI.color = Color.Lerp(Color.red, Color.green, health / maxHealth);
        GUI.DrawTexture(new Rect(x - 25, y - 14, 50 * (health / maxHealth), 6), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (Time.time < shoutUntil)
        {
            GUI.color = Color.black;
            GUI.Label(new Rect(x - 80, y - 56, 160, 20), shout, style);
            GUI.color = Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.6f);
        Gizmos.DrawWireSphere(p, viewDistance);
        float rot = transform.eulerAngles.z;
        Gizmos.DrawLine(p, p + (Vector3)(DirFromAngle(rot + fieldOfView * 0.5f) * viewDistance));
        Gizmos.DrawLine(p, p + (Vector3)(DirFromAngle(rot - fieldOfView * 0.5f) * viewDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p, attackRange);
        if (waypoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
                if (waypoints[i] != null && waypoints[(i + 1) % waypoints.Length] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
