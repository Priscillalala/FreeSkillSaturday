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

    public void Awake()
    {
        instance = this;

        freeitemfridayassets = this.LoadAssetBundleAsync("freeitemfridayassets");

        loadStaticContentAsync += LoadStaticContentAsync;
        finalizeAsync += FinalizeAsync;

        GameObject freeSkillSaturday = new GameObject("FreeSkillSaturday", new[]
        {
            typeof(Entropy),
            typeof(GodlessEye),
            typeof(FlintArrowhead),
            typeof(Theremin),
            typeof(Disembowel),
            typeof(PulseGrenade),
            typeof(Reboot),
            typeof(Venom),
            typeof(XQRChip),
        });
        DontDestroyOnLoad(freeSkillSaturday);
    }

    private IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
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
    }

    private IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        yield return freeitemfridayassets.assetBundle?.UpgradeStubbedShadersAsync();

        freeitemfridayassets.assetBundle?.Unload(false);
    }
}
