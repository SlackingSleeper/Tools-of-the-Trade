using HarmonyLib;
using KinematicCharacterController;
using NeonLite;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static MelonLoader.MelonLogger;
using DamageableType = BaseDamageable.DamageableType;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    internal class SwapBoof : SlackingMod<SwapBoof>
    {
        class Settings : ModSettings
        {
        }
        public static PlayerCardData data = null;
        static bool initialized = false;
        static Dictionary<EnemyJumper, Coroutine> iHateJumpersSoMuchRightNow = [];
        static public void RecieveJumperCoroutine(Coroutine fuckingJumpers, EnemyJumper whichFucker)
        {
            DebugLog("what what what the what");
            iHateJumpersSoMuchRightNow[whichFucker] = fuckingJumpers;
        }
        static readonly FieldInfo MechController_targetAssist = AccessTools.Field(typeof(MechController), "m_targetAssist");
        static readonly FieldInfo MechController_forcefieldLayerMask = AccessTools.Field(typeof(MechController), "forcefieldLayerMask");

        static private RaycastHit[] telefragHits = new RaycastHit[5];

        private class TelefragTarget
        {
            public TelefragTarget(BaseDamageable target, bool wantSnapCamera, bool wantSoundEffect)
            {
                this.target = target;
                this.wantSnapCamera = wantSnapCamera;
                this.wantSoundEffect = wantSoundEffect;
            }
            public BaseDamageable target;
            public bool wantSnapCamera;
            public bool wantSoundEffect;
        }
        static TelefragTarget GetTelefragTarget()
        {
            Ray ray = RM.mechController.playerCamera.PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f));
            Debug.DrawRay(ray.origin, ray.direction * 60f, Color.red, 0.2f);
            RaycastHit raycastHit;
            TargetPoint targetPoint = ((TargetAssist)MechController_targetAssist.GetValue(RM.mechController)).GetHighlightedTarget();
            if (targetPoint != null) //if there is a highlighted target
            {

                BaseDamageable aimAssistTarget = targetPoint.GetBaseDamageable();
                DamageableType aimAssistTargetType = aimAssistTarget.GetDamageableType();
                Vector3 cameraPosition = RM.mechController.playerCamera.transform.position;
                if (Physics.RaycastNonAlloc(new Ray(cameraPosition, targetPoint.Center - cameraPosition),
                                            telefragHits,
                                            Vector3.Distance(targetPoint.Center, cameraPosition),
                                            (int)MechController_forcefieldLayerMask.GetValue(RM.mechController)) <= 0 //if no targets are between player and highlighted target AND
                 && (aimAssistTargetType == DamageableType.Enemy
                  || aimAssistTargetType == DamageableType.Barrel
                  || aimAssistTargetType == DamageableType.Chest
                  || aimAssistTargetType == DamageableType.Crystal
                  || aimAssistTargetType == DamageableType.CrystalExplosive
                  || aimAssistTargetType == DamageableType.EnvironmentPortal) //highlighted target is one of these types AND
                 && (!(aimAssistTarget.dropsCard != null)
                  || aimAssistTarget.dropsCard.consumableType != PlayerCardData.ConsumableType.LoreCollectible))//if the highlighted DOESN'T drop a card OR drops non LoreCollectible
                {
                    bool isOtherThanLast = aimAssistTarget != lastTeleTarget;
                    lastTeleTarget = aimAssistTarget;
                    teleBufferTimer = 0f;
                    teleSnapCameraAngle = RM.mechController.playerCamera.PlayerCam.transform.rotation.eulerAngles;
                    return new(target: aimAssistTarget, wantSnapCamera: !isOtherThanLast, wantSoundEffect: isOtherThanLast);
                }
                return null;
            }
            else if (Physics.Raycast(ray, out raycastHit, 500f, RM.mechController.telefragLayerMask, QueryTriggerInteraction.Ignore))
            {
                BaseDamageable target = raycastHit.collider.GetComponent<BaseDamageable>();
                if (target)
                {
                    DamageableType targetType = target.GetDamageableType();
                    if ((targetType == DamageableType.Enemy
                      || targetType == DamageableType.Barrel
                      || targetType == DamageableType.Chest
                      || targetType == DamageableType.Crystal
                      || targetType == DamageableType.CrystalExplosive
                      || targetType == DamageableType.EnvironmentPortal)//highlighted target is one of these types AND
                     && (!(target.dropsCard != null)
                      || target.dropsCard.consumableType != PlayerCardData.ConsumableType.LoreCollectible))//if the highlighted DOESN'T drop a card OR drops non LoreCollectible
                    {
                        bool isNewTarget = lastTeleTarget == null || lastTeleTarget != target;
                        lastTeleTarget = target;
                        teleBufferTimer = 0f;
                        teleSnapCameraAngle = RM.mechController.playerCamera.PlayerCam.transform.rotation.eulerAngles;
                        return new(target: target, wantSnapCamera: false, wantSoundEffect: isNewTarget);
                    }
                    return null;
                }
                else
                {
                    if (lastTeleTarget != null && teleBufferTimer < 0.25f)
                    {
                        return new(lastTeleTarget, true, false);
                    }
                    return null;
                }
            }
            else
            {
                if (lastTeleTarget != null && teleBufferTimer < 0.25f)
                {
                    return new(lastTeleTarget, true, false);
                }
                return null;
            }

        }

        static UIAbilityIndicator teleUI = default;
        static float teleBufferTimer = default;
        static Vector3 teleSnapCameraAngle = default;
        static BaseDamageable lastTeleTarget = default;
        static bool teleSwapping = false;
        static BaseDamageable teleTarget = default;
        static Vector3 teleStart = default;
        static Vector3 teleEnd = default;
        static float teleTimer = 0f;
        const float teleTimeMax = 0.5f;
        static void TeleStart()
        {
            if (!teleSwapping)
            {
                DebugLog("Attempted telefrag while not teleSwapping! yippee!!!!!");
                var targetInfo = GetTelefragTarget();
                if (targetInfo.wantSnapCamera)
                {
					RM.drifter.mouseLookX.SetRotationX(teleSnapCameraAngle.y);
                }
                teleTarget = targetInfo.target;
                teleStart = RM.mechController.transform.position;
                teleEnd = teleTarget.transform.position;
                RM.exploder.TelefragTarget(teleStart, teleTarget.GetComponent<Collider>().bounds.extents.magnitude * 0.5f);
                RM.exploder.TelefragTarget(teleEnd, teleTarget.GetComponent<Collider>().bounds.extents.magnitude * 0.5f);

                if (RM.drifter.GetIsDashing())
                {
                    AccessTools.Field(typeof(FirstPersonDrifter), "dashing")
                               .SetValue(RM.drifter, false);
                }
                if (RM.drifter.GetIsStomping())
                {
                    AccessTools.Field(typeof(FirstPersonDrifter), "stomping")
                               .SetValue(RM.drifter, false);
                    AudioController.Stop("MECH_BOOST");
                    AudioController.Stop("ABILITY_STOMP_LOOP");
                }
                if (RM.drifter.GetIsTelefragging())
                {
                    //stop telefragging or smth i dunno
                }
                if ((bool)AccessTools.Field(typeof(FirstPersonDrifter), "ziplining").GetValue(RM.drifter))
                {
                    RM.drifter.CancelZiplineFromAnotherAbility();
                }
                AudioController.Play("TELEFRAG");

                BigChungus.currentlyActiveUpdateEffects.Add(data.cardID);
                AccessTools.Method("MechController:DoCardPickup", [typeof(PlayerCardData), typeof(int)])
                           .Invoke(RM.mechController, [data, -1]);
            }
            DebugLog("Attempted telefrag while already teleSwapping");
        }
        static void TeleUpdate(float deltaTime)
        {
            RM.drifter.Motor.SetPosition(Vector3.Lerp(teleStart, teleEnd, RM.drifter.telefragAnimCurve.Evaluate(teleTimer / teleTimeMax)), true);
            teleTimer += deltaTime;
            if (teleTimer > teleTimeMax)
            {
                OnTelefragEnd();
            }
        }
        static bool CanTeleSwap()
        {
            return GetTelefragTarget() != null;
        }
        static Renderer BakedRenderer()
        {
            return RM.ui.transform.Find("uiCard (1)/uiCardBakedMeshes/uiCardBaked_Rapture").GetComponent<MeshRenderer>();
        }
        static void OnTelefragEnd()
        {
            teleSwapping = false;
            if (teleTarget is BookOfLife)
            {
                teleTarget.OnTelefragEnd();
                AudioController.Play("TELEFRAG_END");
                RM.drifter.transform.position = teleEnd;
                return;
            }
            AudioController.Play("TELEFRAG_END");

            RM.drifter.transform.position = teleEnd;
            teleTarget.transform.position = teleStart;
            if (teleTarget is EnemyShocker)
            {
                ref var wpnSprng = ref (teleTarget as EnemyShocker)._shockWeapon.weaponSpring;
                wpnSprng.CurrentPos = teleStart;
                wpnSprng.TargetValue = teleStart;
            }
            if (teleTarget is EnemyMimic)
            {
                ref var wpnSprng1 = ref (teleTarget as EnemyMimic).weapon.weaponSpring;
                wpnSprng1.CurrentPos = teleStart;
                wpnSprng1.TargetValue = teleStart;
                ref var wpnSprng2 = ref (teleTarget as EnemyMimic).weapon2.weaponSpring;
                wpnSprng2.CurrentPos = teleStart;
                wpnSprng2.TargetValue = teleStart;
            }

                ((teleTarget as EnemyTripwire)?.weapons[0] as TripwireWeapon)?.WeaponStart();

            if (teleTarget is EnemyBalloon)
            {
                var originField = typeof(EnemyBalloon).GetField("_origin", AccessTools.allDeclared);
                originField.SetValue(teleTarget, teleStart);
            }
            if (teleTarget is EnemyJumper)
            {
                if (iHateJumpersSoMuchRightNow.TryGetValue(teleTarget as EnemyJumper, out Coroutine theCoroutineInQuestion))
                {
                    teleTarget.StopCoroutine(theCoroutineInQuestion);
                }
                var currentWaypointField = typeof(EnemyJumper).GetField("currentWaypoint", AccessTools.allDeclared);
                var _heightField = typeof(EnemyJumper).GetField("_height", AccessTools.allDeclared);
                (currentWaypointField.GetValue((teleTarget)) as EnemyWaypoint).transform.position = teleStart + Vector3.up * 0.625f;
            }
            DebugLog("new fragger");

            var _breakablePlatformField = typeof(BaseDamageable).GetField("_breakablePlatform", AccessTools.all);
            _breakablePlatformField.SetValue(teleTarget, null);//could change this so that it looks for a new breakable but it needs to only do that for some types so it's complicated

            RM.drifter.ForceZeroVelocity();

            teleTarget = null;
            RM.mechController.TriggerInvincibilityTimer();
            lastTeleTarget = null;
            BigChungus.currentlyActiveUpdateEffects.Remove(data.cardID);
        }
        static void TeleAbort()
        {
            teleSwapping = false;
            BigChungus.currentlyActiveUpdateEffects.Remove(data.cardID);
        }
        static void ToggleUI(bool wantOn)
        {
            if (wantOn)
            {
                var telefragTarget = GetTelefragTarget();
                BaseDamageable damageable = telefragTarget?.target;
                float distance = Vector3.Distance(damageable?.transform.position ?? Vector3.zero, RM.mechController.playerCamera.PlayerCam.transform.position);
                float lerpedDistance = Mathf.InverseLerp(500f, 150f, distance);
                RM.ui.SetTelefragUI(true, telefragTarget != null, lerpedDistance);
                if (telefragTarget?.wantSoundEffect != null)
                {
                    AudioController.Play("WEAPON_BOOKOFLIFE_TARGET");
                }
            }
        }
        /*        [HarmonyPatch(typeof(FirstPersonDrifter))]
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
                            if (___telefragTarget is EnemyJumper)
                            {
                                if (iHateJumpersSoMuchRightNow.TryGetValue(___telefragTarget as EnemyJumper, out Coroutine theCoroutineInQuestion))
                                {
                                    ___telefragTarget.StopCoroutine(theCoroutineInQuestion);
                                }
                                var currentWaypointField = typeof(EnemyJumper).GetField("currentWaypoint", AccessTools.allDeclared);
                                var _heightField = typeof(EnemyJumper).GetField("_height", AccessTools.allDeclared);
                                (currentWaypointField.GetValue((___telefragTarget)) as EnemyWaypoint).transform.position = ___telefragStartPosition + Vector3.up * 0.625f;
                            }
                            DebugLog("new fragger");

                            var _breakablePlatformField = typeof(BaseDamageable).GetField("_breakablePlatform", AccessTools.all);
                            _breakablePlatformField.SetValue(___telefragTarget, null);//could change this so that it looks for a new breakable but it needs to only do that for some types

                            __instance.ForceZeroVelocity();
                            ___moveDirection = Vector3.zero;

                            ___telefragTarget = null;
                            __instance.timeSinceLastTelefrag = 0f;
                            RM.mechController.TriggerInvincibilityTimer();

                            var lastTelefragTargetField = typeof(MechController).GetField("_lastTelefragTarget", AccessTools.allDeclared);
                            lastTelefragTargetField.SetValue(RM.mechController, null);
                        }
                        return false;
                    }
                }*/
        [HarmonyPatch(typeof(EnemyJumper))]
        public class EnemyJumper_
        {
            [HarmonyTranspiler]
            [HarmonyPatch("Jump")]
            public static IEnumerable<CodeInstruction> AbortJump(IEnumerable<CodeInstruction> instructions)
            {
                var myMatcher = new CodeMatcher(instructions);
                myMatcher.MatchForward(true, new CodeMatch(x => x.opcode == OpCodes.Pop))
                         .RemoveInstruction()
                         .Insert([new CodeInstruction(OpCodes.Ldarg_0),
                                  CodeInstruction.Call(typeof(SwapBoof),
                                                       "RecieveJumperCoroutine",
                                                       [typeof(Coroutine),typeof(EnemyJumper)])]);
                return myMatcher.Instructions();
            }
        }
        public override void Patch()
        {
            if (Settings.Active.Value == true)
            {
                if (!initialized)
                {
                    data = ScriptableObject.CreateInstance<PlayerCardData>();
                    if ((data.abilityIconTextureActive = Main.assets.LoadAsset<Texture2D>("BoofAbilityIconActive")) == null)
                    {
                        throw new ArgumentException("failed to load Asset \"BoofAbilityIconActive\"");
                    }
                    if ((data.abilityIconTextureDisabled = Main.assets.LoadAsset<Texture2D>("BoofAbilityIconInactive")) == null)
                    {
                        throw new ArgumentException("failed to load Asset \"BoofAbilityIconInactive\"");
                    }
                    if ((data.cardDesignTexture = Main.assets.LoadAsset<Texture2D>("SwapBoof_cardDesignTexture")) == null)
                    {
                        throw new ArgumentException("failed to load Asset \"SwapBoof_cardDesignTexture\"");
                    }
                    if((data.weaponIconTexture = Main.assets.LoadAsset<Texture2D>("SwapBoof_WeaponIconTexture")) == null)
                    {
                        throw new ArgumentException("failed to load Asset \"SwapBoof_WeaponIconTexture\"");
                    }
                    //teleUI = new UIAbilityIndicator() { }

                    data.cardID = "SWAPBOOF";
                    data.cardName = "Swappus Boofius";
                    data.cardType = PlayerCardData.Type.SpecialAbility;
                    data.discardAbility = (PlayerCardData.DiscardAbility)201;
                    data.screenshake = new Vector2(1, 1);
                    data.cardColor = new Color(1, 1, 0, 1);
                    data.crystalTexture = default;//??????? what to do here i need sleep
                    data.weaponAudioName = "GRENADE";
                    data.showcaseItemType = MenuScreenItemShowcase.ItemType.Card;

                    initialized = true;
                }
                if (BigChungus.customDictionary.ContainsKey(data.cardID) == false)
                {
                    BigChungus.customDictionary.Add(data.cardID, new(data: data,
                                                                     checkDiscardAllowed: CanTeleSwap,
                                                                     getBakedRenderer: BakedRenderer,
                                                                     abortAbility: TeleAbort,
                                                                     doDiscard: TeleStart,
                                                                     updateVelocityEarly: TeleUpdate,
                                                                     toggleCustomUI: ToggleUI));
                }
                if (BigChungus.discardNumberToCardID.ContainsKey((int)data.discardAbility) == false)
                {
                    BigChungus.discardNumberToCardID.Add((int)data.discardAbility, data.cardID);
                }
            }
            else //Settings.Active.Value != true
            {
                BigChungus.customDictionary.Remove(data.cardID);
                BigChungus.discardNumberToCardID.Remove((int)data.discardAbility);
            }
            base.Patch();
        }
    }
}
