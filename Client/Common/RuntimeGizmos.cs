//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using System.Collections.Generic;
using UnityEngine;

namespace SevenBoldPencil.Common
{
    [RequireComponent(typeof(Camera))]
    public class RuntimeGizmos : MonoBehaviour
    {
        public struct Line
        {
            public Vector3 Start;
            public Vector3 End;
        }

        public struct Cube
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        public Material LineMaterial;
        public Vector3[] Vertices = new Vector3[24];
        public List<Line> Lines = new(10);
        public List<Cube> Cubes = new(10);

        private void Awake()
        {
            if (!LineMaterial)
            {
                LineMaterial = new Material(Shader.Find("Unlit/Color"));
            }
        }

        private void OnPostRender()
        {
            if (Cubes.Count == 0 && Lines.Count == 0)
            {
                return;
            }

            LineMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            foreach (var line in Lines)
            {
                GL.Vertex(line.Start);
                GL.Vertex(line.End);
            }
            foreach (var cube in Cubes)
            {
                var verticesCount = FillCubeVertices(cube.Position, cube.Rotation, cube.Scale, Vertices);
                for (var i = 0; i < verticesCount; i++)
                {
                    GL.Vertex(Vertices[i]);
                }
            }
            GL.End();

            Lines.Clear();
            Cubes.Clear();
        }

        private static int FillCubeVertices(Vector3 position, Quaternion rotation, Vector3 scale, Vector3[] vertices)
        {
            var size = scale * 0.5f;

            var vertex1 = new Vector3(position.x - size.x, position.y - size.y, position.z - size.z);
            var vertex2 = new Vector3(position.x + size.x, position.y - size.y, position.z - size.z);
            var vertex3 = new Vector3(position.x + size.x, position.y + size.y, position.z - size.z);
            var vertex4 = new Vector3(position.x - size.x, position.y + size.y, position.z - size.z);

            var vertex5 = new Vector3(position.x - size.x, position.y - size.y, position.z + size.z);
            var vertex6 = new Vector3(position.x + size.x, position.y - size.y, position.z + size.z);
            var vertex7 = new Vector3(position.x + size.x, position.y + size.y, position.z + size.z);
            var vertex8 = new Vector3(position.x - size.x, position.y + size.y, position.z + size.z);

            vertex1 = rotation * (vertex1 - position);
            vertex1 += position;

            vertex2 = rotation * (vertex2 - position);
            vertex2 += position;

            vertex3 = rotation * (vertex3 - position);
            vertex3 += position;

            vertex4 = rotation * (vertex4 - position);
            vertex4 += position;

            vertex5 = rotation * (vertex5 - position);
            vertex5 += position;

            vertex6 = rotation * (vertex6 - position);
            vertex6 += position;

            vertex7 = rotation * (vertex7 - position);
            vertex7 += position;

            vertex8 = rotation * (vertex8 - position);
            vertex8 += position;

            // square

            vertices[0] = vertex1;
            vertices[1] = vertex2;

            vertices[2] = vertex2;
            vertices[3] = vertex3;

            vertices[4] = vertex3;
            vertices[5] = vertex4;

            vertices[6] = vertex4;
            vertices[7] = vertex1;

            // other square

            vertices[8] = vertex5;
            vertices[9] = vertex6;

            vertices[10] = vertex6;
            vertices[11] = vertex7;

            vertices[12] = vertex7;
            vertices[13] = vertex8;

            vertices[14] = vertex8;
            vertices[15] = vertex5;

            // connectors

            vertices[16] = vertex1;
            vertices[17] = vertex5;

            vertices[18] = vertex2;
            vertices[19] = vertex6;

            vertices[20] = vertex3;
            vertices[21] = vertex7;

            vertices[22] = vertex4;
            vertices[23] = vertex8;

            return 24;
        }
    }
}
