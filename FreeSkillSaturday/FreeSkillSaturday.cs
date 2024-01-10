global using System;
global using System.Collections;
global using System.Collections.Generic;
global using UnityEngine;
global using UnityEngine.Networking;
global using UnityEngine.AddressableAssets;
global using UnityEngine.ResourceManagement.AsyncOperations;
global using BepInEx;
global using MonoMod.Cil;
global using Mono.Cecil.Cil;
global using IvyLibrary;
global using R2API;
global using RoR2;
global using RoR2.ContentManagement;

using ShaderSwapper;
using System.Security;
using System.Security.Permissions;
using BepInEx.Configuration;

[module: UnverifiableCode]
#pragma warning disable
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace FreeItemFriday;

[BepInDependency(RecalculateStatsAPI.PluginGUID)]
[BepInDependency(ColorsAPI.PluginGUID)]
[BepInDependency(DamageAPI.PluginGUID)]
[BepInDependency(DotAPI.PluginGUID)]
[BepInPlugin("groovesalad.FreeItemFriday", "FreeItemFriday", "2.0.0")]
public partial class FreeSkillSaturday : BaseContentPlugin
{
    protected static FreeSkillSaturday instance;

    private AssetBundleCreateRequest freeitemfridayassets;
    protected IDictionary<string, ItemDisplayRuleSet> itemDisplayRuleSets;

    public AssetBundle Assets { get; private set; }
    public ConfigFile ArtifactsConfig { get; private set; }
    public ConfigFile EquipmentConfig { get; private set; }
    public ConfigFile ItemsConfig { get; private set; }
    public ConfigFile SkillsConfig { get; private set; }

    protected const string CONTENT_ENABLED_FORMAT = "Enable {0}?";

    public void Awake()
    {
        instance = this;

        freeitemfridayassets = this.LoadAssetBundleAsync("freeitemfridayassets");

        ArtifactsConfig = this.CreateConfigFile(System.IO.Path.ChangeExtension(Config.ConfigFilePath, ".Artifacts.cfg"), false);
        EquipmentConfig = this.CreateConfigFile(System.IO.Path.ChangeExtension(Config.ConfigFilePath, ".Equipment.cfg"), false);
        ItemsConfig = this.CreateConfigFile(System.IO.Path.ChangeExtension(Config.ConfigFilePath, ".Items.cfg"), false);
        SkillsConfig = this.CreateConfigFile(System.IO.Path.ChangeExtension(Config.ConfigFilePath, ".Skills.cfg"), false);

        Entropy.Init();
        GodlessEye.Init();
        FlintArrowhead.Init();
        Theremin.Init();
        Disembowel.Init();
        PulseGrenade.Init();
        Reboot.Init();
        Venom.Init();
        XQRChip.Init();
    }

    protected override IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        if (!freeitemfridayassets.isDone)
        {
            yield return freeitemfridayassets;
        }
        Assets = freeitemfridayassets.assetBundle;

        yield return Assets.LoadAssetAsync<Sprite>("texFreeItemFridayExpansionIcon", out var texFreeItemFridayExpansionIcon);
        Expansion = Content.DefineExpansion()
            .SetIconSprite(texFreeItemFridayExpansionIcon.asset);

        Content.AddEntityStatesFromAssembly(typeof(FreeSkillSaturday).Assembly);

        yield return Ivyl.LoadAddressableAssetsAsync<ItemDisplayRuleSet>(new[] { "ContentPack:RoR2.BaseContent", "ContentPack:RoR2.DLC1" }, out var idrs);
        itemDisplayRuleSets = idrs.Result;

        yield return base.LoadStaticContentAsync(args);
    }

    protected override IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        yield return base.FinalizeAsync(args);

        yield return freeitemfridayassets.assetBundle?.UpgradeStubbedShadersAsync();

        freeitemfridayassets.assetBundle?.Unload(false);
    }
}
