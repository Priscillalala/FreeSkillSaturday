using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Items;
using RoR2.Skills;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HG;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using EntityStates.BrotherMonster;
using UnityEngine.Networking;
using FreeItemFriday.Achievements;
using RoR2.Projectile;
using R2API;
using UnityEngine.UI;
using RoR2.UI;
using TMPro;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;

namespace FreeItemFriday.Skills
{
    public class ElectricGrenadeSkill : FreeSkillSaturday.Behavior
    {
        public static float damageCoefficient = 3f;
        public static float duration = 1.3f;
        public static float firingDelay = 0.3f;

        public static DamageAPI.ModdedDamageType Shock2s { get; private set; }
        public static GameObject GrenadeProjectile { get; private set; }

        public NetworkSoundEventDef nseElectricGrenadeExpired;

        public void Awake()
        {
            Content.Skills.RailgunnerElectricGrenade = Expansion.DefineSkill<RailgunSkillDef>("RailgunnerElectricGrenade")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texRailgunnerElectricGrenadeIcon"))
                .SetActivationState(typeof(EntityStates.Railgunner.Weapon.FireElectricGrenade), "Weapon")
                .SetInterruptPriority(EntityStates.InterruptPriority.Any)
                .SetCooldown(0f)
                .SetMaxStock(0)
                .SetRechargeStock(0)
                .SetRequiredStock(0)
                .SetStockToConsume(0)
                .SetKeywordTokens("KEYWORD_SHOCKING");
            IvyLibrary.LoadAddressableAsync<RailgunSkillDef>("RoR2/DLC1/Railgunner/RailgunnerBodyFirePistol.asset")
                .WhenCompleted(t => (Content.Skills.RailgunnerElectricGrenade as RailgunSkillDef).offlineIcon = t.Result.offlineIcon);

            Content.Achievements.RailgunnerTravelDistance = Expansion.DefineAchievementForSkill("RailgunnerTravelDistance", Content.Skills.RailgunnerElectricGrenade)
                .SetIconSprite(Content.Skills.RailgunnerElectricGrenade.icon)
                .SetTrackerTypes(typeof(RailgunnerTravelDistanceAchievement), null);

            /*Addressables.LoadAssetAsync<SkillFamily>("RoR2/DLC1/Railgunner/RailgunnerBodyPrimaryFamily.asset").Completed += handle =>
            {
                handle.Result.AddSkill(Content.Skills.RailgunnerElectricGrenade, Content.Achievements.RailgunnerTravelDistance.UnlockableDef);
            };*/

            IvyLibrary.LoadAddressableAsync<SkillFamily>("RoR2/DLC1/Railgunner/RailgunnerBodyPrimaryFamily.asset")
                .WhenCompleted(t => t.Result.AddSkill(Content.Skills.RailgunnerElectricGrenade, Content.Achievements.RailgunnerTravelDistance.UnlockableDef));

            Shock2s = DamageAPI.ReserveDamageType();

            nseElectricGrenadeExpired = Expansion.DefineNetworkSoundEvent("nseElectricGrenadeExpired").SetEventName("Play_item_use_BFG_zaps");

            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab", out var _railgunnerCryochargeCrosshair);
            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab").WhenCompleted(t =>
            {
                SetupRailgunnerCrosshair.prefab = IvyLibrary.CreatePrefab(t.Result.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
                Color color = new Color32(198, 169, 217, 255);
                foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
                {
                    image.color = color;
                }
                foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.color = color;
                }
                _railgunnerCryochargeCrosshair.WhenCompleted(t => Instantiate(t.Result.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform));
                t.Result.AddComponent<SetupRailgunnerCrosshair>();
            });

            CreateGrenadeProjectileAsync().WhenCompleted(t => GrenadeProjectile = t.Result);
            //SetupCrosshairAsync();

            IL.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
        }

        public static void Assign<T>(Expression<Func<T>> expr, T value)
        {
            var parameterExpression = Expression.Parameter(typeof(T));
            Action<T> assign = Expression.Lambda<Action<T>>(Expression.Assign(expr.Body, parameterExpression), parameterExpression).Compile();
            assign(value);
        }

