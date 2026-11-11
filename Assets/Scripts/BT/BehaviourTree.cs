using System.Collections.Generic;
using UnityEngine;

// CET2000 Lab 05A - a tiny behaviour tree engine.   *** SOLUTION ***
// Every node has Tick(), which returns Success, Failure or Running.
// The composites are "reactive": they start again from the first child on every tick,
// so a higher-priority branch (further left) takes over the moment its conditions become true.

public enum Status { Success, Failure, Running }

public abstract class Node
{
    public static int CurrentTick;                 // set by the guard before each tick (used by the panel)

    public string Name;
    public Status LastStatus { get; private set; }
    public int LastTick { get; private set; }
    public readonly List<Node> Children = new List<Node>();
    public virtual string Symbol { get { return ""; } }

    protected Node(string name) { Name = name; LastTick = -1; }

    public Status Tick()
    {
        LastStatus = OnTick();
        LastTick = CurrentTick;
        return LastStatus;
    }

    public bool TickedThisTime { get { return LastTick == CurrentTick; } }

    protected abstract Status OnTick();
}

// ------------------------------------------------------------------ COMPOSITES

// SEQUENCE (AND): run the children from left to right.
//   A child fails    -> the Sequence fails at once (stop).
//   A child running  -> the Sequence is running (stop, carry on next tick).
//   All succeed      -> the Sequence succeeds.
public class Sequence : Node
{
    public override string Symbol { get { return "->"; } }
    public Sequence(string name, params Node[] kids) : base(name) { Children.AddRange(kids); }

    protected override Status OnTick()
    {
        foreach (Node child in Children)
        {
            Status s = child.Tick();
            if (s != Status.Success) return s;     // Failure or Running: stop here
        }
        return Status.Success;                     // every child succeeded
    }
}

// SELECTOR (OR): try the children from left to right. The order is the priority.
//   A child succeeds -> the Selector succeeds at once (stop).
//   A child running  -> the Selector is running (stop).
//   All fail         -> the Selector fails.
public class Selector : Node
{
    public override string Symbol { get { return "?"; } }
    public Selector(string name, params Node[] kids) : base(name) { Children.AddRange(kids); }

    protected override Status OnTick()
    {
        foreach (Node child in Children)
        {
            Status s = child.Tick();
            if (s != Status.Failure) return s;     // Success or Running: stop here
        }
        return Status.Failure;                     // every child failed
    }
}

// ------------------------------------------------------------------ LEAVES (provided)

// CONDITION: a quick yes/no question. Success = yes, Failure = no. Never Running.
public class Condition : Node
{
    readonly System.Func<bool> test;
    public override string Symbol { get { return "( )"; } }
    public Condition(string name, System.Func<bool> test) : base(name) { this.test = test; }
    protected override Status OnTick() { return test() ? Status.Success : Status.Failure; }
}

// ACTION: does something in the game. Returns Running while busy.
public class ActionNode : Node
{
    readonly System.Func<Status> act;
    public override string Symbol { get { return "[ ]"; } }
    public ActionNode(string name, System.Func<Status> act) : base(name) { this.act = act; }
    protected override Status OnTick() { return act(); }
}

// ------------------------------------------------------------------ DECORATORS (Expert tasks)

// INVERT: flips the child's answer. Success <-> Failure. Running stays Running.
public class Invert : Node
{
    public override string Symbol { get { return "NOT"; } }
    public Invert(string name, Node child) : base(name) { Children.Add(child); }

    protected override Status OnTick()
    {
        Status s = Children[0].Tick();
        if (s == Status.Success) return Status.Failure;
        if (s == Status.Failure) return Status.Success;
        return Status.Running;
    }
}

// COOLDOWN: after the child succeeds, block it (return Failure) for 'seconds'.
#pragma warning disable 0414, 0169   // (stops 'unused field' warnings before Expert E3 is done)
public class Cooldown : Node
{
    readonly float seconds;
    float readyAt;
    public override string Symbol { get { return "CD"; } }
    public Cooldown(string name, float seconds, Node child) : base(name) { this.seconds = seconds; Children.Add(child); }

    protected override Status OnTick()
    {
        if (Time.time < readyAt) return Status.Failure;    // still cooling down: do not tick the child
        Status s = Children[0].Tick();
        if (s == Status.Success) readyAt = Time.time + seconds;
        return s;
    }
}
#pragma warning restore 0414, 0169
