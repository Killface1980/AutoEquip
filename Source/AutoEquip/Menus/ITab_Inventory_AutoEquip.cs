using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
        private const float _margin = 15f;
        private const float _topPadding = 15f;

        private static readonly Color _highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color _thingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private const float TopPadding = 20f;
        private const float ThingIconSize = 28f;
        private const float ThingRowHeight = 28f;
        private const float ThingLeftX = 36f;

        private Vector2 _scrollPosition = Vector2.zero;

        private float _scrollViewHeight;

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

        public ITab_Pawn_AutoEquip() : base()
        {
            size = new Vector2(432f, 600f);
            labelKey = "TabGear";
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
            // set up rects
            Rect listRect = new Rect(
                _margin,
                _topPadding,
                size.x - 2 * _margin,
                size.y - _topPadding - _margin);

            #region CR Stuff

            // get the inventory comp
            CompInventory comp = SelPawn.TryGetComp<CompInventory>();

            if (comp != null)
            {
                // adjust rects if comp found
                listRect.height -= (_margin * 1.25f + _barHeight * 2);
                Rect weightRect = new Rect(_margin, listRect.yMax + _margin, listRect.width, _barHeight);
                Rect bulkRect = new Rect(_margin, weightRect.yMax + _margin * 0.5f, listRect.width, _barHeight);

                Utility_Loadouts.DrawBar(weightRect, comp.currentWeight, comp.capacityWeight, "CR.Weight".Translate(), SelPawn.GetWeightTip());
                Utility_Loadouts.DrawBar(bulkRect, comp.currentBulk, comp.capacityBulk, "CR.Bulk".Translate(), SelPawn.GetBulkTip());
            }

            #endregion CR Stuff

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
                FillTabVanilla();
                return;
            }

            Text.Font = GameFont.Small;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;


            GUI.BeginGroup(listRect);


            // main canvas
            Rect header = new Rect(listRect.x, _topPadding, listRect.width, listRect.height);
            //            Rect header = new Rect(listRect.x, listRect.yMin, listRect.width, listRect.height);

            Vector2 cur = Vector2.zero;
            cur.y += 35f;

            if (pawnSave != null)
            {
                // Outfit + Status button
                Rect rectStatus = new Rect(0f, 0f, 100f, 30f);

                // select outfit

                if (Widgets.TextButton(rectStatus, pawnSave.Pawn.outfits.CurrentOutfit.label, true, false))
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
                //edit outfit
                rectStatus = new Rect(rectStatus.xMax + _margin, rectStatus.y, rectStatus.width, rectStatus.height);

                if (Widgets.TextButton(rectStatus, "OutfitEdit".Translate(), true, false))
                {
                    Find.WindowStack.Add(new Dialog_ManageOutfits(SelPawn.outfits.CurrentOutfit));
                }


                Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(SelPawn);

                if (outfit.AppendIndividualPawnStatus)
                {
                    rectStatus = new Rect(rectStatus.xMax + _margin, rectStatus.y, rectStatus.width, rectStatus.height);

                    if (Widgets.TextButton(rectStatus, "AutoEquipStatus".Translate(), true, false))
                    {
                        if (pawnSave.Stats == null)
                            pawnSave.Stats = new List<Saveable_Outfit_StatDef>();
                        Find.WindowStack.Add(new Dialog_ManagePawnOutfit(pawnSave.Stats));
                    }
                }


                #region Fixed  Header


                // Equipment
                float posX = 0f;
                if (SelPawnForGear.equipment.AllEquipment.Any() || SelPawnForGear.inventory.container.Count > 0)
                {
                    Widgets.ListSeparator(ref cur.y, header.width, "EquipmentAndInventory".Translate());

                    if (SelPawnForGear.equipment != null)
                    {
                        foreach (ThingWithComps thing in SelPawnForGear.equipment.AllEquipment)
                            DrawWeaponIcon(ref posX, ref cur.y, 80f, thing);
                    }
                    //Inventory
                    if (SelPawnForGear.inventory != null)
                    {
                        foreach (Thing current3 in SelPawnForGear.inventory.container)
                            DrawInventoryIcon(ref posX, ref cur.y, current3);

                        if (SelPawnForGear.equipment == null)
                            cur.y += 80f;
                    }
                }
                if (cur.y < 10f)
                    cur.y = 20f;

                if (posX > 0f)
                    cur.y += 60f;


                // temeprature header

                Widgets.ListSeparator(ref cur.y, header.width, "Apparel".Translate());
                cur.y += 5f;
                Widgets.ListSeparator(ref cur.y, header.width - _margin * 2, "PreferedTemperature".Translate());
                //          Text.Anchor = TextAnchor.UpperLeft;


                // some padding
                cur.y += 15f;

                // temperature slider
                ApparelStatCache pawnStatCache = SelPawn.GetApparelStatCache();
                FloatRange targetTemps = pawnStatCache.TargetTemperatures;
                FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;

                Rect sliderRect = new Rect(cur.x, cur.y, header.width - _margin * 4.5f, 40f);

                Rect tempResetRect = new Rect(header.xMax - _margin * 4 - 1f, cur.y + 12f, 16f, 16f);
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

                Text.Font = GameFont.Small;

                #endregion Fixed Header

            }
            cur.y += 15f;
            listRect.yMin += cur.y;
            listRect.height -= _margin/2f;
            //      header.height += cur.y;

            Text.Font = GameFont.Small;

            listRect.x = 0;
            //       listRect.width -= _margin;

            Rect apparelGroupRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, listRect.height - _margin);
            Rect apparelListRect = new Rect(0f, 0f, listRect.width - _margin * 2, _scrollViewHeight);

            Widgets.BeginScrollView(apparelGroupRect, ref _scrollPosition, apparelListRect);

            float posY = 0f;

            //Apparel
            if (SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref posY, apparelListRect.width, "WornApparel".Translate());
                foreach (Apparel current2 in from ap in SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                    DrawThingRow(ref posY, apparelListRect.width, current2, true, ThingLabelColor, pawnSave, pawnCalc);
            }

            if (pawnSave != null)
            {
                if ((pawnSave.ToWearApparel != null) &&
                    (pawnSave.ToWearApparel.Any()))
                {
                    Widgets.ListSeparator(ref posY, apparelListRect.width, "ToWear".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToWearApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        DrawThingRow(ref posY, apparelListRect.width, current2, false, ThingToEquipLabelColor, pawnSave, pawnCalc);
                }

                if ((pawnSave.ToDropApparel != null) &&
                    (pawnSave.ToDropApparel.Any()))
                {
                    Widgets.ListSeparator(ref posY, apparelListRect.width, "ToDrop".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToDropApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        DrawThingRow(ref posY, apparelListRect.width, current2, SelPawnForGear.apparel != null && SelPawnForGear.apparel.WornApparel.Contains(current2), ThingToDropLabelColor, pawnSave, pawnCalc);
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                _scrollViewHeight = posY;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();

            if (_scrollViewHeight>apparelGroupRect.height)
            {
                GUI.color = Color.gray;
                Widgets.DrawLineHorizontal(_margin, listRect.yMax, apparelListRect.width);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected void FillTabVanilla()
        {
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

            // start drawing list (rip from ITab_Pawn_Gear)
            GUI.BeginGroup(listRect);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, listRect.width, listRect.height);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, this._scrollViewHeight);

            Widgets.BeginScrollView(outRect, ref this._scrollPosition, viewRect);
            float curY = 0f;
            if (this.SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in this.SelPawnForGear.equipment.AllEquipment)
                {
                    this.DrawThingRowVanilla(ref curY, viewRect.width, current);
                }
            }
            if (this.SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in this.SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    this.DrawThingRowVanilla(ref curY, viewRect.width, current2);
                }
            }
            if (this.SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref curY, viewRect.width, "Inventory".Translate());
                foreach (Thing current3 in this.SelPawnForGear.inventory.container)
                {
                    this.DrawThingRowVanilla(ref curY, viewRect.width, current3);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                this._scrollViewHeight = curY + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }


        private float _lastClick;

        private void DrawThingRowVanilla(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            TooltipHandler.TipRegion(rect, thing.GetWeightAndBulkTip());
            if (Mouse.IsOver(rect))
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (Widgets.InvisibleButton(rect) && Event.current.button == 1)
            {
                List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }, MenuOptionPriority.Medium, null, null));
                if (this.CanEdit)
                {
                    // Equip option
                    ThingWithComps eq = thing as ThingWithComps;
                    if (eq != null && eq.TryGetComp<CompEquippable>() != null)
                    {
                        CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
                        if (compInventory != null)
                        {
                            FloatMenuOption equipOption;
                            string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
                            if (SelPawnForGear.equipment.AllEquipment.Contains(eq) && SelPawnForGear.inventory != null)
                            {
                                equipOption = new FloatMenuOption("CR_PutAway".Translate(new object[] { eqLabel }),
                                    new Action(delegate
                                    {
                                        ThingWithComps oldEq;
                                        SelPawnForGear.equipment.TryTransferEquipmentToContainer(SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.container, out oldEq);
                                    }));
                            }
                            else if (!SelPawnForGear.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                equipOption = new FloatMenuOption("CannotEquip".Translate(new object[] { eqLabel }), null);
                            }
                            else
                            {
                                string equipOptionLabel = "Equip".Translate(new object[] { eqLabel });
                                if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
                                {
                                    equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                }
                                equipOption = new FloatMenuOption(equipOptionLabel, new Action(delegate
                                {
                                    compInventory.TrySwitchToWeapon(eq);
                                }));
                            }
                            floatOptionList.Add(equipOption);
                        }
                    }

                    // Drop option
                    Action action = null;
                    Apparel ap = thing as Apparel;
                    if (ap != null && SelPawnForGear.apparel.WornApparel.Contains(ap))
                    {
                        Apparel unused;
                        action = delegate
                        {
                            this.SelPawnForGear.apparel.TryDrop(ap, out unused, this.SelPawnForGear.Position, true);
                        };
                    }
                    else if (eq != null && this.SelPawnForGear.equipment.AllEquipment.Contains(eq))
                    {
                        ThingWithComps unused;
                        action = delegate
                        {
                            this.SelPawnForGear.equipment.TryDropEquipment(eq, out unused, this.SelPawnForGear.Position, true);
                        };
                    }
                    else if (!thing.def.destroyOnDrop)
                    {
                        Thing unused;
                        action = delegate
                        {
                            this.SelPawnForGear.inventory.container.TryDrop(thing, this.SelPawnForGear.Position, ThingPlaceMode.Near, out unused);
                        };
                    }
                    floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
                }
                FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap, false, false);
                Find.WindowStack.Add(window);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = _thingLabelColor;
            Rect rect2 = new Rect(36f, y, width - 36f, 28f);
            string text = thing.LabelCap;
            if (thing is Apparel && this.SelPawnForGear.outfits != null && this.SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label(rect2, text);
            y += 28f;
        }


        private void DrawThingRow(ref float y, float width, Thing thing, bool equiped, Color thingColor, SaveablePawn pawnSave, PawnCalcForApparel pawnCalc)
        {
            var lineheight = 40f;
            Rect rect = new Rect(0f, y, width, lineheight);

            TooltipHandler.TipRegion(rect, thing.GetWeightAndBulkTip());

            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            #region Button Clikcks

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

            #endregion Button Clicks

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

        private void DrawWeaponIcon(ref float x, ref float y, float width, Thing thing)
        {
            var lineheight = 60f;

            Rect rectIconBox = new Rect(x, y, lineheight, lineheight);
            Rect rectIcon = new Rect(rectIconBox.x + 6f, rectIconBox.y + 6f, rectIconBox.width - 12f, rectIconBox.height - 12f);

            TooltipHandler.TipRegion(rectIconBox, thing.GetWeightAndBulkTip());

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(rectIcon, thing);
            }

            TooltipHandler.TipRegion(rectIconBox, thing.LabelCap);

            if (Mouse.IsOver(rectIconBox))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rectIconBox, TexUI.HighlightTex);
            }


            RightMouseButtonClick(thing, rectIconBox);


            if (SelPawnForGear.inventory != null)
                x += lineheight + 20f;


        }

        private void DrawInventoryIcon(ref float x, ref float y, Thing thing)
        {
            var lineheight = 60f;
            Rect rectIconBox = new Rect(x, y + 17f, lineheight * 0.5f, lineheight * 0.5f);
            Rect rectIcon = new Rect(rectIconBox.x + 6f, rectIconBox.y + 6f, rectIconBox.width - 12f, rectIconBox.height - 12f);

            TooltipHandler.TipRegion(rectIconBox, thing.GetWeightAndBulkTip());

            RightMouseButtonClick(thing, rectIconBox);

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(rectIcon, thing);
            }

            if (Mouse.IsOver(rectIconBox))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rectIconBox, TexUI.HighlightTex);
            }
            x += lineheight * 0.5f + 5f;

            if (x > 400f)
            {
                y += lineheight;
                x = 0f;
            }

        }


        private void RightMouseButtonClick(Thing thing, Rect rect)
        {
            if (Widgets.InvisibleButton(rect))
                if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(thing));
                    }, MenuOptionPriority.Medium, null, null));

                    if (CanEdit)
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
                    FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap, false, false);
                    Find.WindowStack.Add(window);
                }
        }

        #endregion Methods

    }
}