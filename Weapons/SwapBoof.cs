using HarmonyLib;
using MelonLoader;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static SoftNormalsToVertexColor;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class SwapBoof : WeaponTool<SwapBoof>
    {

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
                    //bool flag = false;
                    //if (___telefragTarget.GetDamageableType() == BaseDamageable.DamageableType.Enemy
                    // && ___telefragTarget.GetEnemyType() == Enemy.Type.mimic
                    // && EnemyMimic.mimicType == EnemyMimic.MimicType.Attack)
                    //{
                    //    flag = true;
                    //}
                    ___telefragging = false;
                    //if (!flag)
                    //{
                    //    ___telefragTarget.SetDieSFX("");
                    //}
                    string audioID = "TELEFRAG_END";
                    //Vector3 normalized = (___telefragEndPosition - ___telefragStartPosition).normalized;
                    //float d = 1f;
                    //bool flag2 = true;
                    //int damage = __instance.telefragDamage;
                    //float num = 0f;
                    //if (___telefragTarget.GetDamageableType() == BaseDamageable.DamageableType.CrystalExplosive || ___telefragTarget.GetEnemyType() == Enemy.Type.bossBasic)
                    //{
                    //    Vector3 a = ___telefragStartPosition;
                    //    a.y = ___telefragEndPosition.y;
                    //    normalized = (a - ___telefragEndPosition).normalized;
                    //    normalized.y = 0.6f;
                    //    d = 1.5f;
                    //    flag2 = false;
                    //    num = 1f;
                    //    if (___telefragTarget.GetEnemyType() == Enemy.Type.bossBasic)
                    //    {
                    //        damage = 30;
                    //    }
                    //    else
                    //    {
                    //        damage = ___telefragTarget.maxHealth;
                    //    }
                    //}
                    AudioController.Play(audioID);
                    ___myTransform.position = ___telefragEndPosition;
                    /*new*/
                    ___telefragTarget.transform.position = ___telefragStartPosition;
                    DebugLog("new fragger");

                    __instance.ForceZeroVelocity();
                    ___moveDirection = Vector3.zero;
                    //Vector3 vel = normalized * __instance.telefragSpeed * d;
                    //vel.y *= num;
                    //__instance.AddVelocity(vel);
                    //if (flag2)
                    //{
                    //    __instance.ForceJump(__instance.telefragUp, false, true, 1f);
                    //}
                    //___telefragTarget.OnHit(___transform.position, damage, BaseDamageable.DamageSource.Dash);
                    //if (flag)
                    //{
                    //    if (!GameDataManager.saveData.playerAchievementData.bookOfLifeMimicDeath)
                    //    {
                    //        GameDataManager.saveData.playerAchievementData.bookOfLifeMimicDeath = true;
                    //        GameDataManager.SaveGame();
                    //    }
                    //    Achievements.SyncBookOfLifeMimicAchievement(true);
                    //    RM.mechController.OnHit(RM.mechController.currentHealth, ___telefragTarget.transform.position, true);
                    //}

                    ___telefragTarget.OnTelefragEnd();
                    ___telefragTarget = null;
                    __instance.timeSinceLastTelefrag = 0f;
                    RM.mechController.TriggerInvincibilityTimer();
                }
                return false;
            }
        }
    }
}
