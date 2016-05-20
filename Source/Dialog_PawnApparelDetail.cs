using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class DialogPawnApparelDetail : Window
    {
        private readonly Pawn _pawn;
        private readonly Apparel _apparel;

        public DialogPawnApparelDetail(Pawn pawn, Apparel apparel)
        {
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;

            this._pawn = pawn;
            this._apparel = apparel;
        }

        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        private Vector2 _scrollPosition;

        public override void DoWindowContents(Rect windowRect)
        {
            PawnCalcForApparel conf = new PawnCalcForApparel(_pawn);

            Rect groupRect = windowRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);

            float baseValue = 100f;
            float multiplierWidth = 100f;
            float finalValue = 120f;
            float labelWidth = groupRect.width - baseValue - multiplierWidth - finalValue - 8f - 8f;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, Text.LineHeight * 1.2f);

            DrawLine(ref itemRect,
                "Status", labelWidth,
                "Base", baseValue,
                "Strengh", multiplierWidth,
                "Final", finalValue);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f + 5f;

            Saveable_Outfit_StatDef[] stats = PawnCalcForApparel.Stats.ToArray();
            Saveable_Outfit_StatDef[] workstats = PawnCalcForApparel.WorkStats.ToArray();


            Rect viewRect = new Rect(groupRect.xMin, groupRect.yMin, groupRect.width - 16f, stats.Length + workstats.Length * Text.LineHeight * 1.2f + 16f);
            if (viewRect.height < groupRect.height)
                groupRect.height = viewRect.height;

            Rect listRect = viewRect.ContractedBy(4f);


            // Detail list scrollable

            Widgets.BeginScrollView(groupRect, ref _scrollPosition, viewRect);

            float sumValue = 0;

            foreach (Saveable_Outfit_StatDef stat in stats)
            {

                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }
                float value = PawnCalcForApparel.GetStatValue(_apparel, stat);
                sumValue += value;


                DrawLine(ref itemRect,
                    stat.StatDef.label, labelWidth,
                    value.ToString("N3"), baseValue,
                    stat.Strength.ToString("N2"), multiplierWidth,
                    (value * stat.Strength).ToString("N5"), finalValue);

                listRect.yMin = itemRect.yMax;
                Widgets.DrawLineHorizontal(groupRect.xMin, listRect.yMin+0.3f, groupRect.width);

            }


            foreach (Saveable_Outfit_StatDef workstat in workstats)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }
                float value = PawnCalcForApparel.GetStatValue(_apparel, workstat);
                sumValue += value;

                DrawLine(ref itemRect,
                    workstat.StatDef.label, labelWidth,
                    value.ToString("N3"), baseValue,
                    workstat.Strength.ToString("N2"), multiplierWidth,
                    (value * workstat.Strength).ToString("N5"), finalValue);

                listRect.yMin = itemRect.yMax;

            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            itemRect = new Rect(listRect.xMin, groupRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            Saveable_Outfit outfit = MapComponent_AutoEquip.Get.GetOutfit(_pawn.outfits.CurrentOutfit);
            DrawLine(ref itemRect,
                "AverageStat".Translate(), labelWidth,
                (sumValue / stats.Length + workstats.Length).ToString("N3"), baseValue,
                "", multiplierWidth,
                PawnCalcForApparel.ApparelScoreRawStats(_apparel).ToString("N5"), finalValue);

            itemRect.yMax += 5;

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipHitPoints".Translate(), labelWidth,
                conf.ApparelScoreRawHitPointAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTemperature".Translate(), labelWidth,
                PawnCalcForApparel.ApparelScoreRawInsulationColdAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            //    itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            //    DrawLine(ref itemRect,
            //        "AutoEquipWorkstats".Translate(), labelWidth,
            //        PawnCalcForApparel.ApparelScoreRawWorkStats(_pawn, _apparel).ToString("N3"), baseValue,
            //        "", multiplierWidth,
            //        "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTotal".Translate(), labelWidth,
                conf.ApparelModifierRaw(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                conf.ApparelScoreRaw(_apparel).ToString("N5"), finalValue);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLine(ref Rect itemRect,
            string statDefLabelText, float statDefLabelWidth,
            string statDefValueText, float statDefValueWidth,
            string multiplierText, float multiplierWidth,
            string finalValueText, float finalValueWidth)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefLabelWidth, itemRect.height), statDefLabelText);
            itemRect.xMin += statDefLabelWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefValueWidth, itemRect.height), statDefValueText);
            itemRect.xMin += statDefValueWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, multiplierWidth, itemRect.height), multiplierText);
            itemRect.xMin += multiplierWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, finalValueWidth, itemRect.height), finalValueText);
            itemRect.xMin += finalValueWidth;
        }


    }
}