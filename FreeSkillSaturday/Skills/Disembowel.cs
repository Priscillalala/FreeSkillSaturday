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

namespace FreeItemFriday.Skills
{
    public class Disembowel : FreeSkillSaturday.Behavior
    {
        public static float damageCoefficient = 2f;

        public static DamageAPI.ModdedDamageType SuperBleedOnHit { get; private set; }
        public static GameObject CrocoSuperBiteEffect { get; private set; }

        public void Awake()
        {
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
                .SetTrackerTypes(typeof(CrocoKillBossCloakedAchievement), typeof(CrocoKillBossCloakedAchievement.ServerAchievement));

            Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodySpecialFamily.asset").Completed += handle =>
            {
                handle.Result.AddSkill(Content.Skills.CrocoSuperBite, Content.Achievements.CrocoBeatArenaFast.UnlockableDef);
            };

            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoBiteEffect.prefab").Completed += handle =>
            {
                CrocoSuperBiteEffect = handle.Result.InstantiateClone("CrocoSuperBiteEffect", false);
                Addressables.LoadAssetAsync<Material>("RoR2/Base/Croco/matCrocoGooSmall2.mat").Completed += handle =>
                {
                    if (CrocoSuperBiteEffect.transform.TryFind("Goo", out Transform goo) && goo.TryGetComponent(out ParticleSystemRenderer gooRenderer))
                    {
                        gooRenderer.sharedMaterial = handle.Result;
                    }
                };
                Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampPoison.png").Completed += handle =>
                {
                    if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail", out Transform swingTrail) && swingTrail.TryGetComponent(out ParticleSystemRenderer swingTrailRenderer))
                    {
                        swingTrailRenderer.sharedMaterial = new Material(swingTrailRenderer.sharedMaterial);
                        swingTrailRenderer.sharedMaterial.SetTexture("_RemapTex", handle.Result);
                        swingTrailRenderer.sharedMaterial.SetColor("_TintColor", new Color32(121, 255, 107, 255));
                    }
                };
                float multiplier = 1.2f;
                if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail", out Transform swingTrail))
                {
                    swingTrail.localScale *= multiplier;
                }
                if (CrocoSuperBiteEffect.transform.TryFind("SwingTrail, Distortion", out Transform swingTrailDistortion))
                {
                    swingTrailDistortion.localScale *= multiplier;
                }
                if (CrocoSuperBiteEffect.transform.TryFind("Flash", out Transform flash))
                {
                    flash.localScale *= multiplier;
                }
            };

            Events.GlobalEventManager.onHitEnemyAcceptedServer += GlobalEventManager_onHitEnemyAcceptedServer;
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