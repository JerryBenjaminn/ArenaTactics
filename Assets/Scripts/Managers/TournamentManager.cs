using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaTactics.Data;

namespace ArenaTactics.Managers
{
    public class TournamentManager : MonoBehaviour
    {
        public const string PlayerTeamId = "PLAYER";
        public const int RegularGoldReward = 300;
        public const int PlayoffGoldReward = 500;
        public const int FinalsGoldReward = 1000;

        private static TournamentManager instance;
        public static TournamentManager Instance => instance;

        [Header("Runtime")]
        public SeasonData currentSeason;
        public int currentSeasonNumber = 0;

        [SerializeField] private EnemyTeamGenerator enemyTeamGenerator;

        private readonly List<AITeam> aiTeams = new List<AITeam>();
        private readonly List<MatchResult> matchResults = new List<MatchResult>();

        private int playerWins;
        private int playerLosses;
        private int playerPoints;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void StartNewSeason(int seasonNum)
        {
            currentSeasonNumber = seasonNum;
            currentSeason = new SeasonData();
            currentSeason.phase = SeasonData.SeasonPhase.RegularSeason;

            aiTeams.Clear();
            matchResults.Clear();
            playerWins = 0;
            playerLosses = 0;
            playerPoints = 0;

            CreateAiTeams();
            CreateSchedule();

            Debug.Log($"[Tournament] Started season {seasonNum}. Matches: {currentSeason.schedule.Count}");
        }

        public bool HasActiveSeason()
        {
            return currentSeason != null && currentSeason.schedule != null && currentSeason.schedule.Count > 0;
        }

        public void RecordMatchResult(string winnerId, string loserId)
        {
            if (string.IsNullOrEmpty(winnerId) || string.IsNullOrEmpty(loserId))
            {
                return;
            }

            int goldReward = GetGoldRewardForPhase();
            matchResults.Add(new MatchResult(winnerId, loserId, winnerId, loserId, goldReward));

            UpdateTeamRecord(winnerId, loserId, goldReward);
            MarkMatchComplete(winnerId, loserId);

            if (currentSeason.phase == SeasonData.SeasonPhase.RegularSeason &&
                currentSeason.schedule.All(m => m.isCompleted))
            {
                StartPlayoffs();
            }

            if (currentSeason.phase == SeasonData.SeasonPhase.Playoffs)
            {
                UpdatePlayoffProgress();
            }
        }

        public List<TeamStanding> GetStandings()
        {
            List<TeamStanding> standings = new List<TeamStanding>
            {
                new TeamStanding(PlayerTeamId, "Player", playerWins, playerLosses, playerPoints)
            };

            standings.AddRange(aiTeams.Select(t =>
                new TeamStanding(t.teamId, t.teamName, t.wins, t.losses, t.points)));

            return standings
                .OrderByDescending(s => s.points)
                .ThenByDescending(s => s.GetWinPercentage())
                .ToList();
        }

        public void StartPlayoffs()
        {
            currentSeason.phase = SeasonData.SeasonPhase.Playoffs;

            List<TeamStanding> standings = GetStandings();
            if (standings.Count < 4)
            {
                return;
            }

            TeamStanding seed1 = standings[0];
            TeamStanding seed2 = standings[1];
            TeamStanding seed3 = standings[2];
            TeamStanding seed4 = standings[3];

            currentSeason.semifinal1 = new SeasonData.Match(seed1.teamId, seed4.teamId);
            currentSeason.semifinal2 = new SeasonData.Match(seed2.teamId, seed3.teamId);
            currentSeason.finals = null;

            Debug.Log("[Tournament] Playoffs started: 1v4 and 2v3.");
        }

        private void UpdatePlayoffProgress()
        {
            if (currentSeason.semifinal1 == null || currentSeason.semifinal2 == null)
            {
                return;
            }

            if (currentSeason.finals == null &&
                currentSeason.semifinal1.isCompleted &&
                currentSeason.semifinal2.isCompleted)
            {
                currentSeason.finals = new SeasonData.Match(
                    currentSeason.semifinal1.winnerId,
                    currentSeason.semifinal2.winnerId);
                Debug.Log("[Tournament] Finals created.");
            }
            else if (currentSeason.finals != null && currentSeason.finals.isCompleted)
            {
                currentSeason.phase = SeasonData.SeasonPhase.Completed;
                Debug.Log($"[Tournament] Season complete. Champion: {currentSeason.finals.winnerId}");
            }
        }

