using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using R2API;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using HG;
using FreeItemFriday.Achievements;
using System.Threading.Tasks;

namespace FreeItemFriday.Items
{
    public class FlintArrowhead : FreeSkillSaturday.Behavior
    {
        public static float damage = 3f;
        public static float damagePerStack = 3f;

        public DamageColorIndex strongerBurn = ColorsAPI.RegisterDamageColor(new Color32(244, 113, 80, 255));
        public GameObject impactArrowhead;
        public GameObject impactArrowheadStronger;

        public void Awake()
        {
            Content.Items.Arrowhead = Expansion.DefineItem("Arrowhead")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texArrowheadIcon"))
                .SetItemTier(ItemTier.Tier1)
                .SetPickupModelPrefab(Assets.LoadAsset<GameObject>("PickupArrowhead"), new ModelPanelParams(Vector3.zero, 1, 8))
                .SetTags(ItemTag.Damage);

            Content.Achievements.BurnMultipleEnemies = Expansion.DefineAchievementForItem("BurnMultipleEnemies", Content.Items.Arrowhead)
                .SetIconSprite(Assets.LoadAsset<Sprite>("texBurnMultipleEnemiesIcon"))
                .SetTrackerTypes(typeof(BurnMultipleEnemiesAchievement), typeof(BurnMultipleEnemiesAchievement.ServerAchievement));
            // Match achievement identifiers from FreeItemFriday
            Content.Achievements.BurnMultipleEnemies.AchievementDef.identifier = "BurnMultipleEnemies";

            GameObject displayModelPrefab = Assets.LoadAsset<GameObject>("DisplayArrowhead");
            IvyLibrary.SetupItemDisplay(displayModelPrefab);
            ItemDisplaySpec itemDisplay = new ItemDisplaySpec(Content.Items.Arrowhead, displayModelPrefab);
            Idrs.Commando.AddDisplayRule(itemDisplay, "Pelvis", new Vector3(-0.162F, -0.09F, -0.053F), new Vector3(7.522F, 244.056F, 358.818F), new Vector3(0.469F, 0.469F, 0.469F));
            Idrs.Huntress.AddDisplayRule(itemDisplay, "Arrow", new Vector3(0.343F, 0F, 0F), new Vector3(87.415F, 144.866F, 55.112F), new Vector3(0.388F, 0.388F, 0.388F));
            Idrs.Bandit.AddDisplayRule(itemDisplay, "Chest", new Vector3(0.153F, -0.144F, 0.066F), new Vector3(355.538F, 89.398F, 170.59F), new Vector3(0.507F, 0.507F, 0.507F));
            Idrs.MULT.AddDisplayRule(itemDisplay, "Head", new Vector3(-0.925F, 2.842F, 1.601F), new Vector3(45.327F, 331.491F, 198.947F), new Vector3(3.118F, 3.118F, 3.118F));
            Idrs.Engineer.AddDisplayRule(itemDisplay, "Pelvis", new Vector3(0.205F, 0.05F, -0.102F), new Vector3(0F, 114.381F, 354.036F), new Vector3(0.523F, 0.523F, 0.523F));
            Idrs.EngineerTurret.AddDisplayRule(itemDisplay, "Head", new Vector3(0.681F, 1.016F, -0.988F), new Vector3(0.775F, 180F, 202.127F), new Vector3(1.588F, 1.588F, 1.588F));
            Idrs.EngineerWalkerTurret.AddDisplayRule(itemDisplay, "Head", new Vector3(0.566F, 1.491F, -0.94F), new Vector3(7.103F, 180F, 204.769F), new Vector3(1.588F, 1.588F, 1.588F));
            Idrs.Artificer.AddDisplayRule(itemDisplay, "Pelvis", new Vector3(-0.159F, -0.085F, -0.09F), new Vector3(356.235F, 252.299F, 344.311F), new Vector3(0.46F, 0.46F, 0.46F));
            Idrs.Mercenary.AddDisplayRule(itemDisplay, "UpperArmL", new Vector3(0.161F, -0.006F, 0.001F), new Vector3(29.587F, 212.128F, 321.824F), new Vector3(0.493F, 0.493F, 0.493F));
            Idrs.REX.AddDisplayRule(itemDisplay, "PlatformBase", new Vector3(1.062F, 0.782F, 0.174F), new Vector3(337.728F, 201.301F, 224.188F), new Vector3(1.056F, 1.056F, 1.056F));
            Idrs.Loader.AddDisplayRule(itemDisplay, "MechUpperArmL", new Vector3(0.037F, 0.053F, -0.154F), new Vector3(335.055F, 244.872F, 293.27F), new Vector3(0.547F, 0.547F, 0.547F));
            Idrs.Acrid.AddDisplayRule(itemDisplay, "Head", new Vector3(1.926F, -0.053F, -0.112F), new Vector3(45.85F, 17.71F, 113.992F), new Vector3(5.36F, 5.36F, 5.36F));
            Idrs.Captain.AddDisplayRule(itemDisplay, "ClavicleL", new Vector3(0.021F, 0.136F, -0.226F), new Vector3(52.975F, 287.284F, 287.388F), new Vector3(0.587F, 0.587F, 0.587F));
            Idrs.Railgunner.AddDisplayRule(itemDisplay, "Pelvis", new Vector3(0.155F, 0.079F, -0.029F), new Vector3(10.264F, 100.904F, 358.845F), new Vector3(0.434F, 0.434F, 0.434F));
            Idrs.VoidFiend.AddDisplayRule(itemDisplay, "ShoulderL", new Vector3(0.063F, 0.289F, 0.052F), new Vector3(13.815F, 321.452F, 169.227F), new Vector3(0.597F, 0.597F, 0.597F));
            Idrs.Scavenger.AddDisplayRule(itemDisplay, "Weapon", new Vector3(3.037F, 8.08F, 2.629F), new Vector3(45.304F, 318.616F, 106.156F), new Vector3(5.5F, 5.5F, 5.5F));

            /*Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab").Completed += handle =>
            {
                impactArrowhead = IvyLibrary.CreatePrefab(handle.Result, "ImpactArrowhead");

                if (impactArrowhead.TryGetComponent(out EffectComponent effectComponent))
                {
                    effectComponent.soundName = "Play_item_proc_strengthenBurn";
                }
                if (impactArrowhead.TryGetComponent(out VFXAttributes vFXAttributes))
                {
                    vFXAttributes.vfxPriority = VFXAttributes.VFXPriority.Low;
                }
                if (impactArrowhead.TryGetComponent(out OmniEffect omniEffect))
                {
                    for (int i = omniEffect.omniEffectGroups.Length - 1; i >= 0; i--)
                    {
                        switch (omniEffect.omniEffectGroups[i].name)
                        {
                            case "Scaled Smoke":
                            case "Smoke Ring":
                            case "Area Indicator Ring":
                            case "Unscaled Smoke":
                            case "Flames":
                                ArrayUtils.ArrayRemoveAtAndResize(ref omniEffect.omniEffectGroups, i);
                                break;
                        }
                    }
                }
                Expansion.AddEffectPrefab(impactArrowhead);

                impactArrowheadStronger = IvyLibrary.CreatePrefab(impactArrowhead, "ImpactArrowHeadStronger");
                Addressables.LoadAssetAsync<Material>("RoR2/Base/IgniteOnKill/matOmniHitspark3Gasoline.mat").Completed += handle =>
                {
                    impactArrowheadStronger.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = handle.Result;
                };
                //impactArrowheadStronger.transform.GetChild(0).GetComponent<Renderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/IgniteOnKill/matOmniHitspark3Gasoline.mat").WaitForCompletion();
                Expansion.AddEffectPrefab(impactArrowheadStronger);
            };*/

            Task<GameObject> _impactArrowhead = CreateImpactArrowheadAsync();
            _impactArrowhead.WhenCompleted(t => impactArrowhead = t.Result);
            CreateImpactArrowheadStrongerAsync(_impactArrowhead).WhenCompleted(t => impactArrowheadStronger = t.Result);

            On.RoR2.DotController.InitDotCatalog += DotController_InitDotCatalog;
            Events.GlobalEventManager.onHitEnemyAcceptedServer += GlobalEventManager_onHitEnemyAcceptedServer;
        }

