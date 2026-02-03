using System;
using System.Collections.Generic;

namespace ArenaTactics.Data
{
    [Serializable]
    public class SeasonData
    {
        public enum SeasonPhase
        {
            RegularSeason,
            Playoffs,
            Completed
        }

        public SeasonPhase phase = SeasonPhase.RegularSeason;
        public List<Match> schedule = new List<Match>();
        public int currentMatchIndex = 0;
        public List<MatchResult> results = new List<MatchResult>();

        public Match semifinal1;
        public Match semifinal2;
        public Match finals;

        [Serializable]
        public class Match
        {
            public string team1Id;
            public string team2Id;
            public bool isCompleted;
            public string winnerId;

            public Match(string team1, string team2)
            {
                team1Id = team1;
                team2Id = team2;
                isCompleted = false;
                winnerId = string.Empty;
            }
        }
    }
}
