using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class AirZooka : MelonMod
    {
        private static void Log(object message)
        {
            MelonLogger.Msg(message);
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
                if (path != "Projectiles/ProjectileRifle")
                {
                    Log(path);
                    return true;
                }
                Log($"StopProjectile: forward ->{RM.mechController
                                                   .playerCamera
                                                   .PlayerCam
                                                   .transform
                                                   .parent
                                                   .forward}");
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
                if (___data.cardID != "RIFLE")
                {
                    Log(___data.cardID);
                    return true;
                }
                Log($"GatherMomentum: Oh lord they drainin: {gatheredMomentum}");
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
                    Log($"{deltaTime}");
                    Log($"reducedDragTimer: {reducedDragTimer}");
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
            static void DoDashOrDrain(ref Vector3 currentVelocity, ref Vector3 ___velocity)
            {
                if (shouldDrainSpeed)
                {
                    shouldDrainSpeed = false;
                    Log($"drainSpeed: Before: {currentVelocity}");
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

                    Vector3 clampedFlat = /*Vector3.ClampMagnitude(flatCurrent, 18.75f)*/ default;
                    currentVelocity.x = clampedFlat.x;
                    currentVelocity.z = clampedFlat.z;
                    Log($"drainSpeed: Oh lord they drainin: {gatheredMomentum}");
                    Log($"drainSpeed: After: {currentVelocity}");
                }
                else if (shouldStartDash)
                {
                    Log($"pushPullBoostActive: Before: {currentVelocity}");

                    Log($"pushPullBoostActive: Oh lord they flyin: {gatheredMomentum}");
                    shouldStartDash = false;
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
                    Log($"pushPullBoostActive: After: {currentVelocity}");
                }
            }
        }
    }
}
