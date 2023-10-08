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
using EntityStates;
using System.Threading.Tasks;

namespace FreeItemFriday.Skills
{
    public class Disembowel : FreeSkillSaturday.Behavior
    {
        public static float damageCoefficient = 2f;

        public static DamageAPI.ModdedDamageType SuperBleedOnHit { get; private set; }
        public static GameObject CrocoSuperBiteEffect { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _crocoBodySpecialFamily = "RoR2/Base/Croco/CrocoBodySpecialFamily.asset";
            using Task<GameObject> _crocoSuperBiteEffect = CreateCrocoSuperBiteEffectAsync();

            Content.Skills.CrocoSuperBite = Expansion.DefineSkill<SkillDef>("CrocoSuperBite")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texCrocoSuperBiteIcon"))
                .SetActivationState(typeof(EntityStates.Croco.SuperBite), "Weapon")
                .SetCooldown(10f)
                .SetInterruptPriority(InterruptPriority.PrioritySkill)
                .SetKeywordTokens("KEYWORD_POISON", "KEYWORD_SLAYER", "FSS_KEYWORD_BLEED", "KEYWORD_SUPERBLEED");

            SuperBleedOnHit = DamageAPI.ReserveDamageType();

            Content.Achievements.CrocoBeatArenaFast = Expansion.DefineAchievementForSkill("CrocoBeatArenaFast", Content.Skills.CrocoSuperBite)
                .SetIconSprite(Content.Skills.CrocoSuperBite.icon)
                .SetPrerequisiteAchievement("BeatArena")
                .SetTrackerTypes(typeof(CrocoBeatArenaFastAchievement), typeof(CrocoBeatArenaFastAchievement.ServerAchievement));

            SkillFamily crocoBodySpecialFamily = await _crocoBodySpecialFamily;
            crocoBodySpecialFamily.AddSkill(Content.Skills.CrocoSuperBite, Content.Achievements.CrocoBeatArenaFast.UnlockableDef);

            CrocoSuperBiteEffect = await _crocoSuperBiteEffect;
        }

        public static async Task<GameObject> CreateCrocoSuperBiteEffectAsync()
        {
            using RoR2Asset<GameObject> _crocoBiteEffect = "RoR2/Base/Croco/CrocoBiteEffect.prefab";
            using RoR2Asset<Material> _matCrocoGooSmall2 = "RoR2/Base/Croco/matCrocoGooSmall2.mat";
            using RoR2Asset<Texture> _texRampPoison = "RoR2/Base/Common/ColorRamps/texRampPoison.png";

            GameObject crocoSuperBiteEffect = Prefabs.ClonePrefab(await _crocoBiteEffect, "CrocoSuperBiteEffect");
            if (crocoSuperBiteEffect.transform.TryFind("Goo", out Transform goo) && goo.TryGetComponent(out ParticleSystemRenderer gooRenderer))
            {
                gooRenderer.sharedMaterial = await _matCrocoGooSmall2;
            }
            float multiplier = 1.2f;
            if (crocoSuperBiteEffect.transform.TryFind("SwingTrail", out Transform swingTrail))
            {
                swingTrail.localScale *= multiplier;
                if (swingTrail.TryGetComponent(out ParticleSystemRenderer swingTrailRenderer))
                {
                    swingTrailRenderer.sharedMaterial = new Material(swingTrailRenderer.sharedMaterial);
                    swingTrailRenderer.sharedMaterial.SetColor("_TintColor", new Color32(121, 255, 107, 255));
                    swingTrailRenderer.sharedMaterial.SetTexture("_RemapTex", await _texRampPoison);
                }
            }
            if (crocoSuperBiteEffect.transform.TryFind("SwingTrail, Distortion", out Transform swingTrailDistortion))
            {
                swingTrailDistortion.localScale *= multiplier;
            }
            if (crocoSuperBiteEffect.transform.TryFind("Flash", out Transform flash))
            {
                flash.localScale *= multiplier;
            }
            return crocoSuperBiteEffect;
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