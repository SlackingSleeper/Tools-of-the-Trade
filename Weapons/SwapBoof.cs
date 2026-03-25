using HarmonyLib;
using MelonLoader;
using System.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SoftNormalsToVertexColor;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class SwapBoof : WeaponTool<SwapBoof>
    {
        //static float lastBoof = 0f;
        //static BaseDamageable lastTarget = default;
        //TODO: change meatball,shocker behaviour
        [HarmonyPatch(typeof(FirstPersonDrifter))]
        class FirstPersonDrifter_
        {
            [HarmonyPrefix]
            [HarmonyPatch("Telefrag")]
            static bool InitiateBoofSwapProcedure(BaseDamageable damageable,
                                 ref bool ___telefragging,
                                 ref Vector3 ___telefragStartPosition,
                                 ref Vector3 ___telefragEndPosition,
                                 ref float ___telefragTimer,
                                 ref BaseDamageable ___telefragTarget,
                                 bool ___ziplining,
                                 ref bool ___stomping,
                                 ref bool ___dashing,
                                 ref FirstPersonDrifter __instance)
            {
                if (!___telefragging)
                {
                    ___telefragging = true;
                    ___telefragStartPosition = __instance.transform.position;
                    ___telefragEndPosition = damageable.transform.position;
                    RM.exploder.TelefragTarget(damageable.transform.position, damageable.GetComponent<Collider>().bounds.extents.magnitude * 0.5f);
                    RM.exploder.TelefragTarget(___telefragStartPosition, damageable.GetComponent<Collider>().bounds.extents.magnitude * 0.5f);

                    ___telefragTimer = 0f;
                    ___telefragTarget = damageable;
                    ___telefragTarget.OnTelefragStart();
                    if (___ziplining)
                    {
                        __instance.CancelZiplineFromAnotherAbility();
                    }
                    if (___stomping)
                    {
                        ___stomping = false;
                        AudioController.Stop("MECH_BOOST");
                        AudioController.Stop("ABILITY_STOMP_LOOP");
                    }
                    if (___dashing)
                    {
                        ___dashing = false;
                    }
                    AudioController.Play("TELEFRAG");
                    return false;
                }
                Debug.LogWarning("Attempted a telefrag while we are already telefragging");
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch("OnTelefragEnd")]
            static bool DoBoofSwap(ref bool ___telefragging,
                                   ref Vector3 ___telefragStartPosition,
                                   ref Transform ___myTransform,
                                   ref Vector3 ___telefragEndPosition,
                                   ref BaseDamageable ___telefragTarget,
                                   ref Vector3 ___moveDirection,
                                   ref FirstPersonDrifter __instance)
            {
                {
                    ___telefragging = false;
                    if (___telefragTarget is BookOfLife)
                    {
                        ___telefragTarget.OnTelefragEnd();
                        AudioController.Play("TELEFRAG_END");
                        ___myTransform.position = ___telefragEndPosition;
                        return false;
                    }
                    AudioController.Play("TELEFRAG_END");

                    ___myTransform.position = ___telefragEndPosition;
                    ___telefragTarget.transform.position = ___telefragStartPosition;
                    if (___telefragTarget is EnemyShocker)
                    {
                        ref var wpnSprng = ref (___telefragTarget as EnemyShocker)._shockWeapon.weaponSpring;
                        wpnSprng.CurrentPos = ___telefragStartPosition;
                        wpnSprng.TargetValue = ___telefragStartPosition;
                    }
                    if (___telefragTarget is EnemyMimic)
                    {
                        ref var wpnSprng1 = ref (___telefragTarget as EnemyMimic).weapon.weaponSpring;
                        wpnSprng1.CurrentPos = ___telefragStartPosition;
                        wpnSprng1.TargetValue = ___telefragStartPosition;
                        ref var wpnSprng2 = ref (___telefragTarget as EnemyMimic).weapon2.weaponSpring;
                        wpnSprng2.CurrentPos = ___telefragStartPosition;
                        wpnSprng2.TargetValue = ___telefragStartPosition;
                    }

                    ((___telefragTarget as EnemyTripwire)?.weapons[0] as TripwireWeapon)?.WeaponStart();

                    if (___telefragTarget is EnemyBalloon)
                    {
                        var originField = typeof(EnemyBalloon).GetField("_origin", AccessTools.allDeclared);
                        originField.SetValue(___telefragTarget, ___telefragStartPosition);
                    }

                    DebugLog("new fragger");

                    __instance.ForceZeroVelocity();
                    ___moveDirection = Vector3.zero;

                    ___telefragTarget.OnTelefragEnd();
                    ___telefragTarget = null;
                    __instance.timeSinceLastTelefrag = 0f;
                    RM.mechController.TriggerInvincibilityTimer();

                    var lastTelefragTargetField = typeof(MechController).GetField("_lastTelefragTarget", AccessTools.allDeclared);
                    lastTelefragTargetField.SetValue(RM.mechController, null);
                    //lastBoof = Time.time;
                    //lastTarget = ___telefragTarget;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(EnemyShocker))]
        class EnemyShocker_
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyShocker.OnTelefragEnd))]
            static bool SkipTelefragEnd()
            {
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyShocker.OnTelefragStart))]
            static bool SkipTelefragStart()
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(EnemyDemonBall))]
        class EnemyDemonBall_
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyDemonBall.OnTelefragStart))]
            static bool SkipTelefragStart()
            {
                return false;
            }
        }
    }
}
