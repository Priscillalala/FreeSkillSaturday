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
using EntityStates.Railgunner.Weapon;

namespace FreeItemFriday.Skills
{
    public class BouncingBullets : FreeSkillSaturday.Behavior
    {
        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _railgunnerPassiveFamily = "RoR2/DLC1/Railgunner/RailgunnerPassiveFamily.asset";

            Content.Skills.RailgunnerPassiveBouncingBullets = Expansion.DefineSkill<BouncingBulletsSkillDef>("RailgunnerPassiveBouncingBullets")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texRailgunnerElectricGrenadeIcon"));

            /*Content.Achievements.RailgunnerHipster = Expansion.DefineAchievementForSkill("RailgunnerHipster", Content.Skills.RailgunnerElectricGrenade)
                .SetIconSprite(Content.Skills.RailgunnerElectricGrenade.icon)
                .SetTrackerTypes(typeof(RailgunnerHipsterAchievement), null);*/

            SkillFamily railgunnerPassiveFamily = await _railgunnerPassiveFamily;
            railgunnerPassiveFamily.AddSkill(Content.Skills.RailgunnerPassiveBouncingBullets, null);
        }

        public class BouncingBulletsSkillDef : SkillDef
        {
            private static bool setHooks;
            private static Dictionary<GameObject, InstanceData> assignedInstances;

            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                InstanceData instanceData = new InstanceData();
                (assignedInstances ??= new Dictionary<GameObject, InstanceData>()).Add(skillSlot.gameObject, instanceData);
                RecalculateHooks();
                return instanceData;
            }

            public override void OnUnassigned([NotNull] GenericSkill skillSlot)
            {
                assignedInstances?.Remove(skillSlot.gameObject);
                RecalculateHooks();
            }

            public static void RecalculateHooks()
            {
                if (setHooks != assignedInstances.Count > 0)
                {
                    if (setHooks)
                    {
                        UnsetHooks();
                    } 
                    else
                    {
                        SetHooks();
                    }
                }
            }

            public static void SetHooks()
            {
                On.EntityStates.Railgunner.Weapon.BaseFireSnipe.ModifyBullet += BaseFireSnipe_ModifyBullet;
                On.RoR2.UI.SniperTargetViewer.Update += SniperTargetViewer_Update;
                On.RoR2.CharacterBody.OnModelChanged += CharacterBody_OnModelChanged;
                setHooks = true;
            }

            public static void UnsetHooks()
            {
                On.EntityStates.Railgunner.Weapon.BaseFireSnipe.ModifyBullet -= BaseFireSnipe_ModifyBullet;
                On.RoR2.UI.SniperTargetViewer.Update -= SniperTargetViewer_Update;
                setHooks = false;
            }

