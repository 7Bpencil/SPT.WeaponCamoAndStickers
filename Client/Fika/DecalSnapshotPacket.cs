using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace SevenBoldPencil.WeaponCamoAndStickers.Fika
{
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
                var decals = new List<DecalInfo>(decalCount);
                for (var j = 0; j < decalCount; j++)
                {
                    decals.Add(DeserializeDecalInfo(reader));
                }
                ItemDecals[itemId] = decals;
            }
        }

        private static void SerializeDecalInfo(NetDataWriter writer, DecalInfo d)
        {
            writer.Put(d.SchemaVersion);
            writer.Put(d.Name ?? "");
            writer.Put(d.Texture ?? "");
            PutVector4(writer, d.TextureUV);
            PutVector4(writer, d.ColorHSVA);
            writer.Put(d.Mask ?? "");
            PutVector4(writer, d.MaskUV);
            PutVector3(writer, d.LocalPosition);
            PutVector3(writer, d.LocalEulerAngles);
            PutVector3(writer, d.LocalScale);
            writer.Put(d.MaxAngle);
        }

        private static DecalInfo DeserializeDecalInfo(NetDataReader reader)
        {
            return new DecalInfo()
            {
                SchemaVersion = reader.GetInt(),
                Name = reader.GetString(),
                Texture = reader.GetString(),
                TextureUV = GetVector4(reader),
                ColorHSVA = GetVector4(reader),
                Mask = reader.GetString(),
                MaskUV = GetVector4(reader),
                LocalPosition = GetVector3(reader),
                LocalEulerAngles = GetVector3(reader),
                LocalScale = GetVector3(reader),
                MaxAngle = reader.GetFloat(),
            };
        }

        private static void PutVector3(NetDataWriter writer, Vector3 v)
        {
            writer.Put(v.x);
            writer.Put(v.y);
            writer.Put(v.z);
        }

        private static Vector3 GetVector3(NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        private static void PutVector4(NetDataWriter writer, Vector4 v)
        {
            writer.Put(v.x);
            writer.Put(v.y);
            writer.Put(v.z);
            writer.Put(v.w);
        }

        private static Vector4 GetVector4(NetDataReader reader)
        {
            return new Vector4(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }
}
