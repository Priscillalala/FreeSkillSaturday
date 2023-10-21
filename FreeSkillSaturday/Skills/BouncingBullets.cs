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
using EntityStates.Railgunner.Scope;

namespace FreeItemFriday.Skills
{
    public class BouncingBullets : FreeSkillSaturday.Behavior
    {
        public static float bounceRadius = 30f;

        public static GameObject SmartTargetVisualizer { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _railgunnerPassiveFamily = "RoR2/DLC1/Railgunner/RailgunnerPassiveFamily.asset";
            using Task<GameObject> _smartTargetVisualizer = CreateSmartTargetVisualizerAsync();

            Content.Skills.RailgunnerPassiveBouncingBullets = Expansion.DefineSkill<BouncingBulletsSkillDef>("RailgunnerPassiveBouncingBullets")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texRailgunnerBouncingBulletsIcon"));

            Content.Achievements.RailgunnerEliteSniper = Expansion.DefineAchievementForSkill("RailgunnerEliteSniper", Content.Skills.RailgunnerPassiveBouncingBullets)
                .SetIconSprite(Content.Skills.RailgunnerPassiveBouncingBullets.icon)
                .SetTrackerTypes(typeof(RailgunnerEliteSniperAchievement), null);

            SkillFamily railgunnerPassiveFamily = await _railgunnerPassiveFamily;
            railgunnerPassiveFamily.AddSkill(Content.Skills.RailgunnerPassiveBouncingBullets, Content.Achievements.RailgunnerEliteSniper.UnlockableDef);

            SmartTargetVisualizer = await _smartTargetVisualizer;
        }

        public static async Task<GameObject> CreateSmartTargetVisualizerAsync()
        {
            using RoR2Asset<GameObject> _railgunnerSniperTargetVisualizerHeavy = "RoR2/DLC1/Railgunner/RailgunnerSniperTargetVisualizerHeavy.prefab";

            GameObject smartTargetVisualizer = Prefabs.ClonePrefab(await _railgunnerSniperTargetVisualizerHeavy, "RailgunnerSniperSmartTargetVisualizer");
            Image outer = smartTargetVisualizer.transform.Find("Scaler/Outer").GetComponent<Image>();
            Image rectangle = smartTargetVisualizer.transform.Find("Scaler/Rectangle").GetComponent<Image>();
            outer.color = new Color32(79, 32, 29, 101);
            rectangle.color = new Color32(250, 158, 93, 255);
            rectangle.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            return smartTargetVisualizer;
        }

        public class BouncingBulletsSkillDef : SkillDef
        {
            private static bool setHooks;
            private static HashSet<GameObject> assignedInstances;
            private static Dictionary<GameObject, GameObject> smartScopeOverlayPrefabs;

            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                SmartTarget.hurtBoxSmartTargets ??= new Dictionary<HurtBox, HashSet<SmartTarget>>();
                SmartTarget.smartTargetPool ??= new Stack<SmartTarget>();
                (assignedInstances ??= new HashSet<GameObject>()).Add(skillSlot.gameObject);
                smartScopeOverlayPrefabs ??= new Dictionary<GameObject, GameObject>();
                RecalculateHooks();
                return null;
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
                On.EntityStates.Railgunner.Scope.BaseScopeState.OnEnter += BaseScopeState_OnEnter;
                setHooks = true;
            }

            public static void UnsetHooks()
            {
                On.EntityStates.Railgunner.Weapon.BaseFireSnipe.ModifyBullet -= BaseFireSnipe_ModifyBullet;
                On.EntityStates.Railgunner.Scope.BaseScopeState.OnEnter -= BaseScopeState_OnEnter;
                setHooks = false;
            }

