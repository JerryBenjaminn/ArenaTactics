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
        public const int ChampionBonusGold = 500;

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

        private string activeMatchTeam1Id;
        private string activeMatchTeam2Id;

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
            currentSeason.currentMatchIndex = 0;
            currentSeason.results = new List<MatchResult>();

            aiTeams.Clear();
            matchResults.Clear();
            playerWins = 0;
            playerLosses = 0;
            playerPoints = 0;
            activeMatchTeam1Id = string.Empty;
            activeMatchTeam2Id = string.Empty;

            if (enemyTeamGenerator == null)
            {
                Debug.LogWarning("[Tournament] EnemyTeamGenerator missing! Finding...");
                enemyTeamGenerator = FindFirstObjectByType<EnemyTeamGenerator>(FindObjectsInactive.Include);
                if (enemyTeamGenerator == null)
                {
                    Debug.LogError("[Tournament] EnemyTeamGenerator not found in scene!");
                }
            }

            CreateAiTeams();
            CreateSchedule();

            Debug.Log($"[Tournament] Started season {seasonNum}. Matches: {currentSeason.schedule.Count}");
        }

        public bool HasActiveSeason()
        {
            return currentSeason != null && currentSeason.schedule != null && currentSeason.schedule.Count > 0;
        }

        public bool HasActiveMatch()
        {
            return !string.IsNullOrEmpty(activeMatchTeam1Id) && !string.IsNullOrEmpty(activeMatchTeam2Id);
        }

        public void SetActiveMatch(SeasonData.Match match)
        {
            if (match == null)
            {
                activeMatchTeam1Id = string.Empty;
                activeMatchTeam2Id = string.Empty;
                return;
            }

            activeMatchTeam1Id = match.team1Id;
            activeMatchTeam2Id = match.team2Id;
        }

        public void ClearActiveMatch()
        {
            activeMatchTeam1Id = string.Empty;
            activeMatchTeam2Id = string.Empty;
        }

        public AITeam GetCurrentOpponentTeam()
        {
            if (!HasActiveMatch())
            {
                return null;
            }

            string opponentId = activeMatchTeam1Id == PlayerTeamId ? activeMatchTeam2Id : activeMatchTeam1Id;
            if (opponentId == PlayerTeamId)
            {
                return null;
            }

            return aiTeams.FirstOrDefault(t => t.teamId == opponentId);
        }

        public string GetActiveMatchTeam1Id()
        {
            return activeMatchTeam1Id;
        }

        public string GetActiveMatchTeam2Id()
        {
            return activeMatchTeam2Id;
        }

        public void RecordMatchResult(string winnerId, string loserId)
        {
            if (string.IsNullOrEmpty(winnerId) || string.IsNullOrEmpty(loserId))
            {
                return;
            }

            int goldReward = GetGoldRewardForMatch(winnerId, loserId);
            MatchResult result = new MatchResult(winnerId, loserId, winnerId, loserId, goldReward);
            matchResults.Add(result);
            if (currentSeason != null)
            {
                currentSeason.results.Add(result);
            }

            UpdateTeamRecord(winnerId, loserId, goldReward);
            MarkMatchComplete(winnerId, loserId);

            if (currentSeason.phase == SeasonData.SeasonPhase.RegularSeason &&
                currentSeason.schedule.All(m => m.isCompleted))
            {
                StartPlayoffs();
            }
            else if (currentSeason.phase == SeasonData.SeasonPhase.RegularSeason)
            {
                currentSeason.currentMatchIndex = GetNextMatchIndex(currentSeason.currentMatchIndex);
                SimulateAIMatches();
                if (currentSeason.currentMatchIndex >= currentSeason.schedule.Count)
                {
                    Debug.Log("[Tournament] Regular season complete! Generating playoffs...");
                    StartPlayoffs();
                }
            }

            if (currentSeason.phase == SeasonData.SeasonPhase.Playoffs)
            {
                UpdatePlayoffProgress();
                SimulatePlayoffMatches();
            }
        }

        public List<TeamStanding> GetStandings()
        {
            int playerGold = GetPlayerGoldEarned();
            List<TeamStanding> standings = new List<TeamStanding>
            {
                new TeamStanding(PlayerTeamId, "Player", playerWins, playerLosses, playerPoints, playerGold)
            };

            standings.AddRange(aiTeams.Select(t =>
                new TeamStanding(t.teamId, t.teamName, t.wins, t.losses, t.points, t.goldEarned)));

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
            SimulatePlayoffMatches();
        }

        public SeasonData.Match GetCurrentMatch()
        {
            if (currentSeason == null)
            {
                return null;
            }

            if (currentSeason.phase == SeasonData.SeasonPhase.RegularSeason)
            {
                return currentSeason.schedule.FirstOrDefault(m => !m.isCompleted &&
                                                                  (m.team1Id == PlayerTeamId || m.team2Id == PlayerTeamId));
            }

            if (currentSeason.phase == SeasonData.SeasonPhase.Playoffs)
            {
                if (currentSeason.semifinal1 != null && !currentSeason.semifinal1.isCompleted &&
                    (currentSeason.semifinal1.team1Id == PlayerTeamId || currentSeason.semifinal1.team2Id == PlayerTeamId))
                {
                    return currentSeason.semifinal1;
                }
                if (currentSeason.semifinal2 != null && !currentSeason.semifinal2.isCompleted &&
                    (currentSeason.semifinal2.team1Id == PlayerTeamId || currentSeason.semifinal2.team2Id == PlayerTeamId))
                {
                    return currentSeason.semifinal2;
                }
                if (currentSeason.finals != null && !currentSeason.finals.isCompleted &&
                    (currentSeason.finals.team1Id == PlayerTeamId || currentSeason.finals.team2Id == PlayerTeamId))
                {
                    return currentSeason.finals;
                }
            }

            return null;
        }

        public string GetPlayerOpponentId(SeasonData.Match match)
        {
            if (match == null)
            {
                return string.Empty;
            }

            if (match.team1Id == PlayerTeamId)
            {
                return match.team2Id;
            }
            if (match.team2Id == PlayerTeamId)
            {
                return match.team1Id;
            }

            return string.Empty;
        }

        public string GetTeamName(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                return "Unknown";
            }

            if (teamId == PlayerTeamId)
            {
                return "YOU";
            }

            AITeam team = aiTeams.FirstOrDefault(t => t.teamId == teamId);
            return team != null ? team.teamName : teamId;
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
                CompleteSeason(currentSeason.finals.winnerId);
            }
        }

        private void SimulatePlayoffMatches()
        {
            if (currentSeason == null || currentSeason.phase != SeasonData.SeasonPhase.Playoffs)
            {
                return;
            }

            bool simulated = false;

            if (currentSeason.semifinal1 != null && !currentSeason.semifinal1.isCompleted &&
                !IsPlayerMatch(currentSeason.semifinal1))
            {
                SimulatePlayoffMatch(currentSeason.semifinal1);
                simulated = true;
            }

            if (currentSeason.semifinal2 != null && !currentSeason.semifinal2.isCompleted &&
                !IsPlayerMatch(currentSeason.semifinal2))
            {
                SimulatePlayoffMatch(currentSeason.semifinal2);
                simulated = true;
            }

            UpdatePlayoffProgress();

            if (currentSeason.finals != null && !currentSeason.finals.isCompleted &&
                !IsPlayerMatch(currentSeason.finals))
            {
                SimulatePlayoffMatch(currentSeason.finals);
                simulated = true;
            }

            if (currentSeason.finals != null && currentSeason.finals.isCompleted)
            {
                CompleteSeason(currentSeason.finals.winnerId);
            }

            if (simulated)
            {
                Debug.Log("[Tournament] Simulated AI playoff matches.");
            }
        }

        private void SimulatePlayoffMatch(SeasonData.Match match)
        {
            if (match == null || match.isCompleted)
            {
                return;
            }

            AITeam team1 = GetAITeam(match.team1Id);
            AITeam team2 = GetAITeam(match.team2Id);

            float team1Budget = team1 != null ? team1.currentBudget : 2000f;
            float team2Budget = team2 != null ? team2.currentBudget : 2000f;
            float team1WinChance = team1Budget / Mathf.Max(1f, team1Budget + team2Budget);

            bool team1Wins = Random.value < team1WinChance;
            string winnerId = team1Wins ? match.team1Id : match.team2Id;
            string loserId = team1Wins ? match.team2Id : match.team1Id;

            int goldReward = GetGoldRewardForMatch(match);
            MatchResult result = new MatchResult(match.team1Id, match.team2Id, winnerId, loserId, goldReward);
            matchResults.Add(result);
            if (currentSeason != null)
            {
                currentSeason.results.Add(result);
            }

            match.isCompleted = true;
            match.winnerId = winnerId;

            UpdateTeamRecord(winnerId, loserId, goldReward);
            Debug.Log($"[Tournament] Playoff simulated: {GetTeamName(winnerId)} defeats {GetTeamName(loserId)}");
        }

        private void CompleteSeason(string championId)
        {
            if (currentSeason == null)
            {
                return;
            }

            currentSeason.phase = SeasonData.SeasonPhase.Completed;
            Debug.Log($"[Tournament] Season complete. Champion: {GetTeamName(championId)}");

            if (championId == PlayerTeamId)
            {
                PersistentDataManager dataManager = PersistentDataManager.Instance;
                if (dataManager != null)
                {
                    dataManager.AddGold(ChampionBonusGold);
                }
            }
            else
            {
                AITeam team = GetAITeam(championId);
                if (team != null)
                {
                    team.goldEarned += ChampionBonusGold;
                    team.currentBudget += ChampionBonusGold;
                }
            }
        }

        public int GetGoldRewardForMatch(SeasonData.Match match)
        {
            if (match == null)
            {
                return GetGoldRewardForPhase();
            }

            if (currentSeason != null && currentSeason.finals != null &&
                match.team1Id == currentSeason.finals.team1Id &&
                match.team2Id == currentSeason.finals.team2Id)
            {
                return FinalsGoldReward;
            }

            return currentSeason != null && currentSeason.phase == SeasonData.SeasonPhase.Playoffs
                ? PlayoffGoldReward
                : RegularGoldReward;
        }

        public int GetGoldRewardForMatch(string teamA, string teamB)
        {
            if (currentSeason != null && currentSeason.finals != null &&
                ((currentSeason.finals.team1Id == teamA && currentSeason.finals.team2Id == teamB) ||
                 (currentSeason.finals.team1Id == teamB && currentSeason.finals.team2Id == teamA)))
            {
                return FinalsGoldReward;
            }

            return currentSeason != null && currentSeason.phase == SeasonData.SeasonPhase.Playoffs
                ? PlayoffGoldReward
                : RegularGoldReward;
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

        private int GetPlayerGoldEarned()
        {
            return matchResults
                .Where(r => r.winnerId == PlayerTeamId)
                .Sum(r => r.goldReward);
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

        private void SimulateAIMatches()
        {
            if (currentSeason == null || currentSeason.phase != SeasonData.SeasonPhase.RegularSeason)
            {
                return;
            }

            Debug.Log("[Tournament] Simulating AI matches...");
            int simulatedCount = 0;

            currentSeason.currentMatchIndex = GetNextMatchIndex(currentSeason.currentMatchIndex);

            while (currentSeason.currentMatchIndex < currentSeason.schedule.Count)
            {
                SeasonData.Match nextMatch = currentSeason.schedule[currentSeason.currentMatchIndex];
                if (IsPlayerMatch(nextMatch))
                {
                    break;
                }

                SimulateSingleMatch(nextMatch);
                simulatedCount++;
                currentSeason.currentMatchIndex++;
            }

            Debug.Log($"[Tournament] Simulated {simulatedCount} AI matches");
        }

        private void SimulateSingleMatch(SeasonData.Match match)
        {
            if (match == null || match.isCompleted)
            {
                return;
            }

            AITeam team1 = GetAITeam(match.team1Id);
            AITeam team2 = GetAITeam(match.team2Id);

            float team1Budget = team1 != null ? team1.currentBudget : 2000f;
            float team2Budget = team2 != null ? team2.currentBudget : 2000f;
            float team1WinChance = team1Budget / Mathf.Max(1f, team1Budget + team2Budget);

            bool team1Wins = Random.value < team1WinChance;
            string winnerId = team1Wins ? match.team1Id : match.team2Id;
            string loserId = team1Wins ? match.team2Id : match.team1Id;

            int goldReward = GetGoldRewardForPhase();
            MatchResult result = new MatchResult(match.team1Id, match.team2Id, winnerId, loserId, goldReward);
            matchResults.Add(result);
            if (currentSeason != null)
            {
                currentSeason.results.Add(result);
            }

            match.isCompleted = true;
            match.winnerId = winnerId;

            UpdateTeamRecord(winnerId, loserId, goldReward);

            Debug.Log($"[Tournament] Simulated: {GetTeamName(winnerId)} defeats {GetTeamName(loserId)}");
        }

        private bool IsPlayerMatch(SeasonData.Match match)
        {
            if (match == null)
            {
                return false;
            }

            return match.team1Id == PlayerTeamId || match.team2Id == PlayerTeamId;
        }

        private int GetNextMatchIndex(int startIndex)
        {
            if (currentSeason == null)
            {
                return 0;
            }

            for (int i = Mathf.Max(0, startIndex); i < currentSeason.schedule.Count; i++)
            {
                if (!currentSeason.schedule[i].isCompleted)
                {
                    return i;
                }
            }

            return currentSeason.schedule.Count;
        }

        private AITeam GetAITeam(string teamId)
        {
            if (string.IsNullOrEmpty(teamId) || teamId == PlayerTeamId)
            {
                return null;
            }

            return aiTeams.FirstOrDefault(t => t.teamId == teamId);
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
            public int gold;

            public TeamStanding(string id, string name, int w, int l, int p, int g)
            {
                teamId = id;
                teamName = name;
                wins = w;
                losses = l;
                points = p;
                gold = g;
            }

            public float GetWinPercentage()
            {
                int total = wins + losses;
                return total == 0 ? 0f : (float)wins / total;
            }
        }
    }
}
