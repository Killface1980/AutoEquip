using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public static class AutoEquip_JobGiver_OptimizeApparel
    {
        private const int ApparelOptimizeCheckInterval = 3000;
        private const float MinScoreGainToCare = 0.05f;
        private const float ScoreFactorIfNotReplacing = 10f;






#if LOG
        private static StringBuilder debugSb;
#endif

        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 500;
//            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 3000;
        }

        internal static Job _TryGiveTerminalJob(this JobGiver_OptimizeApparel obj, Pawn pawn)
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

            Saveable_Pawn configurarion = MapComponent_AutoEquip.Get.GetCache(pawn);
            Outfit currentOutfit = pawn.outfits.CurrentOutfit;


            #region [  Drops unequiped  ]

            if (configurarion.ToDropApparel != null)
                for (int i = configurarion.ToDropApparel.Count - 1; i >= 0; i--)
                {
                    Apparel a = configurarion.ToDropApparel[i];
                    configurarion.ToDropApparel.Remove(a);

                    if (pawn.apparel.WornApparel.Contains(a))
                    {
                        Apparel t;
                        if (pawn.apparel.TryDrop(a, out t))
                        {
                            t.SetForbidden(false, true);

                            Job job = HaulAIUtility.HaulToStorageJob(pawn, t);

                            if (job != null)
                                return job;
                            else
                            {
                                pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 350;
                                return null;
                            }
                        }
                    }
                }

            #endregion


            #region [  Wear Apparel  ]

            if (configurarion.ToWearApparel.Count > 0)
            {
                List<Thing> listToWear = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
                if (listToWear.Count > 0)
                {
                    foreach (var thing in listToWear)
                    {
                        var ap = (Apparel) thing;
                        if (!configurarion.ToWearApparel.Contains(ap)) continue;
                        if (!ap.IsInValidStorage()) continue;
                        if (pawn.CanReserveAndReach(ap, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
//                                if (pawn.CanReserveAndReach(ap, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                        {
#if LOG && JOBS
                                Log.Message("Pawn " + pawn + " wear apparel: " + ap);
#endif
                            configurarion.ToWearApparel.Remove(ap);
                            return new Job(JobDefOf.Wear, ap);
                        }
                    }
                }
            }

            #endregion


            //  foreach (Thing t in list)
            //  {
            //      Apparel apparel = (Apparel)t;
            //
            //
            //      if (HandleOutfitFilter(currentOutfit, apparel))
            //      {
            //          if (Find.SlotGroupManager.SlotGroupAt(apparel.Position) != null)
            //          {
            //              if (!apparel.IsForbidden(pawn))
            //              {
            //                  float num2 = PawnCalcForApparel.ApparelScoreGain(pawn, apparel);
            //                  if (num2 >= MinScoreGainToCare && num2 >= num)
            //                  {
            //                      if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
            //                      {
            //                          if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
            //                          {
            //                              thing = apparel;
            //                              num = num2;
            //                          }
            //
            //                      }
            //                  }
            //              }
            //
            //          }
            //
            //      }
            //
            //  }


            //  #region [  If no Apparel is Selected to Wear, Delays the next search  ]
            //
            //  if (thing == null)
            //  {
            //      SetNextOptimizeTick(pawn);
            //      return null;
            //  } 
            //
            //  #endregion
            //
            //  return new Job(JobDefOf.Wear, thing);

            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 350;
            return null;
        }



    }
}

