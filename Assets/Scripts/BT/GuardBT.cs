using UnityEngine;

// CET2000 Lab 05A - a guard NPC controlled by a BEHAVIOUR TREE.   *** SOLUTION ***
// You only edit BuildTree() (TODO 3, 4, 5 and the Expert tasks). Everything below it is provided.
// The tree is ticked from the root on every physics step (FixedUpdate).
// The panel on the left of the Game view draws the tree and colours each node by its last status.

[RequireComponent(typeof(Rigidbody2D))]
public class GuardBT : MonoBehaviour
{
    [Header("Links (set by the CET2000 menu)")]
    public Transform player;
    public Transform[] waypoints;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Senses")]
    public float viewDistance = 5f;
    public float fieldOfView = 110f;              // degrees
    public float memorySeconds = 4f;              // how long the guard remembers where it last saw you

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 2.6f;
    public float turnSpeed = 360f;                // degrees per second

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 0.8f;           // seconds between hits

    [Header("Stamina (Expert E1)")]
    public float maxStamina = 100f;
    public float stamina = 100f;
    public float staminaDrainPerSecond = 15f;     // while chasing
    public float staminaRegenPerSecond = 35f;     // while resting

    // ---- read by the panel ----
    public Node Root { get { return root; } }
    public string Doing { get { return doing; } }
    public int HitsOnPlayer { get; private set; }
    public bool CanSee { get { return canSeePlayer; } }

