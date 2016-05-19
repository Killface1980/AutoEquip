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
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(pawn);
            List<Saveable_Outfit_StatDef> calculedStatDef = new List<Saveable_Outfit_StatDef>(outfit.Stats);

            if ((outfit.AppendIndividualPawnStatus) &&
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

            if (outfit.AddWorkStats)
            {
                foreach (WorkTypeDef wType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
                {
                    int priority = pawn.workSettings.GetPriority(wType);

                    float priorityAdjust;
                    switch (priority)
                    {
                        case 1:
                            priorityAdjust = 0.8f;
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

                    foreach (KeyValuePair<StatDef, float> stat in PawnCalcForApparel.GetStatsOfWorkType(wType))
                    {
                        Saveable_Outfit_StatDef statdef = null;
                                foreach (Saveable_Outfit_StatDef s in calculedStatDef)
                                {
                                    if (s.StatDef == stat.Key)
                                    {
                                        statdef = s;
                                        break;
                                    }
                                }

                                if (statdef == null)
                                {
                                    statdef = new Saveable_Outfit_StatDef();
                                    statdef.StatDef = stat.Key;
                                    statdef.Strength = stat.Value * priorityAdjust;
                                    calculedStatDef.Add(statdef);
                                }
                                else
                                    statdef.Strength = Math.Max(statdef.Strength, stat.Value * priorityAdjust);
                            }
                        }
                    }
                

         

            //Log.Message(" ");
            //Log.Message("Stats of Pawn " + this.pawn);
            //foreach (Saveable_Outfit_StatDef s in List<Saveable_Outfit_StatDef>)
            //    Log.Message("  * " + s.strength.ToString("N5") + " - " + s.StatDef.label);

            return calculedStatDef.OrderByDescending(i => Math.Abs(i.Strength));
        }
    }
}