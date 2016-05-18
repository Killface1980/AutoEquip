using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class ITab_Pawn_AutoEquip : ITab
    {
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

        public override bool IsVisible { get { return this.SelPawnForGear.RaceProps.ToolUser; } }
        private bool CanEdit { get { return this.SelPawnForGear.IsColonistPlayerControlled; } }

        private Pawn SelPawnForGear
        {
            get
            {
                if (base.SelPawn != null)
                {
                    return base.SelPawn;
                }
                Corpse corpse = base.SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.innerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + base.SelThing);
            }
        }

        public ITab_Pawn_AutoEquip()
        {
            this.size = new Vector2(540f, 550f);
            this.labelKey = "TabGear";
        }

        protected override void FillTab()
        {
            Saveable_Pawn pawnSave;
            PawnCalcForApparel pawnCalc;
            if (this.SelPawnForGear.IsColonist)
            {
                pawnSave = MapComponent_AutoEquip.Get.GetCache(this.SelPawnForGear);
                pawnCalc = new PawnCalcForApparel(pawnSave);
            }
            else
            {
                pawnSave = null;
                pawnCalc = null;
            }

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, this.size.x, this.size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, position.width, position.height);

            if (pawnSave != null)
            {
                Rect rect3 = new Rect(outRect.xMin + 4f, outRect.yMin, 100f, 30f);
                if (Widgets.TextButton(rect3, pawnSave.pawn.outfits.CurrentOutfit.label, true, false))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Outfit current in Find.Map.outfitDatabase.AllOutfits)
                    {
                        Outfit localOut = current;
                        list.Add(new FloatMenuOption(localOut.label, delegate
                        {
                            pawnSave.pawn.outfits.CurrentOutfit = localOut;
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

                outRect.yMin += rect3.height + 4f;
            }

            Rect viewRect = new Rect(0f, 0f, position.width - 16f, this.scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float num = 0f;
            if (this.SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in this.SelPawnForGear.equipment.AllEquipment)
                    this.DrawThingRow(ref num, viewRect.width, current, true, ITab_Pawn_AutoEquip.ThingLabelColor, pawnSave, pawnCalc);
            }
            if (this.SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in this.SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                    this.DrawThingRow(ref num, viewRect.width, current2, true, ITab_Pawn_AutoEquip.ThingLabelColor, pawnSave, pawnCalc);
            }
            if (pawnSave != null)
            {
                if ((pawnSave.ToWearApparel != null) &&
                    (pawnSave.ToWearApparel.Any()))
                {
                    Widgets.ListSeparator(ref num, viewRect.width, "ToWear".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToWearApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        this.DrawThingRow(ref num, viewRect.width, current2, false, ITab_Pawn_AutoEquip.ThingToEquipLabelColor, pawnSave, pawnCalc);
                }

                if ((pawnSave.ToDropApparel != null) &&
                    (pawnSave.ToDropApparel.Any()))
                {
                    Widgets.ListSeparator(ref num, viewRect.width, "ToDrop".Translate());
                    foreach (Apparel current2 in from ap in pawnSave.ToDropApparel
                                                 orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                                 select ap)
                        this.DrawThingRow(ref num, viewRect.width, current2, this.SelPawnForGear.apparel.WornApparel.Contains(current2), ITab_Pawn_AutoEquip.ThingToDropLabelColor, pawnSave, pawnCalc);
                }
            }
            if (this.SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());
                foreach (Thing current3 in this.SelPawnForGear.inventory.container)
                    this.DrawThingRow(ref num, viewRect.width, current3, true, ITab_Pawn_AutoEquip.ThingLabelColor, pawnSave, pawnCalc);
            }

            if (Event.current.type == EventType.Layout)
                this.scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool equiped, Color thingColor, Saveable_Pawn pawnSave, PawnCalcForApparel pawnCalc)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (Widgets.InvisibleButton(rect) && Event.current.button == 1)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }, MenuOptionPriority.Medium, null, null));
                if (this.CanEdit && equiped)
                {
                    Action action = null;
                    ThingWithComps eq = thing as ThingWithComps;
                    Apparel ap = thing as Apparel;
                    if (ap != null)
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
                        Find.WindowStack.Add(new Dialog_PawnApparelDetail(pawnSave.pawn, (Apparel)thing));
                    }, MenuOptionPriority.Medium, null, null));

                    list.Add(new FloatMenuOption("AutoEquip Comparer", delegate
                    {
                        Find.WindowStack.Add(new Dialog_PawnApparelComparer(pawnSave.pawn, (Apparel)thing));
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
            if (thing is Apparel)
            {
                if ((pawnSave != null) &&
                    (pawnSave.TargetApparel != null))
                    text = pawnCalc.CalculateApparelScoreRaw((Apparel)thing).ToString("N5") + "   " + text;

                if (this.SelPawnForGear.outfits != null && this.SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
                    text = text + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label(rect2, text);
            y += 28f;
        }
    }
}