//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using System.Collections.Generic;

using MainPlugin = SevenBoldPencil.WeaponCamoAndStickers.Plugin;

namespace SevenBoldPencil.WeaponCamoAndStickers.Fika
{
    [BepInPlugin("7Bpencil.WeaponCamoAndStickers.Fika", "7Bpencil.WeaponCamoAndStickers.Fika", "1.2.0")]
    [BepInDependency("7Bpencil.WeaponCamoAndStickers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
	{
		public Dictionary<string, DecalSnapshotPacket> PlayersDecals;
		public List<DecalSnapshotPacket> BotsDecals;

        private void Awake()
		{
			PlayersDecals = new();
			BotsDecals = new();

			FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);
			FikaEventDispatcher.SubscribeEvent<FikaGameCreatedEvent>(OnFikaGameCreatedEvent);
			FikaEventDispatcher.SubscribeEvent<PeerConnectedEvent>(OnPeerConnected);
			FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerDestroyedEvent>(OnFikaNetworkManagerDestroyedEvent);

			MainPlugin.Instance.IsFikaSupportEnabled = true;
			MainPlugin.Instance.IsFikaHeadless = FikaBackendUtils.IsHeadless; // TODO I am not totally sure IsHeadless value is valid at this point, so I keep updating it
		}

		private void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent e)
		{
			MainPlugin.Instance.IsFikaHeadless = FikaBackendUtils.IsHeadless;
            if (FikaBackendUtils.IsServer)
            {
				MainPlugin.Instance.OnBotWeaponCamoGenerated = OnBotWeaponCamoGenerated;
                e.Manager.RegisterPacket<DecalSnapshotPacket, NetPeer>(OnDecalSnapshotReceivedServer);
            }
            else
            {
                e.Manager.RegisterPacket<DecalSnapshotPacket>(OnDecalSnapshotReceivedClient);
            }
			if (FikaBackendUtils.IsServer && !FikaBackendUtils.IsHeadless)
			{
				var decals = GetLocalDecals();
				if (decals.ItemDecals.Count > 0)
				{
					PlayersDecals.Add(decals.ProfileId, decals);
				}
			}
			MainPlugin.Instance.IsFikaServer = new(FikaBackendUtils.IsServer);
		}

		private DecalSnapshotPacket GetLocalDecals()
		{
			var localProfileId = FikaBackendUtils.Profile.ProfileId;
			var decalsRepository = MainPlugin.Instance.SnapshotLocalDecals();
			var decals = new DecalSnapshotPacket()
			{
		        ProfileId = localProfileId,
		        ItemDecals = decalsRepository,
			};

			return decals;
		}

		private void OnBotWeaponCamoGenerated(Dictionary<string, List<DecalInfo>> itemWithDecals)
		{
			var decals = new DecalSnapshotPacket()
			{
		        ProfileId = null,
		        ItemDecals = itemWithDecals,
			};
			BotsDecals.Add(decals);

			Singleton<IFikaNetworkManager>.Instance.SendData(ref decals, DeliveryMethod.ReliableUnordered);
		}

		private void OnFikaGameCreatedEvent(FikaGameCreatedEvent e)
		{
			if (!FikaBackendUtils.IsServer && !FikaBackendUtils.IsHeadless)
			{
				var decals = GetLocalDecals();
				if (decals.ItemDecals.Count > 0)
				{
					Singleton<IFikaNetworkManager>.Instance.SendData(ref decals, DeliveryMethod.ReliableUnordered);
				}
			}
		}

		private void OnPeerConnected(PeerConnectedEvent e)
		{
			if (FikaBackendUtils.IsServer)
			{
	            foreach (var cached in PlayersDecals.Values)
	            {
	                var packet = cached;
	                e.NetworkManager.SendDataToPeer(ref packet, DeliveryMethod.ReliableUnordered, e.Peer);
	            }
	            foreach (var cached in BotsDecals)
	            {
	                var packet = cached;
	                e.NetworkManager.SendDataToPeer(ref packet, DeliveryMethod.ReliableUnordered, e.Peer);
	            }
			}
		}

		private void OnDecalSnapshotReceivedServer(DecalSnapshotPacket packet, NetPeer peer)
		{
            if (packet.ProfileId == null ||
				packet.ProfileId == FikaBackendUtils.Profile.ProfileId)
            {
                return;
            }
			if (PlayersDecals.TryAdd(packet.ProfileId, packet))
			{
				Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableUnordered);
				ApplyDecals(packet);
			}
		}

		private void OnDecalSnapshotReceivedClient(DecalSnapshotPacket packet)
		{
            if (packet.ProfileId == FikaBackendUtils.Profile.ProfileId)
            {
                return;
            }

			ApplyDecals(packet);
		}

		private void ApplyDecals(DecalSnapshotPacket packet)
		{
			if (!FikaBackendUtils.IsHeadless)
			{
                MainPlugin.Instance.IngestRemoteDecals(packet.ItemDecals);
			}
		}

		private void OnFikaNetworkManagerDestroyedEvent(FikaNetworkManagerDestroyedEvent e)
		{
			PlayersDecals.Clear();
			BotsDecals.Clear();

			MainPlugin.Instance.IsFikaHeadless = FikaBackendUtils.IsHeadless;
			MainPlugin.Instance.IsFikaServer = default;
			MainPlugin.Instance.OnBotWeaponCamoGenerated = default;
	        MainPlugin.Instance.WeaponsWaitingForRemoteCamo.Clear();
		}
	}
}
