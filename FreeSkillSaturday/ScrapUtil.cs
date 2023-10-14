using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RoR2;
using UnityEngine;

namespace FreeItemFriday
{
    public static class ScrapUtil
    {
        public static bool CanScrap(ItemDef itemDef)
        {
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
			return (!itemTierDef || itemTierDef.canScrap) && itemDef.canRemove && !itemDef.hidden && itemDef.DoesNotContainTag(ItemTag.Scrap);
		}
    }
}
