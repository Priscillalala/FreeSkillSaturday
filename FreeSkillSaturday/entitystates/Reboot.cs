using RoR2.ConVar;
using System.Linq;
using RoR2.PostProcessing;
using UnityEngine.Networking;
using RoR2.HudOverlay;
using RoR2.UI;
using FreeItemFriday;

namespace EntityStates.Toolbot;

public class Reboot : BaseCharacterMain
{
    const float entryDuration = 0.4f;
    const int stepCount = 3;

    private float duration;
    private GameObject stunVfxInstance;
    private OverlayController overlayController;
    private List<ImageFillController> fillUiList = new List<ImageFillController>();
    private Run.FixedTimeStamp startTime;
    private bool didChangeMusic;
    private bool didPauseAnimations;
    private bool didRestoreAnimations;
    private bool didCleanseBody;
    private bool didResetSkills;

    public override void OnEnter()
    {
        base.OnEnter();
        duration = (FreeSkillSaturday.Reboot.duration / attackSpeedStat) + entryDuration;
        stunVfxInstance = UnityEngine.Object.Instantiate(StunState.stunVfxPrefab, transform);
        stunVfxInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
        startTime = Run.FixedTimeStamp.now;
        if (isAuthority)
        {
            foreach (EntityStateMachine entityStateMachine in gameObject.GetComponents<EntityStateMachine>())
            {
                if (entityStateMachine.state != this && entityStateMachine.customName != "Stance")
                {
                    entityStateMachine.SetState(EntityStateCatalog.InstantiateState(entityStateMachine.mainStateType));
                    entityStateMachine.nextState = null;
                }
            }
        }
        PlayCrossfade("Body", "BoxModeEnter", 0.1f);
        PlayCrossfade("Stance, Override", "PutAwayGun", 0.1f);
        modelAnimator?.SetFloat("aimWeight", 0f);
        characterAnimParamAvailability.turnAngle = false;
        characterAnimParamAvailability.rightSpeed = false;
        characterBody.hideCrosshair = true;
        characterBody.isSprinting = false;
        overlayController = HudOverlayManager.AddOverlay(gameObject, new OverlayCreationParams 
        {
            prefab = FreeSkillSaturday.Reboot.RebootOverlay,
            childLocatorEntry = "CrosshairExtras",
        });
        overlayController.onInstanceAdded += OnOverlayInstanceAdded;
        overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
        /*if (NetworkServer.active)
        {
            characterBody.AddBuff(RoR2Content.Buffs.SmallArmorBoost);
        }*/
        
        Util.PlaySound("Play_engi_R_turret_death", gameObject);
        Util.PlaySound("Play_env_hiddenLab_laptop_active_loop", gameObject);
        On.RoR2.PostProcessing.ScreenDamage.OnRenderImage += ScreenDamage_OnRenderImage;
        if (didChangeMusic = LocalUserManager.readOnlyLocalUsersList.Any(x => x.cachedBodyObject == gameObject))
        {
            AkSoundEngine.SetRTPCValue(AudioManager.cvVolumeMsx.rtpcName, 0f);
            On.RoR2.AudioManager.VolumeConVar.SetString += VolumeConVar_SetString;
            On.RoR2.AudioManager.VolumeConVar.GetString += VolumeConVar_GetString;
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!didPauseAnimations && modelAnimator && fixedAge >= entryDuration)
        {
            modelAnimator.speed = 0f;
            didPauseAnimations = true;
        }
        foreach (ImageFillController imageFillController in fillUiList)
        {
            imageFillController.SetTValue(fixedAge / duration);
        }
        if (HasStepPassed(1))
        {
            CleanseBodyStep();
        }
        if (HasStepPassed(2))
        {
            ResetSkillsStep();
        }
        if (isAuthority && fixedAge >= duration)
        {
            outer.SetNextStateToMain();
        }
    }

