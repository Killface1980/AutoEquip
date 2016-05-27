using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class MapComponent_AutoEquip : MapComponent
    {

  //    static MapComponent_AutoEquip()
  //    {
  //        Log.Message("AutoEquip with Infusion Initialized");
  //        PawnCalcForApparel.ApparelScoreRaw_PawnStatsHandlers += PawnCalcForApparel.InfusionApparelScoreRaw_PawnStatsHandlers;
  //    }

        public int _nextOptimization;
        public List<Saveable_Outfit> OutfitCache = new List<Saveable_Outfit>();
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();


        public static MapComponent_AutoEquip Get
        {
            get
            {
                MapComponent_AutoEquip getComponent = Find.Map.components.OfType<MapComponent_AutoEquip>().FirstOrDefault();
                if (getComponent == null)
                {
                    getComponent = new MapComponent_AutoEquip();
                    Find.Map.components.Add(getComponent);
                }

                return getComponent;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookList(ref OutfitCache, "Outfits", LookMode.Deep);
            Scribe_Collections.LookList(ref PawnCache, "Pawns", LookMode.Deep);
            base.ExposeData();

            if (OutfitCache == null)
                OutfitCache = new List<Saveable_Outfit>();

            if (PawnCache == null)
                PawnCache = new List<SaveablePawn>();
        }

        public Saveable_Outfit GetOutfit(Pawn pawn)
        {
            return GetOutfit(pawn.outfits.CurrentOutfit);
        }

        public Saveable_Outfit GetOutfit(Outfit outfit)
        {
            foreach (Saveable_Outfit o in OutfitCache)
                if (o.Outfit == outfit)
                    return o;

            Saveable_Outfit ret = new Saveable_Outfit();
            ret.Outfit = outfit;
            ret.Stats.Add(new Saveable_Outfit_StatDef { StatDef = StatDefOf.ArmorRating_Sharp, Strength = 0.5f });
            ret.Stats.Add(new Saveable_Outfit_StatDef { StatDef = StatDefOf.ArmorRating_Blunt, Strength = 0.5f });

            OutfitCache.Add(ret);

            return ret;
        }

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in PawnCache)
                if (c.Pawn == pawn)
                    return c;
            SaveablePawn n = new SaveablePawn {Pawn = pawn};
            PawnCache.Add(n);
            return n;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame < _nextOptimization)
                return;

#if LOG
            MapComponent_AutoEquip.logMessage = new StringBuilder();
            MapComponent_AutoEquip.logMessage.AppendLine("Start Scaning Best Apparel");
            MapComponent_AutoEquip.logMessage.AppendLine();
#endif
            List<SaveablePawn> newSaveableList = new List<SaveablePawn>();
            List<PawnCalcForApparel> newCalcList = new List<PawnCalcForApparel>();

            List<Apparel> allApparels = new List<Apparel>(Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>());
            foreach (Pawn pawn in Find.Map.mapPawns.FreeColonists)
            {
                InjectTab(pawn.def);
                SaveablePawn newPawnSaveable = GetCache(pawn);
                PawnCalcForApparel newPawnCalc = new PawnCalcForApparel(newPawnSaveable);

                newSaveableList.Add(newPawnSaveable);
                newCalcList.Add(newPawnCalc);

               newPawnCalc.InitializeFixedApparelsAndGetAvaliableApparels(allApparels);
            }

            PawnCache = newSaveableList;
            PawnCalcForApparel.DoOptimizeApparel(newCalcList, allApparels);


            _nextOptimization = Find.TickManager.TicksGame + 5000;
            //this.nextOptimization = Find.TickManager.TicksGame + 5000;
        }

        private void InjectTab(ThingDef thingDef)
        {
            Debug.Log("Inject Tab");
            if (thingDef.inspectorTabsResolved == null)
            {
                thingDef.inspectorTabsResolved = new List<ITab>();
                foreach (Type current in thingDef.inspectorTabs)
                    thingDef.inspectorTabsResolved.Add(ITabManager.GetSharedInstance(current));
            }

            if (!thingDef.inspectorTabsResolved.OfType<ITab_Pawn_AutoEquip>().Any())
            {
                thingDef.inspectorTabsResolved.Add(ITabManager.GetSharedInstance(typeof(ITab_Pawn_AutoEquip)));
                Debug.Log("Add Tab");
            }

            for (int i = thingDef.inspectorTabsResolved.Count - 1; i >= 0; i--)
                if (thingDef.inspectorTabsResolved[i].GetType() == typeof(ITab_Pawn_Gear))
                    thingDef.inspectorTabsResolved.RemoveAt(i);

            for (int i = thingDef.inspectorTabs.Count - 1; i >= 0; i--)
                if (thingDef.inspectorTabs[i] == typeof(ITab_Pawn_Gear))
                    thingDef.inspectorTabs.RemoveAt(i);
        }
    }
}
