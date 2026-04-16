using HarmonyLib;
using MelonLoader;
using System.Reflection;
using Type = System.Type;
using System.Collections.Generic;
using Exception = System.Exception;
using System.Linq;
using System.Runtime.Hosting;
using System;
using UnityEngine.Assertions;

namespace ToolsOfTheTrade
{
    public abstract class SlackingMod<DerivedType> : SlackingBase where DerivedType : SlackingMod<DerivedType>
    {
        public static bool patched = false;
        public class ModSettings
        {
            public static MelonPreferences_Entry<bool> Debug;
            public static MelonPreferences_Entry<bool> Active;
        }
        public override void RegisterSettings()
        {
            var debugCategory = MelonPreferences.CreateCategory(Main.mainCategoryDebug, is_hidden: true);
            ModSettings.Debug = debugCategory.CreateEntry($"{typeof(DerivedType).Name} debug messages", false);

            var regularCategory = MelonPreferences.CreateCategory(Main.mainCategoryName);
            ModSettings.Active = regularCategory.CreateEntry($"Activate {typeof(DerivedType).Name}", true);
            ModSettings.Active.OnEntryValueChanged.Subscribe((_, _) => TryPatch());
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
                                                                                          new HarmonyMethod(method,debug:true)))
                                                                       .ToArray()))
                                                  .ToArray();
                return methodList;
            }
        }
        public static void DebugLog(object message = null, [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            if (ModSettings.Debug.Value)
            {
                Melon<Main>.Logger.Msg($"[{typeof(DerivedType).Name}][{functionName}]: {message}");
            }
        }
        public static void DebugLog(object message, System.ConsoleColor colour, [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            if (ModSettings.Debug.Value)
            {
                Melon<Main>.Logger.Msg(colour, $"[{typeof(DerivedType).Name}][{functionName}]: {message}");
            }
        }
        public static void PrintSurroundingIL(List<CodeInstruction> instructions, int startPoint, int focusLength, int linesBefore, int linesAfter, [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            Assert.IsTrue(startPoint < instructions.Count && startPoint >= 0);
            var prefixStart = Math.Max(startPoint - linesBefore, 0);
            var suffixEnd = Math.Min(startPoint + focusLength + linesAfter, instructions.Count() - 1);
            for (int index = prefixStart; index < suffixEnd; index++)
            {
                if (index >= startPoint && index < startPoint + focusLength)
                {
                    DebugLog($"{index}: {instructions[index]}", System.ConsoleColor.DarkMagenta, functionName);
                }
                else
                {
                    DebugLog($"{index}: {instructions[index]}", functionName);
                }
            }
        }
        //TODO:  add check for if the patch is in the state that is promised
        public override void TryPatch()
        {
            try
            {
                Patch();
            }
            catch (Exception e)
            {
                DebugLog($"Failed to patch. {e}");
            }
        }
        public override void Patch()
        {
            if (ModSettings.Active.Value == patched)
            {
                DebugLog($"skipped patching");
                return;
            }
            if (ModSettings.Active.Value)
            {
                DebugLog($"Types to patch: {MethodList.Length}");
                foreach ((Type type, var patchList) in MethodList)
                {
                    DebugLog($"{type.Name} patchList length: {patchList.Length}");
                    foreach (var (attributeList, patch) in patchList)
                    {
                        bool methodHasAttribute<T>() => attributeList.Any(attribute => attribute.AttributeType == typeof(T));
                        DebugLog($"\t{patch.methodName} => {patch.method.Name}");

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
                        Main.harmony.Unpatch(target, patch.method);
                    }
                }
                patched = false;
            }
        }
    }
}
