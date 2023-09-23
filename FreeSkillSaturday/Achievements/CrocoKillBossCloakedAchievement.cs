using System;
using Unity;
using UnityEngine;
using RoR2;
using System.Collections;
using HG;
using RoR2.Achievements;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace FreeItemFriday.Achievements
{
    public class CrocoKillBossCloakedAchievement : BaseAchievement
    {
        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("CrocoBody");

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            SetServerTracked(true);
        }

        public override void OnBodyRequirementBroken()
        {
            SetServerTracked(false);
            base.OnBodyRequirementBroken();
        }

        public class ServerAchievement : BaseServerAchievement
        {
            public override void OnInstall()
            {
                base.OnInstall();
                GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            }

            public override void OnUninstall()
            {
                GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
                base.OnUninstall();
            }

            private void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
            {
                if (damageReport.victimIsChampion && networkUser.master == damageReport.attackerMaster)
                {
                    CharacterBody currentBody = GetCurrentBody();
                    if (currentBody.GetVisibilityLevel(damageReport.victimTeamIndex) <= VisibilityLevel.Cloaked)
                    {
                        Grant();
                    }
                }
            }
        }
    }
}

