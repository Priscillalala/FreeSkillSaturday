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
    public class CrocoBeatArenaFastAchievement : BaseAchievement
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
                ArenaMissionController.onBeatArena += ArenaMissionController_onBeatArena;
            }

            public override void OnUninstall()
            {
                ArenaMissionController.onBeatArena -= ArenaMissionController_onBeatArena;
                base.OnUninstall();
            }

            private void ArenaMissionController_onBeatArena()
            {
                if (!Run.instance || Run.instance.ambientLevel >= 10f)
                {
                    return;
                }
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty);
                if (difficultyDef != null && difficultyDef.countsAsHardMode)
                {
                    Grant();
                }
            }
        }
    }
}

