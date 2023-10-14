using BepInEx;
using Ivyl;
using System;
using UnityEngine;
using RoR2;
using RoR2.Skills;
using HG;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.Networking;
using FreeItemFriday.Achievements;
using RoR2.Projectile;
using R2API;
using UnityEngine.UI;
using RoR2.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using JetBrains.Annotations;

namespace FreeItemFriday.Skills
{
    public class FieldRepairs : FreeSkillSaturday.Behavior
    {
        public async void Awake()
        {
            using RoR2Asset<SkillFamily> _toolbotBodySecondaryFamily = "RoR2/Base/Toolbot/ToolbotBodySecondaryFamily.asset";

            Content.Skills.ToolbotRepair = Expansion.DefineSkill<RequiresScrapSkillDef>("ToolbotRepair")
                .SetIconSprite(Assets.LoadAsset<Sprite>("texRailgunnerElectricGrenadeIcon"))
                .SetActivationState(typeof(EntityStates.Railgunner.Weapon.FireElectricGrenade), "Weapon")
                .SetInterruptPriority(EntityStates.InterruptPriority.Skill)
                .SetCooldown(6f)
                .SetFlags(SkillFlags.Agile | SkillFlags.MustKeyPress);

            /*Content.Achievements.RailgunnerHipster = Expansion.DefineAchievementForSkill("RailgunnerHipster", Content.Skills.RailgunnerElectricGrenade)
                .SetIconSprite(Content.Skills.RailgunnerElectricGrenade.icon)
                .SetTrackerTypes(typeof(RailgunnerHipsterAchievement), null);*/

            SkillFamily toolbotBodySecondaryFamily = await _toolbotBodySecondaryFamily;
            toolbotBodySecondaryFamily.AddSkill(Content.Skills.ToolbotRepair, null);
        }

        public class RequiresScrapSkillDef : SkillDef
        {
            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                InstanceData instanceData = new InstanceData
                {
                    skillSlot = skillSlot,
                };
                instanceData.available = instanceData.RecalculateAvailable();
                skillSlot.characterBody.onInventoryChanged += instanceData.OnInventoryChanged;
                return instanceData;
            }

            public override void OnUnassigned([NotNull] GenericSkill skillSlot)
            {
                skillSlot.characterBody.onInventoryChanged -= (skillSlot.skillInstanceData as InstanceData).OnInventoryChanged;
            }

            public override bool IsReady([NotNull] GenericSkill skillSlot)
            {
                return base.IsReady(skillSlot) && (skillSlot.skillInstanceData as InstanceData).available;
            }
            public override bool CanExecute([NotNull] GenericSkill skillSlot)
            {
                return base.CanExecute(skillSlot) && (skillSlot.skillInstanceData as InstanceData).available;
            }

            public class InstanceData : BaseSkillInstanceData
            {
                public void OnInventoryChanged()
                {
                    available = RecalculateAvailable();
                }

                public bool RecalculateAvailable()
                {
                    List<ItemIndex> itemAcquisitionOrder = skillSlot?.characterBody?.inventory?.itemAcquisitionOrder;
                    if (itemAcquisitionOrder == null)
                    {
                        return false;
                    }
                    for (int i = 0; i < itemAcquisitionOrder.Count; i++)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemAcquisitionOrder[i]);
                        if (itemDef && ScrapUtil.CanScrap(itemDef))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public GenericSkill skillSlot;
                public bool available;
            }
        }
    }
}