    public override void OnExit()
    {
        if (stunVfxInstance)
        {
            Destroy(stunVfxInstance);
        }
        PlayAnimation("Body", "BoxModeExit");
        PlayCrossfade("Stance, Override", "Empty", 0.1f);
        modelAnimator?.SetFloat("aimWeight", 1f);
        if (overlayController != null)
        {
            overlayController.onInstanceAdded -= OnOverlayInstanceAdded;
            overlayController.onInstanceRemove -= OnOverlayInstanceRemoved;
            fillUiList.Clear();
            HudOverlayManager.RemoveOverlay(overlayController);
        }
        characterBody.hideCrosshair = false;
        CleanseBodyStep();
        ResetSkillsStep();
        if (NetworkServer.active)
        {
            if (healthComponent.alive)
            {
                healthComponent.Networkhealth = healthComponent.fullHealth;
                healthComponent.ForceShieldRegen();
            }
            //characterBody.RemoveBuff(RoR2Content.Buffs.SmallArmorBoost);
            //characterBody.AddTimedBuff(RoR2Content.Buffs.SmallArmorBoost, 0.2f);
        }
        RestoreAnimator();
        Util.PlaySound("Stop_drone_active_loop", gameObject);
        Util.PlaySound("Stop_env_hiddenLab_laptop_active_loop", gameObject);
        Util.PlaySound("Play_MULT_shift_end", gameObject);
        Util.PlaySound("Play_captain_R_aim", gameObject);
        Util.PlaySound("Play_captain_R_turret_build", gameObject);
        On.RoR2.PostProcessing.ScreenDamage.OnRenderImage -= ScreenDamage_OnRenderImage;
        if (didChangeMusic)
        {
            On.RoR2.AudioManager.VolumeConVar.SetString -= VolumeConVar_SetString;
            On.RoR2.AudioManager.VolumeConVar.GetString -= VolumeConVar_GetString;
            AudioManager.cvVolumeMsx.SetString(AudioManager.cvVolumeMsx.fallbackString);
        }
        base.OnExit();
    }

    public bool HasStepPassed(int step)
    {
        return fixedAge - entryDuration >= duration * step / stepCount;
    }

    public void CleanseBodyStep()
    {
        if (didCleanseBody)
        {
            return;
        }
        EffectManager.SpawnEffect(FreeSkillSaturday.Reboot.VentEffect, new EffectData
        {
            rotation = Quaternion.identity,
            origin = characterBody.corePosition,
        }, false);
        Util.PlaySound("Play_drone_active_loop", gameObject);
        if (NetworkServer.active)
        {
            Util.CleanseBody(characterBody, true, false, true, true, true, false);
        }
        didCleanseBody = true;
    }

    public void ResetSkillsStep()
    {
        RestoreAnimator();
        if (didResetSkills)
        {
            return;
        }
        Util.PlaySound("Play_MULT_R_variant_end", gameObject);
        if (skillLocator)
        {
            foreach (GenericSkill skill in skillLocator.allSkills)
            {
                if (skill != skillLocator.utility)
                {
                    skill.Reset();
                }
            }
        }
        if (NetworkServer.active && characterBody.inventory)
        {
            for (uint i = 0; i < characterBody.inventory.GetEquipmentSlotCount(); i++)
            {
                EquipmentState equipmentState = characterBody.inventory.GetEquipment(i);
                if (equipmentState.equipmentIndex != EquipmentIndex.None && equipmentState.charges <= 0)
                {
                    characterBody.inventory.SetEquipment(new EquipmentState(equipmentState.equipmentIndex, Run.FixedTimeStamp.now, equipmentState.charges), i);
                }
            }
        }
        didResetSkills = true;
    }

    public void RestoreAnimator()
    {
        if (didPauseAnimations && !didRestoreAnimations && modelAnimator)
        {
            modelAnimator.speed = 1f;
            didRestoreAnimations = true;
        }
    }

    public void OnOverlayInstanceAdded(OverlayController controller, GameObject instance)
    {
        fillUiList.Add(instance.GetComponent<ImageFillController>());
    }

    public void OnOverlayInstanceRemoved(OverlayController controller, GameObject instance)
    {
        fillUiList.Remove(instance.GetComponent<ImageFillController>());
    }

    private void ScreenDamage_OnRenderImage(On.RoR2.PostProcessing.ScreenDamage.orig_OnRenderImage orig, ScreenDamage self, RenderTexture source, RenderTexture destination)
    {
        if (self.cameraRigController?.target == gameObject)
        {
            if (healthComponent && healthComponent.lastHitTime > startTime) 
            {
                Run.FixedTimeStamp lastHitTime = healthComponent.lastHitTime;
                healthComponent.lastHitTime = Run.FixedTimeStamp.negativeInfinity;
                try
                {
                    orig(self, source, destination);
                }
                finally
                {
                    healthComponent.lastHitTime = lastHitTime;
                }
                return;
            }
        }
        orig(self, source, destination);
    }

    private void VolumeConVar_SetString(On.RoR2.AudioManager.VolumeConVar.orig_SetString orig, BaseConVar self, string newValue)
    {
        orig(self, newValue);
        if (self == AudioManager.cvVolumeMsx)
        {
            AkSoundEngine.SetRTPCValue(AudioManager.cvVolumeMsx.rtpcName, 0f);
        }
    }

    private string VolumeConVar_GetString(On.RoR2.AudioManager.VolumeConVar.orig_GetString orig, BaseConVar self)
    {
        if (self == AudioManager.cvVolumeMsx)
        {
            return AudioManager.cvVolumeMsx.fallbackString;
        }
        return orig(self);
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
}