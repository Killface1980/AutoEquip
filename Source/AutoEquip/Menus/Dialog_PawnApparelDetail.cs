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
            Saveable_Outfit_WorkStatDef[] workstats = conf.WorkStats.ToArray();
            List<Saveable_Outfit_WorkStatDef> filteredworkstats = new List<Saveable_Outfit_WorkStatDef>();

            foreach (var workstat in workstats)
            {
                float value = conf.GetWorkStatValue(_apparel, workstat);

                if (value <= 0.99f || value >= 1.01f)
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

                var statStrengthDialog = stat.Strength;
                var valueDisplay = value;

                if (stat.Strength < 0) // flipped for calc + *-1
                {
                    statStrengthDialog = statStrengthDialog*-1;
                    valueDisplay = 1/value;
                    sumStatsValue += valueDisplay;
                }
                else sumStatsValue += value;

                float statscore = valueDisplay * statStrengthDialog;

                if (valueDisplay == 1)
                    statscore = 1;

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
                Widgets.DrawLineHorizontal(groupRect.xMin, listRect.yMin, groupRect.width);
                listRect.yMin = itemRect.yMax;
            }


            foreach (Saveable_Outfit_WorkStatDef workstat in filteredworkstats)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, Text.LineHeight * 1.2f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                float value = conf.GetWorkStatValue(_apparel, workstat);

                if (value <= 0.99f || value >= 1.01f)
                {
                    sumWorkStatsValue += value;

                    DrawLine(ref itemRect,
                        workstat.StatDef.label, labelWidth,
                        value.ToString("N3"), baseValue,
                        workstat.Strength.ToString("N2"), multiplierWidth,
                        (value * workstat.Strength).ToString("N5"), finalValue);

                    listRect.yMin = itemRect.yMax;
                }



            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            //          itemRect.yMax += 5; 

            itemRect = new Rect(listRect.xMin, groupRect.yMax + 5, listRect.width, Text.LineHeight * 1.2f);

            if (sumStatsValue > 0)
            {
                DrawLine(ref itemRect,
                "AverageStat".Translate(), labelWidth,
                (sumStatsValue / stats.Length).ToString("N3"), baseValue,
                "", multiplierWidth,
                conf.ApparelScoreRaw_PawnStats(_apparel).ToString("N5"), finalValue);
                itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            }
            if (sumWorkStatsValue > 0)
            {
                DrawLine(ref itemRect,
                "AverageWorkStat".Translate(), labelWidth,
                (sumWorkStatsValue / filteredworkstats.Count).ToString("N3"), baseValue,
                "", multiplierWidth,
                conf.ApparelScoreRaw_PawnWorkStats(_apparel).ToString("N5"), finalValue);
                itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            }
            DrawLine(ref itemRect,
                "AutoEquipHitPoints".Translate(), labelWidth,
                conf.ApparelScoreRawHitPointAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTemperature".Translate(), labelWidth,
                conf.ApparelScoreRawInsulationColdAdjust(_apparel).ToString("N3"), baseValue,
                "", multiplierWidth,
                "", finalValue);

            itemRect = new Rect(listRect.xMin, itemRect.yMax, listRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "AutoEquipTotal".Translate(), labelWidth,
                conf.DIALOGONLY_ApparelModifierRaw(_apparel).ToString("N3"), baseValue,
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