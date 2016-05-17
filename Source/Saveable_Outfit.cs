using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AutoEquip
{
    public class Saveable_Outfit : IExposable
    {
        public Outfit Outfit;
        public bool AddWorkStats = false;
        public List<Saveable_Outfit_StatDef> Stats = new List<Saveable_Outfit_StatDef>();

        public void ExposeData()
        {
            Scribe_Values.LookValue(ref this.AddWorkStats, "addWorkStats", false, false);
            Scribe_References.LookReference(ref this.Outfit, "outfit");
            Scribe_Collections.LookList(ref this.Stats, "stats", LookMode.Deep);
        }
    }
}
