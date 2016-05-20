﻿using System;
using System.Collections.Generic;
using System.Linq;
using Infusion;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class PawnCalcForApparel
    {
        private readonly Saveable_Pawn _saveablePawn;
        private static Pawn _pawn;
        private readonly Outfit _outfit;
        private bool _optimized;
        private List<Apparel> _fixedApparels;
        private float? _totalStats;

        private static Saveable_Outfit_StatDef[] _stats;
        private static Saveable_Outfit_StatDef[] _workstats;

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

        internal static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0.1f),
            new CurvePoint(0.6f, 0.7f),
            new CurvePoint(1f, 1f)
        };

        private List<Apparel> _allApparelsItems;
        private List<float> _allApparelsScore;

        private List<Apparel> _calculatedApparelItems;
        private List<float> _calculedApparelScore;

        public PawnCalcForApparel(Pawn pawn)
            : this(MapComponent_AutoEquip.Get.GetCache(pawn))
        { }


        public PawnCalcForApparel(Saveable_Pawn saveablePawn)
        {
            this._saveablePawn = saveablePawn;
            _pawn = saveablePawn.Pawn;
            _outfit = _pawn.outfits.CurrentOutfit;
            _stats = saveablePawn.NormalizeCalculedStatDef().ToArray();
            _workstats = saveablePawn.NormalizeCalculedWorkStatDef().ToArray();

            _neededWarmth = CalculateNeededWarmth(_pawn, GenDate.CurrentMonth);

        }

        public static IEnumerable<Saveable_Outfit_StatDef> Stats => _stats;

        public static IEnumerable<Saveable_Outfit_StatDef> WorkStats => _workstats;

        public IEnumerable<Apparel> CalculatedApparel => _calculatedApparelItems;

        public void InitializeFixedApparelsAndGetAvaliableApparels(List<Apparel> allApparels)
        {
            _fixedApparels = new List<Apparel>();
            foreach (Apparel pawnApparel in _pawn.apparel.WornApparel)
                if (_pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                    allApparels.Insert(0, pawnApparel);
                else
                    _fixedApparels.Add(pawnApparel);
        }

        private void InitializeAllApparelScores(List<Apparel> allApparels)
        {
            _allApparelsItems = new List<Apparel>();
            _allApparelsScore = new List<float>();
            foreach (Apparel apparel in allApparels)
            {
                _allApparelsItems.Add(apparel);
                _allApparelsScore.Add(ApparelScoreRaw(apparel));
            }
        }

        private void InitializeCalculedApparelScoresFromWornApparel()
        {
            _calculatedApparelItems = new List<Apparel>();
            _calculedApparelScore = new List<float>();
            foreach (Apparel apparel in _pawn.apparel.WornApparel)
            {
                _calculatedApparelItems.Add(apparel);
                _calculedApparelScore.Add(ApparelScoreTotal(apparel))
                   ;
            }
            _optimized = false;
        }

        private void InitializeCalculedApparelScoresFromFixedApparel()
        {
            _calculatedApparelItems = new List<Apparel>();
            _calculedApparelScore = new List<float>();
            foreach (Apparel apparel in _fixedApparels)
            {
                _calculatedApparelItems.Add(apparel);
                _calculedApparelScore.Add(ApparelScoreTotal(apparel));
            }
            _optimized = false;
        }

        #region [  ApparelScoreRawWorkStats  ]


        public float ApparelScoreTotal(Apparel ap)
        {
            return ApparelScoreRawStats(ap) * this.ApparelModifierRaw(ap) + ApparelScoreRawWorkStats(ap);
        }


        //   public static float ApparelScoreRawWorkStats(Pawn pawn, Apparel ap)
        //   {
        //       Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(pawn.outfits.CurrentOutfit);
        //       float score = 1.0f;
        //
        //       //base score
        //       //      float score = 0.1f;
        //
        //       //calculating protection, no buff
        //       float protectionScore =
        //           ap.GetStatValue(StatDefOf.ArmorRating_Sharp) +
        //           ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;
        //
        //
        //       if (outfit.AddWorkStats)
        //       {
        //           score = score + protectionScore * 0.1f;
        //       }
        //       else
        //       {
        //           score += protectionScore * 1.25f;
        //       }
        //
        //       //calculating HP
        //       if (ap.def.useHitPoints)
        //       {
        //           float hpPercent = ap.HitPoints / (float)ap.MaxHitPoints;
        //           score *= HitPointsPercentScoreFactorCurve.Evaluate(hpPercent);
        //       }
        //
        //       //calculating warmth
        //       
        //       score *= ApparelScoreRawInsulationColdAdjust(ap);
        //
        //       return score;
        //   }

        public static float ApparelScoreRaw(Apparel ap)
        {
            float num = ApparelScoreRawStats(ap);
            num *= ApparelScoreRawHitPointAdjust(ap);
            num *= ApparelScoreRawInsulationColdAdjust(ap);
            return num;
        }

        public static float ApparelScoreRawStats(Apparel ap)
        {
            float num = 1.0f;
            float count = 1.0f;
            foreach (Saveable_Outfit_StatDef stat in Stats)
            {
                try
                {
                    float nint = GetStatValue(ap, stat);
                    num += nint * stat.Strength;
                    count++;
                }
                catch (Exception e)
                {
                    throw new Exception("Error Calculation Stat: " + stat.StatDef, e);
                }
            }

            var score = num / count;

            return score;
        }

        public static float ApparelScoreRawWorkStats(Apparel ap)
        {
            float num = 1.0f;
            float count = 1.0f;

            foreach (Saveable_Outfit_StatDef workstat in WorkStats)
            {
                try
                {
                    float nint = GetStatValue(ap, workstat);
                    num += nint * workstat.Strength;
                    count++;
                }
                catch (Exception e)
                {
                    throw new Exception("Error Calculation Stat: " + workstat.StatDef, e);
                }
            }

            var score = num / count;

            return score;
        }


        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(WorkTypeDef worktype)
        {
            switch (worktype.defName)
            {
                case "Research":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ResearchSpeed"), 1f);
                    yield break;
                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.25f);
                    yield break;
                case "Hauling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.5f);
                    yield break;
                case "Crafting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 0.5f);
                    yield break;
                case "Art":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 1f);
                    yield break;
                case "Tailoring":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 1f);
                    yield break;
                case "Smithing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 1f);
                    yield break;
                case "PlantCutting":
                    yield break;
                case "Growing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.15f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.15f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("PlantWorkSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HarvestFailChance"), -0.75f);
                    yield break;
                case "Mining":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.15f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MiningSpeed"), 1f);
                    //yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.3f);
                    yield break;
                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FixBrokenDownBuildingFailChance"), -0.75f);
                    yield break;
                case "Construction":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.2f);
                    //yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ConstructionSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmoothingSpeed"), 0.35f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.5f);
                    yield break;
                case "Hunting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingDelayFactor"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ShootingAccuracy"), 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0015f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.0002f);
                    yield break;
                case "Cooking":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.15f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FoodPoisonChance"), -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 0.5f);
                    yield break;
                case "Handling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.15f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TameAnimalChance"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TrainAnimalChance"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeHitChance"), 1.0f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.0015f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.0002f);
                    yield break;
                case "Warden":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SocialImpact"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("RecruitPrisonerChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("GiftImpact"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TradePriceImprovement"), 0.25f);
                    yield break;
                case "Flicker":
                    yield break;
                case "Patient":
                    yield break;
                case "Firefighter":
                    yield break;
                case "Doctor":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 0.75f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 0.5f);
                    yield break;
                default:
                    yield break;
            }
        }


        public static float GetStatValue(Apparel apparel, Saveable_Outfit_StatDef stat)
        {
            float baseStat = apparel.GetStatValue(stat.StatDef, true);
            float currentStat = baseStat;
            currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            DoApparelScoreRawStatsHandlers(_pawn, apparel, stat.StatDef, ref currentStat);

            if (baseStat == 0)
                return currentStat;
            return currentStat / baseStat;
        }


        #endregion

        #region [  OLD_CalculateApparelModifierRaw  ]

        public float ApparelModifierRaw(Apparel ap)
        {
            float modHit = ApparelScoreRawHitPointAdjust(ap);
            float modCold = ApparelScoreRawInsulationColdAdjust(ap);
            if ((modHit < 0) && (modCold < 0))
                return modHit * modCold * -1;
            return modHit * modCold;
        }



        public static float ApparelScoreRawInsulationColdAdjust(Apparel ap)
        {

            switch (_neededWarmth)
            {
                case NeededWarmth.Warm:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Cold, null);
                        return InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValueAbstract);
                    }
                case NeededWarmth.Cool:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Heat, null);
                        return InsulationWarmScoreFactorCurve_NeedCold.Evaluate(statValueAbstract);
                    }
                default:
                    return 1;
            }
        }

        #endregion

        #region [  NEW_CalculateApparelScoreGain  ]

        public bool OLD_CalculateApparelScoreGain(Apparel apparel, out float gain)
        {
            if (_calculatedApparelItems == null)
                InitializeCalculedApparelScoresFromWornApparel();

            return NEW_CalculateApparelScoreGain(apparel, ApparelScoreTotal(apparel), out gain);
        }

        private bool NEW_CalculateApparelScoreGain(Apparel apparel, float score, out float gain)
        {
            if (apparel.def == ThingDefOf.Apparel_PersonalShield && _pawn.equipment.Primary != null && !_pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                gain = -1000f;
                return false;
            }

            gain = score;
            for (int i = 0; i < _calculatedApparelItems.Count; i++)
            {
                Apparel wornApparel = _calculatedApparelItems[i];

                if (!ApparelUtility.CanWearTogether(wornApparel.def, apparel.def))
                {
                    if (!_pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel))
                    {
                        return false;
                    }
                    gain -= _calculedApparelScore[i];
                }
            }
            return true;
        }

        #endregion



        public static void DoApparelScoreRawStatsHandlers(Pawn pawn, Apparel apparel, StatDef StatDef, ref float num)
        {
            if (ApparelScoreRawStatsHandlers != null)
                ApparelScoreRawStatsHandlers(pawn, apparel, StatDef, ref num);
        }

        #region [  DoOptimize  ]

        public static void DoOptimizeApparel(List<PawnCalcForApparel> newCalcList, List<Apparel> allApparels)
        {

            foreach (PawnCalcForApparel pawnCalc in newCalcList)
            {
                pawnCalc.InitializeAllApparelScores(allApparels);
                pawnCalc.InitializeCalculedApparelScoresFromFixedApparel();
            }

            while (true)
            {
                bool changed = false;
                foreach (PawnCalcForApparel pawnCalc in newCalcList)
                {
                    pawnCalc.OptimeFromList(ref changed);
                }

                if (!CheckForConflicts(newCalcList))
                    break;
            }

            foreach (PawnCalcForApparel pawnCalc in newCalcList)
                pawnCalc.PassToSaveable();
        }

        #region [  Conflict  ]

        private static bool CheckForConflicts(IEnumerable<PawnCalcForApparel> pawns)
        {
            bool any = false;
            var pawnCalcForApparels = pawns as PawnCalcForApparel[] ?? pawns.ToArray();
            foreach (PawnCalcForApparel pawnCalc in pawnCalcForApparels)
            {
                foreach (Apparel apparel in pawnCalc.CalculatedApparel)
                {
                    float? apparalGainPercentual = null;

                    foreach (PawnCalcForApparel otherPawnCalc in pawnCalcForApparels)
                    {
                        if (otherPawnCalc == pawnCalc)
                            continue;

                        foreach (Apparel otherApprel in otherPawnCalc.CalculatedApparel)
                        {
                            if (otherApprel == apparel)
                            {
                                any = true;
                                DoConflict(apparel, pawnCalc, otherPawnCalc, ref apparalGainPercentual);
                                break;
                            }
                        }

                        if ((!pawnCalc._optimized) ||
                            (!otherPawnCalc._optimized))
                            break;
                    }

                    if (!pawnCalc._optimized)
                        break;
                }
            }

#if LOG && CONFLICT
            MapComponent_AutoEquip.logMessage.AppendLine();
#endif

            return any;
        }

        private static void DoConflict(Apparel apparel, PawnCalcForApparel x, PawnCalcForApparel y, ref float? xPercentual)
        {
            if (!xPercentual.HasValue)
            {
                if (x._totalStats == null)
                    x._totalStats = x.OLD_CalculateTotalStats(null);
                float xNoStats = x.OLD_CalculateTotalStats(apparel);
                xPercentual = x._totalStats / xNoStats;
                if (x._saveablePawn.Pawn.apparel.WornApparel.Contains(apparel))
                    xPercentual *= 1.1f;
            }

            if (y._totalStats == null)
                y._totalStats = y.OLD_CalculateTotalStats(null);

            float yNoStats = y.OLD_CalculateTotalStats(apparel);
            if (y._totalStats != null)
            {
                float yPercentual = y._totalStats.Value / yNoStats;

                if (y._saveablePawn.Pawn.apparel.WornApparel.Contains(apparel))
                    yPercentual *= 1.1f;

                if (xPercentual.Value > yPercentual)
                {
#if LOG && CONFLICT
                MapComponent_AutoEquip.logMessage.AppendLine("Conflict: " + apparel.LabelCap + "   Winner: " + x.pawn.LabelCap + " Looser: " + y.pawn.LabelCap);
#endif
                    y.LooseConflict(apparel);
                }
                else
                {
#if LOG && CONFLICT
                MapComponent_AutoEquip.logMessage.AppendLine("Conflict: " + apparel.LabelCap + "   Winner: " + y.pawn.LabelCap + " Looser: " + x.pawn.LabelCap);
#endif
                    x.LooseConflict(apparel);
                }
            }
        }

        private void LooseConflict(Apparel apprel)
        {
            _optimized = false;
            _totalStats = null;
            int index = _calculatedApparelItems.IndexOf(apprel);
            if (index == -1)
                Log.Warning("Warning on LooseConflict loser didnt have the apparel");
            _calculatedApparelItems.RemoveAt(index);
            _calculedApparelScore.RemoveAt(index);
        }

        private void OptimeFromList(ref bool changed)
        {
            if (_optimized)
                return;

            while (true)
            {
                int changeIndex = -1;
                Apparel changeApparel = null;
                float changeApparelRawScore = 0;
                float changeApparelScoreGain = 0;

                for (int i = 0; i < _allApparelsItems.Count; i++)
                {
                    Apparel apparel = _allApparelsItems[i];
                    if ((!apparel.IsForbidden(_saveablePawn.Pawn)) &&
                        (_outfit.filter.Allows(apparel)))
                    {
                        float apparelRawScore = _allApparelsScore[i];
                        float apparelGainScore;
                        if (NEW_CalculateApparelScoreGain(apparel, apparelRawScore, out apparelGainScore))
                        {
                            if ((apparelGainScore > changeApparelScoreGain) ||
                                ((apparelGainScore == changeApparelScoreGain) && (_saveablePawn.Pawn.apparel.WornApparel.Contains(apparel))))
                            {
                                changeIndex = i;
                                changeApparel = apparel;
                                changeApparelRawScore = apparelRawScore;
                                changeApparelScoreGain = apparelGainScore;
                            }
                        }
                    }
                }

                if (changeApparel == null)
                {
                    _optimized = true;
                    return;
                }
                changed = true;
                _allApparelsItems.RemoveAt(changeIndex);
                _allApparelsScore.RemoveAt(changeIndex);

                _calculatedApparelItems.Add(changeApparel);
                _calculedApparelScore.Add(changeApparelRawScore);
            }
        }

        private float OLD_CalculateTotalStats(Apparel ignore)
        {
            float num = 1.0f;
            foreach (Saveable_Outfit_StatDef stat in _stats)
            {
                float nint = stat.StatDef.defaultBaseValue;

                foreach (Apparel a in _calculatedApparelItems)
                {
                    if (a == ignore)
                        continue;
                    nint += a.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);
                }

                foreach (Apparel a in _calculatedApparelItems)
                {
                    if (a == ignore)
                        continue;

                    DoApparelScoreRawStatsHandlers(_pawn, a, stat.StatDef, ref nint);
                }

                num += nint * stat.Strength;
            }

            foreach (Saveable_Outfit_StatDef workstat in _workstats)
            {
                float nint = workstat.StatDef.defaultBaseValue;

                foreach (Apparel a in _calculatedApparelItems)
                {
                    if (a == ignore)
                        continue;
                    nint += a.def.equippedStatOffsets.GetStatOffsetFromList(workstat.StatDef);
                }

                foreach (Apparel a in _calculatedApparelItems)
                {
                    if (a == ignore)
                        continue;

                    DoApparelScoreRawStatsHandlers(_pawn, a, workstat.StatDef, ref nint);
                }

                num += nint * workstat.Strength;
            }
            if (num == 0)
                Log.Warning("No Stat to optimize apparel");

            return num;
        }

        //  private float OLD_CalculateTotalStats(Apparel ignore)
        //  {
        //      float num = 1.0f;
        //      foreach (Saveable_Outfit_StatDef stat in _stats)
        //      {
        //          float nint = stat.StatDef.defaultBaseValue;
        //
        //          foreach (Apparel a in _calculatedApparelItems)
        //          {
        //              if (a == ignore)
        //                  continue;
        //              nint += a.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);
        //          }
        //
        //          foreach (Apparel a in _calculatedApparelItems)
        //          {
        //              if (a == ignore)
        //                  continue;
        //
        //              DoApparelScoreRawStatsHandlers(_pawn, a, stat.StatDef, ref nint);
        //          }
        //
        //          num += nint * stat.Strength;
        //      }
        //
        //      if (num == 0)
        //          Log.Warning("No Stat to optimize apparel");
        //
        //      return num;
        //  }

        #endregion

        public static float ApparelScoreRawHitPointAdjust(Apparel ap)
        {
            if (ap.def.useHitPoints)
            {
                float x = ap.HitPoints / (float)ap.MaxHitPoints;
                return HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            return 1;

        }


        public static NeededWarmth CalculateNeededWarmth(Pawn pawn, Month month)
        {
            float num = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords, month);

            if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
            {
#if LOG
                AutoEquip_JobGiver_OptimizeApparel.debugSb.AppendLine("HEAT_WAVE");
#endif
                num += 20;
            }

            if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
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


        private void PassToSaveable()
        {
            _saveablePawn.ToWearApparel = new List<Apparel>();
            _saveablePawn.ToDropApparel = new List<Apparel>();
            _saveablePawn.TargetApparel = _calculatedApparelItems;

            List<Apparel> pawnApparel = new List<Apparel>(_saveablePawn.Pawn.apparel.WornApparel);
            foreach (Apparel ap in _calculatedApparelItems)
            {
                if (pawnApparel.Contains(ap))
                {
                    pawnApparel.Remove(ap);
                    continue;
                }
                _saveablePawn.ToWearApparel.Add(ap);
            }
            foreach (Apparel ap in pawnApparel)
                _saveablePawn.ToDropApparel.Add(ap);

        }

        #endregion

        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef statDef, ref float num);
        public static event ApparelScoreRawStatsHandler ApparelScoreRawStatsHandlers;



        public static void InfusionApparelScoreRawStatsHandlers(Pawn pawn, Apparel apparel, StatDef stat, ref float val)
        {
            InfusionSet inf;
            if (apparel.TryGetInfusions(out inf))
            {
                StatMod mod;
                var prefix = inf.Prefix.ToInfusionDef();
                var suffix = inf.Suffix.ToInfusionDef();

                if (!inf.PassPre && prefix.GetStatValue(stat, out mod))
                {
                    val += mod.offset;
                    val *= mod.multiplier;
                }
                if (inf.PassSuf || !suffix.GetStatValue(stat, out mod))
                    return;

                val += mod.offset;
                val *= mod.multiplier;

            }
        }

        public static float RawStat(Pawn pawn, Apparel ap, StatDef stat)
        {
            float nint = ap.GetStatValue(stat, true);

            nint += ap.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            if (ApparelScoreRawStatsHandlers != null)
                ApparelScoreRawStatsHandlers(pawn, ap, stat, ref nint);

            return nint;
        }

    }
}
