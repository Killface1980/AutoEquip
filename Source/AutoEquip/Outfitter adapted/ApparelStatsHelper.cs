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

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        public static ApparelStatCache GetApparelStatCache(this Pawn pawn)
        {
            if (!PawnApparelStatCaches.ContainsKey(pawn))
            {
                PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            }
            return PawnApparelStatCaches[pawn];
        }
    }
}