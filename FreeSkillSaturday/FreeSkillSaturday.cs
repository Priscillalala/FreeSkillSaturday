global using System;
global using System.Collections;
global using System.Collections.Generic;
global using UnityEngine;
global using UnityEngine.AddressableAssets;
global using UnityEngine.ResourceManagement.AsyncOperations;
global using BepInEx;
global using IvyLibrary;
global using R2API;
global using RoR2;
global using RoR2.ContentManagement;
using System.IO;
using System.Security;
using System.Security.Permissions;
using RoR2.ExpansionManagement;

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
public partial class FreeSkillSaturday : BaseContentPlugin<FreeSkillSaturday>
{
    public static ExpansionDef Expansion;

    private AssetBundleCreateRequest freeitemfridayassets;
    protected IDictionary<string, ItemDisplayRuleSet> itemDisplayRuleSets;

    public AssetBundle Assets { get; private set; }

    public void Awake()
    {
        freeitemfridayassets = this.LoadAssetBundleAsync("freeitemfridayassets");
        loadStaticContentAsync += LoadStaticContentAsync;
        finalizeAsync += FinalizeAsync;
    }

    private new IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
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

        IEnumerable keys = new[] { "ContentPack:RoR2.BaseContent", "ContentPack:RoR2.DLC1" };
        yield return Ivyl.LoadAddressableAssetsAsync<ItemDisplayRuleSet>(keys, out var idrs);
        itemDisplayRuleSets = idrs.Result;
    }

    private new IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        freeitemfridayassets.assetBundle?.Unload(false);
        yield break;
    }
}
