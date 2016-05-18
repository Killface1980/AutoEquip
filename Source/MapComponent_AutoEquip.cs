using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public class MapComponent_AutoEquip : MapComponent
    {
        public List<Saveable_Outfit> OutfitCache = new List<Saveable_Outfit>();
        public List<Saveable_Pawn> PawnCache = new List<Saveable_Pawn>();

        public int nextOptimization;

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
            Scribe_Collections.LookList(ref this.OutfitCache, "outfits", LookMode.Deep);
            Scribe_Collections.LookList(ref this.PawnCache, "pawns", LookMode.Deep);
            base.ExposeData();

            if (this.OutfitCache == null)
                this.OutfitCache = new List<Saveable_Outfit>();

            if (this.PawnCache == null)
                this.PawnCache = new List<Saveable_Pawn>();
        }

        public Saveable_Outfit GetOutfit(Pawn pawn) { return this.GetOutfit(pawn.outfits.CurrentOutfit); }

        public Saveable_Outfit GetOutfit(Outfit outfit)
        {
            foreach (Saveable_Outfit o in this.OutfitCache)
                if (o.Outfit == outfit)
                    return o;

            Saveable_Outfit ret = new Saveable_Outfit();
            ret.Outfit = outfit;
            ret.Stats.Add(new Saveable_Outfit_StatDef() { StatDef = StatDefOf.ArmorRating_Sharp, Strength = 1.00f });
            ret.Stats.Add(new Saveable_Outfit_StatDef() { StatDef = StatDefOf.ArmorRating_Blunt, Strength = 0.75f });

            this.OutfitCache.Add(ret);

            return ret;
        }

        public Saveable_Pawn GetCache(Pawn pawn)
        {
            foreach (Saveable_Pawn c in this.PawnCache)
                if (c.pawn == pawn)
                    return c;
            Saveable_Pawn n = new Saveable_Pawn();
            n.pawn = pawn;
            this.PawnCache.Add(n);
            return n;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame < this.nextOptimization)
                return;

#if LOG
            MapComponent_AutoEquip.logMessage = new StringBuilder();
            MapComponent_AutoEquip.logMessage.AppendLine("Start Scaning Best Apparel");
            MapComponent_AutoEquip.logMessage.AppendLine();
#endif
            List<Saveable_Pawn> newSaveableList = new List<Saveable_Pawn>();
            List<PawnCalcForApparel> newCalcList = new List<PawnCalcForApparel>();

            List<Apparel> allApparels = new List<Apparel>(Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>());
            foreach (Pawn pawn in Find.Map.mapPawns.FreeColonists)
            {
                InjectTab(pawn.def);
                Saveable_Pawn newPawnSaveable = this.GetCache(pawn);
                PawnCalcForApparel newPawnCalc = new PawnCalcForApparel(newPawnSaveable);

                newSaveableList.Add(newPawnSaveable);
                newCalcList.Add(newPawnCalc);

                newPawnCalc.InitializeFixedApparelsAndGetAvaliableApparels(allApparels);
            }

            this.PawnCache = newSaveableList;
            PawnCalcForApparel.DoOptimizeApparel(newCalcList, allApparels);

#if LOG
            this.nextOptimization = Find.TickManager.TicksGame + 500;
#else
            this.nextOptimization = Find.TickManager.TicksGame + 5000;
#endif
        }

        private static void InjectTab(ThingDef thingDef)
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
