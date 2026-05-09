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

        public Material LineMaterial;
        public Vector3[] Vertices = new Vector3[24];
        public List<Line> Lines = new(10);
        public List<Matrix4x4> Cubes = new(10);

        private void Awake()
        {
            LineMaterial = new Material(Shader.Find("Unlit/Color"));
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
            foreach (var cubeMatrix in Cubes)
            {
                var verticesCount = FillCubeVertices(cubeMatrix, Vertices);
                for (var i = 0; i < verticesCount; i++)
                {
                    GL.Vertex(Vertices[i]);
                }
            }
            GL.End();

            Lines.Clear();
            Cubes.Clear();
        }

        private static Vector3[] CubeVertices =
        [
            new(-0.5f, -0.5f, -0.5f),
            new(+0.5f, -0.5f, -0.5f),
            new(+0.5f, +0.5f, -0.5f),
            new(-0.5f, +0.5f, -0.5f),
            new(-0.5f, -0.5f, +0.5f),
            new(+0.5f, -0.5f, +0.5f),
            new(+0.5f, +0.5f, +0.5f),
            new(-0.5f, +0.5f, +0.5f),
        ];

        private static int FillCubeVertices(in Matrix4x4 trsMatrix, Vector3[] vertices)
        {
            var vertex1 = trsMatrix.MultiplyPoint3x4(CubeVertices[0]);
            var vertex2 = trsMatrix.MultiplyPoint3x4(CubeVertices[1]);
            var vertex3 = trsMatrix.MultiplyPoint3x4(CubeVertices[2]);
            var vertex4 = trsMatrix.MultiplyPoint3x4(CubeVertices[3]);
            var vertex5 = trsMatrix.MultiplyPoint3x4(CubeVertices[4]);
            var vertex6 = trsMatrix.MultiplyPoint3x4(CubeVertices[5]);
            var vertex7 = trsMatrix.MultiplyPoint3x4(CubeVertices[6]);
            var vertex8 = trsMatrix.MultiplyPoint3x4(CubeVertices[7]);

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

		public void OnDestroy()
		{
			if (LineMaterial)
			{
				Destroy(LineMaterial);
			}
		}
    }
}
