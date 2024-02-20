using RoR2.Skills;
using FreeItemFriday.Achievements;
using EntityStates;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class Disembowel
    {
        public static bool enabled = true;
        public static float damageCoefficient = 2f;

        public static DamageAPI.ModdedDamageType SuperBleedOnHit { get; private set; }
        public static GameObject CrocoSuperBiteEffect { get; private set; }

        public static void Init()
        {
            const string SECTION = "Disembowel";
            instance.SkillsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.SkillsConfig.Bind(ref damageCoefficient, SECTION, "Damage Coefficient");
            if (enabled)
            {
                SuperBleedOnHit = DamageAPI.ReserveDamageType();

                instance.loadStaticContentAsync += CreateSkillAsync;
                instance.loadStaticContentAsync += CreateCrocoSuperBiteEffectAsync;
                Events.GlobalEventManager.onHitEnemyAcceptedServer += GlobalEventManager_onHitEnemyAcceptedServer;
            }
        }

        private static IEnumerator<float> CreateSkillAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                texCrocoSuperBiteIcon: args.assets.LoadAsync<Sprite>("texCrocoSuperBiteIcon"),
                CrocoBodySpecialFamily: Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodySpecialFamily.asset")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    Skills.CrocoSuperBite = args.content.DefineSkill<SkillDef>("CrocoSuperBite")
                        .SetIconSprite(loadOps.texCrocoSuperBiteIcon.asset)
                        .SetActivationState(typeof(EntityStates.Croco.SuperBite), "Weapon")
                        .SetCooldown(10f)
                        .SetInterruptPriority(InterruptPriority.PrioritySkill)
                        .SetKeywordTokens("KEYWORD_POISON", "KEYWORD_SLAYER", "FSS_KEYWORD_BLEED", "KEYWORD_SUPERBLEED");

                    ref SkillFamily.Variant skillVariant = ref loadOps.CrocoBodySpecialFamily.Result.AddSkill(Skills.CrocoSuperBite);
                    Achievements.CrocoBeatArenaFast = args.content.DefineAchievementForSkill("CrocoBeatArenaFast", ref skillVariant)
                        .SetIconSprite(Skills.CrocoSuperBite.icon)
                        .SetPrerequisiteAchievement("BeatArena")
                        .SetTrackerTypes(typeof(CrocoBeatArenaFastAchievement), typeof(CrocoBeatArenaFastAchievement.ServerAchievement));
                    // Match achievement identifiers from 1.6.1
                    Achievements.CrocoBeatArenaFast.AchievementDef.identifier = "FSS_CrocoBeatArenaFast";
                }
            };
        }

        private static IEnumerator<float> CreateCrocoSuperBiteEffectAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                CrocoBiteEffect: Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBiteEffect.prefab"),
                matCrocoGooSmall2: Addressables.LoadAssetAsync<Material>("RoR2/Base/Croco/matCrocoGooSmall2.mat"),
                texRampPoison: Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampPoison.png")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    CrocoSuperBiteEffect = Ivyl.ClonePrefab(loadOps.CrocoBiteEffect.Result, "CrocoSuperBiteEffect");
                    if (CrocoSuperBiteEffect.transform.TryFind("Goo", out Transform goo) && goo.TryGetComponent(out ParticleSystemRenderer gooRenderer))
                    {
                        gooRenderer.sharedMaterial = loadOps.matCrocoGooSmall2.Result;
                    }
                    float multiplier = 1.2f;
                    if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail", out Transform swingTrail))
                    {
                        swingTrail.localScale *= multiplier;
                        if (swingTrail.TryGetComponent(out ParticleSystemRenderer swingTrailRenderer))
                        {
                            swingTrailRenderer.sharedMaterial = new Material(swingTrailRenderer.sharedMaterial);
                            swingTrailRenderer.sharedMaterial.SetColor("_TintColor", new Color32(121, 255, 107, 255));
                            swingTrailRenderer.sharedMaterial.SetTexture("_RemapTex", loadOps.texRampPoison.Result);
                        }
                    }
                    if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail, Distortion", out Transform swingTrailDistortion))
                    {
                        swingTrailDistortion.localScale *= multiplier;
                    }
                    if (CrocoSuperBiteEffect.transform.TryFind("Flash", out Transform flash))
                    {
                        flash.localScale *= multiplier;
                    }
                }
            };
        }

        private static void GlobalEventManager_onHitEnemyAcceptedServer(DamageInfo damageInfo, GameObject victim, uint? dotMaxStacksFromAttacker)
        {
            if (damageInfo.HasModdedDamageType(SuperBleedOnHit) && victim)
            {
                DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.SuperBleed, 15f * damageInfo.procCoefficient, 1f, dotMaxStacksFromAttacker);
            }
        }
    }
}