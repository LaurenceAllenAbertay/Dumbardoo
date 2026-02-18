using System.Collections.Generic;

public static class MatchSetupData
{
    public class TeamSetup
    {
        public string TeamName { get; }
        public int UnitCount { get; }
        public List<string> UnitNames { get; }

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
