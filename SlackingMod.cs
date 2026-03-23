using HarmonyLib;
using MelonLoader;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using UniverseLib;
using System.Collections.ObjectModel;

namespace ToolsOfTheTrade
{
    internal abstract class SlackingMod<DerivedType> : SlackingBase where DerivedType : SlackingMod<DerivedType>
    {
        public static bool patched = false;
        protected class ModSettings
        {
            public static MelonPreferences_Entry<bool> Debug;
            public static MelonPreferences_Entry<bool> Active;
        }
        public override void RegisterSettings()
        {
            var debugCategory = MelonPreferences.CreateCategory(Settings.mainCategoryDebug, is_hidden: true);
            ModSettings.Debug = debugCategory.CreateEntry($"{typeof(DerivedType).Name} debug messages", false);

            var regularCategory = MelonPreferences.CreateCategory(Settings.mainCategoryName);
            ModSettings.Active = regularCategory.CreateEntry($"Activate {typeof(DerivedType).Name}", true);
        }
        private static bool IsHarmonyPatch(CustomAttributeData attribute) => attribute.AttributeType == typeof(HarmonyPatch);
        private static bool HasHarmonyPatch(MemberInfo obj) => obj.CustomAttributes.Any(IsHarmonyPatch);

        private static (Type, (IEnumerable<CustomAttributeData>, HarmonyMethod)[])[] methodList = null;
        public static (Type, (IEnumerable<CustomAttributeData>, HarmonyMethod)[])[] MethodList
        {
            get
            {
                methodList ??= typeof(DerivedType).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                                                  .Where(type => HasHarmonyPatch(type))
                                                  .Select(type => ((Type)type.CustomAttributes
                                                                             .First(IsHarmonyPatch)
                                                                             .ConstructorArguments
                                                                             .First()
                                                                             .Value,
                                                                   type.GetMethods(AccessTools.allDeclared)
                                                                       .Where(HasHarmonyPatch)
                                                                       .Select(method => (method.CustomAttributes,
                                                                                          new HarmonyMethod(method)))
                                                                       .ToArray()))
                                                  .ToArray();
                return methodList;
            }
        }
        private static void Log(object message, string functionName)
        {
            Melon<Main>.Logger.Msg($"[{typeof(DerivedType).Name}][{functionName}]: {message}");
        }
        protected static void DebugLog(string message = "", [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            if (ModSettings.Debug.Value)
            {
                Log(message, functionName);
            }
        }
        //TODO:  add check for if the patch is in the state that is promised
        public override void Patch()
        {
            if (ModSettings.Active.Value == patched)
            {
                DebugLog($"skip patching");
                return;
            }
            if (ModSettings.Active.Value)
            {
                DebugLog($"patchList length: {MethodList.Length}");
                foreach ((Type type, var patchList) in MethodList)
                {
                    DebugLog($"{type.Name} patchList length: {patchList.Length}");
                    foreach (var (attributeList, patch) in patchList)
                    {
                        bool methodHasAttribute<T>() => attributeList.Any(attribute => attribute.AttributeType == typeof(T));
                        DebugLog($"{type.Name}::{patch.methodName} => {patch.method.Name}");

                        MethodInfo target;
                        if (patch.argumentTypes == default)
                        {
                            target = type.GetMethod(patch.methodName, AccessTools.allDeclared);
                        }
                        else
                        {
                            target = type.GetMethod(patch.methodName, AccessTools.allDeclared, null, patch.argumentTypes, default);
                        }
                        if (methodHasAttribute<HarmonyPrefix>()) { Main.harmony.Patch(target, prefix: patch); }
                        else if (methodHasAttribute<HarmonyPostfix>()) { Main.harmony.Patch(target, postfix: patch); }
                        else if (methodHasAttribute<HarmonyTranspiler>()) { Main.harmony.Patch(target, transpiler: patch); }
                        else if (methodHasAttribute<HarmonyFinalizer>()) { Main.harmony.Patch(target, finalizer: patch); }
                    }
                }
                //TODO:shut down mod if patching fails
                patched = true;
            }
            else
            {
                DebugLog($"unpatchingList length: {MethodList.Length}");
                foreach ((Type type, var patchList) in MethodList)
                {
                    DebugLog($"{type.Name} patchList length: {patchList.Length}");
                    foreach (var (_, patch) in patchList)
                    {
                        var target = type.GetMethod(patch.methodName);
                        Main.harmony.Unpatch(target, patch.method);
                    }
                }
                patched = false;
            }
        }
    }
}
