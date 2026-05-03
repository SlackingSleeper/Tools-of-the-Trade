using HarmonyLib;
using MelonPrefManager.UI.InteractiveValues;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using Type = System.Type;
using UniverseLib.UI;
using System;
using System.Linq;
using static HarmonyLib.Code;
using MelonLoader;
using System.Runtime.CompilerServices;

namespace ToolsOfTheTrade
{
    [HarmonyPatch]
    public class BigChungus : SlackingMod<BigChungus>
    {
        public class CustomCardInfo(PlayerCardData data,
                                    Func<bool> checkDiscardAllowed = null,
                                    Action<string> abortAbility = null,
                                    Action<bool> toggleCustomUI = null,
                                    Action doDiscard = null,
                                    Action<float> updateVelocityEarly = null,
                                    Action<float> updateVelocityLate = null,
                                    Action<int, string> doConsume = null,
                                    Func<BaseDamageable, bool> onMovementHit = null,
                                    Func<Vector3, Vector3, ProjectileBase> createCustomProjectile = null)
        {
            public PlayerCardData data = data;
            public Func<bool> checkDiscardAllowed = checkDiscardAllowed;
            public Action<bool> toggleCustomUI = toggleCustomUI;
            public Action doDiscard = doDiscard;
            public Action<float> updateVelocityEarly = updateVelocityEarly;
            public Action<float> updateVelocityLate = updateVelocityLate;
            public Action<int, string> doConsume = doConsume;
            public Action<string> abortAbility = abortAbility;
            public Func<BaseDamageable, bool> onMovementHit = onMovementHit;
            public Func<Vector3, Vector3, ProjectileBase> createCustomProjectile = createCustomProjectile;
        }
        public static void Register(CustomCardInfo cardInfo)
        {//TODO: check that mandatory things are non-null
            if (customDictionary.ContainsKey(cardInfo.data.cardID)==false)
            {
                if ((int)cardInfo.data.discardAbility >200)
                {
                    if (discardNumberToCardID.ContainsKey((int)cardInfo.data.discardAbility) == false)
                    {
                        discardNumberToCardID.Add((int)cardInfo.data.discardAbility, cardInfo.data.cardID);
                    }
                    else { throw new ArgumentException($"discardAbility {(int)cardInfo.data.discardAbility} is already registered"); }
                }
                else if(cardInfo.data.discardAbility == PlayerCardData.DiscardAbility.Consumable)
                {
                    if (discardNumberToCardID.ContainsKey((int)cardInfo.data.consumableType) == false)
                    {
                        discardNumberToCardID.Add((int)cardInfo.data.consumableType, cardInfo.data.cardID);
                    }
                    else { throw new ArgumentException($"discardAbility {(int)cardInfo.data.consumableType} is already registered"); }
                }
                customDictionary.Add(cardInfo.data.cardID, cardInfo);
            }
            else { throw new ArgumentException($"cardID {cardInfo.data.cardID} is already registered"); }
        }
        public static bool StartEffect(string cardID)
        {
            return currentlyActiveUpdateEffects.Add(cardID);
        }
        public static bool StopEffect(string cardID)
        {
            return currentlyActiveUpdateEffects.Remove(cardID);
        }
        private static readonly Dictionary<string, CustomCardInfo> customDictionary = [];

