// Outfitter/ApparelStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
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
                return 1f; // disabled for now
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
            //   _lastStatUpdate = -5000;
            _lastTempUpdate = -5000;

        }

        public void UpdateTemperatureIfNecessary(bool force = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force)
            {
                // get desired temperatures
          //    if (!TargetTemperaturesOverride)
          //    {
          //        var pawnBaseTempMin = _pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null)-5f;
          //        var pawnBaseTempMax = _pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null)+5f;


                    var baseTemperatureMonth = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords,
                        GenDate.CurrentMonth);

                  var min_basetemp = baseTemperatureMonth - 15f;
                  var max_basetemp = baseTemperatureMonth + 15f;
              
                  if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
                  {
                      min_basetemp += 20f;
                      max_basetemp += 20f;
                  }
              
                  if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
                  {
                      min_basetemp -= 20f;
                      max_basetemp -= 20f;
                  }
              
              //    var calcMinTemp = Math.Min(pawnBaseTempMin, min_basetemp);
              //    var calcMaxTemp = Math.Max(pawnBaseTempMax, max_basetemp);
              //
              //    _mapTemperatures = new FloatRange(min_basetemp, max_basetemp);
              //
              //    _pawnTemperatures = new FloatRange(pawnBaseTempMin, pawnBaseTempMax);
              //
              //    _pawnCalcTemperatures = new FloatRange(calcMinTemp,calcMaxTemp);

                if (GenTemperature.SeasonAcceptableFor(_pawn.def)) _temperatureWeight = 1f;
                else _temperatureWeight = 1f; // 5f;

                    if (!TargetTemperaturesOverride)
                    {
                        _targetTemperatures = new FloatRange(Math.Max(min_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                              Math.Min(max_basetemp, ApparelStatsHelper.MinMaxTemperatureRange.max));
                    }

                    //_pawnCalcTemperatures = new FloatRange(Math.Max(pawnBaseTempMin, ApparelStatsHelper.MinMaxTemperatureRange.min),
                    //                                       Math.Min(pawnBaseTempMax, ApparelStatsHelper.MinMaxTemperatureRange.max));
               // }
            }
        }
    }
}