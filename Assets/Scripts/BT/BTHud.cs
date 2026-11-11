using UnityEngine;

// CET2000 Lab 05A - on-screen panel (provided, no changes needed).
// Draws the guard's behaviour tree and colours every node by the status it returned on the last tick:
//   green = Success   red = Failure   amber = Running   grey = not ticked this time
public class BTHud : MonoBehaviour
{
    public GuardBT guard;
    public BTPlayerController player;

    GUIStyle box, title, line;
    static readonly Color Green = new Color(0.12f, 0.55f, 0.3f), Red = new Color(0.75f, 0.2f, 0.15f),
                          Amber = new Color(0.85f, 0.55f, 0f), Grey = new Color(0.6f, 0.62f, 0.66f);

    void OnGUI()
    {
        if (guard == null || guard.Root == null) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            title = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            line = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, richText = true };
        }
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        GUILayout.BeginVertical(box);
        GUILayout.Label("Guard is doing: " + guard.Doing, title);
        GUILayout.Label(string.Format("Can see player: {0}    Hits: {1}    Stamina: {2:0}",
            guard.CanSee ? "yes" : "no", guard.HitsOnPlayer, guard.stamina), GUI.skin.label);
        GUILayout.Space(4);
        DrawNode(guard.Root, 0);
        GUILayout.Space(4);
        GUILayout.Label("<color=#1F8C4D>S</color> success   <color=#BF3326>F</color> failure   <color=#D98C00>R</color> running   <color=#999EA8>grey</color> not ticked", line);
        GUILayout.EndVertical();

        GUILayout.Space(6);
        GUILayout.BeginVertical(box);
        if (player != null) player.autopilot = GUILayout.Toggle(player.autopilot, " Player autopilot (wanders by itself)");
        if (GUILayout.Button("Reset guard and player"))
        {
            guard.ResetGuard();
            if (player != null) player.ResetPlayer();
        }
        GUILayout.Label("Move: W A S D / arrows (click the Game view first)");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void DrawNode(Node n, int depth)
    {
        bool ticked = n.TickedThisTime;
        Color c = !ticked ? Grey : n.LastStatus == Status.Success ? Green : n.LastStatus == Status.Failure ? Red : Amber;
        string letter = !ticked ? "-" : n.LastStatus == Status.Success ? "S" : n.LastStatus == Status.Failure ? "F" : "R";
        Color old = GUI.contentColor;
        GUI.contentColor = c;
        GUILayout.Label(new string(' ', depth * 4) + "[" + letter + "] " + n.Symbol + " " + n.Name, line);
        GUI.contentColor = old;
        foreach (Node child in n.Children) DrawNode(child, depth + 1);
    }
}
