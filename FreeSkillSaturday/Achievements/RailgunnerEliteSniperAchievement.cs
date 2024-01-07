using RoR2.Achievements;
using System.Linq;

namespace FreeItemFriday.Achievements;

public class RailgunnerEliteSniperAchievement : BaseEndingAchievement
{
    public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RailgunnerBody");

    public override bool ShouldGrant(RunReport runReport)
    {
        if (runReport == null || !runReport.gameEnding || !runReport.gameEnding.isWin || runReport.ruleBook.FindDifficulty() < DifficultyIndex.Eclipse1)
        {
            return false;
        }
        RunReport.PlayerInfo playerInfo = runReport.FindPlayerInfo(localUser);
        return playerInfo != null && playerInfo.equipment != null && playerInfo.equipment.Any(x => x == RoR2Content.Equipment.QuestVolatileBattery.equipmentIndex);
    }
}

