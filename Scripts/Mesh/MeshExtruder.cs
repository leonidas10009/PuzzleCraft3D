using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public static class MeshExtruder
    {
        /// <summary>
        /// Divide un modelo 3D en piezas de rompecabezas y las extruye en 3D.
        /// </summary>
        /// <param name="analyzer">El analizador de topología de la malla.</param>
        /// <param name="outline">El contorno de la pieza (en 2D).</param>
        /// <param name="depth">La profundidad de la extrusión.</param>
        /// <returns>Una nueva malla extruida basada en el modelo original.</returns>
        public static Mesh ExtrudeMeshFromModel(MeshTopologyAnalyzer analyzer, List<Vector3> outline, float depth)
        {
            // Obtener los vértices y triángulos originales del modelo
            Vector3[] originalVertices = analyzer.vertices;
            int[] originalTriangles = analyzer.triangles;

            // Crear una lista para los nuevos vértices y triángulos
            List<Vector3> newVertices = new List<Vector3>();
            List<int> newTriangles = new List<int>();

            // Mapear el contorno 2D al modelo 3D original
            foreach (Vector3 point in outline)
            {
                // Proyectar el contorno en el modelo original (plano XY)
                Vector3 projectedPoint = ProjectPointOntoMesh(point, originalVertices);
                newVertices.Add(projectedPoint);
            }

            // Crear la cara trasera desplazada en el eje Z
            int vertexCount = newVertices.Count;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 backVertex = newVertices[i] - new Vector3(0, 0, depth);
                newVertices.Add(backVertex);
            }

            // Generar triángulos para la cara delantera
            for (int i = 1; i < vertexCount - 1; i++)
            {
                newTriangles.Add(0);
                newTriangles.Add(i);
                newTriangles.Add(i + 1);
            }

            // Generar triángulos para la cara trasera (invertidos)
            for (int i = 1; i < vertexCount - 1; i++)
            {
                newTriangles.Add(vertexCount);
                newTriangles.Add(vertexCount + i + 1);
                newTriangles.Add(vertexCount + i);
            }

            // Generar triángulos para los lados
            for (int i = 0; i < vertexCount; i++)
            {
                int next = (i + 1) % vertexCount;

                // Lado 1
                newTriangles.Add(i);
                newTriangles.Add(next);
                newTriangles.Add(i + vertexCount);

                // Lado 2
                newTriangles.Add(next);
                newTriangles.Add(next + vertexCount);
                newTriangles.Add(i + vertexCount);
            }

            // Crear la nueva malla
            Mesh extrudedMesh = new Mesh
            {
                vertices = newVertices.ToArray(),
                triangles = newTriangles.ToArray()
            };

            // Recalcular normales para iluminación correcta
            extrudedMesh.RecalculateNormals();

            return extrudedMesh;
        }

        /// <summary>
        /// Proyecta un punto 2D en el plano XY sobre la malla original.
        /// </summary>
        /// <param name="point">El punto 2D a proyectar.</param>
        /// <param name="vertices">Los vértices de la malla original.</param>
        /// <returns>El punto proyectado en el modelo 3D.</returns>
        private static Vector3 ProjectPointOntoMesh(Vector3 point, Vector3[] vertices)
        {
            // Buscar el vértice más cercano en la malla original
            Vector3 closestVertex = vertices[0];
            float minDistance = Vector3.Distance(point, closestVertex);

            foreach (Vector3 vertex in vertices)
            {
                float distance = Vector3.Distance(point, vertex);
                if (distance < minDistance)
                {
                    closestVertex = vertex;
                    minDistance = distance;
                }
            }

            return closestVertex;
        }
    }
}