        private static Dictionary<string, PlayerCardData> vanillaCardDictionary;
        private static Dictionary<string, PlayerCardData> VanillaCardDictionary
        {
            get
            {
                vanillaCardDictionary ??= new()
                {
                    {"PISTOL",Singleton<Game>.Instance.GetGameData().GetCard("PISTOL") },
                    {"RIFLE",Singleton<Game>.Instance.GetGameData().GetCard("RIFLE") },
                    {"SHOTGUN",Singleton<Game>.Instance.GetGameData().GetCard("SHOTGUN") },
                    {"UZI",Singleton<Game>.Instance.GetGameData().GetCard("UZI") },
                    {"ROCKETLAUNCHER",Singleton<Game>.Instance.GetGameData().GetCard("ROCKETLAUNCHER") },
                    {"MACHINEGUN",Singleton<Game>.Instance.GetGameData().GetCard("MACHINEGUN") },
                    {"RAPTURE",Singleton<Game>.Instance.GetGameData().GetCard("RAPTURE") },
                };
                return vanillaCardDictionary;
            }
        }
        private static readonly Dictionary<int, string> discardNumberToCardID = [];
        private static readonly HashSet<string> currentlyActiveUpdateEffects = [];
        public static readonly Dictionary<string, string> cardOverrides = [];
        public class Settings : ModSettings
        {
            //public static MelonPreferences_Category CustomCardShit;
            //public static MelonPreferences_Entry<Dictionary<string, bool>> customCardShowcase;
            private static Dictionary<string, MelonPreferences_Entry<string>> vanillaOverrides;
            public static Dictionary<string, MelonPreferences_Entry<string>> VanillaOverrides
            {
                get
                {
                    vanillaOverrides ??= new()
                        {
                            {"PISTOL", Settings.elevateOverride},
                            {"RIFLE", Settings.godspeedOverride},
                            {"SHOTGUN", Settings.purifyOverride},
                            {"UZI", Settings.stompOverride},
                            {"ROCKETLAUNCHER", Settings.dominionOverride},
                            {"MACHINEGUN", Settings.fireballOverride},
                            {"RAPTURE", Settings.boofOverride}
                        };
                    return vanillaOverrides;
                }
            }
            public enum TargetCards
            {
                None, Elevate, Godspeed, Purify, Stomp, Dominion, Fireball, Boof, Health, Ammo
            }
            //static HashSet<CustomCardInfo> CustomCards;
            public static MelonPreferences_Entry<string> elevateOverride;
            public static MelonPreferences_Entry<string> godspeedOverride;
            public static MelonPreferences_Entry<string> purifyOverride;
            public static MelonPreferences_Entry<string> stompOverride;
            public static MelonPreferences_Entry<string> dominionOverride;
            public static MelonPreferences_Entry<string> fireballOverride;
            public static MelonPreferences_Entry<string> boofOverride;
            public class InteractiveCustomCard : InteractiveValue
            {
                internal Dropdown dropdown;
                public InteractiveCustomCard(object value, Type fallbackType) : base(value, fallbackType) { }

                public override bool SupportsType(Type type) => type == typeof(CustomCardInfo);
                private void SetValueFromDropdown()
                {
                    var type = Value?.GetType() ?? FallbackType;
                    var index = dropdown.value;
                    //TODO: continue on custom MelonPreferencesManager input
                }
                public override void ConstructUI(GameObject parent)
                {
                    base.ConstructUI(parent);

                    var dropdownObj = UIFactory.CreateDropdown(mainContent, "InteractiveCustomCard", out dropdown, "None", 14, null);
                    UIFactory.SetLayoutElement(dropdownObj, minWidth: 400, minHeight: 25);
                    dropdown.onValueChanged.AddListener((val) => SetValueFromDropdown());
                }
            }
            public class ExistingCardValidator : MelonLoader.Preferences.ValueValidator
            {
                public override object EnsureValid(object value)
                {
                    string[] cards = customDictionary.Keys.ToArray().AddToArray("NONE");
                    var result = cards.Contains(((string)value).ToUpper());
                    DebugLog($"{result} {(result ? value : "None")}");
                    if (result) { return value; }
                    else { return value; }
                }

