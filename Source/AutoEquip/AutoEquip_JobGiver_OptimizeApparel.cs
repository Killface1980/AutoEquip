using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public class AutoEquip_JobGiver_OptimizeApparel
    {
        private const int ApparelOptimizeCheckInterval = 500;
        //  private const int ApparelOptimizeCheckInterval = 3000;

        private const float MinScoreGainToCare = 0.09f;
        private const float ScoreFactorIfNotReplacing = 10f;
        private static StringBuilder debugSb;

        private void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + AutoEquip_JobGiver_OptimizeApparel.ApparelOptimizeCheckInterval + Rand.Range(1, 5) * 101;
        }

        internal Job TryGiveTerminalJob(Pawn pawn)
        {
           
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(pawn + " tried to run JobGiver_OptimizeApparel without an OutfitTracker", 5643897);
                return null;
            }
            if (pawn.Faction != Faction.OfColony)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return null;
            }
            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return null;
                }
            }
            else
            {
                AutoEquip_JobGiver_OptimizeApparel.debugSb = new StringBuilder();
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine(string.Concat(new object[]
                {
                "Scanning for ",
                pawn,
                " at ",
                pawn.Position
                }));
            }

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                if (!currentOutfit.filter.Allows(wornApparel[i]) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    return new Job(JobDefOf.RemoveApparel, wornApparel[i])
                    {
                        haulDroppedApparel = true
                    };
                }
            }
            Thing thing = null;
            float num = 0f;
            List<Thing> list = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            if (list.Count == 0)
            {
                this.SetNextOptimizeTick(pawn);
                return null;
            }



            for (int j = 0; j < list.Count; j++)
            {
                Apparel apparel = (Apparel)list[j];
                if (currentOutfit.filter.Allows(apparel))
                {
                    if (Find.SlotGroupManager.SlotGroupAt(apparel.Position) != null)
                    {
                        if (!apparel.IsForbidden(pawn))
                        {
                            float num2 = AutoEquip_JobGiver_OptimizeApparel.ApparelScoreGain(pawn, apparel);
                            if (DebugViewSettings.debugApparelOptimize)
                            {
                                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine(apparel.LabelCap + ": " + num2.ToString("F2"));
                            }
                            if (num2 >= AutoEquip_JobGiver_OptimizeApparel.MinScoreGainToCare && num2 >= num)
                            {
                                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                                {
                                    if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                                    {
                                        thing = apparel;
                                        num = num2;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (DebugViewSettings.debugApparelOptimize)
            {
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("BEST: " + thing);
                Log.Message(AutoEquip_JobGiver_OptimizeApparel.debugSb.ToString());
                AutoEquip_JobGiver_OptimizeApparel.debugSb = null;
            }
            if (thing == null)
            {
                this.SetNextOptimizeTick(pawn);
                return null;
            }
            return new Job(JobDefOf.Wear, thing);
        }

        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            PawnCalcForApparel conf = new PawnCalcForApparel(pawn);

            if (ap.def == ThingDefOf.Apparel_PersonalShield && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }

            float num = conf.ApparelScoreRaw(ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, ap.def))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                    {
                        return -1000f;
                    }
                    num -= conf.ApparelScoreRaw(wornApparel[i]);
                    flag = true;
                }
            }
            if (!flag)
            {
                num *= AutoEquip_JobGiver_OptimizeApparel.ScoreFactorIfNotReplacing;
            }
            return num;
        }

    }
}

