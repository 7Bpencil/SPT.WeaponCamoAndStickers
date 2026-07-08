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
        public Dictionary<string, List<DecalInfo>> Items;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(Items.Count);
            foreach (var (itemId, decalsInfo) in Items)
            {
                writer.Put(itemId);
                writer.Put(decalsInfo.Count);
                foreach (var decalInfo in decalsInfo)
                {
                    SerializeDecalInfo(writer, decalInfo);
                }
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            var itemCount = reader.GetInt();
            Items = new Dictionary<string, List<DecalInfo>>(itemCount);
            for (var i = 0; i < itemCount; i++)
            {
                var itemId = reader.GetString();
                var decalCount = reader.GetInt();
                var decalsInfo = new List<DecalInfo>(decalCount);
                for (var k = 0; k < decalCount; k++)
                {
                    decalsInfo.Add(DeserializeDecalInfo(reader));
                }
                Items[itemId] = decalsInfo;
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
            writer.Put(d.Bone);
            writer.PutUnmanaged<Vector3>(d.LocalPosition);
            writer.PutUnmanaged<Vector3>(d.LocalEulerAngles);
            writer.PutUnmanaged<Vector3>(d.LocalScale);
            writer.Put(d.MaxAngle);
            writer.Put(d.IsVisible);
            writer.PutEnum<DecalMirrorMode>(d.MirrorMode);
            writer.PutEnum<DecalPaintMode>(d.PaintMode);
            writer.Put(d.StencilType);
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
                Bone = reader.GetString(),
                LocalPosition = reader.GetUnmanaged<Vector3>(),
                LocalEulerAngles = reader.GetUnmanaged<Vector3>(),
                LocalScale = reader.GetUnmanaged<Vector3>(),
                MaxAngle = reader.GetFloat(),
                IsVisible = reader.GetBool(),
                MirrorMode = reader.GetEnum<DecalMirrorMode>(),
                PaintMode = reader.GetEnum<DecalPaintMode>(),
                StencilType = reader.GetByte(),
            };
        }
    }
}
