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
using System.Collections.Generic;
using System.Collections;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace FreeItemFriday.Skills
{
    public class PulseGrenade : FreeSkillSaturday.Behavior
    {
        public static float damageCoefficient = 3f;
        public static float duration = 1.3f;
        public static float firingDelay = 0.3f;

        public static DamageAPI.ModdedDamageType Shock2s { get; private set; }
        public static GameObject GrenadeProjectile { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<RailgunSkillDef> _railgunnerBodyFirePistol = "RoR2/DLC1/Railgunner/RailgunnerBodyFirePistol.asset";
            using RoR2Asset<SkillFamily> _railgunnerBodyPrimaryFamily = "RoR2/DLC1/Railgunner/RailgunnerBodyPrimaryFamily.asset";
            using RoR2Asset<GameObject> _railgunnerCrosshair = "RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab";
            using RoR2Asset<GameObject> _railgunnerCryochargeCrosshair = "RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab";
            using Task<GameObject> _grenadeProjectile = CreateGrenadeProjectileAsync(Shock2s = DamageAPI.ReserveDamageType());

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
            (Content.Skills.RailgunnerElectricGrenade as RailgunSkillDef).offlineIcon = (await _railgunnerBodyFirePistol).offlineIcon;

            Content.Achievements.RailgunnerHipster = Expansion.DefineAchievementForSkill("RailgunnerHipster", Content.Skills.RailgunnerElectricGrenade)
                .SetIconSprite(Content.Skills.RailgunnerElectricGrenade.icon)
                .SetTrackerTypes(typeof(RailgunnerHipsterAchievement), null);

            SkillFamily railgunnerBodyPrimaryFamily = await _railgunnerBodyPrimaryFamily;
            railgunnerBodyPrimaryFamily.AddSkill(Content.Skills.RailgunnerElectricGrenade, Content.Achievements.RailgunnerHipster.UnlockableDef);

            GameObject railgunnerCrossHair = await _railgunnerCrosshair;
            SetupRailgunnerCrosshair.prefab = Prefabs.ClonePrefab(railgunnerCrossHair.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
            Color color = new Color32(198, 169, 217, 255);
            foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
            {
                image.color = color;
            }
            foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = color;
            }
            GameObject railgunnerCryochargeCrosshair =  await _railgunnerCryochargeCrosshair;
            Instantiate(railgunnerCryochargeCrosshair.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform);
            railgunnerCrossHair.AddComponent<SetupRailgunnerCrosshair>();

            GrenadeProjectile = await _grenadeProjectile;
        }

        public static async Task<GameObject> CreateGrenadeProjectileAsync(DamageAPI.ModdedDamageType shock2s)
        {
            using Task<GameObject> _grenadeExplosionEffect = CreateGrenadeExplosionEffectAsync();
            using Task<GameObject> _grenadeGhost = CreateGrenadeGhostAsync();
            using RoR2Asset<GameObject> _engiGrenadeProjectile = "RoR2/Base/Engi/EngiGrenadeProjectile.prefab";
            using RoR2Asset<NetworkSoundEventDef> _nseCommandoGrenadeBounce = "RoR2/Base/Commando/nseCommandoGrenadeBounce.asset";
            using RoR2Asset<SkinDef> _skinRailGunnerAlt = "RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset";
            
            GameObject grenadeProjectile = Prefabs.ClonePrefab(await _engiGrenadeProjectile, "RailgunnerElectricGrenadeProjectile");
            if (grenadeProjectile.TryGetComponent(out ProjectileDamage projectileDamage))
            {
                projectileDamage.damageType = DamageType.Shock5s;
            }
            if (grenadeProjectile.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
            {
                projectileImpactExplosion.blastRadius = 5f;
                projectileImpactExplosion.lifetimeAfterImpact = 0.15f;
                projectileImpactExplosion.lifetimeExpiredSound = Expansion.DefineNetworkSoundEvent("nseElectricGrenadeExpired").SetEventName("Play_item_use_BFG_zaps");
                projectileImpactExplosion.offsetForLifetimeExpiredSound = 0.05f;
                projectileImpactExplosion.impactEffect = await _grenadeExplosionEffect;
            }
            if (grenadeProjectile.TryGetComponent(out RigidbodySoundOnImpact rigidbodySoundOnImpact))
            {
                rigidbodySoundOnImpact.impactSoundString = string.Empty;
                rigidbodySoundOnImpact.networkedSoundEvent = await _nseCommandoGrenadeBounce;
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
            grenadeProjectile.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(shock2s);
            DestroyImmediate(grenadeProjectile.GetComponent<AntiGravityForce>());
            GameObject grenadeGhost = await _grenadeGhost;
            grenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = grenadeGhost;
            Expansion.ProjectilePrefabs.Add(grenadeProjectile);

            GameObject grenadeGhostReskin = Prefabs.ClonePrefab(grenadeGhost, "RailgunnerElectricGrenadeGhostReskin");
            if (grenadeGhostReskin.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenadeReskin) && mdlEngiGrenadeReskin.TryGetComponent(out MeshRenderer meshRendererReskin))
            {
                Material matRailgunnerElectricGrenadeAlt = new Material(meshRendererReskin.sharedMaterial);
                matRailgunnerElectricGrenadeAlt.SetTexture("_MainTex", Assets.LoadAsset<Texture>("texRailgunnerElectricGrenadeAlt"));
                meshRendererReskin.sharedMaterial = matRailgunnerElectricGrenadeAlt;
            }

            SkinDef skinRailGunnerAlt = await _skinRailGunnerAlt;
            ArrayUtils.ArrayAppend(ref skinRailGunnerAlt.projectileGhostReplacements, new SkinDef.ProjectileGhostReplacement
            {
                projectilePrefab = grenadeProjectile,
                projectileGhostReplacementPrefab = grenadeGhostReskin
            });

            return grenadeProjectile;
        }

        public static async Task<GameObject> CreateGrenadeExplosionEffectAsync()
        {
            using RoR2Asset<GameObject> _captainTazerSupplyDropNova = "RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab";

            GameObject grenadeExplosionEffect = Prefabs.ClonePrefab(await _captainTazerSupplyDropNova, "RailgunnerElectricGrenadeExplosion");
            grenadeExplosionEffect.GetComponent<EffectComponent>().soundName = "Play_roboBall_attack1_explode";
            grenadeExplosionEffect.GetComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            Expansion.AddEffectPrefab(grenadeExplosionEffect);
            return grenadeExplosionEffect;
        }

        public static async Task<GameObject> CreateGrenadeGhostAsync()
        {
            using RoR2Asset<GameObject> _engiGrenadeGhost = "RoR2/Base/Engi/EngiGrenadeGhost.prefab";

            GameObject grenadeGhost = Prefabs.ClonePrefab(await _engiGrenadeGhost, "RailgunnerElectricGrenadeGhost");
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
            SetupRailgunnerCrosshair.prefab = Prefabs.ClonePrefab(railgunnerCrosshair.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
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

        public void OnEnable()
        {
            IL.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
        }

        public void OnDisable()
        {
            IL.RoR2.SetStateOnHurt.OnTakeDamageServer -= SetStateOnHurt_OnTakeDamageServer;
        }

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
            else Logger.LogError($"{nameof(PulseGrenade)}.{nameof(SetStateOnHurt_OnTakeDamageServer)} IL hook failed!");
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