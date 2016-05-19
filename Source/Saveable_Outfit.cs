using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Outfit : IExposable
    {
        public Outfit Outfit;
        public bool AddWorkStats;
        public List<Saveable_Outfit_StatDef> Stats = new List<Saveable_Outfit_StatDef>();
        public bool AppendIndividualPawnStatus;


        public void ExposeData()
        {
            Scribe_Values.LookValue(ref AddWorkStats, "addWorkStats", false, false);
            Scribe_References.LookReference(ref Outfit, "outfit");
            Scribe_Values.LookValue(ref AppendIndividualPawnStatus, "IndividualStatus", true);
            Scribe_Collections.LookList(ref Stats, "stats", LookMode.Deep);
        }
    }
}
