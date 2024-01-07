using RoR2.Skills;
using FreeItemFriday.Achievements;
using EntityStates;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public class Disembowel : MonoBehaviour
    {
        public static float damageCoefficient = 2f;

        public static DamageAPI.ModdedDamageType SuperBleedOnHit { get; private set; }
        public static GameObject CrocoSuperBiteEffect { get; private set; }

        public void Awake()
        {
            SuperBleedOnHit = DamageAPI.ReserveDamageType();

            Instance.loadStaticContentAsync += LoadStaticContentAsync;
        }

        private IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            yield return Instance.Assets.LoadAssetAsync<Sprite>("texCrocoSuperBiteIcon", out var texCrocoSuperBiteIcon);

            Skills.CrocoSuperBite = Instance.Content.DefineSkill<SkillDef>("CrocoSuperBite")
                .SetIconSprite(texCrocoSuperBiteIcon.asset)
                .SetActivationState(typeof(EntityStates.Croco.SuperBite), "Weapon")
                .SetCooldown(10f)
                .SetInterruptPriority(InterruptPriority.PrioritySkill)
                .SetKeywordTokens("KEYWORD_POISON", "KEYWORD_SLAYER", "FSS_KEYWORD_BLEED", "KEYWORD_SUPERBLEED");

            yield return Ivyl.LoadAddressableAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodySpecialFamily.asset", out var CrocoBodySpecialFamily);

            Achievements.CrocoBeatArenaFast = Instance.Content.DefineAchievementForSkill("CrocoBeatArenaFast", ref CrocoBodySpecialFamily.Result.AddSkill(Skills.CrocoSuperBite))
                .SetIconSprite(Skills.CrocoSuperBite.icon)
                .SetPrerequisiteAchievement("BeatArena")
                .SetTrackerTypes(typeof(CrocoBeatArenaFastAchievement), typeof(CrocoBeatArenaFastAchievement.ServerAchievement));

            yield return CreateCrocoSuperBiteEffectAsync();
        }

        public IEnumerator CreateCrocoSuperBiteEffectAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBiteEffect.prefab", out var CrocoBiteEffect);
            yield return Ivyl.LoadAddressableAssetAsync<Material>("RoR2/Base/Croco/matCrocoGooSmall2.mat", out var matCrocoGooSmall2);
            yield return Ivyl.LoadAddressableAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampPoison.png", out var texRampPoison);

            CrocoSuperBiteEffect = Ivyl.ClonePrefab(CrocoBiteEffect.Result, "CrocoSuperBiteEffect");
            if (CrocoSuperBiteEffect.transform.TryFind("Goo", out Transform goo) && goo.TryGetComponent(out ParticleSystemRenderer gooRenderer))
            {
                gooRenderer.sharedMaterial = matCrocoGooSmall2.Result;
            }
            float multiplier = 1.2f;
            if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail", out Transform swingTrail))
            {
                swingTrail.localScale *= multiplier;
                if (swingTrail.TryGetComponent(out ParticleSystemRenderer swingTrailRenderer))
                {
                    swingTrailRenderer.sharedMaterial = new Material(swingTrailRenderer.sharedMaterial);
                    swingTrailRenderer.sharedMaterial.SetColor("_TintColor", new Color32(121, 255, 107, 255));
                    swingTrailRenderer.sharedMaterial.SetTexture("_RemapTex", texRampPoison.Result);
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

        public void OnEnable()
        {
            Events.GlobalEventManager.onHitEnemyAcceptedServer += GlobalEventManager_onHitEnemyAcceptedServer;
        }

        public void OnDisable()
        {
            Events.GlobalEventManager.onHitEnemyAcceptedServer -= GlobalEventManager_onHitEnemyAcceptedServer;
        }

        private void GlobalEventManager_onHitEnemyAcceptedServer(DamageInfo damageInfo, GameObject victim, uint? dotMaxStacksFromAttacker)
        {
            if (damageInfo.HasModdedDamageType(SuperBleedOnHit) && victim)
            {
                DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.SuperBleed, 15f * damageInfo.procCoefficient, 1f, dotMaxStacksFromAttacker);
            }
        }
    }
}