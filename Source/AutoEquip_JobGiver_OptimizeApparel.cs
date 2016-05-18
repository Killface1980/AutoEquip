using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public static class AutoEquip_JobGiver_OptimizeApparel
    {
        private const int ApparelOptimizeCheckInterval = 3000;
        private const float MinScoreGainToCare = 0.05f;
        private const float ScoreFactorIfNotReplacing = 10f;

        private static NeededWarmth _neededWarmth;

        private static readonly SimpleCurve InsulationColdScoreFactorCurve_NeedWarm = new SimpleCurve
        {
            new CurvePoint(-30f, 8f),
            new CurvePoint(0f, 1f)
        };

        private static readonly SimpleCurve InsulationWarmScoreFactorCurve_NeedCold = new SimpleCurve
        {
            new CurvePoint(30f, 8f),
            new CurvePoint(0f, 1f),
            new CurvePoint(-10, 0.1f)
        };

        private static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0.1f),
            new CurvePoint(0.6f, 0.7f),
            new CurvePoint(1f, 1f)
        };

#if LOG
        private static StringBuilder debugSb;
#endif

        private static void SetNextOptimizeTick(Pawn pawn)
        {

            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 3000;
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

#if !LOG
            if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                return null;
#else            
            AutoEquip_JobGiver_OptimizeApparel.debugSb = new StringBuilder();
            AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("Scanning for " + pawn + " at " + pawn.Position);
#endif

            #region [  Drops forbidden Apparel  ]

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                if ((!currentOutfit.filter.Allows(wornApparel[i])) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    return new Job(JobDefOf.RemoveApparel, wornApparel[i])
                    {
                        haulDroppedApparel = true
                    };
                }
            }

            #endregion

            #region [  If no Apparel are found, Delays the next search  ]

            Apparel thing = null;
            float num = 0f;
            List<Thing> list = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            if (list.Count == 0)
            {
                AutoEquip_JobGiver_OptimizeApparel.SetNextOptimizeTick(pawn);
                return null;
            }

            #endregion


            AutoEquip_JobGiver_OptimizeApparel._neededWarmth = AutoEquip_JobGiver_OptimizeApparel.CalculateNeededWarmth(pawn, GenDate.CurrentMonth);
#if LOG
            if (AutoEquip_JobGiver_OptimizeApparel.neededWarmth != NeededWarmth.Any)
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("Temperature: " + AutoEquip_JobGiver_OptimizeApparel.neededWarmth);
#endif

            for (int j = 0; j < list.Count; j++)
			{
				Apparel apparel = (Apparel)list[j];

#if LOG
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine(apparel.LabelCap);
#endif

                if (HandleOutfitFilter(currentOutfit, apparel))
				{
					if (Find.SlotGroupManager.SlotGroupAt(apparel.Position) != null)
					{
						if (!apparel.IsForbidden(pawn))
						{
							float num2 = AutoEquip_JobGiver_OptimizeApparel.ApparelScoreGain(pawn, apparel);
                            if (num2 >= AutoEquip_JobGiver_OptimizeApparel.MinScoreGainToCare && num2 >= num)
							{
								if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
								{
									if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
									{
										thing = apparel;
										num = num2;
									}
#if LOG
                                    else
                                        AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("  Not CantReserve");
#endif
                                }
                            }
						}
#if LOG
                        else
                            AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("  IsForbidden");
#endif
                    }
#if LOG
                    else
                        AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("  SlotGroupAtNull");
#endif
                }
#if LOG
                else
                    AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("  FilterNotAllows");
#endif
            }

#if LOG
            if (thing != null)
            {
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine();
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("BEST: " + thing.LabelCap + ":        Raw: " + ApparelScoreRaw(pawn, thing).ToString("F2") + "        Gain: " + AutoEquip_JobGiver_OptimizeApparel.ApparelScoreGain(pawn, thing).ToString("F2"));
            }

            if (AutoEquip_JobGiver_OptimizeApparel.debugSb.Length > 0)
                Log.Message(AutoEquip_JobGiver_OptimizeApparel.debugSb.ToString());
            AutoEquip_JobGiver_OptimizeApparel.debugSb = null;