    Node root;
    Rigidbody2D rb;
    SpriteRenderer sr;
    int waypointIndex;
    bool canSeePlayer;
    Vector2 lastSeenPosition;
    float lastSeenTime = -999f;
    float nextHitTime;
    bool needsRest;
    string doing = "";
    Vector2 startPosition;
    float startRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        startPosition = rb.position;
        startRotation = rb.rotation;
        root = BuildTree();
    }

    // =====================================================================================
    //  BUILD THE TREE  (your work goes here)
    // =====================================================================================
    //  Building blocks:
    //    new Selector("name", child, child, ...)     ? try children in order until one works (OR)
    //    new Sequence("name", child, child, ...)     -> do every child in order (AND)
    //    new Condition("Question?", PlayerSeen)      yes/no check
    //    new ActionNode("Do something", Patrol)      an action
    //    new Invert("name", child)                   (Expert E2)  flips Success and Failure
    //    new Cooldown("name", seconds, child)        (Expert E3)  blocks the child for a while
    //
    //  Conditions you can use:  PlayerSeen   PlayerInAttackRange   SeenRecently   Tired
    //  Actions you can use:     Patrol   ChasePlayer   Attack   SearchLastSeen   Rest   HitPlayer   FacePlayerAndWait
    // =====================================================================================
    Node BuildTree()
    {
        return new Selector("Guard",

            // TODO 4  ATTACK: highest priority, so it is the FIRST child
            new Sequence("Attack",
                new Condition("Player seen?", PlayerSeen),
                new Condition("In attack range?", PlayerInAttackRange),
                new ActionNode("Attack", Attack)),

            // EXPERT E1  REST: tired guards stop chasing until stamina is full again
            new Sequence("Rest",
                new Condition("Tired?", Tired),
                new ActionNode("Rest", Rest)),

            // TODO 3  CHASE
            new Sequence("Chase",
                new Condition("Player seen?", PlayerSeen),
                new ActionNode("Chase player", ChasePlayer)),

            // TODO 5  SEARCH: uses the guard's memory of where it last saw you
            //         (Expert E2: Invert makes the "not seen" rule explicit)
            new Sequence("Search",
                new Invert("Not seen?", new Condition("Player seen?", PlayerSeen)),
                new Condition("Seen in last " + memorySeconds + " s?", SeenRecently),
                new ActionNode("Search last seen", SearchLastSeen)),

            // TODO 3  PATROL: lowest priority, so it is the LAST child
            new ActionNode("Patrol", Patrol));
    }

    // Expert E3 (optional): replace the "Attack" action above with this smaller tree, which uses
    // a Cooldown decorator instead of the timer inside Attack():
    //     new Selector("Hit or wait",
    //         new Cooldown("Every " + attackCooldown + " s", attackCooldown, new ActionNode("Hit player", HitPlayer)),
    //         new ActionNode("Face player", FacePlayerAndWait))

    // ------------------------------------------------------------ TICK (provided)
    void FixedUpdate()
    {
        Sense();
        if (needsRest == false && stamina <= 0f) needsRest = true;           // used by Tired (Expert E1)
        if (needsRest && stamina >= maxStamina) needsRest = false;
        Node.CurrentTick++;
        doing = "";
        Status s = root.Tick();
        if (doing == "") { StopMoving(); doing = "nothing (" + s + ")"; }
    }

    // ------------------------------------------------------------ CONDITIONS (provided)
    public bool PlayerSeen() { return canSeePlayer; }
    public bool PlayerInAttackRange() { return DistanceToPlayer() <= attackRange; }
    public bool SeenRecently() { return Time.time - lastSeenTime <= memorySeconds; }
    public bool Tired() { return needsRest; }

    // ------------------------------------------------------------ ACTIONS (provided)
    public Status Patrol()
    {
        Doing_("Patrol", new Color(0.25f, 0.55f, 0.95f));
        if (waypoints == null || waypoints.Length == 0) { StopMoving(); return Status.Failure; }
        Vector2 target = waypoints[waypointIndex].position;
        if (ReachedPoint(target)) waypointIndex = (waypointIndex + 1) % waypoints.Length;
        MoveTowards(waypoints[waypointIndex].position, patrolSpeed);
        return Status.Running;                                               // patrolling never finishes
    }

    public Status ChasePlayer()
    {
        Doing_("Chase", new Color(1f, 0.6f, 0.1f));
        MoveTowards(PlayerPosition, chaseSpeed);
        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.fixedDeltaTime);
        return Status.Running;
    }

    public Status Attack()
    {
        Doing_("Attack", new Color(0.9f, 0.2f, 0.2f));
        StopMoving();
        FacePlayer();
        if (Time.time >= nextHitTime) { DealHit(); nextHitTime = Time.time + attackCooldown; }
        return Status.Running;
    }

    public Status SearchLastSeen()
    {
        Doing_("Search", new Color(0.95f, 0.85f, 0.2f));
        if (ReachedPoint(lastSeenPosition)) { StopMoving(); rb.MoveRotation(rb.rotation + 120f * Time.fixedDeltaTime); }
        else MoveTowards(lastSeenPosition, patrolSpeed);
        return Status.Running;
    }

    public Status Rest()
    {
        Doing_("Rest", new Color(0.65f, 0.65f, 0.65f));
        StopMoving();
        stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.fixedDeltaTime);
        return stamina >= maxStamina ? Status.Success : Status.Running;
    }

    // Expert E3 helpers
    public Status HitPlayer() { Doing_("Attack", new Color(0.9f, 0.2f, 0.2f)); StopMoving(); FacePlayer(); DealHit(); return Status.Success; }
    public Status FacePlayerAndWait() { Doing_("Attack", new Color(0.9f, 0.2f, 0.2f)); StopMoving(); FacePlayer(); return Status.Running; }

    // ============================================================ HELPERS (provided, do not change)
    void Doing_(string what, Color c) { doing = what; if (sr != null) sr.color = c; }

    void Sense()
    {
        canSeePlayer = CanSeePlayer();
        if (canSeePlayer) { lastSeenPosition = player.position; lastSeenTime = Time.time; }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.magnitude > viewDistance) return false;                        // too far
        if (Vector2.Angle(Forward, toPlayer) > fieldOfView * 0.5f) return false;    // outside the view cone
        RaycastHit2D hit = Physics2D.Raycast(rb.position, toPlayer.normalized, viewDistance, playerLayer.value | obstacleLayer.value);
        return hit.collider != null && (hit.collider.transform == player || hit.collider.transform.IsChildOf(player));
    }

    void DealHit()
    {
        HitsOnPlayer++;
        BTPlayerController pc = player != null ? player.GetComponent<BTPlayerController>() : null;
        if (pc != null) pc.TakeHit();
        Debug.Log("[Guard] Hits the player (" + HitsOnPlayer + ")");
    }

    public float DistanceToPlayer() { return player == null ? Mathf.Infinity : Vector2.Distance(rb.position, player.position); }
    public Vector2 PlayerPosition { get { return player == null ? rb.position : (Vector2)player.position; } }
    bool ReachedPoint(Vector2 p) { return Vector2.Distance(rb.position, p) < 0.3f; }

    void MoveTowards(Vector2 target, float speed)
    {
        Vector2 d = target - rb.position;
        if (d.sqrMagnitude < 0.0025f) { StopMoving(); return; }
        Vector2 dir = Steer(d.normalized);
        Turn(dir);
        SetVelocity(dir * speed);
    }

    void StopMoving()
    {
        if (rb == null) return;
        SetVelocity(Vector2.zero);
        rb.angularVelocity = 0f;
    }

    void FacePlayer()
    {
        Vector2 d = PlayerPosition - rb.position;
        if (d.sqrMagnitude > 1e-6f) Turn(d.normalized);
    }

    public void ResetGuard()
    {
        rb.position = startPosition;
        rb.rotation = startRotation;
        transform.SetPositionAndRotation(startPosition, Quaternion.Euler(0f, 0f, startRotation));
        StopMoving();
        stamina = maxStamina;
        needsRest = false;
        HitsOnPlayer = 0;
        waypointIndex = 0;
        lastSeenTime = -999f;
    }

    Vector2 Forward { get { return DirFromAngle(rb != null ? rb.rotation : transform.eulerAngles.z); } }

    static Vector2 DirFromAngle(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(r), Mathf.Cos(r));          // rotation 0 = facing up
    }

    void Turn(Vector2 dir)
    {
        float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        rb.angularVelocity = 0f;
    }

    // Unity 6 renamed Rigidbody2D.velocity to linearVelocity.
    void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
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

    // ---- label above the guard's head (provided) ----
    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.55f);
        if (sp.z < 0f) return;
        float x = sp.x, y = Screen.height - sp.y;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
        GUI.color = Color.black;
        GUI.Label(new Rect(x - 70, y - 34, 140, 20), doing.ToUpper(), style);
        GUI.color = Color.white;
        GUI.Box(new Rect(x - 25, y - 14, 50, 6), GUIContent.none);
        GUI.color = new Color(0.2f, 0.6f, 1f);
        GUI.DrawTexture(new Rect(x - 25, y - 14, 50 * (stamina / maxStamina), 6), Texture2D.whiteTexture);
        GUI.color = Color.white;
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
        if (Application.isPlaying && Time.time - lastSeenTime <= memorySeconds)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(lastSeenPosition, Vector3.one * 0.3f);
        }
    }
}
