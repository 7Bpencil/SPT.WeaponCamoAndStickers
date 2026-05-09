//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Diz.Skinning;
using Diz.Jobs;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;

namespace SevenBoldPencil.ChangeEquipmentColor
{
	// this method is used everywhere to clone items:
	// - hideout shooting range
	// - raid loading screen
	// - raid exit screen
	// - profile overview screen
	public class Patch_GClass3380_smethod_2 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = null;
			Type[] generics = [typeof(Item)];
            return AccessTools.Method(typeof(GClass3380), nameof(GClass3380.smethod_2), parameters, generics);
        }

        [PatchPostfix]
        public static void Postfix(GClass3380 __instance, ref Item __result, Item originalItem, IIdGenerator idGenerator = null, bool skipInvisibleContent = false, bool resetSpawnedInSession = false)
		{
			Plugin.Instance.OnCloneItem(originalItem.Id, __result.Id);
		}
	}

	public class Patch_GClass928_GetItemHash : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass928), nameof(GClass928.GetItemHash));
        }

        [PatchPostfix]
        public static void Postfix(Item item, ref int __result)
		{
			if (Plugin.Instance.GetDecalInfo(item.Id).Some(out var decalInfo))
			{
				// all this shit to fit SaveTime inside int
				var saveTime = decalInfo.SaveTime;
				var newStartPoint = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var newStartPointOffset = new DateTimeOffset(newStartPoint).ToUnixTimeMilliseconds();
				var saveTimeOffset = saveTime - newStartPointOffset;
				var saveTimeOffsetSeconds = (int)(saveTimeOffset / 1000);
				__result ^= saveTimeOffsetSeconds;
			}
		}
	}
}
