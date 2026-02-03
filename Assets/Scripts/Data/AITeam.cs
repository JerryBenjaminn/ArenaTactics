using System;
using System.Collections.Generic;

namespace ArenaTactics.Data
{
    [Serializable]
    public class AITeam
    {
        public string teamId;
        public string teamName;
        public int startingBudget;
        public int currentBudget;
        public int wins;
        public int losses;
        public int points;
        public int goldEarned;
        public List<GladiatorInstance> roster = new List<GladiatorInstance>();

        public AITeam(string id, string name, int budget)
        {
            teamId = id;
            teamName = name;
            startingBudget = budget;
            currentBudget = budget;
            wins = 0;
            losses = 0;
            points = 0;
            goldEarned = 0;
        }

        public void RecordWin(int goldReward)
        {
            wins++;
            points += 3;
            goldEarned += goldReward;
            currentBudget += goldReward;
        }

        public void RecordLoss()
        {
            losses++;
        }

        public float GetWinPercentage()
        {
            int total = wins + losses;
            if (total == 0)
            {
                return 0f;
            }

            return (float)wins / total;
        }
    }
}
