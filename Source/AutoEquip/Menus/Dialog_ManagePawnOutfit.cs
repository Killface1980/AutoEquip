using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class Dialog_ManagePawnOutfit : Window
//    public class Dialog_ManagePawnOutfit : Window
    {
        private readonly List<Saveable_Pawn_StatDef> _stats;
        private Vector2 _scrollPositionStats;

        public Dialog_ManagePawnOutfit(List<Saveable_Pawn_StatDef> stats)
        {
            forcePause = true;
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            _stats = stats;
        }

        public override Vector2 InitialSize
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
            Dialog_ManageOutfitsAutoEquip.DoStatsInput(rect1, ref _scrollPositionStats, _stats);
            GUI.EndGroup();
        }
    }
}