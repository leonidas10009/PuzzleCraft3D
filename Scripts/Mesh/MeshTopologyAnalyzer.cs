using UnityEngine;
using System.Collections.Generic;


namespace PuzzleModules
{
    /// <summary>
    /// Analiza la topología de una malla: extrae vértices, caras, aristas y bordes.
    /// Este módulo se puede ampliar para incluir estructuras half-edge u otros algoritmos.
    /// </summary>
    public class MeshTopologyAnalyzer
    {
        public Mesh mesh;
        public Vector3[] vertices;
        public int[] triangles;
        public List<Edge> edges;

        public MeshTopologyAnalyzer(Mesh mesh)
        {
            this.mesh = mesh;
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            edges = new List<Edge>();

            AnalyzeMesh();
        }

        /// <summary>
        /// Procesa la malla para extraer la topología básica.
        /// </summary>
        void AnalyzeMesh()
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                AddEdge(i1, i2);
                AddEdge(i2, i3);
                AddEdge(i3, i1);
            }
        }

        void AddEdge(int indexA, int indexB)
        {
            Edge newEdge = new Edge(indexA, indexB);
            if (!edges.Contains(newEdge))
            {
                edges.Add(newEdge);
            }
        }
    }

    /// <summary>
    /// Estructura básica para representar una arista en la malla.
    /// </summary>
    public struct Edge
    {
        public int indexA;
        public int indexB;

        public Edge(int a, int b)
        {
            if (a < b)
            {
                indexA = a;
                indexB = b;
            }
            else
            {
                indexA = b;
                indexB = a;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge))
                return false;
            Edge other = (Edge)obj;
            return indexA == other.indexA && indexB == other.indexB;
        }

        public override int GetHashCode()
        {
            return indexA.GetHashCode() ^ indexB.GetHashCode();
        }
    }
}