            private static void BaseFireSnipe_ModifyBullet(On.EntityStates.Railgunner.Weapon.BaseFireSnipe.orig_ModifyBullet orig, BaseFireSnipe self, BulletAttack bulletAttack)
            {
                orig(self, bulletAttack);
                if (assignedInstances.ContainsKey(self.gameObject))
                {
                    if (!self.isPiercing)
                    {
                        self.piercingDamageCoefficientPerTarget = 0.5f;
                    }
                    //bulletAttack.sniper = false;
                    bulletAttack.hitCallback = (BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo) =>
                    {
                        bool result = BulletAttack.defaultHitCallback(bulletAttack, ref hitInfo);
                        if (hitInfo.isSniperHit)
                        {
                            bulletAttack.hitCallback = BulletAttack.defaultHitCallback;
                            BullseyeSearch search = new BullseyeSearch();
                            search.searchOrigin = hitInfo.point;
                            search.searchDirection = hitInfo.direction;
                            search.teamMaskFilter = TeamMask.allButNeutral;
                            search.teamMaskFilter.RemoveTeam(self.teamComponent.teamIndex);
                            search.filterByLoS = true;
                            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
                            search.filterByDistinctEntity = true;
                            search.maxDistanceFilter = 20f;
                            search.maxAngleFilter = 180f;
                            search.RefreshCandidates();
                            search.FilterOutGameObject(hitInfo.entityObject);
                            HurtBox[] targets = search
                            .GetResults()
                            .Select(x => x.hurtBoxGroup?.hurtBoxes[UnityEngine.Random.Range(0, x.hurtBoxGroup.hurtBoxes.Length)])
                            .Where(x => x)
                            .ToArray();
                            if (targets.Length > 0)
                            {
                                int remainingBounces = 2;
                                float distance = hitInfo.distance;
                                Vector3 prevPosition = hitInfo.point;
                                if (Util.CheckRoll(50f, self?.characterBody?.master))
                                {
                                    remainingBounces++;
                                }
                                for (int i = 0; i < targets.Length; i++)
                                {
                                    HurtBox target = targets[i];
                                    Vector3 currentPosition = target.randomVolumePoint;
                                    distance += Vector3.Distance(prevPosition, currentPosition);
                                    if (remainingBounces > 0 && i < targets.Length - 1)
                                    {
                                        BulletAttack.BulletHit bouncedHitInfo = new BulletAttack.BulletHit
                                        {
                                            direction = currentPosition - prevPosition,
                                            point = currentPosition,
                                            surfaceNormal = prevPosition - currentPosition,
                                            distance = distance,
                                            collider = target.collider,
                                            hitHurtBox = target,
                                            entityObject = target.healthComponent ? target.healthComponent.gameObject : target.gameObject,
                                            damageModifier = HurtBox.DamageModifier.Normal,
                                            isSniperHit = false,
                                        };
                                        BulletAttack.defaultHitCallback(bulletAttack, ref bouncedHitInfo);
                                        if (bulletAttack.tracerEffectPrefab)
                                        {
                                            EffectData effectData = new EffectData
                                            {
                                                origin = currentPosition,
                                                start = prevPosition
                                            };
                                            EffectManager.SpawnEffect(bulletAttack.tracerEffectPrefab, effectData, true);
                                        }
                                        if (--remainingBounces <= 0)
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        GameObject previousTargetObject = hitInfo.entityObject;
                                        if (i > 0)
                                        {
                                            HurtBox previousTarget = targets[i - 1];
                                            previousTargetObject = previousTarget.healthComponent ? previousTarget.healthComponent.gameObject : previousTarget.gameObject;
                                        }
                                        bulletAttack.aimVector = currentPosition - prevPosition;
                                        bulletAttack.origin = prevPosition;
                                        bulletAttack.weapon = previousTargetObject;
                                        bulletAttack.bulletCount = 1;
                                        bulletAttack.Fire();
                                    }
                                    prevPosition = currentPosition;
                                }
                            }
                            result = false;
                        }
                        return result;
                    };
                }
            }

            private static void SniperTargetViewer_Update(On.RoR2.UI.SniperTargetViewer.orig_Update orig, SniperTargetViewer self)
            {
                if (self.hud?.targetBodyObject && assignedInstances.TryGetValue(self.hud.targetBodyObject, out InstanceData instanceData))
                {
                    self.SetDisplayedTargets(instanceData.GetValidBounceTargetsForTeam(self.hud.targetMaster.teamIndex));
                    return;
                }
                orig(self);
            }

            private static void CharacterBody_OnModelChanged(On.RoR2.CharacterBody.orig_OnModelChanged orig, CharacterBody self, Transform modelTransform)
            {
                HurtBoxGroup previousHurtboxGroup = self.hurtBoxGroup;
                orig(self, modelTransform);
                if (previousHurtboxGroup != self.hurtBoxGroup && assignedInstances.TryGetValue(self.gameObject, out InstanceData instanceData))
                {

                }
            }

            public class InstanceData : BaseSkillInstanceData
            {
                public HashSet<HurtBox> bounceTargets = new HashSet<HurtBox>();
                public List<HurtBox> validBounceTargets = new List<HurtBox>();

                public void AddHurtboxGroup(CharacterBody body)
                {

                }

                public void RemoveHurtboxGroup(HurtBoxGroup hurtBoxGroup)
                {

                }

                public IReadOnlyList<HurtBox> GetValidBounceTargetsForTeam(TeamIndex teamIndex)
                {
                    validBounceTargets.Clear();
                    foreach (HurtBox hurtBox in bounceTargets)
                    {
                        if (hurtBox.gameObject.activeInHierarchy && hurtBox.healthComponent && hurtBox.healthComponent.alive && FriendlyFireManager.ShouldDirectHitProceed(hurtBox.healthComponent, teamIndex))
                        {
                            validBounceTargets.Add(hurtBox);
                        }
                    }
                    return validBounceTargets;
                }
            }
        }
    }
}