//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using System.Collections.Generic;

using MainPlugin = SevenBoldPencil.MaterialEditor.Plugin;

namespace SevenBoldPencil.MaterialEditor.Fika
{
    public class Plugin
	{
		public Dictionary<string, DecalSnapshotPacket> PlayersMaterials;

        public void Awake()
		{
			PlayersMaterials = new();

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
                e.Manager.RegisterPacket<DecalSnapshotPacket, NetPeer>(OnDecalSnapshotReceivedServer);
            }
            else
            {
                e.Manager.RegisterPacket<DecalSnapshotPacket>(OnDecalSnapshotReceivedClient);
            }
			if (FikaBackendUtils.IsServer && !FikaBackendUtils.IsHeadless)
			{
				var materials = GetLocalMaterials();
				if (materials.Items.Count > 0)
				{
					PlayersMaterials.Add(materials.ProfileId, materials);
				}
			}
		}

		private DecalSnapshotPacket GetLocalMaterials()
		{
			var localProfileId = FikaBackendUtils.Profile.ProfileId;
			var materialsRepository = MainPlugin.Instance.SnapshotLocalMaterials();
			var materials = new DecalSnapshotPacket()
			{
		        ProfileId = localProfileId,
		        Items = materialsRepository,
			};

			return materials;
		}

		private void OnFikaGameCreatedEvent(FikaGameCreatedEvent e)
		{
			if (!FikaBackendUtils.IsServer && !FikaBackendUtils.IsHeadless)
			{
				var materials = GetLocalMaterials();
				if (materials.Items.Count > 0)
				{
					Singleton<IFikaNetworkManager>.Instance.SendData(ref materials, DeliveryMethod.ReliableUnordered);
				}
			}
		}

		private void OnPeerConnected(PeerConnectedEvent e)
		{
			if (FikaBackendUtils.IsServer)
			{
	            foreach (var cached in PlayersMaterials.Values)
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
			if (PlayersMaterials.TryAdd(packet.ProfileId, packet))
			{
				Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableUnordered);
				ApplyMaterials(packet);
			}
		}

		private void OnDecalSnapshotReceivedClient(DecalSnapshotPacket packet)
		{
            if (packet.ProfileId == FikaBackendUtils.Profile.ProfileId)
            {
                return;
            }

			ApplyMaterials(packet);
		}

		private void ApplyMaterials(DecalSnapshotPacket packet)
		{
			if (!FikaBackendUtils.IsHeadless)
			{
                MainPlugin.Instance.IngestRemoteMaterials(packet.Items);
			}
		}

		private void OnFikaNetworkManagerDestroyedEvent(FikaNetworkManagerDestroyedEvent e)
		{
			PlayersMaterials.Clear();

			MainPlugin.Instance.IsFikaHeadless = FikaBackendUtils.IsHeadless;
		}
	}
}
