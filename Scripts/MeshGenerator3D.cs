using System.Collections.Generic;
using UnityEngine;

namespace PuzzleModules
{
    public static class MeshGenerator3D
    {
        /// <summary>
        /// Genera un Mesh 3D a partir de un contorno (lista de Vector3) mediante triangulación simple (fan triangulation).
        /// Se asume que el contorno forma un polígono simple y ordenado.
        /// </summary>
        public static Mesh GenerateMeshFromOutline(List<Vector3> outline)
        {
            Mesh mesh = new Mesh();
            int count = outline.Count;
            mesh.vertices = outline.ToArray();

            List<int> triangles = new List<int>();
            for (int i = 1; i < count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            return mesh;
        }
        public static Mesh GenerateMeshFromPuzzlePiece(PuzzlePiece piece, float connectorSize, int bezierSegments)
        {
            Mesh mesh = new Mesh();

            // Generar los vértices y triángulos de la pieza
            // Aquí puedes usar lógica personalizada para agregar conectores y bordes curvados
            Vector3[] vertices = GenerateVertices(piece, connectorSize, bezierSegments);
            int[] triangles = GenerateTriangles(vertices);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        private static Vector3[] GenerateVertices(PuzzlePiece piece, float connectorSize, int bezierSegments)
        {
            // Generar los vértices de la pieza, incluyendo los conectores
            // Aquí puedes usar curvas de Bézier para crear bordes curvados
            List<Vector3> vertices = new List<Vector3>();

            // Ejemplo básico: Crear un rectángulo con conectores
            vertices.Add(new Vector3(0, 0, 0)); // Esquina inferior izquierda
            vertices.Add(new Vector3(piece.Width, 0, 0)); // Esquina inferior derecha
            vertices.Add(new Vector3(piece.Width, piece.Height, 0)); // Esquina superior derecha
            vertices.Add(new Vector3(0, piece.Height, 0)); // Esquina superior izquierda

            // Agregar lógica para los conectores aquí...

            return vertices.ToArray();
        }
        private static int[] GenerateTriangles(Vector3[] vertices)
        {
            // Generar triángulos para la malla
            List<int> triangles = new List<int>();

            // Ejemplo básico: Crear un rectángulo
            triangles.Add(0);
            triangles.Add(1);
            triangles.Add(2);
            triangles.Add(0);
            triangles.Add(2);
            triangles.Add(3);

            return triangles.ToArray();
        }

    }
}