        private void CreateAiTeams()
        {
            aiTeams.Add(new AITeam("AI_1", "The Peasant Militia", 1500));
            aiTeams.Add(new AITeam("AI_2", "The Village Warriors", 1800));
            aiTeams.Add(new AITeam("AI_3", "The Iron Guard", 2000));
            aiTeams.Add(new AITeam("AI_4", "The Stone Legion", 2000));
            aiTeams.Add(new AITeam("AI_5", "The Silver Shields", 2200));
            aiTeams.Add(new AITeam("AI_6", "The Crimson Blades", 2500));
            aiTeams.Add(new AITeam("AI_7", "The Shadow Reapers", 3000));

            if (enemyTeamGenerator == null)
            {
                enemyTeamGenerator = FindFirstObjectByType<EnemyTeamGenerator>(FindObjectsInactive.Include);
            }

            if (enemyTeamGenerator == null)
            {
                Debug.LogWarning("[Tournament] EnemyTeamGenerator not found. AI rosters left empty.");
                return;
            }

            foreach (AITeam team in aiTeams)
            {
                List<GladiatorInstance> roster = enemyTeamGenerator.GenerateTeamWithBudget(team.currentBudget, null);
                if (roster != null)
                {
                    team.roster = roster;
                }
            }
        }

        private void CreateSchedule()
        {
            List<string> teamIds = new List<string> { PlayerTeamId };
            teamIds.AddRange(aiTeams.Select(t => t.teamId));

            List<string> rotation = new List<string>(teamIds);
            int rounds = rotation.Count - 1;
            int half = rotation.Count / 2;

            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < half; i++)
                {
                    string team1 = rotation[i];
                    string team2 = rotation[rotation.Count - 1 - i];
                    currentSeason.schedule.Add(new SeasonData.Match(team1, team2));
                }

                string last = rotation[rotation.Count - 1];
                rotation.RemoveAt(rotation.Count - 1);
                rotation.Insert(1, last);
            }
        }

        private void UpdateTeamRecord(string winnerId, string loserId, int goldReward)
        {
            if (winnerId == PlayerTeamId)
            {
                playerWins++;
                playerPoints += 3;
            }
            else
            {
                AITeam team = aiTeams.FirstOrDefault(t => t.teamId == winnerId);
                if (team != null)
                {
                    team.RecordWin(goldReward);
                }
            }

            if (loserId == PlayerTeamId)
            {
                playerLosses++;
            }
            else
            {
                AITeam team = aiTeams.FirstOrDefault(t => t.teamId == loserId);
                if (team != null)
                {
                    team.RecordLoss();
                }
            }
        }

        private void MarkMatchComplete(string teamA, string teamB)
        {
            if (currentSeason == null)
            {
                return;
            }

            if (currentSeason.phase == SeasonData.SeasonPhase.RegularSeason)
            {
                SeasonData.Match match = currentSeason.schedule.FirstOrDefault(m =>
                    !m.isCompleted &&
                    ((m.team1Id == teamA && m.team2Id == teamB) ||
                     (m.team1Id == teamB && m.team2Id == teamA)));

                if (match != null)
                {
                    match.isCompleted = true;
                    match.winnerId = teamA;
                }
            }
            else if (currentSeason.phase == SeasonData.SeasonPhase.Playoffs)
            {
                SeasonData.Match[] playoffMatches = { currentSeason.semifinal1, currentSeason.semifinal2, currentSeason.finals };
                foreach (SeasonData.Match match in playoffMatches)
                {
                    if (match == null || match.isCompleted)
                    {
                        continue;
                    }

                    if ((match.team1Id == teamA && match.team2Id == teamB) ||
                        (match.team1Id == teamB && match.team2Id == teamA))
                    {
                        match.isCompleted = true;
                        match.winnerId = teamA;
                        break;
                    }
                }
            }
        }

        private int GetGoldRewardForPhase()
        {
            if (currentSeason == null)
            {
                return RegularGoldReward;
            }

            switch (currentSeason.phase)
            {
                case SeasonData.SeasonPhase.Playoffs:
                    return PlayoffGoldReward;
                case SeasonData.SeasonPhase.Completed:
                    return FinalsGoldReward;
                case SeasonData.SeasonPhase.RegularSeason:
                default:
                    return RegularGoldReward;
            }
        }

        public class TeamStanding
        {
            public string teamId;
            public string teamName;
            public int wins;
            public int losses;
            public int points;

            public TeamStanding(string id, string name, int w, int l, int p)
            {
                teamId = id;
                teamName = name;
                wins = w;
                losses = l;
                points = p;
            }

            public float GetWinPercentage()
            {
                int total = wins + losses;
                return total == 0 ? 0f : (float)wins / total;
            }
        }
    }
}
