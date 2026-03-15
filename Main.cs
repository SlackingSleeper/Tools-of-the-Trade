using HarmonyLib;//Patching functions
using MelonLoader;//Required

//using UniverseLib.Input;//If you want to handle input

namespace ToolsOfTheTrade
{
    internal class Main : MelonMod
    {
        public static Game Game
        {
            get; private set;
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
                Settings.Register();
                PatchGame();
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"failed to initialise", e);
                throw e;
            }
            LoggerInstance.Msg("Completed setup.");
        }
        private void PatchGame()
        {
            HarmonyLib.Harmony harmony = new("Slacking.ToolsOfTheTrade");
            harmony.PatchAll();
        }
    }
}
