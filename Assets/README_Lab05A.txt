CET2000 – Lab 05A SOLUTION: Behaviour Trees (Unity 6.0 to 6.5)
==============================================================

Quick start
1. Unity Hub -> Add -> Add project from disk -> this folder. Open with any Unity 6 version
   (6.0 LTS up to 6.5). If Hub asks to upgrade the project, click Continue / Upgrade.
2. Click "Create scene" in the CET2000 dialog, or use CET2000 -> Create Lab05A Behaviour Tree Scene.
3. Press Play: the guard runs the completed tree (all TODOs and Expert E1-E3 done).

Scripts
  BehaviourTree.cs   the behaviour tree engine: Sequence (TODO 1), Selector (TODO 2),
                     Condition, ActionNode, Invert (Expert E2), Cooldown (Expert E3)
  GuardBT.cs         the guard: build its tree in BuildTree() (TODO 3, 4, 5, Expert E1, E3)
  PlayerController.cs  the player (provided): W A S D / arrows, autopilot
  BTHud.cs           panel (provided): draws the tree, coloured by status every tick
  Editor/SetupLab05A.cs  builds the scene, tags, layers and prefabs

Documentation
  Lab05A_Instructions.md     step-by-step guide (Parts A to E, tests T1 to T9)
  BT_Design_Worksheet.md     rules, tree sketch, hand trace, test log, reflection
  BT_Design_Worksheet_ANSWERS.md  model answers (staff only)

Compatibility
  Written for Unity 6.0 LTS to 6.5 (saved as 6000.0.23f1). Keyboard input is read through OnGUI
  events, so it works with both the old Input Manager and the new Input System. The scripts use
  plain C# and a version check for Rigidbody2D.linearVelocity, so they should also compile in older
  Unity versions (2019-2023), but the lab is supported on Unity 6.
