using RoR2.Items;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public class Theremin : MonoBehaviour
    {
        public static float attackSpeedBonus = 0.45f;
        public static float attackSpeedBonusPerStack = 0.35f;

        public void Awake()
        {
            Instance.loadStaticContentAsync += LoadStaticContentAsync;
        }

        private IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            yield return Instance.Assets.LoadAssetAsync<Sprite>("texThereminIcon", out var texThereminIcon);
            yield return Instance.Assets.LoadAssetAsync<GameObject>("PickupTheremin", out var PickupTheremin);

            Items.Theremin = Instance.Content.DefineItem("Theremin")
                .SetIconSprite(texThereminIcon.asset)
                .SetItemTier(ItemTier.Tier2)
                .SetPickupModelPrefab(PickupTheremin.asset, new ModelPanelParams(new Vector3(56, 180, 0), 1, 5))
                .SetTags(ItemTag.Damage, ItemTag.InteractableRelated, ItemTag.OnKillEffect);

            yield return Instance.Assets.LoadAssetAsync<GameObject>("DisplayTheremin", out var DisplayTheremin);

            Ivyl.SetupItemDisplay(DisplayTheremin.asset);
            ItemDisplaySpec itemDisplay = new ItemDisplaySpec(Items.Theremin, DisplayTheremin.asset);
            var idrs = Instance.itemDisplayRuleSets;
            idrs["idrsCommando"].AddDisplayRule(itemDisplay, "Stomach", new Vector3(0.13491F, -0.05978F, -0.126F), new Vector3(285.9316F, 354.6909F, 343.4794F), new Vector3(1.05089F, 1.06357F, 1.05089F));
            idrs["idrsHuntress"].AddDisplayRule(itemDisplay, "Pelvis", new Vector3(0.14773F, 0.04592F, 0.14647F), new Vector3(55.92511F, 189.9159F, 157.5366F), new Vector3(1.06336F, 1.06336F, 1.06336F));
            idrs["idrsBandit2"].AddDisplayRule(itemDisplay, "Stomach", new Vector3(0.03759F, -0.13868F, -0.20006F), new Vector3(293.316F, 358.5328F, 352.6877F), new Vector3(1.16451F, 1.16451F, 1.16451F));
            idrs["idrsToolbot"].AddDisplayRule(itemDisplay, "Pelvis", new Vector3(2.16572F, 1.92139F, 0.36732F), new Vector3(85.40151F, 186.2415F, 189.3237F), new Vector3(11.43646F, 11.43646F, 11.43646F));
            idrs["idrsEngi"].AddDisplayRule(itemDisplay, "Pelvis", new Vector3(-0.12588F, 0.26729F, 0.16002F), new Vector3(85.04813F, 154.2319F, 180.0005F), new Vector3(1.1882F, 1.1882F, 1.1882F));
            idrs["idrsEngiTurret"].AddDisplayRule(itemDisplay, "Base", new Vector3(0.7389F, 1.19324F, 0.1201F), new Vector3(289.3343F, 254.302F, 2.8411F), new Vector3(5.67193F, 5.67193F, 5.67193F));
            idrs["idrsEngiWalkerTurret"].AddDisplayRule(itemDisplay, "Base", new Vector3(0.7389F, 1.19324F, 0.1201F), new Vector3(289.3343F, 254.302F, 2.8411F), new Vector3(5.67193F, 5.67193F, 5.67193F));
            idrs["idrsMage"].AddDisplayRule(itemDisplay, "Pelvis", new Vector3(0.21058F, 0.084F, -0.00462F), new Vector3(64.12035F, 235.4545F, 135.3318F), new Vector3(1.08244F, 1.08244F, 1.08244F));
            idrs["idrsMerc"].AddDisplayRule(itemDisplay, "Stomach", new Vector3(0.1925F, -0.23371F, -0.13575F), new Vector3(299.6336F, 330.2855F, 344.6577F), new Vector3(1.33784F, 1.33784F, 1.33784F));
            idrs["idrsTreebot"].AddDisplayRule(itemDisplay, "LowerArmL", new Vector3(-0.00397F, -0.03737F, 0.35329F), new Vector3(306.5481F, 357.0492F, 187.2086F), new Vector3(2.72495F, 2.72495F, 2.72495F));
            idrs["idrsLoader"].AddDisplayRule(itemDisplay, "ThighR", new Vector3(-0.1191F, 0.55523F, -0.19393F), new Vector3(61.97856F, 349.4733F, 200.9573F), new Vector3(1.2811F, 1.2811F, 1.2811F));
            idrs["idrsCroco"].AddDisplayRule(itemDisplay, "Head", new Vector3(-0.18713F, 5.86495F, -3.83413F), new Vector3(11.04338F, 1.05804F, 186.2499F), new Vector3(21.72311F, 21.72311F, 21.72311F));
            idrs["idrsCaptain"].AddDisplayRule(itemDisplay, "ClavicleR", new Vector3(-0.08363F, 0.47398F, 0.01307F), new Vector3(52.64773F, 174.563F, 340.3903F), new Vector3(1.36564F, 1.36564F, 1.36564F));
            idrs["idrsRailGunner"].AddDisplayRule(itemDisplay, "Backpack", new Vector3(0.22501F, -0.6498F, 0.05793F), new Vector3(293.1928F, 97.78774F, 253.1585F), new Vector3(1.17195F, 1.17195F, 1.17195F));
            idrs["idrsVoidSurvivor"].AddDisplayRule(itemDisplay, "CalfR", new Vector3(0.09434F, 0.41737F, -0.00356F), new Vector3(82.28655F, 146.8268F, 41.39848F), new Vector3(1.14532F, 1.14532F, 1.14532F));
            idrs["idrsScav"].AddDisplayRule(itemDisplay, "Backpack", new Vector3(-11.52265F, 9.98327F, 1.99621F), new Vector3(284.7374F, 89.41682F, 348.9175F), new Vector3(21.38155F, 21.38155F, 21.38155F));
        }

        public void OnEnable()
        {
            On.RoR2.MusicController.UpdateTeleporterParameters += MusicController_UpdateTeleporterParameters;
        }

        public void OnDisable()
        {
            On.RoR2.MusicController.UpdateTeleporterParameters -= MusicController_UpdateTeleporterParameters;
        }

        private void MusicController_UpdateTeleporterParameters(On.RoR2.MusicController.orig_UpdateTeleporterParameters orig, MusicController self, TeleporterInteraction teleporter, Transform cameraTransform, CharacterBody targetBody)
        {
            orig(self, teleporter, cameraTransform, targetBody);
            self.rtpcTeleporterProximityValue.value = Util.Remap(self.rtpcTeleporterProximityValue.value, 0f, 10000f, 5000f, 10000f);
            if (targetBody && targetBody.HasItem(Items.Theremin) && targetBody.TryGetComponent(out ThereminBehaviour behaviour))
            {
                self.rtpcTeleporterProximityValue.value -= 5000f * behaviour.currentBonusCoefficient;
                float directionValue = self.rtpcTeleporterDirectionValue.value;
                self.rtpcTeleporterDirectionValue.value -= (directionValue <= 180f ? directionValue : directionValue - 360f) * behaviour.currentBonusCoefficient;
            }
        }
        
        public class ThereminBehaviour : BaseItemBodyBehavior
        {
            [ItemDefAssociation(useOnServer = true, useOnClient = true)]
            public static ItemDef GetItemDef() => Items.Theremin;

            public float currentBonus;
            public float currentBonusCoefficient;
            public int lastPercentBonus;

            public void FixedUpdate()
            {
                if (TeleporterUtil.TryLocateTeleporter(out Vector3 position))
                {
                    Vector3 distance = body.corePosition - position;
                    currentBonusCoefficient = 1000f / (1000f + distance.sqrMagnitude);
                    currentBonus = currentBonusCoefficient * Ivyl.StackScaling(attackSpeedBonus, attackSpeedBonusPerStack, stack);
                }
                else
                {
                    currentBonusCoefficient = 0;
                    currentBonus = 0;
                }
                int percentBonus = (int)(currentBonus * 100f);
                if (percentBonus != lastPercentBonus)
                {
                    lastPercentBonus = percentBonus;
                    body.MarkAllStatsDirty();
                }
            }

            public void OnEnable()
            {
                RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            }

            public void OnDisable()
            {
                RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients;
            }

            private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (sender && sender == body)
                {
                    args.attackSpeedMultAdd += currentBonus;
                }
            }
        }
    }
}