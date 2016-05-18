using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;


namespace AutoEquip
{
    public class Dialog_PawnApparelDetail : Window
    {
        private Pawn pawn;
        private Apparel apparel;

        public Dialog_PawnApparelDetail(Pawn pawn, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;

            this.pawn = pawn;
            this.apparel = apparel;
        }

        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        private Vector2 scrollPosition;

        public override void DoWindowContents(Rect windowRect)
        {
            PawnCalcForApparel conf = new PawnCalcForApparel(this.pawn);

            Rect groupRect = windowRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);

            float baseValue = 100f;
            float multiplierWidth = 100f;
            float finalValue = 120f;
            float labelWidth = groupRect.width - baseValue - multiplierWidth - finalValue - 8f - 8f;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, Text.LineHeight * 1.2f);

            this.DrawLine(ref itemRect,
                "Status", labelWidth,
                "Base", baseValue,
                "Strengh", multiplierWidth,
                "Final", finalValue);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f + 5f;

            Saveable_Outfit_StatDef[] stats = conf.Stats.ToArray();
            Rect viewRect = new Rect(groupRect.xMin, groupRect.yMin, groupRect.width - 16f, stats.Length * Text.LineHeight * 1.2f + 16f);
            if (viewRect.height < groupRect.height)
                groupRect.height = viewRect.height;

            Rect listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref scrollPosition, viewRect);

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

                float value = conf.GetStatValue(apparel, stat);
                sumValue += value;

                this.DrawLine(ref itemRect,
                    stat.StatDef.label, labelWidth,
                    value.ToString("N3"), baseValue,
                    stat.Strength.ToString("N2"), multiplierWidth,
                    (value * stat.Strength).ToString("N5"), finalValue);

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            itemRect = new Rect(listRect.xMin, groupRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            this.DrawLine(ref itemRect,
                "AverageStat".Translate(), labelWidth,
                (sumValue / stats.Length).ToString("N3"), baseValue,
                "", multiplierWidth,
                conf.CalculateApparelScoreRawStats(apparel).ToString("N5"), finalValue);

            itemRect.yMax += 5;

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            this.DrawLine(ref itemRect,
                "AutoEquipHitPoints".Translate(), labelWidth,
                conf.ApparelScoreRawHitPointAjust(apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            this.DrawLine(ref itemRect,
                "AutoEquipTemperature".Translate(), labelWidth,
                conf.CalculateApparelScoreRawInsulationColdAjust(apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            this.DrawLine(ref itemRect,
                "AutoEquipTotal".Translate(), labelWidth,
                conf.CalculateApparelModifierRaw(apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                conf.CalculateApparelScoreRaw(apparel).ToString("N5"), finalValue);

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