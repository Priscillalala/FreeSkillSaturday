using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RoR2;
using UnityEngine;

namespace FreeItemFriday;

public static class TeleporterUtil
{
    public static readonly Dictionary<SceneIndex, Vector3> explicitTeleporterLocations = new Dictionary<SceneIndex, Vector3>();

    public static bool TryLocateTeleporter(out Vector3 location)
    {
        if (TeleporterInteraction.instance)
        {
            location = TeleporterInteraction.instance.transform.position;
            return true;
        }
        return explicitTeleporterLocations.TryGetValue(SceneCatalog.mostRecentSceneDef.sceneDefIndex, out location);
    }

    [SystemInitializer(typeof(SceneCatalog))]
    public static void Init()
    {
        explicitTeleporterLocations[SceneCatalog.FindSceneIndex("moon2")] = new Vector3(1108.127f, -282.101f, 1183.366f);
        explicitTeleporterLocations[SceneCatalog.FindSceneIndex("moon")] = new Vector3(2656.971f, -206.239f, 721.6917f);
    }
}
