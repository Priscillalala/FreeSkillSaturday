using System;
using Unity;
using UnityEngine;
using RoR2;
using RoR2.Items;
using R2API;
using System.Collections;
using HG;
using RoR2.Achievements;
using System.Collections.Generic;
using UnityEngine.Networking;
using FreeItemFriday.Achievements;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using KinematicCharacterController;
using UnityEngine.SceneManagement;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Ivyl;
using ThreeEyedGames;
using System.Threading.Tasks;

namespace FreeItemFriday.Artifacts
{
    public class Entropy : FreeSkillSaturday.Behavior
    {
        public static float baseAccelerationMultiplier = 0.05f;
        public static float airAccelerationCoefficient = 2f;
        public static float positiveAccelerationCoefficient = 10f;
        public static float horizontalJumpBoostCoefficient = 0.5f;

        public PhysicMaterial physmatSlidingProjectile = Assets.LoadAsset<PhysicMaterial>("physmatSlidingProjectile");
        public GameObject slipperyTerrainFormulaDisplay;

        public Dictionary<Material, Material> SlipperyMaterialInstances { get; private set; }
        public bool DidUpdateSceneVisuals { get; private set; }

        public async void Awake()
        {
            using RoR2Asset<Material> _matArtifact = "RoR2/Base/artifactworld/matArtifact.mat";
            using RoR2Asset<GameObject> _engiBubbleShield = "RoR2/Base/Engi/EngiBubbleShield.prefab";

            ArtifactCode artifactCode = new ArtifactCode(
                (ArtifactCompound.Circle, ArtifactCompound.Circle, ArtifactCompound.Circle),
                (ArtifactCompound.Square, ArtifactCompound.Square, ArtifactCompound.Square),
                (ArtifactCompound.Triangle, ArtifactCompound.Diamond, ArtifactCompound.Triangle));

            using Task<GameObject> _slipperyTerrainFormulaDisplay = CreateSlipperyTerrainFormulaDisplayAsync(artifactCode);

            Content.Artifacts.SlipperyTerrain = Expansion.DefineArtifact("SlipperyTerrain")
                .SetIconSprites(Assets.LoadAsset<Sprite>("texArtifactSlipperyTerrainEnabled"), Assets.LoadAsset<Sprite>("texArtifactSlipperyTerrainDisabled"))
                .SetPickupModelPrefab(Assets.LoadAsset<GameObject>("PickupSlipperyTerrain"))
                .SetArtifactCode(artifactCode)
                .SetEnabledActions(OnArtifactEnabled, OnArtifactDisabled);

            Content.Achievements.ObtainArtifactSlipperyTerrain = Expansion.DefineAchievementForArtifact("ObtainArtifactSlipperyTerrain", Content.Artifacts.SlipperyTerrain)
                .SetIconSprite(Assets.LoadAsset<Sprite>("texObtainArtifactSlipperyTerrainIcon"))
                .SetTrackerTypes(typeof(ObtainArtifactSlipperyTerrainAchievement));
            // Match achievement identifiers from FreeItemFriday
            Content.Achievements.ObtainArtifactSlipperyTerrain.AchievementDef.identifier = "ObtainArtifactSlipperyTerrain";

            if (Content.Artifacts.SlipperyTerrain.pickupModelPrefab.transform.TryFind("mdlSlipperyTerrainArtifact", out Transform mdl) && mdl.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.sharedMaterial = await _matArtifact;
            }

            GameObject engiBubbleShield = await _engiBubbleShield;
            if (engiBubbleShield.transform.TryFind("Collision/ActiveVisual", out Transform activeVisual))
            {
                activeVisual.gameObject.AddComponent<FreezeRotationWhenArtifactEnabled>();
            }

            slipperyTerrainFormulaDisplay = await _slipperyTerrainFormulaDisplay;
        }

        public static async Task<GameObject> CreateSlipperyTerrainFormulaDisplayAsync(ArtifactCode artifactCode)
        {
            using RoR2Asset<GameObject> _artifactFormulaDisplay = "RoR2/Base/artifactworld/ArtifactFormulaDisplay.prefab";

            GameObject slipperyTerrainFormulaDisplay = Prefabs.ClonePrefab(await _artifactFormulaDisplay, "SlipperyTerrainFormulaDisplay");
            await artifactCode.CopyToFormulaDisplayAsync(slipperyTerrainFormulaDisplay.GetComponent<ArtifactFormulaDisplay>());
            foreach (Decal decal in slipperyTerrainFormulaDisplay.GetComponentsInChildren<Decal>())
            {
                decal.Fade = 0.15f;
            }
            if (slipperyTerrainFormulaDisplay.transform.TryFind("Frame", out Transform frame))
            {
                frame.gameObject.SetActive(false);
            }
            if (slipperyTerrainFormulaDisplay.transform.TryFind("ArtifactFormulaHolderMesh", out Transform mesh))
            {
                mesh.gameObject.SetActive(false);
            }
            return slipperyTerrainFormulaDisplay;
        }
        
