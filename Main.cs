using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using Type = System.Type;
using Activator = System.Activator;
using Exception = System.Exception;
using System;
using UnityEngine;

namespace ToolsOfTheTrade
{
    internal class Main : MelonMod
    {

        public static readonly string mainCategoryName = "Tools of the Trade";
        public static readonly string mainCategoryDebug = "Tools of the Trade/Debug";
        internal static AssetBundle assets;
        internal static readonly HarmonyLib.Harmony harmony = new("Slacking.ToolsOfTheTrade");
        internal static readonly IEnumerable<Type> submoduleTypeList = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass
                                                                                               && type.IsSubclassOf(typeof(SlackingBase))
                                                                                               && (!type.IsAbstract));
        internal static readonly IEnumerable<SlackingBase> submoduleList = submoduleTypeList.Select(type => (SlackingBase)Activator.CreateInstance(type));
        //public override void OnEarlyInitializeMelon()
        //{
        //    InteractiveValue.RegisterIValueType<BigChungus.Settings.InteractiveCustomCard>();
        //}
        public override void OnInitializeMelon()
        {
            foreach (SlackingBase submodule in submoduleList)
            {
                submodule.RegisterSettings();
            }
        }
        public override void OnLateInitializeMelon()
        {
            if (Singleton<Game>.Instance == null)
            {
                LoggerInstance.Msg($"failed to initialise");
                return;
            }
            try
            {
                NeonLite.Modules.Anticheat.Register(this.MelonAssembly);
                PatchSubmodules();
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"failed to initialise {e}");
                throw;
            }
            LoggerInstance.Msg("Completed setup.");
        }
        private void PatchSubmodules()
        {
            foreach (var submodule in submoduleList)
            {
                HarmonyLib.Tools.HarmonyFileLog.Enabled = true;
                submodule.TryPatch();
            }
        }
        //private void LoadAssets()
        //{
        //    if (assets == null)
        //    {
        //        assets = AssetBundle.LoadFromMemory(Resources.Resources.toolsofthetrade);
        //        if (assets == null)
        //        {
        //            throw new ArgumentException("failed to load AssetBundle");
        //        }
        //        else
        //        {
        //            LoggerInstance.Msg($"Loaded {assets.GetAllAssetNames().Length} assets");
        //        }
        //    }
        //}
    }
}
