using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public class AutoEquip_JobGiver_OptimizeApparel
    {
        private const int ApparelOptimizeCheckInterval = 500;
        //  private const int ApparelOptimizeCheckInterval = 3000;

        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 500;
            //            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 3000;
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

            SaveablePawn configuration = MapComponent_AutoEquip.Get.GetCache(pawn);

            #region [  Drops unequiped  ]

            if (configuration.ToDropApparel != null)
                for (int i = configuration.ToDropApparel.Count - 1; i >= 0; i--)
                {
                    Apparel a = configuration.ToDropApparel[i];
                    configuration.ToDropApparel.Remove(a);

                    if (pawn.apparel.WornApparel.Contains(a))
                    {
                        return new Job(JobDefOf.RemoveApparel, a)
                        {
                            haulDroppedApparel = true
                        };
                        

              //        Apparel t;
              //        if (pawn.apparel.TryDrop(a, out t))
              //        {
              //            t.SetForbidden(false, true);
              //
              //            Job job = HaulAIUtility.HaulToStorageJob(pawn, t);
              //
              //            if (job != null)
              //                return job;
              //            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 350;
              //            return null;
              //        }
              //
             //           Apparel t;
             //           if (pawn.apparel.TryDrop(a, out t))
             //           {
             //               t.SetForbidden(false, true);
             //
             //               Job job = HaulAIUtility.HaulToStorageJob(pawn, t);
             //
             //               if (job != null)
             //                   return job;
             //               else
             //               {
             //                   pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 350;
             //                   return null;
             //               }
             //           }
                    }
                }

            #endregion

            #region [  Wear Apparel  ]

            if (configuration.ToWearApparel.Count > 0)
            {
                List<Thing> listToWear = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
                if (listToWear.Count > 0)
                {
                    foreach (var thing in listToWear)
                    {
                        var ap = (Apparel)thing;
                        if (!configuration.ToWearApparel.Contains(ap)) continue;
                        if (Find.SlotGroupManager.SlotGroupAt(thing.Position) == null) continue;
                        if (thing.IsForbidden(pawn)) continue;
                        if (!ApparelUtility.HasPartsToWear(pawn, thing.def)) continue;

                   //     if (!ap.IsInValidStorage()) continue;
                        if (pawn.CanReserveAndReach(thing, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1) 
                            || pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1))
                        {

                            configuration.ToWearApparel.Remove(ap);
                            if (ap != null)
                            return new Job(JobDefOf.Wear, ap);
                        }
                    }
                }
            }

            #endregion
      
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 350;
            return null;
        }



    }
}

