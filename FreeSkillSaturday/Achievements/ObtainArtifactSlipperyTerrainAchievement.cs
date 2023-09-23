using System;
using Unity;
using UnityEngine;
using RoR2;
using System.Collections;
using HG;
using RoR2.Achievements;
using RoR2.Achievements.Artifacts;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace FreeItemFriday.Achievements
{
    public class ObtainArtifactSlipperyTerrainAchievement : BaseObtainArtifactAchievement
    {
        public override ArtifactDef artifactDef => Content.Artifacts.SlipperyTerrain;
    }
}

