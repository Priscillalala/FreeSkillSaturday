using System;
using Unity;
using UnityEngine;
using RoR2;
using System.Collections;
using HG;
using RoR2.Achievements;
using System.Collections.Generic;
using UnityEngine.Networking;
using RoR2.Stats;
using Ivyl;

namespace FreeItemFriday.Achievements
{
    public class RailgunnerHipsterAchievement : BaseAchievement
    {
        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RailgunnerBody");

        public override void OnInstall()
        {
            base.OnInstall();
            iscLunarTeleporter = "RoR2/Base/Teleporters/iscLunarTeleporter.asset";
            listeningToggle = new ToggleAction(SetListeningTrue, SetListeningFalse);
        }

        public override void OnUninstall()
        {
            iscLunarTeleporter.Dispose();
            listeningToggle.SetActive(false);
            base.OnUninstall();
        }

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            listeningToggle.SetActive(true);
        }

        public override void OnBodyRequirementBroken()
        {
            listeningToggle.SetActive(false);
            base.OnBodyRequirementBroken();
        }

        private void SetListeningTrue()
        {
            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterChargedGlobal += TeleporterInteraction_onTeleporterChargedGlobal;
            On.EntityStates.Railgunner.Scope.BaseActive.OnEnter += BaseActive_OnEnter;
        }

        private void SetListeningFalse()
        {
            On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterChargedGlobal -= TeleporterInteraction_onTeleporterChargedGlobal;
            On.EntityStates.Railgunner.Scope.BaseActive.OnEnter -= BaseActive_OnEnter;
        }

        private void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            teleporterValid = sceneDirector.teleporterSpawnCard && sceneDirector.teleporterSpawnCard == iscLunarTeleporter.Value;
            orig(self, sceneDirector, teleporter);
        }

        private void TeleporterInteraction_onTeleporterBeginChargingGlobal(TeleporterInteraction obj)
        {
            hasScoped = false;
        }

        private void TeleporterInteraction_onTeleporterChargedGlobal(TeleporterInteraction obj)
        {
            if (localUser.cachedBody?.healthComponent && localUser.cachedBody.healthComponent.alive && !hasScoped && teleporterValid)
            {
                Grant();
            }
        }

        private void BaseActive_OnEnter(On.EntityStates.Railgunner.Scope.BaseActive.orig_OnEnter orig, EntityStates.Railgunner.Scope.BaseActive self)
        {
            orig(self);
            if (self.gameObject && self.gameObject == localUser.cachedBodyObject)
            {
                hasScoped = true;
            }
        }

        private RoR2Asset<InteractableSpawnCard> iscLunarTeleporter;
        private ToggleAction listeningToggle;
        private bool teleporterValid;
        private bool hasScoped;
    }
}

