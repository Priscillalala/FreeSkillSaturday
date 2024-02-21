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
[BepInPlugin("groovesalad.FreeItemFriday", "FreeItemFriday", "1.7.0")]
public partial class FreeSkillSaturday : BaseContentPlugin<FreeSkillSaturday.LoadStaticContentAsyncArgs, BaseContentPlugin.GetContentPackAsyncArgs, BaseContentPlugin.FinalizeAsyncArgs>
{
    public class LoadStaticContentAsyncArgs : BaseContentPlugin.LoadStaticContentAsyncArgs
    {
        public AssetBundle assets;
        public AsyncOperationHandle<IDictionary<string, ItemDisplayRuleSet>> idrsHandle;
    }

    protected static FreeSkillSaturday instance;

    private AssetBundleCreateRequest freeitemfridayassets;

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

        Entropy.Init(this);
        GodlessEye.Init(this);
        FlintArrowhead.Init(this);
        Theremin.Init(this);
        Disembowel.Init(this);
        PulseGrenade.Init(this);
        Reboot.Init(this);
        Venom.Init(this);
        XQRChip.Init(this);

        Expansion = Content.DefineExpansion();
        loadStaticContentAsync += LoadExpansionIconAsync;
        static IEnumerator<float> LoadExpansionIconAsync(LoadStaticContentAsyncArgs args)
        {
            var texFreeItemFridayExpansionIcon = args.assets.LoadAsync<Sprite>("texFreeItemFridayExpansionIcon");
            while (!texFreeItemFridayExpansionIcon.isDone)
            {
                yield return texFreeItemFridayExpansionIcon.progress;
            }
            Expansion.SetIconSprite(texFreeItemFridayExpansionIcon.asset);
        }
    }

    protected override IEnumerator<float> LoadStaticContentAsync(LoadStaticContentAsyncArgs args) => new GenericLoadingCoroutine
    {
        { new AwaitAssetsCoroutine { freeitemfridayassets }, 0.1f },
        delegate 
        { 
            args.assets = freeitemfridayassets.assetBundle;
            args.idrsHandle = Addressables.LoadResourceLocationsAsync((IEnumerable)new[]
            {
                "ContentPack:RoR2.BaseContent",
                "ContentPack:RoR2.DLC1"
            }, Addressables.MergeMode.Union, typeof(ItemDisplayRuleSet)).ToAssetDictionary<ItemDisplayRuleSet>();
        },
        { () => base.LoadStaticContentAsync(args), 0.8f },
        delegate 
        { 
            Content.AddEntityStatesFromAssembly(typeof(FreeSkillSaturday).Assembly); 
        },
    };

    protected override IEnumerator<float> FinalizeAsync(BaseContentPlugin.FinalizeAsyncArgs args) => new GenericLoadingCoroutine
    {
        { () => base.FinalizeAsync(args), 0.8f },
        { freeitemfridayassets.assetBundle.UpgradeStubbedShadersAsync(), 0.15f },
        delegate 
        { 
            freeitemfridayassets.assetBundle?.Unload(false); 
        },
    };
}
