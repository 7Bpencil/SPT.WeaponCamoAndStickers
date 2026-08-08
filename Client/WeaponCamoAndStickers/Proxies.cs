//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Diz.Skinning;
using EFT;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SevenBoldPencil.WeaponCamoAndStickers
{
	// I know that harmony can pass instance fields with three underscores,
	// but these proxies are more general solution and can be used everywhere

	public struct WeaponPreview_Proxy(WeaponPreview instance)
	{
        private readonly WeaponPreview __instance = instance;

		private static TypedFieldInfo<WeaponPreview, GameObject> __originalObject = new("_originalObject");
		private static TypedFieldInfo<WeaponPreview, Item> __currentItem = new("_currentItem");

		public GameObject _originalObject { get { return __originalObject.Get(__instance); } set { __originalObject.Set(__instance, value); } }
		public Item _currentItem { get { return __currentItem.Get(__instance); } set { __currentItem.Set(__instance, value); } }
	}

	public struct WeaponPrefab_Proxy(WeaponPrefab instance)
	{
        private readonly WeaponPrefab __instance = instance;

		private static TypedFieldInfo<WeaponPrefab, Weapon> __weaponData = new("_weaponData");

		public Weapon _weaponData { get { return __weaponData.Get(__instance); } set { __weaponData.Set(__instance, value); } }
	}

	public struct Dress_Proxy(Dress instance)
	{
        private readonly Dress __instance = instance;

		private static TypedFieldInfo<Dress, PlayerBody> _PlayerBody = new("PlayerBody");

		public PlayerBody PlayerBody { get { return _PlayerBody.Get(__instance); } set { _PlayerBody.Set(__instance, value); } }
	}

	public struct CameraImage_Proxy(CameraImage instance)
	{
        private readonly CameraImage __instance = instance;

		private static TypedFieldInfo<CameraImage, RawImage> __rawImage = new("_rawImage");

		public RawImage _rawImage { get { return __rawImage.Get(__instance); } set { __rawImage.Set(__instance, value); } }
	}
}
