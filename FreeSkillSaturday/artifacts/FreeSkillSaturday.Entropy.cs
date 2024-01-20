using FreeItemFriday.Achievements;
using KinematicCharacterController;
using UnityEngine.SceneManagement;
using RoR2.Projectile;
using ThreeEyedGames;

namespace FreeItemFriday;

partial class FreeSkillSaturday
{
    public static class Entropy
    {
        public static bool enabled = true;
        public static float baseAccelerationMultiplier = 0.05f;
        public static float airAccelerationCoefficient = 2f;
        public static float positiveAccelerationCoefficient = 10f;
        public static float horizontalJumpBoostCoefficient = 0.5f;

        public static PhysicMaterial SlidingProjectile { get; private set; }
        public static GameObject SlipperyTerrainFormulaDisplay { get; private set; }

        public static Dictionary<Material, Material> slipperyMaterialInstances;
        public static bool didUpdateSceneVisuals;

        public static void Init()
        {
            const string SECTION = "Artifact of Entropy";
            instance.ArtifactsConfig.Bind(ref enabled, SECTION, string.Format(CONTENT_ENABLED_FORMAT, SECTION));
            instance.ArtifactsConfig.Bind(ref baseAccelerationMultiplier, SECTION, "Base Acceleration Multiplier");
            instance.ArtifactsConfig.Bind(ref airAccelerationCoefficient, SECTION, "Air Acceleration Coefficient");
            instance.ArtifactsConfig.Bind(ref positiveAccelerationCoefficient, SECTION, "Positive Acceleration Coefficient");
            instance.ArtifactsConfig.Bind(ref horizontalJumpBoostCoefficient, SECTION, "Horizontal Jump Boost Coefficient");
            if (enabled)
            {
                instance.loadStaticContentAsync += LoadStaticContentAsync;
                SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            }
        }

        private static IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            ArtifactCode artifactCode = new ArtifactCode(
                (ArtifactCompound.Circle, ArtifactCompound.Circle, ArtifactCompound.Circle),
                (ArtifactCompound.Square, ArtifactCompound.Square, ArtifactCompound.Square),
                (ArtifactCompound.Triangle, ArtifactCompound.Diamond, ArtifactCompound.Triangle));

            yield return instance.Assets.LoadAssetAsync<Sprite>("texArtifactSlipperyTerrainEnabled", out var texArtifactSlipperyTerrainEnabled);
            yield return instance.Assets.LoadAssetAsync<Sprite>("texArtifactSlipperyTerrainDisabled", out var texArtifactSlipperyTerrainDisabled);
            yield return instance.Assets.LoadAssetAsync<GameObject>("PickupSlipperyTerrain", out var PickupSlipperyTerrain);

            Artifacts.SlipperyTerrain = instance.Content.DefineArtifact("SlipperyTerrain")
                .SetIconSprites(texArtifactSlipperyTerrainEnabled.asset, texArtifactSlipperyTerrainDisabled.asset)
                .SetPickupModelPrefab(PickupSlipperyTerrain.asset)
                .SetArtifactCode(artifactCode)
                .SetEnabledActions(OnArtifactEnabled, OnArtifactDisabled);

            yield return instance.Assets.LoadAssetAsync<Sprite>("texObtainArtifactSlipperyTerrainIcon", out var texObtainArtifactSlipperyTerrainIcon);

            Achievements.ObtainArtifactSlipperyTerrain = instance.Content.DefineAchievementForArtifact("ObtainArtifactSlipperyTerrain", Artifacts.SlipperyTerrain)
                .SetIconSprite(texObtainArtifactSlipperyTerrainIcon.asset)
                .SetTrackerTypes(typeof(ObtainArtifactSlipperyTerrainAchievement));
            // Match achievement identifiers from FreeItemFriday
            Achievements.ObtainArtifactSlipperyTerrain.AchievementDef.identifier = "ObtainArtifactSlipperyTerrain";

            if (Artifacts.SlipperyTerrain.pickupModelPrefab.transform.TryFind("mdlSlipperyTerrainArtifact", out Transform mdl) && mdl.TryGetComponent(out MeshRenderer renderer))
            {
                yield return Ivyl.LoadAddressableAssetAsync<Material>("RoR2/Base/artifactworld/matArtifact.mat", out var matArtifact);

                renderer.sharedMaterial = matArtifact.Result;
            }

            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/Engi/EngiBubbleShield.prefab", out var EngiBubbleShield);

            if (EngiBubbleShield.Result.transform.TryFind("Collision/ActiveVisual", out Transform activeVisual))
            {
                activeVisual.gameObject.AddComponent<FreezeRotationWhenArtifactEnabled>();
            }

            yield return instance.Assets.LoadAssetAsync<PhysicMaterial>("physmatSlidingProjectile", out var physmatSlidingProjectile);

            SlidingProjectile = physmatSlidingProjectile.asset;

            yield return CreateSlipperyTerrainFormulaDisplayAsync(artifactCode);
        }

        public static IEnumerator CreateSlipperyTerrainFormulaDisplayAsync(ArtifactCode artifactCode)
        {
            yield return Ivyl.LoadAddressableAssetAsync<GameObject>("RoR2/Base/artifactworld/ArtifactFormulaDisplay.prefab", out var ArtifactFormulaDisplay);

            SlipperyTerrainFormulaDisplay = Ivyl.ClonePrefab(ArtifactFormulaDisplay.Result, "SlipperyTerrainFormulaDisplay");

            yield return Ivyl.SetupArtifactFormulaDisplayAsync(SlipperyTerrainFormulaDisplay.GetComponent<ArtifactFormulaDisplay>(), artifactCode);

            foreach (Decal decal in SlipperyTerrainFormulaDisplay.GetComponentsInChildren<Decal>())
            {
                decal.Fade = 0.15f;
            }
            if (SlipperyTerrainFormulaDisplay.transform.TryFind("Frame", out Transform frame))
            {
                frame.gameObject.SetActive(false);
            }
            if (SlipperyTerrainFormulaDisplay.transform.TryFind("ArtifactFormulaHolderMesh", out Transform mesh))
            {
                mesh.gameObject.SetActive(false);
            }
        }

