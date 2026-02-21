using System.Collections.Generic;

public static class MatchSetupData
{
    public static int TotalRounds { get; set; } = 3;
    public static int CurrentRound { get; set; } = 1;

    /// <summary>Wins accumulated per team index across all rounds this session.</summary>
    public static readonly Dictionary<int, int> TeamWins = new Dictionary<int, int>();

    /// <summary>Winning team id for each completed round, in order.</summary>
    public static readonly List<int> RoundResults = new List<int>();

    public static void RecordWin(int teamId)
    {
        if (!TeamWins.ContainsKey(teamId))
            TeamWins[teamId] = 0;
        TeamWins[teamId]++;
        RoundResults.Add(teamId);
    }

    public static int GetWins(int teamId)
    {
        TeamWins.TryGetValue(teamId, out int wins);
        return wins;
    }

    /// <summary>
    /// Persists a single unit's name and action loadout across rounds.
    /// LiveUnit is null when the unit has died and been destroyed.
    /// </summary>
    public class UnitSlotData
    {
        public string UnitName { get; set; }

        /// <summary>Three action slots (indices 0-2). Elements may be null.</summary>
        public UnitAction[] Actions { get; } = new UnitAction[3];

        /// <summary>Reference to the live Unit; null after death.</summary>
        public Unit LiveUnit { get; set; }

        public UnitSlotData(string name)
        {
            UnitName = name;
        }
    }

    public class TeamSetup
    {
        public string TeamName { get; }
        public int UnitCount { get; }
        public List<string> UnitNames { get; }

        /// <summary>
        /// Per-unit slot data, populated by MatchSetupSpawner on first spawn
        /// and preserved (with updated actions) across rounds.
        /// </summary>
        public List<UnitSlotData> UnitSlots { get; } = new List<UnitSlotData>();

        public TeamSetup(string teamName, int unitCount, List<string> unitNames)
        {
            TeamName = teamName;
            UnitCount = unitCount;
            UnitNames = unitNames ?? new List<string>();
        }
    }

    public static readonly List<TeamSetup> Teams = new List<TeamSetup>();

    public static void Clear()
    {
        Teams.Clear();
        TeamWins.Clear();
        RoundResults.Clear();
        CurrentRound = 1;
    }
}