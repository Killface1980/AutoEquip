using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class PawnCalcForApparel
    {
        private readonly Saveable_Pawn saveablePawn;
        private readonly Pawn pawn;
        private readonly Outfit _outfit;
        private bool _optimized;
        private List<Apparel> _fixedApparels;
        private float? _totalStats;

        private static Saveable_Outfit_StatDef[] _stats;

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

        private List<Apparel> _calculedApparelItems;
        private List<float> _calculedApparelScore;

        public PawnCalcForApparel(Pawn pawn)
            : this(MapComponent_AutoEquip.Get.GetCache(pawn))
        { }

        public PawnCalcForApparel(Saveable_Pawn saveablePawn)
            : this(saveablePawn, GenDate.CurrentMonth, 0f)
        { }

        public PawnCalcForApparel(Saveable_Pawn saveablePawn, Month month, float temperatureAjust)
        {
            this.saveablePawn = saveablePawn;
            pawn = saveablePawn.pawn;
            _outfit = pawn.outfits.CurrentOutfit;
            _stats = saveablePawn.NormalizeCalculedStatDef().ToArray();
            
            _neededWarmth = CalculateNeededWarmth(pawn, GenDate.CurrentMonth);


        }

        public string LabelCap { get { return pawn.LabelCap; } }
        public static IEnumerable<Saveable_Outfit_StatDef> Stats { get { return _stats; } }

        public IEnumerable<Apparel> CalculedApparel { get { return _calculedApparelItems; } }

        public void InitializeFixedApparelsAndGetAvaliableApparels(List<Apparel> allApparels)
        {
            _fixedApparels = new List<Apparel>();
            foreach (Apparel pawnApparel in pawn.apparel.WornApparel)
                if (pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                    allApparels.Insert(0, pawnApparel);
                else
                    _fixedApparels.Add(pawnApparel);
        }

        public void InitializeAllApparelScores(List<Apparel> allApparels)
        {
            _allApparelsItems = new List<Apparel>();
            _allApparelsScore = new List<float>();
            foreach (Apparel apparel in allApparels)
            {
                _allApparelsItems.Add(apparel);
                _allApparelsScore.Add(ApparelScoreRaw(apparel));
            }
        }

        public void InitializeCalculedApparelScoresFromWornApparel()
        {
            _calculedApparelItems = new List<Apparel>();
            _calculedApparelScore = new List<float>();
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                _calculedApparelItems.Add(apparel);
                _calculedApparelScore.Add(ApparelScoreRaw(apparel));
            }
            _optimized = false;
        }

        public void InitializeCalculedApparelScoresFromFixedApparel()
        {
            _calculedApparelItems = new List<Apparel>();
            _calculedApparelScore = new List<float>();
            foreach (Apparel apparel in _fixedApparels)
            {
                _calculedApparelItems.Add(apparel);
                _calculedApparelScore.Add(ApparelScoreRaw(apparel));
            }
            _optimized = false;
        }

        #region [  ApparelScoreRaw  ]


        public float ApparelScoreRaw(Apparel ap)
        {
            float score = ApparelScoreRawStats(ap);
            //  num *= ApparelScoreRawHitPointAdjust(ap);
            //  num *= ApparelScoreRawInsulationColdAdjust(ap);
            //  return num;

            //base score
            //      float score = 0.1f;

            //calculating protection, it also gets a little buff
            float protectionScore =
                ap.GetStatValue(StatDefOf.ArmorRating_Sharp) +
                ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;


                score += protectionScore * 1.25f;
            

            //calculating HP
            if (ap.def.useHitPoints)
            {
                float hpPercent = ap.HitPoints / (float)ap.MaxHitPoints;
                score *= HitPointsPercentScoreFactorCurve.Evaluate(hpPercent);
            }

            return score;
        }


        public static float ApparelScoreRaw(Pawn pawn, Apparel ap)
        {
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(pawn.outfits.CurrentOutfit);
            float score = ApparelScoreRawStats(pawn, outfit, ap);
            //  num *= ApparelScoreRawHitPointAdjust(ap);
            //  num *= ApparelScoreRawInsulationColdAdjust(ap);
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
                float hpPercent = ap.HitPoints / (float)ap.MaxHitPoints;
                score *= HitPointsPercentScoreFactorCurve.Evaluate(hpPercent);
            }

            //calculating warmth
            
            score *= ApparelScoreRawInsulationColdAdjust(ap);

            return score;
        }

        public float ApparelScoreRawStats(Apparel ap)
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

            return num / count;
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


        private static float ApparelScoreRawStats(Pawn pawn, Saveable_Outfit outfit, Apparel ap)
        {
            float num = 0.1f;

            

            foreach (Saveable_Outfit_StatDef stat in outfit.Stats)
            {
                float nint = RawStat(pawn, ap, stat.StatDef);

                num += nint * stat.Strength;
            }
            return num;
        }

        public float GetStatValue(Apparel apparel, Saveable_Outfit_StatDef stat)
        {
            float baseStat = apparel.GetStatValue(stat.StatDef, true);
            float currentStat = baseStat;
            currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            DoApparelScoreRawStatsHandlers(pawn, apparel, stat.StatDef, ref currentStat);

            if (baseStat == 0)
                return currentStat;
            return currentStat / baseStat;
        }


        #endregion

        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            if (ap.def == ThingDefOf.Apparel_PersonalShield && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }
            float num = ApparelScoreRaw(pawn, ap);
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
                    num -= ApparelScoreRaw(pawn, wornApparel[i]);
                    flag = true;
                }
            }
            if (!flag)
            {
                num *= 10f;
            }
            return num;
        }

        #region [  OLD_CalculateApparelModifierRaw  ]

        public float OLD_CalculateApparelModifierRaw(Apparel ap)
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

        #region [  OLD_CalculateApparelScoreGain  ]

        public bool OLD_CalculateApparelScoreGain(Apparel apparel, out float gain)
        {
            if (_calculedApparelItems == null)
                InitializeCalculedApparelScoresFromWornApparel();

            return OLD_CalculateApparelScoreGain(apparel, ApparelScoreRaw(apparel), out gain);
        }

        private bool OLD_CalculateApparelScoreGain(Apparel apparel, float score, out float gain)
        {
            if (apparel.def == ThingDefOf.Apparel_PersonalShield && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                gain = -1000f;
                return false;
            }

            gain = score;
            for (int i = 0; i < _calculedApparelItems.Count; i++)
            {
                Apparel wornApparel = _calculedApparelItems[i];

                if (!ApparelUtility.CanWearTogether(wornApparel.def, apparel.def))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel))
                        return false;
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

