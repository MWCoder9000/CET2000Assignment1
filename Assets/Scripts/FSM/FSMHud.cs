using System.Collections.Generic;
using UnityEngine;

// CET2000 Lab 05 - on-screen panel (provided, no changes needed).
// Shows the guard's state, health and the last state changes, plus test buttons.
public class FSMHud : MonoBehaviour
{
    public GuardFSM_Template guard;
    public FSMPlayerController player;

    readonly List<string> history = new List<string>();
    GUIStyle box, title, line;

    void OnEnable() { if (guard != null) guard.StateChanged += Record; }
    void OnDisable() { if (guard != null) guard.StateChanged -= Record; }

    void Record(GuardState from, GuardState to)
    {
        history.Insert(0, $"{Time.timeSinceLevelLoad,6:0.0}s  {from} -> {to}");
        if (history.Count > 8) history.RemoveAt(history.Count - 1);
    }

    void OnGUI()
    {
        if (guard == null) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            title = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            line = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        }
        GUILayout.BeginArea(new Rect(10, 10, 290, Screen.height - 20));
        GUILayout.BeginVertical(box);
        GUILayout.Label($"Guard: {guard.State}  ({guard.TimeInState:0.0} s)", title);
        GUILayout.Label($"Health: {guard.health:0} / {guard.maxHealth:0}    Can see player: {(guard.CanSee ? "yes" : "no")}", line);
        GUILayout.Label($"Distance to player: {guard.DistanceToPlayer():0.0} m    Hits on player: {guard.HitsOnPlayer}", line);
        GUILayout.Space(4);
        GUILayout.Label("Last state changes:", line);
        foreach (string h in history) GUILayout.Label("  " + h, line);
        GUILayout.EndVertical();

        GUILayout.Space(6);
        GUILayout.BeginVertical(box);
        if (player != null)
        {
            if (GUILayout.Button($"Hit guard (-{player.hitDamage:0})   [Space when close]")) guard.TakeDamage(player.hitDamage);
            player.autopilot = GUILayout.Toggle(player.autopilot, " Player autopilot (wanders by itself)");
        }
        if (GUILayout.Button("Heal guard to full")) guard.health = guard.maxHealth;
        guard.useSearchState = GUILayout.Toggle(guard.useSearchState, " Expert E1: use Search state");
        if (GUILayout.Button("Reset guard and player"))
        {
            guard.ResetGuard();
            if (player != null) player.ResetPlayer();
            history.Clear();
        }
        GUILayout.Label("Move: W A S D / arrows (click the Game view first)", line);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}