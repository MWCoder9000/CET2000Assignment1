# Lab 05A – Behaviour Trees in Unity (step by step)

**Module:** CET2000 AI and Game Programming · **Lecture:** Behaviour Trees Part 1 · **Time:** about 2 hours
**Unity:** Unity 6 (6.0 LTS up to 6.5)

You will give a guard NPC a **behaviour tree** that makes it patrol, chase you, attack you and search for you.
You write the two core composites (Sequence and Selector) and then build the tree, one branch at a time,
testing in Play mode after every step. A panel on the left of the Game view draws your tree live and
colours every node by the answer it gave on the last tick:

| Colour | Letter | Meaning |
|---|---|---|
| green | S | Success |
| red | F | Failure |
| amber | R | Running |
| grey | - | not ticked this time |

The tree you will end up with:

```
? Guard                              (Selector: try children in order, first one that works wins)
├── -> Attack                        (Sequence: every step must work)
│     ├── ( ) Player seen?
│     ├── ( ) In attack range?
│     └── [ ] Attack
├── -> Chase
│     ├── ( ) Player seen?
│     └── [ ] Chase player
├── -> Search
│     ├── ( ) Seen recently?
│     └── [ ] Search last seen
└── [ ] Patrol
```

---

## Part A – Set up (10 min)

1. **Unzip** `Lab05A_BT_StarterPack.zip` somewhere you can write to (not inside OneDrive if you can avoid it).
2. Open **Unity Hub → Projects → Add → Add project from disk** and choose the `Lab05A_BT_StarterPack` folder.
3. Open it with your **Unity 6** version. If Hub says the project uses a different version (6000.0.23f1),
   choose your version and click **Continue / Upgrade**. This is normal.
4. When the project opens, a dialog asks **“Create the GuardBTScene now?”** Click **Create scene**.
   (Missed it? Use the menu **CET2000 → Create Lab05A Behaviour Tree Scene**.)
5. Press **Play**. The guard (grey circle with a nose) should walk round the four blue waypoints.
   Click the Game view and move the green player with **W A S D** or the arrow keys.

✅ **Checkpoint A:** the guard patrols, the panel shows one node: `[R] [ ] Patrol`.

Files you will edit:

| File | What you do |
|---|---|
| `Assets/Scripts/BehaviourTree.cs` | TODO 1 Sequence, TODO 2 Selector (Expert E2 Invert, E3 Cooldown) |
| `Assets/Scripts/GuardBT.cs` | TODO 3–5: build the tree inside `BuildTree()` (Expert E1, E3) |

Everything else (movement, vision, the panel, the player) is provided.

---

## Part B – Design on paper first (10 min)

Open `Documentation/BT_Design_Worksheet.md`. Write the guard's behaviours as **IF … THEN …** rules
and put them in priority order. The IF part becomes a **Condition**, the THEN part becomes an **Action**,
each rule becomes a **Sequence**, and the list becomes a **Selector**.

---

## Part C – Write the composites (25 min)

Open `Assets/Scripts/BehaviourTree.cs`. Every node has `Tick()`, which returns `Status.Success`,
`Status.Failure` or `Status.Running`. A composite keeps its children in the list `Children`.

### TODO 1 – Sequence (AND)

A Sequence ticks its children from left to right and **stops at the first child that does not succeed**.

```csharp
protected override Status OnTick()
{
    foreach (Node child in Children)
    {
        Status s = child.Tick();
        if (s != Status.Success) return s;     // Failure or Running: stop here
    }
    return Status.Success;                     // every child succeeded
}
```

### TODO 2 – Selector (OR)

A Selector ticks its children from left to right and **stops at the first child that does not fail**.
Try writing it yourself first: it is the Sequence with two words changed.

<details><summary>Show the answer</summary>

```csharp
protected override Status OnTick()
{
    foreach (Node child in Children)
    {
        Status s = child.Tick();
        if (s != Status.Failure) return s;     // Success or Running: stop here
    }
    return Status.Failure;                     // every child failed
}
```
</details>

✅ **Checkpoint C:** the project compiles (no red errors in the Console). Nothing changes in Play mode yet,
because the tree is still a single Patrol action.

---

## Part D – Build the tree (40 min)

Open `Assets/Scripts/GuardBT.cs` and find `BuildTree()`. The conditions and actions are already written
for you (`PlayerSeen`, `PlayerInAttackRange`, `SeenRecently`, `Patrol`, `ChasePlayer`, `Attack`,
`SearchLastSeen`). You only plug them together.

