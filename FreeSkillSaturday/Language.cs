using System;
using RoR2;
using UnityEngine;
using Ivyl;
using static FreeItemFriday.Content.Items;
using static FreeItemFriday.Content.Equipment;
using static FreeItemFriday.Content.Elites;
using static FreeItemFriday.Content.Artifacts;
using static FreeItemFriday.Content.Skills;
using static FreeItemFriday.Content.Achievements;
using System.Collections.Generic;

namespace FreeItemFriday.Language
{
    public static class Language
    {
        [LanguageStrings]
        public static Dictionary<string, string> GetEnglish() => new LanguageDictionary()
        {
            { FreeSkillSaturday.Instance.expansion.GetNameToken(), $"Free Item Friday" },
            { FreeSkillSaturday.Instance.expansion.GetDescriptionToken(), $"Adds content from the 'Free Item Friday' mod to the game." },

            #region items
            { Theremin?.nameToken, $"Theremin" },
            { Theremin?.pickupToken, $"Increase attack speed near the Teleporter." },
            { Theremin?.descriptionToken, $"Increase <style=cIsDamage>attack speed</style> by up to <style=cIsDamage>{Items.Theremin.attackSpeedBonus:0%} <style=cStack>(+{Items.Theremin.attackSpeedBonusPerStack:0%} per stack)</style></style> the closer you are to a Teleporter." },
            { Arrowhead?.nameToken, $"Flint Arrowhead" },
            { Arrowhead?.pickupToken, $"Burn enemies for flat damage on hit." },
            { Arrowhead?.descriptionToken, $"<style=cIsDamage>100%</style> chance to <style=cIsDamage>burn</style> on hit for <style=cIsDamage>{Items.FlintArrowhead.damage} <style=cStack>(+{Items.FlintArrowhead.damagePerStack} per stack)</style></style> damage." },
            #endregion

            #region equipment
            { DeathEye?.nameToken, $"Godless Eye" },
            { DeathEye?.pickupToken, $"Obliterate all nearby enemies from existence, then yourself. Consumed on use." },
            { DeathEye?.descriptionToken, $"Obliterate enemies within <style=cIsUtility>{Equipment.GodlessEye.range}m</style> from existence. Then, <style=cIsHealth>obliterate yourself from existence</style>. Equipment is <style=cIsUtility>consumed</style> on use." },
            { DeathEyeConsumed?.nameToken, $"Godless Eye (Consumed)" },
            { DeathEyeConsumed?.pickupToken, $"Still shocking to the touch. Does nothing." },
            { DeathEyeConsumed?.descriptionToken, $"Still shocking to the touch. Does nothing." },
            #endregion

            #region elites
            { Magnetic?.GetModifierToken(), "Magnetic {0}" },
            { Water?.GetModifierToken(), "Tidal {0}" },
            { Barrier?.GetModifierToken(), "Crystalline {0}" },
            #endregion

            #region artifacts
            { SlipperyTerrain?.nameToken, $"Artifact of Entropy" },
            { SlipperyTerrain?.descriptionToken, $"Terrain is smooth and frictionless." },
            #endregion

            #region skills
            { CrocoPassiveToxin?.skillNameToken, $"Venom" },
            { CrocoPassiveToxin?.skillDescriptionToken, $"Attacks that apply <style=cIsHealing>Poison</style> apply deadly <style=cIsUtility>Venom</style> instead, slowly <style=cIsUtility>immobilizing victims</style>." },
            { "FSS_KEYWORD_VENOM", $"<style=cKeywordName>Venomous</style><style=cSub>Deal <style=cIsDamage>{Skills.Venom.damageCoefficientPerSecond:0%} damage per second</style> and reduce their movement and attack speed by <style=cIsUtility>{Skills.Venom.speedReductionPerSecond:0%} per second</style>. <i>2x effectiveness against lightweight enemies.</i></style>" },
            { CrocoSuperBite?.skillNameToken, $"Disembowel" },
            { CrocoSuperBite?.skillDescriptionToken, $"<style=cIsHealing>Poisonous</style>. <style=cIsDamage>Slayer</style>. Lacerate an enemy for <style=cIsDamage>3x{Skills.Disembowel.damageCoefficient:0%} damage</style>, causing <style=cIsDamage>bleeding</style> and <style=cIsHealth>hemorrhaging</style>." },
            { "FSS_KEYWORD_BLEED", $"<style=cKeywordName>Bleed</style><style=cSub>Deal <style=cIsDamage>320%</style> base damage over 4s. <i>Bleed can stack.</i></style>" },
            { RailgunnerElectricGrenade?.skillNameToken, $"Pulse Grenade" },
            { RailgunnerElectricGrenade?.skillDescriptionToken, $"<style=cIsDamage>Shocking</style>. Fire a grenade that explodes for <style=cIsDamage>{Skills.PulseGrenade.damageCoefficient:0%} damage</style>." },
            { RailgunnerPassiveBouncingBullets?.skillNameToken, $"XQR Chip" },
            { RailgunnerPassiveBouncingBullets?.skillDescriptionToken, $"<style=cIsDamage>Smart Chip</style> intercepts Weak Point display to highlight optimal <style=cIsDamage>ricochet angles</style> instead." },
            { ToolbotReboot?.skillNameToken, $"Reboot" },
            { ToolbotReboot?.skillDescriptionToken, $"<style=cIsHealth>Power down</style> for <style=cIsHealth>{Skills.Reboot.duration}</style> seconds to <style=cIsUtility>cleanse all debuffs</style>, <style=cIsDamage>reset your cooldowns</style>, and <style=cIsHealing>restore missing health</style>." },
            #endregion

            #region achievements
            { BurnMultipleEnemies?.GetNameToken(), "Burn to Kill"  },
            { BurnMultipleEnemies?.GetDescriptionToken(), "Ignite 10 enemies simultaneously." },
            { ObtainArtifactSlipperyTerrain?.GetNameToken(), "Trial of Entropy" },
            { ObtainArtifactSlipperyTerrain?.GetDescriptionToken(), "Complete the Trial of Entropy." },
            { CompleteMultiplayerUnknownEnding?.GetNameToken(), "Fly Away Together" },
            { CompleteMultiplayerUnknownEnding?.GetDescriptionToken(), "In multiplayer, obliterate at the Obelisk with a fellow survivor.." },
            { CrocoKillBossCloaked?.GetNameToken(), "Acrid: Ambush" },
            { CrocoKillBossCloaked?.GetDescriptionToken(), "As Acrid, defeat a boss monster while invisible." },
            { CrocoBeatArenaFast?.GetNameToken(), "Acrid: Virulence" },
            { CrocoBeatArenaFast?.GetDescriptionToken(), "As Acrid, clear the Void Fields on Monsoon before monsters reach Lv. 10." },
            { RailgunnerTravelDistance?.GetNameToken(), "Railgunner: Star Trek" },
            { RailgunnerTravelDistance?.GetDescriptionToken(), "As Railgunner, travel 10 miles in a single run." },
            { RailgunnerHipster?.GetNameToken(), "Railgunner: Hipster" },
            { RailgunnerHipster?.GetDescriptionToken(), "As Railgunner, complete the Primordial Teleporter event without scoping in." },
            { RailgunnerEliteSniper?.GetNameToken(), "Railgunner: Supercharged" },
            { RailgunnerEliteSniper?.GetDescriptionToken(), "As Railgunner, beat the game on Eclipse while carrying a Fuel Array." },
            { ToolbotOverclocked?.GetNameToken(), "MUL-T: Overclocked" },
            { ToolbotOverclocked?.GetDescriptionToken(), "As MUL-T, deal damage 100 times in one second." },
            #endregion
        };
    }
}
