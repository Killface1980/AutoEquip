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
        public List<Saveable_Pawn_WorkStatDef> WorkStats = new List<Saveable_Pawn_WorkStatDef>();

        public List<Apparel> ToWearApparel = new List<Apparel>();
        public List<Apparel> ToDropApparel = new List<Apparel>();
        public List<Apparel> TargetApparel = new List<Apparel>();
        public int _lastStatUpdate;
        public int _lastWorkStatUpdate;

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
                if (Find.TickManager.TicksGame - _lastStatUpdate > 1900)
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
                    _lastStatUpdate = Find.TickManager.TicksGame;
                }
            }

            return calculatedStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }

        public IEnumerable<Saveable_Pawn_WorkStatDef> NormalizeCalculedWorkStatDef()
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(Pawn);
            if (outfit.AddWorkStats)
                if (Find.TickManager.TicksGame - _lastWorkStatUpdate > 1900)
                {
                    List<Saveable_Pawn_WorkStatDef> calculatedWorkStatDef = new List<Saveable_Pawn_WorkStatDef>(outfit.WorkStats);

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
                                    priorityAdjust = 0.5f;
                                    break;
                                case 3:
                                    priorityAdjust = 0.25f;
                                    break;
                                case 4:
                                    priorityAdjust = 0.1f;
                                    break;
                                default:
                                    continue;
                            }

                            foreach (KeyValuePair<StatDef, float> workStat in PawnCalcForApparel.GetStatsOfWorkType(wType))
                            {
                                Saveable_Pawn_WorkStatDef workstatdef = null;
                                foreach (Saveable_Pawn_WorkStatDef saveablePawnWorkStatDef in calculatedWorkStatDef)
                                {
                                    if (saveablePawnWorkStatDef.StatDef.defName == workStat.Key.ToString())
                                    {
                                        workstatdef = saveablePawnWorkStatDef;

                                        break;
                                    }

                                }

                                if (workstatdef == null)
                                {
                                    workstatdef = new Saveable_Pawn_WorkStatDef();

                                    workstatdef.StatDef = workStat.Key;

                                    workstatdef.Strength = workStat.Value * priorityAdjust;

                                    calculatedWorkStatDef.Add(workstatdef);
                                }
                                //       else workstatdef.Strength = Math.Max(workstatdef.Strength, workStat.Value * priorityAdjust);
                                else workstatdef.Strength = workstatdef.Strength + (workStat.Value * priorityAdjust);

                                //    WorkStats.Add(workstatdef);
                            }
                        }
                    }

                    WorkStats = new List<Saveable_Pawn_WorkStatDef>(calculatedWorkStatDef.OrderByDescending(i => Math.Abs(i.Strength)).ToArray());
                }

            _lastWorkStatUpdate = Find.TickManager.TicksGame;

            return WorkStats;
            //  return calculatedWorkStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }

    }
}