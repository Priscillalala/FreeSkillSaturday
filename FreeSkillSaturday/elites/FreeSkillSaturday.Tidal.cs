using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Items;
using R2API;

namespace FreeItemFriday.Elites
{
    public class Water : FreeSkillSaturday.Behavior
    {
        public async void Awake()
        {
            Content.Elites.Water = Expansion.DefineElite("Water")
                .SetEliteRampTexture(Assets.LoadAsset<Texture2D>("texRampEliteWater"))
                .SetStats(EliteStats.TierOne)
                .DefineSubElite("WaterHonor", EliteStats.Honor, EliteTiers.Honor);

            EliteTiers.TierOne.AddElite(Content.Elites.Water.EliteDef);

            Overlays.RegisterConditionalOverlay(Assets.LoadAsset<Material>("matEliteWaterOverlay"), x => x.myEliteIndex == Content.Elites.Water.EliteDef.eliteIndex);
        }
        
        public class AffixWaterBehaviour : BaseBuffBodyBehavior, IOnDamageDealtServerReceiver
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = true)]
            public static BuffDef GetBuffDef() => Content.Elites.Water.EliteBuffDef;

            public void OnDamageDealtServer(DamageReport damageReport)
            {
                
            }
        }
    }
}