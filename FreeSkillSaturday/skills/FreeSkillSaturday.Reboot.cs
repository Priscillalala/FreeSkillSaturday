using RoR2.Skills;
using FreeItemFriday.Achievements;
using UnityEngine.UI;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class Reboot
    {
        public static bool enabled = true;
        public static float duration = 3f;

        public static GameObject RebootOverlay { get; private set; }
        public static GameObject VentEffect { get; private set; }

        public static void Init(FreeSkillSaturday instance)
        {
            const string SECTION = "Reboot";
            instance.SkillsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.SkillsConfig.Bind(ref duration, SECTION, "Duration");
            if (enabled)
            {
                instance.loadStaticContentAsync += CreateSkillAsync;
                instance.loadStaticContentAsync += CreateRebootOverlayAsync;
                instance.loadStaticContentAsync += CreateVentEffectAsync;
            }
        }

        private static IEnumerator<float> CreateSkillAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                texToolbotRebootIcon: args.assets.LoadAsync<Sprite>("texToolbotRebootIcon"),
                ToolbotBodyUtilityFamily: Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Toolbot/ToolbotBodyUtilityFamily.asset")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    Skills.ToolbotReboot = args.content.DefineSkill<SkillDef>("ToolbotReboot")
                        .SetIconSprite(loadOps.texToolbotRebootIcon.asset)
                        .SetActivationState(typeof(EntityStates.Toolbot.Reboot), "Body")
                        .SetCooldown(60f)
                        .SetInterruptPriority(EntityStates.InterruptPriority.Skill)
                        .SetFlags(SkillFlags.MustKeyPress | SkillFlags.BeginSkillCooldownOnSkillEnd | SkillFlags.NonCombat | SkillFlags.NoRestockOnAssign | SkillFlags.Agile);

                    ref SkillFamily.Variant skillVariant = ref loadOps.ToolbotBodyUtilityFamily.Result.AddSkill(Skills.ToolbotReboot);
                    Achievements.ToolbotOverclocked = args.content.DefineAchievementForSkill("ToolbotOverclocked", ref skillVariant)
                        .SetIconSprite(Skills.ToolbotReboot.icon)
                        .SetPrerequisiteAchievement("RepeatFirstTeleporter")
                        .SetTrackerTypes(typeof(ToolbotOverclockedAchievement), null);
                    // Match achievement identifiers from 1.6.1
                    Achievements.ToolbotOverclocked.AchievementDef.identifier = "FSS_ToolbotOverclocked";
                }
            };
        }

        private static IEnumerator<float> CreateRebootOverlayAsync(LoadStaticContentAsyncArgs args)
        {
            var loadOps = (
                RailgunnerOfflineUI: Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerOfflineUI.prefab"),
                texRebootUIGear: args.assets.LoadAsync<Sprite>("texRebootUIGear")
                );
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { loadOps },
                delegate
                {
                    RebootOverlay = Ivyl.ClonePrefab(loadOps.RailgunnerOfflineUI.Result, "RebootOverlay");
                    Transform barContainer = RebootOverlay.transform.Find("BarContainer");
                    barContainer.localPosition = Vector3.zero;
                    barContainer.localEulerAngles = Vector3.zero;
                    barContainer.transform.Find("Inner").localPosition = Vector3.zero;
                    Image backdropImage = barContainer.transform.Find("Inner/FillBarDimensions/Fillbar Backdrop").GetComponent<Image>();
                    backdropImage.transform.localScale = Vector3.one * 1.1f;
                    backdropImage.color = new Color32(0, 0, 0, 200);
                    Image fillbarImage = barContainer.transform.Find("Inner/FillBarDimensions/FillBar").GetComponent<Image>();
                    fillbarImage.sprite = loadOps.texRebootUIGear.asset;
                    fillbarImage.color = Color.white;
                    DestroyImmediate(barContainer.transform.Find("SoftGlow").gameObject);
                    DestroyImmediate(barContainer.transform.Find("Inner/SpinnySquare").gameObject);
                }
            };
        }

        private static IEnumerator<float> CreateVentEffectAsync(LoadStaticContentAsyncArgs args)
        {
            var Chest1Starburst = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1Starburst.prefab");
            return new GenericLoadingCoroutine
            {
                new AwaitAssetsCoroutine { Chest1Starburst },
                delegate
                {
                    VentEffect = Ivyl.ClonePrefab(Chest1Starburst.Result, "ToolbotVentEffect");
                    DestroyImmediate(VentEffect.transform.Find("Dust").gameObject);
                    DestroyImmediate(VentEffect.transform.Find("BurstLight").gameObject);
                    DestroyImmediate(VentEffect.transform.Find("Beams").gameObject);
                    VentEffect.AddComponent(out EffectComponent effectComponent);
                    effectComponent.soundName = "Play_env_geyser_launch";
                    VentEffect.AddComponent(out VFXAttributes vFXAttributes);
                    vFXAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
                    vFXAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;

                    args.content.AddEffectPrefab(VentEffect);
                }
            };
        }
    }
}