using HarmonyLib;//Patching functions
using MelonLoader;
using Steamworks;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using ToolsOfTheTrade.Weapons;
using UnityEngine;

//using UniverseLib.Input;//If you want to handle input

namespace ToolsOfTheTrade
{
    internal class Main : MelonMod
    {
        internal static readonly HarmonyLib.Harmony harmony = new("Slacking.ToolsOfTheTrade");
        internal static readonly IEnumerable<Type> submoduleTypeList = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass
                                                                                               && type.IsSubclassOf(typeof(SlackingBase))
                                                                                               && (!type.IsAbstract));
        internal static readonly IEnumerable<SlackingBase> submoduleList = submoduleTypeList.Select(type => (SlackingBase)Activator.CreateInstance(type));
        public static Game Game
        {
            get; private set;
        }
        public override void OnInitializeMelon()
        {
            foreach (SlackingBase submodule in submoduleList)
            {
                LoggerInstance.Msg(submodule.GetType());
                submodule.RegisterSettings();
            }
            LoggerInstance.Msg("Registered settings");
        }
        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            if (Game == null)
            {
                LoggerInstance.Msg($"failed to initialise");
                return;
            }
            try
            {
                PatchSubmodules();
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"failed to initialise {e}");
                throw;
            }
            LoggerInstance.Msg("Completed setup.");
        }
        public override void OnPreferencesSaved()
        {
            LoggerInstance.Msg("OnPreferencesSaved");
            foreach (var submodule in submoduleList)
            {
                //LoggerInstance.Msg($"{submodule.GetType()} patching");

                submodule.Patch();
                submodule.OnPreferencesSaved();
            }
        }
        private void PatchSubmodules()
        {
            foreach (var submodule in submoduleList)
            {
                //LoggerInstance.Msg($"{submodule.GetType()} patching");
                submodule.Patch();
            }
        }
    }
}
