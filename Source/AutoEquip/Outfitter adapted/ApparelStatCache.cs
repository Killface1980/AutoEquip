// Outfitter/ApparelStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public enum StatAssignment
    {
        Manual,
        Automatic,
        Override
    }

    public class ApparelStatCache
    {
        private int _lastStatUpdate;
        private int _lastTempUpdate;
        private readonly Pawn _pawn;
        private FloatRange _targetTemperatures;
        public bool targetTemperaturesOverride;
        private float _temperatureWeight;

        public float TemperatureWeight
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _temperatureWeight;
            }
        }


        public FloatRange TargetTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _targetTemperatures;
            }
            set
            {
                _targetTemperatures = value;
                targetTemperaturesOverride = true;
            }

        }

        public ApparelStatCache(Pawn pawn)
        {
          _pawn = pawn;
       //   _lastStatUpdate = -5000;
          _lastTempUpdate = -5000;
        }

        public void UpdateTemperatureIfNecessary(bool force = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force)
            {
                // get desired temperatures
                if (!targetTemperaturesOverride)
                {
                    var baseTemperature = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords,
                        GenDate.CurrentMonth);

                    var min_basetemp = baseTemperature;
                    var max_basetemp = baseTemperature;

                    min_basetemp -= 15f;
                    max_basetemp += 15f;

                    if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
                    {
                        min_basetemp += 15f;
                        max_basetemp += 20f;
                    }

                    if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
                    {
                        min_basetemp -= 15f;
                        max_basetemp -= 20f;
                    }

                    _targetTemperatures = new FloatRange(Math.Max(min_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min(max_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.max));
                }
                _temperatureWeight = GenTemperature.SeasonAcceptableFor(_pawn.def) ? 1f : 5f;
            }
        }

    }
}