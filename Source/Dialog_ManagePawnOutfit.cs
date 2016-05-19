using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class Dialog_ManagePawnOutfit : Window
    {
        private List<Saveable_Outfit_StatDef> stats;
        private Vector2 scrollPositionStats;

        public Dialog_ManagePawnOutfit(List<Saveable_Outfit_StatDef> stats)
        {
            forcePause = true;
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            this.stats = stats;
        }

        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2(400f, 650f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, inRect.height - CloseButSize.y).ContractedBy(10f);
            GUI.BeginGroup(rect);
            Rect rect1 = new Rect(0f, 0f, rect.width, rect.height - 5f - 10f);
            Dialog_ManageOutfitsAutoEquip.DoStatsInput(rect1, ref scrollPositionStats, stats);
            GUI.EndGroup();
        }
    }
}