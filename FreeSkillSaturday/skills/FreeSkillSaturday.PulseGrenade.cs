using RoR2.Skills;
using HG;
using FreeItemFriday.Achievements;
using RoR2.Projectile;
using UnityEngine.UI;
using RoR2.UI;
using TMPro;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class PulseGrenade
    {
        public static bool enabled = true;
        public static float damageCoefficient = 3f;
        public static float duration = 1.3f;
        public static float firingDelay = 0.3f;

        public static DamageAPI.ModdedDamageType Shock2s { get; private set; }
        public static GameObject GrenadeExplosionEffect { get; private set; }
        public static GameObject GrenadeGhost { get; private set; }
        public static GameObject GrenadeProjectile { get; private set; }

        public static void Init()
        {
            const string SECTION = "Pulse Grenade";
            instance.SkillsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.SkillsConfig.Bind(ref damageCoefficient, SECTION, "Damage Coefficient");
            instance.SkillsConfig.Bind(ref duration, SECTION, "Duration");
            instance.SkillsConfig.Bind(ref firingDelay, SECTION, "Firing Delay");
            if (enabled)
            {
                Shock2s = DamageAPI.ReserveDamageType();

                instance.loadStaticContentAsync += CreateSkillAsync;
                instance.loadStaticContentAsync += ModifyRailgunnerCrosshairAsync;
                instance.loadStaticContentAsync += CreateGrenadeProjectileAsync;
                IL.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
            }
        }

        private static IEnumerator<float> CreateSkillAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                texRailgunnerElectricGrenadeIcon: args.assets.LoadAsync<Sprite>("texRailgunnerElectricGrenadeIcon"),
                RailgunnerBodyFirePistol: Addressables.LoadAssetAsync<RailgunSkillDef>("RoR2/DLC1/Railgunner/RailgunnerBodyFirePistol.asset"),
                RailgunnerBodyPrimaryFamily: Addressables.LoadAssetAsync<SkillFamily>("RoR2/DLC1/Railgunner/RailgunnerBodyPrimaryFamily.asset")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    Skills.RailgunnerElectricGrenade = args.content.DefineSkill<RailgunSkillDef>("RailgunnerElectricGrenade")
                        .SetIconSprite(loadOps.texRailgunnerElectricGrenadeIcon.asset)
                        .SetActivationState(typeof(EntityStates.Railgunner.Weapon.FireElectricGrenade), "Weapon")
                        .SetInterruptPriority(EntityStates.InterruptPriority.Any)
                        .SetCooldown(0f)
                        .SetMaxStock(0)
                        .SetRechargeStock(0)
                        .SetRequiredStock(0)
                        .SetStockToConsume(0)
                        .SetKeywordTokens("KEYWORD_SHOCKING");
                    Skills.RailgunnerElectricGrenade.offlineIcon = loadOps.RailgunnerBodyFirePistol.Result.offlineIcon;

                    ref SkillFamily.Variant skillVariant = ref loadOps.RailgunnerBodyPrimaryFamily.Result.AddSkill(Skills.RailgunnerElectricGrenade);
                    Achievements.RailgunnerHipster = args.content.DefineAchievementForSkill("RailgunnerHipster", ref skillVariant)
                        .SetIconSprite(Skills.RailgunnerElectricGrenade.icon)
                        .SetTrackerTypes(typeof(RailgunnerHipsterAchievement), null);
                    // Match achievement identifiers from 1.6.1
                    Achievements.RailgunnerHipster.AchievementDef.identifier = "FSS_RailgunnerHipster";
                }
            };
        }

        private static IEnumerator<float> ModifyRailgunnerCrosshairAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                RailgunnerCrosshair: Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab"),
                RailgunnerCryochargeCrosshair: Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    SetupRailgunnerCrosshair.prefab = Ivyl.ClonePrefab(loadOps.RailgunnerCrosshair.Result.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
                    Color color = new Color32(198, 169, 217, 255);
                    foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
                    {
                        image.color = color;
                    }
                    foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        text.color = color;
                    }
                    Instantiate(loadOps.RailgunnerCryochargeCrosshair.Result.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform);
                    loadOps.RailgunnerCrosshair.Result.AddComponent<SetupRailgunnerCrosshair>();
                }
            };
        }

        private static IEnumerator<float> CreateGrenadeProjectileAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                EngiGrenadeProjectile: Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeProjectile.prefab"),
                CaptainTazerSupplyDropNova: Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab"),
                nseCommandoGrenadeBounce: Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Commando/nseCommandoGrenadeBounce.asset"),
                EngiGrenadeGhost: Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeGhost.prefab"),
                texRailgunnerElectricGrenade: args.assets.LoadAsync<Texture>("texRailgunnerElectricGrenade"),
                texRailgunnerElectricGrenadeAlt: args.assets.LoadAsync<Texture>("texRailgunnerElectricGrenadeAlt"),
                skinRailGunnerAlt: Addressables.LoadAssetAsync<SkinDef>("RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    GrenadeProjectile = Ivyl.ClonePrefab(loadOps.EngiGrenadeProjectile.Result, "RailgunnerElectricGrenadeProjectile");
                    if (GrenadeProjectile.TryGetComponent(out ProjectileDamage projectileDamage))
                    {
                        projectileDamage.damageType = DamageType.Shock5s;
                    }

                    GrenadeExplosionEffect = Ivyl.ClonePrefab(loadOps.CaptainTazerSupplyDropNova.Result, "RailgunnerElectricGrenadeExplosion");
                    GrenadeExplosionEffect.GetComponent<EffectComponent>().soundName = "Play_roboBall_attack1_explode";
                    GrenadeExplosionEffect.GetComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
                    args.content.AddEffectPrefab(GrenadeExplosionEffect);

                    if (GrenadeProjectile.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
                    {
                        projectileImpactExplosion.blastRadius = 5f;
                        projectileImpactExplosion.lifetimeAfterImpact = 0.15f;
                        projectileImpactExplosion.lifetimeExpiredSound = instance.Content.DefineNetworkSoundEvent("nseElectricGrenadeExpired").SetEventName("Play_item_use_BFG_zaps");
                        projectileImpactExplosion.offsetForLifetimeExpiredSound = 0.05f;
                        projectileImpactExplosion.impactEffect = GrenadeExplosionEffect;
                    }
                    if (GrenadeProjectile.TryGetComponent(out RigidbodySoundOnImpact rigidbodySoundOnImpact))
                    {
                        rigidbodySoundOnImpact.impactSoundString = string.Empty;
                        rigidbodySoundOnImpact.networkedSoundEvent = loadOps.nseCommandoGrenadeBounce.Result;
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

                    GrenadeGhost = Ivyl.ClonePrefab(loadOps.EngiGrenadeGhost.Result, "RailgunnerElectricGrenadeGhost");
                    if (GrenadeGhost.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenade))
                    {
                        mdlEngiGrenade.localScale = new Vector3(0.3f, 0.3f, 0.5f);
                        if (mdlEngiGrenade.TryGetComponent(out MeshRenderer meshRenderer))
                        {
                            Material matRailgunnerElectricGrenade = new Material(meshRenderer.sharedMaterial);
                            matRailgunnerElectricGrenade.SetColor("_EmColor", new Color32(55, 188, 255, 255));
                            matRailgunnerElectricGrenade.SetFloat("_EmPower", 2f);
                            matRailgunnerElectricGrenade.SetTexture("_MainTex", loadOps.texRailgunnerElectricGrenade.asset);
                            meshRenderer.sharedMaterial = matRailgunnerElectricGrenade;
                        }
                    }

                    GrenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = GrenadeGhost;
                    args.content.projectilePrefabs.Add(GrenadeProjectile);

                    GameObject grenadeGhostReskin = Ivyl.ClonePrefab(GrenadeGhost, "RailgunnerElectricGrenadeGhostReskin");
                    if (grenadeGhostReskin.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenadeReskin) && mdlEngiGrenadeReskin.TryGetComponent(out MeshRenderer meshRendererReskin))
                    {
                        Material matRailgunnerElectricGrenadeAlt = new Material(meshRendererReskin.sharedMaterial);
                        matRailgunnerElectricGrenadeAlt.SetTexture("_MainTex", loadOps.texRailgunnerElectricGrenadeAlt.asset);
                        meshRendererReskin.sharedMaterial = matRailgunnerElectricGrenadeAlt;
                    }

                    ArrayUtils.ArrayAppend(ref loadOps.skinRailGunnerAlt.Result.projectileGhostReplacements, new SkinDef.ProjectileGhostReplacement
                    {
                        projectilePrefab = GrenadeProjectile,
                        projectileGhostReplacementPrefab = grenadeGhostReskin
                    });
                }
            };
        }

        private static void SetStateOnHurt_OnTakeDamageServer(ILContext il)
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
            else instance.Logger.LogError($"{nameof(PulseGrenade)}.{nameof(SetStateOnHurt_OnTakeDamageServer)} IL hook failed!");
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
                        requiredSkillDef = Skills.RailgunnerElectricGrenade,
                        skillSlot = SkillSlot.Primary,
                        target = instance
                    });
                }
                Destroy(this);
            }
        }
    }
}