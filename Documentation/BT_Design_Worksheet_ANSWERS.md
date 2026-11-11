# Lab 05A – Worksheet model answers (staff only)

## 1. Rules in priority order

| Priority | IF | THEN |
|---|---|---|
| 1 | the player is seen AND in attack range | attack |
| 2 | the player is seen | chase the player |
| 3 | the player was seen in the last 4 s | go to where it was last seen and look around |
| 4 | otherwise | patrol |

(With Expert E1: “IF tired THEN rest” goes between 1 and 2.)

## 2. Tree

```
? Guard
├── -> Attack:  ( ) Player seen?   ( ) In attack range?   [ ] Attack
├── -> Chase:   ( ) Player seen?   [ ] Chase player
├── -> Search:  ( ) Seen recently? [ ] Search last seen
└── [ ] Patrol
```

## 3. Trace

| Situation | Attack | Chase | Search | Patrol | Guard does |
|---|---|---|---|---|---|
| Player far away, never seen | F (seen? F) | F | F (recent? F) | R | Patrols |
| Player visible, 3 m away | F (range? F) | R | - | - | Chases |
| Player visible, 1 m away | R | - | - | - | Attacks |
| Player hid 2 s ago | F | F | R | - | Searches last seen spot |
| Player hid 10 s ago | F | F | F | R | Patrols |

## 4. Reflection

1. A Selector stops at the first child that does not fail. Whenever the player is seen, Chase succeeds its
   condition and is Running, so the Selector stops there and never reaches Attack. Order = priority.
2. The tree is ticked from the root every physics step. As soon as “Player seen?” is true, Chase (which is
   to the left of Search) is Running, so Search is simply not ticked. Interruption comes from the priority
   order; no transition is needed.
3. Add `Sequence("Flee", Condition("Health low?"), ActionNode("Run away"))` as the FIRST child of the root
   Selector, because staying alive is more important than attacking. Nothing else in the tree changes.
4. FSM: a new Search state plus new transitions Chase → Search, Search → Chase, Search → Patrol (and the
   rules in each state). BT: one new branch inserted at the right place in the Selector.

## Expert answers

- E1: Rest goes between Attack and Chase. A tired guard still attacks a player who walks right up to it,
  but stops chasing until stamina is full again.
- E2: no visible change. Search only gets ticked when Chase has failed, i.e. when the player is not seen,
  so “Not seen?” is always true there. Invert makes the rule explicit and keeps Search correct if someone
  later moves it above Chase.
- E3: between hits the Cooldown returns Failure, so the inner Selector falls through to “Face player”
  (Running) and the guard keeps facing the player.
