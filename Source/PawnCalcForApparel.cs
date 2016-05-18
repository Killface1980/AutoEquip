using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoEquip
{
    public class PawnCalcForApparel
    {
        private Saveable_Pawn saveablePawn;
        private Pawn pawn;
        private Outfit outfit;
        private bool optimized;
        private List<Apparel> fixedApparels;
        private float? totalStats = null;

        private Saveable_Outfit_StatDef[] stats;
        private SimpleCurve needWarmCurve;
        private SimpleCurve needCoolCurve;

        private List<Apparel> allApparelsItems;
        private List<float> allApparelsScore;

        private List<Apparel> calculedApparelItems;
        private List<float> calculedApparelScore;

        public PawnCalcForApparel(Pawn pawn)
            : this(MapComponent_AutoEquip.Get.GetCache(pawn))
        { }

        public PawnCalcForApparel(Saveable_Pawn saveablePawn)
            : this(saveablePawn, GenDate.CurrentMonth, 0f)
        { }

        public PawnCalcForApparel(Saveable_Pawn saveablePawn, Month month, float temperatureAjust)
        {
            this.saveablePawn = saveablePawn;
            this.pawn = saveablePawn.pawn;
            this.outfit = pawn.outfits.CurrentOutfit;
            this.stats = saveablePawn.NormalizeCalculedStatDef().ToArray();

           
        }

        public string LabelCap { get { return this.pawn.LabelCap; } }
        public IEnumerable<Saveable_Outfit_StatDef> Stats { get { return this.stats; } }

        public IEnumerable<Apparel> CalculedApparel { get { return this.calculedApparelItems; } }

        public void InitializeFixedApparelsAndGetAvaliableApparels(List<Apparel> allApparels)
        {
            this.fixedApparels = new List<Apparel>();
            foreach (Apparel pawnApparel in this.pawn.apparel.WornApparel)
                if (this.pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                    allApparels.Insert(0, pawnApparel);
                else
                    this.fixedApparels.Add(pawnApparel);
        }

        public void InitializeAllApparelScores(List<Apparel> allApparels)
        {
            this.allApparelsItems = new List<Apparel>();
            this.allApparelsScore = new List<float>();
            foreach (Apparel apparel in allApparels)
            {
                this.allApparelsItems.Add(apparel);
                this.allApparelsScore.Add(this.CalculateApparelScoreRaw(apparel));
            }
        }

        public void InitializeCalculedApparelScoresFromWornApparel()
        {
            this.calculedApparelItems = new List<Apparel>();
            this.calculedApparelScore = new List<float>();
            foreach (Apparel apparel in this.pawn.apparel.WornApparel)
            {
                this.calculedApparelItems.Add(apparel);
                this.calculedApparelScore.Add(this.CalculateApparelScoreRaw(apparel));
            }
            this.optimized = false;
        }

        public void InitializeCalculedApparelScoresFromFixedApparel()
        {
            this.calculedApparelItems = new List<Apparel>();
            this.calculedApparelScore = new List<float>();
            foreach (Apparel apparel in this.fixedApparels)
            {
                this.calculedApparelItems.Add(apparel);
                this.calculedApparelScore.Add(this.CalculateApparelScoreRaw(apparel));
            }
            this.optimized = false;
        }

        #region [  CalculateApparelScoreRaw  ]

        public float CalculateApparelScoreRaw(Apparel apparel)
        {
            return this.CalculateApparelScoreRawStats(apparel)
                * this.CalculateApparelModifierRaw(apparel);
        }

        public float CalculateApparelScoreRawStats(Apparel apparel)
        {
            float num = 1.0f;
            float count = 1.0f;
            foreach (Saveable_Outfit_StatDef stat in stats)
            {
                try
                {
                    float nint = this.GetStatValue(apparel, stat);
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

        public float GetStatValue(Apparel apparel, Saveable_Outfit_StatDef stat)
        {
            float baseStat = apparel.GetStatValue(stat.StatDef, true);
            float currentStat = baseStat;
            currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            PawnCalcForApparel.DoApparelScoreRawStatsHandlers(pawn, apparel, stat.StatDef, ref currentStat);

            if (baseStat == 0)
                return currentStat;
            else
                return currentStat / baseStat;
        }

        #endregion

        #region [  CalculateApparelModifierRaw  ]

        public float CalculateApparelModifierRaw(Apparel ap)
        {
            float modHit = this.ApparelScoreRawHitPointAjust(ap);
            float modCold = this.CalculateApparelScoreRawInsulationColdAjust(ap);
            if ((modHit < 0) && (modCold < 0))
                return modHit * modCold * -1;
            else
                return modHit * modCold;
        }


        public float CalculateApparelScoreRawInsulationColdAjust(Apparel ap)
        {
            float num3 = 1f;
            if (this.needWarmCurve != null)
            {
                float statValueAbstract = ap.GetStatValue(StatDefOf.Insulation_Cold);
                num3 *= this.needWarmCurve.Evaluate(statValueAbstract);
            }
            if (this.needCoolCurve != null)
            {
                float statValueAbstract = ap.GetStatValue(StatDefOf.Insulation_Heat);
                num3 *= this.needCoolCurve.Evaluate(statValueAbstract);
            }
            return num3;
        }

        #endregion

        #region [  CalculateApparelScoreGain  ]

        public bool CalculateApparelScoreGain(Apparel apparel, out float gain)
        {
            if (this.calculedApparelItems == null)
                this.InitializeCalculedApparelScoresFromWornApparel();

            return this.CalculateApparelScoreGain(apparel, this.CalculateApparelScoreRaw(apparel), out gain);
        }

        private bool CalculateApparelScoreGain(Apparel apparel, float score, out float gain)
        {
            if (apparel.def == ThingDefOf.Apparel_PersonalShield && this.pawn.equipment.Primary != null && !this.pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                gain = -1000f;
                return false;
            }

            gain = score;
            for (int i = 0; i < this.calculedApparelItems.Count; i++)
            {
                Apparel wornApparel = this.calculedApparelItems[i];

                if (!ApparelUtility.CanWearTogether(wornApparel.def, apparel.def))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel))
                        return false;
                    gain -= this.calculedApparelScore[i];
                }
            }

            return true;
        }

        #endregion

        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef StatDef, ref float num);
        public static event ApparelScoreRawStatsHandler ApparelScoreRawStatsHandlers;

        public static void DoApparelScoreRawStatsHandlers(Pawn pawn, Apparel apparel, StatDef StatDef, ref float num)
        {
            if (PawnCalcForApparel.ApparelScoreRawStatsHandlers != null)
                PawnCalcForApparel.ApparelScoreRawStatsHandlers(pawn, apparel, StatDef, ref num);
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

                if (!PawnCalcForApparel.CheckForConflicts(newCalcList))
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
            foreach (PawnCalcForApparel pawnCalc in pawns)
            {
                foreach (Apparel apprel in pawnCalc.CalculedApparel)
                {
                    float? apparalGainPercentual = null;

                    foreach (PawnCalcForApparel otherPawnCalc in pawns)
                    {
                        if (otherPawnCalc == pawnCalc)
                            continue;

                        foreach (Apparel otherApprel in otherPawnCalc.CalculedApparel)
                        {
                            if (otherApprel == apprel)
                            {
                                any = true;
                                PawnCalcForApparel.DoConflict(apprel, pawnCalc, otherPawnCalc, ref apparalGainPercentual);
                                break;
                            }
                        }

                        if ((!pawnCalc.optimized) ||
                            (!otherPawnCalc.optimized))
                            break;
                    }

                    if (!pawnCalc.optimized)
                        break;
                }
            }

#if LOG && CONFLICT
            MapComponent_AutoEquip.logMessage.AppendLine();
#endif

            return any;
        }

        private static void DoConflict(Apparel apprel, PawnCalcForApparel x, PawnCalcForApparel y, ref float? xPercentual)
        {
            if (!xPercentual.HasValue)
            {
                if (x.totalStats == null)
                    x.totalStats = x.CalculateTotalStats(null);
                float xNoStats = x.CalculateTotalStats(apprel);
                xPercentual = x.totalStats / xNoStats;
                if (x.saveablePawn.pawn.apparel.WornApparel.Contains(apprel))
                    xPercentual *= 1.1f;
            }

            if (y.totalStats == null)
                y.totalStats = y.CalculateTotalStats(null);

            float yNoStats = y.CalculateTotalStats(apprel);
            float yPercentual = y.totalStats.Value / yNoStats;

            if (y.saveablePawn.pawn.apparel.WornApparel.Contains(apprel))
                yPercentual *= 1.1f;

            if (xPercentual.Value > yPercentual)
            {
#if LOG && CONFLICT
                MapComponent_AutoEquip.logMessage.AppendLine("Conflict: " + apprel.LabelCap + "   Winner: " + x.pawn.LabelCap + " Looser: " + y.pawn.LabelCap);
#endif
                y.LooseConflict(apprel);
            }
            else
            {
#if LOG && CONFLICT
                MapComponent_AutoEquip.logMessage.AppendLine("Conflict: " + apprel.LabelCap + "   Winner: " + y.pawn.LabelCap + " Looser: " + x.pawn.LabelCap);
#endif
                x.LooseConflict(apprel);
            }
        }

        private void LooseConflict(Apparel apprel)
        {
            this.optimized = false;
            this.totalStats = null;
            int index = this.calculedApparelItems.IndexOf(apprel);
            if (index == -1)
                Log.Warning("Warning on LooseConflict loser didnt have the apparel");
            this.calculedApparelItems.RemoveAt(index);
            this.calculedApparelScore.RemoveAt(index);
        }

        private void OptimeFromList(ref bool changed)
        {
            if (this.optimized)
                return;

            while (true)
            {
                int changeIndex = -1;
                Apparel changeApparel = null;
                float changeApparelRawScore = 0;
                float changeApparelScoreGain = 0;

                for (int i = 0; i < this.allApparelsItems.Count; i++)
                {
                    Apparel apparel = this.allApparelsItems[i];
                    if ((!apparel.IsForbidden(this.saveablePawn.pawn)) &&
                        (this.outfit.filter.Allows(apparel)))
                    {
                        float apparelRawScore = this.allApparelsScore[i];
                        float apparelGainScore;
                        if (this.CalculateApparelScoreGain(apparel, apparelRawScore, out apparelGainScore))
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
                    this.optimized = true;
                    return;
                }
                else
                {
                    changed = true;
                    this.allApparelsItems.RemoveAt(changeIndex);
                    this.allApparelsScore.RemoveAt(changeIndex);

                    this.calculedApparelItems.Add(changeApparel);
                    this.calculedApparelScore.Add(changeApparelRawScore);
                }
            }
        }

        private float CalculateTotalStats(Apparel ignore)
        {
            float num = 1.0f;
            foreach (Saveable_Outfit_StatDef stat in this.stats)
            {
                float nint = stat.StatDef.defaultBaseValue;

                foreach (Apparel a in this.calculedApparelItems)
                {
                    if (a == ignore)
                        continue;
                    nint += a.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);
                }

                foreach (Apparel a in this.calculedApparelItems)
                {
                    if (a == ignore)
                        continue;

                    PawnCalcForApparel.DoApparelScoreRawStatsHandlers(this.pawn, a, stat.StatDef, ref nint);
                }

                num += nint * stat.Strength;
            }

            if (num == 0)
                Log.Warning("No Stat to optimize apparel");

            return num;
        }

        #endregion

        public float ApparelScoreRawHitPointAjust(Apparel ap)
        {
            if (ap.def.useHitPoints)
            {
                float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
                return AutoEquip_JobGiver_OptimizeApparel.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            else
                return 1;
        }


        private void PassToSaveable()
        {
            this.saveablePawn.ToWearApparel = new List<Apparel>();
            this.saveablePawn.ToDropApparel = new List<Apparel>();
            this.saveablePawn.TargetApparel = this.calculedApparelItems;

            List<Apparel> pawnApparel = new List<Apparel>(this.saveablePawn.pawn.apparel.WornApparel);
            foreach (Apparel ap in this.calculedApparelItems)
            {
                if (pawnApparel.Contains(ap))
                {
                    pawnApparel.Remove(ap);
                    continue;
                }
                this.saveablePawn.ToWearApparel.Add(ap);
            }
            foreach (Apparel ap in pawnApparel)
                this.saveablePawn.ToDropApparel.Add(ap);

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
    }
}