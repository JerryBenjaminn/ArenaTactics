using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenaTactics.Data;
using ArenaTactics.Managers;
using UnityEngine.SceneManagement;

namespace ArenaTactics.UI
{
    public class TournamentView : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI seasonInfoText;
        public TextMeshProUGUI matchInfoText;
        public TextMeshProUGUI playerStatsText;
        public TextMeshProUGUI playoffInfoText;
        public Transform standingsContainer;
        public GameObject standingsRowPrefab;
        public Button startBattleButton;
        public Button startNewSeasonButton;
        public TextMeshProUGUI errorText;

        private TournamentManager tournamentManager;

        private void OnEnable()
        {
            tournamentManager = FindFirstObjectByType<TournamentManager>(FindObjectsInactive.Include);
            if (tournamentManager == null)
            {
                ShowError("TournamentManager not found.");
                return;
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (tournamentManager == null)
            {
                tournamentManager = FindFirstObjectByType<TournamentManager>(FindObjectsInactive.Include);
                if (tournamentManager == null)
                {
                    ShowError("TournamentManager not found.");
                    return;
                }
            }

            RefreshStandings();
            RefreshMatchInfo();
        }

        public void RefreshStandings()
        {
            if (tournamentManager == null)
            {
                return;
            }

            List<TournamentManager.TeamStanding> standings = tournamentManager.GetStandings();
            foreach (TournamentManager.TeamStanding standing in standings)
            {
                Debug.Log($"[TournamentView] {standing.teamName}: Gold={standing.gold}g");
            }
            ClearStandings();

            for (int i = 0; i < standings.Count; i++)
            {
                CreateStandingRow(i + 1, standings[i]);
            }
        }

        public void RefreshMatchInfo()
        {
            if (tournamentManager == null || tournamentManager.currentSeason == null)
            {
                return;
            }

            SeasonData season = tournamentManager.currentSeason;
            if (seasonInfoText != null)
            {
                seasonInfoText.text = $"Season {tournamentManager.currentSeasonNumber} - {season.phase}";
            }

            TournamentManager.TeamStanding player = tournamentManager.GetStandings()
                .Find(s => s.teamId == TournamentManager.PlayerTeamId);

            if (playerStatsText != null && player != null)
            {
                playerStatsText.text = $"YOU: {player.wins}-{player.losses} | {player.points} pts";
            }

            string matchText = GetNextMatchText(season);
            if (matchInfoText != null)
            {
                matchInfoText.text = matchText;
            }

            if (startBattleButton != null)
            {
                SeasonData.Match nextMatch = tournamentManager.GetCurrentMatch();
                bool hasMatch = nextMatch != null;
                startBattleButton.interactable = hasMatch && season.phase != SeasonData.SeasonPhase.Completed;
                SetStartBattleButtonLabel(season, nextMatch);
                startBattleButton.onClick.RemoveAllListeners();
                startBattleButton.onClick.AddListener(OnStartBattle);
            }

            if (startNewSeasonButton != null)
            {
                bool showNewSeason = season.phase == SeasonData.SeasonPhase.Completed;
                startNewSeasonButton.gameObject.SetActive(showNewSeason);
                startNewSeasonButton.onClick.RemoveAllListeners();
                if (showNewSeason)
                {
                    startNewSeasonButton.onClick.AddListener(OnStartNewSeason);
                }
            }

            if (playoffInfoText != null)
            {
                playoffInfoText.text = GetPlayoffText(season);
            }
        }

        private void OnStartBattle()
        {
            if (tournamentManager == null || tournamentManager.currentSeason == null)
            {
                return;
            }

            SeasonData.Match nextMatch = FindNextMatch(tournamentManager.currentSeason);
            if (nextMatch == null)
            {
                Debug.LogWarning("[TournamentView] No available match to start.");
                return;
            }

            tournamentManager.SetActiveMatch(nextMatch);
            SceneManager.LoadScene("Battle");
        }

        private void OnStartNewSeason()
        {
            if (tournamentManager == null)
            {
                return;
            }

            int nextSeason = tournamentManager.currentSeasonNumber + 1;
            tournamentManager.StartNewSeason(nextSeason);
            RefreshUI();
        }

        private string GetNextMatchText(SeasonData season)
        {
            if (season.phase == SeasonData.SeasonPhase.Completed)
            {
                string championName = season.finals != null ? tournamentManager.GetTeamName(season.finals.winnerId) : "TBD";
                return $"Season Complete! Champion: {championName}";
            }

            SeasonData.Match nextMatch = tournamentManager.GetCurrentMatch();
            if (nextMatch == null)
            {
                return "No upcoming matches.";
            }

            string opponentId = tournamentManager.GetPlayerOpponentId(nextMatch);
            string opponentName = tournamentManager.GetTeamName(opponentId);

            return $"NEXT MATCH: YOU vs {opponentName}";
        }

        private SeasonData.Match FindNextMatch(SeasonData season)
        {
            if (season.phase == SeasonData.SeasonPhase.RegularSeason)
            {
                return season.schedule.Find(m => !m.isCompleted &&
                                                 (m.team1Id == TournamentManager.PlayerTeamId || m.team2Id == TournamentManager.PlayerTeamId));
            }

            if (season.phase == SeasonData.SeasonPhase.Playoffs)
            {
                if (season.semifinal1 != null && !season.semifinal1.isCompleted &&
                    (season.semifinal1.team1Id == TournamentManager.PlayerTeamId || season.semifinal1.team2Id == TournamentManager.PlayerTeamId))
                {
                    return season.semifinal1;
                }
                if (season.semifinal2 != null && !season.semifinal2.isCompleted &&
                    (season.semifinal2.team1Id == TournamentManager.PlayerTeamId || season.semifinal2.team2Id == TournamentManager.PlayerTeamId))
                {
                    return season.semifinal2;
                }
                if (season.finals != null && !season.finals.isCompleted &&
                    (season.finals.team1Id == TournamentManager.PlayerTeamId || season.finals.team2Id == TournamentManager.PlayerTeamId))
                {
                    return season.finals;
                }
            }

            return null;
        }

        private string GetPlayoffText(SeasonData season)
        {
            if (season.phase != SeasonData.SeasonPhase.Playoffs)
            {
                return string.Empty;
            }

            string semi1 = season.semifinal1 != null
                ? $"{tournamentManager.GetTeamName(season.semifinal1.team1Id)} vs {tournamentManager.GetTeamName(season.semifinal1.team2Id)}"
                : "TBD";
            string semi2 = season.semifinal2 != null
                ? $"{tournamentManager.GetTeamName(season.semifinal2.team1Id)} vs {tournamentManager.GetTeamName(season.semifinal2.team2Id)}"
                : "TBD";
            string finals = season.finals != null
                ? $"{tournamentManager.GetTeamName(season.finals.team1Id)} vs {tournamentManager.GetTeamName(season.finals.team2Id)}"
                : "TBD";

            return $"SEMIFINALS: {semi1} | {semi2}\nFINALS: {finals}";
        }

        private void SetStartBattleButtonLabel(SeasonData season, SeasonData.Match nextMatch)
        {
            TextMeshProUGUI buttonText = startBattleButton != null
                ? startBattleButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;

            if (buttonText == null)
            {
                return;
            }

            if (season.phase == SeasonData.SeasonPhase.Completed)
            {
                buttonText.text = "SEASON COMPLETE";
                return;
            }

            if (nextMatch == null)
            {
                buttonText.text = "NO MATCH";
                return;
            }

            if (season.phase == SeasonData.SeasonPhase.Playoffs)
            {
                if (season.finals != null && nextMatch == season.finals)
                {
                    buttonText.text = "FINALS";
                }
                else
                {
                    buttonText.text = "SEMIFINAL";
                }
                return;
            }

            buttonText.text = "START BATTLE";
        }

        private void ClearStandings()
        {
            if (standingsContainer == null)
            {
                return;
            }

            foreach (Transform child in standingsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateStandingRow(int rank, TournamentManager.TeamStanding standing)
        {
            if (standingsContainer == null || standingsRowPrefab == null || standing == null)
            {
                return;
            }

            GameObject row = Instantiate(standingsRowPrefab, standingsContainer);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 4)
            {
                texts[0].text = $"#{rank}";
                texts[1].text = standing.teamName;
                texts[2].text = $"{standing.wins}-{standing.losses}";
                texts[3].text = $"{standing.points}";
            }
            if (texts.Length >= 5)
            {
                texts[4].text = $"{standing.gold}g";
            }

            if (standing.teamId == TournamentManager.PlayerTeamId)
            {
                Image img = row.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.2f, 0.4f, 1f, 0.2f);
                }
            }
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }
    }
}
