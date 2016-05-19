using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Pawn : IExposable
    {
        // Exposed members
        public Pawn pawn;
        public List<Saveable_Outfit_StatDef> Stats = new List<Saveable_Outfit_StatDef>();

        public List<Apparel> ToWearApparel = new List<Apparel>();
        public List<Apparel> ToDropApparel = new List<Apparel>();
        public List<Apparel> TargetApparel = new List<Apparel>();

        public void ExposeData()
        {
            Scribe_References.LookReference(ref pawn, "pawn");
            Scribe_Collections.LookList(ref Stats, "stats", LookMode.Deep);
        }

        public IEnumerable<Saveable_Outfit_StatDef> NormalizeCalculedStatDef()
        {
            Saveable_Outfit outFit = MapComponent_AutoEquip.Get.GetOutfit(pawn);
            List<Saveable_Outfit_StatDef> calculedStatDef = new List<Saveable_Outfit_StatDef>(outFit.Stats);

            if ((outFit.AppendIndividualPawnStatus) &&
                (Stats != null))
            {
                foreach (Saveable_Outfit_StatDef stat in Stats)
                {
                    int index = -1;
                    for (int i = 0; i < calculedStatDef.Count; i++)
                    {
                        if (calculedStatDef[i].StatDef == stat.StatDef)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                        calculedStatDef.Add(stat);
                    else
                        calculedStatDef[index] = stat;
                }
            }

            //foreach (WorkTypeDef wType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            //{
            //    int priority = this.pawn.workSettings.GetPriority(wType);

            //    float priorityAjust;
            //    switch (priority)
            //    {                        
            //        case 1:
            //            priorityAjust = 0.5f;
            //            break;
            //        case 2:
            //            priorityAjust = 0.25f;
            //            break;
            //        case 3:
            //            priorityAjust = 0.125f;
            //            break;
            //        case 4:
            //            priorityAjust = 0.0625f;
            //            break;
            //        default:
            //            continue;
            //    }

            //    foreach (KeyValuePair<StatDef, float> stat in MapComponent_AutoEquip.GetStatsOfWorkType(wType))
            //    {
            //        Saveable_Outfit_StatDef StatDef = null;
            //        foreach (Saveable_Outfit_StatDef s in calculedStatDef)
            //        {
            //            if (s.StatDef == stat.Key)
            //            {
            //                StatDef = s;
            //                break;
            //            }
            //        }

            //        if (StatDef == null)
            //        {
            //            StatDef = new Saveable_Outfit_StatDef();
            //            StatDef.StatDef = stat.Key;
            //            StatDef.strength = stat.Value * priorityAjust;
            //            calculedStatDef.Add(StatDef);
            //        }
            //        else
            //            StatDef.strength = Math.Max(StatDef.strength, stat.Value * priorityAjust);
            //    }
            //}

            //Log.Message(" ");
            //Log.Message("Stats of Pawn " + this.pawn);
            //foreach (Saveable_Outfit_StatDef s in List<Saveable_Outfit_StatDef>)
            //    Log.Message("  * " + s.strength.ToString("N5") + " - " + s.StatDef.label);

            return calculedStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }
    }
}