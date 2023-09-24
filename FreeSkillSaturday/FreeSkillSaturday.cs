﻿using BepInEx;
using BepInEx.Logging;
using Ivyl;
using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace FreeItemFriday
{
    [BepInPlugin("com.groovesalad.FreeItemFriday", "FreeItemFriday", "1.3.0")]
    [BepInConfig("FreeItemFriday", BepInConfig.ComponentGroupingFlags.ByComponent | BepInConfig.ComponentGroupingFlags.ByNamespace)]
    public class FreeSkillSaturday : BaseUnityPlugin<FreeSkillSaturday>
    {
        public AssetBundle assets;
        public ExpansionPackage expansion;

        public void Awake()
        {
            assets = this.LoadAssetBundle("freeitemfridayassets", true);
            expansion = new ExpansionPackage("groovesalad.FreeItemFriday", "FSS")
                .SetIconSprite(assets.LoadAsset<Sprite>("texFreeItemFridayExpansionIcon"));
            expansion.AddEntityStatesFromAssembly(typeof(FreeSkillSaturday).Assembly);
        }

        [PluginComponent(typeof(FreeSkillSaturday), PluginComponent.Flags.ConfigComponent | PluginComponent.Flags.ConfigStaticFields)]
        public abstract class Behavior : MonoBehaviour
        {
            public static AssetBundle Assets => Instance.assets;
            public static ExpansionPackage Expansion => Instance.expansion;
            public static ManualLogSource Logger => Instance.Logger;
        }
    }
}
