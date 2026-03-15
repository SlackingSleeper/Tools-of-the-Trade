using MelonLoader;

namespace ToolsOfTheTrade
{
    internal class Settings
    {
        public static MelonPreferences_Category Selection;

        public static MelonPreferences_Entry<bool> MineLayer;
        public static MelonPreferences_Entry<bool> AirZooka;
        public static MelonPreferences_Entry<bool> SpinAttack;
        public static MelonPreferences_Entry<bool> SwapBoof;

        public static void Register()
        {
            Selection = MelonPreferences.CreateCategory("Selection");

            MineLayer = Selection.CreateEntry("MineLayer", true);
            AirZooka = Selection.CreateEntry("AirZooka", true);
            SpinAttack = Selection.CreateEntry("SpinAttack", true);
            //weaponName = Selection.CreateEntry("weaponName", true);


        }

    }
}
