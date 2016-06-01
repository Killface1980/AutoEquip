using System;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace AutoEquip
{
    class CCL_Injectors : SpecialInjector
    {
        public override bool Inject()
        {
            MethodInfo coreMethod = typeof(JobGiver_OptimizeApparel).GetMethod("TryGiveTerminalJob", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo autoEquipMethod = typeof(AutoEquip_JobGiver_OptimizeApparel).GetMethod("TryGiveTerminalJob", BindingFlags.Instance | BindingFlags.NonPublic);

            MethodInfo coreDialogManageOutfits = typeof(Dialog_ManageOutfits).GetMethod("DoWindowContents", BindingFlags.CreateInstance | BindingFlags.Public);
            MethodInfo autoEquipDialogManageOutfits = typeof(Dialog_ManageOutfitsAutoEquip).GetMethod("DoWindowContents", BindingFlags.CreateInstance | BindingFlags.Public);

   //       MethodInfo source = typeof(JobGiver_OptimizeApparel).GetMethod("ApparelScoreRaw", BindingFlags.Static | BindingFlags.Public);
   //       MethodInfo destination = typeof(ApparelStatsHelper).GetMethod("ApparelScoreRaw", BindingFlags.Static | BindingFlags.Public);


            try
            {
     //       Detours.TryDetourFromTo(source, destination);
                Detours.TryDetourFromTo(coreMethod, autoEquipMethod);
            }
            catch (Exception)
            {
                Log.Error("Could not Detour AutoEquip.");
                throw;
            }

            Detours.TryDetourFromTo(coreDialogManageOutfits, autoEquipDialogManageOutfits);

            return true;

        }

    }
}