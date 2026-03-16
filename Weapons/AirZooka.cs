using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class AirZooka : MelonMod
    {
        private static void Log(object message, [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            MelonLogger.Msg($"[AirZooka][{functionName}]: {message}");
        }
        private static void DebugLog(string message = "", [System.Runtime.CompilerServices.CallerMemberName] string functionName = "")
        {
            if (Settings.AirZookaDebug.Value)
            {
                Log(message, functionName);
            }
        }
        static float gatheredMomentum = 0;

        static bool shouldStartDash = false;
        static bool shouldDrainSpeed = false;

        static Vector3 groundDragStore = default;
        static Vector3 airDragStore = default;

        static float reducedDragTimer = 0;
        static bool reducedDragActive = false;

        private const float reducedDragTimerMax = 1f;
        private const float momentumModifier = 2;

        [HarmonyPatch(typeof(ProjectileBase))]
        internal class PatchProjectileBase
        {
            [HarmonyPrefix]
            [HarmonyPatch("CreateProjectile", [typeof(string),
                                               typeof(Vector3),
                                               typeof(Vector3),
                                               typeof(ProjectileWeapon)])]
            [HarmonyPriority(HarmonyLib.Priority.First)]
            static bool StopProjectile(string path, ref ProjectileBase __result)
            {
                DebugLog();
                if (path != "Projectiles/ProjectileRifle")
                {
                    return true;
                }
                __result = new ProjectileBase();
                return false;
            }
        }
        [HarmonyPatch(typeof(PlayerCard))]
        class PlayerCard_
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PlayerCard.OnFire))]
            static bool TriggerDrain(PlayerCardData ___data, ref float ___overheatAmount)
            {
                DebugLog();
                if (___data.cardID != "RIFLE")
                {
                    return true;
                }
                shouldDrainSpeed = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(FirstPersonDrifter))]
        class FirstPersonDrifter_
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FirstPersonDrifter.ForceDash))]
            static bool TriggerDash(ref bool ___dashing,
                                         ref bool ___stomping,
                                         ref bool ___ziplining,
                                         ref Vector3 ___velocity,
                                         FirstPersonDrifter __instance,
                                         bool isGodspeed)
            {
                DebugLog();
                if (isGodspeed == false) { return true; }
                shouldStartDash = true;

                //default stuff
                ___dashing = false;
                AudioController.Play("MECH_DASH");
                if (___stomping)
                {
                    ___stomping = false;
                    AudioController.Stop("MECH_BOOST");
                    AudioController.Stop("ABILITY_STOMP_LOOP");
                }
                if (___ziplining)
                {
                    __instance.StopZipline(false, 1f, float.MaxValue, false);
                }
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FirstPersonDrifter.UpdateVelocity))]
            static void ChangeDrag(float deltaTime,
                                   ref Vector3 ___groundDrag,
                                   ref Vector3 ___airDrag)
            {
                DebugLog();
                if (reducedDragTimer == reducedDragTimerMax)
                {
                    groundDragStore = ___groundDrag;
                    airDragStore = ___airDrag;
                    ___groundDrag = ___groundDrag / 3;
                    ___airDrag = ___airDrag / 3;
                    reducedDragActive = true;
                }
                if (reducedDragActive)
                {
                    reducedDragTimer -= deltaTime;
                    if (reducedDragTimer <= 0)
                    {
                        ___groundDrag = groundDragStore;
                        ___airDrag = airDragStore;
                        reducedDragActive = false;
                    }
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(nameof(FirstPersonDrifter.UpdateVelocity))]
            static void DoDashOrDrain(ref Vector3 currentVelocity, ref Vector3 ___velocity, ref FirstPersonDrifter __instance)
            {
                DebugLog();
                if (shouldDrainSpeed)
                {
                shouldDrainSpeed = false;
                    DrainSpeed(ref currentVelocity, ref __instance);
                }
                else if (shouldStartDash)
                {
                shouldStartDash = false;
                    StartDash(ref currentVelocity, ref ___velocity);
                }
            }
            static void DrainSpeed(ref Vector3 currentVelocity, ref FirstPersonDrifter __instance)
            {
                DebugLog($"Before: {currentVelocity}speed {gatheredMomentum}momentum");
                Vector3 flatCurrent = currentVelocity;
                flatCurrent.y = 0;
                Vector3 forwardDirection = RM.mechController
                                             .playerCamera
                                             .PlayerCam
                                             .transform
                                             .parent
                                             .forward
                                             .normalized;
                float dot = Vector3.Dot(forwardDirection, flatCurrent);
                if (dot < 10 || flatCurrent.magnitude < 18.75) { return; }
                float flatRatio = /*18.75f / flatCurrent.magnitude*/ 0;
                float dotRatiod = dot * flatRatio;
                gatheredMomentum += dot - dotRatiod;
                __instance.MoveStun();
                __instance.moveStunRecoverySpeed = 0.5f;
                Vector3 clampedFlat = /*Vector3.ClampMagnitude(flatCurrent, 18.75f)*/ default;
                currentVelocity.x = clampedFlat.x;
                currentVelocity.z = clampedFlat.z;
                DebugLog($"After: {currentVelocity}speed {gatheredMomentum}momentum");
            }
            static void StartDash(ref Vector3 currentVelocity, ref Vector3 ___velocity)
            {
                DebugLog($"Before: {currentVelocity}speed {gatheredMomentum}momentum");

                Vector3 forwardDirection = RM.mechController
                                             .playerCamera
                                             .PlayerCam
                                             .transform
                                             .parent
                                             .forward
                                             .normalized;
                currentVelocity += momentumModifier * gatheredMomentum * forwardDirection;
                ___velocity += momentumModifier * gatheredMomentum * forwardDirection;
                gatheredMomentum = 0;
                reducedDragTimer = reducedDragTimerMax;
                DebugLog($"After: {currentVelocity}speed {gatheredMomentum}momentum");
            }
        }
    }
}
