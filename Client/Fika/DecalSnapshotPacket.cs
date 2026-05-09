//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Fika.Core.Networking.LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.ChangeEquipmentColor.Fika
{
    public class DecalSnapshotPacket : INetSerializable
    {
        public string ProfileId;
        public Dictionary<string, DecalInfo> ItemDecals;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(ItemDecals.Count);
            foreach (var kvp in ItemDecals)
            {
                writer.Put(kvp.Key);
                SerializeDecalInfo(writer, kvp.Value);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            var itemCount = reader.GetInt();
            ItemDecals = new Dictionary<string, DecalInfo>(itemCount);
            for (var i = 0; i < itemCount; i++)
            {
                var itemId = reader.GetString();
                var decalInfo = DeserializeDecalInfo(reader);
                ItemDecals[itemId] = decalInfo;
            }
        }

        private static void SerializeDecalInfo(NetDataWriter writer, DecalInfo d)
        {
            writer.Put(d.SchemaVersion);
            writer.Put(d.SaveTime);
            writer.PutUnmanaged<Vector4>(d.ColorHSVA);
        }

        private static DecalInfo DeserializeDecalInfo(NetDataReader reader)
        {
            return new DecalInfo()
            {
                SchemaVersion = reader.GetInt(),
                SaveTime = reader.GetLong(),
                ColorHSVA = reader.GetUnmanaged<Vector4>(),
            };
        }
    }
}