#endif

            #region [  If no Apparel is Selected to Wear, Delays the next search  ]

            if (thing == null)
            {
                AutoEquip_JobGiver_OptimizeApparel.SetNextOptimizeTick(pawn);
                return null;
            } 

            #endregion

            return new Job(JobDefOf.Wear, thing);
        }

        public static bool HandleOutfitFilter(Outfit currentOutfit, Apparel apparel)
        {
            return (currentOutfit.filter.Allows(apparel));
        }

        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            if (ap.def == ThingDefOf.Apparel_PersonalShield && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }
            float num = AutoEquip_JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
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
                    num -= AutoEquip_JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, wornApparel[i]);
                    flag = true;
                }
            }
            if (!flag)
            {
                num *= 10f;
            }
            return num;
        }

        public static float ApparelScoreRaw(Pawn pawn, Apparel ap)
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(pawn.outfits.CurrentOutfit);
            float score = ApparelScoreRawStats(pawn, outfit, ap);
        //  num *= ApparelScoreRawHitPointAjust(ap);
        //  num *= ApparalScoreRawInsulationColdAdjust(ap);
        //  return num;

            //base score
      //      float score = 0.1f;

            //calculating protection, it also gets a little buff
            float protectionScore =
                ap.GetStatValue(StatDefOf.ArmorRating_Sharp) +
                ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;

            if (outfit.AddWorkStats)
            {
                score = score + protectionScore * 0.1f;
            }
            else
            {
                score += protectionScore * 1.25f;
            }

            //calculating HP
            if (ap.def.useHitPoints)
            {
                float hpPercent = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                score *= AutoEquip_JobGiver_OptimizeApparel.HitPointsPercentScoreFactorCurve.Evaluate(hpPercent);
            }

            //calculating warmth
            score *= ApparalScoreRawInsulationColdAdjust(ap);

            return score;
        }

        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(WorkTypeDef worktype)
        {
            switch (worktype.defName)
            {
                case "Research":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ResearchSpeed"), 10f);
                    yield break;
                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 2.5f);
                    yield break;
                case "Hauling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 5.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 5.0f);
                    yield break;
                case "Crafting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 5.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 5.0f);
                    yield break;
                case "Art":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 10f);
                    yield break;
                case "Tailoring":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 10f);
                    yield break;
                case "Smithing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 10f);
                    yield break;
                case "PlantCutting":
                    yield break;
                case "Growing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 1.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("PlantWorkSpeed"), 10f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HarvestFailChance"), -7.5f);
                    yield break;
                case "Mining":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 1.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MiningSpeed"), 10f);
                    //yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.3f);
                    yield break;
                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FixBrokenDownBuildingFailChance"), -7.5f);
                    yield break;
                case "Construction":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 2.0f);
                    //yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ConstructionSpeed"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmoothingSpeed"), 3.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 5.0f);
                    yield break;
                case "Hunting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 2.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingDelayFactor"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ShootingAccuracy"), 10f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0015f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.002f);
                    yield break;
                case "Cooking":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 1.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FoodPoisonChance"), -5.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 5.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 5.0f);
                    yield break;
                case "Handling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 2.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TameAnimalChance"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TrainAnimalChance"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeHitChance"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0015f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.002f);
                    yield break;
                case "Warden":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SocialImpact"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("RecruitPrisonerChance"), 10f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("GiftImpact"), 2.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TradePriceImprovement"), 2.5f);
                    yield break;
                case "Flicker":
                    yield break;
                case "Patient":
                    yield break;
                case "Firefighter":
                    yield break;
                case "Doctor":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 7.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 10f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 5.0f);
                    yield break;
                default:
                    yield break;
            }
        }
        
        public static float ApparelScoreRawStats(Pawn pawn, Saveable_Outfit outfit, Apparel ap)
        {            
            float num = 0.1f;
            
            if (outfit.AddWorkStats)
            {
                foreach (WorkTypeDef wType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
                {
                    int priority = pawn.workSettings.GetPriority(wType);

                    float priorityAdjust;
                    switch (priority)
                    {
                        case 1:
                            priorityAdjust = 1.0f;
                            break;
                        case 2:
                            priorityAdjust = 0.2f;
                            break;
                        case 3:
                            priorityAdjust = 0.1f;
                            break;
                        case 4:
                            priorityAdjust = 0.05f;
                            break;
                        default:
                            continue;
                    }

                    foreach (KeyValuePair<StatDef, float> workStat in AutoEquip_JobGiver_OptimizeApparel.GetStatsOfWorkType(wType))
                    {
                        if (!outfit.Stats.Select(i => i.StatDef).Contains(workStat.Key))
                        {
                            float nint = RawStat(pawn, ap, workStat.Key);                            

                            num += nint * workStat.Value * priorityAdjust;
                        }
                    }
                }
            }

            foreach (Saveable_Outfit_StatDef stat in outfit.Stats)
            {
                float nint = RawStat(pawn, ap, stat.StatDef);

                num += nint * stat.Strength;
            }
            return num;
        }

        private static float RawStat(Pawn pawn, Apparel ap, StatDef stat)
        {
            float nint = ap.GetStatValue(stat, true);

            nint += ap.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            if (ApparelScoreRawStatsHandlers != null)
                ApparelScoreRawStatsHandlers(pawn, ap, stat, ref nint);

            return nint;
        }

        private static float RawStatAdjust(Pawn pawn, Apparel ap, StatDef stat)
        {
            float nint = ap.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            if (ApparelScoreRawStatsHandlers != null)
                ApparelScoreRawStatsHandlers(pawn, ap, stat, ref nint);

            return nint;
        }

        public static float ApparalScoreRawInsulationColdAdjust(Apparel ap)
        {
            switch (AutoEquip_JobGiver_OptimizeApparel._neededWarmth)
            {
                case NeededWarmth.Warm:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Cold, null);
                        return AutoEquip_JobGiver_OptimizeApparel.InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValueAbstract);
                    }
                case NeededWarmth.Cool:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Heat, null);
                        return AutoEquip_JobGiver_OptimizeApparel.InsulationWarmScoreFactorCurve_NeedCold.Evaluate(statValueAbstract);
                    }
                default:
                    return 1;
            }
        }

        public static float ApparelScoreRawHitPointAjust(Apparel ap)
        {
            if (ap.def.useHitPoints)
            {
                float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                return AutoEquip_JobGiver_OptimizeApparel.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            else
                return 1;
        }

        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef statDef, ref float num);
        public static event ApparelScoreRawStatsHandler ApparelScoreRawStatsHandlers;

        public static NeededWarmth CalculateNeededWarmth(Pawn pawn, Month month)
        {
            float num = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords, month);

            if (Verse.Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
            {
#if LOG
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("HEAT_WAVE");
#endif
                num += 20;
            }

            if (Verse.Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
            {
#if LOG
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("COLD_SNAP");
#endif
                num -= 20;
            }

            if (num < pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null) - 4f)
                return NeededWarmth.Warm;

            if (num > pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null) + 4f)
                return NeededWarmth.Cool;

            return NeededWarmth.Any;
        }
    }

}

