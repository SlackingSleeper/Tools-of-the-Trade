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

                PatchGame();
                /*Game.OnLevelLoadComplete += OnLevelLoadComplete;
                 *if (RM.drifter)
                 *{
                 *  OnLevelLoadComplete();
                 *}
                */
                Settings.Register();
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
            //patch stuff
        }
        /*private void OnLevelLoadComplete()
        {
            //game objects are cleared when the scene changes by default
            //this function is called when a level is loaded so that you
            //dont have to worry about your mods affecting the menus
        }*/
    }
}
