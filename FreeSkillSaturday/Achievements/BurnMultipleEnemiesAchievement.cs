using RoR2.Achievements;
using UnityEngine.Networking;

namespace FreeItemFriday.Achievements;

public class BurnMultipleEnemiesAchievement : BaseAchievement
{
    public static bool IsBurnDot(DotController.DotIndex index) => index == DotController.DotIndex.Burn || index == DotController.DotIndex.PercentBurn || index == DotController.DotIndex.Helfire || index == DotController.DotIndex.StrongerBurn;
    public override void OnInstall()
    {
        base.OnInstall();
        base.SetServerTracked(true);
    }
    public override void OnUninstall()
    {
        base.SetServerTracked(false);
        base.OnUninstall();
    }
    public class ServerAchievement : BaseServerAchievement
    {
        private List<DotController> affectedDotControllers;
        public override void OnInstall()
        {
            base.OnInstall();
            affectedDotControllers = new List<DotController>();
            DotController.onDotInflictedServerGlobal += DotController_onDotInflictedServerGlobal;
        }

        private void DotController_onDotInflictedServerGlobal(DotController dotController, ref InflictDotInfo inflictDotInfo)
        {
            GameObject currentBodyObject = GetCurrentBody()?.gameObject;
            if (!currentBodyObject) return;
            if (IsBurnDot(inflictDotInfo.dotIndex) && currentBodyObject == inflictDotInfo.attackerObject && currentBodyObject != inflictDotInfo.victimObject)
            {
                if (!affectedDotControllers.Contains(dotController)) affectedDotControllers.Add(dotController);

                HashSet<NetworkInstanceId> affectedBodyObjects = new HashSet<NetworkInstanceId>();
                int burningEnemiesCount = 0;
                for (int i = affectedDotControllers.Count - 1; i >= 0; i--)
                {
                    DotController otherDotController = affectedDotControllers[i];
                    if (!otherDotController)
                    {
                        affectedDotControllers.RemoveAt(i);
                        continue;
                    }

                    if (affectedBodyObjects.Contains(otherDotController.victimObjectId)) continue;

                    affectedBodyObjects.Add(otherDotController.victimObjectId);

                    if (IsDotControllerBurning(otherDotController, currentBodyObject))
                    {
                        burningEnemiesCount++;
                    }
                    else
                    {
                        affectedDotControllers.RemoveAt(i);
                    }
                }
                if (burningEnemiesCount >= 10)
                {
                    base.Grant();
                }
            }
        }
        public bool IsDotControllerBurning(DotController dotController, GameObject currentBodyObject)
        {
            for (int i = 0; i < dotController.dotStackList.Count; i++)
            {
                DotController.DotStack dotStack = dotController.dotStackList[i];
                if (IsBurnDot(dotStack.dotIndex) && dotStack.attackerObject == currentBodyObject)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnUninstall()
        {
            DotController.onDotInflictedServerGlobal -= DotController_onDotInflictedServerGlobal;
            affectedDotControllers = null;
            base.OnUninstall();
        }
    }
}

