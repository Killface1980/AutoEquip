using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Outfit_WorkStatDef : IExposable
    {

        public StatDef StatDef;
        public float Strength;

        public void ExposeData()
        {
            Scribe_Defs.LookDef(ref StatDef, "StatDef");
            Scribe_Values.LookValue(ref Strength, "Strength");
        }
    }
}
