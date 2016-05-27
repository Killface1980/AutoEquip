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

            MethodInfo coreDialogManageOutfits = typeof(Dialog_ManageOutfits).GetMethod("DoWindowContents", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo autoEquipDialogManageOutfits = typeof(Dialog_ManageOutfitsAutoEquip).GetMethod("DoWindowContents", BindingFlags.Instance | BindingFlags.Public);

            try
            {
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