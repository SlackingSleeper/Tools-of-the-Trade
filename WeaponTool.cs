using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToolsOfTheTrade
{
    internal abstract class WeaponTool<DerivedType> : SlackingMod<DerivedType> where DerivedType : WeaponTool<DerivedType>
    {
        //public override void OnPreferencesSaved()
        //{
        //    Debug.Log("Init WT OPScb");
        //    DoPatching();
        //}
        //public override void OnLateInitializeMelon()
        //{
        //    Debug.Log("Init WT OPScb");
        //    DoPatching();
        //}
        //public override void OnInitializeMelon()
        //{
        //    Debug.Log("Init WT OIMcb");
        //    RegisterSettings();
        //}
    }
}
