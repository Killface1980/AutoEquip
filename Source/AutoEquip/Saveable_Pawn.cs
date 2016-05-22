using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;
        public List<Saveable_Outfit_StatDef> Stats = new List<Saveable_Outfit_StatDef>();
        public List<Saveable_Outfit_WorkStatDef> WorkStats = new List<Saveable_Outfit_WorkStatDef>();

        public List<Apparel> ToWearApparel = new List<Apparel>();
        public List<Apparel> ToDropApparel = new List<Apparel>();
        public List<Apparel> TargetApparel = new List<Apparel>();

        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "Pawn");
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
            Scribe_Collections.LookList(ref WorkStats, "WorkStats", LookMode.Deep);
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

        public IEnumerable<Saveable_Outfit_WorkStatDef> NormalizeCalculedWorkStatDef()
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(Pawn);
            List<Saveable_Outfit_WorkStatDef> calculatedWorkStatDef = new List<Saveable_Outfit_WorkStatDef>(outfit.WorkStats);

            if (outfit.AddWorkStats)
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
                            priorityAdjust = 0.3f;
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
                        Saveable_Outfit_WorkStatDef workstatdef = null;

                    foreach (KeyValuePair<StatDef, float> workStat in PawnCalcForApparel.GetStatsOfWorkType(wType))
                    {


                        foreach (Saveable_Outfit_WorkStatDef s in calculatedWorkStatDef)
                        {
                            if (s.StatDef == workStat.Key)
                            {
                                workstatdef = s;
                                break;
                            }
                        }


                        if (workstatdef == null)
                        {
                                workstatdef = new Saveable_Outfit_WorkStatDef();
                                workstatdef.StatDef = workStat.Key;
                                workstatdef.Strength = workStat.Value * priorityAdjust;
                                calculatedWorkStatDef.Add(workstatdef);
                        }
                        else workstatdef.Strength = Math.Max(workstatdef.Strength, workStat.Value * priorityAdjust);




                    }

                }

            }
            return calculatedWorkStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }

    }
}