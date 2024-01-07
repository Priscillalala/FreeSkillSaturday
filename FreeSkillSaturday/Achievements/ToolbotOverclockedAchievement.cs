using RoR2.Achievements;

namespace FreeItemFriday.Achievements;

public class ToolbotOverclockedAchievement : BaseAchievement
{
    public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("ToolbotBody");

    public override void OnBodyRequirementMet()
    {
        base.OnBodyRequirementMet();
        RoR2Application.onFixedUpdate += RoR2Application_onFixedUpdate;
        GlobalEventManager.onClientDamageNotified += GlobalEventManager_onClientDamageNotified;
    }

    public override void OnBodyRequirementBroken()
    {
        RoR2Application.onFixedUpdate -= RoR2Application_onFixedUpdate;
        GlobalEventManager.onClientDamageNotified -= GlobalEventManager_onClientDamageNotified;
        damageCount = 0;
        base.OnBodyRequirementBroken();
    }

    private void RoR2Application_onFixedUpdate()
    {
        if (currentInterval != (currentInterval = (int)(Run.instance?.fixedTime ?? Time.fixedTime)))
        {
            damageCount = 0;
        }
    }

    private void GlobalEventManager_onClientDamageNotified(DamageDealtMessage damageDealt)
    {
        if (damageDealt.attacker == localUser.cachedBodyObject && ++damageCount >= 100)
        {
            Grant();
        }
    }

    private int currentInterval;
    private int damageCount;
}

