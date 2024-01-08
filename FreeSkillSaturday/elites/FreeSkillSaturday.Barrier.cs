using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Items;
using R2API;
using RoR2.UI;
using UnityEngine.UI;

namespace FreeItemFriday.Elites
{
    public class Barrier : FreeSkillSaturday.Behavior
    {
        private static RoR2Asset<HealthBarStyle> combatHealthBar;

        public async void Awake()
        {
            Content.Elites.Barrier = Expansion.DefineElite("Barrier")
                .SetEliteRampTexture(Assets.LoadAsset<Texture2D>("texRampEliteWater"))
                .SetStats(EliteStats.TierOne)
                .DefineSubElite("BarrierHonor", EliteStats.Honor, EliteTiers.Honor);

            EliteTiers.TierOne.AddElite(Content.Elites.Barrier.EliteDef);
        }

        public void OnEnable()
        {
            combatHealthBar = "RoR2/Base/Common/CombatHealthBar.asset";
            On.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
        }

        public void OnDisable()
        {
            combatHealthBar.Dispose();
            On.RoR2.UI.HealthBar.UpdateBarInfos -= HealthBar_UpdateBarInfos;
        }

        private void HealthBar_UpdateBarInfos(On.RoR2.UI.HealthBar.orig_UpdateBarInfos orig, HealthBar self)
        {
            orig(self);
            if (self.source?.body && self.source.body.HasBuff(Content.Elites.Barrier.EliteBuffDef) && self.style == combatHealthBar.Value)
            {
                if (self.barInfoCollection.trailingOverHealthbarInfo.color == combatHealthBar.Value.trailingOverHealthBarStyle.baseColor)
                {
                    self.barInfoCollection.trailingOverHealthbarInfo.color = Color.white;
                }
                self.barInfoCollection.barrierBarInfo.color = new Color32(255, 204, 71, 255);
                self.barInfoCollection.barrierBarInfo.imageType = Image.Type.Tiled;
            }
        }

        public class AffixBarrierBehaviour : BaseBuffBodyBehavior, IOnIncomingDamageServerReceiver, IOnTakeDamageServerReceiver 
        {
            [BuffDefAssociation(useOnServer = true, useOnClient = false)]
            public static BuffDef GetBuffDef() => Content.Elites.Barrier.EliteBuffDef;

            private bool barrierWasActive;
            private float lastRecordedBarrier;

            public void OnEnable()
            {
                body.healthComponent.AddIncomingDamageReceiver(this);
                body.healthComponent.AddTakeDamageReceiver(this);
            }

            public void OnDisable()
            {
                body.healthComponent.RemoveIncomingDamageReceiver(this);
                body.healthComponent.RemoveTakeDamageReceiver(this);
            }

            public void OnIncomingDamageServer(DamageInfo damageInfo)
            {
                lastRecordedBarrier = body.healthComponent.barrier;
            }

            public void OnTakeDamageServer(DamageReport damageReport)
            {
                float barrier = damageReport.damageDealt - lastRecordedBarrier;
                if (barrier > 0f)
                {
                    body.healthComponent.AddBarrier(barrier);
                }
            }

            public void LateUpdate()
            {
                if (barrierWasActive != (barrierWasActive = body.healthComponent ? body.healthComponent.barrier > 0f : false))
                {
                    body.UpdateAllTemporaryVisualEffects();
                }
            }
        }
    }
}