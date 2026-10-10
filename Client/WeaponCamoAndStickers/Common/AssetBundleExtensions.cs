using UnityEngine;

namespace SevenBoldPencil.Common
{
	public static class AssetBundleExtensions
	{
		public static T LoadAsset<T>(this AssetBundle bundle, string path) where T : class
		{
			return bundle.LoadAsset(path, Il2CppInterop.Runtime.Il2CppType.Of<T>()).TryCast<T>();
		}
	}
}
