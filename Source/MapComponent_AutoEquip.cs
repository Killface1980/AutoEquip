using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace AutoEquip
{
    public class MapComponent_AutoEquip : MapComponent
    {
        public List<Saveable_Outfit> OutfitCache = new List<Saveable_Outfit>();

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
            base.ExposeData();
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
    }
}
