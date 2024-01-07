using RoR2.Skills;
using FreeItemFriday.Achievements;
using UnityEngine.UI;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public class Reboot : MonoBehaviour
    {
        public static float duration = 3f;

        public static GameObject RebootOverlay { get; private set; }
        public static GameObject VentEffect { get; private set; }

        public void Awake()
        {
            Instance.loadStaticContentAsync += LoadStaticContentAsync;
        }

        private IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            yield return Instance.Assets.LoadAssetAsync<Sprite>("texToolbotRebootIcon", out var texToolbotRebootIcon);

            Skills.ToolbotReboot = Instance.Content.DefineSkill<SkillDef>("ToolbotReboot")
                .SetIconSprite(texToolbotRebootIcon.asset)
                .SetActivationState(typeof(EntityStates.Toolbot.Reboot), "Body")
                .SetCooldown(60f)
                .SetInterruptPriority(EntityStates.InterruptPriority.Skill)
                .SetFlags(SkillFlags.MustKeyPress | SkillFlags.BeginSkillCooldownOnSkillEnd | SkillFlags.NonCombat | SkillFlags.NoRestockOnAssign | SkillFlags.Agile);

            yield return Ivyl.LoadAddressableAssetAsync<SkillFamily>("RoR2/Base/Toolbot/ToolbotBodyUtilityFamily.asset", out var ToolbotBodyUtilityFamily);

            Achievements.ToolbotOverclocked = Instance.Content.DefineAchievementForSkill("ToolbotOverclocked", ref ToolbotBodyUtilityFamily.Result.AddSkill(Skills.ToolbotReboot))
                .SetIconSprite(Skills.ToolbotReboot.icon)
                .SetPrerequisiteAchievement("RepeatFirstTeleporter")
                .SetTrackerTypes(typeof(ToolbotOverclockedAchievement), null);

            yield return CreateRebootOverlayAsync();
            yield return CreateVentEffectAsync();
        }

        public IEnumerator CreateRebootOverlayAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerOfflineUI.prefab", out var RailgunnerOfflineUI);
            yield return Instance.Assets.LoadAssetAsync<Sprite>("texRebootUIGear", out var texRebootUIGear);

            RebootOverlay = Ivyl.ClonePrefab(RailgunnerOfflineUI.Result, "RebootOverlay");
            Transform barContainer = RebootOverlay.transform.Find("BarContainer");
            barContainer.localPosition = Vector3.zero;
            barContainer.localEulerAngles = Vector3.zero;
            barContainer.transform.Find("Inner").localPosition = Vector3.zero;
            Image backdropImage = barContainer.transform.Find("Inner/FillBarDimensions/Fillbar Backdrop").GetComponent<Image>();
            backdropImage.transform.localScale = Vector3.one * 1.1f;
            backdropImage.color = new Color32(0, 0, 0, 200);
            Image fillbarImage = barContainer.transform.Find("Inner/FillBarDimensions/FillBar").GetComponent<Image>();
            fillbarImage.sprite = texRebootUIGear.asset;
            fillbarImage.color = Color.white;
            DestroyImmediate(barContainer.transform.Find("SoftGlow").gameObject);
            DestroyImmediate(barContainer.transform.Find("Inner/SpinnySquare").gameObject);
        }

        public IEnumerator CreateVentEffectAsync()
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1Starburst.prefab", out var Chest1Starburst);

            VentEffect = Ivyl.ClonePrefab(Chest1Starburst.Result, "ToolbotVentEffect");
            DestroyImmediate(VentEffect.transform.Find("Dust").gameObject);
            DestroyImmediate(VentEffect.transform.Find("BurstLight").gameObject);
            DestroyImmediate(VentEffect.transform.Find("Beams").gameObject);
            VentEffect.AddComponent(out EffectComponent effectComponent);
            effectComponent.soundName = "Play_env_geyser_launch";
            VentEffect.AddComponent(out VFXAttributes vFXAttributes);
            vFXAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            vFXAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;

            Instance.Content.AddEffectPrefab(VentEffect);
        }
    }
}