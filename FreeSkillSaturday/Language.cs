﻿using System;
using RoR2;
using UnityEngine;
using IvyLibrary;
using static FreeItemFriday.FreeSkillSaturday.Items;
using static FreeItemFriday.FreeSkillSaturday.Equipment;
using static FreeItemFriday.FreeSkillSaturday.Elites;
using static FreeItemFriday.FreeSkillSaturday.Artifacts;
using static FreeItemFriday.FreeSkillSaturday.Achievements;
using static FreeItemFriday.FreeSkillSaturday;
using System.Collections.Generic;

namespace FreeItemFriday;

public static class Language
{
    [LanguageStrings]
    public static Dictionary<string, string> GetEnglish() => new LanguageDictionary()
    {
        { Expansion.nameToken, $"Free Item Friday" },
        { Expansion.descriptionToken, $"Adds content from the 'Free Item Friday' mod to the game." },

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
        { Skills.CrocoPassiveToxin?.skillNameToken, $"Venom" },
        { Skills.CrocoPassiveToxin?.skillDescriptionToken, $"Attacks that apply <style=cIsHealing>Poison</style> apply deadly <style=cIsUtility>Venom</style> instead, slowly <style=cIsUtility>immobilizing victims</style>." },
        { "FSS_KEYWORD_VENOM", $"<style=cKeywordName>Venomous</style><style=cSub>Deal <style=cIsDamage>{Venom.damageCoefficientPerSecond:0%} damage per second</style> and reduce their movement and attack speed by <style=cIsUtility>{Venom.speedReductionPerSecond:0%} per second</style>. <i>2x effectiveness against lightweight enemies.</i></style>" },
        { Skills.CrocoSuperBite?.skillNameToken, $"Disembowel" },
        { Skills.CrocoSuperBite?.skillDescriptionToken, $"<style=cIsHealing>Poisonous</style>. <style=cIsDamage>Slayer</style>. Lacerate an enemy for <style=cIsDamage>3x{Disembowel.damageCoefficient:0%} damage</style>, causing <style=cIsDamage>bleeding</style> and <style=cIsHealth>hemorrhaging</style>." },
        { "FSS_KEYWORD_BLEED", $"<style=cKeywordName>Bleed</style><style=cSub>Deal <style=cIsDamage>320%</style> base damage over 4s. <i>Bleed can stack.</i></style>" },
        { Skills.RailgunnerElectricGrenade?.skillNameToken, $"Pulse Grenade" },
        { Skills.RailgunnerElectricGrenade?.skillDescriptionToken, $"<style=cIsDamage>Shocking</style>. Fire a grenade that explodes for <style=cIsDamage>{PulseGrenade.damageCoefficient:0%} damage</style>." },
        { Skills.RailgunnerPassiveBouncingBullets?.skillNameToken, $"XQR Chip" },
        { Skills.RailgunnerPassiveBouncingBullets?.skillDescriptionToken, $"<style=cIsDamage>Smart Chip</style> intercepts Weak Point display to highlight optimal <style=cIsDamage>ricochet angles</style> instead." },
        { Skills.ToolbotReboot?.skillNameToken, $"Reboot" },
        { Skills.ToolbotReboot?.skillDescriptionToken, $"<style=cIsHealth>Power down</style> for <style=cIsHealth>{Reboot.duration}</style> seconds to <style=cIsUtility>cleanse all debuffs</style>, <style=cIsDamage>reset all your cooldowns</style>, and <style=cIsHealing>restore missing health</style>." },
        #endregion

        #region achievements
        { BurnMultipleEnemies?.NameToken, "Burn to Kill"  },
        { BurnMultipleEnemies?.DescriptionToken, "Ignite 10 enemies simultaneously." },
        { ObtainArtifactSlipperyTerrain?.NameToken, "Trial of Entropy" },
        { ObtainArtifactSlipperyTerrain?.DescriptionToken, "Complete the Trial of Entropy." },
        { CompleteMultiplayerUnknownEnding?.NameToken, "Fly Away Together" },
        { CompleteMultiplayerUnknownEnding?.DescriptionToken, "In multiplayer, obliterate at the Obelisk with a fellow survivor.." },
        { CrocoKillBossCloaked?.NameToken, "Acrid: Ambush" },
        { CrocoKillBossCloaked?.DescriptionToken, "As Acrid, defeat a boss monster while invisible." },
        { CrocoBeatArenaFast?.NameToken, "Acrid: Virulence" },
        { CrocoBeatArenaFast?.DescriptionToken, "As Acrid, clear the Void Fields on Monsoon before monsters reach Lv. 10." },
        { RailgunnerTravelDistance?.NameToken, "Railgunner: Star Trek" },
        { RailgunnerTravelDistance?.DescriptionToken, "As Railgunner, travel 10 miles in a single run." },
        { RailgunnerHipster?.NameToken, "Railgunner: Hipster" },
        { RailgunnerHipster?.DescriptionToken, "As Railgunner, complete the Primordial Teleporter event without scoping in." },
        { RailgunnerEliteSniper?.NameToken, "Railgunner: Supercharged" },
        { RailgunnerEliteSniper?.DescriptionToken, "As Railgunner, beat the game on Eclipse while carrying a Fuel Array." },
        { ToolbotOverclocked?.NameToken, "MUL-T: Overclocked" },
        { ToolbotOverclocked?.DescriptionToken, "As MUL-T, deal damage 100 times in one second." },
        #endregion
    };
}