#if LOG && ALLAPPARELS
            MapComponent_AutoEquip.logMessage.AppendLine("All Apparels");
            foreach (Apparel a in allApparels)
                MapComponent_AutoEquip.logMessage.AppendLine("   " + a.LabelCap);
            MapComponent_AutoEquip.logMessage.AppendLine();
#endif

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

#if LOG && PARTIAL_OPTIMIZE
                    MapComponent_AutoEquip.logMessage.AppendLine("Optimization For Pawn: " + pawnCalc.LabelCap);
                    foreach (Apparel ap in pawnCalc.CalculedApparel)
                        MapComponent_AutoEquip.logMessage.AppendLine("    * Apparel: " + ap.LabelCap);
                    MapComponent_AutoEquip.logMessage.AppendLine();
#endif
                }

                if (!CheckForConflicts(newCalcList))
                    break;
            }

            foreach (PawnCalcForApparel pawnCalc in newCalcList)
                pawnCalc.PassToSaveable();

#if LOG
            Type T = typeof(GUIUtility);
            PropertyInfo systemCopyBufferProperty = T.GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
            systemCopyBufferProperty.SetValue(null, MapComponent_AutoEquip.logMessage.ToString(), null);

            Log.Message(MapComponent_AutoEquip.logMessage.ToString());
            MapComponent_AutoEquip.logMessage = null;