            private static bool IsSmartTargetHit(in BulletAttack.BulletHit hitInfo, GameObject attackerBodyObject)
            {
                if (hitInfo.hitHurtBox && hitInfo.hitHurtBox.hurtBoxGroup)
                {
                    foreach (SmartTarget smartTarget in SmartTarget.GetSmartTargets(hitInfo.hitHurtBox.hurtBoxGroup))
                    {
                        if (smartTarget && smartTarget.ownerBodyObject == attackerBodyObject && Vector3.ProjectOnPlane(hitInfo.point - smartTarget.position, hitInfo.direction).sqrMagnitude <= HurtBox.sniperTargetRadiusSqr)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private static void BaseFireSnipe_ModifyBullet(On.EntityStates.Railgunner.Weapon.BaseFireSnipe.orig_ModifyBullet orig, BaseFireSnipe self, BulletAttack bulletAttack)
            {
                orig(self, bulletAttack);
                if (assignedInstances.Contains(self.gameObject))
                {
                    if (!self.isPiercing)
                    {
                        self.piercingDamageCoefficientPerTarget = 0.5f;
                    }
                    bulletAttack.sniper = false;
                    bulletAttack.hitCallback = (BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo) =>
                    {
                        bool willBounce = IsSmartTargetHit(hitInfo, self.gameObject);
                        DamageColorIndex? previousDamageColorIndex = null;
                        if (willBounce)
                        {
                            previousDamageColorIndex = bulletAttack.damageColorIndex;
                            bulletAttack.damageColorIndex = DamageColorIndex.WeakPoint;
                        }
                        bool result = BulletAttack.defaultHitCallback(bulletAttack, ref hitInfo);
                        if (willBounce)
                        {
                            if (BulletAttack.sniperTargetHitEffect != null)
                            {
                                EffectData effectData = new EffectData
                                {
                                    origin = hitInfo.point,
                                    rotation = Quaternion.LookRotation(-hitInfo.direction)
                                };
                                effectData.SetHurtBoxReference(hitInfo.hitHurtBox);
                                EffectManager.SpawnEffect(BulletAttack.sniperTargetHitEffect, effectData, true);
                            }

                            HashSet<HealthComponent> ignoredTargets = new HashSet<HealthComponent>();
                            ignoredTargets.Add(hitInfo.hitHurtBox.healthComponent);

                            BullseyeSearch search = new BullseyeSearch();
                            search.searchOrigin = hitInfo.point;
                            search.searchDirection = hitInfo.direction;
                            search.teamMaskFilter = TeamMask.allButNeutral;
                            search.teamMaskFilter.RemoveTeam(self.teamComponent.teamIndex);
                            search.filterByLoS = true;
                            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
                            search.filterByDistinctEntity = true;
                            search.maxDistanceFilter = bounceRadius;

                            bool TryFindNextTarget(out HurtBox nextTarget)
                            {
                                search.RefreshCandidates();
                                HurtBox target = search.GetResults().FirstOrDefault(x => x && x.healthComponent && !ignoredTargets.Contains(x.healthComponent));
                                if (target)
                                {
                                    nextTarget = target.hurtBoxGroup?.hurtBoxes[UnityEngine.Random.Range(0, target.hurtBoxGroup.hurtBoxes.Length)] ?? target;
                                    return true;
                                }
                                nextTarget = null;
                                return false;
                            }

                            if (TryFindNextTarget(out HurtBox nextTarget))
                            {
                                int remainingBounces = (int)(self.piercingDamageCoefficientPerTarget * 4);
                                float distance = hitInfo.distance;
                                Vector3 prevPosition = hitInfo.point;
                                GameObject previousTargetObject = hitInfo.entityObject;
                                for (; ; )
                                {
                                    HurtBox currentTarget = nextTarget;
                                    ignoredTargets.Add(currentTarget.healthComponent);
                                    Vector3 currentPosition = currentTarget.randomVolumePoint;
                                    GameObject currentTargetObject = currentTarget.healthComponent ? currentTarget.healthComponent.gameObject : currentTarget.gameObject;
                                    distance += Vector3.Distance(prevPosition, currentPosition);
                                    search.searchOrigin = currentPosition;
                                    search.searchDirection = (currentPosition - prevPosition).normalized;
                                    if (remainingBounces > 1 && TryFindNextTarget(out nextTarget))
                                    {
                                        BulletAttack.BulletHit bouncedHitInfo = new BulletAttack.BulletHit
                                        {
                                            direction = (currentPosition - prevPosition).normalized,
                                            point = currentPosition,
                                            surfaceNormal = (prevPosition - currentPosition).normalized,
                                            distance = distance,
                                            collider = currentTarget.collider,
                                            hitHurtBox = currentTarget,
                                            entityObject = currentTarget.healthComponent ? currentTarget.healthComponent.gameObject : currentTarget.gameObject,
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
                                        bulletAttack.force *= self.piercingDamageCoefficientPerTarget;
                                        remainingBounces--;
                                    }
                                    else
                                    {
                                        BulletAttackParams bulletAttackParams = new BulletAttackParams(bulletAttack);
                                        bulletAttack.aimVector = currentPosition - prevPosition;
                                        bulletAttack.origin = prevPosition;
                                        bulletAttack.weapon = previousTargetObject;
                                        bulletAttack.bulletCount = 1;
                                        bulletAttack.hitCallback = BulletAttack.defaultHitCallback;
                                        try
                                        {
                                            bulletAttack.Fire();
                                        }
                                        finally
                                        {
                                            bulletAttackParams.Apply(bulletAttack);
                                        }
                                        break;
                                    }
                                    prevPosition = currentPosition;
                                    previousTargetObject = currentTargetObject;
                                }
                                result = false;
                            }
                        }
                        if (previousDamageColorIndex != null)
                        {
                            bulletAttack.damageColorIndex = (DamageColorIndex)previousDamageColorIndex;
                        }
                        return result;
                    };
                }
            }

            public struct BulletAttackParams
            {
                public Vector3 aimVector;
                public Vector3 origin;
                public GameObject weapon;
                public uint bulletCount;
                public BulletAttack.HitCallback hitCallback;

                public BulletAttackParams(BulletAttack bulletAttack)
                {
                    aimVector = bulletAttack.aimVector;
                    origin = bulletAttack.origin;
                    weapon = bulletAttack.weapon;
                    bulletCount = bulletAttack.bulletCount;
                    hitCallback = bulletAttack.hitCallback;
                }

                public void Apply(BulletAttack bulletAttack)
                {
                    bulletAttack.aimVector = aimVector;
                    bulletAttack.origin = origin;
                    bulletAttack.weapon = weapon;
                    bulletAttack.bulletCount = bulletCount;
                    bulletAttack.hitCallback = hitCallback;
                }
            }

            private static void BaseScopeState_OnEnter(On.EntityStates.Railgunner.Scope.BaseScopeState.orig_OnEnter orig, BaseScopeState self)
            {
                if (assignedInstances.Contains(self.gameObject) && self.scopeOverlayPrefab) 
                {
                    if (!smartScopeOverlayPrefabs.TryGetValue(self.scopeOverlayPrefab, out GameObject smartScopeOverlayPrefab))
                    {
                        smartScopeOverlayPrefab = Prefabs.ClonePrefab(self.scopeOverlayPrefab, "RailgunnerScopeSmartOverlayVariant");
                        SniperTargetViewer sniperTargetViewer = smartScopeOverlayPrefab.GetComponentInChildren<SniperTargetViewer>();
                        if (sniperTargetViewer)
                        {
                            sniperTargetViewer.gameObject.AddComponent<SniperSmartTargetViewer>();
                            DestroyImmediate(sniperTargetViewer);
                        }
                    }
                    self.scopeOverlayPrefab = smartScopeOverlayPrefab;
                }
                orig(self);
            }
        }

        [RequireComponent(typeof(PointViewer))]
        public class SniperSmartTargetViewer : MonoBehaviour
        {
            private void Awake()
            {
                pointViewer = GetComponent<PointViewer>();
                OnTransformParentChanged();
            }

            private void OnTransformParentChanged()
            {
                hud = GetComponentInParent<HUD>();
            }

            private void OnDisable()
            {
                foreach (SmartTarget smartTarget in smartTargets)
                {
                    SmartTarget.Return(smartTarget);
                }
                smartTargets.Clear();
                UpdateTargetVisualizers();
            }

            private void Update()
            {
                foreach (SmartTarget smartTarget in smartTargets)
                {
                    SmartTarget.Return(smartTarget);
                }
                smartTargets.Clear();

                if (hud && hud.targetMaster && hud.targetBodyObject)
                {
                    TeamIndex teamIndex = hud.targetMaster.teamIndex;
                    foreach (CharacterBody targetBody in CharacterBody.readOnlyInstancesList)
                    {
                        if (targetBody.hurtBoxGroup
                            && targetBody.hurtBoxGroup.hurtBoxesDeactivatorCounter <= 0
                            && targetBody?.healthComponent && targetBody.healthComponent.alive
                            && FriendlyFireManager.ShouldDirectHitProceed(targetBody.healthComponent, teamIndex)
                            && targetBody.gameObject != hud.targetBodyObject)
                        {
                            int bounceTargetCount = Mathf.CeilToInt(targetBody.radius);
                            //Logger.LogInfo($"Radius: {targetBody.radius}, count: {bounceTargetCount}");
                            if (bounceTargetCount <= 0)
                            {
                                break;
                            }
                            Vector3 targetForwards = targetBody.characterDirection?.forward ?? targetBody.gameObject.transform.forward;
                            Vector3 attackerPosition = hud.targetMaster.GetBody()?.corePosition ?? hud.targetBodyObject.transform.position;

                            /*void AddSmartTarget(Vector3 inNormal)
                            {
                                //Vector3 point = targetBody.corePosition + Vector3.Reflect(targetBody.corePosition - attackerPosition, inNormal);
                                //Vector3 adjustedAttackerPosition = new Vector3(attackerPosition.x, targetBody.corePosition.y, attackerPosition.z);
                                Vector3 inDirection = targetBody.corePosition - attackerPosition;
                                //inDirection.y = 0f;
                                Vector3 point = targetBody.corePosition + (Vector3.Reflect(inDirection, inNormal).normalized * targetBody.bestFitRadius);
                                if (TryFindClosestPoint(targetBody.hurtBoxGroup, point, out Vector3 closestPoint, out HurtBox closestHurtBox))
                                {
                                    SmartTarget smartTarget = SmartTarget.Request(closestHurtBox, hud.targetBodyObject);
                                    smartTarget.position = closestPoint;
                                    smartTargets.Add(smartTarget);
                                }
                            }*/

                            Vector3 GetTargetPoint(Vector3 inNormal)
                            {
                                Vector3 inDirection = targetBody.corePosition - attackerPosition;
                                return targetBody.corePosition + (Vector3.Reflect(inDirection, inNormal).normalized * targetBody.bestFitRadius);
                            }

                            if (bounceTargetCount == 1 && targetBody.mainHurtBox)
                            {
                                Vector3 point = GetTargetPoint(targetForwards);
                                SmartTarget smartTarget = SmartTarget.Request(targetBody.mainHurtBox, hud.targetBodyObject);
                                smartTarget.position = targetBody.mainHurtBox.collider?.ClosestPoint(point) ?? targetBody.mainHurtBox.transform.position;
                                smartTargets.Add(smartTarget);
                                //AddSmartTarget(targetForwards);
                            } 
                            else
                            {
                                float angle = 180f / bounceTargetCount;
                                for (int i = 0; i < bounceTargetCount; i++)
                                {
                                    Vector3 inNormal = Quaternion.AngleAxis(i * angle, Vector3.up) * targetForwards;
                                    Vector3 point = GetTargetPoint(inNormal);
                                    if (TryFindClosestPoint(targetBody.hurtBoxGroup, point, out Vector3 closestPoint, out HurtBox closestHurtBox))
                                    {
                                        SmartTarget smartTarget = SmartTarget.Request(closestHurtBox, hud.targetBodyObject);
                                        smartTarget.position = closestPoint;
                                        smartTargets.Add(smartTarget);
                                    }
                                    //AddSmartTarget(inNormal);
                                }
                            }
                        }
                    }
                }

                UpdateTargetVisualizers();
            }

            public bool TryFindClosestPoint(HurtBoxGroup group, Vector3 point, out Vector3 closestPoint, out HurtBox closestHurtBox)
            {
                if (group.hurtBoxes == null || group.hurtBoxes.Length <= 0)
                {
                    closestPoint = Vector3.zero;
                    closestHurtBox = null;
                    return false;
                }
                /*(closestHurtBox, closestPoint) = group.hurtBoxes.Select(x => (x, x.collider?.ClosestPoint(point) ?? x.transform.position))
                    .OrderBy(x => (x.Item2 - point).sqrMagnitude).FirstOrDefault();*/
                closestHurtBox = group.hurtBoxes.OrderBy(x => (GetHurtBoxPosition(x) - point).sqrMagnitude).FirstOrDefault();
                closestPoint = closestHurtBox.collider?.ClosestPoint(point) ?? closestHurtBox.transform.position;
                return true;
            }

            public Vector3 GetHurtBoxPosition(HurtBox hurtBox)
            {
                if (!hurtBox || !hurtBox.healthComponent || !hurtBox.healthComponent.body)
                {
                    return hurtBox.transform.position;
                }
                if (relativeHurtBoxPositionsCache.TryGetValue(hurtBox, out Vector3 relativePosition)) 
                {
                    return hurtBox.healthComponent.body.corePosition + relativePosition;
                }
                relativeHurtBoxPositionsCache.Add(hurtBox, hurtBox.transform.position - hurtBox.healthComponent.body.corePosition);
                return hurtBox.transform.position;
            }

            private void UpdateTargetVisualizers()
            {
                _smartTargetToVisualizer.AddRange(smartTargetToVisualizer);
                foreach (KeyValuePair<SmartTarget, GameObject> pair in _smartTargetToVisualizer)
                {
                    if (!pair.Key.target)
                    {
                        pointViewer.RemoveElement(pair.Value);
                        smartTargetToVisualizer.Remove(pair.Key);
                    }
                }
                _smartTargetToVisualizer.Clear();

                foreach (SmartTarget smartTarget in smartTargets)
                {
                    if (!smartTargetToVisualizer.ContainsKey(smartTarget))
                    {
                        GameObject visualizer = pointViewer.AddElement(new PointViewer.AddElementRequest
                        {
                            elementPrefab = SmartTargetVisualizer,
                            target = smartTarget.transform,
                            targetWorldVerticalOffset = 0f,
                            targetWorldRadius = HurtBox.sniperTargetRadius,
                            scaleWithDistance = true
                        });
                        smartTargetToVisualizer.Add(smartTarget, visualizer);
                    }
                }
            }

            private PointViewer pointViewer;
            private HUD hud;
            private Dictionary<SmartTarget, GameObject> smartTargetToVisualizer = new Dictionary<SmartTarget, GameObject>();
            private List<SmartTarget> smartTargets = new List<SmartTarget>();
            private List<KeyValuePair<SmartTarget, GameObject>> _smartTargetToVisualizer = new List<KeyValuePair<SmartTarget, GameObject>>();
            private Dictionary<HurtBox, Vector3> relativeHurtBoxPositionsCache = new Dictionary<HurtBox, Vector3>();
        }

        public class SmartTarget : MonoBehaviour
        {
            public static Dictionary<HurtBox, HashSet<SmartTarget>> hurtBoxSmartTargets = new Dictionary<HurtBox, HashSet<SmartTarget>>();
            public static Stack<SmartTarget> smartTargetPool = new Stack<SmartTarget>();

            public GameObject ownerBodyObject;
            private HurtBox _target;

            public HurtBox target
            {
                get => _target;
                set
                {
                    if (_target != value)
                    {
                        if (_target)
                        {
                            if (hurtBoxSmartTargets.TryGetValue(_target, out HashSet<SmartTarget> smartTargets) && smartTargets.Remove(this) && smartTargets.Count <= 0)
                            {
                                hurtBoxSmartTargets.Remove(_target);
                            }
                        }
                        if (value)
                        {
                            if (!hurtBoxSmartTargets.TryGetValue(value, out HashSet<SmartTarget> smartTargets))
                            {
                                smartTargets = new HashSet<SmartTarget>();
                                hurtBoxSmartTargets.Add(value, smartTargets);
                            }
                            smartTargets.Add(this);
                        }
                        _target = value;
                    }
                }
            }

            public Vector3 position
            {
                get => transform.position;
                set => transform.position = value;
            }

            public void OnDestroy()
            {
                //Return(this);
                target = null;
                ownerBodyObject = null;
            }

            public static SmartTarget Request(HurtBox target, GameObject ownerBodyObject)
            {
                SmartTarget result = null; 
                while (smartTargetPool.Count > 0 && result == null)
                {
                    result = smartTargetPool.Pop();
                }
                result ??= new GameObject("RailgunnerSmartTarget").AddComponent<SmartTarget>();
                //DontDestroyOnLoad(result.gameObject);
                result.transform.parent = target.transform;
                result.target = target;
                result.ownerBodyObject = ownerBodyObject;
                return result;
            }

            public static void Return(SmartTarget smartTarget)
            {
                if (smartTarget != null)
                {
                    smartTarget.transform.parent = null;
                    smartTarget.target = null;
                    smartTargetPool.Push(smartTarget);
                }
            }

            public static IEnumerable<SmartTarget> GetSmartTargets(HurtBoxGroup hurtBoxGroup)
            {
                if (hurtBoxGroup.hurtBoxes == null || hurtBoxGroup.hurtBoxes.Length <= 0)
                {
                    yield break;
                }
                foreach (HurtBox hurtBox in hurtBoxGroup.hurtBoxes)
                {
                    if (hurtBoxSmartTargets.TryGetValue(hurtBox, out HashSet<SmartTarget> smartTargets))
                    {
                        foreach (SmartTarget smartTarget in smartTargets)
                        {
                            yield return smartTarget;
                        }
                    }
                }
            }
        }
    }
}