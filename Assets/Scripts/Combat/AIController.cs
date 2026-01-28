using System.Collections;
using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;

/// <summary>
/// Simple utility class for AI decision making.
/// </summary>
public static class AIController
{
    /// <summary>
    /// Executes the full AI turn for the provided gladiator.
    /// </summary>
    public static IEnumerator ExecuteAITurn(Gladiator aiGladiator)
    {
        if (aiGladiator == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        bool hasSpellsAvailable = HasAnyCastableSpells(aiGladiator);
        if (TryCastBestSpell(aiGladiator))
        {
            yield return new WaitForSeconds(0.2f);
            EndAITurn();
            yield break;
        }

        if (hasSpellsAvailable)
        {
            Gladiator nearestEnemyForSpells = FindNearestEnemy(aiGladiator);
            if (nearestEnemyForSpells != null)
            {
                yield return MoveTowardsTarget(aiGladiator, nearestEnemyForSpells);
            }

            if (TryCastBestSpell(aiGladiator))
            {
                yield return new WaitForSeconds(0.2f);
                EndAITurn();
                yield break;
            }
        }

        List<Gladiator> attackableTargets = aiGladiator.GetAttackableTargets();
        if (attackableTargets.Count > 0 && aiGladiator.RemainingAP > 0 && aiGladiator.CanBasicAttack())
        {
            Gladiator target = SelectAttackTarget(attackableTargets, aiGladiator);
            if (target != null)
            {
                yield return AttackTarget(aiGladiator, target);
                EndAITurn();
                yield break;
            }
        }

        Gladiator nearestEnemy = FindNearestEnemy(aiGladiator);
        if (nearestEnemy != null)
        {
            yield return MoveTowardsTarget(aiGladiator, nearestEnemy);
        }

        attackableTargets = aiGladiator.GetAttackableTargets();
        if (attackableTargets.Count > 0 && aiGladiator.RemainingAP > 0 && aiGladiator.CanBasicAttack())
        {
            Gladiator target = SelectAttackTarget(attackableTargets, aiGladiator);
            if (target != null)
            {
                yield return AttackTarget(aiGladiator, target);
            }
        }

        yield return new WaitForSeconds(0.3f);
        EndAITurn();
    }

    private static bool HasAnyCastableSpells(Gladiator caster)
    {
        if (caster == null || caster.KnownSpells == null)
        {
            return false;
        }

        foreach (SpellData spell in caster.KnownSpells)
        {
            if (spell == null)
            {
                continue;
            }

            if (caster.CanCastSpell(spell))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryCastBestSpell(Gladiator caster)
    {
        if (caster == null || caster.KnownSpells == null || caster.KnownSpells.Count == 0)
        {
            return false;
        }

        if (caster.RemainingAP <= 0 || caster.CurrentSpellSlots <= 0)
        {
            return false;
        }

        List<Gladiator> allGladiators = BattleManager.Instance != null
            ? BattleManager.Instance.AllGladiators
            : null;
        if (allGladiators == null)
        {
            return false;
        }

        SpellData aoeSpell = null;
        Gladiator bestAoeTarget = null;
        int bestAoeCount = 1;

        foreach (SpellData spell in caster.KnownSpells)
        {
            if (spell == null)
            {
                continue;
            }

            if (!caster.CanCastSpell(spell))
            {
                continue;
            }

            if (spell.spellType == SpellType.AOE && spell.aoeRadius > 0)
            {
                foreach (Gladiator enemy in allGladiators)
                {
                    if (enemy == null || enemy.Data == null || enemy.Data.team == caster.Data.team)
                    {
                        continue;
                    }

                    int distance = GetGridDistance(caster.CurrentGridPosition, enemy.CurrentGridPosition);
                    if (distance > spell.range)
                    {
                        continue;
                    }

                    if (spell.requiresLineOfSight &&
                        !CombatSystem.HasLineOfSight(caster.CurrentGridPosition, enemy.CurrentGridPosition, GridManager.Instance))
                    {
                        continue;
                    }

                    int count = CountEnemiesInRadius(allGladiators, caster, enemy.CurrentGridPosition, spell.aoeRadius);
                    if (count > bestAoeCount)
                    {
                        bestAoeCount = count;
                        aoeSpell = spell;
                        bestAoeTarget = enemy;
                    }
                }
            }
        }

        if (aoeSpell != null && bestAoeTarget != null && GridManager.Instance != null)
        {
            GridCell cell = GridManager.Instance.GetCellAtPosition(bestAoeTarget.CurrentGridPosition);
            if (cell != null && caster.CastSpellAOE(aoeSpell, cell))
            {
                return true;
            }
        }

        foreach (SpellData spell in caster.KnownSpells)
        {
            if (spell == null)
            {
                continue;
            }

            if (!caster.CanCastSpell(spell))
            {
                continue;
            }

            if (spell.spellType == SpellType.Buff)
            {
                Gladiator target = SelectBuffTarget(allGladiators, caster);
                if (target != null && caster.CastSpell(spell, target))
                {
                    return true;
                }
            }
            else if (spell.spellType == SpellType.Damage || spell.spellType == SpellType.Debuff)
            {
                List<Gladiator> enemies = allGladiators
                    .FindAll(g => g != null && g.Data != null && g.Data.team != caster.Data.team);
                Gladiator target = SelectAttackTarget(enemies, caster);
                if (target != null && caster.CastSpell(spell, target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Gladiator SelectBuffTarget(List<Gladiator> allGladiators, Gladiator caster)
    {
        Gladiator best = caster;
        float lowestHpPercent = caster.MaxHP > 0 ? caster.CurrentHP / (float)caster.MaxHP : 1f;

        foreach (Gladiator gladiator in allGladiators)
        {
            if (gladiator == null || gladiator.Data == null || gladiator.Data.team != caster.Data.team)
            {
                continue;
            }

            float hpPercent = gladiator.MaxHP > 0 ? gladiator.CurrentHP / (float)gladiator.MaxHP : 1f;
            if (hpPercent < lowestHpPercent)
            {
                lowestHpPercent = hpPercent;
                best = gladiator;
            }
        }

        return best;
    }

    private static int CountEnemiesInRadius(List<Gladiator> allGladiators, Gladiator caster, Vector2Int center, int radius)
    {
        int count = 0;
        foreach (Gladiator gladiator in allGladiators)
        {
            if (gladiator == null || gladiator.Data == null || gladiator.Data.team == caster.Data.team)
            {
                continue;
            }

            int distance = GetGridDistance(gladiator.CurrentGridPosition, center);
            if (distance <= radius)
            {
                count++;
            }
        }

        return count;
    }

    private static void EndAITurn()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.EndTurn();
        }
    }

    private static Gladiator SelectAttackTarget(List<Gladiator> targets, Gladiator attacker)
    {
        Gladiator best = null;
        int lowestHp = int.MaxValue;
        int bestDistance = int.MaxValue;

        foreach (Gladiator target in targets)
        {
            if (target == null || target.CurrentHP <= 0)
            {
                continue;
            }

            int hp = target.CurrentHP;
            int distance = GetGridDistance(attacker.CurrentGridPosition, target.CurrentGridPosition);

            if (hp < lowestHp || (hp == lowestHp && distance < bestDistance))
            {
                lowestHp = hp;
                bestDistance = distance;
                best = target;
            }
        }

        return best;
    }

    private static Gladiator FindNearestEnemy(Gladiator aiGladiator)
    {
        if (BattleManager.Instance == null || aiGladiator == null || aiGladiator.Data == null)
        {
            return null;
        }

        Team myTeam = aiGladiator.Data.team;
        Gladiator nearest = null;
        int bestDistance = int.MaxValue;

        foreach (Gladiator gladiator in BattleManager.Instance.AllGladiators)
        {
            if (gladiator == null || gladiator.CurrentHP <= 0 || gladiator.Data == null)
            {
                continue;
            }

            if (gladiator.Data.team == myTeam)
            {
                continue;
            }

            int distance = GetGridDistance(aiGladiator.CurrentGridPosition, gladiator.CurrentGridPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = gladiator;
            }
        }

        return nearest;
    }

    private static IEnumerator MoveTowardsTarget(Gladiator aiGladiator, Gladiator target)
    {
        if (aiGladiator == null || target == null)
        {
            yield break;
        }

        if (aiGladiator.RemainingMP <= 0)
        {
            yield break;
        }

        Vector2Int bestMove = FindBestMoveTowards(aiGladiator, target.CurrentGridPosition);
        int cost = GetGridDistance(aiGladiator.CurrentGridPosition, bestMove);
        if (cost <= 0)
        {
            yield break;
        }

        if (!aiGladiator.TrySpendMP(cost))
        {
            yield break;
        }

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"AIController: {aiGladiator.name} moving towards {target.name}.");
        }
        aiGladiator.MoveToSmooth(bestMove);

        while (aiGladiator.IsMoving)
        {
            yield return null;
        }
    }

    private static IEnumerator AttackTarget(Gladiator attacker, Gladiator target)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        if (attacker.RemainingAP <= 0)
        {
            yield break;
        }

        bool didCrit;
        bool didMiss;
        int damage = CombatSystem.CalculateDamage(attacker, target, out didCrit, out didMiss);
        if (damage > 0)
        {
            target.TakeDamage(damage, attacker);
        }
        attacker.TrySpendAP(1);

        if (DebugSettings.LOG_COMBAT)
        {
            string result = didMiss ? "missed" : $"hit for {damage} damage";
            string critNote = didCrit ? " (CRIT)" : string.Empty;
            Debug.Log($"AIController: {attacker.name} attacked {target.name} and {result}{critNote}.");
        }
        yield return new WaitForSeconds(0.3f);
    }

    private static int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Vector2Int FindBestMoveTowards(Gladiator mover, Vector2Int targetPos)
    {
        List<Vector2Int> moveRange = mover.GetMovementRange();
        if (moveRange == null || moveRange.Count == 0)
        {
            return mover.CurrentGridPosition;
        }

        Vector2Int bestMove = mover.CurrentGridPosition;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int pos in moveRange)
        {
            if (pos != mover.CurrentGridPosition)
            {
                GridCell cell = GridManager.Instance != null ? GridManager.Instance.GetCellAtPosition(pos) : null;
                if (cell == null)
                {
                    continue;
                }

                if (cell.IsOccupied)
                {
                    continue;
                }
            }

            int distance = GetGridDistance(pos, targetPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMove = pos;
            }
        }

        return bestMove;
    }
}
