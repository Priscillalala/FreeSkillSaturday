using RoR2.Skills;
using JetBrains.Annotations;
using FreeItemFriday.Achievements;
using EntityStates.Croco;
using System.Runtime.CompilerServices;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class Venom
    {
        public static bool enabled = true;
        public static float damageCoefficientPerSecond = 1f;
        public static float duration = 10f;
        public static float slowCoefficient = 0.5f;
        public static float initialSlowBonus = 1f;

        public static DotController.DotIndex ToxinDot { get; private set; }

        public static void Init(FreeSkillSaturday instance)
        {
            const string SECTION = "Venom";
            instance.SkillsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.SkillsConfig.Bind(ref damageCoefficientPerSecond, SECTION, "Damage Coefficient Per Second");
            instance.SkillsConfig.Bind(ref duration, SECTION, "Duration");
            instance.SkillsConfig.Bind(ref slowCoefficient, SECTION, "Move Speed Reduction");
            instance.SkillsConfig.Bind(ref initialSlowBonus, SECTION, "Bonus Move Speed Reduction", "An additional slow applied for the first 0.5 seconds to affect monsters immune to stun.");
            if (enabled)
            {
                Buffs.Toxin = instance.Content.DefineBuff("Toxin");
                ToxinDot = DotAPI.RegisterDotDef(0.333f, damageCoefficientPerSecond * 0.333f, DamageColorIndex.Poison, Buffs.Toxin, (dotController, dotStack) => 
                {
                    dotStack.damageType |= DamageType.NonLethal;
                    for (int i = 0; i < dotController.dotStackList.Count; i++)
                    {
                        if (dotController.dotStackList[i].dotIndex == ToxinDot)
                        {
                            dotStack.damage = Mathf.Max(dotStack.damage, dotController.dotStackList[i].damage);
                            dotStack.timer = Mathf.Max(dotStack.timer, dotController.dotStackList[i].timer);
                            dotController.RemoveDotStackAtServer(i);
                            break;
                        }
                    }
                });

                instance.loadStaticContentAsync += CreateSkillAsync;
                instance.loadStaticContentAsync += CreateToxicBurnEffectParamsAsync;
                instance.loadStaticContentAsync += ModifyCrocoLeapAsync;
                static IEnumerator<float> ModifyCrocoLeapAsync(LoadStaticContentAsyncArgs args)
                {
                    var CrocoLeap = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Croco/CrocoLeap.asset");
                    while (!CrocoLeap.IsDone)
                    {
                        yield return CrocoLeap.PercentComplete;
                    }
                    int index = Array.IndexOf(CrocoLeap.Result.keywordTokens, "KEYWORD_STUNNING");
                    if (index >= 0)
                    {
                        HG.ArrayUtils.ArrayRemoveAtAndResize(ref CrocoLeap.Result.keywordTokens, index);
                    }
                }
                //On.RoR2.DotController.OnDotStackAddedServer += DotController_OnDotStackAddedServer;
                RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
                On.EntityStates.Croco.Leap.GetBlastDamageType += Leap_GetBlastDamageType;
            }
        }

        private static IEnumerator<float> CreateSkillAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                texCrocoPassiveVenomIcon: args.assets.LoadAsync<Sprite>("texCrocoPassiveVenomIcon"),
                CrocoBodyPassiveFamily: Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodyPassiveFamily.asset"),
                texBuffVenomIcon: args.assets.LoadAsync<Sprite>("texBuffVenomIcon")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    Skills.CrocoPassiveToxin = args.content.DefineSkill<ToxinSkillDef>("CrocoPassiveToxin")
                        .SetIconSprite(loadOps.texCrocoPassiveVenomIcon.asset)
                        .SetKeywordTokens("KEYWORD_STUNNING", "FSS_KEYWORD_VENOM");

                    ref SkillFamily.Variant skillVariant = ref loadOps.CrocoBodyPassiveFamily.Result.AddSkill(Skills.CrocoPassiveToxin);
                    Achievements.CrocoKillBossCloaked = args.content.DefineAchievementForSkill("CrocoKillBossCloaked", ref skillVariant)
                        .SetIconSprite(Skills.CrocoPassiveToxin.icon)
                        .SetPrerequisiteAchievement("BeatArena")
                        .SetTrackerTypes(typeof(CrocoKillBossCloakedAchievement), typeof(CrocoKillBossCloakedAchievement.ServerAchievement));
                    // Match achievement identifiers from 1.6.1
                    Achievements.CrocoKillBossCloaked.AchievementDef.identifier = "FSS_CrocoKillBossCloaked";

                    Buffs.Toxin.SetIconSprite(loadOps.texBuffVenomIcon.asset, new Color32(156, 123, 255, 255));

                    Buffs.ToxinSlow = args.content.DefineBuff("ToxinSlow")
                        .SetFlags(BuffFlags.Hidden);
                }
            };
        }

        private static IEnumerator<float> CreateToxicBurnEffectParamsAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                matPoisoned: Addressables.LoadAssetAsync<Material>("RoR2/Base/Croco/matPoisoned.mat"),
                texRampTritoneSmoothed: Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampTritoneSmoothed.png"),
                texCloudLightning1: Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/texCloudLightning1.png"),
                PoisonEffect: Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/PoisonEffect.prefab")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    Material matToxin = new Material(loadOps.matPoisoned.Result);
                    matToxin.SetColor("_TintColor", new Color32(230, 189, 255, 255));
                    matToxin.SetFloat("_AlphaBoost", 2.5f);
                    matToxin.SetFloat("_FresnelPower", -5.6f);
                    matToxin.SetTexture("_RemapTex", loadOps.texRampTritoneSmoothed.Result);
                    matToxin.SetTexture("_Cloud2Tex", loadOps.texCloudLightning1.Result);
                    ToxinBuffBehaviour.toxinBurnEffectParams = new BurnEffectController.EffectParams
                    {
                        overlayMaterial = matToxin,
                        fireEffectPrefab = loadOps.PoisonEffect.Result,
                    };
                }
            };
        }

        private static void DotController_OnDotStackAddedServer(On.RoR2.DotController.orig_OnDotStackAddedServer orig, DotController self, object _dotStack)
        {
            orig(self, _dotStack);
            if (_dotStack is DotController.DotStack dotStack && dotStack.dotIndex == ToxinDot)
            {
                dotStack.damageType |= DamageType.NonLethal;
                for (int i = 0; i < self.dotStackList.Count - 1; i++)
                {
                    if (self.dotStackList[i].dotIndex == ToxinDot)
                    {
                        dotStack.damage = Mathf.Max(dotStack.damage, self.dotStackList[i].damage);
                        dotStack.timer = Mathf.Max(dotStack.timer, self.dotStackList[i].timer);
                        self.RemoveDotStackAtServer(i);
                        break;
                    }
                }
            }
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(Buffs.Toxin))
            {
                args.moveSpeedReductionMultAdd += slowCoefficient;
                if (sender.HasBuff(Buffs.ToxinSlow))
                {
                    args.moveSpeedReductionMultAdd += initialSlowBonus;
                }
            }
        }

        private static DamageType Leap_GetBlastDamageType(On.EntityStates.Croco.Leap.orig_GetBlastDamageType orig, Leap self)
        {
            if (self.crocoDamageTypeController && HasToxin(self.crocoDamageTypeController)) 
            {
                return orig(self);
            }
            return orig(self) & ~DamageType.Stun1s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasToxin(CrocoDamageTypeController crocoDamageTypeController)
        {
            return crocoDamageTypeController.passiveSkillSlot && crocoDamageTypeController.passiveSkillSlot.skillDef is ToxinSkillDef;
        }

        public class ToxinSkillDef : SkillDef
        {
            private const DamageType Unused = (DamageType)(1U << 31);

            private static int assignedCount;

            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                if (assignedCount++ == 0)
                {
                    SetHooks();
                }
                return null;
            }

            public override void OnUnassigned([NotNull] GenericSkill skillSlot)
            {
                if (--assignedCount == 0)
                {
                    UnsetHooks();
                }
            }

            public static void SetHooks()
            {
                Events.GlobalEventManager.onHitEnemyAcceptedServer += GlobalEventManager_onHitEnemyAcceptedServer;
                On.RoR2.CrocoDamageTypeController.GetDamageType += CrocoDamageTypeController_GetDamageType;
            }

            public static void UnsetHooks()
            {
                Events.GlobalEventManager.onHitEnemyAcceptedServer -= GlobalEventManager_onHitEnemyAcceptedServer;
                On.RoR2.CrocoDamageTypeController.GetDamageType -= CrocoDamageTypeController_GetDamageType;
            }

            private static void GlobalEventManager_onHitEnemyAcceptedServer(DamageInfo damageInfo, GameObject victim, uint? dotMaxStacksFromAttacker)
            {
                if ((damageInfo.damageType & Unused) > DamageType.Generic 
                    && damageInfo.attacker.TryGetComponent(out CrocoDamageTypeController crocoDamageTypeController) 
                    && HasToxin(crocoDamageTypeController)
                    && victim)
                {
                    DotController.InflictDot(victim, damageInfo.attacker, ToxinDot, duration * damageInfo.procCoefficient, 1f, dotMaxStacksFromAttacker);
                    if (victim.TryGetComponent(out CharacterBody victimBody))
                    {
                        victimBody.AddTimedBuff(Buffs.ToxinSlow, 0.5f);
                    }
                }
            }

            private static DamageType CrocoDamageTypeController_GetDamageType(On.RoR2.CrocoDamageTypeController.orig_GetDamageType orig, CrocoDamageTypeController self)
            {
                if (HasToxin(self))
                {
                    return orig(self) | Unused | DamageType.Stun1s;
                }
                return orig(self);
            }
        }

        public class ToxinBuffBehaviour : BaseBuffBodyBehavior
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = true)]
            public static BuffDef GetBuffDef() => Buffs.Toxin;

            public static BurnEffectController.EffectParams toxinBurnEffectParams;

            private BurnEffectController burnEffectController;

            public void OnEnable()
            {
                if (body.modelLocator?.modelTransform)
                {
                    burnEffectController = gameObject.AddComponent<BurnEffectController>();
                    burnEffectController.effectType = toxinBurnEffectParams;
                    burnEffectController.target = body.modelLocator.modelTransform.gameObject;
                }
            }

            public void OnDisable()
            {
                if (burnEffectController)
                {
                    Destroy(burnEffectController);
                }
            }
        }
    }
}