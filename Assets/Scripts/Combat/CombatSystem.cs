using UnityEngine;

/// <summary>
/// Utility methods for combat calculations.
/// </summary>
public static class CombatSystem
{
    /// <summary>
    /// Calculates damage dealt by the attacker to the defender.
    /// </summary>
    public static int CalculateDamage(Gladiator attacker, Gladiator defender, out bool didCrit, out bool didMiss)
    {
        didCrit = false;
        didMiss = false;

        if (attacker == null || defender == null)
        {
            return 0;
        }

        float hitChance = attacker.GetAccuracy() - defender.GetDodgeChance();
        hitChance = Mathf.Clamp(hitChance, 0.05f, 0.99f);

        float hitRoll = Random.value;
        if (hitRoll > hitChance)
        {
            didMiss = true;
            if (DebugSettings.LOG_COMBAT)
            {
                Debug.Log($"CombatSystem: {attacker.name} MISSED {defender.name} (needed {hitChance:P0}, rolled {hitRoll:P0})");
            }
            return 0;
        }

        int attack = attacker.GetTotalAttack();
        int defense = defender.GetTotalDefense();

        float critChance = attacker.GetCritChance();
        float critRoll = Random.value;
        if (critRoll <= critChance)
        {
            didCrit = true;
            attack = Mathf.RoundToInt(attack * 1.5f);
            if (DebugSettings.LOG_COMBAT)
            {
                Debug.Log($"CombatSystem: CRITICAL HIT! {attacker.name} -> {defender.name}");
            }
        }

        int finalDamage = Mathf.Max(1, attack - defense);

        if (DebugSettings.LOG_COMBAT)
        {
            Debug.Log($"CombatSystem: {attacker.name} -> {defender.name}: {finalDamage} damage (Attack: {attack}, Defense: {defense}, Crit: {didCrit})");
        }
        return finalDamage;
    }

    /// <summary>
    /// Checks if the target is within the attacker's range.
    /// </summary>
    public static bool IsInRange(Gladiator attacker, Gladiator target)
    {
        if (attacker == null || target == null)
        {
            return false;
        }

        Vector2Int from = attacker.CurrentGridPosition;
        Vector2Int to = target.CurrentGridPosition;
        int distance = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        return distance <= attacker.GetAttackRange();
    }

    /// <summary>
    /// Checks if there is a clear line of sight between two grid positions.
    /// </summary>
    public static bool HasLineOfSight(Vector2Int from, Vector2Int to, GridManager grid)
    {
        if (grid == null)
        {
            return false;
        }

        int x0 = from.x;
        int y0 = from.y;
        int x1 = to.x;
        int y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Vector2Int current = new Vector2Int(x0, y0);
            if (current != from && current != to)
            {
                if (!grid.IsPositionValid(current))
                {
                    return false;
                }

                GridCell cell = grid.GetCellAtPosition(current);
                if (cell == null || !cell.IsWalkable)
                {
                    return false;
                }
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return true;
    }
}