        public async Task<GameObject> CreateImpactArrowheadAsync()
        {
            IvyLibrary.LoadAddressableAsync<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab", out var _omniExplosionVFXQuick);
            GameObject impactArrowhead = IvyLibrary.CreatePrefab(await _omniExplosionVFXQuick, "ImpactArrowhead");

            if (impactArrowhead.TryGetComponent(out EffectComponent effectComponent))
            {
                effectComponent.soundName = "Play_item_proc_strengthenBurn";
            }
            if (impactArrowhead.TryGetComponent(out VFXAttributes vFXAttributes))
            {
                vFXAttributes.vfxPriority = VFXAttributes.VFXPriority.Low;
            }
            if (impactArrowhead.TryGetComponent(out OmniEffect omniEffect))
            {
                for (int i = omniEffect.omniEffectGroups.Length - 1; i >= 0; i--)
                {
                    switch (omniEffect.omniEffectGroups[i].name)
                    {
                        case "Scaled Smoke":
                        case "Smoke Ring":
                        case "Area Indicator Ring":
                        case "Unscaled Smoke":
                        case "Flames":
                            ArrayUtils.ArrayRemoveAtAndResize(ref omniEffect.omniEffectGroups, i);
                            break;
                    }
                }
            }
            Expansion.AddEffectPrefab(impactArrowhead);
            return impactArrowhead;
        }

