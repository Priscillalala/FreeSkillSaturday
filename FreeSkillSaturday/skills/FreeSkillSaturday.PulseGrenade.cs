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

                instance.loadStaticContentAsync += LoadStaticContentAsync;
                IL.RoR2.SetStateOnHurt.OnTakeDamageServer += SetStateOnHurt_OnTakeDamageServer;
            }
        }

        private static IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            yield return instance.Assets.LoadAssetAsync<Sprite>("texRailgunnerElectricGrenadeIcon", out var texRailgunnerElectricGrenadeIcon);
            yield return Ivyl.LoadAddressableAssetAsync<RailgunSkillDef>("RoR2/DLC1/Railgunner/RailgunnerBodyFirePistol.asset", out var RailgunnerBodyFirePistol);

            Skills.RailgunnerElectricGrenade = instance.Content.DefineSkill<RailgunSkillDef>("RailgunnerElectricGrenade")
                .SetIconSprite(texRailgunnerElectricGrenadeIcon.asset)
                .SetActivationState(typeof(EntityStates.Railgunner.Weapon.FireElectricGrenade), "Weapon")
                .SetInterruptPriority(EntityStates.InterruptPriority.Any)
                .SetCooldown(0f)
                .SetMaxStock(0)
                .SetRechargeStock(0)
                .SetRequiredStock(0)
                .SetStockToConsume(0)
                .SetKeywordTokens("KEYWORD_SHOCKING")
                .SetAdditionalFields(railgunSkillDef => railgunSkillDef.offlineIcon = RailgunnerBodyFirePistol.Result.offlineIcon);

            yield return Ivyl.LoadAddressableAssetAsync<SkillFamily>("RoR2/DLC1/Railgunner/RailgunnerBodyPrimaryFamily.asset", out var RailgunnerBodyPrimaryFamily);

            Achievements.RailgunnerHipster = instance.Content.DefineAchievementForSkill("RailgunnerHipster", ref RailgunnerBodyPrimaryFamily.Result.AddSkill(Skills.RailgunnerElectricGrenade))
                .SetIconSprite(Skills.RailgunnerElectricGrenade.icon)
                .SetTrackerTypes(typeof(RailgunnerHipsterAchievement), null);
            // Match achievement identifiers from 1.6.1
            Achievements.RailgunnerHipster.AchievementDef.identifier = "FSS_RailgunnerHipster";

            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCrosshair.prefab", out var RailgunnerCrosshair);
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerCryochargeCrosshair.prefab", out var RailgunnerCryochargeCrosshair);

            SetupRailgunnerCrosshair.prefab = Ivyl.ClonePrefab(RailgunnerCrosshair.Result.transform.Find("Flavor, Ready").gameObject, "Flavor, Grenade Ready");
            Color color = new Color32(198, 169, 217, 255);
            foreach (Image image in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<Image>())
            {
                image.color = color;
            }
            foreach (TextMeshProUGUI text in SetupRailgunnerCrosshair.prefab.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = color;
            }
            Instantiate(RailgunnerCryochargeCrosshair.Result.transform.Find("CenterDot").gameObject, SetupRailgunnerCrosshair.prefab.transform);
            RailgunnerCrosshair.Result.AddComponent<SetupRailgunnerCrosshair>();

            yield return CreateGrenadeProjectileAsync();
        }

        public static IEnumerator CreateGrenadeProjectileAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeProjectile.prefab", out var EngiGrenadeProjectile);

            GrenadeProjectile = Ivyl.ClonePrefab(EngiGrenadeProjectile.Result, "RailgunnerElectricGrenadeProjectile");
            if (GrenadeProjectile.TryGetComponent(out ProjectileDamage projectileDamage))
            {
                projectileDamage.damageType = DamageType.Shock5s;
            }

            yield return CreateGrenadeExplosionEffectAsync();

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
                yield return Ivyl.LoadAddressableAssetAsync<NetworkSoundEventDef>("RoR2/Base/Commando/nseCommandoGrenadeBounce.asset", out var nseCommandoGrenadeBounce);

                rigidbodySoundOnImpact.impactSoundString = string.Empty;
                rigidbodySoundOnImpact.networkedSoundEvent = nseCommandoGrenadeBounce.Result;
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

            yield return CreateGrenadeGhostAsync();

            GrenadeProjectile.GetComponent<ProjectileController>().ghostPrefab = GrenadeGhost;
            instance.Content.projectilePrefabs.Add(GrenadeProjectile);

            GameObject grenadeGhostReskin = Ivyl.ClonePrefab(GrenadeGhost, "RailgunnerElectricGrenadeGhostReskin");
            if (grenadeGhostReskin.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenadeReskin) && mdlEngiGrenadeReskin.TryGetComponent(out MeshRenderer meshRendererReskin))
            {
                yield return instance.Assets.LoadAssetAsync<Texture>("texRailgunnerElectricGrenadeAlt", out var texRailgunnerElectricGrenadeAlt);

                Material matRailgunnerElectricGrenadeAlt = new Material(meshRendererReskin.sharedMaterial);
                matRailgunnerElectricGrenadeAlt.SetTexture("_MainTex", texRailgunnerElectricGrenadeAlt.asset);
                meshRendererReskin.sharedMaterial = matRailgunnerElectricGrenadeAlt;
            }

            yield return Ivyl.LoadAddressableAssetAsync<SkinDef>("RoR2/DLC1/Railgunner/skinRailGunnerAlt.asset", out var skinRailGunnerAlt);

            ArrayUtils.ArrayAppend(ref skinRailGunnerAlt.Result.projectileGhostReplacements, new SkinDef.ProjectileGhostReplacement
            {
                projectilePrefab = GrenadeProjectile,
                projectileGhostReplacementPrefab = grenadeGhostReskin
            });
        }

        public static IEnumerator CreateGrenadeExplosionEffectAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Captain/CaptainTazerSupplyDropNova.prefab", out var CaptainTazerSupplyDropNova);

            GrenadeExplosionEffect = Ivyl.ClonePrefab(CaptainTazerSupplyDropNova.Result, "RailgunnerElectricGrenadeExplosion");
            GrenadeExplosionEffect.GetComponent<EffectComponent>().soundName = "Play_roboBall_attack1_explode";
            GrenadeExplosionEffect.GetComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            instance.Content.AddEffectPrefab(GrenadeExplosionEffect);
        }

        public static IEnumerator CreateGrenadeGhostAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Engi/EngiGrenadeGhost.prefab", out var EngiGrenadeGhost);

            GrenadeGhost = Ivyl.ClonePrefab(EngiGrenadeGhost.Result, "RailgunnerElectricGrenadeGhost");
            if (GrenadeGhost.transform.TryFind("mdlEngiGrenade", out Transform mdlEngiGrenade))
            {
                mdlEngiGrenade.localScale = new Vector3(0.3f, 0.3f, 0.5f);
                if (mdlEngiGrenade.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    yield return instance.Assets.LoadAssetAsync<Texture>("texRailgunnerElectricGrenade", out var texRailgunnerElectricGrenade);

                    Material matRailgunnerElectricGrenade = new Material(meshRenderer.sharedMaterial);
                    matRailgunnerElectricGrenade.SetColor("_EmColor", new Color32(55, 188, 255, 255));
                    matRailgunnerElectricGrenade.SetFloat("_EmPower", 2f);
                    matRailgunnerElectricGrenade.SetTexture("_MainTex", texRailgunnerElectricGrenade.asset);
                    meshRenderer.sharedMaterial = matRailgunnerElectricGrenade;
                }
            }
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