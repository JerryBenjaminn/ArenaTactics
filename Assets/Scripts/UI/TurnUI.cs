using UnityEngine;

/// <summary>
/// Simple UI for turn controls during player turns.
/// </summary>
public class TurnUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentBattleState != BattleManager.BattleState.PlayerTurn)
        {
            return;
        }

        PlayerInputController input = PlayerInputController.Instance;
        if (input == null)
        {
            return;
        }

        Gladiator gladiator = input.SelectedGladiator;

        GUILayout.BeginArea(new Rect(10f, 420f, 300f, 220f), GUI.skin.box);
        GUILayout.Label("Player Turn");

        if (gladiator != null)
        {
            string name = gladiator.Data != null ? gladiator.Data.gladiatorName : gladiator.name;
            GUILayout.Label($"{name}'s Turn");
            GUILayout.Label($"MP: {gladiator.RemainingMP}/{gladiator.MaxMP}  AP: {gladiator.RemainingAP}/{gladiator.MaxAP}");
        }
        else
        {
            GUILayout.Label("No active gladiator.");
        }

        GUILayout.Space(10f);
        GUI.enabled = input.HasMovedThisTurn && !input.HasAttackedThisTurn;
        if (GUILayout.Button("Undo Move"))
        {
            input.UndoMove();
        }

        GUI.enabled = true;
        if (GUILayout.Button("End Turn"))
        {
            input.EndTurn();
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }
}
