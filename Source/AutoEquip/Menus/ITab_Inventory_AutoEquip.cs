using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class ITab_Pawn_AutoEquip : ITab
    {
        #region Fields

        private const float TopPadding = 20f;
        private const float ThingIconSize = 28f;
        private const float ThingRowHeight = 28f;
        private const float ThingLeftX = 36f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color ThingToEquipLabelColor = new Color(0.7f, 0.7f, 1.0f, 1f);
        public static readonly Color ThingToDropLabelColor = new Color(1.0f, 0.7f, 0.7f, 1f);
        public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        public override bool IsVisible { get { return SelPawnForGear.RaceProps.ToolUser; } }

        public static Texture2D resetButton = ContentFinder<Texture2D>.Get("reset"),
                        deleteButton = ContentFinder<Texture2D>.Get("delete"),
                        addButton = ContentFinder<Texture2D>.Get("add");

        #endregion Fields

        #region Constructors

        public ITab_Pawn_AutoEquip()
        {
            size = new Vector2(540f, 550f);
            labelKey = "AutoEquipTab";
        }

        #endregion Constructors

        #region Properties

        private bool CanEdit { get { return SelPawnForGear.IsColonistPlayerControlled; } }

        private Pawn SelPawnForGear
        {
            get
            {
                if (SelPawn != null)
                {
                    return SelPawn;
                }
                Corpse corpse = SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.innerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + SelThing);
            }
        }

        #endregion Properties

        #region Methods


        protected override void FillTab()
        {
            SaveablePawn pawnSave;
            PawnCalcForApparel pawnCalc;
            if (SelPawnForGear.IsColonist)
            {
                pawnSave = MapComponent_AutoEquip.Get.GetCache(SelPawnForGear);
                pawnCalc = new PawnCalcForApparel(pawnSave);
            }
            else
            {
                pawnSave = null;
                pawnCalc = null;
            }

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);


            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, position.width, position.height);

            if (pawnSave != null)
            {
                Rect rect3 = new Rect(outRect.xMin + 4f, outRect.yMin, 100f, 30f);
                if (Widgets.TextButton(rect3, pawnSave.Pawn.outfits.CurrentOutfit.label, true, false))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Outfit current in Find.Map.outfitDatabase.AllOutfits)
                    {
                        Outfit localOut = current;
                        list.Add(new FloatMenuOption(localOut.label, delegate
                        {
                            pawnSave.Pawn.outfits.CurrentOutfit = localOut;
                        }, MenuOptionPriority.Medium, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list, false));
                }
                rect3 = new Rect(rect3.xMax + 4f, outRect.yMin, 100f, 30f);
                if (Widgets.TextButton(rect3, "AutoEquipStatus".Translate(), true, false))
                {
                    if (pawnSave.Stats == null)
                        pawnSave.Stats = new List<Saveable_Outfit_StatDef>();
                    Find.WindowStack.Add(new Dialog_ManagePawnOutfit(pawnSave.Stats));
                }

                #region Temperatures Slider

                // main canvas
                Rect canvas = new Rect(rect3.xMax + 4f, outRect.yMin + 45f, size.x, size.y).ContractedBy(20f);
                Vector2 cur = Vector2.zero;
                cur.y += 45f;

                // header
                Rect tempHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
                cur.y += 30f;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(tempHeaderRect, "PreferedTemperature".Translate());
                Text.Anchor = TextAnchor.UpperLeft;

                // line
                GUI.color = Color.grey;
                Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
                GUI.color = Color.white;

                // some padding
                cur.y += 10f;

                // temperature slider
                ApparelStatCache pawnStatCache = SelPawn.GetApparelStatCache();
                FloatRange targetTemps = pawnStatCache.TargetTemperatures;
                FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;

                Rect sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);

                Rect tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + 10f, 16f, 16f);
                cur.y += 60f; // includes padding 

                // current temperature settings
                GUI.color = pawnStatCache.TargetTemperaturesOverride ? Color.white : Color.grey;
                Widgets_FloatRange.FloatRange(sliderRect, 123123123, ref targetTemps, minMaxTemps, ToStringStyle.Temperature);
                GUI.color = Color.white;

                if (Math.Abs(targetTemps.min - SelPawn.GetApparelStatCache().TargetTemperatures.min) > 1e-4 ||
                     Math.Abs(targetTemps.max - SelPawn.GetApparelStatCache().TargetTemperatures.max) > 1e-4)
                {
                    SelPawn.GetApparelStatCache().TargetTemperatures = targetTemps;
                }

                if (pawnStatCache.TargetTemperaturesOverride)
                {
                    if (Widgets.ImageButton(tempResetRect, resetButton))
                    {
                        pawnStatCache.TargetTemperaturesOverride = false;
                        pawnStatCache.UpdateTemperatureIfNecessary(true);
                    }
                    TooltipHandler.TipRegion(tempResetRect, "TemperatureRangeReset".Translate());
                }


          

                

                #endregion Temperatures Slider


                outRect.yMin += rect3.height + 4f + cur.y;
            }
            Text.Font = GameFont.Small;

            Rect apparelRect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, apparelRect);
            float num = 0f;
            if (SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, apparelRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in SelPawnForGear.equipment.AllEquipment)
                    DrawThingRow(ref num, apparelRect.width, current, true, ThingLabelColor, pawnSave, pawnCalc);
            }
            if (SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref num, apparelRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                    DrawThingRow(ref num, apparelRect.width, current2, true, ThingLabelColor, pawnSave, pawnCalc);
            }
            if (pawnSave != null)
            {
                if ((pawnSave.ToWearApparel != null) &&
                    (pawnSave.ToWearApparel.Any()))
                {
                    Widgets.ListSeparator(ref num, apparelRect.width, "ToWear".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToWearApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        DrawThingRow(ref num, apparelRect.width, current2, false, ThingToEquipLabelColor, pawnSave, pawnCalc);
                }

                if ((pawnSave.ToDropApparel != null) &&
                    (pawnSave.ToDropApparel.Any()))
                {
                    Widgets.ListSeparator(ref num, apparelRect.width, "ToDrop".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToDropApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        DrawThingRow(ref num, apparelRect.width, current2, SelPawnForGear.apparel != null && SelPawnForGear.apparel.WornApparel.Contains(current2), ThingToDropLabelColor, pawnSave, pawnCalc);
                }
            }
            if (SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref num, apparelRect.width, "Inventory".Translate());
                foreach (Thing current3 in SelPawnForGear.inventory.container)
                    DrawThingRow(ref num, apparelRect.width, current3, true, ThingLabelColor, pawnSave, pawnCalc);
            }

            if (Event.current.type == EventType.Layout)
                scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool equiped, Color thingColor, SaveablePawn pawnSave, PawnCalcForApparel pawnCalc)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (Widgets.InvisibleButton(rect) && Event.current.button == 1)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }, MenuOptionPriority.Medium, null, null));
                if (CanEdit && equiped)
                {
                    Action action = null;
                    ThingWithComps eq = thing as ThingWithComps;
                    Apparel ap = thing as Apparel;
                    if (ap != null)
                    {
                        Apparel unused;
                        action = delegate
                        {
                            SelPawnForGear.apparel.TryDrop(ap, out unused, SelPawnForGear.Position, true);
                        };
                    }
                    else if (eq != null && SelPawnForGear.equipment.AllEquipment.Contains(eq))
                    {
                        ThingWithComps unused;
                        action = delegate
                        {
                            SelPawnForGear.equipment.TryDropEquipment(eq, out unused, SelPawnForGear.Position, true);
                        };
                    }
                    else if (!thing.def.destroyOnDrop)
                    {
                        Thing unused;
                        action = delegate
                        {
                            SelPawnForGear.inventory.container.TryDrop(thing, SelPawnForGear.Position, ThingPlaceMode.Near, out unused);
                        };
                    }
                    list.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
                }

                if ((pawnSave != null) &&
                    (thing is Apparel))
                {
                    if (!equiped)
                        list.Add(new FloatMenuOption("Locate", delegate
                        {
                            Pawn apparelEquipedThing = null;

                            foreach (Pawn p in Find.Map.mapPawns.FreeColonists)
                            {
                                foreach (Apparel a in p.apparel.WornApparel)
                                    if (a == thing)
                                    {
                                        apparelEquipedThing = p;
                                        break;
                                    }
                                if (apparelEquipedThing != null)
                                    break;
                            }

                            if (apparelEquipedThing != null)
                            {
                                Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
                                Find.Selector.ClearSelection();
                                if (apparelEquipedThing.Spawned)
                                    Find.Selector.Select(apparelEquipedThing, true, true);
                            }
                            else
                            {
                                Find.CameraMap.JumpTo(thing.PositionHeld);
                                Find.Selector.ClearSelection();
                                if (thing.Spawned)
                                    Find.Selector.Select(thing, true, true);
                            }
                        }, MenuOptionPriority.Medium, null, null));
                    list.Add(new FloatMenuOption("AutoEquip Details", delegate
                    {
                        Find.WindowStack.Add(new DialogPawnApparelDetail(pawnSave.Pawn, (Apparel)thing));
                    }, MenuOptionPriority.Medium, null, null));

                    list.Add(new FloatMenuOption("AutoEquip Comparer", delegate
                    {
                        Find.WindowStack.Add(new Dialog_PawnApparelComparer(pawnSave.Pawn, (Apparel)thing));
                    }, MenuOptionPriority.Medium, null, null));
                }

                FloatMenu window = new FloatMenu(list, thing.LabelCap, false, false);
                Find.WindowStack.Add(window);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = thingColor;
            Rect rect2 = new Rect(36f, y, width - 36f, 28f);
            string text = thing.LabelCap;
            var apparel = thing as Apparel;
            if (apparel != null)
            {
                if ((pawnSave != null) &&
                    (pawnSave.TargetApparel != null))
                    text = pawnCalc.ApparelScoreRaw(apparel).ToString("N5") + "   " + text;

                if (SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced(apparel))
                    text = text + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label(rect2, text);
            y += 28f;
        }
        #endregion Methods
    }
}