        public async Task<GameObject> CreateGrenadeProjectileAsync()
        {
            Task<GameObject> _grenadeExplosionEffect = CreateGrenadeExplosionEffectAsync();
            Task<GameObject> _grenadeGhost = CreateGrenadeGhostAsync();
            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeProjectile.prefab", out var _engiGrenadeProjectile);
            IvyLibrary.LoadAddressableAsync<NetworkSoundEventDef>("RoR2/Base/Commando/nseCommandoGrenadeBounce.asset", out var _nseCommandoGrenadeBounce);
            IvyLibrary.LoadAddressableAsync<SkinDef>("RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset", out var _skinRailGunnerAlt);
            //Task<GameObject> _engiGrenadeProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeProjectile.prefab").Task;
            //Task<NetworkSoundEventDef> _nseCommandoGrenadeBounce = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Commando/nseCommandoGrenadeBounce.asset").Task;
            //Task<SkinDef> _skinRailGunnerAlt = Addressables.LoadAssetAsync<SkinDef>("RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset").Task;
            GameObject grenadeProjectile = IvyLibrary.CreatePrefab(await _engiGrenadeProjectile, "RailgunnerElectricGrenadeProjectile");
            if (grenadeProjectile.TryGetComponent(out ProjectileDamage projectileDamage))
            {
                projectileDamage.damageType = DamageType.Shock5s;
            }
            if (grenadeProjectile.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
            {
                projectileImpactExplosion.blastRadius = 5f;
                projectileImpactExplosion.lifetimeAfterImpact = 0.15f;
                projectileImpactExplosion.lifetimeExpiredSound = nseElectricGrenadeExpired;
                projectileImpactExplosion.offsetForLifetimeExpiredSound = 0.05f;
                _grenadeExplosionEffect.WhenCompleted(t => projectileImpactExplosion.impactEffect = t.Result);
            }
            if (grenadeProjectile.TryGetComponent(out RigidbodySoundOnImpact rigidbodySoundOnImpact))
            {
                rigidbodySoundOnImpact.impactSoundString = string.Empty;
                _nseCommandoGrenadeBounce.WhenCompleted(t => rigidbodySoundOnImpact.networkedSoundEvent = t.Result);
            }
            if (grenadeProjectile.TryGetComponent(out SphereCollider sphereCollider))
            {
                sphereCollider.radius = 0.8f;
            }
            if (grenadeProjectile.TryGetComponent(out ProjectileSimple projectileSimple))
            {
                projectileSimple.desiredForwardSpeed = 60f;
            }
            if (grenadeProjectile.TryGetComponent(out ApplyTorqueOnStart applyTorqueOnStart))
            {
                applyTorqueOnStart.localTorque = Vector3.one * 100f;
            }
            grenadeProjectile.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(Shock2s);
            DestroyImmediate(grenadeProjectile.GetComponent<AntiGravityForce>());
            _grenadeGhost.WhenCompleted(x => grenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = x.Result);
            Expansion.ProjectilePrefabs.Add(grenadeProjectile);

            GameObject grenadeGhostReskin = IvyLibrary.CreatePrefab(await _grenadeGhost, "RailgunnerElectricGrenadeGhostReskin");
            if (grenadeGhostReskin.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenadeReskin) && mdlEngiGrenadeReskin.TryGetComponent(out MeshRenderer meshRendererReskin))
            {
                Material matRailgunnerElectricGrenadeAlt = new Material(meshRendererReskin.sharedMaterial);
                matRailgunnerElectricGrenadeAlt.SetTexture("_MainTex", Assets.LoadAsset<Texture>("texRailgunnerElectricGrenadeAlt"));
                meshRendererReskin.sharedMaterial = matRailgunnerElectricGrenadeAlt;
            }
            _skinRailGunnerAlt.WhenCompleted(t => ArrayUtils.ArrayAppend(ref t.Result.projectileGhostReplacements, new SkinDef.ProjectileGhostReplacement
            {
                projectilePrefab = grenadeProjectile,
                projectileGhostReplacementPrefab = grenadeGhostReskin
            }));

            return grenadeProjectile;
        }

        public async Task<GameObject> CreateGrenadeExplosionEffectAsync()
        {
            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab", out var _captainTazerSupplyDropNova);
            //Task<GameObject> loadCaptainTazerSupplyDropNova = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab").Task;
            GameObject grenadeExplosionEffect = IvyLibrary.CreatePrefab(await _captainTazerSupplyDropNova, "RailgunnerElectricGrenadeExplosion");
            grenadeExplosionEffect.GetComponent<EffectComponent>().soundName = "Play_roboBall_attack1_explode";
            grenadeExplosionEffect.GetComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            Expansion.AddEffectPrefab(grenadeExplosionEffect);
            return grenadeExplosionEffect;
        }