### TODO 3 – Chase or Patrol

Replace `return new ActionNode("Patrol", Patrol);` with:

```csharp
return new Selector("Guard",
    new Sequence("Chase",
        new Condition("Player seen?", PlayerSeen),
        new ActionNode("Chase player", ChasePlayer)),
    new ActionNode("Patrol", Patrol));
```

Press **Play** and try it:

| Test | Do this | You should see |
|---|---|---|
| T1 | Stay out of the guard’s view | Blue guard patrols. Panel: `Player seen?` **F**, `Chase player` grey, `Patrol` **R** |
| T2 | Walk in front of the guard | Orange guard chases you. `Player seen?` **S**, `Chase player` **R**, `Patrol` grey |
| T3 | Hide behind the centre block | It goes straight back to patrolling (it has no memory yet) |

> **Stuck?** If the guard stands still and the panel shows everything red, TODO 1 or TODO 2 still returns
> `Status.Failure` without ticking the children.

### TODO 4 – Attack (highest priority)

Add an **Attack** Sequence as the **first** child of the Selector (before Chase):

```csharp
new Sequence("Attack",
    new Condition("Player seen?", PlayerSeen),
    new Condition("In attack range?", PlayerInAttackRange),
    new ActionNode("Attack", Attack)),
```

| Test | Do this | You should see |
|---|---|---|
| T4 | Let the guard catch you | Red guard stops and hits you about every 0.8 s (Hits counter goes up, you flash red) |
| T5 | Move just out of range | It switches back to Chase at once |
| T6 | **Experiment:** move Attack *below* Chase, press Play | The guard never attacks. Why? (Chase is checked first and always wins.) Put it back. |

### TODO 5 – Search (memory)

Add a **Search** Sequence **between Chase and Patrol**:

```csharp
new Sequence("Search",
    new Condition("Seen recently?", SeenRecently),
    new ActionNode("Search last seen", SearchLastSeen)),
```

| Test | Do this | You should see |
|---|---|---|
| T7 | Get chased, then hide behind the block | Yellow guard walks to where it last saw you and looks around |
| T8 | Stay hidden for 4 seconds | It gives up and patrols again |
| T9 | While it is searching, step back into view | It chases at once: the higher-priority branch interrupts Search |

✅ **Checkpoint D:** all nine tests behave as described. Take a screenshot of the panel during T7.

---

## Part E – Expert tasks (if you finish early)

**E1 – Rest when tired.** The guard loses stamina while chasing (blue bar above its head). Add a branch
`Sequence("Rest", Condition("Tired?", Tired), ActionNode("Rest", Rest))`. Where must it go so that a tired
guard stops chasing, but still attacks if you walk right up to it? (Answer: between Attack and Chase.)

**E2 – Invert decorator.** In `BehaviourTree.cs`, finish `Invert` so it flips Success and Failure
(Running stays Running). Then make the Search rule explicit:
`new Invert("Not seen?", new Condition("Player seen?", PlayerSeen))` as the first child of Search.
Does the behaviour change? Why not? (Chase already handles “seen”, so Search only runs when not seen.)

**E3 – Cooldown decorator.** Finish `Cooldown`: if `Time.time < readyAt` return Failure **without** ticking
the child; otherwise tick it and, if it succeeded, set `readyAt = Time.time + seconds`. Then replace the
`Attack` action with the small tree given in the comment under `BuildTree()`. The guard should still hit
every 0.8 s, but now the timing lives in the tree, not in the action.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| The scene is empty / no guard | Menu **CET2000 → Create Lab05A Behaviour Tree Scene** |
| Keys do nothing | Click inside the Game view first |
| Guard frozen, every node red | TODO 1 or TODO 2 not finished (still `return Status.Failure;`) |
| Red error “; expected” or “) expected” | Check commas between children and the brackets at the end of the Selector |
| Guard never attacks | Attack must be the **first** child of the Selector |
| Search never happens | Search must come **after** Chase and **before** Patrol |
| Hub warns about the editor version | Pick your Unity 6 version and click Continue / Upgrade |

## Deliverables

1. Your completed `BehaviourTree.cs` and `GuardBT.cs`.
2. The completed design worksheet, including your answers to T6 and the reflection questions.
3. A screenshot of the panel during test T7 (Search running).
4. A short demo in the lab: show T2, T4 and T9.
