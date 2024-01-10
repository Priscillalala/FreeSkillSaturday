using RoR2.Skills;
using JetBrains.Annotations;
using EntityStates.BrotherMonster;
using FreeItemFriday.Achievements;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class Venom
    {
        public static bool enabled = true;
        public static float damageCoefficientPerSecond = 1f;
        public static float duration = 5f;
        public static float speedReductionPerSecond = 0.05f;
        public static bool enableMithrixAttackSpeedFix = true;

        public static DotController.DotIndex ToxinDot { get; private set; }

        public static void Init()
        {
            const string SECTION = "Venom";
            instance.SkillsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.SkillsConfig.Bind(ref damageCoefficientPerSecond, SECTION, "Damage Coefficient Per Second");
            instance.SkillsConfig.Bind(ref duration, SECTION, "Duration");
            instance.SkillsConfig.Bind(ref speedReductionPerSecond, SECTION, "Speed Reduction Per Second");
            instance.SkillsConfig.Bind(ref enableMithrixAttackSpeedFix, SECTION, "Enable Mithrix Attack Speed Fix", "Fix attack speed scaling on Mithrix's hammer slam");
            if (enabled)
            {
                Buffs.Toxin = instance.Content.DefineBuff("Toxin");
                ToxinDot = DotAPI.RegisterDotDef(0.333f, damageCoefficientPerSecond * 0.333f, DamageColorIndex.Poison, Buffs.Toxin);

                instance.loadStaticContentAsync += LoadStaticContentAsync;
                On.RoR2.DotController.OnDotStackAddedServer += DotController_OnDotStackAddedServer;
                RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
                if (enableMithrixAttackSpeedFix)
                {
                    IL.EntityStates.BrotherMonster.WeaponSlam.OnEnter += FixWeaponSlamDuration;
                    IL.EntityStates.BrotherMonster.WeaponSlam.FixedUpdate += FixWeaponSlamDuration;
                    IL.EntityStates.BrotherMonster.WeaponSlam.GetMinimumInterruptPriority += FixWeaponSlamPriorityDuration;
                }
            }
        }

        private static IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            yield return instance.Assets.LoadAssetAsync<Sprite>("texCrocoPassiveVenomIcon", out var texCrocoPassiveVenomIcon);

            Skills.CrocoPassiveToxin = instance.Content.DefineSkill<ToxinSkillDef>("CrocoPassiveToxin")
                .SetIconSprite(texCrocoPassiveVenomIcon.asset)
                .SetKeywordTokens("FSS_KEYWORD_VENOM");

            yield return Ivyl.LoadAddressableAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodyPassiveFamily.asset", out var CrocoBodyPassiveFamily);

            Achievements.CrocoKillBossCloaked = instance.Content.DefineAchievementForSkill("CrocoKillBossCloaked", ref CrocoBodyPassiveFamily.Result.AddSkill(Skills.CrocoPassiveToxin))
                .SetIconSprite(Skills.CrocoPassiveToxin.icon)
                .SetPrerequisiteAchievement("BeatArena")
                .SetTrackerTypes(typeof(CrocoKillBossCloakedAchievement), typeof(CrocoKillBossCloakedAchievement.ServerAchievement));
            // Match achievement identifiers from 1.6.1
            Achievements.CrocoKillBossCloaked.AchievementDef.identifier = "FSS_CrocoKillBossCloaked";

            yield return Ivyl.LoadAddressableAssetAsync<Sprite>("RoR2/Base/Common/texBuffBleedingIcon.tif", out var texBuffBleedingIcon);

            Buffs.Toxin.SetIconSprite(texBuffBleedingIcon.Result, new Color32(156, 123, 255, 255));

            Buffs.ToxinSlow = instance.Content.DefineBuff("ToxinSlow")
                .SetFlags(BuffFlags.Stackable | BuffFlags.Hidden);

            yield return CreateToxicBurnEffectParamsAsync();
        }

        public static IEnumerator CreateToxicBurnEffectParamsAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<Material>("RoR2/Base/Croco/matPoisoned.mat", out var matPoisoned);
            yield return Ivyl.LoadAddressableAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampTritoneSmoothed.png", out var texRampTritoneSmoothed);
            yield return Ivyl.LoadAddressableAssetAsync<Texture>("RoR2/Base/Common/texCloudLightning1.png", out var texCloudLightning1);
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Croco/PoisonEffect.prefab", out var PoisonEffect);

            Material matToxin = new Material(matPoisoned.Result);
            matToxin.SetColor("_TintColor", new Color32(230, 189, 255, 255));
            matToxin.SetFloat("_AlphaBoost", 2.5f);
            matToxin.SetTexture("_RemapTex", texRampTritoneSmoothed.Result);
            matToxin.SetTexture("_Cloud2Tex", texCloudLightning1.Result);
            ToxinBuffBehaviour.toxinBurnEffectParams = new BurnEffectController.EffectParams
            {
                overlayMaterial = matToxin,
                fireEffectPrefab = PoisonEffect.Result,
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
            if (sender.HasBuff(Buffs.ToxinSlow, out int count))
            {
                float reduction = speedReductionPerSecond * count;
                if (sender.rigidbody && sender.rigidbody.mass < 250f)
                {
                    reduction *= 2f;
                }
                args.moveSpeedReductionMultAdd += reduction;
                args.attackSpeedReductionMultAdd += reduction;
            }
        }

        private static void FixWeaponSlamDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<WeaponSlam>(nameof(WeaponSlam.duration))))
            {
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<float, WeaponSlam, float>>((duration, weaponSlam) => duration / weaponSlam.attackSpeedStat);
            }
            else instance.Logger.LogError($"{nameof(Venom)}.{nameof(FixWeaponSlamDuration)} IL hook failed!");
        }

        private static void FixWeaponSlamPriorityDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<WeaponSlam>(nameof(WeaponSlam.durationBeforePriorityReduces))))
            {
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<float, WeaponSlam, float>>((duration, weaponSlam) => duration / weaponSlam.attackSpeedStat);
            }
            else instance.Logger.LogError($"{nameof(Venom)}.{nameof(FixWeaponSlamPriorityDuration)} IL hook failed!");
        }

        public class ToxinBuffBehaviour : BaseBuffBodyBehavior, IOnTakeDamageServerReceiver
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = true)]
            public static BuffDef GetBuffDef() => Buffs.Toxin;

            public static BurnEffectController.EffectParams toxinBurnEffectParams;

            private BurnEffectController burnEffectController;
            private int _slowBuffCount;

            public void OnEnable()
            {
                body.healthComponent?.AddTakeDamageReceiver(this);
                if (body.modelLocator?.modelTransform)
                {
                    burnEffectController = base.gameObject.AddComponent<BurnEffectController>();
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
                body.healthComponent?.RemoveTakeDamageReceiver(this);
            }

            public void OnTakeDamageServer(DamageReport damageReport)
            {
                if (damageReport.dotType == ToxinDot && Util.CheckRoll(33.3f, damageReport.attackerMaster))
                {
                    body.AddBuff(Buffs.ToxinSlow);
                }
            }

            public void Update()
            {
                if (_slowBuffCount == (_slowBuffCount = body.GetBuffCount(Buffs.ToxinSlow)))
                {
                    return;
                }
                if (burnEffectController?.temporaryOverlay && burnEffectController.temporaryOverlay.materialInstance)
                {
                    float fresnelPower = 3f - _slowBuffCount * 0.15f;
                    burnEffectController.temporaryOverlay.materialInstance.SetFloat("_FresnelPower", Mathf.Clamp(fresnelPower, 0.5f, 3f));
                }
            }

            public void OnDestroy()
            {
                if (NetworkServer.active)
                {
                    body.SetBuffCount(Buffs.ToxinSlow.buffIndex, 0);
                }
            }
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
                    && crocoDamageTypeController.passiveSkillSlot 
                    && crocoDamageTypeController.passiveSkillSlot.skillDef is ToxinSkillDef
                    && victim)
                {
                    DotController.InflictDot(victim, damageInfo.attacker, ToxinDot, duration * damageInfo.procCoefficient, 1f, dotMaxStacksFromAttacker);
                }
            }

            private static DamageType CrocoDamageTypeController_GetDamageType(On.RoR2.CrocoDamageTypeController.orig_GetDamageType orig, CrocoDamageTypeController self)
            {
                if (self.passiveSkillSlot && self.passiveSkillSlot.skillDef is ToxinSkillDef)
                {
                    return Unused;
                }
                return orig(self);
            }
        }
    }
}