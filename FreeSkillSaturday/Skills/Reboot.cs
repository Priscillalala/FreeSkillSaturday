using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Skills;
using HG;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.Networking;
using FreeItemFriday.Achievements;
using RoR2.Projectile;
using R2API;
using UnityEngine.UI;
using RoR2.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using JetBrains.Annotations;

namespace FreeItemFriday.Skills
{
    public class Reboot : FreeSkillSaturday.Behavior
    {
        public static float duration = 3f;

        public static GameObject RebootOverlay { get; private set; }
        public static GameObject VentEffect { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _toolbotBodyUtilityFamily = "RoR2/Base/Toolbot/ToolbotBodyUtilityFamily.asset";
            using Task<GameObject> _rebootOverlay = CreateRebootOverlayAsync();
            using Task<GameObject> _ventEffect = CreateVentEffectAsync();

            Content.Skills.ToolbotReboot = Expansion.DefineSkill<SkillDef>("ToolbotReboot")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texToolbotRebootIcon"))
                .SetActivationState(typeof(EntityStates.Toolbot.Reboot), "Body")
                .SetCooldown(60f)
                .SetInterruptPriority(EntityStates.InterruptPriority.Skill)
                .SetFlags(SkillFlags.MustKeyPress | SkillFlags.BeginSkillCooldownOnSkillEnd | SkillFlags.NonCombat | SkillFlags.NoRestockOnAssign | SkillFlags.Agile);

            /*Content.Achievements.RailgunnerEliteSniper = Expansion.DefineAchievementForSkill("RailgunnerEliteSniper", Content.Skills.RailgunnerPassiveBouncingBullets)
                .SetIconSprite(Content.Skills.RailgunnerPassiveBouncingBullets.icon)
                .SetTrackerTypes(typeof(RailgunnerEliteSniperAchievement), null);*/

            SkillFamily toolbotBodyUtilityFamily = await _toolbotBodyUtilityFamily;
            toolbotBodyUtilityFamily.AddSkill(Content.Skills.ToolbotReboot, null);

            RebootOverlay = await _rebootOverlay;
            VentEffect = await _ventEffect;
        }

        public static async Task<GameObject> CreateRebootOverlayAsync()
        {
            using RoR2Asset<GameObject> _railgunnerOfflineUI = "RoR2/DLC1/Railgunner/RailgunnerOfflineUI.prefab";

            GameObject rebootOverley = Prefabs.ClonePrefab(await _railgunnerOfflineUI, "RebootOverlay");
            Transform barContainer = rebootOverley.transform.Find("BarContainer");
            barContainer.localPosition = Vector3.zero;
            barContainer.localEulerAngles = Vector3.zero;
            barContainer.transform.Find("Inner").localPosition = Vector3.zero;
            Image backdropImage = barContainer.transform.Find("Inner/FillBarDimensions/Fillbar Backdrop").GetComponent<Image>();
            backdropImage.transform.localScale = Vector3.one * 1.1f;
            backdropImage.color = new Color32(0, 0, 0, 200);
            Image fillbarImage = barContainer.transform.Find("Inner/FillBarDimensions/FillBar").GetComponent<Image>();
            fillbarImage.sprite = Assets.LoadAsset<Sprite>("texRebootUIGear");
            fillbarImage.color = Color.white;
            DestroyImmediate(barContainer.transform.Find("SoftGlow").gameObject);
            DestroyImmediate(barContainer.transform.Find("Inner/SpinnySquare").gameObject);
            
            return rebootOverley;
        }

        public static async Task<GameObject> CreateVentEffectAsync()
        {
            using RoR2Asset<GameObject> _chest1Starburst = "RoR2/Base/Chest1/Chest1Starburst.prefab";

            GameObject ventEffect = Prefabs.ClonePrefab(await _chest1Starburst, "ToolbotVentEffect");
            DestroyImmediate(ventEffect.transform.Find("Dust").gameObject);
            DestroyImmediate(ventEffect.transform.Find("BurstLight").gameObject);
            DestroyImmediate(ventEffect.transform.Find("Beams").gameObject);
            ventEffect.AddComponent(out EffectComponent effectComponent);
            effectComponent.soundName = "Play_env_geyser_launch";
            ventEffect.AddComponent(out VFXAttributes vFXAttributes);
            vFXAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            vFXAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;

            Expansion.AddEffectPrefab(ventEffect);

            return ventEffect;
        }
    }
}