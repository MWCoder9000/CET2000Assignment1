using UnityEngine;

// CET2000 Lab 05 - Expert E3: the same FSM written as one class per state (the State pattern).
// *** SOLUTION *** Tick "Use State Classes" on the guard to run this version instead of the switch.

public interface IGuardState
{
    GuardState Id { get; }
    void Enter(GuardFSM_Template g);
    void Tick(GuardFSM_Template g, float dt);
    void Exit(GuardFSM_Template g);
}

// One object per state for each guard.
public class GuardStateSet
{
    public readonly IGuardState Idle = new IdleState();
    public readonly IGuardState Patrol = new PatrolState();
    public readonly IGuardState Chase = new ChaseState();
    public readonly IGuardState Attack = new AttackState();
    public readonly IGuardState Flee = new FleeState();
}

public class IdleState : IGuardState
{
    public GuardState Id => GuardState.Idle;
    public void Enter(GuardFSM_Template g) { g.SetColour(new Color(0.65f, 0.65f, 0.65f)); g.StopMoving(); }
    public void Tick(GuardFSM_Template g, float dt)
    {
        g.StopMoving();
        g.Regenerate(dt);
        if (g.CanSee) g.SetState(g.S.Chase);
        else if (g.TimeInState >= g.idleTime) g.SetState(g.S.Patrol);
    }
    public void Exit(GuardFSM_Template g) { }
}

public class PatrolState : IGuardState
{
    public GuardState Id => GuardState.Patrol;
    public void Enter(GuardFSM_Template g) => g.SetColour(new Color(0.25f, 0.55f, 0.95f));
    public void Tick(GuardFSM_Template g, float dt)
    {
        g.PatrolWaypoints(g.patrolSpeed);
        if (g.CanSee) g.SetState(g.S.Chase);
    }
    public void Exit(GuardFSM_Template g) { }
}

public class ChaseState : IGuardState
{
    public GuardState Id => GuardState.Chase;
    public void Enter(GuardFSM_Template g) { g.SetColour(new Color(1f, 0.6f, 0.1f)); g.Shout("Hey!"); }
    public void Tick(GuardFSM_Template g, float dt)
    {
        g.MoveTowards(g.PlayerPosition, g.chaseSpeed);
        if (g.health < g.fleeHealth) g.SetState(g.S.Flee);
        else if (g.DistanceToPlayer() <= g.attackRange) g.SetState(g.S.Attack);
        else if (!g.CanSee) g.SetState(g.S.Patrol);
    }
    public void Exit(GuardFSM_Template g) { }
}

public class AttackState : IGuardState
{
    float cooldown;
    public GuardState Id => GuardState.Attack;
    public void Enter(GuardFSM_Template g) { g.SetColour(new Color(0.9f, 0.2f, 0.2f)); cooldown = 0.2f; }
    public void Tick(GuardFSM_Template g, float dt)
    {
        g.StopMoving();
        g.FacePlayer();
        cooldown -= dt;
        if (cooldown <= 0f) { g.HitPlayer(); cooldown = g.attackCooldown; }
        if (g.health < g.fleeHealth) g.SetState(g.S.Flee);
        else if (g.DistanceToPlayer() > g.attackExitRange) g.SetState(g.S.Chase);
    }
    public void Exit(GuardFSM_Template g) => g.StopMoving();
}

public class FleeState : IGuardState
{
    float safe;
    public GuardState Id => GuardState.Flee;
    public void Enter(GuardFSM_Template g) { g.SetColour(new Color(0.6f, 0.35f, 0.85f)); safe = 0f; g.Shout("Retreat!"); }
    public void Tick(GuardFSM_Template g, float dt)
    {
        g.MoveAwayFrom(g.PlayerPosition, g.fleeSpeed);
        g.Regenerate(dt);
        safe = g.DistanceToPlayer() >= g.safeDistance ? safe + dt : 0f;
        if (safe >= g.safeTime) g.SetState(g.S.Idle);
    }
    public void Exit(GuardFSM_Template g) { }
}
