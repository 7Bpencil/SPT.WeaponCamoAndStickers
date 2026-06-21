//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Fika.Core.Networking.LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.MaterialEditor.Fika
{
    public class MaterialSnapshotPacket : INetSerializable
    {
        public string ProfileId;
        public Dictionary<string, MaterialsInfo> Items;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProfileId);
            writer.Put(Items.Count);
            foreach (var (itemId, materialsInfo) in Items)
            {
                writer.Put(itemId);
                SerializeMaterialsInfo(writer, materialsInfo);
            }
        }

        private static void SerializeMaterialsInfo(NetDataWriter writer, MaterialsInfo d)
        {
            writer.Put(d.SchemaVersion);
            writer.Put(d.SaveTime);
            writer.Put(d.Materials.Count);
            foreach (var (materialName, materialInfo) in d.Materials)
            {
                writer.Put(materialName);
                SerializeMaterialInfo(writer, materialInfo);
            }
        }

        private static void SerializeMaterialInfo(NetDataWriter writer, MaterialInfo d)
        {
            writer.Put(d.SchemaVersion);
            writer.PutUnmanaged<Vector3>(d.ColorHSV);
            writer.PutUnmanaged<Vector3>(d.SpecColorHSV);
    		writer.Put(d.Glossness);
    		writer.Put(d.Specularness);
            writer.PutUnmanaged<Vector3>(d.ReflectColorHSV);
            writer.Put(d.Texture);
            writer.PutUnmanaged<Vector4>(d.TextureUV);
            writer.PutUnmanaged<Vector2>(d.SpecVals);
            writer.PutUnmanaged<Vector2>(d.DefVals);
            writer.Put(d.CompensateSpecular);
        }

        public void Deserialize(NetDataReader reader)
        {
            ProfileId = reader.GetString();
            var itemCount = reader.GetInt();
            Items = new Dictionary<string, MaterialsInfo>(itemCount);

            for (var i = 0; i < itemCount; i++)
            {
                var itemId = reader.GetString();
                var materialsInfo = DeserializeMaterialsInfo(reader);
                Items.Add(itemId, materialsInfo);
            }
        }

        private static MaterialsInfo DeserializeMaterialsInfo(NetDataReader reader)
        {
            var schemaVersion = reader.GetInt();
            var saveTime = reader.GetLong();
            var materialsCount = reader.GetInt();
            var materials = new Dictionary<string, MaterialInfo>(materialsCount);

            for (var i = 0; i < materialsCount; i++)
            {
                var materialName = reader.GetString();
                var material = DeserializeMaterialInfo(reader);
                materials.Add(materialName, material);
            }

            return new()
            {
                SchemaVersion = schemaVersion,
                SaveTime = saveTime,
                Materials = materials,
            };
        }

        private static MaterialInfo DeserializeMaterialInfo(NetDataReader reader)
        {
            return new()
            {
                SchemaVersion = reader.GetInt(),
                ColorHSV = reader.GetUnmanaged<Vector3>(),
                SpecColorHSV = reader.GetUnmanaged<Vector3>(),
        		Glossness = reader.GetFloat(),
        		Specularness = reader.GetFloat(),
                ReflectColorHSV = reader.GetUnmanaged<Vector3>(),
                Texture = reader.GetString(),
                TextureUV = reader.GetUnmanaged<Vector4>(),
                SpecVals = reader.GetUnmanaged<Vector2>(),
                DefVals = reader.GetUnmanaged<Vector2>(),
                CompensateSpecular = reader.GetBool(),
            };
        }

    }
}
