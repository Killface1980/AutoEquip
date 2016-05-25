using System.Collections.Generic;
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

            _pawn = pawn;
            _apparel = apparel;
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
                "Score", finalValue);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f + 5f;

            groupRect.height -= 60f; // test

            Saveable_Outfit_StatDef[] stats = conf.Stats.ToArray();
            Saveable_Pawn_WorkStatDef[] workstats = conf.WorkStats.ToArray();
            List<Saveable_Pawn_WorkStatDef> filteredworkstats = new List<Saveable_Pawn_WorkStatDef>();

            foreach (var workstat in workstats)
            {
                float value = conf.GetWorkStatValue(_apparel, workstat);

                if (value <= 0.999f || value >= 1.001f)
                {
                    filteredworkstats.Add(workstat);
                }
            }


            Rect viewRect = new Rect(groupRect.xMin, groupRect.yMin, groupRect.width - 16f, (stats.Length + filteredworkstats.Count + 1) * Text.LineHeight * 1.2f + 16f);
            if (viewRect.height < groupRect.height)
                groupRect.height = viewRect.height;

            Rect listRect = viewRect.ContractedBy(4f);


            // Detail list scrollable

            Widgets.BeginScrollView(groupRect, ref _scrollPosition, viewRect);

            float sumStatsValue = 0;
            float sumWorkStatsValue = 0;

            bool check = false;

            foreach (Saveable_Outfit_StatDef stat in stats)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }
                float value = conf.GetStatValue(_apparel, stat);

                var valueDisplay = value;
                var statStrengthDialog = stat.Strength;

                if (value != 0)
                {
                    if (value < 1) // flipped for calc + *-1
                    {
                        statStrengthDialog = statStrengthDialog * -1;
                        valueDisplay = 1 / value;
                        sumStatsValue += valueDisplay;
                    }
                    if (value != 1)
                        sumStatsValue += value;
                }

                float statscore = valueDisplay * statStrengthDialog;

                if (valueDisplay == 1)
                    statscore = 0;

                DrawLine(ref itemRect,
                    stat.StatDef.label, labelWidth,
                    value.ToString("N3"), baseValue,
                    stat.Strength.ToString("N2"), multiplierWidth,
                    statscore.ToString("N5"), finalValue);

                listRect.yMin = itemRect.yMax;

                check = true;

            }

            if (check)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 0.5f);
                Widgets.DrawLineHorizontal(itemRect.xMin, (itemRect.yMin + itemRect.yMax) / 2, labelWidth);
                DrawLine(ref itemRect, "", labelWidth, "", baseValue, "", multiplierWidth, "", finalValue);
                listRect.yMin = itemRect.yMax;

            }


            foreach (Saveable_Pawn_WorkStatDef workstat in filteredworkstats)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                float value = conf.GetWorkStatValue(_apparel, workstat);
                var workvalueDisplay = value;
                var workstatStrengthDialog = workstat.Strength;


                if (value < 1) // flipped for calc + *-1
                {
                    workstatStrengthDialog = workstatStrengthDialog * -1;
                    workvalueDisplay = 1 / value;
            //        sumWorkStatsValue += workvalueDisplay;
                }
           //     else if (value>1)
                    sumWorkStatsValue += value;


                if (value <= 0.999f || value >= 1.001f)
                {
                    DrawLine(ref itemRect,
                        workstat.StatDef.label, labelWidth,
                        value.ToString("N3"), baseValue,
                        workstatStrengthDialog.ToString("N2"), multiplierWidth,
                        (value * workstatStrengthDialog).ToString("N5"), finalValue);

                    listRect.yMin = itemRect.yMax;
                }



            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            //          itemRect.yMax += 5; 

            itemRect = new Rect(listRect.xMin, groupRect.yMax, listRect.width, Text.LineHeight * 0.6f);
            DrawLine(ref itemRect,
                "", labelWidth,
                "", baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "", labelWidth,
                "", baseValue,
                "Status Offset", multiplierWidth,
                "Subtotal", finalValue);

            //       itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 0.6f);
            //       Widgets.DrawLineHorizontal(itemRect.xMin, itemRect.yMax, itemRect.width);

            float subtotal = 1;

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "BasicStatusOfApparel".Translate(), labelWidth,
                "1.000", baseValue,
                "", multiplierWidth,
                "1.000", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);

            if (sumStatsValue > 0 && stats.Length > 0)
            {
                subtotal += conf.ApparelScoreRaw_PawnStats(_apparel);

                DrawLine(ref itemRect,
                "AverageStat".Translate(), labelWidth,
                (sumStatsValue / stats.Length).ToString("N3"), baseValue,
                conf.ApparelScoreRaw_PawnStats(_apparel).ToString("N5"), multiplierWidth,
                subtotal.ToString("N5"), finalValue);

                itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            }

            if (sumWorkStatsValue > 0)
            {
                subtotal += conf.ApparelScoreRaw_PawnWorkStats(_apparel);

                DrawLine(ref itemRect,
                "AverageWorkStat".Translate(), labelWidth,
                (sumWorkStatsValue / filteredworkstats.Count).ToString("N3"), baseValue,
                conf.ApparelScoreRaw_PawnWorkStats(_apparel).ToString("N5"), multiplierWidth,
                subtotal.ToString("N5"), finalValue);

                itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            }

            float armor = conf.ApparelScoreRaw_ProtectionBaseStat(_apparel)*0.25f;

            subtotal += armor;

            DrawLine(ref itemRect,
                "AutoEquipArmor".Translate(), labelWidth,
                "+", baseValue,
                armor.ToString("N4"), multiplierWidth,
                subtotal.ToString("N5"), finalValue);

            subtotal = subtotal * conf.ApparelScoreRawHitPointAdjust(_apparel);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipHitPoints".Translate(), labelWidth,
                conf.ApparelScoreRawHitPointAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                subtotal.ToString("N5"), finalValue);

            subtotal = subtotal * conf.ApparelScoreRawInsulationColdAdjust(_apparel);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTemperature".Translate(), labelWidth,
                conf.ApparelScoreRawInsulationColdAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                subtotal.ToString("N5"), finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTotal".Translate(), labelWidth,
                "", baseValue,
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