        public async Task<GameObject> CreateGrenadeGhostAsync()
        {
            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeGhost.prefab", out var _engiGrenadeGhost);
            //Task<GameObject> loadEngiGrenadeGhost = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeGhost.prefab").Task;
            GameObject grenadeGhost = IvyLibrary.CreatePrefab(await _engiGrenadeGhost, "RailgunnerElectricGrenadeGhost");
            if (grenadeGhost.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenade))
            {
                mdlEngiGrenade.localScale = new Vector3(0.3f, 0.3f, 0.5f);
                if (mdlEngiGrenade.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    Material matRailgunnerElectricGrenade = new Material(meshRenderer.sharedMaterial);
                    matRailgunnerElectricGrenade.SetColor("_EmColor", new Color32(55, 188, 255, 255));
                    matRailgunnerElectricGrenade.SetFloat("_EmPower", 2f);
                    matRailgunnerElectricGrenade.SetTexture("_MainTex", Assets.LoadAsset<Texture>("texRailgunnerElectricGrenade"));
                    meshRenderer.sharedMaterial = matRailgunnerElectricGrenade;
                }
            }
            return grenadeGhost;
        }

        /*public async void SetupCrosshairAsync()
        {
            Task<GameObject> loadRailgunnerCrosshair = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab").Task;
            Task<GameObject> loadRailgunnerCryochargeCrosshair = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab").Task;
            GameObject railgunnerCrosshair = await loadRailgunnerCrosshair;
            SetupRailgunnerCrosshair.prefab = IvyLibrary.CreatePrefab(railgunnerCrosshair.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
            Color color = new Color32(198, 169, 217, 255);
            foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
            {
                image.color = color;
            }
            foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = color;
            }
            loadRailgunnerCryochargeCrosshair.WhenCompleted(t => Instantiate(t.Result.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform));
            railgunnerCrosshair.AddComponent<SetupRailgunnerCrosshair>();
        }*/

        private void SetStateOnHurt_OnTakeDamageServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<SetStateOnHurt>(nameof(SetStateOnHurt.SetShock))))
            {
                c.Emit(OpCodes.Ldarg, 1);
                c.EmitDelegate<Func<float, DamageReport, float>>((duration, damageReport) =>
                {
                    if (damageReport.damageInfo.HasModdedDamageType(Shock2s))
                    {
                        return 2f * damageReport.damageInfo.procCoefficient;
                    }
                    return duration;
                });
            }
            else Logger.LogError($"{nameof(ElectricGrenadeSkill)}.{nameof(SetStateOnHurt_OnTakeDamageServer)} IL hook failed!");
        }

        public class SetupRailgunnerCrosshair : MonoBehaviour
        {
            public static GameObject prefab;

            public void Awake()
            {
                GameObject instance = Instantiate(prefab, transform);
                if (TryGetComponent(out CrosshairController crosshairController))
                {
                    ArrayUtils.ArrayAppend(ref crosshairController.skillStockSpriteDisplays, new CrosshairController.SkillStockSpriteDisplay
                    {
                        requiredSkillDef = Content.Skills.RailgunnerElectricGrenade,
                        skillSlot = SkillSlot.Primary,
                        target = instance
                    });
                }
                Destroy(this);
            }
        }
    }
}
/*Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeProjectile.prefab").Completed += handle =>
            {
                GrenadeProjectile = IvyLibrary.CreatePrefab(handle.Result, "RailgunnerElectricGrenadeProjectile");
                if (GrenadeProjectile.TryGetComponent(out ProjectileDamage projectileDamage))
                {
                    projectileDamage.damageType = DamageType.Shock5s;
                }
                if (GrenadeProjectile.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
                {
                    projectileImpactExplosion.blastRadius = 5f;
                    projectileImpactExplosion.lifetimeAfterImpact = 0.15f;
                    projectileImpactExplosion.lifetimeExpiredSound = nseElectricGrenadeExpired;
                    projectileImpactExplosion.offsetForLifetimeExpiredSound = 0.05f;
                    Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab").Completed += handle =>
                    {
                        grenadeExplosionEffect = IvyLibrary.CreatePrefab(handle.Result, "RailgunnerElectricGrenadeExplosion");
                        grenadeExplosionEffect.GetComponent<EffectComponent>().soundName = "Play_roboBall_attack1_explode";
                        grenadeExplosionEffect.GetComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
                        Expansion.AddEffectPrefab(grenadeExplosionEffect);
                        projectileImpactExplosion.impactEffect = grenadeExplosionEffect;
                    };
                }
                if (GrenadeProjectile.TryGetComponent(out RigidbodySoundOnImpact rigidbodySoundOnImpact))
                {
                    rigidbodySoundOnImpact.impactSoundString = string.Empty;
                    Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Commando/nseCommandoGrenadeBounce.asset").Completed += handle =>
                    {
                        rigidbodySoundOnImpact.networkedSoundEvent = handle.Result;
                    };
                }
                if (GrenadeProjectile.TryGetComponent(out SphereCollider sphereCollider))
                {
                    sphereCollider.radius = 0.8f;
                }
                if (GrenadeProjectile.TryGetComponent(out ProjectileSimple projectileSimple))
                {
                    projectileSimple.desiredForwardSpeed = 60f;
                }
                if (GrenadeProjectile.TryGetComponent(out ApplyTorqueOnStart applyTorqueOnStart))
                {
                    applyTorqueOnStart.localTorque = Vector3.one * 100f;
                }
                GrenadeProjectile.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(Shock2s);
                DestroyImmediate(GrenadeProjectile.GetComponent<AntiGravityForce>());
                Expansion.ProjectilePrefabs.Add(GrenadeProjectile);

                Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeGhost.prefab").Completed += handle =>
                {
                    GameObject grenadeGhost = IvyLibrary.CreatePrefab(handle.Result, "RailgunnerElectricGrenadeGhost");
                    if (grenadeGhost.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenade))
                    {
                        mdlEngiGrenade.localScale = new Vector3(0.3f, 0.3f, 0.5f);
                        if (mdlEngiGrenade.TryGetComponent(out MeshRenderer meshRenderer))
                        {
                            Material matRailgunnerElectricGrenade = new Material(meshRenderer.sharedMaterial);
                            matRailgunnerElectricGrenade.SetColor("_EmColor", new Color32(55, 188, 255, 255));
                            matRailgunnerElectricGrenade.SetFloat("_EmPower", 2f);
                            matRailgunnerElectricGrenade.SetTexture("_MainTex", Assets.LoadAsset<Texture>("texRailgunnerElectricGrenade"));
                            meshRenderer.sharedMaterial = matRailgunnerElectricGrenade;
                        }
                    }
                    GrenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = grenadeGhost;

                    GameObject grenadeGhostReskin = IvyLibrary.CreatePrefab(grenadeGhost, "RailgunnerElectricGrenadeGhostReskin");
                    if (grenadeGhostReskin.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenadeReskin) && mdlEngiGrenadeReskin.TryGetComponent(out MeshRenderer meshRendererReskin))
                    {
                        Material matRailgunnerElectricGrenadeAlt = new Material(meshRendererReskin.sharedMaterial);
                        //matRailgunnerElectricGrenade.SetColor("_EmColor", new Color32());
                        matRailgunnerElectricGrenadeAlt.SetTexture("_MainTex", Assets.LoadAsset<Texture>("texRailgunnerElectricGrenadeAlt"));
                        meshRendererReskin.sharedMaterial = matRailgunnerElectricGrenadeAlt;
                    }

                    Addressables.LoadAssetAsync<SkinDef>("RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset").Completed += handle =>
                    {
                        ArrayUtils.ArrayAppend(ref handle.Result.projectileGhostReplacements, new SkinDef.ProjectileGhostReplacement
                        {
                            projectilePrefab = GrenadeProjectile,
                            projectileGhostReplacementPrefab = grenadeGhostReskin
                        });
                    };
                };
            };

            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab").Completed += handle =>
            {
                SetupRailgunnerCrosshair.prefab = IvyLibrary.CreatePrefab(handle.Result.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
                Color color = new Color32(198, 169, 217, 255);
                foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
                {
                    image.color = color;
                }
                foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.color = color;
                }
                Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab").Completed += handle =>
                {
                    Instantiate(handle.Result.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform);
                };
                handle.Result.AddComponent<SetupRailgunnerCrosshair>();
            };*/