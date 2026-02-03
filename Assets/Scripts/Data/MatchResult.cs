using System;

namespace ArenaTactics.Data
{
    [Serializable]
    public class MatchResult
    {
        public string team1Id;
        public string team2Id;
        public string winnerId;
        public string loserId;
        public int goldReward;
        public DateTime timestamp;

        public MatchResult(string team1, string team2, string winner, string loser, int reward)
        {
            team1Id = team1;
            team2Id = team2;
            winnerId = winner;
            loserId = loser;
            goldReward = reward;
            timestamp = DateTime.UtcNow;
        }
    }
}
