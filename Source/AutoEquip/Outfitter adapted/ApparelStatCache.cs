// Outfitter/ApparelStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{

    public class ApparelStatCache
    {
        private int _lastTempUpdate;
        private readonly Pawn _pawn;
        private FloatRange _pawnTemperatures;
        private FloatRange _mapTemperatures;
        private FloatRange _pawnCalcTemperatures;
        private FloatRange _targetTemperatures;

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
                TargetTemperaturesOverride = true;
            }

        }

        public bool TargetTemperaturesOverride;

        public FloatRange PawnCalcTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _pawnCalcTemperatures;
            }
            set
            {
                _pawnCalcTemperatures = value;
            }

        }

        public FloatRange PawnTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _pawnTemperatures;
            }
            set
            {
                _pawnTemperatures = value;
            }

        }

        public FloatRange MapTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _mapTemperatures;
            }
            set
            {
                _mapTemperatures = value;
            }

        }

        private float _temperatureWeight;

        public float TemperatureWeight
        {
            get
            {
                //             return 1f; // disabled for now
                UpdateTemperatureIfNecessary();
                return _temperatureWeight;
            }
        }

        public ApparelStatCache(Pawn pawn)
        {
            _pawn = pawn;
            _pawnTemperatures = PawnTemperatures;
            _mapTemperatures = MapTemperatures;
            _pawnCalcTemperatures = PawnCalcTemperatures;
            _targetTemperatures = TargetTemperatures;
            _lastTempUpdate = -5000;
        }

        private float _pawnBaseTempMin;
        private float _pawnBaseTempMax;
        private float _pawnBaseTempAverage = 0;

        public void UpdateTemperatureIfNecessary(bool force = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force)
            {

                if (_pawnBaseTempAverage.Equals(null))
                {
                    _pawnBaseTempMin = _pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null);
                    _pawnBaseTempMax = _pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null);
                    _pawnBaseTempAverage = (_pawnBaseTempMin + _pawnBaseTempMax) / 2;

                    foreach (var trait in _pawn.story.traits.allTraits)
                    {
                        if (trait.OffsetOfStat(StatDef.Named("ComfyTemperatureMin")) != 0)
                        {
                            _pawnBaseTempMin += trait.OffsetOfStat(StatDef.Named("ComfyTemperatureMin"));
                        }
                        if (trait.OffsetOfStat(StatDef.Named("ComfyTemperatureMax")) != 0)
                        {
                            _pawnBaseTempMax += trait.OffsetOfStat(StatDef.Named("ComfyTemperatureMax"));
                        }
                    }
                }

                var baseTemperatureMonth = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords,
                    GenDate.CurrentMonth);
                var averageTempNow = baseTemperatureMonth;

                var min_basetemp = baseTemperatureMonth - 15f;
                var max_basetemp = baseTemperatureMonth + 15f;

                if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
                {
                    min_basetemp += 20f;
                    max_basetemp += 20f;
                    averageTempNow += 20f;
                }

                if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
                {
                    min_basetemp -= 20f;
                    max_basetemp -= 20f;
                    averageTempNow -= 20f;
                }

                float calcweight = 1 + Math.Abs(_pawnBaseTempAverage - averageTempNow) / 10;

                _temperatureWeight = calcweight;

                if (!TargetTemperaturesOverride)
                {
                    _targetTemperatures = new FloatRange(Math.Max(min_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min(max_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.max));
                }

                _pawnCalcTemperatures = new FloatRange(_pawnBaseTempMin, _pawnBaseTempMax);

                // }
                _lastTempUpdate = Find.TickManager.TicksGame;

            }
        }

    }
}