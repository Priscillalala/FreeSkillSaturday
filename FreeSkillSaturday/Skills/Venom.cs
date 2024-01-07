using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Items;
using R2API;
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
using System.Threading.Tasks;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public class Venom : MonoBehaviour
    {
        public static float damageCoefficientPerSecond = 1f;
        public static float duration = 5f;
        public static float speedReductionPerSecond = 0.05f;
        [Tooltip("Fix attack speed scaling on Mithrix's hammer slam")]
        public static bool enableMithrixAttackSpeedFix = true;

        public static DotController.DotIndex ToxinDot { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _crocoBodyPassiveFamily = "RoR2/Base/Croco/CrocoBodyPassiveFamily.asset";
            using RoR2Asset<Sprite> _texBuffBleedingIcon = "RoR2/Base/Common/texBuffBleedingIcon.tif";
            using Task<BurnEffectController.EffectParams> _toxicBurnEffectParams = CreateToxicBurnEffectParamsAsync();

            Content.Skills.CrocoPassiveToxin = Expansion.DefineSkill<ToxinSkillDef>("CrocoPassiveToxin")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texCrocoPassiveVenomIcon"))
                .SetKeywordTokens("FSS_KEYWORD_VENOM");

            Content.Achievements.CrocoKillBossCloaked = Expansion.DefineAchievementForSkill("CrocoKillBossCloaked", Content.Skills.CrocoPassiveToxin)
                .SetIconSprite(Content.Skills.CrocoPassiveToxin.icon)
                .SetPrerequisiteAchievement("BeatArena")
                .SetTrackerTypes(typeof(CrocoKillBossCloakedAchievement), typeof(CrocoKillBossCloakedAchievement.ServerAchievement));

            SkillFamily crocoBodyPassiveFamily = await _crocoBodyPassiveFamily;
            crocoBodyPassiveFamily.AddSkill(Content.Skills.CrocoPassiveToxin, Content.Achievements.CrocoKillBossCloaked.UnlockableDef);

            Content.Buffs.Toxin = Expansion.DefineBuff("Toxin")
                .SetIconSprite(await _texBuffBleedingIcon, new Color32(156, 123, 255, 255));

            Content.Buffs.ToxinSlow = Expansion.DefineBuff("ToxinSlow")
                //.SetIconSprite(Assets.LoadAsset<Sprite>("texThereminIcon"), Color.blue)
                .SetFlags(BuffFlags.Stackable | BuffFlags.Hidden);

            ToxinDot = DotAPI.RegisterDotDef(0.333f, damageCoefficientPerSecond * 0.333f, DamageColorIndex.Poison, Content.Buffs.Toxin);

            ToxinBuffBehaviour.toxinBurnEffectParams = await _toxicBurnEffectParams;
        }

        public static async Task<BurnEffectController.EffectParams> CreateToxicBurnEffectParamsAsync()
        {
            using RoR2Asset<Material> _matPoisoned = "RoR2/Base/Croco/matPoisoned.mat";
            using RoR2Asset<Texture> _texRampTritoneSmoothed = "RoR2/Base/Common/ColorRamps/texRampTritoneSmoothed.png";
            using RoR2Asset<Texture> _texCloudLightning1 = "RoR2/Base/Common/texCloudLightning1.png";
            using RoR2Asset<GameObject> _poisonEffect = "RoR2/Base/Croco/PoisonEffect.prefab";

            Material matToxin = new Material(await _matPoisoned);
            matToxin.SetColor("_TintColor", new Color32(230, 189, 255, 255));
            matToxin.SetFloat("_AlphaBoost", 2.5f);
            matToxin.SetTexture("_RemapTex", await _texRampTritoneSmoothed);
            matToxin.SetTexture("_Cloud2Tex", await _texCloudLightning1);
            return new BurnEffectController.EffectParams
            {
                overlayMaterial = matToxin,
                fireEffectPrefab = await _poisonEffect,
            };
        }

        public void OnEnable()
        {
            On.RoR2.DotController.OnDotStackAddedServer += DotController_OnDotStackAddedServer;
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            if (enableMithrixAttackSpeedFix)
            {
                IL.EntityStates.BrotherMonster.WeaponSlam.OnEnter += FixWeaponSlamDuration;
                IL.EntityStates.BrotherMonster.WeaponSlam.FixedUpdate += FixWeaponSlamDuration;
                IL.EntityStates.BrotherMonster.WeaponSlam.GetMinimumInterruptPriority += FixWeaponSlamPriorityDuration;
            }
        }

        public void OnDisable()
        {
            On.RoR2.DotController.OnDotStackAddedServer -= DotController_OnDotStackAddedServer;
            RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
            if (enableMithrixAttackSpeedFix)
            {
                IL.EntityStates.BrotherMonster.WeaponSlam.OnEnter -= FixWeaponSlamDuration;
                IL.EntityStates.BrotherMonster.WeaponSlam.FixedUpdate -= FixWeaponSlamDuration;
                IL.EntityStates.BrotherMonster.WeaponSlam.GetMinimumInterruptPriority -= FixWeaponSlamPriorityDuration;
            }
        }

        private void DotController_OnDotStackAddedServer(On.RoR2.DotController.orig_OnDotStackAddedServer orig, DotController self, object _dotStack)
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

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(Content.Buffs.ToxinSlow, out int count))
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

        private void FixWeaponSlamDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<WeaponSlam>(nameof(WeaponSlam.duration))))
            {
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<float, WeaponSlam, float>>((duration, weaponSlam) => duration / weaponSlam.attackSpeedStat);
            }
            else Logger.LogError($"{nameof(Venom)}.{nameof(FixWeaponSlamDuration)} IL hook failed!");
        }

        private void FixWeaponSlamPriorityDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<WeaponSlam>(nameof(WeaponSlam.durationBeforePriorityReduces))))
            {
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<float, WeaponSlam, float>>((duration, weaponSlam) => duration / weaponSlam.attackSpeedStat);
            }
            else Logger.LogError($"{nameof(Venom)}.{nameof(FixWeaponSlamPriorityDuration)} IL hook failed!");
        }

        public class ToxinBuffBehaviour : BaseBuffBodyBehavior, IOnTakeDamageServerReceiver, IOnGetStatCoefficientsReciever
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = true)]
            public static BuffDef GetBuffDef() => Content.Buffs.Toxin;

            public static BurnEffectController.EffectParams toxinBurnEffectParams;

            private BurnEffectController burnEffectController;

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
                    body.AddBuff(Content.Buffs.ToxinSlow);
                }
            }

            public void OnGetStatCoefficients(RecalculateStatsAPI.StatHookEventArgs _)
            {
                if (burnEffectController?.temporaryOverlay && burnEffectController.temporaryOverlay.materialInstance)
                {
                    float buffCount = body.GetBuffCount(Content.Buffs.ToxinSlow);
                    float fresnelPower = 3f - buffCount * 0.15f;
                    burnEffectController.temporaryOverlay.materialInstance.SetFloat("_FresnelPower", Mathf.Clamp(fresnelPower, 0.5f, 3f));
                }
            }

            public void OnDestroy()
            {
                if (NetworkServer.active)
                {
                    body.SetBuffCount(Content.Buffs.ToxinSlow.buffIndex, 0);
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