        public async Task<GameObject> CreateImpactArrowheadStrongerAsync(Task<GameObject> _impactArrowhead)
        {
            IvyLibrary.LoadAddressableAsync<Material>("RoR2/Base/IgniteOnKill/matOmniHitspark3Gasoline.mat", out var _matOmniHitspark3Gasoline);
            GameObject impactArrowheadStronger = IvyLibrary.CreatePrefab(await _impactArrowhead, "ImpactArrowHeadStronger");
            _matOmniHitspark3Gasoline.WhenCompleted(t => impactArrowheadStronger.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial = t.Result);
            Expansion.AddEffectPrefab(impactArrowheadStronger);
            return impactArrowheadStronger;
        }

        private void DotController_InitDotCatalog(On.RoR2.DotController.orig_InitDotCatalog orig)
        {
            orig();
            DotController.dotDefs[(int)DotController.DotIndex.StrongerBurn].damageColorIndex = strongerBurn;
        }

        private void GlobalEventManager_onHitEnemyAcceptedServer(DamageInfo damageInfo, GameObject victim, uint? dotMaxStacksFromAttacker)
        {
            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.HasItem(Content.Items.Arrowhead, out int stack) && Util.CheckRoll(100f * damageInfo.procCoefficient, attackerBody.master))
            {
                InflictDotInfo inflictDotInfo = new InflictDotInfo
                {
                    attackerObject = damageInfo.attacker,
                    dotIndex = DotController.DotIndex.Burn,
                    victimObject = victim,
                    totalDamage = IvyLibrary.StackScaling(damage, damagePerStack, stack),
                };
                StrengthenBurnUtils.CheckDotForUpgrade(attackerBody.inventory, ref inflictDotInfo);
                DotController.DotDef dotDef = DotController.GetDotDef(inflictDotInfo.dotIndex);
                if (dotDef != null)
                {
                    DamageInfo burnDamageInfo = new DamageInfo();
                    burnDamageInfo.attacker = inflictDotInfo.attackerObject;
                    burnDamageInfo.crit = false;
                    burnDamageInfo.damage = (float)inflictDotInfo.totalDamage;
                    burnDamageInfo.force = Vector3.zero;
                    burnDamageInfo.inflictor = inflictDotInfo.attackerObject;
                    burnDamageInfo.position = damageInfo.position;
                    burnDamageInfo.procCoefficient = 0f;
                    burnDamageInfo.damageColorIndex = dotDef.damageColorIndex;
                    burnDamageInfo.damageType = DamageType.DoT | DamageType.Silent;
                    burnDamageInfo.dotIndex = inflictDotInfo.dotIndex;
                    if (inflictDotInfo.victimObject && inflictDotInfo.victimObject.TryGetComponent(out CharacterBody victimBody) && victimBody.healthComponent)
                    {
                        victimBody.healthComponent.TakeDamage(burnDamageInfo);
                        EffectManager.SpawnEffect(inflictDotInfo.dotIndex == DotController.DotIndex.Burn ? impactArrowhead : impactArrowheadStronger, new EffectData
                        {
                            origin = damageInfo.position,
                            rotation = Util.QuaternionSafeLookRotation(-damageInfo.force),
                            scale = inflictDotInfo.dotIndex == DotController.DotIndex.Burn ? 1.5f : 2.5f
                        }, true);
                    }
                }
            }
        }
    }
}