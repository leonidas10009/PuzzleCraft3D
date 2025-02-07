using UnityEngine;
using System.Collections.Generic;
using UnityMeshSimplifier;
using LibTessDotNet; // Para triangulación avanzada


namespace PuzzleModules
{
    public static class MeshGenerator
    {
        /// <summary>
        /// Genera un Mesh 2D a partir de un contorno (lista de puntos) utilizando LibTessDotNet para triangulación avanzada.
        /// </summary>
       public static Mesh GenerateMeshFromOutline(List<Vector3> outline)
{
    Mesh mesh = new Mesh();

    // Convertir el contorno a un formato compatible con LibTessDotNet
    Tess tess = new Tess();
    ContourVertex[] contour = new ContourVertex[outline.Count];
    for (int i = 0; i < outline.Count; i++)
    {
        contour[i] = new ContourVertex
        {
            Position = new Vec3(outline[i].x, outline[i].y, 0)
        };
    }
    tess.AddContour(contour);

    // Triangulación
    tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

    // Verificar si la triangulación fue exitosa
    if (tess.Vertices.Length == 0 || tess.Elements.Length == 0)
    {
        Debug.LogError("La triangulación falló. Verifica el contorno proporcionado.");
        return null;
    }

    // Convertir los resultados de LibTessDotNet a un formato compatible con Unity
    Vector3[] vertices = new Vector3[tess.Vertices.Length];
    for (int i = 0; i < tess.Vertices.Length; i++)
    {
        vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0);
    }

    int[] triangles = new int[tess.Elements.Length];
    for (int i = 0; i < tess.Elements.Length; i++)
    {
        triangles[i] = tess.Elements[i];
    }

    // Asignar vértices y triángulos a la malla
    mesh.vertices = vertices;
    mesh.triangles = triangles;

    // Generar UVs
    mesh.uv = GenerateUVs(vertices);

    mesh.RecalculateNormals();
    mesh.RecalculateBounds();

    return mesh;
}

        /// <summary>
        /// Genera UVs normalizados para una malla.
        /// </summary>
        internal static Vector2[] GenerateUVs(Vector3[] vertices)
        {
            Vector2[] uvs = new Vector2[vertices.Length];
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // Encontrar los límites del contorno
            foreach (Vector3 vertex in vertices)
            {
                if (vertex.x < minX) minX = vertex.x;
                if (vertex.x > maxX) maxX = vertex.x;
                if (vertex.y < minY) minY = vertex.y;
                if (vertex.y > maxY) maxY = vertex.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            // Normalizar las coordenadas UV
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2(
                    (vertices[i].x - minX) / width,
                    (vertices[i].y - minY) / height
                );
            }

            return uvs;
        }

        /// <summary>
        /// Genera una envolvente convexa (Convex Hull) de la malla y la usa como base para optimizar la simplificación.
        /// </summary>
        public static Mesh OptimizeMeshWithSupport(Mesh originalMesh, float simplificationQuality = 0.5f)
        {
            // Obtener los vértices de la malla original
            Vector3[] vertices = originalMesh.vertices;

            // Generar la envolvente convexa (Convex Hull) usando ConvexHullCalculator
            List<Vector3> points = new List<Vector3>(vertices);
            List<ConvexHullCalculator.Face> hullFaces = ConvexHullCalculator.CalculateConvexHull(points);

            if (hullFaces == null || hullFaces.Count == 0)
            {
                Debug.LogError("No se pudo generar la envolvente convexa. Verifica los datos de entrada.");
                return null;
            }

            // Crear listas para los vértices y triángulos de la malla de soporte
            List<Vector3> hullVertices = new List<Vector3>();
            List<int> hullTriangles = new List<int>();

            // Mapear las caras del Convex Hull a los datos de la malla
            foreach (var face in hullFaces)
            {
                int indexA = AddVertex(hullVertices, face.A);
                int indexB = AddVertex(hullVertices, face.B);
                int indexC = AddVertex(hullVertices, face.C);

                hullTriangles.Add(indexA);
                hullTriangles.Add(indexB);
                hullTriangles.Add(indexC);
            }

            // Crear la malla de soporte
            Mesh supportMesh = new Mesh();
            supportMesh.vertices = hullVertices.ToArray();
            supportMesh.triangles = hullTriangles.ToArray();
            supportMesh.RecalculateNormals();

            // Simplificar la malla de soporte
            MeshSimplifier simplifier = new MeshSimplifier();
            simplifier.Initialize(supportMesh);
            simplifier.SimplifyMesh(simplificationQuality);
            Mesh optimizedMesh = simplifier.ToMesh();

            return optimizedMesh;
        }

        /// <summary>
        /// Agrega un vértice a la lista si no existe ya y devuelve su índice.
        /// </summary>
        private static int AddVertex(List<Vector3> vertices, Vector3 vertex)
        {
            int index = vertices.IndexOf(vertex);
            if (index == -1)
            {
                vertices.Add(vertex);
                return vertices.Count - 1;
            }
            return index;
        }
    }
}