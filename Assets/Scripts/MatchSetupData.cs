using System.Collections.Generic;

public static class MatchSetupData
{
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
    }
}