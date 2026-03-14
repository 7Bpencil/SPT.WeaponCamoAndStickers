using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamo
{
    [BepInPlugin("7Bpencil.WeaponCamo", "7Bpencil.WeaponCamo", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
		public ManualLogSource LoggerInstance;

        public static ConfigEntry<KeyboardShortcut> SpawnButton;


        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            SpawnButton = Config.Bind("Main", "Spawn Button", new KeyboardShortcut(KeyCode.F4), "Spawn Button");
        }

		public void Update()
		{
			if (Input.GetKeyDown(SpawnButton.Value.MainKey))
			{
			}
		}

    }
}
