using System;
using System.Collections.Generic;
using System.Linq;
using Combat_Realism;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class ITab_Pawn_AutoEquip : ITab
    {
        #region Fields

        private const float _barHeight = 20f;
        private const float _margin = 6f;
        private const float _topPadding = 20f;



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
            size = new Vector2(432f, 600f);
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
            #region CR Stuff

            // get the inventory comp
            CompInventory comp = SelPawn.TryGetComp<CompInventory>();

            // set up rects
            Rect listRect = new Rect(
                _margin,
                _topPadding,
                size.x - 2 * _margin,
                size.y - _topPadding - _margin);

            if (comp != null)
            {
                // adjust rects if comp found
                listRect.height -= (_margin + _barHeight) * 2;
                Rect weightRect = new Rect(_margin, listRect.yMax + _margin, listRect.width, _barHeight);
                Rect bulkRect = new Rect(_margin, weightRect.yMax + _margin, listRect.width, _barHeight);

                Utility_Loadouts.DrawBar(bulkRect, comp.currentBulk, comp.capacityBulk, "CR.Bulk".Translate(), SelPawn.GetBulkTip());
                Utility_Loadouts.DrawBar(weightRect, comp.currentWeight, comp.capacityWeight, "CR.Weight".Translate(), SelPawn.GetWeightTip());
            }

            #endregion CR Stuff


            //


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
            Rect mainrRect = new Rect(0f, 20f, size.x, size.y - _barHeight * 1.25f);
            Rect rect2 = mainrRect.ContractedBy(10f);

            Rect position = listRect;
            //       Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height-listRect.height);


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
                cur.y += 5f; // includes padding 

                // current temperature settings
                if (pawnStatCache.TargetTemperaturesOverride)
                {
                    GUI.color = Color.white;
                }

                else GUI.color = Color.grey;

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

            // Equipment
            if (SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, apparelRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in SelPawnForGear.equipment.AllEquipment)
                    DrawThingRow(ref num, apparelRect.width, current, true, ThingLabelColor, pawnSave, pawnCalc);
            }

            //Inventory
            if (SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref num, apparelRect.width, "Inventory".Translate());
                foreach (Thing current3 in SelPawnForGear.inventory.container)
                    DrawThingRow(ref num, apparelRect.width, current3, true, ThingLabelColor, pawnSave, pawnCalc);
            }

            //Apparel
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

            if (Event.current.type == EventType.Layout)
                scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private float _lastClick;

        private void DrawThingRow(ref float y, float width, Thing thing, bool equiped, Color thingColor, SaveablePawn pawnSave, PawnCalcForApparel pawnCalc)
        {
            var lineheight = 40f;
            Rect rect = new Rect(0f, y, width, lineheight);
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            // LMB doubleclick

            if (Widgets.InvisibleButton(rect))
            {
                if (!equiped && Event.current.button == 0)
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
                    if (Time.time - _lastClick < 0.3f)
                    {
                        if (apparelEquipedThing != null)
                        {
                            Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
                            return;
                        }
                        Find.CameraMap.JumpTo(thing.PositionHeld);
                        return;
                    }
                    _lastClick = Time.time;

                }

                //Middle Mouse Button Menu
                else if (Event.current.button == 2)
                {
                    Find.WindowStack.Add(new DialogPawnApparelDetail(pawnSave.Pawn, (Apparel)thing));
                }

                // RMB menu
                else if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(thing));
                    }, MenuOptionPriority.Medium, null, null));

                    if (CanEdit && equiped)
                    {
                        ThingWithComps eq = thing as ThingWithComps;


                        #region CR Stuff #2

                        // Equip option
                        //  ThingWithComps eq = thing as ThingWithComps;
                        if (eq != null && eq.TryGetComp<CompEquippable>() != null)
                        {
                            CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
                            if (compInventory != null)
                            {
                                FloatMenuOption equipOption;
                                string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
                                if (SelPawnForGear.equipment.AllEquipment.Contains(eq) && SelPawnForGear.inventory != null)
                                {
                                    equipOption = new FloatMenuOption("CR_PutAway".Translate(eqLabel),
                                        delegate
                                        {
                                            ThingWithComps oldEq;
                                            SelPawnForGear.equipment.TryTransferEquipmentToContainer(SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.container, out oldEq);
                                        });
                                }
                                else if (!SelPawnForGear.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                                {
                                    equipOption = new FloatMenuOption("CannotEquip".Translate(eqLabel), null);
                                }
                                else
                                {
                                    string equipOptionLabel = "Equip".Translate(eqLabel);
                                    if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
                                    {
                                        equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                    }
                                    equipOption = new FloatMenuOption(equipOptionLabel, delegate
                                    {
                                        compInventory.TrySwitchToWeapon(eq);
                                    });
                                }
                                floatOptionList.Add(equipOption);
                            }
                        }

                        #endregion CR Stuff #2

                        Action action = null;
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
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
                    }

                    if ((pawnSave != null) &&
                        (thing is Apparel))
                    {
                        if (!equiped)
                            floatOptionList.Add(new FloatMenuOption("Locate", delegate
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
                                    //                      Find.Selector.ClearSelection();
                                    //                      if (apparelEquipedThing.Spawned)
                                    //                          Find.Selector.Select(apparelEquipedThing, true, true);
                                }
                                else
                                {
                                    Find.CameraMap.JumpTo(thing.PositionHeld);
                                    //                      Find.Selector.ClearSelection();
                                    //                      if (thing.Spawned)
                                    //                          Find.Selector.Select(thing, true, true);
                                }
                            }, MenuOptionPriority.Medium, null, null));
                        floatOptionList.Add(new FloatMenuOption("AutoEquip Details", delegate
                        {
                            Find.WindowStack.Add(new DialogPawnApparelDetail(pawnSave.Pawn, (Apparel)thing));
                        }, MenuOptionPriority.Medium, null, null));

                        floatOptionList.Add(new FloatMenuOption("AutoEquip Comparer", delegate
                        {
                            Find.WindowStack.Add(new Dialog_PawnApparelComparer(pawnSave.Pawn, (Apparel)thing));
                        }, MenuOptionPriority.Medium, null, null));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap, false, false);
                    Find.WindowStack.Add(window);
                }
            }
            // draw apparel icon
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(6f, y + 4f, lineheight - 12f, lineheight - 12f), thing);
            }

            // draw apparel list
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = thingColor;
            Rect rectScoreText = new Rect(45f, y, 70f, lineheight);
            Rect rectApparelText = new Rect(115f, y, width - 140f, lineheight);
            string text_Score = "";
            string text_ApparelName = thing.LabelCap;
            var apparel = thing as Apparel;
            if (apparel != null)
            {
                text_Score = pawnCalc.ApparelScoreRaw(apparel).ToString("N5");

                //  if ((pawnSave != null) && (pawnSave.TargetApparel != null))
                //  {
                //      text_ApparelName = pawnCalc.ApparelScoreRaw(apparel).ToString("N5") + "   " + text_ApparelName;
                //  }
                if (SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced(apparel))
                {
                    text_Score = "ApparelForcedLower".Translate();
                    //   text_ApparelName = text_ApparelName + ", " + "ApparelForcedLower".Translate();
                }
            }
            Widgets.Label(rectScoreText, text_Score);
            Widgets.Label(rectApparelText, text_ApparelName);
            y += lineheight;
        }

        //private void DrawThingRow(ref float y, float width, Thing thing, SaveablePawn pawnSave)
        //{
        //    var lineheight = 40f;
        //    Rect rect = new Rect(0f, y, width, lineheight);
        //    if (Mouse.IsOver(rect))
        //    {
        //        GUI.color = HighlightColor;
        //        GUI.DrawTexture(rect, TexUI.HighlightTex);
        //    }
        //
        //    // LMB doubleclick
        //
        //    if (Widgets.InvisibleButton(rect))
        //    {
        //        if (!equiped && Event.current.button == 0)
        //        {
        //            Pawn apparelEquipedThing = null;
        //
        //            foreach (Pawn p in Find.Map.mapPawns.FreeColonists)
        //            {
        //                foreach (Apparel a in p.apparel.WornApparel)
        //                    if (a == thing)
        //                    {
        //                        apparelEquipedThing = p;
        //                        break;
        //                    }
        //                if (apparelEquipedThing != null)
        //                    break;
        //            }
        //            if (Time.time - _lastClick < 0.3f)
        //            {
        //                if (apparelEquipedThing != null)
        //                {
        //                    Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
        //                    return;
        //                }
        //                else
        //                {
        //                    Find.CameraMap.JumpTo(thing.PositionHeld);
        //                    return;
        //                }
        //            }
        //            _lastClick = Time.time;
        //
        //        }
        //
        //        //Middle Mouse Button Menu
        //        else if (Event.current.button == 2)
        //        {
        //            Find.WindowStack.Add(new DialogPawnApparelDetail(pawnSave.Pawn, (Apparel)thing));
        //        }
        //
        //        // RMB menu
        //        else if (Event.current.button == 1)
        //        {
        //            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
        //            floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
        //            {
        //                Find.WindowStack.Add(new Dialog_InfoCard(thing));
        //            }, MenuOptionPriority.Medium, null, null));
        //
        //            if (CanEdit && equiped)
        //            {
        //                ThingWithComps eq = thing as ThingWithComps;
        //
        //
        //                #region CR Stuff #2
        //
        //                // Equip option
        //                //  ThingWithComps eq = thing as ThingWithComps;
        //                if (eq != null && eq.TryGetComp<CompEquippable>() != null)
        //                {
        //                    CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
        //                    if (compInventory != null)
        //                    {
        //                        FloatMenuOption equipOption;
        //                        string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
        //                        if (SelPawnForGear.equipment.AllEquipment.Contains(eq) && SelPawnForGear.inventory != null)
        //                        {
        //                            equipOption = new FloatMenuOption("CR_PutAway".Translate(new object[] { eqLabel }),
        //                                new Action(delegate
        //                                {
        //                                    ThingWithComps oldEq;
        //                                    SelPawnForGear.equipment.TryTransferEquipmentToContainer(SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.container, out oldEq);
        //                                }));
        //                        }
        //                        else if (!SelPawnForGear.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        //                        {
        //                            equipOption = new FloatMenuOption("CannotEquip".Translate(new object[] { eqLabel }), null);
        //                        }
        //                        else
        //                        {
        //                            string equipOptionLabel = "Equip".Translate(new object[] { eqLabel });
        //                            if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
        //                            {
        //                                equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
        //                            }
        //                            equipOption = new FloatMenuOption(equipOptionLabel, new Action(delegate
        //                            {
        //                                compInventory.TrySwitchToWeapon(eq);
        //                            }));
        //                        }
        //                        floatOptionList.Add(equipOption);
        //                    }
        //                }
        //
        //                #endregion CR Stuff #2
        //
        //                Action action = null;
        //                Apparel ap = thing as Apparel;
        //                if (ap != null)
        //                {
        //                    Apparel unused;
        //                    action = delegate
        //                    {
        //                        SelPawnForGear.apparel.TryDrop(ap, out unused, SelPawnForGear.Position, true);
        //                    };
        //                }
        //                else if (eq != null && SelPawnForGear.equipment.AllEquipment.Contains(eq))
        //                {
        //                    ThingWithComps unused;
        //                    action = delegate
        //                    {
        //                        SelPawnForGear.equipment.TryDropEquipment(eq, out unused, SelPawnForGear.Position, true);
        //                    };
        //                }
        //                else if (!thing.def.destroyOnDrop)
        //                {
        //                    Thing unused;
        //                    action = delegate
        //                    {
        //                        SelPawnForGear.inventory.container.TryDrop(thing, SelPawnForGear.Position, ThingPlaceMode.Near, out unused);
        //                    };
        //                }
        //                floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
        //            }
        //
        //            if ((pawnSave != null) &&
        //                (thing is Apparel))
        //            {
        //                if (!equiped)
        //                    floatOptionList.Add(new FloatMenuOption("Locate", delegate
        //                    {
        //                        Pawn apparelEquipedThing = null;
        //
        //                        foreach (Pawn p in Find.Map.mapPawns.FreeColonists)
        //                        {
        //                            foreach (Apparel a in p.apparel.WornApparel)
        //                                if (a == thing)
        //                                {
        //                                    apparelEquipedThing = p;
        //                                    break;
        //                                }
        //                            if (apparelEquipedThing != null)
        //                                break;
        //                        }
        //
        //                        if (apparelEquipedThing != null)
        //                        {
        //                            Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
        //                            //                      Find.Selector.ClearSelection();
        //                            //                      if (apparelEquipedThing.Spawned)
        //                            //                          Find.Selector.Select(apparelEquipedThing, true, true);
        //                        }
        //                        else
        //                        {
        //                            Find.CameraMap.JumpTo(thing.PositionHeld);
        //                            //                      Find.Selector.ClearSelection();
        //                            //                      if (thing.Spawned)
        //                            //                          Find.Selector.Select(thing, true, true);
        //                        }
        //                    }, MenuOptionPriority.Medium, null, null));
        //                floatOptionList.Add(new FloatMenuOption("AutoEquip Details", delegate
        //                {
        //                    Find.WindowStack.Add(new DialogPawnApparelDetail(pawnSave.Pawn, (Apparel)thing));
        //                }, MenuOptionPriority.Medium, null, null));
        //
        //                floatOptionList.Add(new FloatMenuOption("AutoEquip Comparer", delegate
        //                {
        //                    Find.WindowStack.Add(new Dialog_PawnApparelComparer(pawnSave.Pawn, (Apparel)thing));
        //                }, MenuOptionPriority.Medium, null, null));
        //            }
        //
        //            FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap, false, false);
        //            Find.WindowStack.Add(window);
        //        }
        //    }
        //    // draw apparel icon
        //    if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
        //    {
        //        Widgets.ThingIcon(new Rect(6f, y + 4f, lineheight - 12f, lineheight - 12f), thing);
        //    }
        //
        //    // draw apparel list
        //    Text.Anchor = TextAnchor.MiddleLeft;
        //    GUI.color = thingColor;
        //    Rect rectScoreText = new Rect(45f, y, 70f, lineheight);
        //    Rect rectApparelText = new Rect(115f, y, width - 140f, lineheight);
        //    string text_Score = "";
        //    string text_ApparelName = thing.LabelCap;
        //    var apparel = thing as Apparel;
        //    if (apparel != null)
        //    {
        //        text_Score = pawnCalc.ApparelScoreRaw(apparel).ToString("N5");
        //
        //        //  if ((pawnSave != null) && (pawnSave.TargetApparel != null))
        //        //  {
        //        //      text_ApparelName = pawnCalc.ApparelScoreRaw(apparel).ToString("N5") + "   " + text_ApparelName;
        //        //  }
        //        if (SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced(apparel))
        //        {
        //            text_Score = "ApparelForcedLower".Translate();
        //            //   text_ApparelName = text_ApparelName + ", " + "ApparelForcedLower".Translate();
        //        }
        //    }
        //    Widgets.Label(rectScoreText, text_Score);
        //    Widgets.Label(rectApparelText, text_ApparelName);
        //    y += lineheight;
        //}


        #endregion Methods
    }
}