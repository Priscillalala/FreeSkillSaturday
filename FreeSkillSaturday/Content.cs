using System;
using Ivyl;
using JetBrains.Annotations;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;

namespace FreeItemFriday
{
    public static class Content
    {
        public static class Items
        {
            [CanBeNull] public static ItemDef Theremin;
            [CanBeNull] public static ItemDef Arrowhead;
        }

        public static class Equipment
        {
            [CanBeNull] public static EquipmentDef DeathEye;
            [CanBeNull] public static EquipmentDef DeathEyeConsumed;
        }

        public static class Buffs
        {
            [CanBeNull] public static BuffDef Toxin;
            [CanBeNull] public static BuffDef ToxinSlow;
        }

        public static class Artifacts
        {
            [CanBeNull] public static ArtifactDef SlipperyTerrain;
        }

        public static class Skills
        {
            [CanBeNull] public static SkillDef CrocoPassiveToxin;
            [CanBeNull] public static SkillDef CrocoSuperBite;
        }

        public static class Achievements
        {
            [CanBeNull] public static AchievementWrapper BurnMultipleEnemies;
            [CanBeNull] public static AchievementWrapper ObtainArtifactSlipperyTerrain;
            [CanBeNull] public static AchievementWrapper CompleteMultiplayerUnknownEnding;
            [CanBeNull] public static AchievementWrapper CrocoKillBossCloaked;
            [CanBeNull] public static AchievementWrapper CrocoBeatArenaFast;
        }
    }
}
