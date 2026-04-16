/*using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class SpinAttack : SlackingMod<SpinAttack>
    {
        class Settings : ModSettings
        {
            public static MelonPreferences_Category category;

            public static MelonPreferences_Entry<float> SpinAttackSpinSpeed;
            public static MelonPreferences_Entry<float> SpinAttackLength;
        }
        public override void RegisterSettings()
        {
            Settings.category = MelonPreferences.CreateCategory(Main.mainCategoryName);

            Settings.SpinAttackSpinSpeed = Settings.category.CreateEntry("SpinAttackSpinSpeed", 1f);
            Settings.SpinAttackLength = Settings.category.CreateEntry("SpinAttackLength", 10f);

            base.RegisterSettings();
        }
        static float spinTimer = 0;
        static bool spinDone = false;
        static Vector3 preSpinLook = default;
        static BaseDamageable spinTarget = default;
        static int num = 0;
        static float saveMaxY = 85;

        [HarmonyPatch(typeof(FirstPersonDrifter))]
        class FirstPersonDrifter_
        {
            [HarmonyPrefix]
            [HarmonyPatch("Telefrag")]
            static bool SpinOrTele(BaseDamageable damageable,
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
                    if (___dashing) { ___dashing = false; }
                    if (___ziplining) { __instance.CancelZiplineFromAnotherAbility(); }
                    if (___stomping)
                    {
                        ___stomping = false;
                        AudioController.Stop("MECH_BOOST");
                        AudioController.Stop("ABILITY_STOMP_LOOP");
                    }

                    if (spinDone)
                    {
                        spinDone = false;
                        RM.acceptInput = true;
                        DebugLog("accept input");
                        ___telefragging = true;
                        ___telefragStartPosition = __instance.transform.position;
                        ___telefragEndPosition = damageable.transform.position;
                        RM.exploder.TelefragTarget(damageable.transform.position, damageable.GetComponent<Collider>().bounds.extents.magnitude * 0.5f);

                        ___telefragTimer = 0f;
                        ___telefragTarget = damageable;
                        ___telefragTarget.OnTelefragStart();
                        AudioController.Play("TELEFRAG");
                    }
                    else if (spinTimer <= 0)
                    {//init
                        preSpinLook = (___telefragEndPosition - ___telefragStartPosition).normalized;
                        RM.acceptInput = false;
                        DebugLog("deny input");

                        saveMaxY = __instance.mouseLookY.maximumY;
                        __instance.mouseLookY.maximumY = float.MaxValue;
                        spinTimer = Settings.SpinAttackLength.Value;
                        spinTarget = damageable;
                    }
                    return false;
                }
                Debug.LogWarning("Attempted a telefrag while we are already telefragging");
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch("UpdateVelocity")]
            static bool Spin2Win(ref Vector3 currentVelocity, float deltaTime, ref FirstPersonDrifter __instance)
            {
                if (spinTimer > 0)
                {
                    spinTimer -= deltaTime;
                    RM.drifter.mouseLookY.SetRotationY(Settings.SpinAttackSpinSpeed.Value * num++);
                    //RM.mechController.playerCamera.PlayerCam.transform.Rotate(new(Settings.SpinAttackSpinSpeed.Value, 0, 0));
                    DebugLog("spin spin spin");
                    if (spinTimer <= 0)
                    {
                        num = 0;
                        spinDone = true;
                        RM.acceptInput = true;
                        RM.drifter.mouseLookY.SetRotationY(preSpinLook.GetYaw());
                        __instance.Telefrag(spinTarget);

                    }
                    currentVelocity = Vector3.zero;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnTelefragEnd")]
            static bool DoAttack(ref bool ___telefragging,
                                   ref Vector3 ___telefragStartPosition,
                                   ref Transform ___myTransform,
                                   ref Vector3 ___telefragEndPosition,
                                   ref BaseDamageable ___telefragTarget,
                                   ref Vector3 ___moveDirection,
                                   ref FirstPersonDrifter __instance)
            {
                {
                    ___telefragging = false;

                    bool targetIsMimic = false;
                    if (___telefragTarget.GetDamageableType() == BaseDamageable.DamageableType.Enemy
                     && ___telefragTarget.GetEnemyType() == Enemy.Type.mimic
                     && EnemyMimic.mimicType == EnemyMimic.MimicType.Attack)
                    {
                        targetIsMimic = true;
                    }
                    else
                    {
                        ___telefragTarget.SetDieSFX("");
                    }
                    string audioID = "TELEFRAG_END";
                    var diff = ___telefragStartPosition - ___telefragEndPosition;
                    diff.y = 0;
                    Vector3 normalized = diff.normalized;
                    float speedModifier = 1f;
                    bool shouldJump = true;
                    int damage = __instance.telefragDamage;
                    if (___telefragTarget.GetDamageableType() == BaseDamageable.DamageableType.CrystalExplosive || ___telefragTarget.GetEnemyType() == Enemy.Type.bossBasic)
                    {
                        normalized.y = 0.6f;
                        speedModifier = 1.5f;
                        shouldJump = false;
                        if (___telefragTarget.GetEnemyType() == Enemy.Type.bossBasic)
                        {
                            damage = 30;
                        }
                        else
                        {
                            damage = ___telefragTarget.maxHealth;
                        }
                    }
                    AudioController.Play(audioID);
                    ___myTransform.position = ___telefragEndPosition;
                    DebugLog("new fragger");

                    __instance.ForceZeroVelocity();
                    ___moveDirection = Vector3.zero;
                    Vector3 vel = normalized * __instance.telefragSpeed * speedModifier;
                    __instance.AddVelocity(vel);
                    if (shouldJump)
                    {
                        __instance.ForceJump(__instance.telefragUp, false, true, 1f);
                    }
                    ___telefragTarget.OnHit(___myTransform.position, damage, BaseDamageable.DamageSource.Dash);
                    if (targetIsMimic)
                    {
                        RM.mechController.OnHit(RM.mechController.currentHealth, ___telefragTarget.transform.position, true);
                    }

                    ___telefragTarget.OnTelefragEnd();
                    ___telefragTarget = null;
                    __instance.timeSinceLastTelefrag = 0f;
                    RM.mechController.TriggerInvincibilityTimer();
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(MouseLook))]
        class MouseLook_
        {
            [HarmonyPrefix]
            [HarmonyPatch("UpdateRotation")]
            static bool SpinCamera()
            {
                if (spinTimer <= 0) { return true; }

                return false;
            }
        }
    }
}
*/