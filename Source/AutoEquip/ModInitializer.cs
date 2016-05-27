﻿using System;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class ModInitializer : ITab
    {
        protected GameObject _modInitializerControllerObject;

        public ModInitializer()
        {
            _modInitializerControllerObject = new GameObject("ModInitializer");
            _modInitializerControllerObject.AddComponent<ModInitializerBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(_modInitializerControllerObject);
        }

        protected override void FillTab() { }
    }

    class ModInitializerBehaviour : MonoBehaviour
    {
        protected bool _reinjectNeeded = false;
        protected float _reinjectTime = 0;

        public void OnLevelWasLoaded(int level)
        {
            _reinjectNeeded = true;
            if (level >= 0)
                _reinjectTime = 1;
            else
                _reinjectTime = 0;
        }

        public void FixedUpdate()
        {
            if (_reinjectNeeded)
            {
                _reinjectTime -= Time.fixedDeltaTime;

                if (_reinjectTime <= 0)
                {
                    _reinjectNeeded = false;
                    _reinjectTime = 0;

#if LOG
                    Log.Message("AutoEquip Injected");
#endif
                    MapComponent_AutoEquip component = MapComponent_AutoEquip.Get;
                }
            }
        }

        public void Start()
        {
            MethodInfo coreMethod = typeof(JobGiver_OptimizeApparel).GetMethod("TryGiveTerminalJob", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo autoEquipMethod = typeof(AutoEquip_JobGiver_OptimizeApparel).GetMethod("TryGiveTerminalJob", BindingFlags.Instance | BindingFlags.NonPublic);


            try
            {
                Detours.TryDetourFromTo(coreMethod, autoEquipMethod);
            }
            catch (Exception)
            {
                Log.Error("Could not Detour AutoEquip.");
                throw;
            }
            MethodInfo coreDialogManageOutfits = typeof(Dialog_ManageOutfits).GetMethod("DoWindowContents", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo autoEquipDialogManageOutfits = typeof(Dialog_ManageOutfitsAutoEquip).GetMethod("DoWindowContents", BindingFlags.Instance | BindingFlags.Public);

            try
            {
                Detours.TryDetourFromTo(coreDialogManageOutfits, autoEquipDialogManageOutfits);
            }
            catch (Exception)
            {
                Log.Error("Could not Detour AutoEquip Outfit Window.");
                throw;
            }




            OnLevelWasLoaded(-1);
        }
    }

}
