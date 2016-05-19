﻿using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Outfit_StatDef : IExposable
    {
        public Saveable_Outfit_StatDef()
        {
            Log.Message("Saveable_Outfit Constructor");
        }        

        public StatDef StatDef;
        public float Strength;

        public void ExposeData()
        {
            Scribe_Defs.LookDef(ref StatDef, "StatDef");
            Scribe_Values.LookValue(ref Strength, "Strength");
        }
    }
}