        private static void SceneManager_activeSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "snowyforest" && SlipperyTerrainFormulaDisplay)
            {
                Instantiate(SlipperyTerrainFormulaDisplay, new Vector3(150, 67, 237), Quaternion.Euler(new Vector3(276, 10, 190))).transform.localScale = Vector3.one * 12f;
            }
            if (didUpdateSceneVisuals = RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(Artifacts.SlipperyTerrain))
            {
                instance.StartCoroutine(UpdateSceneVisuals());
            }
        }
        public static IEnumerator UpdateSceneVisuals()
        {
            yield return new WaitForEndOfFrame();
            if (slipperyMaterialInstances != null)
            {
                foreach (Material materialInstance in slipperyMaterialInstances.Values)
                {
                    Destroy(materialInstance);
                }
                slipperyMaterialInstances.Clear();
            }
            else
            {
                slipperyMaterialInstances = new Dictionary<Material, Material>();
            }

            Shader HGStandard = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGStandard.shader").WaitForCompletion();
            Shader HGSnowTopped = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGSnowTopped.shader").WaitForCompletion();
            Shader HGTriplanarTerrainBlend = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGTriplanarTerrainBlend.shader").WaitForCompletion();

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
                if (!slipperyMaterialInstances.TryGetValue(mat, out Material matInstance))
                {
                    if (mat.shader == HGStandard)
                    {
                        matInstance = new Material(mat);
                        matInstance.SetFloat("_SpecularStrength", 0.6f);
                        matInstance.SetFloat("_SpecularExponent ", 10f);
                    }
                    else if (mat.shader == HGSnowTopped)
                    {
                        matInstance = new Material(mat);
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
                    else if (mat.shader == HGTriplanarTerrainBlend)
                    {
                        matInstance = new Material(mat);
                        matInstance.SetFloat("_GreenChannelSpecularStrength", 0.15f);
                        matInstance.SetFloat("_GreenChannelSpecularExponent", 8f);
                    }
                    if (matInstance)
                    {
                        matInstance.SetInt("_RampInfo", 1);
                        slipperyMaterialInstances.Add(mat, matInstance);
                    }
                }
                if (matInstance)
                {
                    meshRenderers[i].sharedMaterial = matInstance;
                }
            }
            yield break;
        }

        public static void OnArtifactEnabled()
        {
            if (!didUpdateSceneVisuals)
            {
                instance.StartCoroutine(UpdateSceneVisuals());
            }
            On.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity += GenericCharacterMain_ApplyJumpVelocity;
            IL.RoR2.CharacterMotor.PreMove += CharacterMotor_PreMove;
            On.RoR2.Projectile.ProjectileStickOnImpact.UpdateSticking += ProjectileStickOnImpact_UpdateSticking;
            On.RoR2.Projectile.ProjectileStickOnImpact.Awake += ProjectileStickOnImpact_Awake;
        }

        public static void OnArtifactDisabled()
        {
            On.RoR2.Projectile.ProjectileStickOnImpact.Awake -= ProjectileStickOnImpact_Awake;
            On.RoR2.Projectile.ProjectileStickOnImpact.UpdateSticking -= ProjectileStickOnImpact_UpdateSticking;
            IL.RoR2.CharacterMotor.PreMove -= CharacterMotor_PreMove;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity -= GenericCharacterMain_ApplyJumpVelocity;
            On.RoR2.CharacterMotor.OnGroundHit -= CharacterMotor_OnGroundHit;
        }

        private static void ProjectileStickOnImpact_Awake(On.RoR2.Projectile.ProjectileStickOnImpact.orig_Awake orig, ProjectileStickOnImpact self)
        {
            orig(self);
            if (self.TryGetComponent(out Collider collider))
            {
                collider.sharedMaterial = SlidingProjectile;
            }
            if (self.TryGetComponent(out ProjectileSimple projectileSimple))
            {
                projectileSimple.updateAfterFiring = false;
                self.rigidbody.useGravity = true;
            }
        }

        private static void ProjectileStickOnImpact_UpdateSticking(On.RoR2.Projectile.ProjectileStickOnImpact.orig_UpdateSticking orig, ProjectileStickOnImpact self)
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

        private static void CharacterMotor_OnGroundHit(On.RoR2.CharacterMotor.orig_OnGroundHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
            self.isAirControlForced = true;
        }

        private static void GenericCharacterMain_ApplyJumpVelocity(ILContext il)
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
            else instance.Logger.LogError($"{nameof(Entropy)}.{nameof(GenericCharacterMain_ApplyJumpVelocity)} IL hook failed!");
        }

        private static void CharacterMotor_PreMove(ILContext il)
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
            else instance.Logger.LogError($"{nameof(Entropy)}.{nameof(CharacterMotor_PreMove)} IL hook failed!");
        }

        public class FreezeRotationWhenArtifactEnabled : MonoBehaviour
        {
            private Quaternion rotation;

            public void Start()
            {
                rotation = transform.rotation;
                if (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(Artifacts.SlipperyTerrain))
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