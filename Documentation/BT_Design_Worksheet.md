# Lab 05A – Behaviour Tree Design Worksheet

Name: ______________________   Date: ____________

## 1. Behaviours as IF … THEN … rules

Write one rule per behaviour. Then number them in priority order (1 = most important).

| Priority | IF (condition or conditions) | THEN (action) |
|---|---|---|
| | the player is seen and is close enough | |
| | | chase the player |
| | the player was seen a few seconds ago | |
| | (otherwise) | |

## 2. Draw the tree

Turn the table into a tree: one **Selector** at the top, one **Sequence** per rule (conditions first,
then the action), in priority order from left to right. Use: `?` Selector, `->` Sequence,
`( )` Condition, `[ ]` Action.

```
? Guard
├──
├──
├──
└──
```

## 3. Trace it by hand

For each situation, write the status of every node (S, F, R, or - for not ticked) and what the guard does.

| Situation | Attack | Chase | Search | Patrol | Guard does |
|---|---|---|---|---|---|
| Player far away, never seen | | | | | |
| Player visible, 3 m away | | | | | |
| Player visible, 1 m away | | | | | |
| Player hid 2 s ago | | | | | |
| Player hid 10 s ago | | | | | |

## 4. Test log

| Test | Expected | What happened | Pass? |
|---|---|---|---|
| T1 Patrol | | | |
| T2 Chase | | | |
| T3 Hide (before TODO 5) | | | |
| T4 Attack | | | |
| T5 Out of range | | | |
| T6 Attack below Chase | | | |
| T7 Search | | | |
| T8 Give up after 4 s | | | |
| T9 Interrupt Search | | | |

## 5. Reflection

1. In T6, why did the guard never attack when Attack was below Chase?
2. Why does the guard stop searching the moment it sees you again (T9)? No “Search → Chase” rule was written.
3. How would you add a new behaviour “run away when health is low”? Where would it go, and why?
4. Compare this with your Lab 05 FSM: what did you have to change to add Search there, and here?
