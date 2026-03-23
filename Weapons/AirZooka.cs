using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class AirZooka : WeaponTool<AirZooka>
    {
        class Settings : ModSettings
        {
            public static MelonPreferences_Entry<float> AirZookaMomentumModifier;
            public static MelonPreferences_Entry<float> AirZookaDragFactor;
        }
        public override void RegisterSettings()
        {
            var Selection = MelonPreferences.CreateCategory(ToolsOfTheTrade.Settings.mainCategoryName);

            Settings.AirZookaMomentumModifier = Selection.CreateEntry("AirZookaMomentumModifier", 1f);
            Settings.AirZookaDragFactor = Selection.CreateEntry("AirZookaDragFactor", 2f);

            base.RegisterSettings();
        }
        static float gatheredMomentum = 0;

        static bool shouldStartDash = false;
        static bool shouldDrainSpeed = false;

        static Vector3 groundDragStore = default;
        static Vector3 airDragStore = default;

        static float reducedDragTimer = 0;
        static bool reducedDragActive = false;

        private const float reducedDragTimerMax = 1f;
        //private const float momentumModifier = 1;

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
                    return true;
                }
                DebugLog();
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
                    return true;
                }
                DebugLog();
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
                if (reducedDragTimer == reducedDragTimerMax)
                {
                    DebugLog("drag start");
                    groundDragStore = ___groundDrag;
                    airDragStore = ___airDrag;
                    ___groundDrag = ___groundDrag / Settings.AirZookaDragFactor.Value;
                    ___airDrag = ___airDrag / Settings.AirZookaDragFactor.Value;
                    reducedDragActive = true;
                }
                if (reducedDragActive)
                {
                    reducedDragTimer -= deltaTime;
                    if (reducedDragTimer <= 0)
                    {
                        DebugLog("drag end");
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
                //DebugLog();
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
                currentVelocity += Settings.AirZookaMomentumModifier.Value * gatheredMomentum * forwardDirection;
                ___velocity += Settings.AirZookaMomentumModifier.Value * gatheredMomentum * forwardDirection;
                gatheredMomentum = 0;
                reducedDragTimer = reducedDragTimerMax;
                DebugLog($"After: {currentVelocity}speed {gatheredMomentum}momentum");
            }
        }
    }
}
