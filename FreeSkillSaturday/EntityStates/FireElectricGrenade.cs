using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using R2API;
using RoR2.Skills;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HG;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.Networking;

namespace EntityStates.Railgunner.Weapon
{
    public class FireElectricGrenade : GenericProjectileBaseState, IBaseWeaponState
    {
        public FireElectricGrenade() : base()
        {
            using RoR2Asset<GameObject> _muzzleflashSmokeRing = "RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab";

            effectPrefab = _muzzleflashSmokeRing.Value;
            projectilePrefab = FreeItemFriday.Skills.ElectricGrenadeSkill.GrenadeProjectile;
            damageCoefficient = FreeItemFriday.Skills.ElectricGrenadeSkill.damageCoefficient;
            force = 600f;
            minSpread = 0f;
            maxSpread = 0f;
            baseDuration = FreeItemFriday.Skills.ElectricGrenadeSkill.duration;
            recoilAmplitude = 1f;
            attackSoundString = "Play_MULT_m1_grenade_launcher_shoot";
            projectilePitchBonus = -5f;
            baseDelayBeforeFiringProjectile = FreeItemFriday.Skills.ElectricGrenadeSkill.firingDelay;
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
}