using FreeItemFriday;

namespace EntityStates.Railgunner.Weapon;

public class FireElectricGrenade : GenericProjectileBaseState, IBaseWeaponState
{
    public FireElectricGrenade() : base()
    {
        effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab").WaitForCompletion();
        projectilePrefab = FreeSkillSaturday.PulseGrenade.GrenadeProjectile;
        damageCoefficient = FreeSkillSaturday.PulseGrenade.damageCoefficient;
        force = 600f;
        minSpread = 0f;
        maxSpread = 0f;
        baseDuration = FreeSkillSaturday.PulseGrenade.duration;
        recoilAmplitude = 1f;
        attackSoundString = "Play_MULT_m1_grenade_launcher_shoot";
        projectilePitchBonus = -5f;
        baseDelayBeforeFiringProjectile = FreeSkillSaturday.PulseGrenade.firingDelay;
        targetMuzzle = "MuzzlePistol";
        bloom = 1f;
    }

    public override void PlayAnimation(float duration)
    {
        base.PlayAnimation(duration);
        Util.PlayAttackSpeedSound("Play_MULT_R_variant_end", gameObject, attackSpeedStat);
        PlayAnimation("Gesture, Override", "FirePistol", "FirePistol.playbackRate", delayBeforeFiringProjectile);
        //PlayAnimation("Gesture, Override", "WindupSuper", "Super.playbackRate", delayBeforeFiringProjectile);
    }

    public override void DoFireEffects()
    {
        base.DoFireEffects();
        if (isAuthority)
        {
            characterMotor?.ApplyForce(-1000f * GetAimRay().direction, false, false);
        }
        PlayAnimation("Gesture, Override", "FireSniper", "FireSniper.playbackRate", duration - fixedAge);
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;

    public bool CanScope() => true;
}