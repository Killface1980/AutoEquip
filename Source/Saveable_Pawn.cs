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
        public Pawn Pawn;
        public List<Saveable_Outfit_StatDef> Stats = new List<Saveable_Outfit_StatDef>();

        public List<Apparel> ToWearApparel = new List<Apparel>();
        public List<Apparel> ToDropApparel = new List<Apparel>();
        public List<Apparel> TargetApparel = new List<Apparel>();

        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "pawn");
            Scribe_Collections.LookList(ref Stats, "stats", LookMode.Deep);
        }

        public IEnumerable<Saveable_Outfit_StatDef> NormalizeCalculedStatDef()
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(Pawn);
            List<Saveable_Outfit_StatDef> calculatedStatDef = new List<Saveable_Outfit_StatDef>(outfit.Stats);

            if ((outfit.AppendIndividualPawnStatus) && (Stats != null))
            {
                foreach (Saveable_Outfit_StatDef stat in Stats)
                {
                    int index = -1;
                    for (int i = 0; i < calculatedStatDef.Count; i++)
                    {
                        if (calculatedStatDef[i].StatDef == stat.StatDef)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                        calculatedStatDef.Add(stat);
                    else
                        calculatedStatDef[index] = stat;
                }
            }

         
            return calculatedStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }

        public IEnumerable<Saveable_Outfit_StatDef> NormalizeCalculedWorkStatDef()
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(Pawn);
            List<Saveable_Outfit_StatDef> calculatedStatDef = new List<Saveable_Outfit_StatDef>(outfit.Stats);


            if (outfit.AddWorkStats && (Stats != null))
            {
                foreach (WorkTypeDef wType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
                {

                    int priority = Pawn.workSettings.GetPriority(wType);

                    float priorityAdjust;
                    switch (priority)
                    {
                        case 1:
                            priorityAdjust = 1f;
                            break;
                        case 2:
                            priorityAdjust = 0.4f;
                            break;
                        case 3:
                            priorityAdjust = 0.2f;
                            break;
                        case 4:
                            priorityAdjust = 0.1f;
                            break;
                        default:
                            continue;
                    }

                    foreach (KeyValuePair<StatDef, float> workStat in PawnCalcForApparel.GetStatsOfWorkType(wType))
                    {
                        Saveable_Outfit_StatDef statdef = null;
                        foreach (Saveable_Outfit_StatDef s in calculatedStatDef)
                        {
                            if (s.StatDef == workStat.Key)
                            {
                                statdef = s;
                                break;
                            }
                        }
                        if (statdef == null)
                        {
                            statdef = new Saveable_Outfit_StatDef();
                            statdef.StatDef = workStat.Key;
                            statdef.Strength = workStat.Value * priorityAdjust;
                            calculatedStatDef.Add(statdef);
                        }
                        else statdef.Strength = Math.Max(statdef.Strength, workStat.Value * priorityAdjust);
                    }

                }

            }
            return calculatedStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }

    }
}