                public override bool IsValid(object value)
                {
                    string[] cards = customDictionary.Keys.ToArray().AddToArray("NONE");
                    var result = cards.Contains(((string)value).ToUpper());
                    DebugLog(result);
                    return result;
                }
            }
        }
        public override void RegisterSettings()
        {
            //Settings.CustomCardShit = MelonPreferences.CreateCategory("CustomCardShit");
            //Settings.customCardShowcase = Settings.CustomCardShit.CreateEntry<Dictionary<string, bool>>("cardShowCase", []);
            //Settings.CustomCardShit.SetFilePath("ToolsOfTheTradeSettings");
            //Settings.CustomCardShit.SaveToFile();
            base.RegisterSettings();

            var overrrideCategory = MelonPreferences.CreateCategory(Main.mainCategoryName);
            Settings.elevateOverride = overrrideCategory.CreateEntry("elevateOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.godspeedOverride = overrrideCategory.CreateEntry("godspeedOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.purifyOverride = overrrideCategory.CreateEntry("purifyOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.stompOverride = overrrideCategory.CreateEntry("stompOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.dominionOverride = overrrideCategory.CreateEntry("dominionOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.fireballOverride = overrrideCategory.CreateEntry("fireballOverride", "None", validator: new Settings.ExistingCardValidator());
            Settings.boofOverride = overrrideCategory.CreateEntry("boofOverride", "None", validator: new Settings.ExistingCardValidator());
        }
        /** modder needs to implement:
        *   PlayerCardData
        *   Various assets
        *       Card/hud textures
        *   Depending on use:
        *       Projectile from BaseProjectile
        *       DoConsumable
        *       DoDiscardAbility
        *       UseDiscardAbility
        *       Update Velocity stuff
        *       MechController::Update UI stuff
        *       MechController::FireCard -> hitscan effect stuff
        *       ?UIAbilityIndicator   //think the zipline "Rock on!" thing
        *       CardShowcase tutorial
        */
        //TODO: MechController::Update currentPassiveAbility: 690 allow passive card effects
        //MAYBE LATER: MiracleButton::OnMiracleSelected----Hell rush miracle menu
        //MAYBE LATER: UICard::GetAbilityNameFormatted                         optional
        //MAYBE LATER: UICardAesthetics::CheckDiscardAmmoVisibility----havent checked what this is
        //TODO: MechController::FireCard----hitscan stuff needs to be done
        //i think thats it
        public static void ClearActiveDiscardEffects(string cardID) => Handlers.ClearVelociticUpdates(cardID);
        private struct Handlers
        {
            static string lastActiveCustomUI = "";
            public static bool HandleCardShowCase(string cardID)
            {
                if (customDictionary.TryGetValue(cardID, out CustomCardInfo cardInfo))
                {//TODO:add showcase framework and stuff
                    //var localDictionary = Settings.customCardShowcase.Value;
                    //bool hasSeen;
                    //if (localDictionary.TryGetValue(cardID, out hasSeen) == false)
                    //{
                    //    localDictionary[cardID] = false;
                    //}
                    //if (localDictionary[cardID] == false)
                    //{

                    //}

                    //do stuff here
                    return true;
                }
                return false;
            }
            public static void ClearAllCustomCardUI()
            {
                if (lastActiveCustomUI != "")
                {
                    customDictionary[lastActiveCustomUI].toggleCustomUI(false);
                    lastActiveCustomUI = "";
                }
            }
            public static void HandleCustomCardUI(string cardID)
            {
                if (customDictionary.TryGetValue(cardID, out CustomCardInfo cardInfo))
                {//TODO:customUI
                    //if (cardID != lastActiveCustomUI && lastActiveCustomUI != "")
                    //{
                    //    customDictionary[lastActiveCustomUI].toggleCustomUI(false);
                    //}
                    //cardInfo.toggleCustomUI(true);
                }
            }
            public static void ClearVelociticUpdates(string cardID)
            {
                DebugLog("Clear currentlyActiveUpdateEffects");
                foreach (string activeCardID in currentlyActiveUpdateEffects.ToArray())
                {
                    customDictionary[activeCardID].abortAbility?.Invoke(cardID);
                }
            }
            public static bool UpdateVelocityEarly(float deltaTime)
            {
                bool ret = false;
                foreach (string cardID in currentlyActiveUpdateEffects.ToArray())
                {
                    customDictionary[cardID].updateVelocityEarly?.Invoke(deltaTime);
                    ret = true;
                }
                return ret;
            }
            public static void UpdateVelocityLate(float deltaTime)
            {
                foreach (string cardID in currentlyActiveUpdateEffects.ToArray())
                {
                    customDictionary[cardID].updateVelocityLate?.Invoke(deltaTime);
                }
            }
            public static bool OnMovementHitCustom(BaseDamageable damageable)
            {
                bool doVanillaMovementHit = true;
                foreach (string cardID in currentlyActiveUpdateEffects.ToArray())
                {
                    if (customDictionary[cardID].onMovementHit != null)
                    {
                        doVanillaMovementHit |= customDictionary[cardID].onMovementHit(damageable);
                    }//if any active effect wants to skip we skip
                }
                return doVanillaMovementHit;
            }
            public static void DoDiscard(int discardNumber)
            {
                DebugLog(discardNumber);
                if (discardNumberToCardID.TryGetValue(discardNumber, out string cardID))
                {
                    customDictionary[cardID].doDiscard();
                }
            }
            public static bool IsCustomDiscardAllowed(int discardNumber)
            {
                DebugLog(discardNumber);
                if (discardNumberToCardID.TryGetValue(discardNumber, out string cardID))
                {
                    return customDictionary[cardID]?.checkDiscardAllowed.Invoke() ?? false;
                }
                else { return true; }//MAYBE LATER: Should this be false????
            }
            //MAYBE LATER: look into enabling custom baked renderers/custom pickup meshes and stuff
            /* public static Renderer SetCardRendererRecieve(string cardID)
             {
                 DebugLog(cardID);
                 if (customDictionary.TryGetValue(cardID, out CustomCardInfo ret))
                 {
                     return ret.getBakedRenderer();
                 }
                 else { return null; }
             }*/
            public static void DoConsume(int consumeNumber, int optionalInt, string optionalString)
            {
                if (discardNumberToCardID.TryGetValue(consumeNumber, out string cardID))
                {
                    customDictionary[cardID]?.doConsume.Invoke(optionalInt, optionalString);
                }
                //TODO: Make consumable effects and injector
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ProjectileBase CreateCustomProjectile(string name, Vector3 origin, Vector3 forward)
            {
                if (customDictionary.TryGetValue(name, out CustomCardInfo cardInfo))
                {
                    var ret = cardInfo.createCustomProjectile?.Invoke(origin, forward);
                    if (ret == null)
                    {
                        throw new ArgumentException($"{name}.data.projectileID points to {name} but the corresponding createCustomProjectile is null or returned null");
                    }
                    return ret;
                }
                return null;
            }
        }
        [HarmonyPatch(typeof(MechController))]
        class _MechController
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(MechController.DoConsumable))]
            static bool CustomConsumable(PlayerCardData.ConsumableType consumableType, bool ____isAlive)
            {
                if (!____isAlive) { return false; }
                if(consumableType <= PlayerCardData.ConsumableType.GreenMemoryItem) { return true; }
                Handlers.DoConsume((int)consumableType);
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch("DoDiscardAbility")]
            static void ClearCustomEffectsOnVanillaDiscard(PlayerCardData.DiscardAbility ability)
            {
                //MAYBE LATER: Verify that this actually works
                if ((int)ability <= 200) { ClearActiveDiscardEffects(ability.ToString()); }
            }
            [HarmonyTranspiler]
            [HarmonyPatch("DoDiscardAbility")]
            public static IEnumerable<CodeInstruction> DoDiscardInject(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var my = new CodeMatcher(instructions, generator).End();
                //very end of function
                my.MatchStartBackwards(new CodeMatch(cm => cm.opcode == OpCodes.Ret));
                var retLabels = my.Labels;
                my.Insert([new CodeInstruction(OpCodes.Ldarg_1),
                           CodeInstruction.Call(typeof(Handlers), nameof(Handlers.DoDiscard), [typeof(int)])])
                  .CreateLabel(out Label label);
                //--->  if (ability == PlayerCardData.DiscardAbility.Miracle)
                //      {
                my.MatchStartBackwards(new CodeMatch(peePoo => peePoo.Branches(out Label? hereLabel)
                                                           && retLabels.Contains(hereLabel.Value)))
                  .Set(OpCodes.Bne_Un_S, label);
                return my.Instructions();
            }
            [HarmonyTranspiler]
            [HarmonyPatch("UseDiscardAbility", [typeof(PlayerCardData),
                                                typeof(int),
                                                typeof(bool),
                                                typeof(bool)])]
            public static IEnumerable<CodeInstruction> CustomDiscardAllowedCheck(IEnumerable<CodeInstruction> instructions)
            {
                var my = new CodeMatcher(instructions);

                //      else if (discardAbility != PlayerCardData.DiscardAbility.None)
                //      {
                //--->      flag = true;
                //      }
                my.MatchEndForward([new CodeMatch(Brfalse),
                                    new CodeMatch(Ldc_I4_1)])
                  .SetAndAdvance(OpCodes.Ldloc_1, null)
                  .Insert(CodeInstruction.Call(typeof(Handlers),
                                               nameof(Handlers.IsCustomDiscardAllowed),
                                               [typeof(int)]));

                return my.Instructions();
            }
            [HarmonyTranspiler]
            [HarmonyPatch("Update")]
            static IEnumerable<CodeInstruction> UpdateUIInject(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var my = new CodeMatcher(instructions, generator).Start();

                //      else
                //      {
                //          RM.ui.SetFireballUI(false);
                //--->  }
                //      this._stompAfterglowTimer -= 1f * Time.deltaTime;
                my.MatchEndForward(new CodeMatch(OpCodes.Callvirt,
                                                 AccessTools.Method(typeof(PlayerUI),
                                                                    "SetFireballUI",
                                                                    [typeof(bool)])),
                                   new CodeMatch(Ldarg_0)).ThrowIfInvalid("Didn't find SetFireballUI call");
                Label after = my.Instruction.labels[0];
                var clearCustomUIInstruction = CodeInstruction.Call(typeof(Handlers), nameof(Handlers.ClearAllCustomCardUI));

                Label clearCustomUILabel = generator.DefineLabel();
                clearCustomUIInstruction.labels.Add(clearCustomUILabel);
                CodeInstruction[] instructionsToInsert =
                [
                    Ldloc_0,
                    new CodeInstruction(OpCodes.Brfalse, clearCustomUILabel),
                    Ldloc_0,
                    CodeInstruction.LoadField(typeof(PlayerCard),"data"),
                    CodeInstruction.LoadField(typeof(PlayerCardData),"cardID"),
                    CodeInstruction.Call(typeof(Handlers), nameof(Handlers.HandleCustomCardUI)),
                    new CodeInstruction(OpCodes.Br, after),
                    clearCustomUIInstruction, //clearCustomUI label
                ];
                my.Insert(instructionsToInsert);
                return my.Instructions();
            }
            [HarmonyTranspiler]
            [HarmonyPatch("DoCardPickup")]
            static IEnumerable<CodeInstruction> DoCardPickupCustomShowcase(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var my = new CodeMatcher(instructions, generator).Start();
                CodeMatch[] jumpSearchPack =
                [
                    Ldarg_1,
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerCardData), "cardType")),
                    Ldc_I4_4,
                    Bne_Un
                ];
                //--->  if (card.cardType == PlayerCardData.Type.SpecialConsumableAutomatic)
                //      {
                //          this.FireCard(new PlayerCard
                //          {
                //              data = card
                //          });
                my.MatchStartForward(jumpSearchPack).ThrowIfInvalid("couldn't find showcase skip label");
                Label showcaseSkipLabel = my.Instruction.labels[0];