        public void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "snowyforest" && slipperyTerrainFormulaDisplay)
            {
                Instantiate(slipperyTerrainFormulaDisplay, new Vector3(150, 67, 237), Quaternion.Euler(new Vector3(276, 10, 190))).transform.localScale = Vector3.one * 12f;
            }
            if (DidUpdateSceneVisuals = RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Content.Artifacts.SlipperyTerrain))
            {
                StartCoroutine(nameof(UpdateSceneVisuals));
            }
        }
        public IEnumerator UpdateSceneVisuals()
        {
            yield return new WaitForEndOfFrame();
            if (SlipperyMaterialInstances != null)
            {
                foreach (Material materialInstance in SlipperyMaterialInstances.Values)
                {
                    Destroy(materialInstance);
                }
                SlipperyMaterialInstances.Clear();
            }
            else
            {
                SlipperyMaterialInstances = new Dictionary<Material, Material>();
            }

            using RoR2Asset<Shader> _HGStandard = "RoR2/Base/Shaders/HGStandard.shader";
            using RoR2Asset<Shader> _HGSnowTopped = "RoR2/Base/Shaders/HGSnowTopped.shader";
            using RoR2Asset<Shader> _HGTriplanarTerrainBlend = "RoR2/Base/Shaders/HGTriplanarTerrainBlend.shader";

            MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                if (meshRenderer.gameObject.layer != LayerIndex.world.intVal || !meshRenderer.gameObject.activeInHierarchy || meshRenderer.GetComponent<Decal>())
                {
                    continue;
                }
                Material mat = meshRenderer.sharedMaterial;
                if (!mat)
                {
                    continue;
                }
                if (!SlipperyMaterialInstances.TryGetValue(mat, out Material matInstance))
                {
                    if (mat.shader == _HGStandard.Value)
                    {
                        matInstance = Instantiate(mat);
                        matInstance.SetFloat("_SpecularStrength", 0.6f);
                        matInstance.SetFloat("_SpecularExponent ", 10f);
                    }
                    else if (mat.shader == _HGSnowTopped.Value)
                    {
                        matInstance = Instantiate(mat);
                        if (matInstance.GetTexture("_SnowNormalTex"))
                        {
                            matInstance.SetFloat("_SpecularStrength", 0.1f);
                            matInstance.SetFloat("_SpecularExponent", 20f);
                            matInstance.SetFloat("_SnowSpecularStrength", 0.4f);
                            matInstance.SetFloat("_SnowSpecularExponent", 8f);
                        }
                        else
                        {
                            matInstance.SetFloat("_SpecularStrength", 0.4f);
                            matInstance.SetFloat("_SpecularExponent", 3f);
                        }
                    }
                    else if (mat.shader == _HGTriplanarTerrainBlend.Value)
                    {
                        matInstance = Instantiate(mat);
                        matInstance.SetFloat("_GreenChannelSpecularStrength", 0.15f);
                        matInstance.SetFloat("_GreenChannelSpecularExponent", 8f);
                    }
                    if (matInstance)
                    {
                        matInstance.SetInt("_RampInfo", 1);
                        SlipperyMaterialInstances.Add(mat, matInstance);
                    }
                }
                if (matInstance)
                {
                    meshRenderers[i].sharedMaterial = matInstance;
                }
            }
            yield break;
        }

        public void OnArtifactEnabled()
        {
            if (!DidUpdateSceneVisuals)
            {
                StartCoroutine(nameof(UpdateSceneVisuals));
            }
            On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity += GenericCharacterMain_ApplyJumpVelocity;
            IL.RoR2.CharacterMotor.PreMove += CharacterMotor_PreMove;
            On.RoR2.Projectile.ProjectileStickOnImpact.UpdateSticking += ProjectileStickOnImpact_UpdateSticking;
            On.RoR2.Projectile.ProjectileStickOnImpact.Awake += ProjectileStickOnImpact_Awake;
        }

        public void OnArtifactDisabled()
        {
            On.RoR2.Projectile.ProjectileStickOnImpact.Awake -= ProjectileStickOnImpact_Awake;
            On.RoR2.Projectile.ProjectileStickOnImpact.UpdateSticking -= ProjectileStickOnImpact_UpdateSticking;
            IL.RoR2.CharacterMotor.PreMove -= CharacterMotor_PreMove;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity -= GenericCharacterMain_ApplyJumpVelocity;
            On.RoR2.CharacterMotor.OnGroundHit -= CharacterMotor_OnGroundHit;
        }

        private void ProjectileStickOnImpact_Awake(On.RoR2.Projectile.ProjectileStickOnImpact.orig_Awake orig, ProjectileStickOnImpact self)
        {
            orig(self);
            if (self.TryGetComponent(out Collider collider))
            {
                collider.sharedMaterial = physmatSlidingProjectile;
            }
            if (self.TryGetComponent(out ProjectileSimple projectileSimple))
            {
                projectileSimple.updateAfterFiring = false;
                self.rigidbody.useGravity = true;
            }
        }

        private void ProjectileStickOnImpact_UpdateSticking(On.RoR2.Projectile.ProjectileStickOnImpact.orig_UpdateSticking orig, ProjectileStickOnImpact self)
        {
            if (self.hitHurtboxIndex == -2 && !self.GetComponent<ProjectileGrappleController>())
            {
                if (self.rigidbody.isKinematic)
                {
                    self.rigidbody.isKinematic = false;
                    self.rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
                return;
            }
            orig(self);
        }

        private void CharacterMotor_OnGroundHit(On.RoR2.CharacterMotor.orig_OnGroundHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
            self.isAirControlForced = true;
        }

        private void GenericCharacterMain_ApplyJumpVelocity(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            bool ilFound = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<KinematicCharacterMotor>(nameof(KinematicCharacterMotor.ForceUnground))
                ) && c.TryGotoPrev(MoveType.Before,
                x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.velocity))
                );
            if (ilFound)
            {
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldarg, 2);
                c.EmitDelegate<Func<Vector3, CharacterMotor, float, Vector3>>((newVelocity, motor, horizontalMultiplier) =>
                {
                    float adjustedHorizontalMultiplier = ((horizontalMultiplier - 1) / horizontalMultiplier) * horizontalJumpBoostCoefficient;
                    return new Vector3(motor.velocity.x + newVelocity.x * adjustedHorizontalMultiplier, newVelocity.y, motor.velocity.z + newVelocity.z * adjustedHorizontalMultiplier);
                });
            }
            else Logger.LogError($"{nameof(Entropy)}.{nameof(GenericCharacterMain_ApplyJumpVelocity)} IL hook failed!");
        }

        private void CharacterMotor_PreMove(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int locTargetIndex = -1;
            int locAcclerationIndex = -1;
            bool ilFound = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt(typeof(Vector3).GetMethod(nameof(Vector3.MoveTowards))),
                x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.velocity))
                ) && c.TryGotoPrev(MoveType.Before, 
                x => x.MatchLdfld<CharacterMotor>(nameof(CharacterMotor.velocity)),
                x => x.MatchLdloc(out locTargetIndex),
                x => x.MatchLdloc(out locAcclerationIndex)
                );
            if (ilFound)
            {
                c.Emit(OpCodes.Ldloc, locTargetIndex);
                c.Emit(OpCodes.Ldloc, locAcclerationIndex);
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<Vector3, float, CharacterMotor, float>>((target, acceleration, motor) =>
                {
                    float multiplier = baseAccelerationMultiplier;
                    if (target.sqrMagnitude > motor.velocity.sqrMagnitude)
                    {
                        multiplier *= positiveAccelerationCoefficient;
                    }
                    else if (!motor.isGrounded)
                    {
                        multiplier *= airAccelerationCoefficient;
                    }
                    if (!motor.body || motor.body.moveSpeed * motor.body.moveSpeed >= motor.velocity.sqrMagnitude)
                    {
                        acceleration *= multiplier;
                    }
                    else
                    {
                        acceleration *= 1 - ((1 - multiplier) * Mathf.Sqrt(motor.body.moveSpeed / motor.velocity.magnitude));
                    }
                    return acceleration;
                });
                c.Emit(OpCodes.Stloc, locAcclerationIndex);
            }
            else Logger.LogError($"{nameof(Entropy)}.{nameof(CharacterMotor_PreMove)} IL hook failed!");
        }

        public class FreezeRotationWhenArtifactEnabled : MonoBehaviour
        {
            private Quaternion rotation;

            public void Start()
            {
                rotation = transform.rotation;
                if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(Content.Artifacts.SlipperyTerrain))
                {
                    enabled = false;
                }
            }

            public void Update()
            {
                transform.rotation = rotation;
            }
        }
    }
}