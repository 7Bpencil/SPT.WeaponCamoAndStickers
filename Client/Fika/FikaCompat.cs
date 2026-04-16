using System.Collections.Generic;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;

namespace SevenBoldPencil.WeaponCamoAndStickers.Fika
{
    public static class FikaCompat
    {
        private static readonly Dictionary<string, DecalSnapshotPacket> Cache = new();
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStarted);
            FikaEventDispatcher.SubscribeEvent<PeerConnectedEvent>(OnPeerConnected);
            FikaEventDispatcher.SubscribeEvent<FikaGameEndedEvent>(OnGameEnded);

            Plugin.Instance.LoggerInstance.LogInfo("FikaCompat: initialized");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent e)
        {
            if (FikaBackendUtils.IsServer)
            {
                e.Manager.RegisterPacket<DecalSnapshotPacket, NetPeer>(OnSnapshotReceivedServer);
            }
            else
            {
                e.Manager.RegisterPacket<DecalSnapshotPacket>(OnSnapshotReceivedClient);
            }
        }

        private static void OnGameWorldStarted(GameWorldStartedEvent e)
        {
            var profileId = FikaBackendUtils.Profile?.ProfileId;
            if (string.IsNullOrEmpty(profileId))
            {
                Plugin.Instance.LoggerInstance.LogWarning("FikaCompat: no local profile id, skipping snapshot send");
                return;
            }

            var itemDecals = Plugin.Instance.SnapshotLocalDecals();
            if (itemDecals.Count == 0)
            {
                return;
            }

            var packet = new DecalSnapshotPacket()
            {
                ProfileId = profileId,
                ItemDecals = itemDecals,
            };

            var manager = Singleton<IFikaNetworkManager>.Instance;
            if (manager == null)
            {
                Plugin.Instance.LoggerInstance.LogWarning("FikaCompat: no network manager, cannot send snapshot");
                return;
            }

            manager.SendData(ref packet, DeliveryMethod.ReliableUnordered, broadcast: true);

            // host's own SendData goes to peers only (not loopback), so cache ourselves for late-join replay
            if (FikaBackendUtils.IsServer)
            {
                Cache[profileId] = packet;
            }

            Plugin.Instance.LoggerInstance.LogInfo($"FikaCompat: sent snapshot ({itemDecals.Count} items)");
        }

        private static void OnPeerConnected(PeerConnectedEvent e)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var fikaServer = Singleton<FikaServer>.Instance;
            if (fikaServer == null)
            {
                return;
            }

            foreach (var cached in Cache.Values)
            {
                var packet = cached;
                fikaServer.SendDataToPeer(ref packet, DeliveryMethod.ReliableUnordered, e.Peer);
            }

            Plugin.Instance.LoggerInstance.LogInfo($"FikaCompat: replayed {Cache.Count} cached snapshots to new peer");
        }

        private static void OnGameEnded(FikaGameEndedEvent e)
        {
            Cache.Clear();
            Plugin.Instance.ClearAllRemoteDecals();
        }

        private static void OnSnapshotReceivedServer(DecalSnapshotPacket packet, NetPeer peer)
        {
            var localProfileId = FikaBackendUtils.Profile?.ProfileId;
            if (packet.ProfileId == localProfileId)
            {
                return;
            }

            Cache[packet.ProfileId] = packet;

            // headless renders nothing, so skip creating decal GameObjects/materials locally
            if (!FikaBackendUtils.IsHeadless)
            {
                ApplyRemoteSnapshot(packet);
            }

            // rebroadcast to every peer except the sender
            var fikaServer = Singleton<FikaServer>.Instance;
            if (fikaServer != null)
            {
                fikaServer.SendData(ref packet, DeliveryMethod.ReliableUnordered, peer);
            }
        }

        private static void OnSnapshotReceivedClient(DecalSnapshotPacket packet)
        {
            var localProfileId = FikaBackendUtils.Profile?.ProfileId;
            if (packet.ProfileId == localProfileId)
            {
                return;
            }

            ApplyRemoteSnapshot(packet);
        }

        private static void ApplyRemoteSnapshot(DecalSnapshotPacket packet)
        {
            foreach (var kvp in packet.ItemDecals)
            {
                Plugin.Instance.IngestRemoteDecals(kvp.Key, kvp.Value);
            }

            Plugin.Instance.LoggerInstance.LogInfo($"FikaCompat: applied snapshot from {packet.ProfileId} ({packet.ItemDecals.Count} items)");
        }
    }
}