#endif
        }

        #region [  Conflict  ]

        private static bool CheckForConflicts(IEnumerable<PawnCalcForApparel> pawns)
        {
            bool any = false;
            var pawnCalcForApparels = pawns as PawnCalcForApparel[] ?? pawns.ToArray();
            foreach (PawnCalcForApparel pawnCalc in pawnCalcForApparels)
            {
                foreach (Apparel apprel in pawnCalc.CalculedApparel)
                {
                    float? apparalGainPercentual = null;

                    foreach (PawnCalcForApparel otherPawnCalc in pawnCalcForApparels)
                    {
                        if (otherPawnCalc == pawnCalc)
                            continue;

                        foreach (Apparel otherApprel in otherPawnCalc.CalculedApparel)
                        {
                            if (otherApprel == apprel)
                            {
                                any = true;
                                DoConflict(apprel, pawnCalc, otherPawnCalc, ref apparalGainPercentual);
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
                if (x.saveablePawn.pawn.apparel.WornApparel.Contains(apparel))
                    xPercentual *= 1.1f;
            }

            if (y._totalStats == null)
                y._totalStats = y.OLD_CalculateTotalStats(null);

            float yNoStats = y.OLD_CalculateTotalStats(apparel);
            if (y._totalStats != null)
            {
                float yPercentual = y._totalStats.Value / yNoStats;

                if (y.saveablePawn.pawn.apparel.WornApparel.Contains(apparel))
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
            int index = _calculedApparelItems.IndexOf(apprel);
            if (index == -1)
                Log.Warning("Warning on LooseConflict loser didnt have the apparel");
            _calculedApparelItems.RemoveAt(index);
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
                    if ((!apparel.IsForbidden(saveablePawn.pawn)) &&
                        (_outfit.filter.Allows(apparel)))
                    {
                        float apparelRawScore = _allApparelsScore[i];
                        float apparelGainScore;
                        if (OLD_CalculateApparelScoreGain(apparel, apparelRawScore, out apparelGainScore))
                        {
                            if ((apparelGainScore > changeApparelScoreGain) ||
                                ((apparelGainScore == changeApparelScoreGain) && (saveablePawn.pawn.apparel.WornApparel.Contains(apparel))))
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

                _calculedApparelItems.Add(changeApparel);
                _calculedApparelScore.Add(changeApparelRawScore);
            }
        }

        private float OLD_CalculateTotalStats(Apparel ignore)
        {
            float num = 1.0f;
            foreach (Saveable_Outfit_StatDef stat in _stats)
            {
                float nint = stat.StatDef.defaultBaseValue;

                foreach (Apparel a in _calculedApparelItems)
                {
                    if (a == ignore)
                        continue;
                    nint += a.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);
                }

                foreach (Apparel a in _calculedApparelItems)
                {
                    if (a == ignore)
                        continue;

                    DoApparelScoreRawStatsHandlers(pawn, a, stat.StatDef, ref nint);
                }

                num += nint * stat.Strength;
            }

            if (num == 0)
                Log.Warning("No Stat to optimize apparel");

            return num;
        }

        #endregion

        public float ApparelScoreRawHitPointAdjust(Apparel ap)
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
            saveablePawn.ToWearApparel = new List<Apparel>();
            saveablePawn.ToDropApparel = new List<Apparel>();
            saveablePawn.TargetApparel = _calculedApparelItems;

            List<Apparel> pawnApparel = new List<Apparel>(saveablePawn.pawn.apparel.WornApparel);
            foreach (Apparel ap in _calculedApparelItems)
            {
                if (pawnApparel.Contains(ap))
                {
                    pawnApparel.Remove(ap);
                    continue;
                }
                saveablePawn.ToWearApparel.Add(ap);
            }
            foreach (Apparel ap in pawnApparel)
                saveablePawn.ToDropApparel.Add(ap);

#if LOG && CHANGES
            if (this.saveablePawn.toWearApparel.Any() || this.saveablePawn.toDropApparel.Any())
            {
                MapComponent_AutoEquip.logMessage.AppendLine();
                MapComponent_AutoEquip.logMessage.AppendLine("Apparel Change for: " + this.pawn.LabelCap);

                foreach (Apparel ap in this.saveablePawn.toDropApparel)
                    MapComponent_AutoEquip.logMessage.AppendLine(" * Drop: " + ap);

                foreach (Apparel ap in this.saveablePawn.toWearApparel)
                    MapComponent_AutoEquip.logMessage.AppendLine(" * Wear: " + ap);
            }
#endif
        }

        #endregion


        public static bool HandleOutfitFilter(Outfit currentOutfit, Apparel apparel)
        {
            return (currentOutfit.filter.Allows(apparel));
        }






        public static float RawStat(Pawn pawn, Apparel ap, StatDef stat)
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

        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef statDef, ref float num);
        public static event ApparelScoreRawStatsHandler ApparelScoreRawStatsHandlers;
    }
}
