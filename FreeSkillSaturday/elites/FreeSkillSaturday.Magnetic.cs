using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Items;
using R2API;

namespace FreeItemFriday.Elites
{
    public class Magnetic : FreeSkillSaturday.Behavior
    {
        public async void Awake()
        {
            Content.Elites.Magnetic = Expansion.DefineElite("Magnetic")
                .SetEliteRampTexture(Assets.LoadAsset<Texture2D>("texRampEliteMagnetic"))
                .SetStats(EliteStats.TierOne)
                .DefineSubElite("MagneticHonor", EliteStats.Honor, EliteTiers.Honor);

            EliteTiers.TierOne.AddElite(Content.Elites.Magnetic.EliteDef);
        }
        
        public class AffixMagneticBehaviour : BaseBuffBodyBehavior, IOnDamageDealtServerReceiver
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = true)]
            public static BuffDef GetBuffDef() => Content.Elites.Magnetic.EliteBuffDef;

            public void OnDamageDealtServer(DamageReport damageReport)
            {
                
            }
        }
    }
}