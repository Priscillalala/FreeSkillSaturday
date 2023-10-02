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

namespace FreeItemFriday.Achievements
{
    public class RailgunnerTravelDistanceAchievement : BaseAchievement
    {
        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RailgunnerBody");

        public override void OnInstall()
        {
            base.OnInstall();
            listeningToggle = new ToggleAction(SetListeningTrue, SetListeningFalse);
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
            localUser.onMasterChanged += OnMasterChanged;
            OnMasterChanged();
            UserProfile userProfile = base.userProfile;
            userProfile.onStatsReceived = (Action)Delegate.Combine(userProfile.onStatsReceived, OnStatsReceived);
        }

        private void SetListeningFalse()
        {
            UserProfile userProfile = base.userProfile;
            userProfile.onStatsReceived = (Action)Delegate.Remove(userProfile.onStatsReceived, OnStatsReceived);
            playerStatsComponent = null;
            localUser.onMasterChanged -= OnMasterChanged;
        }

        private void OnMasterChanged()
        {
            playerStatsComponent = localUser.cachedMasterController?.GetComponent<PlayerStatsComponent>();
        }

        private void OnStatsReceived()
        {
            Check();
        }

        private void Check()
        {
            if (playerStatsComponent)
            {
                Debug.Log("Distance Traveled: " + playerStatsComponent.currentStats.GetStatValueDouble(StatDef.totalDistanceTraveled));
            }
            if (playerStatsComponent != null && playerStatsComponent.currentStats.GetStatValueDouble(StatDef.totalDistanceTraveled) >= requirement)
            {
                Grant();
            }
        }

        private static readonly double requirement = 10.0 * HGUnitConversions.milesToMeters;
        private PlayerStatsComponent playerStatsComponent;
        private ToggleAction listeningToggle;
    }
}