                //      if (card == null)
                //      {
                //          Debug.LogError("Can't pickup card it's null");
                //          return;
                //--->  }
                //      if (card.cardID == "FISTS")
                my.Start().MatchEndForward([Ret, Ldarg_1]).ThrowIfInvalid("couldn't find ret").Advance(1);
                my.Insert(
                [
                    CodeInstruction.LoadField(typeof(PlayerCardData),"cardID"),
                    CodeInstruction.Call(typeof(Handlers), nameof(Handlers.HandleCardShowCase)),
                    new CodeInstruction(OpCodes.Brtrue, showcaseSkipLabel),
                    Ldarg_1,
                ]);

                return my.Instructions();
            }
        }
        [HarmonyPatch(typeof(FirstPersonDrifter))]
        class _FirstPersonDrifter
        {
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(FirstPersonDrifter.UpdateVelocity))]
            static IEnumerable<CodeInstruction> UpdateVelocityEarly(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var my = new CodeMatcher(instructions, generator).Start();

                CodeMatch[] searchInstructions = [new CodeMatch(Ldarg_0),
                                      new CodeMatch(CodeInstruction.LoadField(typeof(FirstPersonDrifter), "dashing")),
                                      new CodeMatch(Brfalse)];

                //      float diagonalWalkAdjust = (this.inputX != 0f && this.inputY != 0f) ? 0.7071f : 1f;

                //--->  if (this.dashing)
                //      {
                my.MatchEndForward(searchInstructions).ThrowIfInvalid("First search");

                //--->  if (this.dashing)
                //      {
                //          this.movementVelocity.y = 0f;
                //          this.movementVelocity = this.moveDirection;
                //      }
                my.MatchStartForward(searchInstructions).ThrowIfInvalid("Second search");
                Label skipFirstHalf = my.Instruction.labels[0];

                //same as first search
                my.Start().MatchStartForward(searchInstructions).ThrowIfInvalid("first search again");
                CodeInstruction[] instructionsToInsert = [Ldarg_2,//deltaTime
                           CodeInstruction.Call(typeof(Handlers), nameof(Handlers.UpdateVelocityEarly)),
                           new CodeInstruction(OpCodes.Brtrue, skipFirstHalf)];
                my.Insert(instructionsToInsert);
                return my.Instructions();
            }
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(FirstPersonDrifter.UpdateVelocity))]
            static IEnumerable<CodeInstruction> UpdateVelocityLate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var my = new CodeMatcher(instructions, generator).End();

                //      currentVelocity = this.velocity + this.movementVelocity;
                //--->  if (this.grounded)
                //      {
                //          this._lastGroundedPosition = base.transform.position - Vector3.up * (this._capsule.height * 0.5f);
                //          return;
                //      }
                my.MatchEndBackwards(Ldarg_1, Ldarg_0);
                my.Insert([
                    Ldarg_2,
                    CodeInstruction.Call(typeof(Handlers), nameof(Handlers.UpdateVelocityLate))
                ]);
                return my.Instructions();
            }
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FirstPersonDrifter.OnMovementHitDamageable))]
            static bool OnMovementHitCustom(BaseDamageable dmg)
            {
                return Handlers.OnMovementHitCustom(dmg);
            }
        }
        [HarmonyPatch(typeof(UICard))]
        class _UICard
        {
            //[HarmonyPrefix]
            [HarmonyPatch(nameof(UICard.SetCard))]
            static void SwapCardSet(ref PlayerCard card)
            {
                if (Settings.VanillaOverrides.TryGetValue(card.data.cardID, out var value) == false)
                {
                    return;
                }
                if (value.Value.ToUpper() == "NONE")
                {
                    return;
                }
                if (customDictionary.TryGetValue(value.Value.ToUpper(), out CustomCardInfo cardInfo))
                {
                    card.data = cardInfo.data;
                    card.currentAmmo = cardInfo.data.clipSize;
                }
                else
                {
                    DebugLog($"Value is set for {card.data.cardID} but the card isn't active right now");
                }
            }
        }
        //[HarmonyPatch(typeof(UICardAesthetics))]
        class _UICardAesthetics
        {
            [HarmonyPrefix]
            [HarmonyPatch("UpdateCardAbilityDisplay")]
            static bool FloatingCardTextFix(PlayerCard card, UICardAesthetics __instance)
            {
                if ((int)card.data.discardAbility > 200 || (int)card.data.consumableType > 200)
                {
                    if (__instance.textDiscardAbility)
                    {
                        var key = card.data.cardName;
                        //Helpers.Field<AxKLocalizedText>("m_replacementPairs").GetValue<List<AxKReplacementPair>>(__instance.textDiscardAbility_Localized).Clear();
                        __instance.textDiscardAbility_Localized.localizationKey = key;
                        __instance.textDiscardAbility_Localized.Localize();//TODO:REPLACE THIS WITH INNARDS THAT DON'T *ACTUALLY* LOCALIZE
                    }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ProjectileBase))]
        class _ProjectileBase
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ProjectileBase.CreateProjectile), [typeof(string), typeof(Vector3), typeof(Vector3), typeof(ProjectileWeapon)])]
            static bool CreateCustomProjectile(string path, Vector3 origin, Vector3 forward, ProjectileWeapon parentWeapon, ref ProjectileBase __result)
            {
                var ret = Handlers.CreateCustomProjectile(path, origin, forward);
                if (ret == null)
                {
                    return true;
                }
                __result = ret;
                return false;
            }
        }
        [HarmonyPatch(typeof(CardPickupSpawner))]
        class _CardPickupSpawner
        {
            [HarmonyPrefix]
            [HarmonyPatch("Start")]
            public static void SwapInCustomCard(ref PlayerCardData ___card/*, ref int ___overrideStartingAmmo*/)
            {
                if (___card == null)
                {
                    return;
                }
                if (Settings.VanillaOverrides.TryGetValue(___card.cardID, out var setting) == false)
                {
                    return;
                }
                if (setting.Value.ToUpper() == "NONE")
                {
                    return;
                }
                if (customDictionary.TryGetValue(setting.Value.ToUpper(), out CustomCardInfo cardInfo))
                {
                    ___card = cardInfo.data;
                    //___overrideStartingAmmo = -1;
                }
                else if (VanillaCardDictionary.TryGetValue(setting.Value.ToUpper(), out PlayerCardData cardData))
                {
                    ___card = cardData;
                }
                else
                {
                    DebugLog($"Value is set for {___card.cardID} but the card isn't active right now");
                }
            }
        }
        [HarmonyPatch(typeof(BaseDamageable))]
        class _BaseDamageable
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(BaseDamageable.DropCard))]
            public static void DropCustomCard(ref PlayerCardData ___dropsCard)
            {
                _CardPickupSpawner.SwapInCustomCard(ref ___dropsCard);
                //if (___dropsCard==null)
                //{
                //    return;
                //}
                //if (Settings.VanillaOverrides.TryGetValue(___dropsCard.cardID, out var setting) == false)
                //{
                //    return;
                //}
                //if (setting.Value.ToUpper() == "NONE")
                //{
                //    return;
                //}
                //if (customDictionary.TryGetValue(setting.Value.ToUpper(), out CustomCardInfo cardInfo))
                //{
                //    ___dropsCard = cardInfo.data;
                //}
                //else
                //{
                //    DebugLog($"Value is set for {___dropsCard.cardID} but the card isn't active right now");
                //}
            }
        }
        [HarmonyPatch(typeof(EnemyJumper))]
        class _EnemyJumper
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(BaseDamageable.DropCard))]
            static void DropCustomCard(ref PlayerCardData ___dropsCard)
            {
                _CardPickupSpawner.SwapInCustomCard(ref ___dropsCard);
            }
        }
    }
}
