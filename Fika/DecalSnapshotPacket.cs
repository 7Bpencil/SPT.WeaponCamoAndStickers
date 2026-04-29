//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Fika.Core.Networking.LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers.Fika
{
    // TODO this could use some CompressAndPutByteArray
    public class DecalSnapshotPacket : INetSerializable
    {
        public string ProfileId;
        public Dictionary<string, List<DecalInfo>> ItemDecals;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(ItemDecals.Count);
            foreach (var kvp in ItemDecals)
            {
                writer.Put(kvp.Key);
                writer.Put(kvp.Value.Count);
                foreach (var decal in kvp.Value)
                {
                    SerializeDecalInfo(writer, decal);
                }
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            var itemCount = reader.GetInt();
            ItemDecals = new Dictionary<string, List<DecalInfo>>(itemCount);
            for (var i = 0; i < itemCount; i++)
            {
                var itemId = reader.GetString();
                var decalCount = reader.GetInt();
                var decalsInfo = new List<DecalInfo>(decalCount);
                for (var j = 0; j < decalCount; j++)
                {
                    decalsInfo.Add(DeserializeDecalInfo(reader));
                }
                ItemDecals[itemId] = decalsInfo;
            }
        }

        private static void SerializeDecalInfo(NetDataWriter writer, DecalInfo d)
        {
            writer.Put(d.SchemaVersion);
            writer.Put(d.SaveTime);
            writer.Put(d.Name);
            writer.Put(d.Texture);
            writer.PutUnmanaged<Vector4>(d.TextureUV);
            writer.Put(d.TextureAngle);
            writer.PutUnmanaged<Vector4>(d.ColorHSVA);
            writer.Put(d.Mask);
            writer.PutUnmanaged<Vector4>(d.MaskUV);
            writer.Put(d.MaskAngle);
            writer.PutUnmanaged<Vector3>(d.LocalPosition);
            writer.PutUnmanaged<Vector3>(d.LocalEulerAngles);
            writer.PutUnmanaged<Vector3>(d.LocalScale);
            writer.Put(d.MaxAngle);
            writer.Put(d.IsVisible);
        }

        private static DecalInfo DeserializeDecalInfo(NetDataReader reader)
        {
            return new DecalInfo()
            {
                SchemaVersion = reader.GetInt(),
                SaveTime = reader.GetLong(),
                Name = reader.GetString(),
                Texture = reader.GetString(),
                TextureUV = reader.GetUnmanaged<Vector4>(),
                TextureAngle = reader.GetFloat(),
                ColorHSVA = reader.GetUnmanaged<Vector4>(),
                Mask = reader.GetString(),
                MaskUV = reader.GetUnmanaged<Vector4>(),
                MaskAngle = reader.GetFloat(),
                LocalPosition = reader.GetUnmanaged<Vector3>(),
                LocalEulerAngles = reader.GetUnmanaged<Vector3>(),
                LocalScale = reader.GetUnmanaged<Vector3>(),
                MaxAngle = reader.GetFloat(),
                IsVisible = reader.GetBool(),
            };
        }
    }
}
