// Outfitter/ApparelStatsHelper.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2015-12-31 14:34

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public static class ApparelStatsHelper
    {
        private static readonly Dictionary<Pawn, ApparelStatCache> PawnApparelStatCaches = new Dictionary<Pawn, ApparelStatCache>();

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-80, 80);
//        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        public static ApparelStatCache GetApparelStatCache(this Pawn pawn)
        {
            if (!PawnApparelStatCaches.ContainsKey(pawn))
            {
                PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            }
            return PawnApparelStatCaches[pawn];
        }

        public static Dictionary<StatDef, float> GetWeightedApparelStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            dict.Add(StatDefOf.ArmorRating_Blunt, .5f);
            dict.Add(StatDefOf.ArmorRating_Sharp, .5f);

            // add weights for all worktypes, multiplied by job priority
            foreach (
                WorkTypeDef workType in
                    DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => pawn.workSettings.WorkIsActive(def))
                )
            {
                foreach (KeyValuePair<StatDef, float> stat in PawnCalcForApparel.GetStatsOfWorkType(workType))
                {
                    float weight = stat.Value * (5 - pawn.workSettings.GetPriority(workType));
                    if (dict.ContainsKey(stat.Key))
                    {
                        dict[stat.Key] += weight;
                    }
                    else
                    {
                        dict.Add(stat.Key, weight);
                    }
                }
            }

            // normalize weights
            float max = dict.Values.Select(Math.Abs).Max();
            foreach (StatDef key in new List<StatDef>(dict.Keys))
            {
                // normalize max of absolute weigths to be 10
                dict[key] /= max / 10f;
            }

            return dict;
        }

        private static List<StatDef> _allApparelStats;

        public static List<StatDef> AllStatDefsModifiedByAnyApparel
        {
            get
            {
                if (_allApparelStats == null)
                {
                    _allApparelStats = new List<StatDef>();

                    // add all stat modifiers from all apparels
                    foreach (ThingDef apparel in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.IsApparel))
                    {
                        if (apparel.equippedStatOffsets != null &&
                             apparel.equippedStatOffsets.Count > 0)
                        {
                            foreach (StatModifier modifier in apparel.equippedStatOffsets)
                            {
                                if (!_allApparelStats.Contains(modifier.stat))
                                {
                                    _allApparelStats.Add(modifier.stat);
                                }
                            }
                        }
                    }

                    //// add all stat modifiers from all infusions
                    //foreach ( InfusionDef infusion in DefDatabase<InfusionDef>.AllDefsListForReading )
                    //{
                    //    foreach ( KeyValuePair<StatDef, StatMod> mod in infusion.stats )
                    //    {
                    //        if ( !_allApparelStats.Contains( mod.Key ) )
                    //        {
                    //            _allApparelStats.Add( mod.Key );
                    //        }
                    //    }
                    //}
                }
                return _allApparelStats;
            }
        }



    }
}