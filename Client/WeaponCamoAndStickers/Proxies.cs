//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Diz.Skinning;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	// I know that harmony can pass instance fields with three underscores,
	// but these proxies are more general solution and can be used everywhere

	public struct WeaponPreview_Proxy(WeaponPreview instance)
	{
        private readonly WeaponPreview __instance = instance;

		private static TypedFieldInfo<WeaponPreview, GameObject> __gameObject_0 = new("gameObject_0");
		private static TypedFieldInfo<WeaponPreview, Item> __item_0 = new("item_0");

		public GameObject gameObject_0 { get { return __gameObject_0.Get(__instance); } set { __gameObject_0.Set(__instance, value); } }
		public Item item_0 { get { return __item_0.Get(__instance); } set { __item_0.Set(__instance, value); } }
	}

	public struct WeaponPrefab_Proxy(WeaponPrefab instance)
	{
        private readonly WeaponPrefab __instance = instance;

		private static TypedFieldInfo<WeaponPrefab, Weapon> __weapon_0 = new("weapon_0");

		public Weapon weapon_0 { get { return __weapon_0.Get(__instance); } set { __weapon_0.Set(__instance, value); } }
	}

	public struct LoddedSkin_Proxy(LoddedSkin instance)
	{
        private readonly LoddedSkin __instance = instance;

		private static TypedFieldInfo<LoddedSkin, AbstractSkin[]> __lods = new("_lods");

		public AbstractSkin[] _lods { get { return __lods.Get(__instance); } set { __lods.Set(__instance, value); } }
	}

	public struct ItemInfoInteractionsAbstractClass_Proxy<T>(ItemInfoInteractionsAbstractClass<T> instance) where T : struct, Enum
	{
        private readonly ItemInfoInteractionsAbstractClass<T> __instance = instance;

		private static TypedFieldInfo<ItemInfoInteractionsAbstractClass<T>, Dictionary<string, DynamicInteractionClass>> __Dictionary_0 = new("Dictionary_0");

		public Dictionary<string, DynamicInteractionClass> Dictionary_0 { get { return __Dictionary_0.Get(__instance); } set { __Dictionary_0.Set(__instance, value); } }
	}

	public struct InteractionButtonsContainer_Proxy(InteractionButtonsContainer instance)
	{
        private readonly InteractionButtonsContainer __instance = instance;

		private static TypedFieldInfo<InteractionButtonsContainer, SimpleContextMenuButton> __buttonTemplate = new("_buttonTemplate");
		private static TypedFieldInfo<InteractionButtonsContainer, RectTransform> __buttonsContainer = new("_buttonsContainer");

		public SimpleContextMenuButton _buttonTemplate { get { return __buttonTemplate.Get(__instance); } set { __buttonTemplate.Set(__instance, value); } }
		public RectTransform _buttonsContainer { get { return __buttonsContainer.Get(__instance); } set { __buttonsContainer.Set(__instance, value); } }
	}
}
