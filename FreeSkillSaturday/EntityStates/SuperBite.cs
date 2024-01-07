using FreeItemFriday;

namespace EntityStates.Croco;

public class SuperBite : BasicMeleeAttack
{
    public static AnimationCurve _forwardVelocityCurve;

    public SuperBite() : base()
    {
        baseDuration = 1.2f;
        damageCoefficient = FreeSkillSaturday.Disembowel.damageCoefficient;
        hitBoxGroupName = "Slash";
        hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/OmniImpactVFXSlashSyringe.prefab").WaitForCompletion();
        procCoefficient = 1f;
        hitPauseDuration = 0.1f;
        mecanimHitboxActiveParameter = "Bite.hitBoxActive";
        shorthopVelocityFromHit = 6f;
        beginStateSoundString = "Play_acrid_m2_bite_shoot";
        impactSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Croco/nseAcridM1Hit.asset").WaitForCompletion();
        forceForwardVelocity = true;
        forwardVelocityCurve = (_forwardVelocityCurve ??= new AnimationCurve(new Keyframe(0.17f, 0f), new Keyframe(0.21f, 0.55f), new Keyframe(0.3f, 0f)));
        swingEffectMuzzleString = "Slash3";
        swingEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoComboFinisherSlash.prefab").WaitForCompletion();
    }

    public override bool allowExitFire => false;// characterBody && !characterBody.isSprinting;

    public const int attackResetCount = 2;
    public const float baseAttackResetDelay = 0.1f;
    public const float baseDurationBeforeInterruptable = 0.55f;
    public const float bloom = 0.25f;
    public const float recoilAmplitude = -1f;

    private float attackResetDelay;
    private float attackResetTimer;
    private float durationBeforeInterruptable;
    private float currentResetAttackCount;
    private GameObject secondSwingEffectInstance;
    //private GameObject slash2EffectInstance;

    public override void OnEnter()
    {
        base.OnEnter();
        attackResetDelay = baseAttackResetDelay / attackSpeedStat;
        durationBeforeInterruptable = baseDurationBeforeInterruptable / attackSpeedStat;
        characterDirection.forward = GetAimRay().direction;
    }

    public override void OnExit()
    {
        if (secondSwingEffectInstance)
        {
            Destroy(secondSwingEffectInstance);
        }
        /*if (slash2EffectInstance)
        {
            Destroy(slash2EffectInstance);
        }*/
        base.OnExit();
    }

    public override void PlayAnimation()
    {
        base.PlayAnimation();
        PlayCrossfade("Gesture, Override", "Bite", "Bite.playbackRate", duration, 0.05f);
        PlayCrossfade("Gesture, Additive", "Slash3", "Slash.playbackRate", duration, 0.05f);
        Util.PlaySound("Play_acrid_m1_bigSlash", base.gameObject);
    }

    public override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
    {
        base.AuthorityModifyOverlapAttack(overlapAttack);
        overlapAttack.damageType |= DamageType.BonusToLowHealth | DamageType.BleedOnHit;
    }

    public override void OnMeleeHitAuthority()
    {
        characterBody.AddSpreadBloom(bloom);
        base.OnMeleeHitAuthority();
    }

    public override void AuthorityFixedUpdate()
    {
        if (authorityHitThisFixedUpdate && authorityInHitPause)
        {
            if (secondSwingEffectInstance && secondSwingEffectInstance.TryGetComponent(out ScaleParticleSystemDuration scaleParticleSystemDuration))
            {
                scaleParticleSystemDuration.newDuration = 20f;
            }
            /*if (slash2EffectInstance && slash2EffectInstance.TryGetComponent(out scaleParticleSystemDuration))
            {
                scaleParticleSystemDuration.newDuration = 20f;
            }*/
        }
        base.AuthorityFixedUpdate();
        if (authorityHasFiredAtAll)
        {
            attackResetTimer += Time.fixedDeltaTime;
            if ((attackResetTimer >= attackResetDelay || forceFire) && currentResetAttackCount < attackResetCount)
            {
                attackResetTimer -= attackResetDelay;
                ResetOverlapAttack();
            }
        }
    }

    public void ResetOverlapAttack()
    {
        currentResetAttackCount++;
        if (currentResetAttackCount >= 2)
        {
            overlapAttack.impactSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Croco/nseAcridBiteHit.asset").WaitForCompletion().index;
            overlapAttack.AddModdedDamageType(FreeSkillSaturday.Disembowel.SuperBleedOnHit);
            CrocoDamageTypeController crocoDamageTypeController = base.GetComponent<CrocoDamageTypeController>();
            if (crocoDamageTypeController)
            {
                overlapAttack.damageType |= crocoDamageTypeController.GetDamageType();
            }
        }
        overlapAttack.ResetIgnoredHealthComponents();
    }

    public override void BeginMeleeAttackEffect()
    {
        bool meleeAttackHasBegun = this.meleeAttackHasBegun;
        if (!meleeAttackHasBegun)
        {
            //GameObject crocoSlashEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoComboFinisherSlash.prefab").WaitForCompletion();
            Transform transform = base.FindModelChild("MouthMuzzle");
            if (transform && !secondSwingEffectInstance)
            {
                secondSwingEffectInstance = UnityEngine.Object.Instantiate(FreeSkillSaturday.Disembowel.CrocoSuperBiteEffect, transform);
                secondSwingEffectInstance.transform.forward = characterDirection.forward;
                if (secondSwingEffectInstance.TryGetComponent(out ScaleParticleSystemDuration scaleParticleSystemDuration))
                {
                    scaleParticleSystemDuration.newDuration = scaleParticleSystemDuration.initialDuration;
                }
            }
            /*Transform slash2Transform = base.FindModelChild("Slash2");
            if (slash2Transform && !slash2EffectInstance)
            {
                slash2EffectInstance = UnityEngine.Object.Instantiate(crocoSlashEffect, slash2Transform);
                if (slash2EffectInstance.TryGetComponent(out ScaleParticleSystemDuration scaleParticleSystemDuration))
                {
                    scaleParticleSystemDuration.newDuration = scaleParticleSystemDuration.initialDuration;
                }
            }*/
        }
        AddRecoil(0.9f * recoilAmplitude, 1.1f * recoilAmplitude, -0.1f * recoilAmplitude, 0.1f * recoilAmplitude);
        base.BeginMeleeAttackEffect();
        /*if (!meleeAttackHasBegun && this.meleeAttackHasBegun && swingEffectInstance)
        {
            swingEffectInstance.transform.forward = characterDirection.forward;
            swingEffectInstance.transform.localScale *= 1.2f;
        } */
    }

    public override void AuthorityExitHitPause()
    {
        base.AuthorityExitHitPause();
        if (secondSwingEffectInstance && secondSwingEffectInstance.TryGetComponent(out ScaleParticleSystemDuration scaleParticleSystemDuration))
        {
            scaleParticleSystemDuration.newDuration = scaleParticleSystemDuration.initialDuration;
        }
        /*if (slash2EffectInstance && slash2EffectInstance.TryGetComponent(out scaleParticleSystemDuration))
        {
            scaleParticleSystemDuration.newDuration = scaleParticleSystemDuration.initialDuration;
        }*/
    }

    public override InterruptPriority GetMinimumInterruptPriority() => fixedAge >= durationBeforeInterruptable ? InterruptPriority.Skill : InterruptPriority.Pain;
}