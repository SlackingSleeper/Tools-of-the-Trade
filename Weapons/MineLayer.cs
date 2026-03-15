using UnityEngine;
using HarmonyLib;
using MelonLoader;

namespace ToolsOfTheTrade.Weapons
{
    [HarmonyPatch]
    //[HarmonyPatchCategory("MineLayer")]
    internal class MineLayer : MelonMod
    {
        private static void Log(object message)
        {
            Melon<MineLayer>.Logger.Msg(message);
        }
        internal class RocketMine : ProjectileBase
        {
            public static void ExplodePlayer2(Vector3 origin, float radius, float force)
            {
                float distance = Vector3.Distance(origin, RM.mechController.playerCamera.transform.position);
                if (distance < radius)
                {
                    Debug.DrawLine(origin, RM.mechController.playerCamera.transform.position, Color.red, 5f);
                    Vector3 normalized = (origin - RM.mechController.playerCamera.transform.position).normalized;
                    normalized.x *= 0.75f;
                    normalized.z *= 0.75f;
                    normalized.y = 0f;
                    RM.drifter.ForceJump(force, false, true, 1f);
                    RM.drifter.AddVelocity(-normalized * force);
                    RM.mechController.ForceStompAfterglow();
                    RM.mechController.TriggerInvincibilityTimer();
                }
            }
            public override void OnSpawn(Vector3 origin, Vector3 forward)
            {
                this._projectileModelHolder = this.gameObject.transform.Find("Body");
                this._collisionLayerMask = LayerMask.GetMask(["Player"]);
                this._explosionLayerMask = LayerMask.GetMask(["Player"]);
                this._collisionRadiusDamageable = this.transform.localScale.magnitude / 2;//   /2 cuz radius vs diameter
                this._collisionRadiusWorld = 0;
                this._hitType = ProjectileBase.HitType.Explosion;
                this._explosionPlayerForceMax = 44;
                this.ignoreBombHits = true;
                this._explosionRadius = 20;
                base.OnSpawn(origin, forward);
            }
            public override void UpdateTime(float deltaTime)
            {
                return;
            }
            public override void AfterHitBehavior(ProjectileHit hit)
            {
                return;
            }
            public override void OnProjectileHit(ProjectileBase.ProjectileHit hit)
            {
                Log("RocketMine OnProjectileHit");
                //this._collisionLayerMask = LayerMask.GetMask(new string[] { "Player" });
                base.OnProjectileHit(hit);
            }
            public override bool OnCollide(ProjectileBase.ProjectileHit hit, bool testDamageable)
            {
                Log("RocketMine OnCollide");
                return base.OnCollide(hit, testDamageable);
            }
            public override void OnExplode(ProjectileBase.ProjectileHit hit)
            {
                Log($"OnExplode position {this.transform.position} vs {hit}");
                base.OnExplode(hit);
                if (this.IsProjectileAlive)
                {
                    this.ExplodeFX();
                    ProjectileBase.Explode(new Collider[100], base.transform.position, this._explosionRadius, this.ExplosionDamage, this._explosionLayerMask, this._damageTarget, base.gameObject, true);
                    if (this._explosionPushesPlayer)
                    {
                        ProjectileBase.ExplodePlayer(base.transform.position, this._explosionRadius, this._explosionPlayerForceMax);
                    }
                    this.OnDespawn();
                }
            }
            public override void OnDespawn()
            {
                Log("OnDespawn => isAlive == false");
                base.OnDespawn();
            }

        }
        static private UnityEngine.AssetBundle asset;
        static private UnityEngine.GameObject minePrefab;
        public override void OnPreferencesSaved() => OnPreferencesLoaded();
        public override void OnPreferencesLoaded()
        {
            if (Settings.MineLayer.Value == true)
            {
                Log("Awoke");
                if (asset == null)
                {
                    asset = AssetBundle.LoadFromMemory(Resources.Resources.rocketmine);
                    if (asset == null)
                    {
                        Log("failed to LoadFromMemory");
                        return;
                    }
                    minePrefab = asset.LoadAsset<GameObject>("RocketMine");
                    if (minePrefab == null)
                    {
                        Log("failed to LoadAsset");
                        return;
                    }
                    else
                    {
                        Log("Mine to LoadAsset");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MechController))]
        internal class MechController_
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetZiplinePoint")]
            public static bool AlwaysOK(ref MechController.ZiplinePoint __result)
            {
                MechController.ZiplinePoint ziplinePoint = default;
                ziplinePoint.hasValidPoint = true;
                __result = ziplinePoint;
                return false;
            }
        }
        [HarmonyPatch(typeof(FirstPersonDrifter))]
        internal class FirstPersonDrifter_
        {
            [HarmonyPrefix]
            [HarmonyPatch("DoZipline")]
            public static bool LayMine()
            {
                ProjectileRocket[] rocketList = UnityEngine.Object.FindObjectsOfType<ProjectileRocket>();

                foreach (ProjectileRocket rocket in rocketList)
                {
                    Vector3 position = rocket.transform.position;
                    rocket.OnDespawn();
                    GameObject newMine = ObjectPool.Spawn(minePrefab, position, Quaternion.identity);

                    if (newMine.TryGetComponent<RocketMine>(out RocketMine component) == false)
                    {
                        component = newMine.AddComponent<RocketMine>();
                    }
                    component.OnSpawn(position, Vector3.forward);
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ProjectileBase))]
        internal class ProjectileBase_
        {
            [HarmonyPostfix]
            [HarmonyPatch("CreateProjectile", [typeof(string),
                                               typeof(Vector3),
                                               typeof(Vector3),
                                               typeof(ProjectileWeapon)])]
            public static void WeakRocket(string path, ref ProjectileBase __result)
            {
                if (path != "Projectiles/ProjectileRocket")
                {
                    return;
                }
                __result._explosionDamage = 4;
                __result.Velocity = __result.Velocity.normalized * __result._initialVelocity.magnitude * 0.75f;
            }
        }
    }
}
